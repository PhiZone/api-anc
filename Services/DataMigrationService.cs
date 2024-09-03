using System.Data;
using System.Net.Sockets;
using System.Reflection;
using MySqlConnector;
using PhiZoneApi.Constants;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

// ReSharper disable StringLiteralTypo

namespace PhiZoneApi.Services;

public class DataMigrationService(IServiceProvider serviceProvider) : IHostedService
{
    private IAdmissionRepository _admissionRepository = null!;
    private IApplicationRepository _applicationRepository = null!;
    private IChapterRepository _chapterRepository = null!;
    private IChartRepository _chartRepository = null!;
    private IConfiguration _configuration = null!;
    private ILogger<DataMigrationService> _logger = null!;
    private ISongRepository _songRepository = null!;
    private IUserRepository _userRepository = null!;
    private IApplicationUserRepository _applicationUserRepository = null!;
    private readonly List<Guid> _migratedChapters = [];
    private readonly List<Guid> _migratedSongs = [];
    private readonly Dictionary<int, int> _userIdDict = new();
    private readonly Dictionary<int, int> _reverseUserIdDict = new();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        _logger = scope.ServiceProvider.GetRequiredService<ILogger<DataMigrationService>>();
        _configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        _applicationRepository = scope.ServiceProvider.GetRequiredService<IApplicationRepository>();
        _chapterRepository = scope.ServiceProvider.GetRequiredService<IChapterRepository>();
        _admissionRepository = scope.ServiceProvider.GetRequiredService<IAdmissionRepository>();
        _songRepository = scope.ServiceProvider.GetRequiredService<ISongRepository>();
        _chartRepository = scope.ServiceProvider.GetRequiredService<IChartRepository>();
        _userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        _applicationUserRepository = scope.ServiceProvider.GetRequiredService<IApplicationUserRepository>();
        scope.ServiceProvider.GetRequiredService<IPlayConfigurationRepository>();

        _logger.LogInformation(LogEvents.DataMigration, "Data migration started");
        try
        {
            await MigrateDataAsync(cancellationToken);
            _logger.LogInformation(LogEvents.DataMigration, "Data migration finished");
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Data migration failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task MigrateDataAsync(CancellationToken cancellationToken)
    {
        await using var mysqlConnection =
            new MySqlConnection(_configuration.GetConnectionString("PhigrimMySQLConnection"));
        await mysqlConnection.OpenAsync(cancellationToken);
        await MigrateChapters(mysqlConnection, cancellationToken);
        await MigrateSongs(mysqlConnection, cancellationToken);
        await MigrateSongAdmissions(mysqlConnection, cancellationToken);
        await MigrateCharts(mysqlConnection, cancellationToken);
        await MigrateApplications(mysqlConnection, cancellationToken);
        await MigrateUsers(mysqlConnection, cancellationToken);
        await MigrateApplicationUsers(mysqlConnection, cancellationToken);
    }

    private async Task MigrateChapters(MySqlConnection mysqlConnection, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating chapters...");
            await using var transaction = await mysqlConnection.BeginTransactionAsync(cancellationToken);
            var command = new MySqlCommand { Connection = mysqlConnection, Transaction = transaction };
            var chapters = await _chapterRepository.GetChaptersAsync();
            command.CommandText = string.Join('\n',
                from chapter in chapters select GetInsertCommand(chapter, "Chapters"));
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _migratedChapters.AddRange(chapters.Select(e => e.Id));
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "An error occurred whilst migrating chapters");
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateChapters(mysqlConnection, cancellationToken);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "An error occurred whilst migrating chapters");
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateChapters(mysqlConnection, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "An error occurred whilst migrating chapters");
        }
    }

    private async Task MigrateSongs(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startPosition = 0)
    {
        var position = startPosition;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating songs...");
            ICollection<Song> songs;
            do
            {
                await using var transaction = await mysqlConnection.BeginTransactionAsync(cancellationToken);
                var command = new MySqlCommand { Connection = mysqlConnection, Transaction = transaction };
                songs = await _songRepository.GetSongsAsync(position: position, take: 50);
                if (songs.Count == 0) break;
                command.CommandText = string.Join('\n', from song in songs select GetInsertCommand(song, "Songs"));
                await command.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                _migratedSongs.AddRange(songs.Select(e => e.Id));
                position += 50;
            } while (songs.Count > 0);
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating songs (at position {Position})", position);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateSongs(mysqlConnection, cancellationToken, position);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating songs (at position {Position})", position);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateSongs(mysqlConnection, cancellationToken, position);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating songs (at position {Position})", position);
        }
    }

    private async Task MigrateSongAdmissions(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startPosition = 0)
    {
        var position = startPosition;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating song admissions...");
            ICollection<Admission> songAdmissions;
            do
            {
                await using var transaction = await mysqlConnection.BeginTransactionAsync(cancellationToken);
                var command = new MySqlCommand { Connection = mysqlConnection, Transaction = transaction };
                songAdmissions = await _admissionRepository.GetAdmissionsAsync(position: position, take: 50,
                    predicate: e => _migratedChapters.Contains(e.AdmitterId) && _migratedSongs.Contains(e.AdmitteeId));
                if (songAdmissions.Count == 0) break;
                command.CommandText = string.Join('\n',
                    from songAdmission in songAdmissions select GetInsertCommand(songAdmission, "Admissions"));
                await command.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                position += 50;
            } while (songAdmissions.Count > 0);
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating song admissions (at position {Position})", position);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateSongAdmissions(mysqlConnection, cancellationToken, position);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating song admissions (at position {Position})", position);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateSongAdmissions(mysqlConnection, cancellationToken, position);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating song admissions (at position {Position})", position);
        }
    }

    private async Task MigrateCharts(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startPosition = 0)
    {
        var position = startPosition;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating charts...");
            ICollection<Chart> charts;
            do
            {
                await using var transaction = await mysqlConnection.BeginTransactionAsync(cancellationToken);
                var command = new MySqlCommand { Connection = mysqlConnection, Transaction = transaction };
                charts = await _chartRepository.GetChartsAsync(position: position, take: 50,
                    predicate: e => _migratedSongs.Contains(e.SongId));
                if (charts.Count == 0) break;
                command.CommandText = string.Join('\n', from chart in charts select GetInsertCommand(chart, "Charts"));
                await command.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                position += 50;
            } while (charts.Count > 0);
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating charts (at position {Position})", position);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateCharts(mysqlConnection, cancellationToken, position);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating charts (at position {Position})", position);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateCharts(mysqlConnection, cancellationToken, position);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating charts (at position {Position})", position);
        }
    }

    private async Task MigrateApplications(MySqlConnection mysqlConnection, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating applications...");
            await using var transaction = await mysqlConnection.BeginTransactionAsync(cancellationToken);
            var command = new MySqlCommand { Connection = mysqlConnection, Transaction = transaction };
            var applications = await _applicationRepository.GetApplicationsAsync();
            command.CommandText = string.Join('\n',
                from application in applications select GetInsertCommand(application, "Applications"));
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "An error occurred whilst migrating applications");
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateApplications(mysqlConnection, cancellationToken);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "An error occurred whilst migrating applications");
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateApplications(mysqlConnection, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "An error occurred whilst migrating applications");
        }
    }

    private async Task MigrateUsers(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startPosition = 0)
    {
        var position = startPosition;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating users...");
            ICollection<User> users;
            var initialUserCount =
                Convert.ToInt32(
                    await new MySqlCommand("SELECT COUNT(*) FROM Users", mysqlConnection).ExecuteScalarAsync(
                        cancellationToken));
            do
            {
                await using var transaction = await mysqlConnection.BeginTransactionAsync(cancellationToken);
                var command = new MySqlCommand { Connection = mysqlConnection, Transaction = transaction };
                var applicationId = Guid.Parse("3241d866-814e-4012-881a-72c55fe5d58e");
                users = await _userRepository.GetUsersAsync(position: position, take: 50,
                    predicate: e => e.ApplicationLinks.Any(f => f.ApplicationId == applicationId));
                if (users.Count == 0) break;
                var idBegin = initialUserCount + position + 1;
                command.CommandText = string.Join('\n', from user in users.Select((e, i) =>
                    {
                        var newId = idBegin + i;
                        _userIdDict.Add(e.Id, newId);
                        _reverseUserIdDict.Add(newId, e.Id);
                        e.Id = newId;
                        return e;
                    })
                    select GetInsertCommand(user, "Users"));
                await command.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                position += 50;
            } while (users.Count > 0);
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating users (at position {Position})", position);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateUsers(mysqlConnection, cancellationToken, position);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating users (at position {Position})", position);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateUsers(mysqlConnection, cancellationToken, position);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating users (at position {Position})", position);
        }
    }

    private async Task MigrateApplicationUsers(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startPosition = 0)
    {
        var position = startPosition;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating user application links...");
            ICollection<ApplicationUser> applicationUsers;
            do
            {
                await using var transaction = await mysqlConnection.BeginTransactionAsync(cancellationToken);
                var command = new MySqlCommand { Connection = mysqlConnection, Transaction = transaction };
                var applicationId = Guid.Parse("3241d866-814e-4012-881a-72c55fe5d58e");
                applicationUsers = await _applicationUserRepository.GetRelationsAsync(position: position, take: 50,
                    predicate: e => e.ApplicationId == applicationId);
                if (applicationUsers.Count == 0) break;
                command.CommandText = string.Join('\n',
                    from applicationUser in applicationUsers
                    select GetInsertCommand(applicationUser, "ApplicationUsers"));
                await command.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                position += 50;
            } while (applicationUsers.Count > 0);
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating user application links (at position {Position})", position);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateApplicationUsers(mysqlConnection, cancellationToken, position);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating user application links (at position {Position})", position);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateApplicationUsers(mysqlConnection, cancellationToken, position);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating user application links (at position {Position})", position);
        }
    }

    private string GetInsertCommand(object entry, string table)
    {
        var type = entry.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Dictionary<string, string?> propertyMap = new();

        foreach (var property in properties)
        {
            if (!IsPrimitiveType(property.PropertyType) || property.PropertyType.IsSubclassOf(typeof(Delegate)) ||
                (property.GetMethod != null && property.GetMethod.IsStatic) || property.Name == "DateFileUpdated")
                continue;

            var value = property.GetValue(entry);
            if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
            {
                value = Convert.ToBoolean(value) ? 1 : 0;
            }
            else if (property.PropertyType.IsEnum)
            {
                value = Convert.ToInt32(value);
            }
            else if (property.PropertyType == typeof(DateTimeOffset) ||
                     property.PropertyType == typeof(DateTimeOffset?))
            {
                value = ((DateTimeOffset?)value)?.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else if ((property.PropertyType == typeof(int) || property.PropertyType == typeof(int?)) &&
                     property.Name.EndsWith("Id") && property.Name != "RegionId" && value != null)
            {
                if (property.Name == "UserId")
                {
                    value = _userIdDict[(int)value];
                }
                else if (property.Name != "Id")
                {
                    value = 1;
                }
                else
                {
                    propertyMap.Add("PhiZoneId", $"'{_reverseUserIdDict[(int)value].ToString().Replace("'", "''")}'");
                }
            }

            if (value != null) propertyMap.Add(property.Name, $"'{value.ToString()!.Replace("'", "''")}'");
        }

        return "INSERT INTO " + table + " (" + string.Join(", ", propertyMap.Keys) + ") VALUES (" +
               string.Join(", ", propertyMap.Values) + ") ON DUPLICATE KEY UPDATE " +
               string.Join(", ", propertyMap.Select(e => $"{e.Key} = {e.Value}")) + ";";
    }

    private static bool IsPrimitiveType(Type type)
    {
        var types = new[]
        {
            typeof(bool), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
            typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal), typeof(string),
            typeof(char), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan), typeof(Guid), typeof(bool?),
            typeof(byte?), typeof(sbyte?), typeof(short?), typeof(ushort?), typeof(int?), typeof(uint?),
            typeof(long?), typeof(ulong?), typeof(float?), typeof(double?), typeof(decimal?), typeof(char?),
            typeof(DateTime?), typeof(DateTimeOffset?), typeof(TimeSpan?), typeof(Guid?)
        };

        return types.Contains(type) || type.IsEnum;
    }
}
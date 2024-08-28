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
    private IChapterRepository _chapterRepository = null!;
    private IChartRepository _chartRepository = null!;
    private IConfiguration _configuration = null!;
    private ILogger<DataMigrationService> _logger = null!;
    private ISongRepository _songRepository = null!;
    private IUserRepository _userRepository = null!;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        _logger = scope.ServiceProvider.GetRequiredService<ILogger<DataMigrationService>>();
        _configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        _chapterRepository = scope.ServiceProvider.GetRequiredService<IChapterRepository>();
        _admissionRepository = scope.ServiceProvider.GetRequiredService<IAdmissionRepository>();
        _songRepository = scope.ServiceProvider.GetRequiredService<ISongRepository>();
        _chartRepository = scope.ServiceProvider.GetRequiredService<IChartRepository>();
        _userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
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
        await MigrateUsers(mysqlConnection, cancellationToken);
    }

    private async Task MigrateChapters(MySqlConnection mysqlConnection, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating chapters...");
            await using var transaction = await mysqlConnection.BeginTransactionAsync(cancellationToken);
            var command = new MySqlCommand { Connection = mysqlConnection, Transaction = transaction };
            command.CommandText = string.Join('\n',
                from chapter in await _chapterRepository.GetChaptersAsync()
                select GetInsertCommand(chapter, "Chapters"));
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
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
            await using var transaction = await mysqlConnection.BeginTransactionAsync(cancellationToken);
            ICollection<Song> songs;
            do
            {
                var command = new MySqlCommand { Connection = mysqlConnection, Transaction = transaction };
                songs = await _songRepository.GetSongsAsync(position: position, take: 50);
                command.CommandText = string.Join('\n', from song in songs select GetInsertCommand(song, "Songs"));
                await command.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                position += 50;
            } while (songs.Count > 0);
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating songs (Position: {Position})", position);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateSongs(mysqlConnection, cancellationToken, position);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating songs (Position: {Position})", position);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateSongs(mysqlConnection, cancellationToken, position);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating songs (Position: {Position})", position);
        }
    }

    private async Task MigrateSongAdmissions(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startPosition = 0)
    {
        var position = startPosition;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating song admissions...");
            await using var transaction = await mysqlConnection.BeginTransactionAsync(cancellationToken);
            var chapters = (await _chapterRepository.GetChaptersAsync()).Select(e => e.Id);
            ICollection<Admission> songAdmissions;
            do
            {
                var command = new MySqlCommand { Connection = mysqlConnection, Transaction = transaction };
                songAdmissions = await _admissionRepository.GetAdmissionsAsync(position: position, take: 50,
                    predicate: e => chapters.Contains(e.AdmitterId));
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
                "An error occurred whilst migrating song admissions (Position: {Position})", position);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateSongAdmissions(mysqlConnection, cancellationToken, position);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating song admissions (Position: {Position})", position);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateSongAdmissions(mysqlConnection, cancellationToken, position);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating song admissions (Position: {Position})", position);
        }
    }

    private async Task MigrateCharts(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startPosition = 0)
    {
        var position = startPosition;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating charts...");
            await using var transaction = await mysqlConnection.BeginTransactionAsync(cancellationToken);
            ICollection<Chart> charts;
            do
            {
                var command = new MySqlCommand { Connection = mysqlConnection, Transaction = transaction };
                charts = await _chartRepository.GetChartsAsync(position: position, take: 50);
                command.CommandText = string.Join('\n', from chart in charts select GetInsertCommand(chart, "Charts"));
                await command.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                position += 50;
            } while (charts.Count > 0);
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating charts (Position: {Position})", position);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateCharts(mysqlConnection, cancellationToken, position);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating charts (Position: {Position})", position);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateCharts(mysqlConnection, cancellationToken, position);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating charts (Position: {Position})", position);
        }
    }

    private async Task MigrateUsers(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startPosition = 0)
    {
        var position = startPosition;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating users...");
            await using var transaction = await mysqlConnection.BeginTransactionAsync(cancellationToken);
            ICollection<User> users;
            do
            {
                var command = new MySqlCommand { Connection = mysqlConnection, Transaction = transaction };
                var applicationId = Guid.Parse("3241d866-814e-4012-881a-72c55fe5d58e");
                users = await _userRepository.GetUsersAsync(position: position, take: 50,
                    predicate: e => e.ApplicationLinks.Any(f => f.ApplicationId == applicationId));
                command.CommandText = string.Join('\n', from user in users select GetInsertCommand(user, "Users"));
                await command.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                position += 50;
            } while (users.Count > 0);
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating users (Position: {Position})", position);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateUsers(mysqlConnection, cancellationToken, position);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating users (Position: {Position})", position);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateUsers(mysqlConnection, cancellationToken, position);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex,
                "An error occurred whilst migrating users (Position: {Position})", position);
        }
    }

    private static string GetInsertCommand(object entry, string table)
    {
        var type = entry.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Dictionary<string, string?> propertyMap = new();

        foreach (var property in properties)
        {
            if (!IsPrimitiveType(property.PropertyType) || property.PropertyType.IsSubclassOf(typeof(Delegate)) ||
                (property.GetMethod != null && property.GetMethod.IsStatic))
                continue;

            var value = property.GetValue(entry);
            if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
            {
                value = Convert.ToBoolean(value) ? 1 : 0;
            }
            else if (property.PropertyType == typeof(DateTimeOffset) ||
                     property.PropertyType == typeof(DateTimeOffset?))
            {
                value = ((DateTimeOffset?)value)?.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else if ((property.PropertyType == typeof(int) || property.PropertyType == typeof(int?)) &&
                     property.Name.EndsWith("Id") && property.Name != "RegionId" && value != null)
            {
                if (property.Name == "PhiZoneId")
                {
                    var user = (User)entry;
                    value = user.ApplicationLinks.FirstOrDefault(e =>
                            e.ApplicationId == Guid.Parse("3241d866-814e-4012-881a-72c55fe5d58e") &&
                            e.TapUnionId != null)
                        ?.TapUnionId;
                }
                else
                {
                    value = 1;
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
using System.Data;
using System.Net.Sockets;
using Microsoft.AspNetCore.Identity;
using MySqlConnector;
using PhiZoneApi.Constants;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Services;

public class DataMigrationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private ILogger<DataMigrationService> _logger = null!;
    private ApplicationDbContext _context = null!;
    private UserManager<User> _userManager = null!;
    private IFileStorageService _fileStorageService = null!;
    private IConfiguration _configuration = null!;
    private IRegionRepository _regionRepository = null!;
    private IChapterRepository _chapterRepository = null!;
    private string _mediaPath = null!;

    public DataMigrationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        _logger = scope.ServiceProvider.GetRequiredService<ILogger<DataMigrationService>>();
        _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        _fileStorageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        _configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        _regionRepository = scope.ServiceProvider.GetRequiredService<IRegionRepository>();
        _chapterRepository = scope.ServiceProvider.GetRequiredService<IChapterRepository>();
        _mediaPath = _configuration.GetSection("Migration")["MediaPath"]!;

        _logger.LogInformation(LogEvents.DataMigration, "Data migration started");
        try
        {
            await MigrateDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Data migration failed");
        }

        _logger.LogInformation(LogEvents.DataMigration, "Data migration finished");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task MigrateDataAsync()
    {
        await using var mysqlConnection = new MySqlConnection(_configuration.GetConnectionString("MySQLConnection"));
        await mysqlConnection.OpenAsync();
        await MigrateUsers(mysqlConnection);
    }

    private async Task MigrateUsers(MySqlConnection mysqlConnection, int startIndex = 0)
    {
        var index = startIndex;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating users...");
            await using var mysqlCommand =
                new MySqlCommand($"SELECT * FROM phizone_user WHERE id > {index}", mysqlConnection);
            await using var reader = await mysqlCommand.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                index = reader.GetInt32("id");
                var userName = reader.GetString("username");
                _logger.LogInformation(LogEvents.DataMigration, "Migrating #{Id} {UserName}", index, userName);
                var avatarPath = reader.GetString("avatar");
                avatarPath = avatarPath == "user/default.webp" ? null : Path.Combine(_mediaPath, avatarPath);
                string? avatar = null;
                if (avatarPath != null)
                {
                    avatar = (await _fileStorageService.UploadImage<User>(userName, await File.ReadAllBytesAsync(avatarPath), (1, 1)))
                        .Item1;
                }

                var regionCode = await reader.IsDBNullAsync("region") ? null : reader.GetString("region");
                var regionId = regionCode == null ? 47 : (await _regionRepository.GetRegionAsync(regionCode)).Id;

                var user = new User
                {
                    SecurityStamp = Guid.NewGuid().ToString(),
                    UserName = userName,
                    Email = reader.GetString("email"),
                    Avatar = avatar,
                    Language = reader.GetString("language") switch
                    {
                        "en" => "en",
                        "zh-Hans" => "zh-CN",
                        "zh-Hant" => "zh-TW",
                        _ => "zh-CN"
                    },
                    Gender = reader.GetInt32("gender"),
                    Biography = await reader.GetStr("bio"),
                    Experience = reader.GetInt32("exp"),
                    Tag = await reader.GetStr("tag"),
                    Rks = reader.GetDouble("rks"),
                    RegionId = regionId,
                    DateOfBirth = null,
                    DateJoined = reader.GetDateTimeOffset("date_joined"),
                    DateLastLoggedIn = await reader.GetTime("last_login"),
                    DateLastModifiedUserName = await reader.GetTime("username_last_modified"),
                };
                user.PasswordHash = reader.GetString("password");
                await _userManager.CreateAsync(user);
                user.LockoutEnabled = false;
                await _userManager.UpdateAsync(user);
            }
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after #{Index}", index);
            await MigrateUsers(mysqlConnection, index);
        }
    }
}
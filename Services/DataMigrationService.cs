using System.Data;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable StringLiteralTypo

namespace PhiZoneApi.Services;

public partial class DataMigrationService : IHostedService
{
    private readonly Dictionary<int, Guid> _applicationDictionary = new();
    private readonly Dictionary<int, Guid> _chapterDictionary = new();
    private readonly Dictionary<int, Guid> _chartDictionary = new();
    private readonly Dictionary<int, Guid> _chartSubmissionDictionary = new();
    private readonly Dictionary<int, Guid> _commentDictionary = new();
    private readonly Dictionary<int, Guid> _replyDictionary = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<int, Guid> _songDictionary = new();
    private readonly Dictionary<int, Guid> _songSubmissionDictionary = new();
    private readonly Dictionary<int, int> _userDictionary = new();
    private IAdmissionRepository _admissionRepository = null!;
    private IAuthorshipRepository _authorshipRepository = null!;
    private IChapterRepository _chapterRepository = null!;
    private IChartRepository _chartRepository = null!;
    private IChartService _chartService = null!;
    private IChartSubmissionRepository _chartSubmissionRepository = null!;
    private ICollaborationRepository _collaborationRepository = null!;
    private ICommentRepository _commentRepository = null!;
    private IConfiguration _configuration = null!;
    private IPlayConfigurationRepository _configurationRepository = null!;
    private IFileStorageService _fileStorageService = null!;
    private ILikeService _likeService = null!;
    private ILogger<DataMigrationService> _logger = null!;
    private string _mediaPath = null!;
    private IRecordRepository _recordRepository = null!;
    private IRecordService _recordService = null!;
    private IRegionRepository _regionRepository = null!;
    private IReplyRepository _replyRepository = null!;
    private ISongRepository _songRepository = null!;
    private ISongService _songService = null!;
    private ISongSubmissionRepository _songSubmissionRepository = null!;
    private ITemplateService _templateService = null!;
    private UserManager<User> _userManager = null!;
    private IUserRelationRepository _userRelationRepository = null!;
    private IVolunteerVoteRepository _volunteerVoteRepository = null!;
    private IVoteRepository _voteRepository = null!;
    private IVoteService _voteService = null!;

    public DataMigrationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        _logger = scope.ServiceProvider.GetRequiredService<ILogger<DataMigrationService>>();
        _userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        _fileStorageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        _songService = scope.ServiceProvider.GetRequiredService<ISongService>();
        _chartService = scope.ServiceProvider.GetRequiredService<IChartService>();
        scope.ServiceProvider.GetRequiredService<IResourceService>();
        _templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();
        _recordService = scope.ServiceProvider.GetRequiredService<IRecordService>();
        _voteService = scope.ServiceProvider.GetRequiredService<IVoteService>();
        _likeService = scope.ServiceProvider.GetRequiredService<ILikeService>();
        _configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        _regionRepository = scope.ServiceProvider.GetRequiredService<IRegionRepository>();
        _userRelationRepository = scope.ServiceProvider.GetRequiredService<IUserRelationRepository>();
        _chapterRepository = scope.ServiceProvider.GetRequiredService<IChapterRepository>();
        _songRepository = scope.ServiceProvider.GetRequiredService<ISongRepository>();
        _chartRepository = scope.ServiceProvider.GetRequiredService<IChartRepository>();
        _songSubmissionRepository = scope.ServiceProvider.GetRequiredService<ISongSubmissionRepository>();
        _chartSubmissionRepository = scope.ServiceProvider.GetRequiredService<IChartSubmissionRepository>();
        _admissionRepository = scope.ServiceProvider.GetRequiredService<IAdmissionRepository>();
        _authorshipRepository = scope.ServiceProvider.GetRequiredService<IAuthorshipRepository>();
        _collaborationRepository = scope.ServiceProvider.GetRequiredService<ICollaborationRepository>();
        _recordRepository = scope.ServiceProvider.GetRequiredService<IRecordRepository>();
        _configurationRepository = scope.ServiceProvider.GetRequiredService<IPlayConfigurationRepository>();
        _voteRepository = scope.ServiceProvider.GetRequiredService<IVoteRepository>();
        _volunteerVoteRepository = scope.ServiceProvider.GetRequiredService<IVolunteerVoteRepository>();
        _commentRepository = scope.ServiceProvider.GetRequiredService<ICommentRepository>();
        _replyRepository = scope.ServiceProvider.GetRequiredService<IReplyRepository>();
        _mediaPath = _configuration.GetSection("Migration")["MediaPath"]!;

        foreach (var child in _configuration.GetSection("Migration").GetSection("ApplicationDictionary").GetChildren())
            _applicationDictionary[int.Parse(child.Key)] = Guid.Parse(child.Value!);

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
        await using var mysqlConnection = new MySqlConnection(_configuration.GetConnectionString("MySQLConnection"));
        if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
        await MigrateUsers(mysqlConnection, cancellationToken);
        await MigrateUserRelations(mysqlConnection, cancellationToken);
        await MigrateChapters(mysqlConnection, cancellationToken);
        await MigrateSongs(mysqlConnection, cancellationToken);
        await MigrateSongAdmissions(mysqlConnection, cancellationToken);
        await MigrateCharts(mysqlConnection, cancellationToken);
        await MigratePlayConfigurations(mysqlConnection, cancellationToken);
        await MigrateRecords(mysqlConnection, cancellationToken);
        await MigrateComments(mysqlConnection, cancellationToken);
        await MigrateReplies(mysqlConnection, cancellationToken);
        await MigrateVotes(mysqlConnection, cancellationToken);
        await MigrateLikes(mysqlConnection, cancellationToken);
        await MigrateSongSubmissions(mysqlConnection, cancellationToken);
        await MigrateChartSubmissions(mysqlConnection, cancellationToken);
        await MigrateVolunteerVotes(mysqlConnection, cancellationToken);
        await MigrateCollaborations(mysqlConnection, cancellationToken);
    }

    private async Task MigrateUsers(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startIndex = 0)
    {
        var index = startIndex;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating users...");
            await using var mysqlCommand =
                new MySqlCommand($"SELECT * FROM phizone_user WHERE id > {index}", mysqlConnection);
            await using var reader = await mysqlCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                index = reader.GetInt32("id");
                var userName = reader.GetString("username");
                var user = await _userManager.FindByNameAsync(userName);
                if (user != null)
                {
                    _userDictionary.Add(index, user.Id);
                    continue;
                }

                _logger.LogInformation(LogEvents.DataMigration, "Migrating User #{Id} {UserName}", index, userName);
                // var avatarPath = reader.GetString("avatar");
                // avatarPath = avatarPath == "user/default.webp" ? null : Path.Combine(_mediaPath, avatarPath);
                string? avatar = null;
                // if (avatarPath != null)
                //     avatar = (await _fileStorageService.UploadImage<User>(userName,
                //         await File.ReadAllBytesAsync(avatarPath, cancellationToken), (1, 1))).Item1;

                var regionCode = await reader.IsDBNullAsync("region", cancellationToken)
                    ? null
                    : reader.GetString("region");
                var regionId = regionCode == null ? 47 : (await _regionRepository.GetRegionAsync(regionCode)).Id;

                user = new User
                {
                    SecurityStamp = Guid.NewGuid().ToString(),
                    UserName = userName,
                    PasswordHash = reader.GetString("password"),
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
                    DateLastModifiedUserName = await reader.GetTime("username_last_modified")
                };
                await _userManager.CreateAsync(user);

                user.EmailConfirmed = true;
                user.LockoutEnabled = false;
                await _userManager.UpdateAsync(user);
                await _userManager.AddToRoleAsync(user, Roles.Member.Name);
                _userDictionary.Add(index, user.Id);

                var configuration = new PlayConfiguration
                {
                    Name = _templateService.GetMessage("default", user.Language),
                    PerfectJudgment = 80,
                    GoodJudgment = 160,
                    AspectRatio = null,
                    NoteSize = 1,
                    ChartMirroring = ChartMirroringMode.Off,
                    BackgroundLuminance = 0.5,
                    BackgroundBlur = 1,
                    SimultaneousNoteHint = true,
                    FcApIndicator = true,
                    ChartOffset = 0,
                    HitSoundVolume = 0,
                    MusicVolume = 0,
                    OwnerId = user.Id,
                    DateCreated = DateTimeOffset.UtcNow
                };
                await _configurationRepository.CreatePlayConfigurationAsync(configuration);
            }
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after User #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateUsers(mysqlConnection, cancellationToken, index);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after User #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateUsers(mysqlConnection, cancellationToken, index);
        }
    }

    private async Task MigrateUserRelations(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startIndex = 0)
    {
        var index = startIndex;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating user relations...");
            await using var mysqlCommand =
                new MySqlCommand($"SELECT * FROM phizone_relation WHERE id > {index}", mysqlConnection);
            await using var reader = await mysqlCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                index = reader.GetInt32("id");
                var followeeId = _userDictionary[reader.GetInt32("followee_id")];
                var followerId = _userDictionary[reader.GetInt32("follower_id")];
                if (await _userRelationRepository.RelationExistsAsync(followerId, followeeId)) continue;
                _logger.LogInformation(LogEvents.DataMigration, "Migrating User Relation #{Id}", index);

                var userRelation = new UserRelation
                {
                    FolloweeId = followeeId,
                    FollowerId = followerId,
                    Type = UserRelationType.Following,
                    DateCreated = reader.GetDateTimeOffset("time")
                };
                await _userRelationRepository.CreateRelationAsync(userRelation);
            }
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after User Relation #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateUserRelations(mysqlConnection, cancellationToken, index);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after User Relation #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateUserRelations(mysqlConnection, cancellationToken, index);
        }
    }

    private async Task MigrateChapters(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startIndex = 0)
    {
        var index = startIndex;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating chapters...");
            await using var mysqlCommand =
                new MySqlCommand($"SELECT * FROM phizone_chapter WHERE id > {index}", mysqlConnection);
            await using var reader = await mysqlCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                index = reader.GetInt32("id");
                var title = reader.GetString("title");
                var subtitle = reader.GetString("subtitle");
                if (await _chapterRepository.CountChaptersAsync(predicate: e =>
                        e.Title == title && e.Subtitle == subtitle) > 0)
                {
                    _chapterDictionary.Add(index,
                        (await _chapterRepository.GetChaptersAsync("DateCreated", false, 0, -1, null,
                            e => e.Title == title && e.Subtitle == subtitle)).FirstOrDefault()!.Id);
                    continue;
                }

                _logger.LogInformation(LogEvents.DataMigration, "Migrating Chapter #{Id} {Title} - {Subtitle}", index,
                    title, subtitle);
                var illustrationPath = Path.Combine(_mediaPath, reader.GetString("illustration"));
                var illustration = (await _fileStorageService.UploadImage<Chapter>(title,
                    await File.ReadAllBytesAsync(illustrationPath, cancellationToken), (16, 9))).Item1;
                var date = reader.GetDateTimeOffset("time");

                var chapter = new Chapter
                {
                    Title = title,
                    Subtitle = subtitle,
                    Illustration = illustration,
                    Illustrator = reader.GetString("illustrator"),
                    Description = await reader.GetStr("description"),
                    Accessibility = (Accessibility)reader.GetInt32("accessibility"),
                    IsHidden = false,
                    IsLocked = false,
                    OwnerId = _userDictionary[reader.GetInt32("owner_id")],
                    DateCreated = date,
                    DateUpdated = date,
                    LikeCount = reader.GetInt32("like_count")
                };
                await _chapterRepository.CreateChapterAsync(chapter);
                _chapterDictionary.Add(index, chapter.Id);
            }
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Chapter #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateChapters(mysqlConnection, cancellationToken, index);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Chapter #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateChapters(mysqlConnection, cancellationToken, index);
        }
    }

    private async Task MigrateSongs(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startIndex = 0)
    {
        var index = startIndex;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating songs...");
            await using var mysqlCommand =
                new MySqlCommand($"SELECT * FROM phizone_song WHERE id > {index}", mysqlConnection);
            await using var reader = await mysqlCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                index = reader.GetInt32("id");
                var title = reader.GetString("name");
                var edition = reader.GetString("edition");
                var authorName = reader.GetString("composer");
                if (await _songRepository.CountSongsAsync(
                        predicate: e => e.Title == title && e.AuthorName == authorName) > 0)
                {
                    _songDictionary.Add(index,
                        (await _songRepository.GetSongsAsync("DateCreated", false, 0, -1, null,
                            e => e.Title == title && e.AuthorName == authorName)).FirstOrDefault()!.Id);
                    continue;
                }

                _logger.LogInformation(LogEvents.DataMigration, "Migrating Song #{Id} {Title}", index, title);
                var filePath = Path.Combine(_mediaPath, reader.GetString("song"));
                var fileInfo =
                    await _songService.UploadAsync(title, await File.ReadAllBytesAsync(filePath, cancellationToken));
                var illustrationPath = Path.Combine(_mediaPath, reader.GetString("illustration"));
                var illustration = (await _fileStorageService.UploadImage<User>(title,
                    await File.ReadAllBytesAsync(illustrationPath, cancellationToken), (16, 9))).Item1;
                var date = reader.GetDateTimeOffset("time");
                var bpm = NonDigitRegex()
                    .Split(reader.GetString("bpm"))
                    .Where(s => !s.IsNullOrEmpty())
                    .Select(double.Parse)
                    .ToArray();

                var song = new Song
                {
                    Title = title,
                    EditionType =
                        edition.Contains("原版") || edition.ToUpper().Contains("ORIGINAL") ? EditionType.Original :
                        edition.Contains('剪') || edition.ToUpper().Contains("SELF") ||
                        edition.ToUpper().Contains("EDITED") ? EditionType.EditedByUploaderUnlicensed :
                        EditionType.EditedByFirstParty,
                    Edition =
                        edition.Contains("原版") || edition.ToUpper().Contains("ORIGINAL") || edition.Contains('剪') ||
                        edition.ToUpper().Contains("SELF") || edition.ToUpper().Contains("EDITED")
                            ? null
                            : edition,
                    AuthorName = authorName,
                    File = fileInfo!.Value.Item1,
                    FileChecksum = fileInfo.Value.Item2,
                    Illustration = illustration,
                    Illustrator = reader.GetString("illustrator"),
                    Lyrics = await reader.GetStr("lyrics"),
                    Description = await reader.GetStr("description"),
                    Accessibility = (Accessibility)reader.GetInt32("accessibility"),
                    Bpm = bpm[^1],
                    MinBpm = bpm[0],
                    MaxBpm = bpm.Length > 1 ? bpm[1] : bpm[0],
                    Offset = reader.GetInt32("offset"),
                    IsOriginal = reader.GetBoolean("original"),
                    Duration = fileInfo.Value.Item3,
                    PreviewStart = reader.GetTimeSpan("preview_start"),
                    PreviewEnd = reader.GetTimeSpan("preview_end"),
                    IsHidden = reader.GetBoolean("hidden"),
                    IsLocked = false,
                    OwnerId = _userDictionary[reader.GetInt32("uploader_id")],
                    DateCreated = date,
                    DateUpdated = date,
                    LikeCount = reader.GetInt32("like_count")
                };
                await _songRepository.CreateSongAsync(song);
                if (song.IsOriginal)
                {
                    var authorship = new Authorship
                    {
                        ResourceId = song.Id, AuthorId = song.OwnerId, DateCreated = DateTimeOffset.UtcNow
                    };
                    await _authorshipRepository.CreateAuthorshipAsync(authorship);
                }

                _songDictionary.Add(index, song.Id);
            }
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Song #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateSongs(mysqlConnection, cancellationToken, index);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Song #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateSongs(mysqlConnection, cancellationToken, index);
        }
    }

    private async Task MigrateSongAdmissions(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startIndex = 0)
    {
        var index = startIndex;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating song admissions...");
            await using var mysqlCommand =
                new MySqlCommand($"SELECT * FROM phizone_song_chapters WHERE id > {index} ORDER BY chapter_id, song_id",
                    mysqlConnection);
            await using var reader = await mysqlCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                index = reader.GetInt32("id");
                var chapterId = _chapterDictionary[reader.GetInt32("chapter_id")];
                var songId = _songDictionary[reader.GetInt32("song_id")];
                if (await _admissionRepository.AdmissionExistsAsync(chapterId, songId))
                {
                    continue;
                }

                _logger.LogInformation(LogEvents.DataMigration, "Migrating Song Admission #{Id}", index);

                var admission = new Admission
                {
                    AdmitterId = chapterId,
                    AdmitteeId = songId,
                    Status = RequestStatus.Approved,
                    RequesterId = (await _songRepository.GetSongAsync(songId)).OwnerId,
                    RequesteeId = (await _chapterRepository.GetChapterAsync(chapterId)).OwnerId,
                    DateCreated = DateTimeOffset.UtcNow
                };
                await _admissionRepository.CreateAdmissionAsync(admission);
            }
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Song Admission #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateSongAdmissions(mysqlConnection, cancellationToken, index);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Song Admission #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateSongAdmissions(mysqlConnection, cancellationToken, index);
        }
    }

    private async Task MigrateCharts(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startIndex = 0)
    {
        var index = startIndex;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating charts...");
            await using var mysqlCommand =
                new MySqlCommand($"SELECT * FROM phizone_chart WHERE id > {index}", mysqlConnection);
            await using var reader = await mysqlCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                index = reader.GetInt32("id");
                var song = await _songRepository.GetSongAsync(_songDictionary[reader.GetInt32("song_id")]);
                var levelType = (ChartLevel)reader.GetInt32("level_type");
                var level = reader.GetString("level");
                var difficulty = reader.GetDouble("difficulty");
                if (await _chartRepository.CountChartsAsync(predicate: e =>
                        e.SongId == song.Id && e.LevelType == levelType && e.Level == level &&
                        e.OwnerId == _userDictionary[reader.GetInt32("owner_id")]) > 0)
                {
                    _chartDictionary.Add(index,
                        (await _chartRepository.GetChartsAsync("DateCreated", false, 0, -1, null,
                            e =>
                                e.SongId == song.Id && e.LevelType == levelType && e.Level == level &&
                                e.OwnerId == _userDictionary[reader.GetInt32("owner_id")])).FirstOrDefault()!.Id);
                    continue;
                }
                
                _logger.LogInformation(LogEvents.DataMigration, "Migrating Chart #{Id} {Title} {Level} Lv.{Difficulty}", index, song.Title, level, Math.Floor(difficulty));

                var chartFile = reader.GetString("chart");
                (string, string, ChartFormat, int)? chartInfo = null;
                if (!chartFile.IsNullOrEmpty())
                {
                    var filePath = Path.Combine(_mediaPath, chartFile);
                    chartInfo = await _chartService.Upload(song.Title, filePath);
                }

                var authorName = string.Join("",
                    SplitAuthorName(reader.GetString("charter"))
                        .Select(s =>
                            s.StartsWith("[PZUser:")
                                ? $"[PZUser:{_userDictionary[int.Parse(s.Split(':')[1])]}:{s.Split(':')[2]}:PZRT]"
                                : s)
                        .ToArray());
                var date = reader.GetDateTimeOffset("time");

                var chart = new Chart
                {
                    LevelType = levelType,
                    Level = level,
                    Difficulty = difficulty,
                    AuthorName = authorName,
                    File = chartInfo?.Item1,
                    FileChecksum = chartInfo?.Item2 ?? string.Empty,
                    Format = chartInfo?.Item3 ?? ChartFormat.Phigrim,
                    NoteCount = chartInfo?.Item4 ?? reader.GetInt32("notes"),
                    Description = await reader.GetStr("description"),
                    IsRanked = reader.GetBoolean("ranked"),
                    IsHidden = reader.GetBoolean("hidden"),
                    IsLocked = false,
                    SongId = song.Id,
                    OwnerId = _userDictionary[reader.GetInt32("owner_id")],
                    DateCreated = date,
                    DateUpdated = date,
                    LikeCount = reader.GetInt32("like_count")
                };
                await _chartRepository.CreateChartAsync(chart);
                _chartDictionary.Add(index, chart.Id);
            }
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Chart #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateCharts(mysqlConnection, cancellationToken, index);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Chart #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateCharts(mysqlConnection, cancellationToken, index);
        }
    }

    private async Task MigratePlayConfigurations(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startIndex = 0)
    {
        var index = startIndex;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating play configurations...");
            await using var mysqlCommand =
                new MySqlCommand(
                    $"SELECT * FROM player_configuration WHERE id > {index} AND perfect_judgment != 80 AND good_judgment != 160",
                    mysqlConnection);
            await using var reader = await mysqlCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                index = reader.GetInt32("id");
                _logger.LogInformation(LogEvents.DataMigration, "Migrating Play Configuration #{Id}", index);

                var playConfiguration = new PlayConfiguration
                {
                    PerfectJudgment = reader.GetInt32("perfect_judgment"),
                    GoodJudgment = reader.GetInt32("good_judgment"),
                    AspectRatio = reader.GetString("aspect_ratio").Split(":").Select(int.Parse).ToList(),
                    NoteSize = reader.GetDouble("note_size"),
                    ChartMirroring = (ChartMirroringMode)reader.GetInt32("chart_mirroring"),
                    BackgroundLuminance = reader.GetDouble("background_luminance"),
                    BackgroundBlur = reader.GetDouble("background_blur"),
                    SimultaneousNoteHint = reader.GetBoolean("simul_note_highlight"),
                    FcApIndicator = reader.GetBoolean("fc_ap_indicator"),
                    ChartOffset = reader.GetInt32("chart_offset"),
                    HitSoundVolume = reader.GetDouble("hitsound_volume"),
                    MusicVolume = reader.GetDouble("music_volume"),
                    OwnerId = _userDictionary[reader.GetInt32("user_id")],
                    DateCreated = DateTimeOffset.UtcNow
                };
                await _configurationRepository.CreatePlayConfigurationAsync(playConfiguration);
            }
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Play Configuration #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigratePlayConfigurations(mysqlConnection, cancellationToken, index);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Play Configuration #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigratePlayConfigurations(mysqlConnection, cancellationToken, index);
        }
    }

    private async Task MigrateRecords(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startIndex = 0)
    {
        var index = startIndex;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating records...");
            await using var mysqlCommand =
                new MySqlCommand($"SELECT * FROM phizone_record WHERE id > {index}", mysqlConnection);
            await using var reader = await mysqlCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                index = reader.GetInt32("id");
                var chart = await _chartRepository.GetChartAsync(_chartDictionary[reader.GetInt32("chart_id")]);
                var maxCombo = reader.GetInt32("max_combo");
                var perfect = reader.GetInt32("perfect");
                var goodEarly = reader.GetInt32("good_early");
                var goodLate = reader.GetInt32("good_late");
                var bad = reader.GetInt32("bad");
                var miss = reader.GetInt32("miss");
                var perfectJudgment = reader.GetInt32("perfect_judgment");
                var goodJudgment = reader.GetInt32("perfect_judgment");
                var score = _recordService.CalculateScore(perfect, goodEarly + goodLate, bad, miss, maxCombo);
                var accuracy = _recordService.CalculateAccuracy(perfect, goodEarly + goodLate, bad, miss);
                var rksFactor = _recordService.CalculateRksFactor(perfectJudgment, goodJudgment);
                var rks = _recordService.CalculateRks(perfect, goodEarly + goodLate, bad, miss, chart.Difficulty, 40) *
                          rksFactor;
                _logger.LogInformation(LogEvents.DataMigration, "Migrating Record #{Id} {Score} {Accuracy}", index,
                    score, accuracy.ToString("P"));
                var appId = await reader.GetInt("app_id") ?? 1;

                var record = new Record
                {
                    ChartId = chart.Id,
                    Score = score,
                    Accuracy = accuracy,
                    IsFullCombo = maxCombo == perfect + goodEarly + goodLate + bad + miss,
                    MaxCombo = maxCombo,
                    Perfect = perfect,
                    GoodEarly = goodEarly,
                    GoodLate = goodLate,
                    Bad = bad,
                    Miss = miss,
                    StdDeviation = 40,
                    Rks = rks,
                    PerfectJudgment = perfectJudgment,
                    GoodJudgment = goodJudgment,
                    ApplicationId = _applicationDictionary[appId],
                    OwnerId = _userDictionary[reader.GetInt32("player_id")],
                    DateCreated = reader.GetDateTimeOffset("time")
                };
                await _recordRepository.CreateRecordAsync(record);
            }
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Record #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateRecords(mysqlConnection, cancellationToken, index);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Record #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateRecords(mysqlConnection, cancellationToken, index);
        }
    }

    private async Task MigrateComments(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startIndex = 0)
    {
        var index = startIndex;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating comments...");
            await using var mysqlCommand =
                new MySqlCommand($"SELECT * FROM phizone_comment WHERE id > {index}", mysqlConnection);
            await using var reader = await mysqlCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                index = reader.GetInt32("id");
                Guid? resourceId = null;
                var id = await reader.GetInt("chapter_id");
                if (id != null)
                {
                    resourceId = _chapterDictionary[id.Value];
                }
                else
                {
                    id = await reader.GetInt("song_id");
                    if (id != null)
                    {
                        resourceId = _songDictionary[id.Value];
                    }
                    else
                    {
                        id = await reader.GetInt("chart_id");
                        if (id != null) resourceId = _chartDictionary[id.Value];
                    }
                }

                if (resourceId == null || !await reader.IsDBNullAsync("deletion", cancellationToken)) continue;

                _logger.LogInformation(LogEvents.DataMigration, "Migrating Comment #{Id}", index);

                var comment = new Comment
                {
                    ResourceId = resourceId.Value,
                    Content = reader.GetString("content"),
                    Language = reader.GetString("language"),
                    OwnerId = _userDictionary[reader.GetInt32("user_id")],
                    DateCreated = reader.GetDateTimeOffset("creation")
                };

                await _commentRepository.CreateCommentAsync(comment);
                _commentDictionary.Add(index, comment.Id);
            }
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Comment #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateComments(mysqlConnection, cancellationToken, index);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Comment #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateComments(mysqlConnection, cancellationToken, index);
        }
    }

    private async Task MigrateReplies(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startIndex = 0)
    {
        var index = startIndex;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating replies...");
            await using var mysqlCommand =
                new MySqlCommand($"SELECT * FROM phizone_reply WHERE id > {index}", mysqlConnection);
            await using var reader = await mysqlCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                index = reader.GetInt32("id");
                if (!await reader.IsDBNullAsync("deletion", cancellationToken)) continue;

                _logger.LogInformation(LogEvents.DataMigration, "Migrating Reply #{Id}", index);

                var reply = new Reply
                {
                    CommentId = _commentDictionary[reader.GetInt32("comment_id")],
                    Content = reader.GetString("content"),
                    Language = reader.GetString("language"),
                    OwnerId = _userDictionary[reader.GetInt32("user_id")],
                    DateCreated = reader.GetDateTimeOffset("creation")
                };

                await _replyRepository.CreateReplyAsync(reply);
                _replyDictionary.Add(index, reply.Id);
            }
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Reply #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateReplies(mysqlConnection, cancellationToken, index);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Reply #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateReplies(mysqlConnection, cancellationToken, index);
        }
    }

    private async Task MigrateVotes(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startIndex = 0)
    {
        var index = startIndex;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating votes...");
            await using var mysqlCommand =
                new MySqlCommand($"SELECT * FROM phizone_vote WHERE id > {index} AND total > 0 ORDER BY chart_id",
                    mysqlConnection);
            await using var reader = await mysqlCommand.ExecuteReaderAsync(cancellationToken);

            Guid? lastChartId = null;
            while (await reader.ReadAsync(cancellationToken))
            {
                index = reader.GetInt32("id");
                var chartId = _chartDictionary[reader.GetInt32("chart_id")];
                if (lastChartId != null)
                {
                    if (chartId != lastChartId)
                    {
                        await _voteService.UpdateChartAsync(await _chartRepository.GetChartAsync(lastChartId.Value));
                        lastChartId = chartId;
                    }
                }
                else
                {
                    lastChartId = chartId;
                }

                _logger.LogInformation(LogEvents.DataMigration, "Migrating Vote #{Id}", index);

                var vote = new Vote
                {
                    ChartId = chartId,
                    Arrangement = reader.GetInt32("arrangement"),
                    Feel = reader.GetInt32("feel"),
                    VisualEffects = reader.GetInt32("visual_effects"),
                    Creativity = reader.GetInt32("innovativeness"),
                    Concord = reader.GetInt32("concord"),
                    Impression = reader.GetInt32("impression"),
                    Total = reader.GetInt32("total"),
                    Multiplier = reader.GetDouble("multiplier"),
                    OwnerId = _userDictionary[reader.GetInt32("user_id")],
                    DateCreated = reader.GetDateTimeOffset("time")
                };
                await _voteRepository.CreateVoteAsync(vote);
            }
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Vote #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateVotes(mysqlConnection, cancellationToken, index);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Vote #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateVotes(mysqlConnection, cancellationToken, index);
        }
    }

    private async Task MigrateLikes(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startIndex = 0)
    {
        var index = startIndex;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating likes...");
            await using var mysqlCommand = new MySqlCommand($"SELECT * FROM phizone_like WHERE id > {index}",
                mysqlConnection);
            await using var reader = await mysqlCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                index = reader.GetInt32("id");
                _logger.LogInformation(LogEvents.DataMigration, "Migrating Like #{Id}", index);
                var id = await reader.GetInt("chapter_id");
                if (id != null)
                {
                    var resource = await _chapterRepository.GetChapterAsync(_chapterDictionary[id.Value]);
                    await _likeService.CreateLikeAsync(resource, _userDictionary[reader.GetInt32("user_id")],
                        reader.GetDateTimeOffset("time"));
                    continue;
                }

                id = await reader.GetInt("song_id");
                if (id != null)
                {
                    var resource = await _songRepository.GetSongAsync(_songDictionary[id.Value]);
                    await _likeService.CreateLikeAsync(resource, _userDictionary[reader.GetInt32("user_id")],
                        reader.GetDateTimeOffset("time"));
                    continue;
                }

                id = await reader.GetInt("chart_id");
                if (id != null)
                {
                    var resource = await _chartRepository.GetChartAsync(_chartDictionary[id.Value]);
                    await _likeService.CreateLikeAsync(resource, _userDictionary[reader.GetInt32("user_id")],
                        reader.GetDateTimeOffset("time"));
                    continue;
                }

                id = await reader.GetInt("comment_id");
                if (id != null)
                {
                    var resource = await _commentRepository.GetCommentAsync(_commentDictionary[id.Value]);
                    await _likeService.CreateLikeAsync(resource, _userDictionary[reader.GetInt32("user_id")],
                        reader.GetDateTimeOffset("time"));
                    continue;
                }

                id = await reader.GetInt("reply_id");
                // ReSharper disable once InvertIf
                if (id != null)
                {
                    var resource = await _replyRepository.GetReplyAsync(_replyDictionary[id.Value]);
                    await _likeService.CreateLikeAsync(resource, _userDictionary[reader.GetInt32("user_id")],
                        reader.GetDateTimeOffset("time"));
                }
            }
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Like #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateLikes(mysqlConnection, cancellationToken, index);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Like #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateLikes(mysqlConnection, cancellationToken, index);
        }
    }

    private async Task MigrateSongSubmissions(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startIndex = 0)
    {
        var index = startIndex;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating song submissions...");
            await using var mysqlCommand =
                new MySqlCommand($"SELECT * FROM phizone_songupload WHERE id > {index} AND status != 2",
                    mysqlConnection);
            await using var reader = await mysqlCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                index = reader.GetInt32("id");
                var title = reader.GetString("name");

                _logger.LogInformation(LogEvents.DataMigration, "Migrating Song Submission #{Id} {Title}", index,
                    title);
                var filePath = Path.Combine(_mediaPath, reader.GetString("song"));
                var fileInfo =
                    await _songService.UploadAsync(title, await File.ReadAllBytesAsync(filePath, cancellationToken));
                var illustrationPath = Path.Combine(_mediaPath, reader.GetString("illustration"));
                var illustration = (await _fileStorageService.UploadImage<User>(title,
                    await File.ReadAllBytesAsync(illustrationPath, cancellationToken), (16, 9))).Item1;
                var date = reader.GetDateTimeOffset("time");
                var edition = reader.GetString("edition");
                var bpm = NonDigitRegex()
                    .Split(reader.GetString("bpm"))
                    .Where(s => !s.IsNullOrEmpty())
                    .Select(double.Parse)
                    .ToArray();

                var songSubmission = new SongSubmission
                {
                    Title = title,
                    EditionType =
                        edition.Contains("原版") || edition.ToUpper().Contains("ORIGINAL") ? EditionType.Original :
                        edition.Contains('剪') || edition.ToUpper().Contains("SELF") ||
                        edition.ToUpper().Contains("EDITED") ? EditionType.EditedByUploaderUnlicensed :
                        EditionType.EditedByFirstParty,
                    Edition =
                        edition.Contains("原版") || edition.ToUpper().Contains("ORIGINAL") || edition.Contains('剪') ||
                        edition.ToUpper().Contains("SELF") || edition.ToUpper().Contains("EDITED")
                            ? null
                            : edition,
                    AuthorName = reader.GetString("composer"),
                    File = fileInfo!.Value.Item1,
                    FileChecksum = fileInfo.Value.Item2,
                    Illustration = illustration,
                    Illustrator = reader.GetString("illustrator"),
                    Lyrics = await reader.GetStr("lyrics"),
                    Description = await reader.GetStr("description"),
                    Accessibility = (Accessibility)reader.GetInt32("accessibility"),
                    Bpm = bpm[^1],
                    MinBpm = bpm[0],
                    MaxBpm = bpm.Length > 1 ? bpm[1] : bpm[0],
                    Offset = reader.GetInt32("offset"),
                    OriginalityProof = null,
                    Duration = fileInfo.Value.Item3,
                    PreviewStart = reader.GetTimeSpan("preview_start"),
                    PreviewEnd = reader.GetTimeSpan("preview_end"),
                    OwnerId = _userDictionary[reader.GetInt32("uploader_id")],
                    Status = (RequestStatus)reader.GetInt32("status"),
                    RepresentationId =
                        await reader.IsDBNullAsync("representation_id", cancellationToken)
                            ? null
                            : _songDictionary[reader.GetInt32("representation_id")],
                    ReviewerId =
                        await reader.IsDBNullAsync("uploader_id", cancellationToken)
                            ? null
                            : _userDictionary[reader.GetInt32("uploader_id")],
                    Message = await reader.GetStr("message"),
                    DateCreated = date,
                    DateUpdated = date
                };
                await _songSubmissionRepository.CreateSongSubmissionAsync(songSubmission);

                _songSubmissionDictionary.Add(index, songSubmission.Id);
            }
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Song Submission #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateSongSubmissions(mysqlConnection, cancellationToken, index);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Song Submission #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateSongSubmissions(mysqlConnection, cancellationToken, index);
        }
    }

    private async Task MigrateChartSubmissions(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startIndex = 0)
    {
        var index = startIndex;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating chart submissions...");
            await using var mysqlCommand =
                new MySqlCommand($"SELECT * FROM phizone_chartupload WHERE id > {index} AND status != 2",
                    mysqlConnection);
            await using var reader = await mysqlCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                index = reader.GetInt32("id");
                var song = await reader.IsDBNullAsync("song_id", cancellationToken)
                    ? null
                    : await _songRepository.GetSongAsync(_songDictionary[reader.GetInt32("song_id")]);
                var songSubmission = await reader.IsDBNullAsync("song_upload_id", cancellationToken)
                    ? null
                    : await _songSubmissionRepository.GetSongSubmissionAsync(
                        _songSubmissionDictionary[reader.GetInt32("song_upload_id")]);
                var title = song != null ? song.Title : songSubmission!.Title;
                _logger.LogInformation(LogEvents.DataMigration, "Migrating Chart Submission #{Id} {Title}", index,
                    title);

                var filePath = Path.Combine(_mediaPath, reader.GetString("chartupload"));
                var chartSubmissionInfo = await _chartService.Upload(title, filePath);
                
                var authorName = string.Join("",
                    SplitAuthorName(reader.GetString("charter"))
                        .Select(s =>
                            s.StartsWith("[PZUser:")
                                ? $"[PZUser:{_userDictionary[int.Parse(s.Split(':')[1])]}:{s.Split(':')[2]}:PZRT]"
                                : s)
                        .ToArray());
                var date = reader.GetDateTimeOffset("time");
                Guid? representationId = await reader.IsDBNullAsync("representation_id", cancellationToken)
                    ? null
                    : _chartDictionary[reader.GetInt32("representation_id")];

                var chartSubmission = new ChartSubmission
                {
                    LevelType = (ChartLevel)reader.GetInt32("level_type"),
                    Level = reader.GetString("level"),
                    Difficulty = reader.GetDouble("difficulty"),
                    AuthorName = authorName,
                    File = chartSubmissionInfo!.Value.Item1,
                    FileChecksum = chartSubmissionInfo.Value.Item2,
                    Format = chartSubmissionInfo.Value.Item3,
                    NoteCount = chartSubmissionInfo.Value.Item4,
                    Description = await reader.GetStr("description"),
                    IsRanked =
                        representationId != null &&
                        (await _chartRepository.GetChartAsync(representationId.Value)).IsRanked,
                    RepresentationId = representationId,
                    SongId = song?.Id,
                    SongSubmissionId = songSubmission?.Id,
                    Status = (RequestStatus)reader.GetInt32("status"),
                    VolunteerStatus = (RequestStatus)reader.GetInt32("volunteer_status"),
                    AdmissionStatus = (RequestStatus)reader.GetInt32("adm_status"),
                    OwnerId = _userDictionary[reader.GetInt32("uploader_id")],
                    DateCreated = date,
                    DateUpdated = date
                };
                await _chartSubmissionRepository.CreateChartSubmissionAsync(chartSubmission);
                _chartSubmissionDictionary.Add(index, chartSubmission.Id);
            }
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Chart Submission #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateChartSubmissions(mysqlConnection, cancellationToken, index);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Chart Submission #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateChartSubmissions(mysqlConnection, cancellationToken, index);
        }
    }

    private async Task MigrateVolunteerVotes(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startIndex = 0)
    {
        var index = startIndex;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating volunteer votes...");
            await using var mysqlCommand = new MySqlCommand($"SELECT * FROM phizone_volunteervote WHERE id > {index}",
                mysqlConnection);
            await using var reader = await mysqlCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                index = reader.GetInt32("id");

                _logger.LogInformation(LogEvents.DataMigration, "Migrating Volunteer Vote #{Id}", index);

                var volunteerVote = new VolunteerVote
                {
                    ChartId = _chartSubmissionDictionary[reader.GetInt32("chart_id")],
                    Score = reader.GetInt32("value"),
                    Message = reader.GetString("message"),
                    OwnerId = _userDictionary[reader.GetInt32("user_id")],
                    DateCreated = reader.GetDateTimeOffset("time")
                };
                await _volunteerVoteRepository.CreateVolunteerVoteAsync(volunteerVote);
            }
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Volunteer Vote #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateVolunteerVotes(mysqlConnection, cancellationToken, index);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Volunteer Vote #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateVolunteerVotes(mysqlConnection, cancellationToken, index);
        }
    }

    private async Task MigrateCollaborations(MySqlConnection mysqlConnection, CancellationToken cancellationToken,
        int startIndex = 0)
    {
        var index = startIndex;
        try
        {
            _logger.LogInformation(LogEvents.DataMigration, "Migrating collaborations...");
            await using var mysqlCommand = new MySqlCommand($"SELECT * FROM phizone_volunteervote WHERE id > {index}",
                mysqlConnection);
            await using var reader = await mysqlCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                index = reader.GetInt32("id");
                var chartSubmission =
                    await _chartSubmissionRepository.GetChartSubmissionAsync(
                        _chartSubmissionDictionary[reader.GetInt32("chart_id")]);
                _logger.LogInformation(LogEvents.DataMigration, "Migrating Collaboration #{Id}", index);
                var inviteeId = _userDictionary[reader.GetInt32("invitee_id")];
                var date = reader.GetDateTimeOffset("time");

                var collaboration = new Collaboration
                {
                    SubmissionId = chartSubmission.Id,
                    Status = (RequestStatus)reader.GetInt32("status"),
                    InviterId = _userDictionary[reader.GetInt32("inviter_id")],
                    InviteeId = inviteeId,
                    DateCreated = date
                };
                await _collaborationRepository.CreateCollaborationAsync(collaboration);
                if (collaboration.Status != RequestStatus.Approved ||
                    chartSubmission.Status != RequestStatus.Approved || chartSubmission.RepresentationId == null)
                    continue;
                var authorship = new Authorship
                {
                    ResourceId = chartSubmission.RepresentationId.Value, AuthorId = inviteeId, DateCreated = date
                };
                await _authorshipRepository.CreateAuthorshipAsync(authorship);
            }
        }
        catch (SocketException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Collaboration #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateCollaborations(mysqlConnection, cancellationToken, index);
        }
        catch (IOException ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Migration aborted after Collaboration #{Index}", index);
            if (mysqlConnection.State == ConnectionState.Closed) await mysqlConnection.OpenAsync(cancellationToken);
            await MigrateCollaborations(mysqlConnection, cancellationToken, index);
        }
    }

    private static IEnumerable<string> SplitAuthorName(string input)
    {
        var regex = UserRichTextRegex();
        var segments = regex.Split(input);
        return segments.Where(segment => !string.IsNullOrEmpty(segment)).ToList();
    }

    [GeneratedRegex(@"[^\d.]+")]
    private static partial Regex NonDigitRegex();

    [GeneratedRegex("(\\[PZUser:\\d+:[^:]+(?::PZRT)?\\])")]
    private static partial Regex UserRichTextRegex();
}
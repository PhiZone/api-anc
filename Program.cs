using System.Reflection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using OpenIddict.Abstractions;
using PhiZoneApi.Configurations;
using PhiZoneApi.Data;
using PhiZoneApi.Filters;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Repositories;
using PhiZoneApi.Services;
using StackExchange.Redis;
using Role = PhiZoneApi.Models.Role;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddMvc(options => { options.Filters.Add(typeof(ModelValidationFilter)); });

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        };
    });
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(2, 0);
    options.ReportApiVersions = true;
    options.ApiVersionReader = new HeaderApiVersionReader("api-version");
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
    options.UseOpenIddict<int>();
});

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>().ReplaceDefaultEntities<int>();
    })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/auth/token");
        options.SetRevocationEndpointUris("/auth/revoke");
        options.AllowPasswordFlow();
        options.AllowRefreshTokenFlow();
        options.UseReferenceAccessTokens();
        options.UseReferenceRefreshTokens();
        // options.RegisterScopes(OpenIddictConstants.Permissions.Scopes.Email,
        //     OpenIddictConstants.Permissions.Scopes.Profile,
        //     OpenIddictConstants.Permissions.Scopes.Roles);
        options.SetAccessTokenLifetime(TimeSpan.FromHours(6));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(31));
        options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
        options.UseAspNetCore().EnableTokenEndpointPassthrough().DisableTransportSecurityRequirement();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddictConstants.Schemes.Bearer;
    options.DefaultChallengeScheme = OpenIddictConstants.Schemes.Bearer;
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AllowAnonymous", policy => { policy.RequireAssertion(_ => true); });

builder.Services.AddIdentity<User, Role>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddUserManager<UserManager<User>>()
    .AddDefaultTokenProviders();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserRelationRepository, UserRelationRepository>();
builder.Services.AddScoped<IRegionRepository, RegionRepository>();
builder.Services.AddScoped<IChapterRepository, ChapterRepository>();
builder.Services.AddScoped<ICollectionRepository, CollectionRepository>();
builder.Services.AddScoped<ISongRepository, SongRepository>();
builder.Services.AddScoped<IChartRepository, ChartRepository>();
builder.Services.AddScoped<IChartAssetRepository, ChartAssetRepository>();
builder.Services.AddScoped<IRecordRepository, RecordRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IReplyRepository, ReplyRepository>();
builder.Services.AddScoped<IChartRepository, ChartRepository>();
builder.Services.AddScoped<ILikeRepository, LikeRepository>();
builder.Services.AddScoped<IVoteRepository, VoteRepository>();
builder.Services.AddScoped<IVolunteerVoteRepository, VolunteerVoteRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IServiceScriptRepository, ServiceScriptRepository>();
builder.Services.AddScoped<IServiceRecordRepository, ServiceRecordRepository>();
builder.Services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
builder.Services.AddScoped<IAdmissionRepository, AdmissionRepository>();
builder.Services.AddScoped<IAuthorshipRepository, AuthorshipRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IPlayConfigurationRepository, PlayConfigurationRepository>();
builder.Services.AddScoped<ISongSubmissionRepository, SongSubmissionRepository>();
builder.Services.AddScoped<IChartSubmissionRepository, ChartSubmissionRepository>();
builder.Services.AddScoped<IChartAssetSubmissionRepository, ChartAssetSubmissionRepository>();
builder.Services.AddScoped<ICollaborationRepository, CollaborationRepository>();
builder.Services.AddScoped<IResourceRecordRepository, ResourceRecordRepository>();
builder.Services.AddScoped<IPetQuestionRepository, PetQuestionRepository>();
builder.Services.AddScoped<IPetChoiceRepository, PetChoiceRepository>();
builder.Services.AddScoped<IPetAnswerRepository, PetAnswerRepository>();
builder.Services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IEventDivisionRepository, EventDivisionRepository>();
builder.Services.AddScoped<IEventTaskRepository, EventTaskRepository>();
builder.Services.AddScoped<IEventTeamRepository, EventTeamRepository>();
builder.Services.AddScoped<IEventResourceRepository, EventResourceRepository>();
builder.Services.AddScoped<IParticipationRepository, ParticipationRepository>();
builder.Services.AddScoped<IHostshipRepository, HostshipRepository>();
builder.Services.AddScoped<IFilterService, FilterService>();
builder.Services.AddScoped<ILikeService, LikeService>();
builder.Services.AddScoped<IVoteService, VoteService>();
builder.Services.AddScoped<IVolunteerVoteService, VolunteerVoteService>();
builder.Services.AddScoped<IDtoMapper, DtoMapper>();
builder.Services.AddScoped<ETagFilter>();
builder.Services.AddScoped<ISongService, SongService>();
builder.Services.AddScoped<IChartService, ChartService>();
builder.Services.AddScoped<IRecordService, RecordService>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITapTapService, TapTapService>();
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<IMultimediaService, MultimediaService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddSingleton<IResourceService, ResourceService>();
builder.Services.AddSingleton<IFileStorageService, FileStorageService>();
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();
builder.Services.AddSingleton<IFeishuService, FeishuService>();
builder.Services.AddSingleton<IMessengerService, MessengerService>();
builder.Services.AddSingleton<IMeilisearchService, MeilisearchService>();
builder.Services.AddSingleton<ILeaderboardService, LeaderboardService>();
builder.Services.AddSingleton<IScriptService, ScriptService>();
builder.Services.AddSingleton<AuthProviderFactory>();
builder.Services.AddSingleton<IAuthProvider, GitHubAuthProvider>();
builder.Services.AddSingleton<IAuthProvider, PhiraAuthProvider>();
builder.Services.AddSingleton<IAuthProvider, DiscordAuthProvider>();
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetValue<string>("RedisConnection") ?? "localhost"));
builder.Services.AddSingleton<IHostedService>(provider => new MailSenderService(
    provider.GetService<IServiceScopeFactory>()!.CreateScope().ServiceProvider.GetService<IMailService>()!,
    provider.GetService<IRabbitMqService>()!, provider.GetService<IHostEnvironment>()!));
builder.Services.AddSingleton<IHostedService>(provider => new SongConverterService(
    provider.GetService<IRabbitMqService>()!,
    provider.GetService<IServiceScopeFactory>()!.CreateScope().ServiceProvider.GetService<ISongService>()!,
    provider.GetService<IServiceScopeFactory>()!.CreateScope().ServiceProvider.GetService<ISongRepository>()!,
    provider.GetService<IServiceScopeFactory>()!.CreateScope().ServiceProvider.GetService<ISongSubmissionRepository>()!,
    provider.GetService<IFeishuService>()!, provider.GetService<IHostEnvironment>()!,
    provider.GetService<ILogger<SongConverterService>>()!));
builder.Services.AddHostedService<DatabaseSeeder>();
builder.Services.AddHostedService<DataConsistencyMaintainer>();
builder.Services.AddHostedService<EventTaskScheduler>();

if (args.Length >= 1)
{
    if (string.Equals(args[0], "migrate", StringComparison.InvariantCultureIgnoreCase))
        builder.Services.AddHostedService<DataMigrationService>();
    if (string.Equals(args[0], "chartMigrate", StringComparison.InvariantCultureIgnoreCase))
        builder.Services.AddHostedService<ChartMigrationService>();
    if (string.Equals(args[0], "fileMigrate", StringComparison.InvariantCultureIgnoreCase))
        builder.Services.AddSingleton<IHostedService>(provider =>
            new FileMigrationService(provider, args.Length >= 2 ? int.Parse(args[1]) : 0));
}

builder.Services.Configure<ApiBehaviorOptions>(options => { options.SuppressModelStateInvalidFilter = true; });
builder.Services.Configure<DataSettings>(builder.Configuration.GetSection("DataSettings"));
builder.Services.Configure<TapTapSettings>(builder.Configuration.GetSection("TapTapSettings"));
builder.Services.Configure<FeishuSettings>(builder.Configuration.GetSection("FeishuSettings"));
builder.Services.Configure<MessengerSettings>(builder.Configuration.GetSection("MessengerSettings"));
builder.Services.Configure<LanguageSettings>(builder.Configuration.GetSection("LanguageSettings"));
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQSettings"));
builder.Services.Configure<MeilisearchSettings>(builder.Configuration.GetSection("MeilisearchSettings"));
builder.Services.Configure<List<AuthProvider>>(builder.Configuration.GetSection("AuthProviders"));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 256L * 1024 * 1024;
    options.ValueLengthLimit = int.MaxValue;
});
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 256L * 1024 * 1024;
});
builder.Services.Configure<IdentityOptions>(options =>
{
    options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Name;
    options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
    options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;
    options.SignIn.RequireConfirmedEmail = true;
    options.User.AllowedUserNameCharacters = string.Empty;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 0;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v2",
        new OpenApiInfo
        {
            Version = "v2", Title = "PhiZone API", Description = "Backend of PhiZone, based on ASP.NET Core."
        });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});

builder.Services.AddSwaggerGenNewtonsoftSupport();

var app = builder.Build();
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() ||
    (args.Length >= 1 && string.Equals(args[0], "docs", StringComparison.InvariantCultureIgnoreCase)))
{
    app.UseSwagger();
    app.UseSwaggerUI(options => { options.SwaggerEndpoint("/swagger/v2/swagger.json", "PhiZone API v2"); });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultControllerRoute();

app.Run();
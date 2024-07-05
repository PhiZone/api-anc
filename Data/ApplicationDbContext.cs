using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PhiZoneApi.Models;

namespace PhiZoneApi.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User, Role, int>(options)
{
    public override DbSet<User> Users { get; set; } = null!;
    public DbSet<UserRelation> UserRelations { get; init; } = null!;
    public DbSet<Region> Regions { get; init; } = null!;
    public DbSet<Chapter> Chapters { get; init; } = null!;
    public DbSet<Collection> Collections { get; init; } = null!;
    public DbSet<Song> Songs { get; init; } = null!;
    public DbSet<Admission> Admissions { get; init; } = null!;
    public DbSet<Chart> Charts { get; init; } = null!;
    public DbSet<ChartAsset> ChartAssets { get; init; } = null!;
    public DbSet<Authorship> Authorships { get; init; } = null!;
    public DbSet<Record> Records { get; init; } = null!;
    public DbSet<Tag> Tags { get; init; } = null!;
    public DbSet<Comment> Comments { get; init; } = null!;
    public DbSet<Reply> Replies { get; init; } = null!;
    public DbSet<Like> Likes { get; init; } = null!;
    public DbSet<Vote> Votes { get; init; } = null!;
    public DbSet<PlayConfiguration> PlayConfigurations { get; init; } = null!;
    public DbSet<Application> Applications { get; init; } = null!;
    public DbSet<ServiceScript> ServiceScripts { get; init; } = null!;
    public DbSet<ServiceRecord> ServiceRecords { get; init; } = null!;
    public DbSet<Announcement> Announcements { get; init; } = null!;
    public DbSet<VolunteerVote> VolunteerVotes { get; init; } = null!;
    public DbSet<SongSubmission> SongSubmissions { get; init; } = null!;
    public DbSet<ChartSubmission> ChartSubmissions { get; init; } = null!;
    public DbSet<ChartAssetSubmission> ChartAssetSubmissions { get; init; } = null!;
    public DbSet<Collaboration> Collaborations { get; init; } = null!;
    public DbSet<Notification> Notifications { get; init; } = null!;
    public DbSet<PetQuestion> PetQuestions { get; init; } = null!;
    public DbSet<PetChoice> PetChoices { get; init; } = null!;
    public DbSet<PetAnswer> PetAnswers { get; init; } = null!;
    public DbSet<ResourceRecord> ResourceRecords { get; init; } = null!;
    public DbSet<ApplicationUser> ApplicationUsers { get; init; } = null!;
    public DbSet<Event> Events { get; init; } = null!;
    public DbSet<EventDivision> EventDivisions { get; init; } = null!;
    public DbSet<EventTask> EventTasks { get; init; } = null!;
    public DbSet<EventTeam> EventTeams { get; init; } = null!;
    public DbSet<EventResource> EventResources { get; init; } = null!;
    public DbSet<Participation> Participations { get; init; } = null!;
    public DbSet<Hostship> Hostships { get; init; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        base.OnConfiguring(builder);
        builder.ConfigureWarnings(wb => { wb.Ignore(CoreEventId.RowLimitingOperationWithoutOrderByWarning); });
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>()
            .HasMany(e => e.Followers)
            .WithMany(e => e.Followees)
            .UsingEntity<UserRelation>(l => l.HasOne<User>(e => e.Follower).WithMany(e => e.FolloweeRelations),
                r => r.HasOne<User>(e => e.Followee).WithMany(e => e.FollowerRelations));

        builder.Entity<User>()
            .HasMany(e => e.Applications)
            .WithMany(e => e.ApplicationUsers)
            .UsingEntity<ApplicationUser>(
                l => l.HasOne<Application>(e => e.Application).WithMany(e => e.ApplicationUserRelations),
                r => r.HasOne<User>(e => e.User).WithMany(e => e.ApplicationLinks));

        builder.Entity<EventTeam>()
            .HasMany(e => e.Participants)
            .WithMany(e => e.EventTeams)
            .UsingEntity<Participation>(l => l.HasOne<User>(e => e.Participant).WithMany(e => e.Participations),
                r => r.HasOne<EventTeam>(e => e.Team).WithMany(e => e.Participations));

        builder.Entity<Event>()
            .HasMany(e => e.Hosts)
            .WithMany(e => e.Events)
            .UsingEntity<Hostship>(
                l => l.HasOne<User>(e => e.User).WithMany(e => e.Hostships),
                r => r.HasOne<Event>(e => e.Event).WithMany(e => e.Hostships));

        builder.Entity<Song>().HasMany(e => e.Tags).WithMany(e => e.Songs);

        builder.Entity<Chart>().HasMany(e => e.Tags).WithMany(e => e.Charts);

        builder.Entity<Resource>().UseTpcMappingStrategy();

        builder.Entity<User>().ToTable("Users");
        builder.Entity<Role>().ToTable("Roles");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
        builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
        builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
    }
}
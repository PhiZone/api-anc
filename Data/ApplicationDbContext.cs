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
    public DbSet<UserRelation> UserRelations { get; set; } = null!;
    public DbSet<Region> Regions { get; set; } = null!;
    public DbSet<Chapter> Chapters { get; set; } = null!;
    public DbSet<Collection> Collections { get; set; } = null!;
    public DbSet<Song> Songs { get; set; } = null!;
    public DbSet<Admission> Admissions { get; set; } = null!;
    public DbSet<Chart> Charts { get; set; } = null!;
    public DbSet<ChartAsset> ChartAssets { get; set; } = null!;
    public DbSet<Authorship> Authorships { get; set; } = null!;
    public DbSet<Record> Records { get; set; } = null!;
    public DbSet<Tag> Tags { get; set; } = null!;
    public DbSet<Comment> Comments { get; set; } = null!;
    public DbSet<Reply> Replies { get; set; } = null!;
    public DbSet<Like> Likes { get; set; } = null!;
    public DbSet<Vote> Votes { get; set; } = null!;
    public DbSet<PlayConfiguration> PlayConfigurations { get; set; } = null!;
    public DbSet<Application> Applications { get; set; } = null!;
    public DbSet<ApplicationService> ApplicationServices { get; set; } = null!;
    public DbSet<ApplicationServiceRecord> ApplicationServiceRecords { get; set; } = null!;
    public DbSet<Announcement> Announcements { get; set; } = null!;
    public DbSet<VolunteerVote> VolunteerVotes { get; set; } = null!;
    public DbSet<SongSubmission> SongSubmissions { get; set; } = null!;
    public DbSet<ChartSubmission> ChartSubmissions { get; set; } = null!;
    public DbSet<ChartAssetSubmission> ChartAssetSubmissions { get; set; } = null!;
    public DbSet<Collaboration> Collaborations { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<PetQuestion> PetQuestions { get; set; } = null!;
    public DbSet<PetChoice> PetChoices { get; set; } = null!;
    public DbSet<PetAnswer> PetAnswers { get; set; } = null!;
    public DbSet<ResourceRecord> ResourceRecords { get; set; } = null!;
    public DbSet<ApplicationUser> ApplicationUsers { get; set; } = null!;
    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<EventDivision> EventDivisions { get; set; } = null!;
    public DbSet<EventTask> EventTasks { get; set; } = null!;
    public DbSet<EventTeam> EventTeams { get; set; } = null!;
    public DbSet<Participation> Participations { get; set; } = null!;

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
            .HasMany(e => e.TapApplications)
            .WithMany(e => e.TapUsers)
            .UsingEntity<ApplicationUser>(
                l => l.HasOne<Application>(e => e.Application).WithMany(e => e.TapUserRelations),
                r => r.HasOne<User>(e => e.User).WithMany(e => e.TapUserRelations));

        builder.Entity<EventTeam>()
            .HasMany(e => e.Participants)
            .WithMany(e => e.EventTeams)
            .UsingEntity<Participation>(l => l.HasOne<User>(e => e.Participant).WithMany(e => e.Participations),
                r => r.HasOne<EventTeam>(e => e.EventTeam).WithMany(e => e.Participations));

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
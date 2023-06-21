using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Models;

namespace PhiZoneApi.Data;

public class DataContext : IdentityDbContext<User, Role, int>
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public DbSet<UserRelation> UserRelations { get; set; }
    public DbSet<Region> Regions { get; set; }
    public override DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserRelation>()
            .HasKey(rel => new { rel.FollowerId, rel.FolloweeId });

        builder.Entity<UserRelation>()
            .HasOne(rel => rel.Follower)
            .WithMany(user => user.Followees)
            .HasForeignKey(rel => rel.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserRelation>()
            .HasOne(rel => rel.Followee)
            .WithMany(user => user.Followers)
            .HasForeignKey(rel => rel.FolloweeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<User>().ToTable("Users");
        builder.Entity<Role>().ToTable("Roles");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
        builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
        builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
    }
}
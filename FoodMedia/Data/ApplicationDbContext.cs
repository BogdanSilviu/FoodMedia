using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Post> Posts { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<PostCategory> PostCategories { get; set; }
    public DbSet<UserFollow> UserFollows { get; set; }
    public DbSet<PostLike> PostLikes { get; set; }
    public DbSet<SavedPost> SavedPosts { get; set; }
    public DbSet<PostMedia> PostMedia { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Composite key for PostCategory (many-to-many)
        builder.Entity<PostCategory>()
            .HasKey(pc => new { pc.PostId, pc.CategoryId });

        // Configure UserFollow (self-referencing relationship)
        builder.Entity<UserFollow>()
            .HasOne(uf => uf.Follower)
            .WithMany(u => u.Following)
            .HasForeignKey(uf => uf.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserFollow>()
            .HasOne(uf => uf.Followee)
            .WithMany(u => u.Followers)
            .HasForeignKey(uf => uf.FolloweeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserFollow>()
            .HasIndex(uf => new { uf.FollowerId, uf.FolloweeId })
            .IsUnique();

        builder.Entity<PostLike>()
            .HasIndex(pl => new { pl.UserId, pl.PostId })
            .IsUnique();

        builder.Entity<SavedPost>()
            .HasIndex(sp => new { sp.UserId, sp.PostId })
            .IsUnique();

        // Prevent multiple cascade delete issues
        builder.Entity<Comment>()
            .HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PostLike>()
            .HasOne(pl => pl.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(pl => pl.PostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SavedPost>()
            .HasOne(sp => sp.Post)
            .WithMany(p => p.SavedByUsers)
            .HasForeignKey(sp => sp.PostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PostMedia>()
            .HasOne(pm => pm.Post)
            .WithMany(p => p.Media)
            .HasForeignKey(pm => pm.PostId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

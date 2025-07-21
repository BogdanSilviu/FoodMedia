using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }

    public string? ProfilePictureUrl { get; set; }
    public bool IsProfileComplete { get; set; } = false; // ✅ New property
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<UserFollow> Followers { get; set; } = new List<UserFollow>();
    public ICollection<UserFollow> Following { get; set; } = new List<UserFollow>();
    public ICollection<PostLike> LikedPosts { get; set; } = new List<PostLike>();
    public ICollection<SavedPost> SavedPosts { get; set; } = new List<SavedPost>();

}

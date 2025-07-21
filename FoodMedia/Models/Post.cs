public class Post
{
    public int Id { get; set; }

    public string Title { get; set; }
    public string Content { get; set; }
    public string MainImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public string UserId { get; set; }
    public ApplicationUser User { get; set; }

    public ICollection<Comment> Comments { get; set; }
    public ICollection<PostCategory> PostCategories { get; set; }
    public ICollection<PostLike> Likes { get; set; }
    public ICollection<SavedPost> SavedByUsers { get; set; }
    public ICollection<PostMedia> Media { get; set; }
}

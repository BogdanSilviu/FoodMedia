using Microsoft.Extensions.Hosting;

public class PostLike
{
    public int Id { get; set; }

    public string UserId { get; set; }
    public ApplicationUser User { get; set; }

    public int PostId { get; set; }
    public Post Post { get; set; }

    public DateTime LikedAt { get; set; }
}

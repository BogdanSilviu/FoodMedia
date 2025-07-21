using Microsoft.Extensions.Hosting;

public class PostMedia
{
    public int Id { get; set; }

    public string Url { get; set; }
    public string Type { get; set; } // e.g., "image", "video"

    public int PostId { get; set; }
    public Post Post { get; set; }
}

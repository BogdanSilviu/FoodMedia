using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

[AllowAnonymous]
public class FeedModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public FeedModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<ApplicationUser> ActiveFollowees { get; set; } = new();
    public List<Post> FeedPosts { get; set; } = new();
    public List<Category> AvailableCategories { get; set; } = new();
    public bool HasMorePosts { get; set; } = false;

    public async Task<IActionResult> OnGetAsync(int page = 0, int? categoryId = null)
    {
        var user = await _userManager.GetUserAsync(User);

        // Load categories for sidebar
        AvailableCategories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();

        IQueryable<Post> query;

        if (User.Identity?.IsAuthenticated == true && user != null)
        {
            ActiveFollowees = await _db.UserFollows
                .Where(f => f.FollowerId == user.Id)
                .Select(f => f.Followee)
                .ToListAsync();

            var followedIds = ActiveFollowees.Select(f => f.Id).ToList();

            query = _db.Posts
                .Include(p => p.User)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .Include(p => p.Likes)
                .Include(p => p.Comments).ThenInclude(c => c.User);

            if (followedIds.Any())
                query = query.Where(p => followedIds.Contains(p.UserId));
        }
        else
        {
            // guest fallback
            query = _db.Posts
                .Include(p => p.User)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .Include(p => p.Likes)
                .Include(p => p.Comments).ThenInclude(c => c.User);
        }

        if (categoryId.HasValue)
            query = query.Where(p => p.PostCategories.Any(pc => pc.CategoryId == categoryId.Value));

        FeedPosts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(page * 3)
            .Take(3)
            .ToListAsync();

        HasMorePosts = FeedPosts.Count == 3;

        return Page();
    }

    public async Task<IActionResult> OnGetLoadFeedAsync(int page = 0, int? categoryId = null)
    {
        var user = await _userManager.GetUserAsync(User);

        IQueryable<Post> query;

        if (User.Identity?.IsAuthenticated == true && user != null)
        {
            var followedIds = await _db.UserFollows
                .Where(f => f.FollowerId == user.Id)
                .Select(f => f.FolloweeId)
                .ToListAsync();

            query = _db.Posts
                .Include(p => p.User)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .Include(p => p.Likes)
                .Include(p => p.Comments).ThenInclude(c => c.User);

            if (followedIds.Any())
                query = query.Where(p => followedIds.Contains(p.UserId));
        }
        else
        {
            query = _db.Posts
                .Include(p => p.User)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .Include(p => p.Likes)
                .Include(p => p.Comments).ThenInclude(c => c.User);
        }

        if (categoryId.HasValue)
            query = query.Where(p => p.PostCategories.Any(pc => pc.CategoryId == categoryId.Value));

        var posts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(page * 3)
            .Take(3)
            .ToListAsync();

        // Return partial view with new posts
        return Partial("_PostCardList", posts);
    }

    [ValidateAntiForgeryToken]
    public async Task<JsonResult> OnPostToggleLikeAsync([FromBody] ToggleLikeRequest req)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return new JsonResult(new { success = false });

        var post = await _db.Posts.FindAsync(req.PostId);
        if (post == null) return new JsonResult(new { success = false });

        var existingLike = await _db.PostLikes
            .FirstOrDefaultAsync(l => l.PostId == req.PostId && l.UserId == user.Id);

        if (existingLike != null)
        {
            _db.PostLikes.Remove(existingLike);
        }
        else
        {
            _db.PostLikes.Add(new PostLike { PostId = req.PostId, UserId = user.Id, LikedAt = DateTime.UtcNow });
        }

        await _db.SaveChangesAsync();

        // Get the actual like count from DB
        var likeCount = await _db.PostLikes.CountAsync(l => l.PostId == req.PostId);

        return new JsonResult(new { success = true, likes = likeCount });
    }


    public class ToggleLikeRequest
    {
        public int PostId { get; set; }
    }


    public async Task<JsonResult> OnPostAddCommentAsync(int postId, string content)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || string.IsNullOrWhiteSpace(content))
            return new JsonResult(new { success = false });

        var comment = new Comment
        {
            PostId = postId,
            UserId = user.Id,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        return new JsonResult(new
        {
            success = true,
            comment = new
            {
                user = user.DisplayName ?? user.UserName,
                content = comment.Content,
                createdAt = comment.CreatedAt.ToString("g")
            }
        });
    }



}

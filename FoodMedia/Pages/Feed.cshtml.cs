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

        if (User.Identity?.IsAuthenticated == true && user != null)
        {
            ActiveFollowees = await _db.UserFollows
                .Where(f => f.FollowerId == user.Id)
                .Select(f => f.Followee)
                .ToListAsync();

            var followedIds = ActiveFollowees.Select(f => f.Id).ToList();

            IQueryable<Post> query;

            if (followedIds.Any())
            {
                query = _db.Posts
                    .Include(p => p.User)
                    .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                    .Where(p => followedIds.Contains(p.UserId));
            }
            else
            {
                // fallback for empty follow list
                query = _db.Posts
                    .Include(p => p.User)
                    .Include(p => p.PostCategories).ThenInclude(pc => pc.Category);
            }

            if (categoryId.HasValue)
                query = query.Where(p => p.PostCategories.Any(pc => pc.CategoryId == categoryId.Value));

            FeedPosts = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip(page * 3)
                .Take(3)
                .ToListAsync();

            HasMorePosts = FeedPosts.Count == 3;
        }

        else
        {
            // Guest mode: show popular and recent posts
            var popularPosts = await _db.Posts
                .Include(p => p.User)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .OrderByDescending(p => p.Likes.Count) // Make sure Likes navigation is loaded or consider a like count field for performance
                .Take(5)
                .ToListAsync();

            var latestPosts = await _db.Posts
                .Include(p => p.User)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            FeedPosts = popularPosts
                .Concat(latestPosts)
                .Distinct()
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            HasMorePosts = false;
        }

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

            if (followedIds.Any())
            {
                // show posts from followees
                query = _db.Posts
                    .Include(p => p.User)
                    .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                    .Where(p => followedIds.Contains(p.UserId));
            }
            else
            {
                // fallback: show all posts
                query = _db.Posts
                    .Include(p => p.User)
                    .Include(p => p.PostCategories).ThenInclude(pc => pc.Category);
            }
        }
        else
        {
            // guest fallback: show all posts
            query = _db.Posts
                .Include(p => p.User)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category);
        }

        if (categoryId.HasValue)
            query = query.Where(p => p.PostCategories.Any(pc => pc.CategoryId == categoryId.Value));

        var posts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(page * 3)
            .Take(3)
            .ToListAsync();

        // Return partial view with only the new batch of posts
        return Partial("_PostCardList", posts);
    }

}

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class ViewModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public ViewModel(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public string DisplayName { get; set; } = "";
    public string Bio { get; set; } = "";
    public string ProfilePictureUrl { get; set; } = "";
    public List<Post> UserPosts { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return NotFound();

        var user = await _userManager.Users
            .Include(u => u.Posts)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        DisplayName = user.DisplayName ?? user.UserName;
        Bio = user.Bio ?? "";
        ProfilePictureUrl = user.ProfilePictureUrl ?? "/images/default-profile.png";

        UserPosts = await _db.Posts
            .Where(p => p.UserId == userId)
            .Include(p => p.PostCategories)
                .ThenInclude(pc => pc.Category)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Page();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;
    private readonly ApplicationDbContext _dbContext;

    public ProfileModel(UserManager<ApplicationUser> userManager, IWebHostEnvironment env, ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _env = env;
        _dbContext = dbContext;
    }

    public string DisplayName { get; set; }
    public string Bio { get; set; }
    public string ProfilePictureUrl { get; set; }
    public List<Post> UserPosts { get; set; } = new();


    // List of categories to display
    public List<Category> AvailableCategories { get; set; }

    [BindProperty]
    public NewPostInputModel NewPost { get; set; }

    [BindProperty]
    public IFormFile? ImageFile { get; set; }

    public class NewPostInputModel
    {
        [Required]
        public string Title { get; set; }

        public string? Content { get; set; }

        public List<int> SelectedCategoryIds { get; set; } = new List<int>();

        public string? ImageUrl { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        DisplayName = user.DisplayName;
        Bio = user.Bio ?? "";
        ProfilePictureUrl = user.ProfilePictureUrl ?? "https://example.com/default-profile.jpg";

        AvailableCategories = await _dbContext.Categories.ToListAsync();
        NewPost = new NewPostInputModel();

        // Load user's posts
        UserPosts = await _dbContext.Posts
            .Include(p => p.PostCategories)
            .ThenInclude(pc => pc.Category)
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Page();
    }

 

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadProfileDataAsync();
            AvailableCategories = await _dbContext.Categories.ToListAsync();
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        string? imageUrl = null;

        if (ImageFile != null)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ImageFile.FileName)}";
            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "posts");
            Directory.CreateDirectory(uploadFolder);

            var filePath = Path.Combine(uploadFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await ImageFile.CopyToAsync(stream);
            }

            imageUrl = $"/uploads/posts/{fileName}";
        }

        // Create new post
        var post = new Post
        {
            Title = NewPost.Title,
            Content = NewPost.Content ?? string.Empty,
            MainImageUrl = imageUrl ?? "",
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id,
            PostCategories = new List<PostCategory>()
        };

        // Add categories to post
        foreach (var catId in NewPost.SelectedCategoryIds)
        {
            post.PostCategories.Add(new PostCategory
            {
                CategoryId = catId,
                Post = post
            });
        }

        _dbContext.Posts.Add(post);
        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your post has been created.";

        return RedirectToPage();
    }

    //private async Task LoadProfileDataAsync()
    //{
    //    var user = await _userManager.GetUserAsync(User);
    //    if (user != null)
    //    {
    //        DisplayName = user.DisplayName;
    //        Bio = user.Bio ?? "";
    //        ProfilePictureUrl = user.ProfilePictureUrl ?? "https://example.com/default-profile.jpg";
    //    }
    //}

    private async Task LoadProfileDataAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            DisplayName = user.DisplayName;
            Bio = user.Bio ?? "";
            ProfilePictureUrl = user.ProfilePictureUrl ?? "https://example.com/default-profile.jpg";

            UserPosts = await _dbContext.Posts
                .Include(p => p.PostCategories)
                .ThenInclude(pc => pc.Category)
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        if (NewPost == null)
        {
            NewPost = new NewPostInputModel();
        }
    }
}

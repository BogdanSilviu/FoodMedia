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

    public EditProfileModel EditModel { get; set; } = new();
    public NewPostInputModel NewPost { get; set; } = new();
    public List<Category> AvailableCategories { get; set; } = new();

    public class NewPostInputModel
    {
        [Required]
        public string Title { get; set; }

        public string? Content { get; set; }

        public List<int> SelectedCategoryIds { get; set; } = new();

        public string? ImageUrl { get; set; }
    }

    public class EditPostModelInput
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string? Content { get; set; }

        public IFormFile? ImageFile { get; set; }
    }

    public class EditProfileModel
    {
        [Required]
        public string DisplayName { get; set; }

        public string? Bio { get; set; }

        public IFormFile? ProfilePicture { get; set; }
    }

    public class NewPostModel
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public List<int> SelectedCategoryIds { get; set; } = new List<int>();
    }


    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToPage("/Account/Login");

        DisplayName = user.DisplayName;
        Bio = user.Bio;
        ProfilePictureUrl = user.ProfilePictureUrl;

        // Populate AvailableCategories and initialize models
        AvailableCategories = await _dbContext.Categories.ToListAsync();

        EditModel = new EditProfileModel
        {
            DisplayName = DisplayName,
            Bio = Bio
        };

        NewPost = new NewPostInputModel();

        return Page();
    }

    public async Task<IActionResult> OnPostCreatePostAsync(
        [FromForm] NewPostInputModel newPost,
        IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
        {
            await LoadProfileDataAsync();
            ViewData["ShowCreatePostForm"] = true;
            NewPost = newPost;
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        string? imageUrl = null;
        if (imageFile != null)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "posts");
            Directory.CreateDirectory(uploadFolder);
            var filePath = Path.Combine(uploadFolder, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await imageFile.CopyToAsync(stream);
            imageUrl = $"/uploads/posts/{fileName}";
        }

        var post = new Post
        {
            Title = newPost.Title,
            Content = newPost.Content ?? string.Empty,
            MainImageUrl = imageUrl ?? "",
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id,
            PostCategories = newPost.SelectedCategoryIds.Select(id => new PostCategory { CategoryId = id }).ToList()
        };

        _dbContext.Posts.Add(post);
        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your post has been created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditProfileAsync(
        [FromForm] EditProfileModel editModel)
    {
        if (!ModelState.IsValid)
        {
            await LoadProfileDataAsync();
            ViewData["ShowEditProfileForm"] = true;
            EditModel = editModel;
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        user.DisplayName = editModel.DisplayName;
        user.Bio = editModel.Bio;

        if (editModel.ProfilePicture != null)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(editModel.ProfilePicture.FileName)}";
            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploadFolder);
            var filePath = Path.Combine(uploadFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await editModel.ProfilePicture.CopyToAsync(stream);
            user.ProfilePictureUrl = $"/uploads/profiles/{fileName}";
        }

        await _userManager.UpdateAsync(user);

        TempData["SuccessMessage"] = "Profile updated successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditPostAsync(EditPostModelInput EditPostModel)


    {
        if (!ModelState.IsValid)
        {
            await LoadProfileDataAsync();
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var post = await _dbContext.Posts.FirstOrDefaultAsync(p => p.Id == EditPostModel.Id && p.UserId == user.Id);

        if (post == null) return NotFound();

        post.Title = EditPostModel.Title;
        post.Content = EditPostModel.Content ?? "";

        if (EditPostModel.ImageFile != null)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(EditPostModel.ImageFile.FileName)}";
            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "posts");
            Directory.CreateDirectory(uploadFolder);
            var filePath = Path.Combine(uploadFolder, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await EditPostModel.ImageFile.CopyToAsync(stream);
            post.MainImageUrl = $"/uploads/posts/{fileName}";
        }

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Post updated successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeletePostAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var post = await _dbContext.Posts.FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);
        if (post == null) return NotFound();

        _dbContext.Posts.Remove(post);
        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Post deleted successfully.";
        return RedirectToPage();
    }

    private async Task LoadProfileDataAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            DisplayName = user.DisplayName;
            Bio = user.Bio ?? string.Empty;
            ProfilePictureUrl = user.ProfilePictureUrl ?? "https://example.com/default-profile.jpg";

            UserPosts = await _dbContext.Posts
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .Include(p => p.Likes)
                // Include Comments and comment user to display commenter names
                .Include(p => p.Comments).ThenInclude(c => c.User)
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        AvailableCategories = await _dbContext.Categories.ToListAsync();
    }

    public async Task<IActionResult> OnPostAddCommentAsync(int postId, string commentContent)
    {
        if (string.IsNullOrWhiteSpace(commentContent))
        {
            // You might want to handle error feedback here if needed
            return RedirectToPage();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var comment = new Comment
        {
            PostId = postId,
            UserId = user.Id,
            Content = commentContent,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync();

        // Reload posts and comments so the new comment appears
        await LoadProfileDataAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetLoadPostsAsync(int page = 0, int pageSize = 5)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var posts = await _dbContext.Posts
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
            .Include(p => p.Likes)
            .Include(p => p.Comments).ThenInclude(c => c.User)
            .ToListAsync();

        var result = posts.Select(p => new
        {
            p.Id,
            p.Title,
            p.Content,
            p.MainImageUrl,
            CreatedAt = p.CreatedAt,
            Likes = p.Likes.Select(l => new { l.Id }), // minimal data just for count
            Comments = p.Comments.Select(c => new {
                c.Id,
                c.Content,
                User = new { displayName = c.User.DisplayName }
            }).ToList(),
            PostCategories = p.PostCategories.Select(pc => new {
                pc.Category.Id,
                pc.Category.Name
            }).ToList()
        });

        return new JsonResult(new { posts = result });
    }

    // Classes for models used in forms - adjust to your actual models
  
   
    // Your ApplicationUser, ApplicationDbContext, Category classes should be defined elsewhere
}


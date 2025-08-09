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

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadProfileDataAsync();

        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            EditModel.DisplayName = user.DisplayName;
            EditModel.Bio = user.Bio;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCreatePostAsync(
        [FromForm] NewPostInputModel newPost,
        IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
        {
            Console.WriteLine("CreatePost: ModelState is invalid.");
            foreach (var kvp in ModelState)
                foreach (var error in kvp.Value.Errors)
                    Console.WriteLine($"Field: {kvp.Key} — Error: {error.ErrorMessage}");

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
            Console.WriteLine("EditProfile: ModelState is invalid.");
            foreach (var kvp in ModelState)
                foreach (var error in kvp.Value.Errors)
                    Console.WriteLine($"Field: {kvp.Key} — Error: {error.ErrorMessage}");

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

    public async Task<IActionResult> OnPostEditPostAsync(int id, [FromForm] EditPostModelInput EditPostModel)
    {
        if (!ModelState.IsValid)
        {
            await LoadProfileDataAsync();
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var post = await _dbContext.Posts.FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);
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
                .Include(p => p.PostCategories)
                .ThenInclude(pc => pc.Category)
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        AvailableCategories = await _dbContext.Categories.ToListAsync();
    }
}

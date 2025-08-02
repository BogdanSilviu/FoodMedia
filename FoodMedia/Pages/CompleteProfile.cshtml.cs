using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

public class CompleteProfileModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IWebHostEnvironment _env;

    public CompleteProfileModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _env = env;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    [FromQuery]
    public string UserId { get; set; }

    [FromQuery]
    public string ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        public string DisplayName { get; set; }

        public string? Bio { get; set; }

        [Display(Name = "Profile Picture")]
        public IFormFile? ProfilePictureFile { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        Input = new InputModel
        {
            DisplayName = user.DisplayName,
            Bio = user.Bio
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        user.DisplayName = Input.DisplayName;
        user.Bio = Input.Bio ?? string.Empty;

        // Handle profile picture upload
        if (Input.ProfilePictureFile != null)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(Input.ProfilePictureFile.FileName)}";
            var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "profile-pictures");

            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            var filePath = Path.Combine(uploadDir, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await Input.ProfilePictureFile.CopyToAsync(stream);
            }

            user.ProfilePictureUrl = $"/uploads/profile-pictures/{fileName}";
        }
        else
        {
            user.ProfilePictureUrl ??= "https://example.com/default-profile.jpg";
        }

        user.IsProfileComplete = true;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return Page();
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(ReturnUrl ?? "~/");
    }
}

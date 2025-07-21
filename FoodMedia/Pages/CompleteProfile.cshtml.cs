using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

public class CompleteProfileModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public CompleteProfileModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
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

        [Display(Name = "Profile Picture URL")]
        public string? ProfilePictureUrl { get; set; }
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
            Bio = user.Bio,
            ProfilePictureUrl = user.ProfilePictureUrl
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
        user.ProfilePictureUrl = string.IsNullOrWhiteSpace(Input.ProfilePictureUrl)
            ? "https://example.com/default-profile.jpg"
            : Input.ProfilePictureUrl;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        // Optionally sign in the user now
        await _signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(ReturnUrl ?? "~/");
    }
}

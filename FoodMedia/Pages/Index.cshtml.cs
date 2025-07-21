using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FoodMedia.Data; // Adjust namespace for ApplicationUser if needed
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FoodMedia.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ILogger<IndexModel> logger, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null && !user.IsProfileComplete)
                {
                    // Redirect to complete profile page if profile is incomplete
                    return RedirectToPage("/CompleteProfile", new { UserId = user.Id, ReturnUrl = Url.Content("~/") });
                }
            }

            // Otherwise, just render the page normally
            return Page();
        }
    }
}

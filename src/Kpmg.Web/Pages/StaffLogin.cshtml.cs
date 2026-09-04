using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Kpmg.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kpmg.Web.Pages;

[AllowAnonymous]
public class StaffLoginModel : PageModel
{
    private readonly IStaffAccessCodeValidator _accessCodes;

    public StaffLoginModel(IStaffAccessCodeValidator accessCodes)
    {
        _accessCodes = accessCodes;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Please enter your name.")]
        [StringLength(80)]
        [Display(Name = "Your name")]
        public string StaffName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the team access code.")]
        [DataType(DataType.Password)]
        [Display(Name = "Team access code")]
        public string AccessCode { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!_accessCodes.IsValid(Input.AccessCode))
        {
            ModelState.AddModelError(string.Empty, "That access code is not recognised.");
            return Page();
        }

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, Input.StaffName), new Claim(ClaimTypes.Role, "CustomerService") },
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToPage("/Staff/Index");
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Index");
    }
}

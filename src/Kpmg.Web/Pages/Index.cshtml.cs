using System.ComponentModel.DataAnnotations;
using Kpmg.Web.Models;
using Kpmg.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kpmg.Web.Pages;

[RequestSizeLimit(30 * 1024 * 1024)]
public class IndexModel : PageModel
{
    private readonly IComplaintService _complaints;

    public IndexModel(IComplaintService complaints)
    {
        _complaints = complaints;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public List<IFormFile> Photos { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Please choose the type of issue.")]
        [Display(Name = "Type of issue")]
        public ComplaintCategory? Category { get; set; }

        [Required(ErrorMessage = "Please describe the issue.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Please describe the issue in at least 10 characters.")]
        [Display(Name = "What is the problem?")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please tell us your name.")]
        [StringLength(120)]
        [Display(Name = "Your name")]
        public string ReporterName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide an email address so we can send you updates.")]
        [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
        [StringLength(200)]
        [Display(Name = "Your email address")]
        public string ReporterEmail { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please provide a valid phone number.")]
        [StringLength(40)]
        [Display(Name = "Your phone number (optional)")]
        public string? ReporterPhone { get; set; }

        [Required(ErrorMessage = "We need your location. Please allow location access, or enter the coordinates manually.")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double? Latitude { get; set; }

        [Required(ErrorMessage = "We need your location. Please allow location access, or enter the coordinates manually.")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double? Longitude { get; set; }

        [StringLength(300)]
        [Display(Name = "Landmark or street name (optional)")]
        public string? LocationDescription { get; set; }
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var uploads = Photos
            .Where(file => file.Length > 0)
            .Select(file => new PhotoUpload(file.FileName, file.ContentType, file.Length, file.OpenReadStream))
            .ToList();

        var submission = new ComplaintSubmission
        {
            Category = Input.Category!.Value,
            Description = Input.Description,
            ReporterName = Input.ReporterName,
            ReporterEmail = Input.ReporterEmail,
            ReporterPhone = Input.ReporterPhone,
            Latitude = Input.Latitude!.Value,
            Longitude = Input.Longitude!.Value,
            LocationDescription = Input.LocationDescription,
            Photos = uploads
        };

        var result = await _complaints.SubmitAsync(submission, cancellationToken);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return Page();
        }

        return RedirectToPage("/Confirmation", new { requestNumber = result.Complaint!.RequestNumber });
    }
}

using System.ComponentModel.DataAnnotations;
using Kpmg.Web.Models;
using Kpmg.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kpmg.Web.Pages.Staff;

public class DetailsModel : PageModel
{
    private readonly IComplaintService _complaints;

    public DetailsModel(IComplaintService complaints)
    {
        _complaints = complaints;
    }

    public Complaint Complaint { get; private set; } = default!;

    [BindProperty]
    public ComplaintStatus Status { get; set; }

    [BindProperty]
    [StringLength(2000)]
    [Display(Name = "Case notes")]
    public string? StaffNotes { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var complaint = await _complaints.GetByIdAsync(id, cancellationToken);
        if (complaint is null)
        {
            return NotFound();
        }

        Complaint = complaint;
        Status = complaint.Status;
        StaffNotes = complaint.StaffNotes;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await OnGetAsync(id, cancellationToken);
        }

        var updated = await _complaints.UpdateStatusAsync(id, Status, StaffNotes, cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "The request has been updated.";
        return RedirectToPage("./Details", new { id });
    }
}

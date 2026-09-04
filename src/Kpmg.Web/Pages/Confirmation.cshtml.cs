using Kpmg.Web.Models;
using Kpmg.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kpmg.Web.Pages;

public class ConfirmationModel : PageModel
{
    private readonly IComplaintService _complaints;

    public ConfirmationModel(IComplaintService complaints)
    {
        _complaints = complaints;
    }

    public Complaint Complaint { get; private set; } = default!;

    public async Task<IActionResult> OnGetAsync(string? requestNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestNumber))
        {
            return RedirectToPage("/Index");
        }

        var complaint = await _complaints.GetByRequestNumberAsync(requestNumber, cancellationToken);
        if (complaint is null)
        {
            return RedirectToPage("/Track", new { requestNumber });
        }

        Complaint = complaint;
        return Page();
    }
}

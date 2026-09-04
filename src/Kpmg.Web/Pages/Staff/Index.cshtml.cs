using Kpmg.Web.Models;
using Kpmg.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kpmg.Web.Pages.Staff;

public class IndexModel : PageModel
{
    private readonly IComplaintService _complaints;

    public IndexModel(IComplaintService complaints)
    {
        _complaints = complaints;
    }

    [BindProperty(SupportsGet = true)]
    public ComplaintStatus? Status { get; set; }

    public IReadOnlyList<Complaint> Complaints { get; private set; } = Array.Empty<Complaint>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Complaints = await _complaints.ListAsync(Status, cancellationToken);
    }
}

using System.ComponentModel.DataAnnotations;
using Kpmg.Web.Models;
using Kpmg.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kpmg.Web.Pages;

public class TrackModel : PageModel
{
    private readonly IComplaintService _complaints;

    public TrackModel(IComplaintService complaints)
    {
        _complaints = complaints;
    }

    [BindProperty(SupportsGet = true)]
    [Display(Name = "Service request number")]
    public string? RequestNumber { get; set; }

    public Complaint? Complaint { get; private set; }

    public bool RequestNotFound { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(RequestNumber))
        {
            return;
        }

        Complaint = await _complaints.GetByRequestNumberAsync(RequestNumber, cancellationToken);
        RequestNotFound = Complaint is null;
    }
}

using System.ComponentModel.DataAnnotations;

namespace Kpmg.Web.Models;

public enum ComplaintStatus
{
    [Display(Name = "Submitted")]
    Submitted = 0,

    [Display(Name = "Acknowledged")]
    Acknowledged = 1,

    [Display(Name = "In progress")]
    InProgress = 2,

    [Display(Name = "Resolved")]
    Resolved = 3,

    [Display(Name = "Rejected")]
    Rejected = 4
}

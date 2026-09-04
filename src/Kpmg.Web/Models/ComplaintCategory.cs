using System.ComponentModel.DataAnnotations;

namespace Kpmg.Web.Models;

public enum ComplaintCategory
{
    [Display(Name = "Pothole")]
    Pothole = 0,

    [Display(Name = "Garbage on road")]
    GarbageOnRoad = 1,

    [Display(Name = "Broken road")]
    BrokenRoad = 2,

    [Display(Name = "No street lights")]
    NoStreetLights = 3,

    [Display(Name = "Blocked drain / flooding")]
    BlockedDrain = 4,

    [Display(Name = "Fly tipping")]
    FlyTipping = 5,

    [Display(Name = "Graffiti / vandalism")]
    Graffiti = 6,

    [Display(Name = "Other civic issue")]
    Other = 99
}

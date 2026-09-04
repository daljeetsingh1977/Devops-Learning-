using System.Security.Cryptography;
using System.Text;
using Kpmg.Web.Options;
using Microsoft.Extensions.Options;

namespace Kpmg.Web.Services;

public interface IStaffAccessCodeValidator
{
    /// <summary>Checks a code supplied on the staff sign-in page against the configured one.</summary>
    bool IsValid(string? suppliedCode);
}

public class StaffAccessCodeValidator : IStaffAccessCodeValidator
{
    private readonly IOptionsMonitor<StaffPortalOptions> _options;

    public StaffAccessCodeValidator(IOptionsMonitor<StaffPortalOptions> options)
    {
        _options = options;
    }

    public bool IsValid(string? suppliedCode)
    {
        var expected = _options.CurrentValue.AccessCode;
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(suppliedCode))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(suppliedCode));
    }
}

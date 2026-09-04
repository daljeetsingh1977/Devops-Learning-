using System.Security.Cryptography;

namespace Kpmg.Web.Services;

public interface IRequestNumberGenerator
{
    /// <summary>Creates a unique, human readable service request number.</summary>
    string Generate(DateTimeOffset timestamp);
}

public class RequestNumberGenerator : IRequestNumberGenerator
{
    // Characters that are hard to confuse when read out over the phone.
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public string Generate(DateTimeOffset timestamp)
    {
        Span<char> suffix = stackalloc char[6];
        for (var i = 0; i < suffix.Length; i++)
        {
            suffix[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return $"CSR-{timestamp.UtcDateTime:yyyyMMdd}-{new string(suffix)}";
    }
}

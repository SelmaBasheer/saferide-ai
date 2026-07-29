using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SafeRide.Identity.Application.Abstractions;

namespace SafeRide.Identity.Infrastructure.Security;

public sealed class OtpService : IOtpService
{
    public string Generate() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);

    public string Hash(string code) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

    public bool Verify(string code, string hash) => Hash(code) == hash;
}

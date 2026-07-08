using Microsoft.Extensions.Caching.Memory;

namespace PrimeRx.Services;

public class OtpEntry
{
    public string Code { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public bool IsUsed { get; set; }
}

public class OtpStore(IMemoryCache cache)
{
    private const string OtpPrefix = "otp_";
    private const string VerifiedPrefix = "otp_verified_";
    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(10);

    public string GenerateOtp(string normalizedEmail)
    {
        var code = Random.Shared.Next(100000, 999999).ToString();
        var entry = new OtpEntry
        {
            Code = code,
            ExpiresAt = DateTime.UtcNow.Add(OtpTtl)
        };
        cache.Set($"{OtpPrefix}{normalizedEmail}", entry, OtpTtl);
        return code;
    }

    public bool VerifyOtp(string normalizedEmail, string code)
    {
        if (cache.Get<OtpEntry>($"{OtpPrefix}{normalizedEmail}") is not { } entry)
            return false;
        if (entry.IsUsed || entry.Code != code || DateTime.UtcNow > entry.ExpiresAt)
            return false;
        entry.IsUsed = true;
        cache.Set($"{OtpPrefix}{normalizedEmail}", entry, OtpTtl);
        return true;
    }

    public void StoreVerifiedToken(string normalizedEmail, string token)
    {
        cache.Set($"{VerifiedPrefix}{normalizedEmail}", token, TimeSpan.FromMinutes(30));
    }

    public string? GetVerifiedToken(string normalizedEmail)
    {
        return cache.Get<string>($"{VerifiedPrefix}{normalizedEmail}");
    }

    public void Cleanup(string normalizedEmail)
    {
        cache.Remove($"{OtpPrefix}{normalizedEmail}");
        cache.Remove($"{VerifiedPrefix}{normalizedEmail}");
    }
}

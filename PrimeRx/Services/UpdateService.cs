using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace PrimeRx.Services;

// ─────────────────────────────────────────────────────────────────────────────
// UpdateService  —  checks GitHub releases, downloads, and verifies updates.
// The actual file-swap is performed by PrimeRxUpdater.exe (separate process).
// ─────────────────────────────────────────────────────────────────────────────

public sealed class UpdateService : IDisposable
{
    private readonly HttpClient _httpClient;
    private const string GitHubRepo  = "Mrcoderv/PrimeRx-Releases";
    private const string ApiUrl      = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";
    private const long   MinZipBytes = 1024 * 1024; // 1 MB — sanity guard

    public UpdateService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(15); // large download headroom
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "PrimeRx-Updater/2.0");

        var token = Environment.GetEnvironmentVariable("GITHUB_PERSONAL_ACCESS_TOKEN");
        if (!string.IsNullOrWhiteSpace(token))
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────

    public sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]    public string TagName     { get; set; } = string.Empty;
        [JsonPropertyName("name")]        public string Name        { get; set; } = string.Empty;
        [JsonPropertyName("body")]        public string Body        { get; set; } = string.Empty;
        [JsonPropertyName("html_url")]    public string HtmlUrl     { get; set; } = string.Empty;
        [JsonPropertyName("published_at")]public DateTime PublishedAt{ get; set; }
        [JsonPropertyName("assets")]      public List<GitHubAsset> Assets { get; set; } = [];
    }

    public sealed class GitHubAsset
    {
        [JsonPropertyName("name")]                 public string Name               { get; set; } = string.Empty;
        [JsonPropertyName("browser_download_url")] public string BrowserDownloadUrl { get; set; } = string.Empty;
        [JsonPropertyName("size")]                 public long   Size               { get; set; }
    }

    public sealed class UpdateInfo
    {
        public string  CurrentVersion   { get; set; } = string.Empty;
        public string  LatestVersion    { get; set; } = string.Empty;
        public bool    UpdateAvailable  { get; set; }
        public string? DownloadUrl      { get; set; }
        /// <summary>URL of the companion .sha256 file on GitHub releases.</summary>
        public string? ChecksumUrl      { get; set; }
        /// <summary>SHA256 hex string fetched from the release (populated by PrepareUpdateAsync).</summary>
        public string? ExpectedChecksum { get; set; }
        public string? ReleaseNotes     { get; set; }
        public string? ReleaseUrl       { get; set; }
        public long    DownloadSize     { get; set; }
    }

    /// <summary>Represents a successfully downloaded and (optionally) verified update package.</summary>
    public sealed class UpdatePackage
    {
        public string  ZipPath          { get; set; } = string.Empty;
        public string? VerifiedChecksum { get; set; }
        /// <summary>True if a checksum was present AND matched. False means no checksum was available.</summary>
        public bool    ChecksumVerified { get; set; }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Contacts GitHub and returns version + download metadata.
    /// Never throws — returns safe defaults on error.
    /// </summary>
    public async Task<UpdateInfo> CheckForUpdatesAsync()
    {
        var currentVersion = GetCurrentVersion();
        try
        {
            var release = await _httpClient.GetFromJsonAsync<GitHubRelease>(ApiUrl);
            if (release == null)
                return NoUpdate(currentVersion);

            var latestVersion = release.TagName.TrimStart('v');
            if (!Version.TryParse(latestVersion, out var latest) ||
                !Version.TryParse(currentVersion, out var current))
                return NoUpdate(currentVersion);

            var updateAvailable = latest > current;
            var zipAsset = release.Assets.FirstOrDefault(a =>
                a.Name.Contains("win-x64", StringComparison.OrdinalIgnoreCase) &&
                a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

            // Look for companion checksum file: same name + ".sha256"
            string? checksumUrl = null;
            if (zipAsset != null)
            {
                var checksumAssetName = zipAsset.Name + ".sha256";
                checksumUrl = release.Assets
                    .FirstOrDefault(a => a.Name.Equals(checksumAssetName, StringComparison.OrdinalIgnoreCase))
                    ?.BrowserDownloadUrl;
            }

            return new UpdateInfo
            {
                CurrentVersion  = currentVersion,
                LatestVersion   = latestVersion,
                UpdateAvailable = updateAvailable,
                DownloadUrl     = zipAsset?.BrowserDownloadUrl,
                ChecksumUrl     = checksumUrl,
                ReleaseNotes    = release.Body,
                ReleaseUrl      = release.HtmlUrl,
                DownloadSize    = zipAsset?.Size ?? 0,
            };
        }
        catch (Exception ex)
        {
            // Expected in dev / before the releases repo has its first published
            // release (404). Not fatal — the app just reports "no update available".
            Serilog.Log.Debug(ex, "[UpdateService] CheckForUpdates skipped (non-fatal)");
            return NoUpdate(currentVersion);
        }
    }

    /// <summary>
    /// Full pipeline: download zip, download checksum, verify, return package ready for the updater.
    /// Reports download progress (0–100) via <paramref name="progress"/>.
    /// Throws <see cref="InvalidOperationException"/> when verification fails.
    /// </summary>
    public async Task<UpdatePackage> PrepareUpdateAsync(
        UpdateInfo info,
        string destinationZipPath,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(info.DownloadUrl))
            throw new InvalidOperationException("No download URL in UpdateInfo.");

        // Download zip with progress
        await DownloadWithProgressAsync(info.DownloadUrl, destinationZipPath, info.DownloadSize, progress, ct);

        // Basic sanity: file must exist and be at least 1 MB
        var fi = new FileInfo(destinationZipPath);
        if (!fi.Exists || fi.Length < MinZipBytes)
            throw new InvalidOperationException(
                $"Downloaded file is too small ({fi.Length:N0} bytes). It may be corrupt.");

        // Checksum verification
        // Rule: if ChecksumUrl is present, verification is MANDATORY — a network
        // failure or empty file is treated as a hard error, not a silent skip.
        // If no ChecksumUrl was published (legacy/developer release), we allow the
        // update but log a warning so the operator knows integrity was not verified.
        string? expectedHash = null;
        bool checksumVerified = false;

        if (!string.IsNullOrEmpty(info.ChecksumUrl))
        {
            // Download — let any exception propagate; caller sees a clear message.
            string rawHash;
            try
            {
                rawHash = await DownloadChecksumAsync(info.ChecksumUrl, ct);
            }
            catch (Exception ex)
            {
                TryDeleteFile(destinationZipPath);
                throw new InvalidOperationException(
                    $"Failed to download the integrity checksum file from GitHub. " +
                    $"The update has been cancelled to protect against installing a corrupt package. " +
                    $"Please check your internet connection and try again. (Detail: {ex.Message})", ex);
            }

            expectedHash = rawHash.Trim();
            if (string.IsNullOrWhiteSpace(expectedHash))
            {
                TryDeleteFile(destinationZipPath);
                throw new InvalidOperationException(
                    "The checksum file on GitHub is empty. " +
                    "This may indicate a broken release. Please contact Prime LogicTech support.");
            }

            var actualHash = await ComputeSha256Async(destinationZipPath, ct);
            if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                TryDeleteFile(destinationZipPath);
                throw new InvalidOperationException(
                    $"SHA-256 checksum mismatch — the downloaded file is corrupt or has been tampered with.\n" +
                    $"Expected : {expectedHash.ToUpperInvariant()}\n" +
                    $"Actual   : {actualHash.ToUpperInvariant()}\n" +
                    "The corrupt file has been deleted. Please try the update again.");
            }

            checksumVerified = true;
        }
        else
        {
            // No checksum asset was published with this release.
            Console.WriteLine(
                "[UpdateService] WARNING: No .sha256 asset found for this release. " +
                "Integrity cannot be verified. Consider re-publishing with a checksum file.");
        }

        return new UpdatePackage
        {
            ZipPath          = destinationZipPath,
            VerifiedChecksum = expectedHash?.Trim(),
            ChecksumVerified = checksumVerified,
        };
    }

    /// <summary>Downloads a file with progress reporting.</summary>
    public async Task DownloadWithProgressAsync(
        string url,
        string destinationPath,
        long expectedBytes,
        IProgress<int>? progress,
        CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? expectedBytes;
        await using var dest   = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await using var source = await response.Content.ReadAsStreamAsync(ct);

        var buffer     = new byte[81920];
        long bytesRead = 0;
        int  read;

        while ((read = await source.ReadAsync(buffer, ct)) > 0)
        {
            await dest.WriteAsync(buffer.AsMemory(0, read), ct);
            bytesRead += read;

            if (totalBytes > 0)
                progress?.Report((int)(bytesRead * 100 / totalBytes));
        }

        progress?.Report(100);
    }

    /// <summary>Downloads and returns the SHA256 hex string from a .sha256 file.</summary>
    public async Task<string> DownloadChecksumAsync(string checksumUrl, CancellationToken ct = default)
    {
        var text = await _httpClient.GetStringAsync(checksumUrl, ct);
        // File format: "<hex>  <filename>" or just "<hex>"
        return text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)[0];
    }

    /// <summary>Computes SHA256 of a local file, returned as lowercase hex.</summary>
    public static async Task<string> ComputeSha256Async(string filePath, CancellationToken ct = default)
    {
        using var sha256 = SHA256.Create();
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        var hash = await sha256.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public static string GetCurrentVersion() =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    private static UpdateInfo NoUpdate(string version) =>
        new() { CurrentVersion = version, LatestVersion = version, UpdateAvailable = false };

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
    }

    public void Dispose() => _httpClient.Dispose();
}

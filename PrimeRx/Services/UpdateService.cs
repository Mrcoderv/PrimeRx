using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;

namespace PrimeRx.Services;

public class UpdateService
{
    private readonly HttpClient _httpClient;
    private const string GitHubRepo = "Mrcoderv/PrimeRx-Releases";
    private const string ApiUrl = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";

    public UpdateService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "PrimeRx");
    }

    public class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = new();
    }

    public class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    public class UpdateInfo
    {
        public string CurrentVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public bool UpdateAvailable { get; set; }
        public string? DownloadUrl { get; set; }
        public string? ReleaseNotes { get; set; }
        public string? ReleaseUrl { get; set; }
        public long DownloadSize { get; set; }
    }

    public async Task<UpdateInfo> CheckForUpdatesAsync()
    {
        try
        {
            var currentVersion = Assembly.GetExecutingAssembly()
                .GetName()
                .Version?.ToString() ?? "1.0.0";

            var release = await _httpClient.GetFromJsonAsync<GitHubRelease>(ApiUrl);
            
            if (release == null)
            {
                return new UpdateInfo
                {
                    CurrentVersion = currentVersion,
                    LatestVersion = currentVersion,
                    UpdateAvailable = false
                };
            }

            var latestVersion = release.TagName.Replace("v", "");
            
            var updateAvailable = new Version(latestVersion) > new Version(currentVersion);

            var win64Asset = release.Assets.FirstOrDefault(a => 
                a.Name.Contains("win-x64") && a.Name.EndsWith(".zip"));

            return new UpdateInfo
            {
                CurrentVersion = currentVersion,
                LatestVersion = latestVersion,
                UpdateAvailable = updateAvailable,
                DownloadUrl = win64Asset?.BrowserDownloadUrl,
                ReleaseNotes = release.Body,
                ReleaseUrl = release.HtmlUrl,
                DownloadSize = win64Asset?.Size ?? 0
            };
        }
        catch (Exception ex)
        {
            // Log error but don't crash the app
            Console.WriteLine($"Error checking for updates: {ex.Message}");
            
            return new UpdateInfo
            {
                CurrentVersion = Assembly.GetExecutingAssembly()
                    .GetName()
                    .Version?.ToString() ?? "1.0.0",
                LatestVersion = "1.0.0",
                UpdateAvailable = false
            };
        }
    }

    public async Task<string?> DownloadUpdateAsync(string downloadUrl, string destinationPath)
    {
        try
        {
            var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var fileStream = File.Create(destinationPath);
            await using var stream = await response.Content.ReadAsStreamAsync();
            await stream.CopyToAsync(fileStream);

            return destinationPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading update: {ex.Message}");
            return null;
        }
    }
}

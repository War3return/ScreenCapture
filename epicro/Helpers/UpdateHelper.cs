using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json.Linq;

namespace epicro.Helpers
{
    public static class UpdateHelper
    {
        // ── 설정: 아래 두 값을 실제 GitHub 계정명/레포명으로 변경하세요 ──
        private const string GitHubOwner = "War3return";
        private const string GitHubRepo  = "epicro_update";
        // ─────────────────────────────────────────────────────────────────

        private const string AssetName = "epicro.zip";
        private const string ApiUrl    =
            "https://api.github.com/repos/" + GitHubOwner + "/" + GitHubRepo + "/releases/latest";

        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        static UpdateHelper()
        {
            _http.DefaultRequestHeaders.Add("User-Agent", "epicro-updater/1.0");
        }

        /// <summary>
        /// 앱 시작 시 호출. GitHub에서 최신 버전을 확인하고, 더 높은 버전이 있으면
        /// 사용자에게 업데이트 여부를 물어봅니다. 네트워크 오류는 무음으로 처리됩니다.
        /// </summary>
        public static async Task CheckAndPromptUpdateAsync()
        {
            // GitHub 정보가 아직 설정되지 않으면 조용히 건너뜀
            if (GitHubOwner == "your-github-username")
            {
                Debug.WriteLine("[UpdateHelper] GitHub owner가 설정되지 않아 업데이트 확인을 건너뜁니다.");
                return;
            }

            try
            {
                var (hasUpdate, latestTag, downloadUrl) = await FetchLatestReleaseAsync();

                if (!hasUpdate || string.IsNullOrEmpty(downloadUrl))
                    return;

                var result = MessageBox.Show(
                    $"새 버전 {latestTag}이(가) 출시되었습니다.\n지금 업데이트하시겠습니까?",
                    "epicro 업데이트",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result != MessageBoxResult.Yes)
                    return;

                string tempPath = await DownloadToTempAsync(downloadUrl);

                if (string.IsNullOrEmpty(tempPath))
                {
                    MessageBox.Show(
                        "파일 다운로드에 실패했습니다.\n인터넷 연결을 확인하고 다시 시도하세요.",
                        "업데이트 오류",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                LaunchUpdaterAndExit(tempPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateHelper] {ex.GetType().Name}: {ex.Message}");
                // 네트워크 오류 등은 무음 처리 - 앱 시작에 영향 없음
            }
        }

        private static async Task<(bool hasUpdate, string latestTag, string downloadUrl)>
            FetchLatestReleaseAsync()
        {
            string json = await _http.GetStringAsync(ApiUrl);
            JObject release = JObject.Parse(json);

            string tagName = release["tag_name"]?.ToString();
            if (string.IsNullOrEmpty(tagName))
                return (false, null, null);

            Version latestVersion  = ParseTagVersion(tagName);
            Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

            if (latestVersion == null || latestVersion <= currentVersion)
                return (false, tagName, null);

            string dlUrl = null;
            if (release["assets"] is JArray assets)
            {
                foreach (var asset in assets)
                {
                    string name = asset["name"]?.ToString();
                    if (string.Equals(name, AssetName, StringComparison.OrdinalIgnoreCase))
                    {
                        dlUrl = asset["browser_download_url"]?.ToString();
                        break;
                    }
                }
            }

            return (!string.IsNullOrEmpty(dlUrl), tagName, dlUrl);
        }

        private static Version ParseTagVersion(string tag)
        {
            string cleaned = tag.TrimStart('v', 'V').Trim();
            return Version.TryParse(cleaned, out Version v) ? v : null;
        }

        private static async Task<string> DownloadToTempAsync(string url)
        {
            try
            {
                string tempFile = Path.Combine(
                    Path.GetTempPath(),
                    $"epicro_update_{DateTime.Now:yyyyMMddHHmmss}.zip");

                using (var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                using (var srcStream = await response.Content.ReadAsStreamAsync())
                using (var dstStream = File.Create(tempFile))
                {
                    await srcStream.CopyToAsync(dstStream);
                }

                return tempFile;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateHelper] Download error: {ex.Message}");
                return null;
            }
        }

        private static void LaunchUpdaterAndExit(string zipPath)
        {
            string currentExePath = Process.GetCurrentProcess().MainModule.FileName;
            string installDir     = Path.GetDirectoryName(currentExePath);
            string extractDir     = Path.Combine(Path.GetTempPath(), "epicro_update_extracted");
            int    pid            = Process.GetCurrentProcess().Id;

            // bat 파일 방식은 CMD 인코딩(CP949↔UTF-8 불일치), xcopy 레거시 한계로
            // 한글 경로에서 실패함. PowerShell -EncodedCommand를 직접 사용한다.
            //
            // -EncodedCommand: 스크립트를 UTF-16LE Base64로 전달 →
            //   • CMD를 거치지 않으므로 chcp/인코딩 문제 없음
            //   • PowerShell cmdlet은 유니코드 경로를 네이티브 지원
            //   • xcopy 대신 Copy-Item 사용 (유니코드 경로 완전 지원)
            string psScript = $@"
$targetPid = {pid}
while (Get-Process -Id $targetPid -ErrorAction SilentlyContinue) {{
    Start-Sleep -Seconds 1
}}
if (Test-Path -LiteralPath ""{extractDir}"") {{
    Remove-Item -LiteralPath ""{extractDir}"" -Recurse -Force
}}
Expand-Archive -LiteralPath ""{zipPath}"" -DestinationPath ""{extractDir}"" -Force
Copy-Item -Path ""{extractDir}\*"" -Destination ""{installDir}"" -Recurse -Force
Remove-Item -LiteralPath ""{extractDir}"" -Recurse -Force
Remove-Item -LiteralPath ""{zipPath}"" -Force
Start-Process -FilePath ""{currentExePath}""
";
            // UTF-16LE Base64 인코딩 — PowerShell -EncodedCommand의 표준 인코딩
            string encodedCommand = Convert.ToBase64String(
                System.Text.Encoding.Unicode.GetBytes(psScript));

            Process.Start(new ProcessStartInfo
            {
                FileName        = "powershell.exe",
                Arguments       = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encodedCommand}",
                UseShellExecute = true,
                Verb            = "runas",
                WindowStyle     = ProcessWindowStyle.Hidden
            });

            // Window_Closing 이벤트를 통해 매크로/캡처 등 기존 cleanup 실행
            Application.Current.Shutdown();
        }
    }
}

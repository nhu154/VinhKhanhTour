using Microsoft.Maui.Media;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using VinhKhanhTour.Models;

namespace VinhKhanhTour.Services
{
    public class AudioService
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        private static AudioService? _instance;
        public static AudioService Instance => _instance ??= new AudioService();

        // ── Google Cloud TTS config ────────────────────────────────────────────
        private const string GOOGLE_TTS_API_KEY = "AIzaSyAMX0XgjmNv2O4Twk_CBBmjzDwopqtuexE";
        private const string GOOGLE_TTS_URL =
            "https://texttospeech.googleapis.com/v1/text:synthesize?key=" + GOOGLE_TTS_API_KEY;

        // Giọng theo ngôn ngữ
        private static readonly Dictionary<string, (string voice, string lang, string gender)> VoiceMap = new()
        {
            ["vi"] = ("vi-VN-Wavenet-A", "vi-VN", "FEMALE"),
            ["en"] = ("en-US-Wavenet-F", "en-US", "FEMALE"),
            ["zh"] = ("cmn-CN-Wavenet-A", "cmn-CN", "FEMALE"),
            ["ja"] = ("ja-JP-Wavenet-D", "ja-JP", "FEMALE"),
            ["ko"] = ("ko-KR-Wavenet-A", "ko-KR", "FEMALE"),
            ["fr"] = ("fr-FR-Wavenet-C", "fr-FR", "FEMALE"),
            ["th"] = ("th-TH-Neural2-C", "th-TH", "FEMALE"),
            ["ru"] = ("ru-RU-Wavenet-C", "ru-RU", "FEMALE"),
            ["de"] = ("de-DE-Wavenet-F", "de-DE", "FEMALE"),
            ["es"] = ("es-ES-Wavenet-C", "es-ES", "FEMALE"),
        };

        // ── Ngôn ngữ hiện tại ─────────────────────────────────────────────────
        private string _language = "vi";

        public void SetLanguage(string lang)
        {
            _language = string.IsNullOrWhiteSpace(lang) ? "vi" : lang.Trim().ToLower();
            System.Diagnostics.Debug.WriteLine($"[AudioService] Language set to: {_language}");
        }

        public string CurrentLanguage => _language;

        // ── Trạng thái ────────────────────────────────────────────────────────
        private bool _isPlaying;
        private int _currentPoiId = -1;
        private CancellationTokenSource? _ttsCts;

        public string? CurrentTrack { get; private set; }
        public event Action<bool>? PlaybackStateChanged;
        public bool IsPlaying => _isPlaying;

        // ── Entry point cho LocationForegroundService ─────────────────────────

        /// <summary>
        /// Phát thuyết minh cho một POI. Ngắt ngay POI hiện tại (nếu có) để nhảy sang bài mới.
        /// </summary>
        public async Task PlayNarrationAsync(Restaurant poi, double lat = 0, double lng = 0)
        {
            var script = GetScript(poi, _language);

            if (string.IsNullOrWhiteSpace(script))
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] POI {poi.Id} không có script [{_language}], bỏ qua.");
                return;
            }

            if (_isPlaying && _currentPoiId == poi.Id) return;

            if (_isPlaying)
            {
                await StopAsync();
            }

            await PlayCommentaryAsync(poi.Id, script, lat, lng);
        }

        // Lấy TTS script đúng ngôn ngữ từ model Restaurant - ĐÃ NÂNG CẤP ĐỘNG
        private static string GetScript(Restaurant poi, string lang) => poi.GetTtsScript();

        // ── Core playback ─────────────────────────────────────────────────────

        private readonly SemaphoreSlim _playSemaphore = new SemaphoreSlim(1, 1);

        public async Task PlayCommentaryAsync(int poiId, string ttsScript, double lat = 0, double lng = 0)
        {
            // Ngắt bài cũ (nếu có)
            await StopAsync();

            // Chờ bài cũ dọn dẹp xong hoàn toàn trạng thái và record API
            await _playSemaphore.WaitAsync();
            try
            {
                _isPlaying = true;
                _currentPoiId = poiId;
                PlaybackStateChanged?.Invoke(true);
                if (poiId > 0) _ = AnalyticsService.Instance.RecordPoiVisitAsync(poiId, $"poi_audio_started_{CurrentLanguage}", lat, lng);

                _ttsCts = new CancellationTokenSource();
                DateTime startTime = DateTime.Now;

                try
                {
                    var hasInternet = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
                    if (hasInternet)
                        await PlayGoogleTtsAsync(ttsScript, _ttsCts.Token);
                    else
                        await PlayMauiTtsAsync(ttsScript, _ttsCts.Token);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AudioService] TTS lỗi: {ex.Message}");
                    if (!(_ttsCts?.IsCancellationRequested ?? true))
                    {
                        try { await PlayMauiTtsAsync(ttsScript, _ttsCts?.Token ?? default); }
                        catch { }
                    }
                }
                finally
                {
                    double durationSeconds = (DateTime.Now - startTime).TotalSeconds;
                    // Phải đảm bảo await thành công Record audio trước khi kết thúc
                    if (poiId > 0) await AnalyticsService.Instance.RecordAudioPlayedAsync(poiId, CurrentLanguage, durationSeconds);
                    
                    _isPlaying = false;
                    _currentPoiId = -1;
                    CurrentTrack = null;
                    PlaybackStateChanged?.Invoke(false);
                }
            }
            finally
            {
                _playSemaphore.Release();
            }
        }

        public async Task StopAsync()
        {
            _ttsCts?.Cancel();

            if (_isPlaying)
            {
                try { await TextToSpeech.Default.SpeakAsync("", new SpeechOptions()); }
                catch { /* ignore */ }
            }
            // Không set _isPlaying = false ở đây vì việc đó thuộc trách nhiệm của finally block trong hàm PlayCommentaryAsync,
            // để đảm bảo nó tính thời gian và record xong rồi mới clean up data!
            await Task.Delay(100);
        }

        public void ResetPoi(int poiId)
        {
            if (_currentPoiId == poiId) _currentPoiId = -1;
        }

        // ── Google Cloud TTS ───────────────────────────────────────────────────

        private async Task PlayGoogleTtsAsync(string script, CancellationToken token)
        {
            var (voiceName, langCode, gender) = VoiceMap.TryGetValue(_language, out var v)
                ? v : ("vi-VN-Wavenet-A", "vi-VN", "FEMALE");

            if (!VoiceMap.ContainsKey(_language))
                langCode = _language.Length == 2
                    ? $"{_language}-{_language.ToUpper()}"
                    : _language;

            CurrentTrack = $"Google TTS ({_language.ToUpper()})";

            using var md5 = System.Security.Cryptography.MD5.Create();
            var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(script + _language));
            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            var tempPath = Path.Combine(FileSystem.CacheDirectory,
                $"gtts_{_currentPoiId}_{_language}_{hashString}.mp3");

            if (!File.Exists(tempPath))
            {
                var requestBody = new
                {
                    input = new { text = script },
                    voice = new { languageCode = langCode, name = voiceName, ssmlGender = gender },
                    audioConfig = new { audioEncoding = "MP3", speakingRate = 0.95, pitch = 0.0 }
                };

                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await http.PostAsync(GOOGLE_TTS_URL, content, token);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(token);
                using var doc = JsonDocument.Parse(responseJson);
                var audioBase64 = doc.RootElement.GetProperty("audioContent").GetString()
                    ?? throw new Exception("audioContent trống");

                await File.WriteAllBytesAsync(tempPath, Convert.FromBase64String(audioBase64), token);
            }

            if (!token.IsCancellationRequested)
                await PlayLocalFileAsync(tempPath, token);
        }

        // ── Phát file MP3 local ────────────────────────────────────────────────

        private static Task PlayLocalFileAsync(string filePath, CancellationToken token)
        {
#if ANDROID
            return Task.Run(() =>
            {
                var player = new Android.Media.MediaPlayer();
                try
                {
                    player.SetDataSource(filePath);
                    player.Prepare();
                    player.Start();

                    var done = new System.Threading.ManualResetEventSlim(false);
                    player.Completion += (s, e) => done.Set();

                    using var reg = token.Register(() =>
                    {
                        try { if (player.IsPlaying) player.Stop(); } catch { }
                        done.Set();
                    });

                    done.Wait(TimeSpan.FromMinutes(5));
                }
                catch { }
                finally { try { player.Release(); } catch { } }
            });
#elif IOS
            return Task.Run(() =>
            {
                var url = new Foundation.NSUrl(filePath, false);
                using var session = AVFoundation.AVAudioSession.SharedInstance();
                session.SetCategory(AVFoundation.AVAudioSessionCategory.Playback);
                session.SetActive(true);

                var player = AVFoundation.AVAudioPlayer.FromUrl(url);
                if (player == null) return;
                player.PrepareToPlay();
                player.Play();

                using var reg = token.Register(() =>
                {
                    try { player.Stop(); } catch { }
                });

                while (player.Playing && !token.IsCancellationRequested)
                    System.Threading.Thread.Sleep(100);

                try { player.Stop(); player.Dispose(); } catch { }
            });
#else
            throw new PlatformNotSupportedException();
#endif
        }

        // ── MAUI TTS fallback (offline) ────────────────────────────────────────

        private async Task PlayMauiTtsAsync(string script, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(script)) return;

            CurrentTrack = $"TTS offline ({_language.ToUpper()})";

            var locale = await GetLocaleAsync(_language);
            await Task.Delay(600, token);
            if (token.IsCancellationRequested) return;

            var opts = new SpeechOptions
            {
                Volume = 1.0f,
                Pitch = 1.0f,
                Locale = locale
            };

            await MainThread.InvokeOnMainThreadAsync(async () =>
                await TextToSpeech.Default.SpeakAsync(script, opts, token));
        }

        private static async Task<Locale?> GetLocaleAsync(string lang)
        {
            try
            {
                var locales = await TextToSpeech.Default.GetLocalesAsync();
                return locales.FirstOrDefault(l =>
                    l.Language.StartsWith(lang, StringComparison.OrdinalIgnoreCase))
                    ?? locales.FirstOrDefault(l => 
                    l.Language.StartsWith("vi", StringComparison.OrdinalIgnoreCase));
            }
            catch { return null; }
        }
    }
}
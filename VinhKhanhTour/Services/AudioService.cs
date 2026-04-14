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
        private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private const string GOOGLE_TTS_API_KEY = "AIzaSyAMX0XgjmNv2O4Twk_CBBmjzDwopqtuexE";
        private const string GOOGLE_TTS_URL =
            "https://texttospeech.googleapis.com/v1/text:synthesize?key=" + GOOGLE_TTS_API_KEY;

        // Giọng theo ngôn ngữ — các ngôn ngữ được hỗ trợ sẵn
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

        // Lấy TTS script đúng ngôn ngữ từ model Restaurant
        private static string GetScript(Restaurant poi, string lang) => poi.GetTtsScript();

        // ── Core playback ─────────────────────────────────────────────────────

        private readonly SemaphoreSlim _playSemaphore = new SemaphoreSlim(1, 1);

        public async Task PlayCommentaryAsync(int poiId, string ttsScript, double lat = 0, double lng = 0)
        {
            // Ngắt bài cũ (nếu có)
            await StopAsync();

            await _playSemaphore.WaitAsync();
            try
            {
                _isPlaying = true;
                _currentPoiId = poiId;
                PlaybackStateChanged?.Invoke(true);
                if (poiId > 0) _ = AnalyticsService.RecordPoiVisitAsync(poiId, $"poi_audio_started_{CurrentLanguage}", lat, lng);

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
                    if (poiId > 0) await AnalyticsService.RecordAudioPlayedAsync(poiId, CurrentLanguage, durationSeconds);

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
                try { await TextToSpeech.Default.SpeakAsync("", new()); }
                catch { /* ignore */ }
            }
            await Task.Delay(100);
        }

        public void ResetPoi(int poiId)
        {
            if (_currentPoiId == poiId) _currentPoiId = -1;
        }

        // ── Google Cloud TTS ───────────────────────────────────────────────────

        private async Task PlayGoogleTtsAsync(string script, CancellationToken token)
        {
            // ── FIX: Tách rõ ràng việc resolve voice để tránh voiceName bị sai ──
            string voiceName, langCode, gender;

            if (VoiceMap.TryGetValue(_language, out var v))
            {
                // Ngôn ngữ có sẵn trong bảng → dùng thẳng
                (voiceName, langCode, gender) = v;
            }
            else
            {
                // Ngôn ngữ động (thêm từ CMS) không có trong VoiceMap
                // Tự suy ra langCode theo chuẩn BCP-47, dùng Wavenet-A generic
                if (_language.Length == 2)
                {
                    // Ví dụ: "it" → "it-IT", "pt" → "pt-PT"
                    var upper = _language.ToUpper();
                    langCode = $"{_language}-{upper}";
                    voiceName = $"{_language}-{upper}-Wavenet-A";
                }
                else
                {
                    // Đã là dạng đầy đủ như "pt-BR"
                    langCode = _language;
                    voiceName = $"{_language}-Wavenet-A";
                }
                gender = "FEMALE";
                System.Diagnostics.Debug.WriteLine($"[AudioService] Ngôn ngữ '{_language}' dùng voice generic: {voiceName}");
            }

            CurrentTrack = $"Google TTS ({_language.ToUpper()})";

            byte[] hashBytes = System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes(script + _language));
            string hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();
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
                var json = JsonSerializer.Serialize(requestBody, _jsonOpts);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                try
                {
                    response = await http.PostAsync(GOOGLE_TTS_URL, content, token);
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    // ── FIX: Nếu voice không tồn tại (ngôn ngữ mới) → fallback Maui TTS ──
                    System.Diagnostics.Debug.WriteLine($"[AudioService] Google TTS thất bại cho '{_language}': {ex.Message}. Fallback Maui TTS.");
                    await PlayMauiTtsAsync(script, token);
                    return;
                }

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
            }, token);
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
            }, token);
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
                // ── FIX: Thử match đúng ngôn ngữ trước, fallback vi nếu không có ──
                return locales.FirstOrDefault(l =>
                    l.Language.StartsWith(lang, StringComparison.OrdinalIgnoreCase))
                    ?? locales.FirstOrDefault(l =>
                    l.Language.StartsWith("vi", StringComparison.OrdinalIgnoreCase));
            }
            catch { return null; }
        }
    }
}

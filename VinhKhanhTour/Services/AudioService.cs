using Microsoft.Maui.Media;
using System.Text;
using System.Text.Json;

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
            ["vi"] = ("vi-VN-Wavenet-A",  "vi-VN",  "FEMALE"),
            ["en"] = ("en-US-Wavenet-F",  "en-US",  "FEMALE"),
            ["zh"] = ("cmn-CN-Wavenet-A", "cmn-CN", "FEMALE"),
            ["ja"] = ("ja-JP-Wavenet-D",  "ja-JP",  "FEMALE"),
            ["ko"] = ("ko-KR-Wavenet-A",  "ko-KR",  "FEMALE"),
            ["fr"] = ("fr-FR-Wavenet-C",  "fr-FR",  "FEMALE"),
            ["th"] = ("th-TH-Neural2-C",  "th-TH",  "FEMALE"),
            ["ru"] = ("ru-RU-Wavenet-C",  "ru-RU",  "FEMALE"),
            ["de"] = ("de-DE-Wavenet-F",  "de-DE",  "FEMALE"),
            ["es"] = ("es-ES-Wavenet-C",  "es-ES",  "FEMALE"),
        };

        // ── Ngôn ngữ hiện tại ─────────────────────────────────────────────────
        private string _language = "vi";

        /// <summary>Đặt ngôn ngữ thuyết minh: "vi", "en", "zh", "ja", "ko"...</summary>
        public void SetLanguage(string lang)
        {
            // Chấp nhận bất kì mã ngôn ngữ hợp lệ; fallback vi nếu rỗng
            _language = string.IsNullOrWhiteSpace(lang) ? "vi" : lang.Trim().ToLower();
            System.Diagnostics.Debug.WriteLine($"[AudioService] Language set to: {_language}");
        }

        public string CurrentLanguage => _language;

        // ── Trạng thái ─────────────────────────────────────────────────────────
        private bool _isPlaying;
        private int _currentPoiId = -1;
        private CancellationTokenSource? _ttsCts;

        public string? CurrentTrack { get; private set; }
        public event Action<bool>? PlaybackStateChanged;

        // ── Public API ─────────────────────────────────────────────────────────

        public async Task PlayCommentaryAsync(int poiId, string ttsScript)
        {
            if (_isPlaying && _currentPoiId == poiId) return;

            await StopAsync();

            _isPlaying = true;
            _currentPoiId = poiId;
            PlaybackStateChanged?.Invoke(true);
            _ = VinhKhanhTour.Services.AnalyticsService.Instance.RecordPoiVisitAsync(poiId);

            _ttsCts = new CancellationTokenSource();

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
                System.Diagnostics.Debug.WriteLine($"[AudioService] Google TTS lỗi: {ex.Message}, fallback MAUI");
                try { await PlayMauiTtsAsync(ttsScript, _ttsCts?.Token ?? default); }
                catch (Exception tex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AudioService] MAUI TTS lỗi: {tex.Message}");
                }
            }
            finally
            {
                await VinhKhanhTour.Services.AnalyticsService.Instance.RecordAudioPlayedAsync(_currentPoiId, CurrentLanguage, 0);
                _isPlaying = false;
                CurrentTrack = null;
                PlaybackStateChanged?.Invoke(false);
            }
        }

        public async Task StopAsync()
        {
            _ttsCts?.Cancel();
            _ttsCts = null;

            if (_isPlaying)
            {
                try { await TextToSpeech.Default.SpeakAsync("", new SpeechOptions()); }
                catch { /* ignore */ }
            }

            _isPlaying = false;
            _currentPoiId = -1;
            CurrentTrack = null;
            PlaybackStateChanged?.Invoke(false);
            await Task.Delay(100); // Give time for cancellation to propagate
        }

        public bool IsPlaying => _isPlaying;

        public void ResetPoi(int poiId)
        {
            if (_currentPoiId == poiId)
                _currentPoiId = -1;
        }

        // ── Google Cloud TTS ───────────────────────────────────────────────────

        private async Task PlayGoogleTtsAsync(string script, CancellationToken token)
        {
            // Lấy voice config, fallback sang vi nếu không có mapping riêng
            var (voiceName, langCode, gender) = VoiceMap.TryGetValue(_language, out var v)
                ? v : ("vi-VN-Wavenet-A", "vi-VN", "FEMALE");

            // Nếu là ngôn ngữ không có trong VoiceMap (e.g. 'th' mà chưa có), cố gắng đưa ra ISO code
            if (!VoiceMap.ContainsKey(_language))
            {
                langCode = _language.Length == 2 ? $"{_language}-{_language.ToUpper()}" : _language;
                voiceName = ""; // Dùng giọng mặc định của Google
            }
            CurrentTrack = $"Google TTS ({_language.ToUpper()})";
            System.Diagnostics.Debug.WriteLine($"[AudioService] Google TTS [{_language}]: {script[..Math.Min(60, script.Length)]}...");

            using var md5 = System.Security.Cryptography.MD5.Create();
            var hashBytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(script + _language));
            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            var tempPath = Path.Combine(FileSystem.CacheDirectory, $"gtts_{_currentPoiId}_{_language}_{hashString}.mp3");

            if (!File.Exists(tempPath))
            {
                var requestBody = new
                {
                    input = new { text = script },
                    voice = new
                    {
                        languageCode = langCode,
                        name = voiceName,
                        ssmlGender = gender
                    },
                    audioConfig = new
                    {
                        audioEncoding = "MP3",
                        speakingRate = 0.95,
                        pitch = 0.0
                    }
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

                var audioBytes = Convert.FromBase64String(audioBase64);
                await File.WriteAllBytesAsync(tempPath, audioBytes, token);
            }

            if (!token.IsCancellationRequested)
            {
                await PlayLocalFileAsync(tempPath, token);
            }
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
                    player.Completion += (s, e) => { done.Set(); };
                    
                    using var reg = token.Register(() => 
                    {
                        try { if (player.IsPlaying) player.Stop(); } catch {}
                        done.Set();
                    });

                    done.Wait(TimeSpan.FromMinutes(5));
                }
                catch { }
                finally
                {
                    try { player.Release(); } catch {}
                }
            });
#elif IOS
            return Task.Run(() =>
            {
                var url    = new Foundation.NSUrl(filePath, false);
                using var session = AVFoundation.AVAudioSession.SharedInstance();
                session.SetCategory(AVFoundation.AVAudioSessionCategory.Playback);
                session.SetActive(true);

                var player = AVFoundation.AVAudioPlayer.FromUrl(url);
                if (player == null) return;
                player.PrepareToPlay();
                player.Play();
                
                var done = new System.Threading.ManualResetEventSlim(false);
                using var reg = token.Register(() => 
                {
                    try { player.Stop(); } catch {}
                    done.Set();
                });
                
                while (player.Playing && !token.IsCancellationRequested)
                {
                    System.Threading.Thread.Sleep(100);
                }
                
                try { player.Stop(); player.Dispose(); } catch {}
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
            System.Diagnostics.Debug.WriteLine($"[AudioService] MAUI TTS [{_language}]: {script[..Math.Min(60, script.Length)]}...");

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
            {
                await TextToSpeech.Default.SpeakAsync(script, opts, token);
            });
        }

        private static async Task<Locale?> GetLocaleAsync(string lang)
        {
            try
            {
                var locales = await TextToSpeech.Default.GetLocalesAsync();
                string prefix = lang switch
                {
                    "en" => "en",
                    "zh" => "zh",
                    _ => "vi"
                };
                return locales.FirstOrDefault(l =>
                    l.Language.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            }
            catch { return null; }
        }
    }
}
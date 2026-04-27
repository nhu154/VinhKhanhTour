
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
        private bool _isPaused;
        private int _currentPoiId = -1;
        private CancellationTokenSource? _ttsCts;
        private readonly System.Threading.ManualResetEventSlim _pauseEvent = new(true);

        // ── Resume support: lưu vị trí bị ngắt ──────────────────────────────
        private readonly Dictionary<int, int> _resumeSentenceIndex = new(); // poiId → câu bị ngắt
        private readonly Dictionary<int, string[]> _resumeSentences = new(); // poiId → array câu
        public bool HasResumePosition(int poiId) => _resumeSentenceIndex.ContainsKey(poiId) && _resumeSentenceIndex[poiId] > 0;
        public void ClearResumePosition(int poiId) { _resumeSentenceIndex.Remove(poiId); _resumeSentences.Remove(poiId); }

        public int CurrentPoiId => _currentPoiId;
        public string? CurrentTrack { get; private set; }
        public event Action<bool>? PlaybackStateChanged;
        public bool IsPlaying => _isPlaying;
        public bool IsPaused => _isPaused;

        public void Pause()
        {
            if (_isPlaying && !_isPaused)
            {
                _isPaused = true;
                _pauseEvent.Reset();
                PlaybackStateChanged?.Invoke(_isPlaying);
            }
        }

        public void Resume()
        {
            if (_isPlaying && _isPaused)
            {
                _isPaused = false;
                _pauseEvent.Set();
                PlaybackStateChanged?.Invoke(_isPlaying);
            }
        }

        // ── Hàng đợi audio ────────────────────────────────────────────────────
        // Khi user đi qua nhiều quán liên tiếp, audio xếp hàng phát lần lượt

        /// <summary>Item trong hàng đợi audio</summary>
        public class AudioQueueItem
        {
            public int PoiId { get; set; }
            public string PoiName { get; set; } = "";
            public string TtsScript { get; set; } = "";
            public double Lat { get; set; }
            public double Lng { get; set; }
            public DateTime EnqueuedAt { get; set; } = DateTime.Now;
        }

        private readonly ConcurrentQueue<AudioQueueItem> _audioQueue = new();
        private bool _isQueueProcessing = false;
        private readonly object _queueLock = new();

        /// <summary>Số item đang chờ trong hàng đợi</summary>
        public int QueueCount => _audioQueue.Count;

        /// <summary>Item đang phát hiện tại (null nếu không phát)</summary>
        public AudioQueueItem? CurrentQueueItem { get; private set; }

        /// <summary>Danh sách POI đang chờ trong queue (readonly)</summary>
        public List<AudioQueueItem> GetQueueSnapshot()
            => _audioQueue.ToList();

        /// <summary>Event khi hàng đợi thay đổi (thêm/bớt item)</summary>
        public event Action<int>? QueueChanged;

        // ── Entry point cho LocationForegroundService ─────────────────────────

        /// <summary>
        /// Thêm POI vào hàng đợi audio.
        /// - Nếu không có gì đang phát → phát ngay.
        /// - Nếu đang phát bài khác → xếp hàng chờ, phát khi đến lượt.
        /// - Nếu POI đã có trong queue hoặc đang phát → bỏ qua (chống trùng).
        /// </summary>
        public async Task PlayNarrationAsync(Restaurant poi, double lat = 0, double lng = 0)
        {
            var script = GetScript(poi, _language);

            if (string.IsNullOrWhiteSpace(script))
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] POI {poi.Id} không có script [{_language}], bỏ qua.");
                return;
            }

            // Chống trùng: đang phát POI này rồi
            if (_isPlaying && _currentPoiId == poi.Id) return;

            // Chống trùng: POI đã có trong hàng đợi
            if (_audioQueue.Any(q => q.PoiId == poi.Id))
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] POI {poi.Id} đã có trong queue, bỏ qua.");
                return;
            }

            var item = new AudioQueueItem
            {
                PoiId = poi.Id,
                PoiName = poi.Name,
                TtsScript = script,
                Lat = lat,
                Lng = lng
            };

            _audioQueue.Enqueue(item);
            QueueChanged?.Invoke(_audioQueue.Count);
            System.Diagnostics.Debug.WriteLine(
                $"[AudioService] Queued: {poi.Name} (queue size: {_audioQueue.Count})");

            // Bắt đầu xử lý hàng đợi nếu chưa chạy
            await ProcessQueueAsync();
        }

        /// <summary>
        /// Phát trực tiếp 1 POI — NGẮT bài hiện tại + XÓA hàng đợi.
        /// Dùng khi user bấm nút phát thủ công (không phải tự động geofencing).
        /// </summary>
        public async Task PlayNarrationImmediateAsync(Restaurant poi, double lat = 0, double lng = 0)
        {
            var script = GetScript(poi, _language);
            if (string.IsNullOrWhiteSpace(script)) return;

            // Xóa hết hàng đợi
            ClearQueue();

            // Ngắt bài hiện tại
            if (_isPlaying) await StopAsync();

            // Phát trực tiếp
            await PlayCommentaryAsync(poi.Id, script, lat, lng);
        }

        /// <summary>
        /// Xử lý hàng đợi audio — phát lần lượt FIFO
        /// </summary>
        private async Task ProcessQueueAsync()
        {
            lock (_queueLock)
            {
                if (_isQueueProcessing) return;
                _isQueueProcessing = true;
            }

            try
            {
                while (_audioQueue.TryDequeue(out var item))
                {
                    CurrentQueueItem = item;
                    QueueChanged?.Invoke(_audioQueue.Count);

                    System.Diagnostics.Debug.WriteLine(
                        $"[AudioService] Playing from queue: {item.PoiName} (remaining: {_audioQueue.Count})");

                    await PlayCommentaryAsync(item.PoiId, item.TtsScript, item.Lat, item.Lng);

                    CurrentQueueItem = null;

                    // Khoảng nghỉ ngắn giữa các bài (1 giây)
                    if (_audioQueue.Count > 0)
                        await Task.Delay(1000);
                }
            }
            finally
            {
                lock (_queueLock) { _isQueueProcessing = false; }
                QueueChanged?.Invoke(0);
            }
        }

        /// <summary>Xóa toàn bộ hàng đợi (không ảnh hưởng bài đang phát)</summary>
        public void ClearQueue()
        {
            while (_audioQueue.TryDequeue(out _)) { }
            QueueChanged?.Invoke(0);
            System.Diagnostics.Debug.WriteLine("[AudioService] Queue cleared");
        }

        /// <summary>Bỏ qua bài đang phát → chuyển sang bài tiếp theo trong queue</summary>
        public async Task SkipCurrentAsync()
        {
            if (_isPlaying)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AudioService] Skipping POI {_currentPoiId}");
                await StopAsync();
                // ProcessQueueAsync sẽ tự phát bài tiếp theo
            }
        }

        // Lấy TTS script đúng ngôn ngữ từ model Restaurant
        private static string GetScript(Restaurant poi, string lang) => poi.GetTtsScript();

        // ── Core playback ─────────────────────────────────────────────────────

        private readonly SemaphoreSlim _playSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>Chia script thành mảng câu để hỗ trợ resume</summary>
        private static string[] SplitSentences(string script)
        {
            // Tách theo dấu câu kết thúc, giữ lại dấu câu
            var raw = System.Text.RegularExpressions.Regex.Split(script.Trim(), @"(?<=[.!?。！？])\s+");
            return raw.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        }

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

                // Chia script thành câu, lưu để resume sau này
                var sentences = SplitSentences(ttsScript);
                _resumeSentences[poiId] = sentences;

                // Bắt đầu từ câu bị ngắt (nếu có), hoặc từ đầu
                int startIdx = _resumeSentenceIndex.TryGetValue(poiId, out int saved) ? saved : 0;
                if (startIdx >= sentences.Length) startIdx = 0;
                _resumeSentenceIndex[poiId] = startIdx;

                System.Diagnostics.Debug.WriteLine($"[AudioService] Phát POI {poiId} từ câu {startIdx}/{sentences.Length}");

                try
                {
                    var hasInternet = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

                    for (int i = startIdx; i < sentences.Length; i++)
                    {
                        if (_ttsCts.Token.IsCancellationRequested) break;
                        
                        // ── Chờ nếu đang tạm dừng ──
                        try { _pauseEvent.Wait(_ttsCts.Token); } catch { break; }

                        // Cập nhật index câu hiện tại (để resume nếu bị ngắt)
                        _resumeSentenceIndex[poiId] = i;

                        var sentence = sentences[i];
                        try
                        {
                            if (hasInternet)
                                await PlayGoogleTtsAsync(sentence, _ttsCts.Token);
                            else
                                await PlayMauiTtsAsync(sentence, _ttsCts.Token);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[AudioService] Câu {i} lỗi: {ex.Message}");
                            if (!(_ttsCts?.IsCancellationRequested ?? true))
                            {
                                try { await PlayMauiTtsAsync(sentence, _ttsCts?.Token ?? default); }
                                catch { }
                            }
                        }

                        if (_ttsCts.Token.IsCancellationRequested)
                        {
                            // Bị ngắt giữa chừng → lưu câu tiếp theo để resume
                            _resumeSentenceIndex[poiId] = i + 1 < sentences.Length ? i + 1 : 0;
                            System.Diagnostics.Debug.WriteLine($"[AudioService] Bị ngắt tại câu {i}, resume sẽ bắt đầu từ câu {_resumeSentenceIndex[poiId]}");
                            break;
                        }
                    }

                    // Phát xong hoàn toàn → xóa resume position
                    if (!_ttsCts.Token.IsCancellationRequested)
                        _resumeSentenceIndex.Remove(poiId);
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
            _isPaused = false;
            _pauseEvent.Set();
            _ttsCts?.Cancel();

            if (_isPlaying)
            {
                try { await TextToSpeech.Default.SpeakAsync("", new()); }
                catch { /* ignore */ }
            }
            await Task.Delay(100);
        }

        /// <summary>Dừng hoàn toàn: ngắt bài đang phát + xóa hàng đợi</summary>
        public async Task StopAllAsync()
        {
            ClearQueue();
            await StopAsync();
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
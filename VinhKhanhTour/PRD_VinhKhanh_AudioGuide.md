# PRD – Vĩnh Khánh Audio Guide

## 1. Thông tin tài liệu
- Product: Vinh Khanh Audio Guide (Mobile + Web Admin)
- Phiên bản PRD: 1.0
- Ngày cập nhật: 2026-03-25
- Trạng thái: Draft for implementation
- Owner: Product/Engineering Team

---

## 2. Tóm tắt sản phẩm

Vĩnh Khánh Audio Guide là hệ sinh thái gồm:
- Ứng dụng mobile MAUI cho người dùng cuối để khám phá địa điểm ẩm thực, nghe thuyết minh tự động khi đến gần POI.
- Web Admin ASP.NET Core Razor Pages cho vận hành nội dung, phân quyền hình:
  - Admin Hệ Thống
  - Admin POI (quản lý theo địa điểm được phân quyền)

Mục tiêu là chuẩn hoá trải nghiệm khám phá ẩm thực bằng audio, đồng thời quản trị nội dung hiệu quả, trực quan và hoàn toàn tiếng Việt.

---

## 3. Bài toán cần giải quyết

### 3.1 Vấn đề hiện tại
- Nội dung audio/địa điểm cần quản trị tập trung và phân quyền rõ ràng.
- Người dùng cần được hướng dẫn ẩm thực cho người đến khu vực lần đầu, không biết địa điểm nào ngon.
- Trải nghiệm khám phá phố ẩm thực cần thống nhất ngôn ngữ, đúng định hướng thiết kế.
- Giao diện quản trị cần thống nhất ngôn ngữ, đúng định hướng thiết kế content.

### 3.2 Cơ hội
- Vĩnh Khánh là điểm đến ẩm thực nổi tiếng, chưa có app hướng dẫn chuyên biệt.
- Du khách nước ngoài và người Việt xa quê đang tăng mạnh tại Quận 4.
- Công nghệ GPS + TTS trên mobile đủ trưởng thành để triển khai offline.

---

## 4. Đối tượng người dùng

- Khách tham quan lần đầu đến Vĩnh Khánh.
- Người Việt xa quê muốn khám phá lại ẩm thực quê hương.
- Du khách quốc tế (hỗ trợ EN / ZH).
- Quản trị viên nội dung (upload audio, sửa bản dịch, xem analytics).

---

## 5. Phạm vi sản phẩm

### 5.1 Trong phạm vi (In Scope)
- GPS tracking & Geofence trigger (bán kính 50m, thuật toán Haversine).
- TTS thuyết minh 3 ngôn ngữ (VI / EN / ZH) qua Google Cloud TTS Wavenet.
- Phát audio file offline khi có sẵn trong Resources/Raw.
- SQLite local database (offline-first), version migration tự động.
- Bản đồ Google Maps + chỉ đường đi bộ qua OSRM.
- 4 Tour chủ đề: Ốc / Nướng / Ăn vặt / Đặc sản.
- Trang cá nhân: lịch sử ghé thăm, điểm thưởng, quán yêu thích.
- Analytics: top POI, thời gian nghe trung bình, heatmap GPS (ẩn danh).
- Web Admin CMS: CRUD POI, audio, bản dịch, tour.
- Đăng nhập người dùng (local SQLite auth).

### 5.2 Ngoài phạm vi — v1 (Out of Scope)
- Thanh toán / đặt bàn trực tuyến.
- Backend API server thật (v1 dùng SQLite local seed).
- Bản đồ offline (Mapbox / Here).
- Push notification.

---

## 6. Kiến trúc gợi ý

- **Location + Geofencing:** Theo dõi GPS background và tạo điểm quan tâm (POI) với bán kính geofence.
- **Geofence Engine:** Quyết định khi nào phát thuyết minh (vào vùng / đến gần), có cooldown 300 giây chống lặp.
- **Narration Engine:** Chọn TTS hoặc phát file audio có sẵn, quản lý hàng đợi, chống trùng lặp.
- **Content Layer:** Dữ liệu POI offline (SQLite) + đồng bộ từ server khi có mạng.
- **UI/UX:** Bản đồ (Maps), danh sách POI, cài đặt độ nhạy GPS/bán kính, chọn giọng TTS, tải gói offline.

---

## 7. Luồng hoạt động mẫu

- App tải danh sách POI (lat/lng, bán kính, ưu tiên, nội dung thuyết minh).
- Khi người dùng di chuyển, background service cập nhật vị trí.
- Geofence Engine xác định POI gần nhất / ưu tiên cao nhất trong bán kính → gửi sự kiện.
- Narration Engine kiểm tra trạng thái (đang phát? đã phát trong X phút?) → quyết định phát TTS / Audio.
- Ghi log đã phát, tránh lặp.

---

## 8. Data Models

### Restaurant (POI)
- `Id` – int, PK AutoIncrement
- `Name` – string, tên quán
- `Description` – string, mô tả ngắn
- `Category` – string, phân loại
- `Latitude` / `Longitude` – double, tọa độ GPS
- `Address` – string
- `ImageUrl` – string, tên file ảnh trong Resources/Raw
- `Rating` – double (0.0 – 5.0)
- `OpenHours` – string
- `IsFavorite` – bool
- `AudioFile` – string, tên file MP3 trong Resources/Raw
- `TtsScript` – string, script TTS tiếng Việt
- `TtsScriptEn` – string, script TTS tiếng Anh
- `TtsScriptZh` – string, script TTS tiếng Trung

### Tour
- `Id` – string ("T01", "T02", ...)
- `Name` – string
- `Description` – string
- `Emoji` – string
- `Duration` – string (VD: "45 phút")
- `Rating` – double
- `RestaurantIds` – List<int>, danh sách POI theo thứ tự

### AnalyticsEvent
- `Id` – int, PK AutoIncrement
- `EventType` – string ("listen_start" / "listen_stop" / "geofence_enter")
- `PoiId` – int, FK → Restaurant.Id
- `TimestampTicks` – long, UTC ticks
- `Value` – double, thời gian nghe (giây)
- `Lat` / `Lng` – double, vị trí người dùng lúc sự kiện

### VisitHistory
- `Id` – int, PK AutoIncrement
- `RestaurantId` – int, FK → Restaurant.Id
- `VisitedAt` – DateTime

### User
- `Id` – int, PK AutoIncrement
- `Username` – string, Unique max 100 chars
- `Password` – string (hashed)
- `FullName` – string

---

## 9. Tính năng chi tiết — Mobile App

### WelcomePage (Trang chủ)
- Hero banner: tên "VĨNH KHÁNH", stats (11 quán, 4 tour, 4.5★).
- Chọn ngôn ngữ: 3 nút VI / EN / ZH → cập nhật toàn app.
- Mini Map: WebView Google Maps, hiển thị 11 POI markers.
- Tour Cards: 4 card (Ốc / Nướng / Ăn vặt / Đặc sản), tap → TourDetailPage.
- CTA Button: "Bắt đầu khám phá" → chuyển sang tab MapPage.

### MapPage (Bản đồ)
- Google Maps WebView full-screen, hiển thị POI markers.
- GPS Blue Dot: vị trí người dùng realtime.
- Filter Chips: lọc theo danh mục.
- POI Bottom Card: kéo lên xem danh sách, ảnh thật từ Resources.
- Route Display: đường đi bộ theo OSRM → vẽ polyline.
- Bottom Sheet POI: tap POI → sheet chi tiết (tên, ảnh, rating, nút "Chỉ đường").

### ProfilePage (Cá nhân)
- Avatar + tên "Du khách Vĩnh Khánh".
- Stats Row: quán đã ghé / tour xong / điểm thưởng.
- Lịch sử ghé thăm: 4 địa điểm gần nhất từ VisitHistory.
- Cài đặt & Tiện ích: quán yêu thích / thống kê hành trình / ngôn ngữ thuyết minh.

### AnalyticsPage (Thống kê)
- Tổng quan: lần nghe / POI khác / avg nghe / tour xong.
- Top POI bar chart horizontal.
- Thời gian nghe trung bình bar chart.
- Heatmap GPS: Google Maps với 49+ điểm GPS ẩn danh.
- Nút xóa dữ liệu analytics.

---

## 10. Tính năng chi tiết — Web Admin CMS

### Dashboard
- Stat cards: tổng POI / lượt dùng hôm nay / lượt nghe / tour xong.
- Top POI chart, donut chart phân bổ tour, hour chart.
- Activity feed (log gần đây), mini map POI, heatmap GPS.

### Quản lý POI
- CRUD đầy đủ: tên, tọa độ, bán kính geofence, ưu tiên, danh mục.
- Upload ảnh minh họa (JPG/PNG max 5MB).
- Upload audio file (MP3/WAV/AAC max 10MB).
- Nhập script TTS per ngôn ngữ.

### Quản lý Audio
- Danh sách file audio per POI với waveform preview.
- Badge trạng thái: ✓ Có audio / ✗ Thiếu file.

### Quản lý Bản dịch
- Tab VI / EN / ZH.
- Progress bar % hoàn thành per POI.
- Editor nội dung TTS script per ngôn ngữ.

### Quản lý Tour
- Grid card: ảnh cover, tên, mô tả, số POI, rating, thời lượng.
- Tạo / sửa / xóa tour, cấu hình thứ tự POI.

### Analytics
- Top POI theo thời gian nghe trung bình.
- Tỉ lệ hoàn thành tour.
- Heatmap GPS ẩn danh.
- Xuất CSV.

---

## 11. Framework .NET MAUI (Android / iOS)

- GPS & Background: background service tracking vị trí thông qua dependency service hoặc Essentials (Geolocation).
- iOS: CLLocationManager với quyền always, region monitoring (geofence).
- Geofencing: tự tính khoảng cách bằng Haversine, trigger theo ngưỡng 50m, cooldown 300s.
- TTS / Audio:
  - Google Cloud TTS Wavenet (vi-VN-Wavenet-A / en-US-Wavenet-F / cmn-CN-Wavenet-A).
  - Fallback sang local audio file nếu offline.
- Map: Google Maps JavaScript API qua WebView (full-screen).
- Offline: SQLite (sqlite-net-pcl) + file âm thanh tải trước trong Resources/Raw.
- Routing: OSRM API + polyline decoder thủ công.

---

## 12. Hệ thống quản trị nội dung (CMS)

- Tạo trang web quản lý:
  - POI (CRUD địa điểm)
  - Audio (upload / preview / xóa)
  - Bản dịch (VI / EN / ZH, progress tracking)
  - Lịch sử sử dụng (analytics log, export CSV)
  - Quản lý tour (tạo / sửa / thứ tự POI)

---

## 13. Yêu cầu phi chức năng

- Offline: app hoạt động không cần mạng (TTS offline fallback về local audio).
- Hiệu năng GPS: cập nhật vị trí ≤ 5 giây một lần.
- Độ chính xác Geofence: Haversine ≤ 50m, cooldown 300s chống spam.
- Latency TTS: Google Wavenet phát ≤ 3 giây sau trigger.
- Database: SQLite version migration tự động, seed data 11 POI thật.
- Bảo mật: API Key không commit lên Git (Config.cs trong .gitignore).
- Đa ngôn ngữ: hỗ trợ VI / EN / ZH, fallback về VI.
- Tương thích: Android 21+ (API 21), iOS 11+.

---

## 14. Tech Stack

- Framework: .NET MAUI 8 (Android / iOS)
- UI: XAML + C# code-behind
- Local DB: sqlite-net-pcl + SQLitePCLRaw.bundle_green
- Maps: Google Maps JavaScript API (WebView)
- TTS: Google Cloud Text-to-Speech API (Wavenet)
- Routing: OSRM Open Source Routing Machine
- GPS: MAUI Geolocation (Essentials)
- Analytics: Custom SQLite logging (bảng AnalyticsEvent)
- CMS: HTML5 + CSS3 + Vanilla JS (single file, có thể mở rộng sang ASP.NET Core Razor Pages)

---

## 15. Lộ trình phát triển

### Phase 1 — Core (Hiện tại ✅)
- SQLite offline DB + seed 11 POI thật với tọa độ GPS thực.
- GPS tracking + Geofence (Haversine 50m, cooldown 300s).
- Google Cloud TTS (VI / EN / ZH Wavenet).
- Google Maps WebView + POI markers + routing OSRM.
- 4 Tab navigation (Home / Map / Profile / Thống kê).
- 4 Tour chủ đề.
- Analytics tracking local.

### Phase 2 — Enhancement (Kế hoạch)
- Web Admin CMS hoàn chỉnh với backend thật.
- Backend API (ASP.NET Core Razor Pages).
- Đồng bộ nội dung từ server khi có mạng.
- Login / auth đa người dùng.
- Favorite restaurants.

### Phase 3 — Scale (Tương lai)
- Bản đồ offline (Mapbox).
- Push notification khi gần POI.
- Rating / review người dùng.
- Mở rộng sang khu vực ẩm thực khác.

---

## 16. Rủi ro & Giảm thiểu

- Google Maps API bị chặn / hết quota → cache tile, fallback OpenStreetMap.
- Google Cloud TTS latency cao → pre-cache audio file offline.
- GPS không chính xác trong hẻm → tăng bán kính geofence lên 80m nếu cần.
- API Key lộ lên GitHub → Config.cs trong .gitignore, dùng Config.example.cs.
- SQLite migration mất dữ liệu → version-based migration, backup trước drop table.

---

## Phụ lục — Danh sách 11 POI thực tế

- P01 – Ốc Oanh: 10.760825, 106.703313 – Ốc – 4.3★
- P02 – Ốc Sáu Nở: 10.761090, 106.702899 – Ốc – 4.4★
- P03 – Ốc Thảo: 10.761758, 106.702358 – Ốc – 4.2★
- P04 – Lãng Quán: 10.761281, 106.705373 – Ăn vặt
- P05 – Chili Lẩu Nướng: 10.761345, 106.705690 – Nướng
- P06 – Ốc Xiêm Quan: 10.761060, 106.706682 – Ốc
- P07 – Bò Lá Lốt Cút: 10.760840, 106.704050 – Nướng
- P08 – Bún Cá Châu Đốc: 10.764267, 106.701181 – Đặc sản
- P09 – Cơm Chay Khô Quẹt: 10.760625, 106.703716 – Ăn vặt
- P10 – Bún Thịt Nướng Conga: 10.761278, 106.705293 – Nướng
- P11 – Thế Giới Bò: 10.760883, 106.706741 – Nướng – 4.5★


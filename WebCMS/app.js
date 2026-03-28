const API = 'http://localhost:5256/api', BASE_URL = 'http://localhost:5256';
let map, markers = [], allPois = [], tours = [], historyData = [];
let mainChart, pieChart;
let selectedPois = [];
let currentFilter = 'all';

// ══ INIT ══
document.addEventListener('DOMContentLoaded', () => {
  initApp();
  setupAccordions();
  setupHistorySearch();
});

async function initApp() {
  showSkeleton();
  await Promise.all([loadPois(), loadTours(), loadAnalytics(), loadStats()]);
  lucide.createIcons();
  switchPage('page-dashboard', document.getElementById('menu-dashboard'));
}

function showSkeleton() {
  const stats = document.getElementById('dashboard-stats');
  if (stats) stats.innerHTML = Array(4).fill(`
    <div class="stat-card">
      <div class="skeleton" style="width:48px;height:48px;border-radius:12px"></div>
      <div style="flex:1">
        <div class="skeleton" style="height:12px;width:60%;margin-bottom:8px"></div>
        <div class="skeleton" style="height:28px;width:40%"></div>
      </div>
    </div>`).join('');
}

function setupAccordions() {
  document.querySelectorAll('.section-title').forEach(title => {
    title.addEventListener('click', () => title.parentElement.classList.toggle('closed'));
  });
}

function setupHistorySearch() {
  const inp = document.getElementById('history-search');
  if (inp) inp.addEventListener('input', e => filterHistory(e.target.value));
}

// ══ PAGE NAVIGATION ══
function switchPage(pageId, navEl) {
  document.querySelectorAll('.main-content').forEach(p => p.classList.remove('active'));
  const target = document.getElementById(pageId);
  if (target) target.classList.add('active');

  document.querySelectorAll('.menu-item').forEach(m => m.classList.remove('active'));
  if (navEl) navEl.classList.add('active');

  const titleMap = {
    'page-dashboard': 'Tổng quan hệ thống',
    'page-poi': 'Bản đồ & Quản lý điểm',
    'page-tour': 'Quản lý hành trình Tour',
    'page-audio': 'Quản lý âm thanh thuyết minh',
    'page-trans': 'Quản lý nội dung đa ngữ',
    'page-history': 'Nhật ký hành trình khách hàng'
  };
  document.getElementById('current-page-title').textContent = titleMap[pageId] || 'Dashboard';

  if (pageId === 'page-poi') setTimeout(() => { if (map) google.maps.event.trigger(map, 'resize'); }, 300);
  if (pageId === 'page-audio') renderAudio();
  if (pageId === 'page-trans') renderTrans();
  if (pageId === 'page-history') renderHistory();
  if (pageId === 'page-dashboard') { loadStats().then(() => { renderStatsCards(); initCharts(); renderDashboardRecent(); }); }
  if (pageId === 'page-tour') renderTours();
}

// ══ GOOGLE MAPS ══
function initMap() {
  map = new google.maps.Map(document.getElementById('google-map'), {
    center: { lat: 10.7615, lng: 106.7033 }, zoom: 17,
    mapTypeControl: false, streetViewControl: false, fullscreenControl: false,
    styles: [{ featureType: 'poi', elementType: 'labels', stylers: [{ visibility: 'off' }] }]
  });
  map.addListener('click', e => { closeMapDetail(); openNewPoiForm(e.latLng.lat(), e.latLng.lng()); });
  if (allPois.length) renderMarkers();
}

function renderMarkers() {
  markers.forEach(m => m.setMap(null)); markers = [];
  allPois.forEach(p => {
    const isMajor = (p.category || p.Category || '') === 'Quán ăn';
    const m = new google.maps.Marker({
      position: { lat: p.latitude || p.Latitude, lng: p.longitude || p.Longitude },
      map,
      icon: {
        url: `https://maps.google.com/mapfiles/ms/icons/${isMajor ? 'red' : 'green'}-dot.png`,
        scaledSize: new google.maps.Size(36, 36)
      },
      title: p.name || p.Name
    });
    m.addListener('click', () => { closePoiForm(); showMapDetail(p); map.panTo(m.getPosition()); });
    markers.push(m);
  });
  renderPoiCarousel();
}

function renderPoiCarousel() {
  const el = document.getElementById('poi-carousel'); if (!el) return;
  el.innerHTML = allPois.map(p => `
    <div class="poi-mini-card" onclick="map.panTo({lat:${p.latitude||p.Latitude},lng:${p.longitude||p.Longitude}});showMapDetail(${JSON.stringify(p).replace(/"/g,'&quot;')})">
      <img src="${getImgUrl(p)}" onerror="this.src='https://via.placeholder.com/260x130/f1f5f9/94a3b8?text=No+Image'">
      <div class="poi-mini-info">
        <h4>${p.name || p.Name}</h4>
        <div class="meta">
          <span class="rating">⭐ ${(p.rating || p.Rating || 0).toFixed(1)}</span>
          <span class="badge badge-info" style="font-size:10px">${p.category || p.Category || 'Quán ăn'}</span>
        </div>
        <small>🕒 ${p.openHours || p.OpenHours || 'Chưa cập nhật'}</small>
      </div>
    </div>`).join('');
}

function showMapDetail(poi) {
  const panel = document.getElementById('map-detail-panel');
  document.getElementById('map-detail-title').innerHTML = `${poi.name || poi.Name}`;
  document.getElementById('map-detail-addr').innerHTML = `📍 ${poi.address || poi.Address || 'Chưa có địa chỉ'} &nbsp;·&nbsp; ⭐ ${(poi.rating || poi.Rating || 0).toFixed(1)}`;
  document.getElementById('map-detail-desc').textContent = poi.description || poi.Description || 'Chưa có mô tả.';
  document.getElementById('map-detail-img').src = getImgUrl(poi);
  document.getElementById('map-detail-img').onerror = function(){ this.src='https://via.placeholder.com/360x150/f1f5f9/94a3b8?text=No+Image'; };
  document.getElementById('btn-edit-from-map').onclick = () => openEditPoiForm(poi);
  panel.style.display = 'flex';
  lucide.createIcons();
}
function closeMapDetail() { document.getElementById('map-detail-panel').style.display = 'none'; }

// ══ POI CRUD ══
async function loadPois() {
  try {
    const res = await fetch(`${API}/restaurants`);
    allPois = await res.json();
    renderMarkers();
    updatePOIBadge();
  } catch(e) { console.error('loadPois:', e); }
}

function updatePOIBadge() {
  const b = document.getElementById('poi-badge');
  if (b) b.textContent = allPois.length;
}

function openNewPoiForm(lat, lng) {
  resetPoiForm();
  document.getElementById('poi-form-title').textContent = 'Tạo điểm mới';
  const center = map ? map.getCenter() : { lat: () => 10.7615, lng: () => 106.7033 };
  document.getElementById('poi-lat').value = (lat || center.lat()).toFixed(7);
  document.getElementById('poi-lng').value = (lng || center.lng()).toFixed(7);
  document.getElementById('btn-save-poi').innerHTML = '<i data-lucide="plus"></i> Tạo điểm mới';
  document.getElementById('btn-delete-poi').style.display = 'none';
  document.getElementById('poi-form-panel').classList.add('open');
  document.getElementById('panel-overlay-poi').style.display = 'block';
  lucide.createIcons();
}

function openEditPoiForm(poi) {
  resetPoiForm();
  const p = poi;
  document.getElementById('poi-form-title').textContent = 'Chỉnh sửa điểm';
  document.getElementById('poi-id').value = p.id || p.Id;
  document.getElementById('poi-lat').value = (p.latitude || p.Latitude || 0).toFixed(7);
  document.getElementById('poi-lng').value = (p.longitude || p.Longitude || 0).toFixed(7);
  document.getElementById('poi-radius').value = p.radius || p.Radius || 50;
  document.getElementById('poi-category').value = p.category || p.Category || 'Quán ăn';
  document.getElementById('poi-rating').value = p.rating || p.Rating || 4.0;
  document.getElementById('poi-hours').value = p.openHours || p.OpenHours || '';
  document.getElementById('poi-address').value = p.address || p.Address || '';
  document.getElementById('poi-audio-url').value = p.audioUrl || p.AudioUrl || p.audioFile || p.AudioFile || '';

  document.getElementById('poi-name-vi').value = p.name || p.Name || '';
  document.getElementById('poi-desc-vi').value = p.ttsScript || p.TtsScript || p.description || p.Description || '';
  document.getElementById('poi-name-en').value = '';
  document.getElementById('poi-desc-en').value = p.ttsScriptEn || p.TtsScriptEn || '';
  document.getElementById('poi-name-zh').value = '';
  document.getElementById('poi-desc-zh').value = p.ttsScriptZh || p.TtsScriptZh || '';

  document.getElementById('btn-save-poi').innerHTML = '<i data-lucide="save"></i> Lưu thay đổi';
  document.getElementById('btn-delete-poi').style.display = 'flex';
  document.getElementById('poi-form-panel').classList.add('open');
  document.getElementById('panel-overlay-poi').style.display = 'block';
  lucide.createIcons();
}

async function savePoiData() {
  const id = document.getElementById('poi-id').value;
  const name = document.getElementById('poi-name-vi').value.trim();
  if (!name) { showToast('Vui lòng nhập tên điểm (Tiếng Việt)', 'warning'); return; }

  const btn = document.getElementById('btn-save-poi');
  btn.innerHTML = '<i data-lucide="loader-2" class="spin"></i> Đang lưu...';
  btn.disabled = true;

  const body = {
    Name: name,
    Description: document.getElementById('poi-desc-vi').value,
    Category: document.getElementById('poi-category').value,
    Latitude: parseFloat(document.getElementById('poi-lat').value) || 0,
    Longitude: parseFloat(document.getElementById('poi-lng').value) || 0,
    Rating: parseFloat(document.getElementById('poi-rating').value) || 4.0,
    OpenHours: document.getElementById('poi-hours').value,
    Address: document.getElementById('poi-address').value || 'Vĩnh Khánh, Phường 8, Quận 4',
    Radius: parseInt(document.getElementById('poi-radius').value) || 50,
    AudioFile: document.getElementById('poi-audio-url').value,
    AudioUrl: document.getElementById('poi-audio-url').value,
    TtsScript: document.getElementById('poi-desc-vi').value,
    TtsScriptEn: document.getElementById('poi-desc-en').value,
    TtsScriptZh: document.getElementById('poi-desc-zh').value,
    IsFavorite: false
  };

  try {
    const method = id ? 'PUT' : 'POST';
    const url = id ? `${API}/restaurants/${id}` : `${API}/restaurants`;
    const res = await fetch(url, { method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
    if (res.ok) {
      showToast(id ? '✅ Cập nhật thành công!' : '✅ Tạo điểm mới thành công!', 'success');
      closePoiForm();
      await loadPois();
      renderStatsCards();
    }
  } catch(e) {
    showToast('❌ Lỗi khi lưu dữ liệu', 'danger');
  } finally {
    btn.innerHTML = id ? '<i data-lucide="save"></i> Lưu thay đổi' : '<i data-lucide="plus"></i> Tạo điểm mới';
    btn.disabled = false;
    lucide.createIcons();
  }
}

async function deletePoiData() {
  const id = document.getElementById('poi-id').value;
  const name = document.getElementById('poi-name-vi').value;
  if (!id) return;
  if (!confirm(`⚠️ Xóa "${name}"?\nThao tác này không thể hoàn tác.`)) return;
  try {
    const res = await fetch(`${API}/restaurants/${id}`, { method: 'DELETE' });
    if (res.ok) { showToast('🗑️ Đã xóa thành công', 'success'); closePoiForm(); await loadPois(); renderStatsCards(); }
  } catch(e) { showToast('❌ Lỗi khi xóa', 'danger'); }
}

function resetPoiForm() {
  ['poi-id','poi-lat','poi-lng','poi-audio-url','poi-address','poi-hours'].forEach(id => { const el = document.getElementById(id); if(el) el.value = ''; });
  document.getElementById('poi-radius').value = 50;
  document.getElementById('poi-rating').value = 4.0;
  document.getElementById('poi-category').value = 'Quán ăn';
  ['vi','en','zh'].forEach(lang => {
    const n = document.getElementById(`poi-name-${lang}`);
    const d = document.getElementById(`poi-desc-${lang}`);
    if(n) n.value = ''; if(d) d.value = '';
  });
}
function closePoiForm() {
  document.getElementById('poi-form-panel').classList.remove('open');
  document.getElementById('panel-overlay-poi').style.display = 'none';
}

// ══ TOUR CRUD ══
async function loadTours() {
  try {
    const res = await fetch(`${API}/tours`);
    tours = await res.json();
  } catch(e) { tours = []; }
}

function renderTours() {
  const el = document.getElementById('tour-grid'); if (!el) return;
  if (!tours.length) {
    el.innerHTML = `<div class="empty-state" style="grid-column:1/-1"><i data-lucide="map"></i><p>Chưa có tour nào. Nhấn "+ Tạo Tour mới" để bắt đầu!</p></div>`;
    lucide.createIcons(); return;
  }
  el.innerHTML = tours.map((t, idx) => {
    const ps = (() => { try { return typeof t.Pois === 'string' ? JSON.parse(t.Pois||'[]') : (t.Pois||[]); } catch { return []; } })();
    const emoji = t.Emoji || t.emoji || '🍜';
    const rating = t.Rating || t.rating || 4.0;
    const duration = t.Duration || t.duration || '';
    const nameEn = t.NameEn || t.nameEn || '';
    const nameZh = t.NameZh || t.nameZh || '';
    return `
    <div class="tour-card">
      <div style="position:relative">
        <img class="tour-card-img" src="${t.ImageUrl||t.img||'https://via.placeholder.com/400x160/f1f5f9/94a3b8?text=Tour'}" onerror="this.src='https://via.placeholder.com/400x160/f1f5f9/94a3b8?text=Tour'">
        <div style="position:absolute;top:12px;left:12px;font-size:28px">${emoji}</div>
        <div style="position:absolute;top:12px;right:12px;background:rgba(0,0,0,0.6);color:#fbbf24;font-size:12px;font-weight:700;padding:4px 10px;border-radius:20px">⭐ ${Number(rating).toFixed(1)}</div>
      </div>
      <div class="tour-card-body">
        <div class="tour-card-title">${t.Name || t.name || 'Tour'}</div>
        ${nameEn ? `<div style="font-size:11px;color:var(--text-muted);margin:-4px 0 6px">🇬🇧 ${nameEn}${nameZh ? ' &nbsp;·&nbsp; 🇨🇳 ' + nameZh : ''}</div>` : ''}
        <div class="tour-card-desc">${t.Description || t.desc || ''}</div>
        <div class="tour-card-meta">
          <div style="display:flex;gap:8px;flex-wrap:wrap">
            <span class="badge badge-info">📍 ${ps.length} điểm</span>
            ${duration ? `<span class="badge badge-neutral">⏱ ${duration}</span>` : ''}
          </div>
          <div style="display:flex;gap:6px">
            <button class="btn btn-ghost btn-sm" onclick="editTour(${idx})"><i data-lucide="edit-3"></i></button>
            <button class="btn btn-ghost btn-sm" onclick="deleteTour(${t.Id||t.id})" style="color:var(--danger)"><i data-lucide="trash-2"></i></button>
          </div>
        </div>
      </div>
    </div>`;
  }).join('');
  lucide.createIcons();
}

function showTourModal() {
  resetTourForm();
  renderPoiChecklist();
  document.getElementById('tour-form-title').textContent = '🗺️ Tạo hành trình mới';
  document.getElementById('tour-form-panel').classList.add('open');
  document.getElementById('panel-overlay-tour').style.display = 'block';
  lucide.createIcons();
}

function editTour(idx) {
  const t = tours[idx]; resetTourForm();
  document.getElementById('tour-id').value = t.Id || t.id || '';
  document.getElementById('tour-name').value = t.Name || t.name || '';
  document.getElementById('tour-name-en').value = t.NameEn || t.nameEn || '';
  document.getElementById('tour-name-zh').value = t.NameZh || t.nameZh || '';
  document.getElementById('tour-desc').value = t.Description || t.desc || '';
  document.getElementById('tour-desc-en').value = t.DescEn || t.descEn || '';
  document.getElementById('tour-desc-zh').value = t.DescZh || t.descZh || '';
  document.getElementById('tour-duration').value = t.Duration || t.duration || '';
  document.getElementById('tour-rating').value = t.Rating || t.rating || 4.0;
  document.getElementById('tour-emoji').value = t.Emoji || t.emoji || '🍜';
  document.getElementById('tour-img').value = t.ImageUrl || t.img || '';
  try { selectedPois = typeof t.Pois === 'string' ? JSON.parse(t.Pois||'[]') : (t.Pois||[]); } catch { selectedPois = []; }
  document.getElementById('tour-form-title').textContent = '✏️ Chỉnh sửa Tour';
  renderPoiChecklist();
  document.getElementById('tour-form-panel').classList.add('open');
  document.getElementById('panel-overlay-tour').style.display = 'block';
  lucide.createIcons();
}

function selectPoiInTour(id) {
  const idx = selectedPois.indexOf(id);
  if (idx > -1) selectedPois.splice(idx, 1); else selectedPois.push(id);
  renderPoiChecklist();
}

function renderPoiChecklist() {
  const el = document.getElementById('tour-poi-checklist'); if (!el) return;
  document.getElementById('selected-poi-count').textContent = selectedPois.length;
  el.innerHTML = allPois.map(p => {
    const pId = p.id || p.Id, ord = selectedPois.indexOf(pId) + 1;
    return `
    <div class="poi-check-item ${ord > 0 ? 'selected' : ''}" onclick="selectPoiInTour(${pId})">
      <div class="poi-check-num">${ord > 0 ? ord : ''}</div>
      <span style="font-size:13px;font-weight:500">${p.name || p.Name}</span>
    </div>`;
  }).join('');
}

async function saveTourData() {
  const id = document.getElementById('tour-id').value;
  const name = document.getElementById('tour-name').value.trim();
  if (!name || selectedPois.length === 0) { showToast('Nhập tên tour và chọn ít nhất 1 điểm', 'warning'); return; }
  const body = {
    Name:        name,
    NameEn:      document.getElementById('tour-name-en').value.trim(),
    NameZh:      document.getElementById('tour-name-zh').value.trim(),
    Description: document.getElementById('tour-desc').value,
    DescEn:      document.getElementById('tour-desc-en').value,
    DescZh:      document.getElementById('tour-desc-zh').value,
    Duration:    document.getElementById('tour-duration').value,
    Rating:      parseFloat(document.getElementById('tour-rating').value) || 4.0,
    Emoji:       document.getElementById('tour-emoji').value || '🍜',
    ImageUrl:    document.getElementById('tour-img').value,
    Pois:        JSON.stringify(selectedPois),
    IsActive:    true
  };
  try {
    const method = id ? 'PUT' : 'POST', url = id ? `${API}/tours/${id}` : `${API}/tours`;
    await fetch(url, { method, headers: {'Content-Type':'application/json'}, body: JSON.stringify(body) });
    showToast('✅ Đã lưu Tour!', 'success'); closeTourModal(); await loadTours(); renderTours(); renderStatsCards();
  } catch(e) { showToast('❌ Lỗi khi lưu Tour', 'danger'); }
}

async function deleteTour(id) {
  if (!confirm('Xóa tour này?')) return;
  try {
    await fetch(`${API}/tours/${id}`, { method: 'DELETE' });
    showToast('🗑️ Đã xóa Tour', 'success'); await loadTours(); renderTours(); renderStatsCards();
  } catch(e) { showToast('❌ Lỗi khi xóa', 'danger'); }
}

function closeTourModal() { document.getElementById('tour-form-panel').classList.remove('open'); document.getElementById('panel-overlay-tour').style.display = 'none'; }
function resetTourForm() { ['tour-id','tour-name','tour-name-en','tour-name-zh','tour-desc','tour-desc-en','tour-desc-zh','tour-duration','tour-img','tour-emoji'].forEach(id => { const el = document.getElementById(id); if(el) el.value = ''; }); const r = document.getElementById('tour-rating'); if(r) r.value = 4.0; selectedPois = []; }

// ══ ANALYTICS ══
async function loadAnalytics() {
  try {
    const res = await fetch(`${API}/analytics`);
    historyData = await res.json();
    renderStatsCards(); renderHistory(); renderDashboardRecent();
  } catch(e) { console.error('loadAnalytics:', e); }
}

function renderStatsCards() {
  const el = document.getElementById('dashboard-stats'); if (!el) return;
  const totalPois = allPois.length;
  const totalTours = tours.length;
  const totalVisits = statsData?.totalVisits ?? historyData.length;
  const todayVisits = statsData?.todayVisits ?? historyData.filter(h => new Date(h.Timestamp||h.timestamp).toDateString() === new Date().toDateString()).length;
  const weekVisits = statsData?.weekVisits ?? 0;
  const majorPois = allPois.filter(p => (p.category || p.Category) === 'Quán ăn').length;

  el.innerHTML = `
    <div class="stat-card">
      <div class="stat-icon blue"><i data-lucide="map-pin"></i></div>
      <div class="stat-info">
        <p class="text-muted">Tổng POI</p>
        <h2 class="stat-val">${totalPois}</h2>
        <div class="stat-trend up">↑ ${majorPois} quán ăn</div>
      </div>
    </div>
    <div class="stat-card">
      <div class="stat-icon green"><i data-lucide="share-2"></i></div>
      <div class="stat-info">
        <p class="text-muted">Hành trình Tour</p>
        <h2 class="stat-val">${totalTours}</h2>
        <div class="stat-trend" style="color:var(--text-muted)">Tuyến tham quan</div>
      </div>
    </div>
    <div class="stat-card">
      <div class="stat-icon orange"><i data-lucide="activity"></i></div>
      <div class="stat-info">
        <p class="text-muted">Lượt ghé thăm</p>
        <h2 class="stat-val">${totalVisits}</h2>
        <div class="stat-trend up">↑ ${todayVisits} hôm nay · ${weekVisits} tuần này</div>
      </div>
    </div>
    <div class="stat-card">
      <div class="stat-icon red"><i data-lucide="utensils"></i></div>
      <div class="stat-info">
        <p class="text-muted">Quán ăn chính</p>
        <h2 class="stat-val">${majorPois}</h2>
        <div class="stat-trend" style="color:var(--text-muted)">/ ${totalPois} tổng POI</div>
      </div>
    </div>`;
  lucide.createIcons();
}

function renderHistory(filter = '') {
  const tbody = document.getElementById('history-tbody'); if (!tbody) return;
  let data = historyData;
  if (currentFilter !== 'all') data = data.filter(h => (h.EventType || h.eventType || '').toLowerCase() === currentFilter);
  if (filter) data = data.filter(h => (h.RestaurantName || h.restaurantName || '').toLowerCase().includes(filter.toLowerCase()));

  if (!data.length) {
    tbody.innerHTML = `<tr><td colspan="5" style="text-align:center;padding:40px;color:var(--text-muted)">Chưa có dữ liệu</td></tr>`;
    return;
  }
  tbody.innerHTML = data.map((h, i) => {
    const evt = h.EventType || h.eventType || 'visit';
    const name = h.RestaurantName || h.restaurantName || '—';
    const time = new Date(h.Timestamp || h.timestamp).toLocaleString('vi-VN');
    const badgeClass = evt.toLowerCase().includes('enter') ? 'badge-success' : evt.toLowerCase().includes('exit') ? 'badge-warning' : 'badge-info';
    return `
    <tr>
      <td><div style="display:flex;align-items:center;gap:8px"><div style="width:8px;height:8px;border-radius:50%;background:#22c55e;flex-shrink:0"></div><strong>${name}</strong></div></td>
      <td><span class="badge ${badgeClass}">${evt.toUpperCase()}</span></td>
      <td style="font-family:monospace;font-size:11px;color:var(--text-muted)">POI #${h.RestaurantId || h.restaurantId || '?'}</td>
      <td style="font-size:12px;color:var(--text-muted)">${time}</td>
      <td><span style="color:#16a34a;font-size:12px;font-weight:600">● Completed</span></td>
    </tr>`;
  }).join('');
}

function filterHistory(val) { renderHistory(val); }

function setFilter(f, el) {
  currentFilter = f;
  document.querySelectorAll('.filter-chip').forEach(c => c.classList.remove('active'));
  el.classList.add('active');
  renderHistory(document.getElementById('history-search')?.value || '');
}

function renderDashboardRecent() {
  const tbody = document.getElementById('dashboard-recent-tbody'); if (!tbody) return;
  tbody.innerHTML = historyData.slice(0, 5).map(h => `
    <tr>
      <td><strong>${h.RestaurantName || h.restaurantName || 'POI'}</strong></td>
      <td><span class="badge badge-success" style="font-size:10px">${h.EventType || h.eventType || 'visit'}</span></td>
      <td style="color:var(--text-muted);font-size:12px">${new Date(h.Timestamp || h.timestamp).toLocaleTimeString('vi-VN')}</td>
    </tr>`).join('') || `<tr><td colspan="3" style="text-align:center;color:var(--text-muted);padding:20px">Chưa có hoạt động</td></tr>`;
}

let statsData = null;

async function loadStats() {
  try {
    const res = await fetch(`${API}/analytics/stats`);
    statsData = await res.json();
  } catch(e) { statsData = null; }
}

function initCharts() {
  const ctx = document.getElementById('mainChart')?.getContext('2d');
  const ctxPie = document.getElementById('pieChart')?.getContext('2d');
  if (!ctx || !ctxPie) return;
  if (mainChart) mainChart.destroy();
  if (pieChart) pieChart.destroy();

  // Line chart: visits by day (7 days) from stats endpoint
  let chartLabels = [], chartData = [];
  if (statsData?.byDay?.length > 0) {
    statsData.byDay.forEach(d => {
      const date = new Date(d.Day || d.day);
      chartLabels.push(date.toLocaleDateString('vi-VN', { month:'short', day:'numeric' }));
      chartData.push(d.Count || d.count || 0);
    });
  } else {
    // Fallback: group by hour from historyData
    const hourCounts = {};
    historyData.forEach(h => {
      const hour = new Date(h.Timestamp || h.timestamp).getHours();
      hourCounts[hour] = (hourCounts[hour] || 0) + 1;
    });
    Object.keys(hourCounts).forEach(h => { chartLabels.push(`${h}:00`); chartData.push(hourCounts[h]); });
    if (!chartLabels.length) { chartLabels.push('Chưa có data'); chartData.push(0); }
  }

  mainChart = new Chart(ctx, {
    type: 'bar',
    data: {
      labels: chartLabels,
      datasets: [{
        label: 'Lượt ghé thăm', data: chartData,
        backgroundColor: 'rgba(37,99,235,0.8)',
        borderRadius: 6, borderSkipped: false
      }]
    },
    options: {
      responsive: true, maintainAspectRatio: false,
      plugins: { legend: { display: false } },
      scales: {
        y: { beginAtZero: true, grid: { color: '#f1f5f9' }, ticks: { stepSize: 1 } },
        x: { grid: { display: false } }
      }
    }
  });

  // Doughnut chart: top POI visits
  let topLabels = [], topValues = [];
  if (statsData?.topPoi?.length > 0) {
    statsData.topPoi.forEach(p => {
      topLabels.push(p.Name || p.name || '?');
      topValues.push(p.VisitCount || p.visitCount || 0);
    });
  } else {
    const rCounts = {};
    historyData.forEach(h => { const n = h.RestaurantName || h.restaurantName || 'Khác'; rCounts[n] = (rCounts[n]||0)+1; });
    const top = Object.entries(rCounts).sort((a,b)=>b[1]-a[1]).slice(0,5);
    top.forEach(([n,v]) => { topLabels.push(n); topValues.push(v); });
  }
  if (!topLabels.length) { topLabels.push('Chưa có data'); topValues.push(1); }

  pieChart = new Chart(ctxPie, {
    type: 'doughnut',
    data: {
      labels: topLabels,
      datasets: [{ data: topValues, backgroundColor: ['#2563eb','#10b981','#f59e0b','#ef4444','#8b5cf6'], borderWidth: 0, hoverOffset: 4 }]
    },
    options: { responsive: true, maintainAspectRatio: false, cutout: '65%', plugins: { legend: { position: 'bottom', labels: { font: { size: 11 }, padding: 12 } } } }
  });
}

// ══ AUDIO ══
function renderAudio() {
  const el = document.getElementById('audio-list'); if (!el) return;
  if (!allPois.length) { el.innerHTML = `<div class="empty-state"><i data-lucide="music"></i><p>Chưa có POI nào</p></div>`; lucide.createIcons(); return; }
  el.innerHTML = allPois.map(p => {
    const hasVi = !!(p.ttsScript || p.TtsScript);
    const hasEn = !!(p.ttsScriptEn || p.TtsScriptEn);
    const hasZh = !!(p.ttsScriptZh || p.TtsScriptZh);
    const hasAudio = !!(p.audioFile || p.AudioFile || p.audioUrl || p.AudioUrl);
    return `
    <div class="audio-item">
      <div class="audio-icon"><i data-lucide="music-2"></i></div>
      <div style="flex:1;min-width:0">
        <div style="font-weight:700;font-size:14px">${p.name || p.Name}</div>
        <div class="audio-langs">
          <span class="badge ${hasVi ? 'badge-success' : 'badge-neutral'}">🇻🇳 ${hasVi ? 'Có TTS' : 'Thiếu'}</span>
          <span class="badge ${hasEn ? 'badge-success' : 'badge-neutral'}">🇬🇧 ${hasEn ? 'Có TTS' : 'Thiếu'}</span>
          <span class="badge ${hasZh ? 'badge-success' : 'badge-neutral'}">🇨🇳 ${hasZh ? 'Có TTS' : 'Thiếu'}</span>
          <span class="badge ${hasAudio ? 'badge-info' : 'badge-warning'}">${hasAudio ? '🎵 Audio' : '⚠️ Thiếu audio'}</span>
        </div>
      </div>
      <button class="btn btn-ghost btn-sm" onclick="openEditPoiForm(${JSON.stringify(p).replace(/"/g,'&quot;')})"><i data-lucide="edit-3"></i> Sửa</button>
    </div>`;
  }).join('');
  lucide.createIcons();
}

// ══ TRANSLATION ══
function renderTrans() {
  const tbody = document.getElementById('trans-tbody'); if (!tbody) return;
  if (!allPois.length) { tbody.innerHTML = `<tr><td colspan="5" style="text-align:center;padding:40px;color:var(--text-muted)">Chưa có dữ liệu</td></tr>`; return; }
  tbody.innerHTML = allPois.map(p => {
    const hasVi = !!(p.ttsScript || p.TtsScript);
    const hasEn = !!(p.ttsScriptEn || p.TtsScriptEn);
    const hasZh = !!(p.ttsScriptZh || p.TtsScriptZh);
    return `
    <tr>
      <td><div style="display:flex;align-items:center;gap:10px"><img class="thumb" src="${getImgUrl(p)}" onerror="this.src='https://via.placeholder.com/42'"><strong>${p.name || p.Name}</strong></div></td>
      <td><span class="badge ${hasVi ? 'badge-success' : 'badge-danger'}">${hasVi ? '✓ Có' : '✗ Thiếu'}</span></td>
      <td><span class="badge ${hasEn ? 'badge-success' : 'badge-danger'}">${hasEn ? '✓ Có' : '✗ Thiếu'}</span></td>
      <td><span class="badge ${hasZh ? 'badge-success' : 'badge-danger'}">${hasZh ? '✓ Có' : '✗ Thiếu'}</span></td>
      <td><button class="btn btn-ghost btn-sm" onclick="openEditPoiForm(${JSON.stringify(p).replace(/"/g,'&quot;')})"><i data-lucide="edit-3"></i> Sửa TTS</button></td>
    </tr>`;
  }).join('');
  lucide.createIcons();
}

// ══ AUTO TRANSLATE ══
async function autoTranslate(targetLang, btn) {
  const srcName = document.getElementById('poi-name-vi').value;
  const srcDesc = document.getElementById('poi-desc-vi').value;
  if (!srcName && !srcDesc) { showToast('Nhập nội dung Tiếng Việt trước', 'warning'); return; }
  const orig = btn.innerHTML; btn.innerHTML = '<i data-lucide="loader-2" class="spin"></i> Đang dịch...'; btn.disabled = true; lucide.createIcons();
  try {
    if (srcDesc) { const t = await translateText(srcDesc, targetLang); document.getElementById(`poi-desc-${targetLang}`).value = t; }
    showToast(`✅ Đã dịch sang ${targetLang.toUpperCase()}`, 'success');
  } catch(e) { showToast('❌ Lỗi dịch thuật', 'danger'); }
  finally { btn.innerHTML = orig; btn.disabled = false; lucide.createIcons(); }
}

async function translateText(text, targetLang) {
  const url = `https://translate.googleapis.com/translate_a/single?client=gtx&sl=vi&tl=${targetLang}&dt=t&q=${encodeURIComponent(text)}`;
  const res = await fetch(url); const json = await res.json();
  let out = ''; if (json && json[0]) json[0].forEach(p => { if (p[0]) out += p[0]; });
  return out || text;
}

// ══ EXPORT ══
function exportDataToCSharp() {
  if (!allPois.length) { showToast('Chưa có dữ liệu POI', 'warning'); return; }
  const v = (p, k1, k2) => ((p[k1] || p[k2] || '').toString().replace(/"/g,'\\"').replace(/\n/g,' '));
  let code = `// ═══════════════════════════════════════\n// CODE TỰ ĐỘNG SINH TỪ VĨNH KHÁNH CMS\n// Paste vào App.xaml.cs → InitializeSampleData()\n// ═══════════════════════════════════════\n\n`;
  allPois.forEach(p => {
    code += `await Database.SaveRestaurantAsync(new Restaurant\n{\n`;
    code += `    Name        = "${v(p,'name','Name')}",\n`;
    code += `    Description = "${v(p,'description','Description')}",\n`;
    code += `    Category    = "${v(p,'category','Category')}",\n`;
    code += `    Latitude    = ${p.latitude || p.Latitude || 0},\n`;
    code += `    Longitude   = ${p.longitude || p.Longitude || 0},\n`;
    code += `    Address     = "${v(p,'address','Address')}",\n`;
    code += `    ImageUrl    = "${v(p,'imageUrl','ImageUrl')}",\n`;
    code += `    Rating      = ${p.rating || p.Rating || 4.0},\n`;
    code += `    OpenHours   = "${v(p,'openHours','OpenHours')}",\n`;
    code += `    AudioFile   = "${v(p,'audioFile','AudioFile')}",\n`;
    code += `    TtsScript   = "${v(p,'ttsScript','TtsScript')}",\n`;
    code += `    TtsScriptEn = "${v(p,'ttsScriptEn','TtsScriptEn')}",\n`;
    code += `    TtsScriptZh = "${v(p,'ttsScriptZh','TtsScriptZh')}"\n`;
    code += `});\n\n`;
  });
  document.getElementById('export-code-area').textContent = code;
  document.getElementById('modal-export').style.display = 'flex';
}

function copyExportCode() {
  const el = document.getElementById('export-code-area');
  navigator.clipboard.writeText(el.textContent).then(() => showToast('📋 Đã copy code!', 'success'));
}

// ══ HELPERS ══
function getImgUrl(p) {
  const img = p.imageUrl || p.ImageUrl;
  if (!img) return 'https://via.placeholder.com/260x130/f1f5f9/94a3b8?text=No+Image';
  if (img.startsWith('http') || img.startsWith('data:')) return img;
  return `${BASE_URL}/${img}`;
}

function showToast(msg, type = 'success') {
  const container = document.getElementById('toast-container');
  const toast = document.createElement('div');
  toast.className = `toast ${type}`;
  const icon = type === 'success' ? 'check-circle' : type === 'danger' ? 'x-circle' : type === 'warning' ? 'alert-triangle' : 'info';
  toast.innerHTML = `<i data-lucide="${icon}"></i><span>${msg}</span>`;
  container.appendChild(toast);
  lucide.createIcons();
  setTimeout(() => { toast.style.opacity = '0'; toast.style.transform = 'translateX(100%)'; toast.style.transition = '0.3s'; setTimeout(() => toast.remove(), 300); }, 3000);
}

function logout() { sessionStorage.removeItem('cms_logged_in'); window.location.href = 'login.html'; }
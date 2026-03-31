const API = 'http://localhost:5256/api', BASE_URL = 'http://localhost:5256';
let map, markers = [], allPois = [], tours = [], historyData = [];
let mainChart, pieChart;
let selectedPois = [];
let currentFilter = 'all';
let statsData = null;
let poiCatFilter = '';

// ══ NGÔN NGỮ - lưu trong localStorage ══
const DEFAULT_LANGS = [
  { code: 'vi', name: 'Tiếng Việt', flag: '🇻🇳', isDefault: true },
  { code: 'en', name: 'English',    flag: '🇺🇸', isDefault: false },
  { code: 'zh', name: '中文',        flag: '🇨🇳', isDefault: false },
];
function getLangs() {
  try {
    const saved = localStorage.getItem('cms_languages');
    return saved ? JSON.parse(saved) : DEFAULT_LANGS;
  } catch { return DEFAULT_LANGS; }
}
function saveLangs(langs) {
  localStorage.setItem('cms_languages', JSON.stringify(langs));
}

// ══ INIT ══
document.addEventListener('DOMContentLoaded', () => {
  setupAccordions();
  setupHistorySearch();
  initApp();
});

async function initApp() {
  showSkeleton();
  await Promise.all([loadPois(), loadTours(), loadAnalytics(), loadStats(), loadUsers()]);
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
  document.addEventListener('click', e => {
    const title = e.target.closest('.section-title');
    if (title) title.parentElement.classList.toggle('closed');
  });
}

function setupHistorySearch() {
  const inp = document.getElementById('history-search');
  if (inp) inp.addEventListener('input', e => filterHistory(e.target.value));
}

// ══ PAGE NAV ══
function switchPage(pageId, navEl) {
  document.querySelectorAll('.main-content').forEach(p => p.classList.remove('active'));
  const target = document.getElementById(pageId);
  if (target) target.classList.add('active');
  document.querySelectorAll('.menu-item').forEach(m => m.classList.remove('active'));
  if (navEl) navEl.classList.add('active');

  const titleMap = {
    'page-dashboard': 'Tổng quan hệ thống',
    'page-poi':       'Bản đồ & Quản lý địa điểm',
    'page-tour':      'Quản lý hành trình Tour',
    'page-trans':     'Quản lý nội dung đa ngữ',
    'page-history':   'Nhật ký hành trình khách hàng',
    'page-lang':      'Quản lý ngôn ngữ',
    'page-users':     'Quản lý người dùng',
    'page-audio':     'Quản lý Audio & Thuyết minh',
  };
  document.getElementById('current-page-title').textContent = titleMap[pageId] || 'Dashboard';

  if (pageId === 'page-poi')       { setTimeout(() => { if (map) google.maps.event.trigger(map, 'resize'); }, 200); renderPoiTable(); }
  if (pageId === 'page-trans')     renderTrans();
  if (pageId === 'page-history')   renderHistory();
  if (pageId === 'page-lang')      { renderLangList(); }
  if (pageId === 'page-users')     { loadUsers(); }
  if (pageId === 'page-audio')     renderAudioPage();
  if (pageId === 'page-tour')      renderTours();
  if (pageId === 'page-dashboard') loadStats().then(() => { renderStatsCards(); initCharts(); renderDashboardRecent(); });
}

// ══ GOOGLE MAPS ══
function initMap() {
  map = new google.maps.Map(document.getElementById('google-map'), {
    center: { lat: 10.7615, lng: 106.7033 }, zoom: 16,
    mapTypeControl: false, streetViewControl: false, fullscreenControl: true,
    zoomControl: true, zoomControlOptions: { position: google.maps.ControlPosition.RIGHT_BOTTOM },
    styles: [{ featureType: 'poi', elementType: 'labels', stylers: [{ visibility: 'off' }] }]
  });
  map.addListener('click', e => { closeMapDetail(); openNewPoiForm(e.latLng.lat(), e.latLng.lng()); });
  if (allPois.length) renderMarkers();
}

function renderMarkers() {
  markers.forEach(m => m.setMap(null)); markers = [];
  allPois.forEach(p => {
    const isMajor = (p.category || p.Category || '') === 'Quán ăn';
    if (poiCatFilter && (p.category || p.Category) !== poiCatFilter) return;
    const m = new google.maps.Marker({
      position: { lat: p.latitude || p.Latitude, lng: p.longitude || p.Longitude },
      map,
      icon: { url: `https://maps.google.com/mapfiles/ms/icons/${isMajor ? 'red' : 'green'}-dot.png`, scaledSize: new google.maps.Size(32, 32) },
      title: p.name || p.Name
    });
    m.addListener('click', () => { closeMapDetail(); showMapDetail(p); map.panTo(m.getPosition()); });
    markers.push(m);
  });
  renderPoiCarousel();
  renderPoiTable();
  updatePOIBadge();
}

function renderPoiCarousel() {
  const label = document.getElementById('poi-count-label');
  if (label) label.textContent = `${allPois.length} điểm`;
  const el = document.getElementById('poi-carousel'); if (!el) return;
  el.innerHTML = allPois.map(p => `
    <div class="poi-mini-card" onclick="map&&map.panTo({lat:${p.latitude||p.Latitude},lng:${p.longitude||p.Longitude}});showMapDetail(${JSON.stringify(p).replace(/"/g,'&quot;')})">
      <img src="${getImgUrl(p)}" onerror="this.src='https://via.placeholder.com/260x130/f1f5f9/94a3b8?text=?'">
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

// ══ POI TABLE ══
function filterPoiCat(cat, el) {
  poiCatFilter = cat;
  document.querySelectorAll('.filter-chip').forEach(c => c.classList.remove('active'));
  el.classList.add('active');
  renderPoiTable();
  renderMarkers();
}

function renderPoiTable() {
  const q = (document.getElementById('poi-search')?.value || '').toLowerCase();
  let list = allPois;
  if (poiCatFilter) list = list.filter(p => (p.category||p.Category) === poiCatFilter);
  if (q) list = list.filter(p => (p.name||p.Name||'').toLowerCase().includes(q));

  const langs = getLangs();
  const tbody = document.getElementById('poi-table-body');
  if (!tbody) return;

  if (!list.length) {
    tbody.innerHTML = `<div style="text-align:center;padding:40px;color:var(--text-muted)">Không tìm thấy địa điểm nào</div>`;
    return;
  }

  tbody.innerHTML = list.map(p => {
    const langs2 = getLangs();
    const langDots = langs2.map(l => {
      let has = false;
      if (l.code === 'vi') has = !!(p.ttsScript||p.TtsScript);
      else if (l.code === 'en') has = !!(p.ttsScriptEn||p.TtsScriptEn);
      else if (l.code === 'zh') has = !!(p.ttsScriptZh||p.TtsScriptZh);
      return `<span title="${l.flag} ${l.name}" style="width:7px;height:7px;border-radius:50%;background:${has?'#22c55e':'#e2e8f0'};display:inline-block;flex-shrink:0"></span>`;
    }).join('');
    const hasAudio = !!(p.audioFile||p.AudioFile||p.audioUrl||p.AudioUrl);
    const cat = p.category||p.Category||'—';

    return `
    <div onclick="showMapDetail(${JSON.stringify(p).replace(/\"/g,'&quot;')});if(map){map.panTo({lat:${p.latitude||p.Latitude||0},lng:${p.longitude||p.Longitude||0}});}"
      style="display:flex;align-items:center;gap:12px;padding:12px 16px;border-bottom:1px solid #f1f5f9;cursor:pointer;transition:.15s"
      onmouseover="this.style.background='#f8faff'" onmouseout="this.style.background=''"
    >
      <img src="${getImgUrl(p)}" onerror="this.src='https://via.placeholder.com/40?text=?'"
        style="width:40px;height:40px;border-radius:8px;object-fit:cover;flex-shrink:0;border:1px solid #e2e8f0">
      <div style="flex:1;min-width:0">
        <div style="font-size:13px;font-weight:600;white-space:nowrap;overflow:hidden;text-overflow:ellipsis">${p.name||p.Name}</div>
        <div style="font-size:11px;color:#64748b;margin-top:2px;display:flex;align-items:center;gap:6px">
          <span>⭐ ${(p.rating||p.Rating||0).toFixed(1)}</span>
          <span style="color:#e2e8f0">·</span>
          <span>${p.openHours||p.OpenHours||'—'}</span>
        </div>
      </div>
      <div style="display:flex;flex-direction:column;align-items:flex-end;gap:5px;flex-shrink:0">
        <span style="font-size:10px;font-weight:600;padding:2px 8px;border-radius:20px;background:#eff6ff;color:#2563eb">${cat}</span>
        <div style="display:flex;gap:3px;align-items:center">${langDots}${hasAudio?'<span style="font-size:10px;margin-left:3px">🎵</span>':''}</div>
      </div>
    </div>`;
  }).join('');
}

function showMapDetail(poi) {
  const panel = document.getElementById('map-detail-panel');
  document.getElementById('map-detail-title').innerHTML = `${poi.name || poi.Name}`;
  document.getElementById('map-detail-addr').innerHTML = `📍 ${poi.address || poi.Address || '—'} &nbsp;·&nbsp; ⭐ ${(poi.rating || poi.Rating || 0).toFixed(1)}`;
  document.getElementById('map-detail-desc').textContent = poi.description || poi.Description || 'Chưa có mô tả.';
  document.getElementById('map-detail-img').src = getImgUrl(poi);
  document.getElementById('btn-edit-from-map').onclick = () => openEditPoiForm(poi);
  panel.style.display = 'flex';
  lucide.createIcons();
}
function closeMapDetail() { document.getElementById('map-detail-panel').style.display = 'none'; }

// ══ POI CRUD ══
async function loadPois() {
  try { const res = await fetch(`${API}/restaurants`); allPois = await res.json(); renderMarkers(); }
  catch(e) { console.error('loadPois:', e); }
}

function updatePOIBadge() {
  const b = document.getElementById('poi-badge');
  if (b) b.textContent = allPois.length;
}

// ── Render lang blocks động trong form POI ──
function renderPoiLangBlocks(poi) {
  const langs = getLangs();
  const container = document.getElementById('poi-lang-blocks');
  if (!container) return;

  let extraTrans = {};
  if (poi && (poi.translations || poi.Translations)) {
    try { extraTrans = JSON.parse(poi.translations || poi.Translations); } catch(e){}
  }

  container.innerHTML = langs.map(l => {
    const isDefault = l.isDefault;
    const fieldPrefix = `poi-lang-${l.code}`;
    let nameVal = '', ttsVal = '', audioVal = '';
    if (poi) {
      if (l.code === 'vi') { nameVal = poi.name||poi.Name||''; ttsVal = poi.ttsScript||poi.TtsScript||poi.description||poi.Description||''; }
      else if (l.code === 'en') { nameVal = ''; ttsVal = poi.ttsScriptEn||poi.TtsScriptEn||''; }
      else if (l.code === 'zh') { nameVal = ''; ttsVal = poi.ttsScriptZh||poi.TtsScriptZh||''; }
      else {
        if (extraTrans[l.code]) {
          nameVal = extraTrans[l.code].name || '';
          ttsVal = extraTrans[l.code].tts || '';
        }
      }
      audioVal = poi.audioUrl||poi.AudioUrl||poi.audioFile||poi.AudioFile||'';
    }
    const translateBtn = !isDefault ? `<button class="btn btn-ghost btn-sm" onclick="autoTranslateLang('${l.code}', this)"><i data-lucide="sparkles"></i> Dịch tự động</button>` : '';

    return `
    <div class="lang-block-dynamic ${l.code}" style="margin-bottom:12px">
      <div class="lang-label">
        <span>${l.flag} ${l.name}${isDefault ? ' (mặc định)' : ''}</span>
        ${translateBtn}
      </div>
      <div class="form-group"><label>Tên điểm</label><input type="text" class="form-control" id="${fieldPrefix}-name" value="${escHtml(nameVal)}" placeholder="Tên địa điểm bằng ${l.name}"></div>
      <div class="form-group"><label>Kịch bản TTS / Mô tả</label><textarea class="form-control" id="${fieldPrefix}-tts" rows="2" placeholder="Nội dung thuyết minh bằng ${l.name}...">${escHtml(ttsVal)}</textarea></div>
      <div class="form-group" style="margin:0"><label>File Audio (URL hoặc tên file)</label><input type="text" class="form-control" id="${fieldPrefix}-audio" value="${escHtml(audioVal)}" placeholder="audio_${l.code}.mp3"></div>
    </div>`;
  }).join('');
  lucide.createIcons();
}

function previewPoiImage(input) {
  if (!input.files || !input.files[0]) return;
  const file = input.files[0];

  // Kiểm tra kích thước (max 3MB)
  if (file.size > 3 * 1024 * 1024) {
    showToast('Ảnh quá lớn! Vui lòng chọn ảnh dưới 3MB', 'warning');
    input.value = '';
    return;
  }

  const reader = new FileReader();
  reader.onload = function(e) {
    const base64 = e.target.result;
    // Hiển thị preview
    const preview = document.getElementById('poi-image-preview');
    const placeholder = document.getElementById('poi-image-placeholder');
    preview.src = base64;
    preview.style.display = 'block';
    if (placeholder) placeholder.style.display = 'none';
    // Lưu base64 để gửi lên API
    document.getElementById('poi-image-url').value = base64;
    // Hiện tên file
    const nameEl = document.getElementById('poi-image-filename');
    if (nameEl) nameEl.textContent = `📎 ${file.name} (${(file.size/1024).toFixed(0)} KB)`;
  };
  reader.readAsDataURL(file);
}

function clearPoiImage() {
  document.getElementById('poi-image-file').value = '';
  document.getElementById('poi-image-url').value = '';
  const preview = document.getElementById('poi-image-preview');
  if (preview) { preview.src = ''; preview.style.display = 'none'; }
  const placeholder = document.getElementById('poi-image-placeholder');
  if (placeholder) placeholder.style.display = 'block';
  const nameEl = document.getElementById('poi-image-filename');
  if (nameEl) nameEl.textContent = '';
}

// ── Drag & drop image ──
function handleImageDrop(event) {
  event.preventDefault();
  const dropzone = document.getElementById('poi-image-dropzone');
  if (dropzone) { dropzone.style.borderColor = '#cbd5e1'; dropzone.style.background = '#f8fafc'; }
  const file = event.dataTransfer?.files?.[0];
  if (!file || !file.type.startsWith('image/')) { showToast('Vui lòng thả file ảnh (JPG/PNG/WebP)', 'warning'); return; }
  const fakeInput = { files: [file] };
  previewPoiImage(fakeInput);
}

// ── Preview từ URL ──
function previewFromUrl(url) {
  if (!url || url.startsWith('data:')) return;
  const preview = document.getElementById('poi-image-preview');
  const placeholder = document.getElementById('poi-image-placeholder');
  if (!url.startsWith('http') && !url.startsWith('/')) return;
  preview.src = url.startsWith('http') ? url : `${BASE_URL}/${url}`;
  preview.style.display = 'block';
  if (placeholder) placeholder.style.display = 'none';
}

function escHtml(s) { return (s||'').replace(/"/g,'&quot;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }

function openNewPoiForm(lat, lng) {
  resetPoiForm();
  document.getElementById('poi-form-title').textContent = 'Tạo điểm mới';
  const center = map ? map.getCenter() : { lat: () => 10.7615, lng: () => 106.7033 };
  document.getElementById('poi-lat').value = (lat || center.lat()).toFixed(7);
  document.getElementById('poi-lng').value = (lng || center.lng()).toFixed(7);
  document.getElementById('btn-save-poi').innerHTML = '<i data-lucide="plus"></i> Tạo điểm mới';
  document.getElementById('btn-delete-poi').style.display = 'none';

  renderPoiLangBlocks(null);

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
  document.getElementById('btn-save-poi').innerHTML = '<i data-lucide="save"></i> Lưu thay đổi';
  document.getElementById('btn-delete-poi').style.display = 'flex';

  const imgUrl = p.imageUrl || p.ImageUrl || '';
  document.getElementById('poi-image-url').value = imgUrl;
  const preview = document.getElementById('poi-image-preview');
  const placeholder = document.getElementById('poi-image-placeholder');
  const nameEl = document.getElementById('poi-image-filename');
  if (imgUrl) {
    const src = imgUrl.startsWith('http') || imgUrl.startsWith('data:') ? imgUrl : `${BASE_URL}/${imgUrl}`;
    preview.src = src;
    preview.style.display = 'block';
    if (placeholder) placeholder.style.display = 'none';
    if (nameEl) nameEl.textContent = imgUrl.startsWith('data:') ? '📎 Ảnh đã upload' : `🔗 ${imgUrl}`;
  } else {
    if (preview) { preview.src = ''; preview.style.display = 'none'; }
    if (placeholder) placeholder.style.display = 'block';
    if (nameEl) nameEl.textContent = '';
  }

  renderPoiLangBlocks(p);

  document.getElementById('poi-form-panel').classList.add('open');
  document.getElementById('panel-overlay-poi').style.display = 'block';
  lucide.createIcons();
}

async function savePoiData() {
  const id = document.getElementById('poi-id').value;
  const nameVi = document.getElementById('poi-lang-vi-name')?.value;
  if (!nameVi) { showToast('Vui lòng nhập Tên điểm', 'warning'); return; }

  const btn = document.getElementById('btn-save-poi');
  btn.innerHTML = '<i data-lucide="loader-2" class="spin"></i> Đang lưu...';
  btn.disabled = true;

  const targetLangs = getLangs();
  const translations = {};
  targetLangs.forEach(l => {
    if (!['vi', 'en', 'zh'].includes(l.code)) {
      translations[l.code] = {
        name: document.getElementById(`poi-lang-${l.code}-name`)?.value || '',
        tts: document.getElementById(`poi-lang-${l.code}-tts`)?.value || ''
      };
    }
  });

  const getTts = (code) => document.getElementById(`poi-lang-${code}-tts`)?.value || '';

  const body = {
    Name:        nameVi,
    Description: getTts('vi'),
    Category:    document.getElementById('poi-category').value,
    Latitude:    parseFloat(document.getElementById('poi-lat').value) || 0,
    Longitude:   parseFloat(document.getElementById('poi-lng').value) || 0,
    Rating:      parseFloat(document.getElementById('poi-rating').value) || 4.0,
    OpenHours:   document.getElementById('poi-hours').value,
    Address:     document.getElementById('poi-address').value || 'Vĩnh Khánh, Phường 8, Quận 4',
    Radius:      parseInt(document.getElementById('poi-radius').value) || 50,
    ImageUrl:    document.getElementById('poi-image-url').value,
    AudioFile:   document.getElementById('poi-lang-vi-audio')?.value || '',
    AudioUrl:    document.getElementById('poi-lang-vi-audio')?.value || '',
    TtsScript:   getTts('vi'),
    TtsScriptEn: getTts('en'),
    TtsScriptZh: getTts('zh'),
    Translations: JSON.stringify(translations),
    IsFavorite:  false,
    IsAdsPopup:  false
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
      
      const activePage = document.querySelector('.main-content.active')?.id;
      if (activePage === 'page-poi') { if(typeof renderPoiTable === 'function') renderPoiTable(); }
      if (activePage === 'page-lang') { if(typeof renderTransList === 'function') renderTransList(); }
    }
  } catch(e) { showToast('❌ Lỗi khi lưu dữ liệu', 'danger'); }
  finally {
    btn.innerHTML = id ? '<i data-lucide="save"></i> Lưu thay đổi' : '<i data-lucide="plus"></i> Tạo điểm mới';
    btn.disabled = false; lucide.createIcons();
  }
}

async function deletePoiData() {
  const id = document.getElementById('poi-id').value;
  if (!id) return;
  if (!confirm('⚠️ Xóa địa điểm này? Thao tác không thể hoàn tác.')) return;
  try {
    const res = await fetch(`${API}/restaurants/${id}`, { method: 'DELETE' });
    if (res.ok) { showToast('🗑️ Đã xóa thành công', 'success'); closePoiForm(); await loadPois(); renderStatsCards(); }
  } catch(e) { showToast('❌ Lỗi khi xóa', 'danger'); }
}

function resetPoiForm() {
  ['poi-id','poi-lat','poi-lng','poi-address','poi-hours','poi-image-url','poi-image-file'].forEach(id => { const el = document.getElementById(id); if(el) el.value = ''; });
  document.getElementById('poi-radius').value = 50;
  document.getElementById('poi-category').value = 'Quán ăn';
  const r = document.getElementById('poi-rating'); if(r) r.value = 4.0;
  clearPoiImage();
}

function closePoiForm() {
  document.getElementById('poi-form-panel').classList.remove('open');
  document.getElementById('panel-overlay-poi').style.display = 'none';
}

// ══ AUTO TRANSLATE LANG ══
async function autoTranslateLang(targetLang, btn) {
  const langs = getLangs();
  const defLang = langs.find(l => l.isDefault) || langs[0];
  const srcTts = document.getElementById(`poi-lang-${defLang.code}-tts`)?.value || '';
  const srcName = document.getElementById(`poi-lang-${defLang.code}-name`)?.value || '';
  if (!srcTts && !srcName) { showToast(`Nhập nội dung ${defLang.flag} ${defLang.name} trước`, 'warning'); return; }

  const orig = btn.innerHTML; btn.innerHTML = '<i data-lucide="loader-2" class="spin"></i> Đang dịch...'; btn.disabled = true; lucide.createIcons();
  try {
    if (srcName) {
      const t = await translateText(srcName, targetLang);
      const nameEl = document.getElementById(`poi-lang-${targetLang}-name`);
      if (nameEl) nameEl.value = t;
    }
    if (srcTts) {
      const t = await translateText(srcTts, targetLang);
      const ttsEl = document.getElementById(`poi-lang-${targetLang}-tts`);
      if (ttsEl) ttsEl.value = t;
    }
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

// ══ NGÔN NGỮ MANAGER ══
function renderLangList() {
  const langs = getLangs();
  const container = document.getElementById('lang-list');
  if (!container) return;
  container.innerHTML = langs.map((l, i) => `
    <div class="lang-item-row">
      <div class="lang-flag-big">${l.flag}</div>
      <div class="lang-item-info">
        <div class="lang-item-code">${l.code}</div>
        <div class="lang-item-name">${l.name}</div>
        <div class="lang-item-stats">${allPois.length} địa điểm sẽ có trường nhập nội dung ngôn ngữ này</div>
      </div>
      ${l.isDefault ? '<span class="badge badge-success">Mặc định</span>' : '<span class="badge badge-neutral">Phụ</span>'}
      ${!l.isDefault ? `<button class="btn btn-ghost btn-sm" style="color:var(--danger)" onclick="deleteLang(${i})"><i data-lucide="trash-2"></i></button>` : ''}
    </div>`).join('') || '<div style="text-align:center;padding:30px;color:var(--text-muted)">Chưa có ngôn ngữ nào</div>';
  lucide.createIcons();
}

function openLangPanel() {
  ['lang-code','lang-name','lang-flag'].forEach(id => { const el=document.getElementById(id); if(el) el.value=''; });
  document.getElementById('lang-form-panel').classList.add('open');
  document.getElementById('panel-overlay-lang').style.display = 'block';
  lucide.createIcons();
}
function closeLangPanel() {
  document.getElementById('lang-form-panel').classList.remove('open');
  document.getElementById('panel-overlay-lang').style.display = 'none';
}

function quickLang(code, name, flag) {
  document.getElementById('lang-code').value = code;
  document.getElementById('lang-name').value = name;
  document.getElementById('lang-flag').value = flag;
}

function saveLang() {
  const code = document.getElementById('lang-code').value.trim().toLowerCase();
  const name = document.getElementById('lang-name').value.trim();
  const flag = document.getElementById('lang-flag').value.trim() || '🌐';
  if (!code || !name) { showToast('Nhập mã và tên ngôn ngữ', 'warning'); return; }
  const langs = getLangs();
  if (langs.find(l => l.code === code)) { showToast(`Ngôn ngữ "${code}" đã tồn tại`, 'warning'); return; }
  langs.push({ code, name, flag, isDefault: false });
  saveLangs(langs);
  showToast(`✅ Đã thêm ngôn ngữ ${flag} ${name}`, 'success');
  closeLangPanel();
  renderLangList();
  renderTransList();
}

function deleteLang(idx) {
  const langs = getLangs();
  if (!confirm(`Xóa ngôn ngữ "${langs[idx].name}"?`)) return;
  langs.splice(idx, 1);
  saveLangs(langs);
  showToast('🗑️ Đã xóa ngôn ngữ', 'success');
  renderLangList();
  renderTransList();
}

// ══ BẢNG DỊCH (DƯỚI QUẢN LÝ NGÔN NGỮ) ══
function renderTransList() {
  const tbody = document.getElementById('trans-tbody');
  if (!tbody) return;
  const q = (document.getElementById('search-trans-input')?.value || '').toLowerCase();
  
  const langs = getLangs();
  let html = '';
  allPois.filter(p => (p.name||p.Name||'').toLowerCase().includes(q)).forEach((p) => {
    let extraTrans = {};
    if (p.translations || p.Translations) {
      try { extraTrans = JSON.parse(p.translations || p.Translations); } catch(e){}
    }
    
    const statusBadges = langs.map(l => {
      let textToPlay = '';
      if (l.code === 'vi') { textToPlay = p.ttsScript || p.TtsScript || p.description || p.Description || ''; }
      else if (l.code === 'en') { textToPlay = p.ttsScriptEn || p.TtsScriptEn || ''; }
      else if (l.code === 'zh') { textToPlay = p.ttsScriptZh || p.TtsScriptZh || ''; }
      else {
        if (extraTrans[l.code]) textToPlay = extraTrans[l.code].tts || '';
      }
      
      const hasText = !!textToPlay;
      const opacity = hasText ? '1' : '0.3';
      const playBtn = hasText ? `<button class="btn btn-ghost btn-sm" style="padding:4px;width:24px;height:24px;min-height:24px" onclick="playTtsAudio('${escapeSq(textToPlay)}', '${l.code}')" title="Nghe thử ${l.name}">🔊</button>` : '';
      
      return `<div style="display:inline-flex;align-items:center;opacity:${opacity};margin-right:8px;background:#f1f5f9;padding:2px 6px;border-radius:12px;font-size:12px;border:1px solid #cbd5e1">
                <span style="font-size:16px;margin-right:4px">${l.flag}</span> ${playBtn}
              </div>`;
    }).join('');

    html += `
      <tr>
        <td style="font-weight:600">${p.name||p.Name}</td>
        <td><div style="display:flex;flex-wrap:wrap;gap:4px;align-items:center">${statusBadges}</div></td>
        <td style="text-align:right">
          <button class="btn btn-ghost btn-sm" onclick='openEditPoiForm(allPois.find(x=>(x.id||x.Id)==${p.id||p.Id}))'><i data-lucide="edit-3"></i> Sửa</button>
        </td>
      </tr>
    `;
  });
  
  tbody.innerHTML = html || '<tr><td colspan="3" style="text-align:center;padding:20px;color:var(--text-muted)">Không tìm thấy địa điểm</td></tr>';
  lucide.createIcons();
}

// ══ AUDIO MANAGEMENT PAGE ══
let _audioLangFilter = 'all';

// Script ~30 giây (Google TTS Wavenet ~130-150 tiếng mỗi phút ở 0.95x speed)
const RICH_SCRIPTS = {
  "Ốc Oanh": {
    vi: "Chào mừng bạn đến Ốc Oanh — linh hồn phố ẩm thực Vĩnh Khánh hơn mười năm qua! Quán nổi tiếng với ốc hương nướng tiêu xanh, ốc mỡ hấp sả gừng và ốc len xào dừa béo ngậy. Mỗi con ốc tươi chọn lọc từ miền Tây mỗi sáng, đảm bảo chất lượng tuyệt đối. Thực đơn hơn hai mươi món, nước chấm bí truyền độc quyền. Mở cửa từ bốn giờ chiều đến mười một giờ đêm. Đặt chỗ sớm kẻo hết!",
    en: "Welcome to Oc Oanh — the soul of Vinh Khanh food street for over a decade! Famous for pepper-grilled rock snails, lemongrass-steamed butter snails, and rich coconut-sauteed mud creepers, all hand-picked fresh every morning from the Mekong Delta. Our menu features twenty shellfish preparations with a secret house dipping sauce. Open four in the afternoon until eleven at night. Book early — always packed!",
    zh: "欢迎来到'欧莺'——永庆街美食灵魂超过十年！以胡椒烤香螺、香茅蒸蜗牛和椰汁炒泥螺著称，每日清晨从湄公河精选食材。菜单超过二十种贝类料理，搭配秘制蘸酱。下午四点至晚上十一点营业，请提前预订！"
  },
  "Ốc Sáu Nở": {
    vi: "Bạn đang đến Ốc Sáu Nở — biểu tượng hải sản Sài Gòn nhiều thập kỷ! Thử ngay ốc bươu nhồi thịt hấp gừng, ốc nhảy xào me chua ngọt và mâm ốc tổng hợp năm loại đặc biệt. Nước chấm bí truyền gia truyền là linh hồn của quán. Ngồi vỉa hè Vĩnh Khánh, nhâm nhi ly mát cùng mâm ốc bốc khói — trải nghiệm Sài Gòn đích thực. Mở từ ba giờ chiều đến mười một giờ đêm!",
    en: "Approaching Oc Sau No — a Saigon seafood icon for decades! Must-try: ginger-steamed stuffed snails, tamarind-glazed jumping snails, and the five-variety shellfish platter. Their secret dipping sauce keeps regulars coming back. Sitting on the lively Vinh Khanh sidewalk with an ice-cold drink alongside steaming shellfish — this is authentic Saigon street food. Open three in the afternoon until eleven at night!",
    zh: "欢迎来到'六绽海鲜'——西贡数十年的海鲜传奇！必点：姜汁蒸肉馅田螺、酸甜罗望子炒蜗牛和五种贝类拼盘。秘制蘸酱代代相传，令老顾客念念不忘。坐在永庆街边，冷饮配热螺，尽享西贡街头真味。每天下午三点至晚上十一点营业！"
  },
  "Ốc Thảo": {
    vi: "Phía trước là Ốc Thảo — một trong những quán ốc lâu đời nhất Vĩnh Khánh! Điểm đặc biệt là gia vị độc đáo kết hợp truyền thống Nam Bộ và sáng tạo hiện đại. Thử ốc nướng muối ớt giòn tan, ốc sò hấp bia thơm nồng và ốc de nấu tiêu đen đậm đà. Chị Thảo luôn đích thân đứng bếp đảm bảo hương vị chuẩn nhất. Không khí thân thiện, giá hợp lý. Mở từ bốn giờ chiều đến mười giờ rưỡi tối!",
    en: "Just ahead is Oc Thao — one of Vinh Khanh's oldest and most beloved shellfish spots! Known for blending traditional Southern spices with modern creativity. Try salt-chili grilled snails, beer-steamed clams, and savory black pepper mud snails. Ms. Thao personally oversees every dish from the kitchen. Friendly atmosphere, affordable prices. Open four in the afternoon until half past ten at night!",
    zh: "前方是'草海鲜'——永庆街历史最悠久的贝壳餐厅之一！以传统南越香料与现代创意的独特融合闻名。必尝盐辣烤螺、啤酒蒸蛤蜊和黑椒焖泥螺。草姐每天亲自掌厨，品质始终如一。氛围友好，价格亲民。下午四点至晚上十点半营业！"
  },
  "Lãng Quán": {
    vi: "Chào bạn đến Lãng Quán — không gian ẩm thực trẻ trung sáng tạo giữa phố Vĩnh Khánh! Không chỉ là quán ăn — đây là nơi bạn bè hội tụ và tạo kỷ niệm. Thực đơn đa dạng từ ăn vặt Sài Gòn đến lẩu riêu cua, bò lúc lắc tiêu xanh, gà nướng mật ong. Cuối tuần có âm nhạc live tạo không gian lãng mạn. Phục vụ từ năm giờ chiều đến mười một giờ đêm. Hẹn gặp bạn tại Lãng Quán!",
    en: "Welcome to Lang Quan — a vibrant creative dining space in the heart of Vinh Khanh! More than a restaurant — a gathering place for friends and memories. The menu spans Saigon street snacks to crab roe hotpot, green pepper beef, and honey-glazed grilled chicken. Weekend evenings feature live acoustic music for a romantic atmosphere. Open five in the afternoon until eleven at night. See you at Lang Quan!",
    zh: "欢迎来到'浪漫小馆'——永庆街充满青春创意的餐饮空间！不只是餐厅，更是朋友聚会创造回忆之地。菜单从西贡小吃到蟹黄火锅、青椒牛肉、蜂蜜烤鸡应有尽有。周末有现场原声音乐，浪漫氛围满满。下午五点至晚上十一点营业，期待与您相聚！"
  },
  "Ớt Xiêm Quán": {
    vi: "Ớt Xiêm Quán đang hiện ra trước mắt bạn — thiên đường của những tín đồ vị cay kiểu Khmer Nam Bộ! Ớt xiêm xanh nhỏ nhưng cực cay là linh hồn mọi món. Đừng bỏ qua gà nướng mật ong giòn vàng, tôm rang muối ớt giòn rụm và lẩu thái chua cay đậm vị. Nhân viên sẵn sàng tư vấn mức cay phù hợp. Chuẩn bị khăn giấy và chinh phục ẩm thực cay nhất Vĩnh Khánh! Mở từ mười một giờ sáng đến chín giờ tối.",
    en: "Ot Xiem Quan is right before you — paradise for lovers of bold Khmer-style fiery flavors! The small but ferocious Siam chili pepper defines every dish. Don't miss honey-glazed chili grilled chicken, crunchy salt-chili shrimp, and the sour-spicy Thai-style hotpot. Staff always help you find your ideal heat level. Grab your tissues and conquer the spiciest food on Vinh Khanh street! Open eleven in the morning until nine at night.",
    zh: "朝天椒餐厅就在眼前——辣味爱好者的天堂，以越南南部高棉风味为灵感！个头小却无比辣的暹罗朝天椒贯穿所有菜肴。必尝蜂蜜烤鸡、椒盐炸虾和酸辣泰式火锅。员工随时协助您选择合适辣度。备好纸巾，挑战永庆街最辣美食！上午十一点至晚上九点营业。"
  },
  "Bún Cá Châu Đốc - Dì Tư": {
    vi: "Mùi mắm cá linh và ruốc Châu Đốc chính gốc đang lan tỏa từ bếp Dì Tư! Đây là địa chỉ hiếm tại Sài Gòn giữ nguyên hương vị bún cá An Giang. Nước lèo hầm cá lóc tươi cùng sả gừng tạo mùi thơm đặc trưng. Chan vào tô bún, thêm rau đắng, bắp chuối và cá chiên vàng — bữa sáng hoàn hảo! Mở từ sáu giờ sáng đến hai giờ chiều. Đến sớm kẻo hết!",
    en: "The sweet aroma of linh fish paste and authentic Chau Doc shrimp paste drifts from Auntie Tu's kitchen! One of Saigon's rare spots preserving the original An Giang fish noodle flavors. The broth is slow-simmered with fresh snakehead fish, lemongrass and ginger. Ladled over soft rice noodles with bitter greens, banana blossom and crispy fish — the perfect breakfast! Open six in the morning until two in the afternoon. Come early before it sells out!",
    zh: "四姨厨房飘来正宗朱笃虾酱的甘美香气！西贡难得保留安江原味鱼粉的珍贵地址。汤底以新鲜乌鱼、香茅姜慢火熬制，香气独特无误。浇在米粉上，配苦菜、芭蕉花和炸鱼片——完美早餐！早上六点至下午两点营业，早来早得，卖完即止！"
  },
  "Chilli Lẩu Nướng Quán": {
    vi: "Chào mừng đến Chilli Lẩu Nướng — buffet lẩu nướng tự chọn hấp dẫn giữa phố Vĩnh Khánh! Tự do chọn bò Mỹ thái mỏng, hải sản tươi và rau củ đa dạng để nướng và nhúng lẩu. Nồi lẩu thái chua cay hầm xương cả ngày là điểm nhấn đặc biệt. Giá hợp lý, không gian rộng rãi, thân thiện với sinh viên. Mở từ mười giờ sáng đến mười một giờ đêm mỗi ngày. Rủ bạn bè đến cùng!",
    en: "Welcome to Chilli Hotpot and BBQ — an all-you-can-eat buffet in the heart of Vinh Khanh! Choose from premium US beef, fresh shrimp, crab, squid, and seasonal vegetables to grill and dip in broth. The signature sour-spicy Thai hotpot, simmered all day, is the star of every visit. Student-friendly prices, spacious seating. Open ten in the morning until eleven at night, seven days a week. Bring your crew!",
    zh: "欢迎来到辣椒火锅烤肉——永庆街物超所值的自助烤涮体验！自由挑选优质美国牛肉、新鲜海鲜和各色蔬菜，自助烤涮随心所欲。全天慢火熬制的酸辣泰式火锅是每次必点亮点。价格亲民，空间宽敞，深受学生喜爱。每天上午十点至晚上十一点，欢迎携友同来！"
  },
  "Thế Giới Bò": {
    vi: "Bạn đã đến Thế Giới Bò — vương quốc của tín đồ thịt bò tại Vĩnh Khánh! Gần ba mươi món bò từ truyền thống Việt đến sáng tạo Tây phương. Nổi bật là bò tái chanh lá quế, bò nhúng giấm sôi sùng sục và bò lúc lắc tiêu đen với khoai tây chiên. Nguyên liệu nhập nguồn uy tín, đảm bảo sạch tươi. Không gian industrial hiện đại, ánh đèn warm tone. Mở từ mười giờ sáng đến mười giờ tối!",
    en: "You've arrived at The World of Beef — the ultimate beef kingdom on Vinh Khanh street! Nearly thirty preparations spanning Vietnamese tradition to Western fusion. Standouts include rare beef with lime and basil, bubbling vinegar fondue, and irresistible black pepper stir-fry with crispy fries. All beef sourced from trusted suppliers for guaranteed freshness. Modern industrial decor, warm inviting lighting. Open ten in the morning until ten at night!",
    zh: "您已踏入'牛肉世界'——永庆街牛肉爱好者的终极王国！近三十种料理从越式传统到西式创新应有尽有。亮点包括柠檬香草腌牛肉、醋涮牛肉火锅和黑椒炒牛肉配薯条。食材来自信誉供应商，品质有保障。工业风装潢，灯光温馨。每天上午十点至晚上十点营业！"
  },
  "Cơm Cháy Kho Quet": {
    vi: "Tiếng giòn rụm và mùi khói than — dấu hiệu bạn đang đến Cơm Cháy Kho Quẹt! Cơm cháy giòn tan từ đáy nồi đất nung, ăn kèm kho quẹt thịt ba chỉ và tép bạc đậm đà. Sự kết hợp cơm cháy giòn và kho quẹt ngọt mặn tạo hương vị mộc mạc khó quên. Còn có trứng ốp la, tôm kho tàu và canh chua cá kèo Nam Bộ. Mở từ mười giờ sáng đến chín giờ tối. Ghé ngay kẻo hết!",
    en: "That crackling sound and charcoal aroma mean you're near Com Chay Kho Quet! Crispy rice scraped from clay pot bottoms, served with thick pork belly and shrimp braising sauce. The contrast of crunchy rice with savory-sweet sauce creates a humble yet unforgettable flavor. Also serving fried eggs, caramelized braised shrimp, and Southern Vietnamese sour catfish soup. Open ten in the morning until nine at night. Come before the crispy rice runs out!",
    zh: "酥脆声和炭火烟香——您正在靠近'锅巴蘸酱'！从陶土锅底精心刮下的酥脆锅巴，搭配五花肉银虾浓稠蘸酱。酥脆锅巴与咸甜蘸酱的完美对比，朴实而令人难忘。另有荷包蛋、焦糖焖虾和南越酸鱼汤。上午十点至晚上九点营业，锅巴卖完即止！"
  },
  "Bò Lá Lốt Cô Út": {
    vi: "Hương thơm nồng nàn trong không khí — đó là mùi bò lá lốt nướng than của Cô Út! Hơn mười lăm năm, Cô Út nhóm bếp than mỗi ngày tạo ra những cuộn bò lá lốt vàng ươm thơm phức. Thịt bò xay pha sả tiêu tỏi và mắm ruốc, cuộn lá lốt tươi nướng đỏ than. Chấm nước mắm chua ngọt gừng hoặc muối tiêu chanh — mỗi miếng đều tuyệt vời. Còn có nem nướng sả và bún thịt nướng. Mở từ ba giờ chiều đến mười một giờ đêm!",
    en: "That seductive aroma in the evening air is Auntie Ut's famous lolot leaf beef rolls! For over fifteen years she lights the charcoal grill daily, crafting golden fragrant beef rolls nobody stops at just one. Ground beef seasoned with lemongrass, pepper, garlic and shrimp paste, wrapped in fresh lolot leaves, grilled over glowing charcoal. Dip in ginger fish sauce or salt-pepper-lime — extraordinary every bite. Also serving lemongrass sausage and grilled pork noodles. Open three until eleven at night!",
    zh: "空气中弥漫着迷人香气——那是幺姑著名蒌叶烤牛肉卷的气息！十五年来每天点燃炭炉，精心制作金黄牛肉卷，令人无法只吃一串。牛肉末调入香茅、胡椒、大蒜和虾酱，紧裹新鲜蒌叶，炭火烤制。蘸姜汁鱼露或椒盐柠檬汁，每口都是享受。另有香茅烤肠和烤肉米粉。下午三点至晚上十一点营业！"
  },
  "Bún Thịt Nướng Cô Nga": {
    vi: "Bạn đang đứng trước Bún Thịt Nướng Cô Nga — tiệm bún yêu thích Vĩnh Khánh hơn hai mươi năm! Cô Nga ướp thịt bằng sả mật ong ngũ vị hương và nước cốt dừa, nướng than đến vàng thơm. Tô bún hoàn hảo: bún trắng mềm, thịt nướng thái lát, chả giò giòn và nước mắm chua ngọt. Giá bình dân, phục vụ nhanh và luôn tươi cười. Mở từ sáu giờ sáng đến tám giờ tối. Đến sớm thưởng thức bữa sáng hoàn hảo!",
    en: "You're at Ms. Nga's Grilled Pork Noodles — Vinh Khanh's beloved bun thit nuong stall for over twenty years! Ms. Nga marinates pork daily with lemongrass, honey, five-spice and coconut milk, grilled over charcoal until golden. A perfect bowl: soft rice noodles, sliced grilled pork, crispy spring rolls, fresh herbs, and sweet-sour fish sauce. Affordable, fast service, warm smiles. Open six in the morning until eight at night. Come early for the best breakfast on Vinh Khanh!",
    zh: "您正站在阿娥烤肉米粉前——永庆街深受喜爱超过二十年的米粉摊！阿娥每天用香茅、蜂蜜、五香粉和椰奶腌制猪肉，炭火烤至金黄焦香。完美一碗：柔软米粉、薄切烤肉、酥脆炸春卷和酸甜鱼露。价格亲民，服务快捷，笑容温暖。早上六点至晚上八点营业，早来享用永庆最美早餐！"
  }
};

async function bulkUpdateScripts() {
  if (!allPois.length) { showToast('Chưa load dữ liệu POI', 'warning'); return; }
  const btn = document.getElementById('btn-bulk-update');
  if (btn) { btn.disabled = true; btn.innerHTML = '<i data-lucide="loader-2" class="spin"></i> Đang cập nhật...'; lucide.createIcons(); }

  // Map theo ID để tránh lỗi tên không khớp
  const ID_SCRIPT_MAP = {
    1: RICH_SCRIPTS["Ốc Oanh"],
    2: RICH_SCRIPTS["Ốc Sáu Nở"],
    3: RICH_SCRIPTS["Ốc Thảo"],
    4: RICH_SCRIPTS["Lãng Quán"],
    5: RICH_SCRIPTS["Ớt Xiêm Quán"],
    6: RICH_SCRIPTS["Bún Cá Châu Đốc - Dì Tư"],
    7: RICH_SCRIPTS["Chilli Lẩu Nướng Quán"],
    8: RICH_SCRIPTS["Thế Giới Bò"],
    9: RICH_SCRIPTS["Cơm Cháy Kho Quet"],
    10: RICH_SCRIPTS["Bò Lá Lốt Cô Út"],
    11: RICH_SCRIPTS["Bún Thịt Nướng Cô Nga"],
  };

  let ok = 0, fail = 0;
  for (const p of allPois) {
    const id = p.id || p.Id;
    const name = p.name || p.Name;
    // Ưu tiên match theo ID, fallback theo tên
    const script = ID_SCRIPT_MAP[id] || RICH_SCRIPTS[name];
    if (!script) { console.warn(`[Bulk] Không tìm thấy script cho ID=${id} Name="${name}"`); fail++; continue; }
    const body = {
      ...p,
      Name:         name,
      TtsScript:    script.vi,
      TtsScriptEn:  script.en,
      TtsScriptZh:  script.zh,
      Translations: p.Translations || p.translations || '{}',
      IsFavorite:   p.IsFavorite ?? p.isFavorite ?? false,
      IsAdsPopup:   p.IsAdsPopup ?? p.isAdsPopup ?? false,
      AudioFile:    p.AudioFile || p.audioFile || '',
      AudioUrl:     p.AudioUrl  || p.audioUrl  || '',
      Radius:       p.Radius    || p.radius     || 50,
    };
    try {
      const res = await fetch(`${API}/restaurants/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
      });
      if (res.ok) ok++; else { console.error(`[Bulk] PUT lỗi ID=${id}:`, await res.text()); fail++; }
    } catch(e) { console.error(`[Bulk] Exception ID=${id}:`, e); fail++; }
  }

  if (btn) { btn.disabled = false; btn.innerHTML = '<i data-lucide="refresh-cw"></i> Cập nhật Scripts (30s)'; lucide.createIcons(); }
  showToast(`✅ Đã cập nhật ${ok} quán${fail ? ` | ❌ ${fail} lỗi` : ''}`, ok > 0 ? 'success' : 'danger');
  if (ok > 0) { await loadPois(); renderAudioPage(); }
}

function renderAudioPage() {
  renderAudioStats();
  renderAudioLangTabs();
  renderAudioTable();
}


function renderAudioStats() {
  const bar = document.getElementById('audio-stats-bar');
  if (!bar || !allPois.length) return;
  const total = allPois.length;
  const viCount  = allPois.filter(p => !!(p.ttsScript  ||p.TtsScript )).length;
  const enCount  = allPois.filter(p => !!(p.ttsScriptEn||p.TtsScriptEn)).length;
  const zhCount  = allPois.filter(p => !!(p.ttsScriptZh||p.TtsScriptZh)).length;
  const pct = n => Math.round(n/total*100);
  const card = (icon, label, count, color, bg) => `
    <div style="background:#fff;border:1px solid #e2e8f0;border-radius:14px;padding:16px;display:flex;flex-direction:column;gap:8px">
      <div style="display:flex;align-items:center;gap:8px;font-size:12px;font-weight:600;color:${color}">
        <div style="width:32px;height:32px;background:${bg};border-radius:8px;display:flex;align-items:center;justify-content:center;font-size:16px">${icon}</div>
        ${label}
      </div>
      <div style="font-size:26px;font-weight:800;color:#0f172a">${count}<span style="font-size:13px;font-weight:500;color:#94a3b8">/${total}</span></div>
      <div style="background:#f1f5f9;border-radius:99px;height:6px;overflow:hidden">
        <div style="height:100%;background:${color};border-radius:99px;width:${pct(count)}%;transition:.5s"></div>
      </div>
      <div style="font-size:11px;color:#64748b">${pct(count)}% hoàn thành</div>
    </div>`;
  bar.innerHTML =
    card('📍','Tổng địa điểm', total, '#2563eb','#eff6ff') +
    card('🇻🇳','Tiếng Việt',  viCount,  '#16a34a','#f0fdf4') +
    card('🇺🇸','English',     enCount,  '#d97706','#fffbeb') +
    card('🇨🇳','中文',         zhCount,  '#dc2626','#fef2f2');
}

function renderAudioLangTabs() {
  const el = document.getElementById('audio-lang-tabs');
  if (!el) return;
  const tabs = [
    { code:'all', label:'🎵 Tất cả' },
    { code:'vi',  label:'🇻🇳 VI' },
    { code:'en',  label:'🇺🇸 EN' },
    { code:'zh',  label:'🇨🇳 ZH' },
  ];
  el.innerHTML = tabs.map(t => `
    <button class="filter-chip ${_audioLangFilter===t.code?'active':''}"
      onclick="_audioLangFilter='${t.code}';renderAudioLangTabs();renderAudioTable()">
      ${t.label}
    </button>`).join('');
}

function renderAudioTable() {
  const tbody = document.getElementById('audio-tbody');
  if (!tbody) return;
  const q = (document.getElementById('audio-search')?.value||'').toLowerCase();
  let list = allPois.filter(p => (p.name||p.Name||'').toLowerCase().includes(q));

  // filter: only show rows missing script for selected lang
  if (_audioLangFilter === 'vi') list = list.filter(p => !(p.ttsScript||p.TtsScript));
  if (_audioLangFilter === 'en') list = list.filter(p => !(p.ttsScriptEn||p.TtsScriptEn));
  if (_audioLangFilter === 'zh') list = list.filter(p => !(p.ttsScriptZh||p.TtsScriptZh));

  if (!list.length) {
    tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;padding:40px;color:var(--text-muted)">
      ${_audioLangFilter==='all' ? 'Không tìm thấy địa điểm' : '✅ Tất cả địa điểm đã có script cho ngôn ngữ này!'}
    </td></tr>`;
    return;
  }

  const audioCell = (text, langCode) => {
    if (!text) return `<td style="text-align:center"><span style="font-size:12px;color:#f87171;background:#fef2f2;padding:4px 10px;border-radius:20px;border:1px solid #fecaca">✗ Thiếu</span></td>`;
    const safe = (text||'').replace(/\\/g,'\\\\').replace(/'/g,"\\'").replace(/\n/g,' ');
    return `<td style="text-align:center">
      <button class="btn btn-ghost btn-sm" style="background:#f0fdf4;border:1px solid #bbf7d0;border-radius:20px;padding:4px 10px;font-size:12px;color:#16a34a;gap:4px"
        onclick="playAudioCell('${safe}','${langCode}',this)" title="Nghe thử ${langCode.toUpperCase()}">
        ✓ &nbsp;🔊
      </button>
    </td>`;
  };

  tbody.innerHTML = list.map(p => {
    const id   = p.id||p.Id;
    const name = p.name||p.Name||'—';
    const img  = p.imageUrl||p.ImageUrl||'';
    const imgSrc = img ? (img.startsWith('http')||img.startsWith('data:') ? img : `${BASE_URL}/${img}`) : 'https://via.placeholder.com/44?text=?';
    const vi   = p.ttsScript||p.TtsScript||'';
    const en   = p.ttsScriptEn||p.TtsScriptEn||'';
    const zh   = p.ttsScriptZh||p.TtsScriptZh||'';
    return `<tr>
      <td><img src="${imgSrc}" onerror="this.src='https://via.placeholder.com/44?text=?'"
        style="width:44px;height:44px;border-radius:8px;object-fit:cover;border:1px solid #e2e8f0"></td>
      <td><div style="font-weight:600;font-size:13px">${name}</div>
          <div style="font-size:11px;color:#94a3b8">⭐ ${(p.rating||p.Rating||0).toFixed(1)} · ${p.openHours||p.OpenHours||'—'}</div></td>
      ${audioCell(vi,'vi',id)}
      ${audioCell(en,'en',id)}
      ${audioCell(zh,'zh',id)}
      <td style="text-align:right">
        <button class="btn btn-ghost btn-sm" onclick='openEditPoiForm(allPois.find(x=>(x.id||x.Id)==${id}))'><i data-lucide="edit-3"></i></button>
      </td>
    </tr>`;
  }).join('');
  lucide.createIcons();
}

async function playAudioCell(text, langCode, btn) {
  const stopBtn = document.getElementById('btn-stop-audio');
  const orig = btn.innerHTML;
  btn.innerHTML = '⏳'; btn.disabled = true;
  if (stopBtn) stopBtn.style.display = 'inline-flex';
  try {
    await playTtsAudio(text, langCode);
  } finally {
    btn.innerHTML = orig; btn.disabled = false;
    if (stopBtn) stopBtn.style.display = 'none';
  }
}

function stopCurrentAudio() {
  window.speechSynthesis.cancel();
  if (_ttsAudio) { _ttsAudio.pause(); _ttsAudio = null; }
  const stopBtn = document.getElementById('btn-stop-audio');
  if (stopBtn) stopBtn.style.display = 'none';
}

function escapeSq(s) {
  return (s||'').replace(/'/g, "\\'").replace(/"/g, '&quot;').replace(/\n/g, ' ');
}

// ══ GOOGLE CLOUD TTS (primary) + Web Speech API (fallback) ══
const GOOGLE_TTS_KEY = 'AIzaSyAMX0XgjmNv2O4Twk_CBBmjzDwopqtuexE';
const GOOGLE_TTS_VOICE_MAP = {
  'vi': { languageCode: 'vi-VN', name: 'vi-VN-Wavenet-A', ssmlGender: 'FEMALE' },
  'en': { languageCode: 'en-US', name: 'en-US-Wavenet-F', ssmlGender: 'FEMALE' },
  'zh': { languageCode: 'cmn-CN', name: 'cmn-CN-Wavenet-A', ssmlGender: 'FEMALE' },
  'ja': { languageCode: 'ja-JP', name: 'ja-JP-Wavenet-A', ssmlGender: 'FEMALE' },
  'ko': { languageCode: 'ko-KR', name: 'ko-KR-Wavenet-A', ssmlGender: 'FEMALE' },
  'fr': { languageCode: 'fr-FR', name: 'fr-FR-Wavenet-A', ssmlGender: 'FEMALE' },
};

let _ttsAudio = null; // audio element hiện tại để có thể dừng

async function playTtsAudio(text, langCode) {
  if (!text) return;

  // Dừng audio đang phát (nếu có)
  window.speechSynthesis.cancel();
  if (_ttsAudio) { _ttsAudio.pause(); _ttsAudio = null; }

  try {
    await playGoogleTts(text, langCode);
  } catch (err) {
    console.warn('[TTS] Google Cloud TTS thất bại, fallback Web Speech API:', err.message);
    playWebSpeechTts(text, langCode);
  }
}

async function playGoogleTts(text, langCode) {
  const voiceConfig = GOOGLE_TTS_VOICE_MAP[langCode];
  if (!voiceConfig) throw new Error(`Không có voice map cho ${langCode}`);

  const body = {
    input: { text },
    voice: voiceConfig,
    audioConfig: { audioEncoding: 'MP3', speakingRate: 0.95, pitch: 0.0 }
  };

  const res = await fetch(
    `https://texttospeech.googleapis.com/v1/text:synthesize?key=${GOOGLE_TTS_KEY}`,
    { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }
  );

  if (!res.ok) {
    const errText = await res.text();
    throw new Error(`Google TTS API lỗi ${res.status}: ${errText}`);
  }

  const data = await res.json();
  const audioBase64 = data.audioContent;
  if (!audioBase64) throw new Error('audioContent trống');

  // Decode base64 → blob → phát
  const byteChars = atob(audioBase64);
  const byteArr = new Uint8Array(byteChars.length);
  for (let i = 0; i < byteChars.length; i++) byteArr[i] = byteChars.charCodeAt(i);
  const blob = new Blob([byteArr], { type: 'audio/mpeg' });
  const url = URL.createObjectURL(blob);

  _ttsAudio = new Audio(url);
  _ttsAudio.onended = () => { URL.revokeObjectURL(url); _ttsAudio = null; };
  await _ttsAudio.play();
  console.log(`[TTS] Google Cloud TTS [${langCode}]: OK`);
}

function playWebSpeechTts(text, langCode) {
  const localeMap = { 'vi':'vi-VN', 'en':'en-US', 'zh':'zh-CN', 'ja':'ja-JP', 'ko':'ko-KR', 'fr':'fr-FR', 'th':'th-TH', 'ru':'ru-RU' };
  const utterance = new SpeechSynthesisUtterance(text);
  utterance.lang = localeMap[langCode] || langCode;

  const voices = window.speechSynthesis.getVoices();
  if (voices.length > 0) {
    const targetLang = (localeMap[langCode] || langCode).toLowerCase();
    let voice = voices.find(v => v.lang.toLowerCase().startsWith(targetLang));
    if (!voice && langCode === 'vi') {
      voice = voices.find(v => v.name.toLowerCase().includes('vietnamese') || v.name.toLowerCase().includes('việt'));
    }
    if (voice) utterance.voice = voice;
  }
  window.speechSynthesis.speak(utterance);
}

// Preload Web Speech voices khi trang load
if (typeof window.speechSynthesis !== 'undefined') {
  window.speechSynthesis.onvoiceschanged = () => window.speechSynthesis.getVoices();
  window.speechSynthesis.getVoices();
}

// ══ TOUR ══
async function loadTours() {
  try { const res = await fetch(`${API}/tours`); tours = await res.json(); }
  catch(e) { tours = []; }
}

function renderTours() {
  const el = document.getElementById('tour-grid'); if (!el) return;
  if (!tours.length) {
    el.innerHTML = `<div class="empty-state" style="grid-column:1/-1"><i data-lucide="map"></i><p>Chưa có tour nào. Nhấn "+ Tạo Tour mới" để bắt đầu!</p></div>`;
    lucide.createIcons(); return;
  }
  el.innerHTML = tours.map((t, idx) => {
    const ps = (() => { try { return typeof t.Pois==='string'?JSON.parse(t.Pois||'[]'):(t.Pois||[]); } catch{return[];} })();
    const emoji = t.Emoji||t.emoji||'🍜';
    const rating = t.Rating||t.rating||4.0;
    const duration = t.Duration||t.duration||'';
    const nameEn = t.NameEn||t.nameEn||'';
    const nameZh = t.NameZh||t.nameZh||'';
    return `
    <div class="tour-card">
      <div style="position:relative">
        <img class="tour-card-img" src="${t.ImageUrl||t.img||'https://via.placeholder.com/400x160/f1f5f9/94a3b8?text=Tour'}" onerror="this.src='https://via.placeholder.com/400x160/f1f5f9/94a3b8?text=Tour'">
        <div style="position:absolute;top:12px;left:12px;font-size:28px">${emoji}</div>
        <div style="position:absolute;top:12px;right:12px;background:rgba(0,0,0,0.6);color:#fbbf24;font-size:12px;font-weight:700;padding:4px 10px;border-radius:20px">⭐ ${Number(rating).toFixed(1)}</div>
      </div>
      <div class="tour-card-body">
        <div class="tour-card-title">${t.Name||t.name||'Tour'}</div>
        ${nameEn?`<div style="font-size:11px;color:var(--text-muted);margin:-4px 0 6px">🇬🇧 ${nameEn}${nameZh?' &nbsp;·&nbsp; 🇨🇳 '+nameZh:''}</div>`:''}
        <div class="tour-card-desc">${t.Description||t.desc||''}</div>
        <div class="tour-card-meta">
          <div style="display:flex;gap:8px;flex-wrap:wrap">
            <span class="badge badge-info">📍 ${ps.length} điểm</span>
            ${duration?`<span class="badge badge-neutral">⏱ ${duration}</span>`:''}
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
  resetTourForm(); renderPoiChecklist();
  document.getElementById('tour-form-title').textContent = '🗺️ Tạo hành trình mới';
  document.getElementById('tour-form-panel').classList.add('open');
  document.getElementById('panel-overlay-tour').style.display = 'block';
  lucide.createIcons();
}

function editTour(idx) {
  const t = tours[idx]; resetTourForm();
  document.getElementById('tour-id').value = t.Id||t.id||'';
  document.getElementById('tour-name').value = t.Name||t.name||'';
  document.getElementById('tour-desc').value = t.Description||t.desc||'';
  document.getElementById('tour-duration').value = t.Duration||t.duration||'';
  document.getElementById('tour-rating').value = t.Rating||t.rating||4.0;
  document.getElementById('tour-emoji').value = t.Emoji||t.emoji||'🍜';
  document.getElementById('tour-img').value = t.ImageUrl||t.img||'';
  try { selectedPois = typeof t.Pois==='string'?JSON.parse(t.Pois||'[]'):(t.Pois||[]); } catch { selectedPois=[]; }
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
    const pId = p.id||p.Id, ord = selectedPois.indexOf(pId)+1;
    return `<div class="poi-check-item ${ord>0?'selected':''}" onclick="selectPoiInTour(${pId})">
      <div class="poi-check-num">${ord>0?ord:''}</div>
      <span style="font-size:13px;font-weight:500">${p.name||p.Name}</span>
    </div>`;
  }).join('');
}

async function saveTourData() {
  const id = document.getElementById('tour-id').value;
  const name = document.getElementById('tour-name').value.trim();
  if (!name||selectedPois.length===0) { showToast('Nhập tên tour và chọn ít nhất 1 điểm','warning'); return; }
  const body = {
    Name: name, NameEn: '',
    NameZh: '',
    Description: document.getElementById('tour-desc').value,
    DescEn: '',
    DescZh: '',
    Duration: document.getElementById('tour-duration').value,
    Rating: parseFloat(document.getElementById('tour-rating').value)||4.0,
    Emoji: document.getElementById('tour-emoji').value||'🍜',
    ImageUrl: document.getElementById('tour-img').value,
    Pois: JSON.stringify(selectedPois), IsActive: true
  };
  try {
    const method=id?'PUT':'POST', url=id?`${API}/tours/${id}`:`${API}/tours`;
    await fetch(url,{method,headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});
    showToast('✅ Đã lưu Tour!','success'); closeTourModal(); await loadTours(); renderTours(); renderStatsCards();
  } catch(e) { showToast('❌ Lỗi khi lưu Tour','danger'); }
}

async function deleteTour(id) {
  if (!confirm('Xóa tour này?')) return;
  try {
    await fetch(`${API}/tours/${id}`,{method:'DELETE'});
    showToast('🗑️ Đã xóa Tour','success'); await loadTours(); renderTours(); renderStatsCards();
  } catch(e) { showToast('❌ Lỗi khi xóa','danger'); }
}

function closeTourModal() { document.getElementById('tour-form-panel').classList.remove('open'); document.getElementById('panel-overlay-tour').style.display='none'; }
function resetTourForm() {
  ['tour-id','tour-name','tour-desc','tour-duration','tour-img','tour-emoji'].forEach(id=>{const el=document.getElementById(id);if(el)el.value='';});
  const r=document.getElementById('tour-rating');if(r)r.value=4.0; selectedPois=[];
}

// ══ ANALYTICS ══
async function loadAnalytics() {
  try { const res=await fetch(`${API}/analytics`); historyData=await res.json(); renderStatsCards(); renderHistory(); renderDashboardRecent(); }
  catch(e) { console.error('loadAnalytics:',e); }
}

function renderStatsCards() {
  const el = document.getElementById('dashboard-stats'); if (!el) return;
  const totalPois = allPois.length;
  const totalTours = tours.length;
  const totalVisits = statsData?.totalVisits ?? historyData.length;
  const todayVisits = statsData?.todayVisits ?? historyData.filter(h => new Date(h.Timestamp||h.timestamp).toDateString()===new Date().toDateString()).length;
  const weekVisits = statsData?.weekVisits ?? 0;
  const majorPois = allPois.filter(p=>(p.category||p.Category)==='Quán ăn').length;
  el.innerHTML = `
    <div class="stat-card"><div class="stat-icon blue"><i data-lucide="map-pin"></i></div><div class="stat-info"><p class="text-muted">Tổng POI</p><h2 class="stat-val">${totalPois}</h2><div class="stat-trend up">↑ ${majorPois} quán ăn</div></div></div>
    <div class="stat-card"><div class="stat-icon green"><i data-lucide="share-2"></i></div><div class="stat-info"><p class="text-muted">Hành trình Tour</p><h2 class="stat-val">${totalTours}</h2><div class="stat-trend" style="color:var(--text-muted)">Tuyến tham quan</div></div></div>
    <div class="stat-card"><div class="stat-icon orange"><i data-lucide="activity"></i></div><div class="stat-info"><p class="text-muted">Lượt ghé thăm</p><h2 class="stat-val">${totalVisits}</h2><div class="stat-trend up">↑ ${todayVisits} hôm nay · ${weekVisits} tuần này</div></div></div>
    <div class="stat-card"><div class="stat-icon red"><i data-lucide="languages"></i></div><div class="stat-info"><p class="text-muted">Ngôn ngữ</p><h2 class="stat-val">${getLangs().length}</h2><div class="stat-trend" style="color:var(--text-muted)">${getLangs().map(l=>l.flag).join(' ')}</div></div></div>`;
  lucide.createIcons();
}

function renderHistory(filter='') {
  const tbody = document.getElementById('history-tbody'); if (!tbody) return;
  let data = historyData;
  if (currentFilter!=='all') data=data.filter(h=>(h.EventType||h.eventType||'').toLowerCase()===currentFilter);
  if (filter) data=data.filter(h=>(h.RestaurantName||h.restaurantName||'').toLowerCase().includes(filter.toLowerCase()));
  if (!data.length) { tbody.innerHTML=`<tr><td colspan="4" style="text-align:center;padding:40px;color:var(--text-muted)">Chưa có dữ liệu</td></tr>`; return; }
  tbody.innerHTML = data.map(h => {
    const evt=(h.EventType||h.eventType||'visit').toLowerCase();
    const name=h.RestaurantName||h.restaurantName||'—';
    const time=new Date(h.Timestamp||h.timestamp).toLocaleString('vi-VN');
    
    let icon = 'activity', action = 'Tương tác ẩn', color = '#64748b', bg = '#f8fafc';
    if(evt.includes('enter')) {
        icon = 'navigation'; action = 'Vừa đi ngang qua / Ghé thăm'; color = '#10b981'; bg = '#f0fdf4';
    } else if(evt.includes('click')) {
        icon = 'mouse-pointer'; action = 'Bấm xem chi tiết'; color = '#3b82f6'; bg = '#eff6ff';
    } else if(evt.includes('exit')) {
        icon = 'log-out'; action = 'Rời khỏi khu vực'; color = '#f59e0b'; bg = '#fffbeb';
    } else if(evt.includes('audio') || evt.includes('play')) {
        icon = 'volume-2'; action = 'Nghe thuyết minh audio'; color = '#8b5cf6'; bg = '#f3e8ff';
    }

    return `<tr>
      <td>
        <div style="width:36px;height:36px;border-radius:10px;background:${bg};color:${color};display:flex;align-items:center;justify-content:center">
          <i data-lucide="${icon}" style="width:16px;height:16px"></i>
        </div>
      </td>
      <td><span style="font-weight:600;font-size:13px">${action}</span></td>
      <td><strong>${name}</strong></td>
      <td style="font-size:12px;color:var(--text-muted)">${time}</td>
    </tr>`;
  }).join('');
  lucide.createIcons();
}

function filterHistory(val) { renderHistory(val); }
function setFilter(f,el) { currentFilter=f; document.querySelectorAll('#page-history .filter-chip').forEach(c=>c.classList.remove('active')); el.classList.add('active'); renderHistory(document.getElementById('history-search')?.value||''); }

function renderDashboardRecent() {
  const tbody=document.getElementById('dashboard-recent-tbody'); if(!tbody) return;
  tbody.innerHTML=historyData.slice(0,5).map(h=>{
    const evt=(h.EventType||h.eventType||'visit').toLowerCase();
    let action = 'Tương tác';
    if(evt.includes('enter')) action = 'Ghé thăm';
    else if(evt.includes('click')) action = 'Xem thông tin';
    else if(evt.includes('exit')) action = 'Rời khỏi';
    return `<tr><td><strong>${h.RestaurantName||h.restaurantName||'POI'}</strong></td><td><span class="badge badge-info" style="font-size:11px;background:#f8fafc;color:#475569;border:1px solid #e2e8f0;font-weight:500">${action}</span></td><td style="color:var(--text-muted);font-size:12px">${new Date(h.Timestamp||h.timestamp).toLocaleTimeString('vi-VN')}</td></tr>`
  }).join('')||`<tr><td colspan="3" style="text-align:center;color:var(--text-muted);padding:20px">Chưa có hoạt động</td></tr>`;
}

async function loadStats() {
  try { const res=await fetch(`${API}/analytics/stats`); statsData=await res.json(); } catch(e) { statsData=null; }
}

function initCharts() {
  const ctx=document.getElementById('mainChart')?.getContext('2d');
  const ctxPie=document.getElementById('pieChart')?.getContext('2d');
  if(!ctx||!ctxPie) return;
  if(mainChart) mainChart.destroy();
  if(pieChart) pieChart.destroy();
  let labels=[],data=[];
  if(statsData?.byDay?.length>0) { statsData.byDay.forEach(d=>{ labels.push(new Date(d.Day||d.day).toLocaleDateString('vi-VN',{month:'short',day:'numeric'})); data.push(d.Count||d.count||0); }); }
  else { const hc={}; historyData.forEach(h=>{const hr=new Date(h.Timestamp||h.timestamp).getHours();hc[hr]=(hc[hr]||0)+1;}); Object.keys(hc).forEach(h=>{labels.push(`${h}:00`);data.push(hc[h]);}); if(!labels.length){labels.push('Chưa có data');data.push(0);} }
  mainChart=new Chart(ctx,{type:'bar',data:{labels,datasets:[{label:'Lượt ghé thăm',data,backgroundColor:'rgba(37,99,235,0.8)',borderRadius:6,borderSkipped:false}]},options:{responsive:true,maintainAspectRatio:false,plugins:{legend:{display:false}},scales:{y:{beginAtZero:true,grid:{color:'#f1f5f9'},ticks:{stepSize:1}},x:{grid:{display:false}}}}});
  let topLabels=[],topValues=[];
  if(statsData?.topPoi?.length>0){statsData.topPoi.forEach(p=>{topLabels.push(p.Name||p.name||'?');topValues.push(p.VisitCount||p.visitCount||0);});}
  else{const rc={};historyData.forEach(h=>{const n=h.RestaurantName||h.restaurantName||'Khác';rc[n]=(rc[n]||0)+1;});const top=Object.entries(rc).sort((a,b)=>b[1]-a[1]).slice(0,5);top.forEach(([n,v])=>{topLabels.push(n);topValues.push(v);});}
  if(!topLabels.length){topLabels.push('Chưa có data');topValues.push(1);}
  pieChart=new Chart(ctxPie,{type:'doughnut',data:{labels:topLabels,datasets:[{data:topValues,backgroundColor:['#2563eb','#10b981','#f59e0b','#ef4444','#8b5cf6'],borderWidth:0,hoverOffset:4}]},options:{responsive:true,maintainAspectRatio:false,cutout:'65%',plugins:{legend:{position:'bottom',labels:{font:{size:11},padding:12}}}}});
}

// ══ TRANSLATION ══
function renderTrans() {
  const langs = getLangs();
  // Cập nhật header động
  const thead = document.getElementById('trans-thead-row');
  if (thead) {
    thead.innerHTML = '<th>Tên điểm</th>' + langs.map(l => `<th>${l.flag} ${l.name.split(' ')[0]}</th>`).join('') + '<th>Thao tác</th>';
  }
  const tbody = document.getElementById('trans-tbody'); if (!tbody) return;
  if (!allPois.length) { tbody.innerHTML=`<tr><td colspan="${langs.length+2}" style="text-align:center;padding:40px;color:var(--text-muted)">Chưa có dữ liệu</td></tr>`; return; }
  tbody.innerHTML = allPois.map(p => {
    const langCols = langs.map(l => {
      let has = false;
      if (l.code==='vi') has=!!(p.ttsScript||p.TtsScript);
      else if (l.code==='en') has=!!(p.ttsScriptEn||p.TtsScriptEn);
      else if (l.code==='zh') has=!!(p.ttsScriptZh||p.TtsScriptZh);
      return `<td><span class="badge ${has?'badge-success':'badge-danger'}">${has?'✓ Có':'✗ Thiếu'}</span></td>`;
    }).join('');
    return `<tr><td><strong>${p.name||p.Name}</strong></td>${langCols}<td><button class="btn btn-ghost btn-sm" onclick='openEditPoiForm(${JSON.stringify(p).replace(/'/g,"&apos;")})'><i data-lucide="edit-3"></i></button></td></tr>`;
  }).join('');
  lucide.createIcons();
}

// ══ EXPORT ══
function exportDataToCSharp() {
  if (!allPois.length) { showToast('Chưa có dữ liệu POI','warning'); return; }
  const v=(p,k1,k2)=>((p[k1]||p[k2]||'').toString().replace(/"/g,'\\"').replace(/\n/g,' '));
  let code=`// ═══════════════════════════════════════\n// CODE TỰ ĐỘNG SINH TỪ VĨNH KHÁNH CMS\n// Paste vào App.xaml.cs → InitializeSampleData()\n// ═══════════════════════════════════════\n\n`;
  allPois.forEach(p=>{
    code+=`await Database.SaveRestaurantAsync(new Restaurant\n{\n`;
    code+=`    Name        = "${v(p,'name','Name')}",\n`;
    code+=`    Description = "${v(p,'description','Description')}",\n`;
    code+=`    Category    = "${v(p,'category','Category')}",\n`;
    code+=`    Latitude    = ${p.latitude||p.Latitude||0},\n`;
    code+=`    Longitude   = ${p.longitude||p.Longitude||0},\n`;
    code+=`    Address     = "${v(p,'address','Address')}",\n`;
    code+=`    ImageUrl    = "${v(p,'imageUrl','ImageUrl')}",\n`;
    code+=`    Rating      = ${p.rating||p.Rating||4.0},\n`;
    code+=`    OpenHours   = "${v(p,'openHours','OpenHours')}",\n`;
    code+=`    AudioFile   = "${v(p,'audioFile','AudioFile')}",\n`;
    code+=`    TtsScript   = "${v(p,'ttsScript','TtsScript')}",\n`;
    code+=`    TtsScriptEn = "${v(p,'ttsScriptEn','TtsScriptEn')}",\n`;
    code+=`    TtsScriptZh = "${v(p,'ttsScriptZh','TtsScriptZh')}"\n`;
    code+=`});\n\n`;
  });
  document.getElementById('export-code-area').textContent=code;
  document.getElementById('modal-export').style.display='flex';
}

function copyExportCode() { navigator.clipboard.writeText(document.getElementById('export-code-area').textContent).then(()=>showToast('📋 Đã copy code!','success')); }

// ══ HELPERS ══
function getImgUrl(p) {
  const img=p.imageUrl||p.ImageUrl;
  if(!img) return 'https://via.placeholder.com/260x130/f1f5f9/94a3b8?text=No+Image';
  if(img.startsWith('http')||img.startsWith('data:')) return img;
  return `${BASE_URL}/${img}`;
}

function showToast(msg,type='success') {
  const container=document.getElementById('toast-container');
  const toast=document.createElement('div');
  toast.className=`toast ${type}`;
  const icon=type==='success'?'check-circle':type==='danger'?'x-circle':type==='warning'?'alert-triangle':'info';
  toast.innerHTML=`<i data-lucide="${icon}"></i><span>${msg}</span>`;
  container.appendChild(toast); lucide.createIcons();
  setTimeout(()=>{toast.style.opacity='0';toast.style.transform='translateX(100%)';toast.style.transition='0.3s';setTimeout(()=>toast.remove(),300);},3000);
}

// ══════════════════════════════════════
// QUẢN LÝ NGƯỜI DÙNG
// ══════════════════════════════════════
let allUsers = [];
let userRoleFilter = '';

async function loadUsers() {
  try {
    const res = await fetch(`${API}/auth/users`);
    if (!res.ok) throw new Error('API không hỗ trợ');
    allUsers = await res.json();
  } catch(e) {
    // Fallback: dùng mock data nếu chưa có endpoint
    allUsers = [
      { id: 1, username: 'admin', fullName: 'Administrator', role: 'admin', createdAt: new Date().toISOString() }
    ];
  }
  const b = document.getElementById('user-badge');
  if (b) b.textContent = allUsers.length;
  renderUserTable();
}

function filterUserRole(role, el) {
  userRoleFilter = role;
  document.querySelectorAll('#page-users .filter-chip').forEach(c => c.classList.remove('active'));
  if (el) el.classList.add('active');
  renderUserTable();
}

function renderUserTable() {
  const q = (document.getElementById('user-search')?.value || '').toLowerCase();
  let list = allUsers;
  if (userRoleFilter) list = list.filter(u => (u.role || u.Role || '').toLowerCase() === userRoleFilter);
  if (q) list = list.filter(u =>
    (u.fullName || u.FullName || '').toLowerCase().includes(q) ||
    (u.username || u.Username || '').toLowerCase().includes(q)
  );

  const tbody = document.getElementById('user-tbody');
  if (!tbody) return;

  const badge = document.getElementById('user-badge');
  if (badge) badge.textContent = allUsers.length;

  if (!list.length) {
    tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;padding:40px;color:var(--text-muted)">Không tìm thấy người dùng</td></tr>`;
    return;
  }

  tbody.innerHTML = list.map(u => {
    const id       = u.id || u.Id;
    const name     = u.fullName || u.FullName || '—';
    const uname    = u.username || u.Username || '—';
    const role     = (u.role || u.Role || 'user').toLowerCase();
    const isAdmin  = role === 'admin';
    const isSelf   = uname === 'admin'; // bảo vệ tài khoản gốc
    const created  = u.createdAt || u.CreatedAt;
    const dateStr  = created ? new Date(created).toLocaleDateString('vi-VN') : '—';

    const initials = name.split(' ').map(w => w[0]).slice(-2).join('').toUpperCase() || uname[0].toUpperCase();
    const avatarColor = isAdmin ? '#2563eb' : '#10b981';

    return `<tr>
      <td>
        <div style="display:flex;align-items:center;gap:10px">
          <div style="width:36px;height:36px;border-radius:50%;background:${avatarColor};color:#fff;display:flex;align-items:center;justify-content:center;font-size:13px;font-weight:700;flex-shrink:0">${initials}</div>
          <div>
            <div style="font-weight:600;font-size:13px">${name}</div>
            <div style="font-size:11px;color:var(--text-muted)">#${id}</div>
          </div>
        </div>
      </td>
      <td style="font-family:monospace;font-size:13px">${uname}</td>
      <td>
        <span class="badge ${isAdmin ? 'badge-info' : 'badge-success'}">
          ${isAdmin ? '👑 Admin' : '👤 User'}
        </span>
      </td>
      <td><span class="badge badge-success">● Hoạt động</span></td>
      <td style="font-size:12px;color:var(--text-muted)">${dateStr}</td>
      <td>
        <div style="display:flex;gap:6px">
          <button class="btn btn-ghost btn-sm" onclick="editUser(${id})" title="Chỉnh sửa">
            <i data-lucide="edit-3"></i>
          </button>
          ${!isSelf ? `<button class="btn btn-ghost btn-sm" style="color:var(--danger)" onclick="deleteUserById(${id})" title="Xóa">
            <i data-lucide="trash-2"></i>
          </button>` : '<span style="font-size:10px;color:var(--text-muted);padding:0 4px">Gốc</span>'}
        </div>
      </td>
    </tr>`;
  }).join('');
  lucide.createIcons();
}

function openUserPanel() {
  document.getElementById('user-id').value = '';
  document.getElementById('user-panel-title').textContent = 'Thêm người dùng';
  document.getElementById('btn-delete-user').style.display = 'none';
  document.getElementById('user-pw-group').style.display = 'block';
  document.getElementById('user-pw-hint').style.display = 'none';
  ['user-fullname','user-username','user-password','user-password-edit'].forEach(id => {
    const el = document.getElementById(id); if (el) el.value = '';
  });
  document.getElementById('user-role').value = 'user';
  document.getElementById('user-form-panel').classList.add('open');
  document.getElementById('panel-overlay-user').style.display = 'block';
  lucide.createIcons();
}

function editUser(id) {
  const u = allUsers.find(x => (x.id || x.Id) === id);
  if (!u) return;
  document.getElementById('user-id').value = id;
  document.getElementById('user-panel-title').textContent = 'Chỉnh sửa người dùng';
  document.getElementById('btn-delete-user').style.display = (u.username || u.Username) === 'admin' ? 'none' : 'flex';
  document.getElementById('user-pw-group').style.display = 'none';
  document.getElementById('user-pw-hint').style.display = 'block';
  document.getElementById('user-fullname').value = u.fullName || u.FullName || '';
  document.getElementById('user-username').value = u.username || u.Username || '';
  document.getElementById('user-password-edit').value = '';
  document.getElementById('user-role').value = (u.role || u.Role || 'user').toLowerCase();
  document.getElementById('user-form-panel').classList.add('open');
  document.getElementById('panel-overlay-user').style.display = 'block';
  lucide.createIcons();
}

function closeUserPanel() {
  document.getElementById('user-form-panel').classList.remove('open');
  document.getElementById('panel-overlay-user').style.display = 'none';
}

async function saveUser() {
  const id       = document.getElementById('user-id').value;
  const fullName = document.getElementById('user-fullname').value.trim();
  const username = document.getElementById('user-username').value.trim();
  const password = id
    ? document.getElementById('user-password-edit').value
    : document.getElementById('user-password').value;
  const role     = document.getElementById('user-role').value;

  if (!fullName || !username) { showToast('Nhập đầy đủ họ tên và username', 'warning'); return; }
  if (!id && password.length < 6) { showToast('Mật khẩu tối thiểu 6 ký tự', 'warning'); return; }

  const body = { FullName: fullName, Username: username, Role: role };
  if (password) body.Password = password;

  try {
    const method = id ? 'PUT' : 'POST';
    const url    = id ? `${API}/auth/users/${id}` : `${API}/auth/register`;
    const res    = await fetch(url, { method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
    if (res.ok) {
      showToast(id ? '✅ Cập nhật thành công!' : '✅ Đã thêm người dùng!', 'success');
      closeUserPanel();
      await loadUsers();
    } else {
      const err = await res.json().catch(() => ({}));
      showToast('❌ ' + (err.message || 'Lỗi khi lưu'), 'danger');
    }
  } catch(e) { showToast('❌ Không kết nối được API', 'danger'); }
}

async function deleteUserById(id) {
  const u = allUsers.find(x => (x.id || x.Id) === id);
  if (!confirm(`Xóa người dùng "${u?.fullName || u?.FullName}"?`)) return;
  try {
    const res = await fetch(`${API}/auth/users/${id}`, { method: 'DELETE' });
    if (res.ok) { showToast('🗑️ Đã xóa người dùng', 'success'); await loadUsers(); }
    else showToast('❌ Lỗi khi xóa', 'danger');
  } catch(e) { showToast('❌ Không kết nối được API', 'danger'); }
}

async function deleteUser() {
  const id = document.getElementById('user-id').value;
  if (id) { closeUserPanel(); await deleteUserById(parseInt(id)); }
}

function togglePw() {
  const inp = document.getElementById('user-password');
  const eye = document.getElementById('pw-eye');
  if (inp.type === 'password') { inp.type = 'text'; eye.setAttribute('data-lucide', 'eye-off'); }
  else { inp.type = 'password'; eye.setAttribute('data-lucide', 'eye'); }
  lucide.createIcons();
}

function togglePwEdit() {
  const inp = document.getElementById('user-password-edit');
  const eye = document.getElementById('pw-eye-edit');
  if (inp.type === 'password') { inp.type = 'text'; eye.setAttribute('data-lucide', 'eye-off'); }
  else { inp.type = 'password'; eye.setAttribute('data-lucide', 'eye'); }
  lucide.createIcons();
}

function logout() { sessionStorage.removeItem('cms_logged_in'); window.location.href='login.html'; }
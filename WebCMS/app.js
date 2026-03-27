const API = 'http://localhost:5256/api', BASE_URL = 'http://localhost:5256';
let map, markers = [], allPois = [], tours = [], historyData = [];
let mainChart, pieChart;

// ══ FORM STATE (POI) ══
let currentPoiLang = 'vi';
let poiLangData = {
  vi: { name: '', desc: '', audio: '' },
  en: { name: '', desc: '', audio: '' },
  zh: { name: '', desc: '', audio: '' }
};

// ══ FORM STATE (TOUR) ══
let selectedPois = [];

document.addEventListener('DOMContentLoaded', () => {
    initApp();
    setupAccordions();
});

async function initApp() {
    await loadPois();
    await loadTours();
    await loadAnalytics();
    lucide.createIcons();
    switchPage('page-dashboard', document.getElementById('menu-dashboard'));
}

function setupAccordions() {
    document.querySelectorAll('.section-title').forEach(title => {
        title.addEventListener('click', () => title.parentElement.classList.toggle('closed'));
    });
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
  document.getElementById('current-page-title').textContent = titleMap[pageId] || 'VinhKhanhTour Admin';

  if (pageId === 'page-poi') setTimeout(() => google.maps.event.trigger(map, 'resize'), 300);
  if (pageId === 'page-audio') renderAudio();
  if (pageId === 'page-trans') renderTrans();
  if (pageId === 'page-history') renderHistory();
  if (pageId === 'page-dashboard') initCharts();
}

// ══ GOOGLE MAPS ══
function initMap() {
  map = new google.maps.Map(document.getElementById('google-map'), {
    center: { lat: 10.8231, lng: 106.6297 }, zoom: 15,
    styles: [ { featureType: "poi", elementType: "labels", stylers: [{ visibility: "off" }] } ]
  });
  map.addListener('click', e => { closeMapDetail(); openNewPoiForm(e.latLng.lat(), e.latLng.lng()); });
}

function renderMarkers() {
  markers.forEach(m => m.setMap(null)); markers = [];
  allPois.forEach(p => {
    const isMajor = (p.category || p.Category || 'Quán ăn') === 'Quán ăn';
    const color = isMajor ? 'red' : 'green';
    const m = new google.maps.Marker({ 
      position: { lat: p.latitude || p.Latitude, lng: p.longitude || p.Longitude }, 
      map, 
      icon: { url: `https://maps.google.com/mapfiles/ms/icons/${color}-dot.png`, scaledSize: new google.maps.Size(32, 32) } 
    });
    m.addListener('click', () => { closePoiForm(); showMapDetail(p); map.panTo(m.getPosition()); });
    markers.push(m);
  });
  renderPoiCarousel();
}

function renderPoiCarousel() {
  const el = document.getElementById('poi-carousel'); if (!el) return;
  el.innerHTML = allPois.map(p => `
    <div class="poi-mini-card" onclick="map.panTo({lat:${p.latitude || p.Latitude}, lng:${p.longitude || p.Longitude}}); showMapDetail(${JSON.stringify(p).replace(/"/g, '&quot;')})">
      <img src="${getImgUrl(p)}">
      <div class="poi-mini-info"><h4>${p.name || p.Name}</h4><small>${p.category || p.Category || 'Quán ăn'}</small></div>
    </div>`).join('');
}

function showMapDetail(poi) {
  const panel = document.getElementById('map-detail-panel');
  const cat = poi.category || poi.Category || 'Quán ăn';
  const badgeClass = cat === 'Quán ăn' ? 'badge-danger' : 'badge-success';
  document.getElementById('map-detail-title').innerHTML = `${poi.name || poi.Name} <span class="badge ${badgeClass}" style="margin-left:8px">${cat}</span>`;
  document.getElementById('map-detail-addr').textContent = poi.address || poi.Address || 'Không có địa chỉ';
  document.getElementById('map-detail-desc').textContent = poi.description || poi.Description || 'Chưa có mô tả chi tiết cho địa điểm này.';
  document.getElementById('map-detail-img').src = getImgUrl(poi);
  document.getElementById('btn-edit-from-map').onclick = () => openEditPoiForm(poi);
  panel.style.display = 'flex'; lucide.createIcons();
}

function closeMapDetail() { document.getElementById('map-detail-panel').style.display = 'none'; }

// ══ POI DATA CRUD (GPS ADMIN STYLE) ══
async function loadPois() {
  try {
    const res = await fetch(`${API}/restaurants`);
    allPois = await res.json();
    renderMarkers();
  } catch(e) { console.error(e); }
}

function openNewPoiForm(lat, lng) {
    resetPoiForm();
    document.getElementById('poi-form-title').textContent = '📍 Tạo điểm mới';
    
    // Nếu không có tọa độ truyền vào, lấy trung tâm bản đồ hiện tại hoặc để trống tùy ý
    const center = map.getCenter();
    document.getElementById('poi-lat').value = (lat || center.lat()).toFixed(7);
    document.getElementById('poi-lng').value = (lng || center.lng()).toFixed(7);
    
    // UI Adjustments
    const btnSave = document.getElementById('btn-save-poi');
    if (btnSave) btnSave.innerHTML = '<i data-lucide="plus"></i> Tạo điểm mới';
    const btnDel = document.getElementById('btn-delete-poi');
    if (btnDel) btnDel.style.display = 'none';

    document.getElementById('poi-form-panel').classList.add('open');
    document.getElementById('panel-overlay-poi').style.display = 'block';
    lucide.createIcons();
}

function openEditPoiForm(poi) {
    resetPoiForm();
    const p = poi;
    document.getElementById('poi-form-title').textContent = '📝 Chỉnh sửa điểm';
    document.getElementById('poi-id').value = p.id || p.Id;
    document.getElementById('poi-lat').value = (p.latitude || p.Latitude || 0).toFixed(7);
    document.getElementById('poi-lng').value = (p.longitude || p.Longitude || 0).toFixed(7);
    document.getElementById('poi-radius').value = p.radius || p.Radius || 50;
    document.getElementById('poi-category').value = p.category || p.Category || 'Quán ăn';
    document.getElementById('poi-ads-popup').checked = p.isAdsPopup || p.IsAdsPopup || false;
    document.getElementById('poi-audio-url').value = p.audioUrl || p.AudioUrl || p.audioFile || p.AudioFile || '';
    
    // UI Adjustments
    const btnSave = document.getElementById('btn-save-poi');
    if (btnSave) btnSave.innerHTML = '<i data-lucide="save"></i> Lưu thay đổi';
    const btnDel = document.getElementById('btn-delete-poi');
    if (btnDel) btnDel.style.display = 'block';

    // Populate Language Fields
    document.getElementById('poi-name-vi').value = p.name || p.Name || '';
    document.getElementById('poi-desc-vi').value = p.description || p.Description || p.ttsScript || p.TtsScript || '';
    
    document.getElementById('poi-name-en').value = p.ttsScriptEn || p.TtsScriptEn || ''; // Assuming EN Name/Script stored here
    document.getElementById('poi-desc-en').value = p.ttsScriptEn || ''; 
    
    document.getElementById('poi-name-zh').value = p.ttsScriptZh || p.TtsScriptZh || '';
    document.getElementById('poi-desc-zh').value = p.ttsScriptZh || '';

    document.getElementById('poi-form-panel').classList.add('open');
    document.getElementById('panel-overlay-poi').style.display = 'block';
    lucide.createIcons();
}

function switchFormLanguage(lang) {
    poiLangData[currentPoiLang] = { name: document.getElementById('poi-name').value, desc: document.getElementById('poi-desc').value, audio: document.getElementById('poi-audio-url').value };
    currentPoiLang = lang; updateFormFieldsFromState();
}

function updateFormFieldsFromState() {
    const data = poiLangData[currentPoiLang];
    document.getElementById('poi-name').value = data.name;
    document.getElementById('poi-desc').value = data.desc;
    document.getElementById('poi-audio-url').value = data.audio;
}

async function savePoiData() {
    const id = document.getElementById('poi-id').value;
    const body = {
        Name: document.getElementById('poi-name-vi').value,
        Description: document.getElementById('poi-desc-vi').value,
        Category: document.getElementById('poi-category').value,
        Latitude: parseFloat(document.getElementById('poi-lat').value),
        Longitude: parseFloat(document.getElementById('poi-lng').value),
        Radius: parseInt(document.getElementById('poi-radius').value),
        IsAdsPopup: document.getElementById('poi-ads-popup').checked,
        AudioUrl: document.getElementById('poi-audio-url').value,
        AudioFile: document.getElementById('poi-audio-url').value,
        TtsScript: document.getElementById('poi-desc-vi').value,
        TtsScriptEn: document.getElementById('poi-name-en').value,
        TtsScriptZh: document.getElementById('poi-name-zh').value,
        Address: 'Bình Thạnh, TP.HCM'
    };
    const method = id ? 'PUT' : 'POST', url = id ? `${API}/restaurants/${id}` : `${API}/restaurants`;
    await fetch(url, { method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
    showToast('✅ Đã lưu thành công!', 'success'); closePoiForm(); loadPois();
}

async function deletePoiData() {
    const id = document.getElementById('poi-id').value;
    if (!id) return;
    if (!confirm('⚠️ Bạn có chắc chắn muốn xóa địa điểm này không? Thao tác này không thể hoàn tác.')) return;

    try {
        const res = await fetch(`${API}/restaurants/${id}`, { method: 'DELETE' });
        if (res.ok) {
            showToast('🗑️ Đã xóa địa điểm thành công', 'success');
            closePoiForm();
            loadPois();
        }
    } catch (e) { console.error('Delete fail:', e); }
}

function resetPoiForm() {
    document.getElementById('poi-id').value = ''; 
    document.getElementById('poi-lat').value = ''; 
    document.getElementById('poi-lng').value = '';
    document.getElementById('poi-radius').value = 50;
    document.getElementById('poi-audio-url').value = '';
    document.getElementById('poi-ads-popup').checked = false;
    
    // Clear all language fields
    ['vi','en','zh'].forEach(lang => {
        document.getElementById(`poi-name-${lang}`).value = '';
        document.getElementById(`poi-desc-${lang}`).value = '';
    });
}
function closePoiForm() { document.getElementById('poi-form-panel').classList.remove('open'); document.getElementById('panel-overlay-poi').style.display = 'none'; }

// ══ TOUR ══
async function loadTours() {
  try { const res = await fetch(`${API}/tours`); tours = await res.json(); renderTours(); } catch(e) { console.error(e); }
}
function renderTours() {
  const el = document.getElementById('tour-grid'); if (!el) return;
  el.innerHTML = tours.map((t, idx) => {
    const ps = typeof t.Pois === 'string' ? JSON.parse(t.Pois) : (t.Pois || []);
    return `
      <div class="card" style="background:#fff;border-radius:12px;overflow:hidden;border:1px solid #e2e8f0;padding:0">
        <img src="${t.ImageUrl || t.img || 'https://via.placeholder.com/400x150'}" style="width:100%;height:150px;object-fit:cover">
        <div style="padding:16px">
          <h4 style="font-weight:700">${t.Name || t.name}</h4>
          <p style="font-size:12px;color:#64748b;margin-top:6px">${t.Description || t.desc}</p>
          <div style="margin-top:16px;display:flex;justify-content:space-between;align-items:center">
            <span style="font-size:12px;font-weight:700;color:var(--primary)">📍 ${ps.length} điểm dừng</span>
            <div style="display:flex;gap:4px">
              <button class="btn btn-ghost" onclick="editTour(${idx})"><i data-lucide="edit-3"></i></button>
              <button class="btn btn-ghost" onclick="deleteTour(${t.id || t.Id})"><i data-lucide="trash-2"></i></button>
            </div>
          </div>
        </div>
      </div>`;
  }).join('');
  lucide.createIcons();
}
function showTourModal() { resetTourForm(); renderPoiChecklist(); document.getElementById('tour-form-title').textContent = '📍 Tạo hành trình mới'; document.getElementById('tour-form-panel').classList.add('open'); document.getElementById('panel-overlay-tour').style.display = 'block'; }
function editTour(idx) { 
    const t = tours[idx]; resetTourForm(); 
    document.getElementById('tour-id').value = t.id || t.Id; document.getElementById('tour-name').value = t.Name || t.name; 
    document.getElementById('tour-desc').value = t.Description || t.desc; document.getElementById('tour-img').value = t.ImageUrl || t.img; 
    selectedPois = typeof t.Pois === 'string' ? JSON.parse(t.Pois) : (t.Pois || []);
    renderPoiChecklist(); document.getElementById('tour-form-panel').classList.add('open'); document.getElementById('panel-overlay-tour').style.display = 'block'; 
}
function selectPoiInTour(id) {
    const idx = selectedPois.indexOf(id);
    if (idx > -1) selectedPois.splice(idx, 1); else selectedPois.push(id);
    renderPoiChecklist();
}
function renderPoiChecklist() { 
    const el = document.getElementById('tour-poi-checklist'); el.innerHTML = '';
    document.getElementById('selected-poi-count').textContent = selectedPois.length;
    allPois.forEach(p => {
        const pId = p.id || p.Id, ord = selectedPois.indexOf(pId) + 1;
        const div = document.createElement('div'); div.className = `poi-check-item ${ord > 0 ? 'selected' : ''}`;
        div.style = `display:flex; align-items:center; gap:8px; padding:10px; background:#fff; border-radius:8px; cursor:pointer; border:1px solid ${ord > 0 ? 'var(--primary)' : '#e2e8f0'}`;
        div.onclick = () => selectPoiInTour(pId);
        div.innerHTML = `<span style="width:24px; height:24px; display:flex; align-items:center; justify-content:center; border-radius:50%; background:${ord > 0 ? 'var(--primary)' : '#f1f5f9'}; color:${ord > 0 ? '#fff' : '#64748b'}; font-size:12px; font-weight:700">${ord > 0 ? ord : ''}</span><span style="font-size:13px; font-weight:500">${p.name || p.Name}</span>`;
        el.appendChild(div);
    });
}
async function saveTourData() {
  const id = document.getElementById('tour-id').value;
  const body = { Name: document.getElementById('tour-name').value, Description: document.getElementById('tour-desc').value, ImageUrl: document.getElementById('tour-img').value, Pois: JSON.stringify(selectedPois) };
  if (!body.Name || selectedPois.length === 0) { showToast('Nhập tên và chọn ít nhất 1 điểm', 'danger'); return; }
  const method = id ? 'PUT' : 'POST', url = id ? `${API}/tours/${id}` : `${API}/tours`;
  await fetch(url, { method, headers: {'Content-Type':'application/json'}, body: JSON.stringify(body) });
  showToast('✅ Đã lưu Tour!', 'success'); closeTourModal(); await loadTours();
}
function closeTourModal() { document.getElementById('tour-form-panel').classList.remove('open'); document.getElementById('panel-overlay-tour').style.display = 'none'; }
function resetTourForm() { ['tour-id','tour-name','tour-desc','tour-img'].forEach(id => document.getElementById(id).value = ''); selectedPois = []; }

// ══ ANALYTICS & DASHBOARD ══
async function loadAnalytics() {
  try { 
    const res = await fetch(`${API}/analytics`); 
    historyData = await res.json(); 
    renderHistory(); 
    renderDashboardRecent(); 
    renderStatsCards();
  } catch(e) { console.error(e); }
}

function renderStatsCards() {
  const el = document.getElementById('dashboard-stats'); if (!el) return;
  const totalPois = allPois.length;
  const totalTours = tours.length;
  const totalInteractions = historyData.length;
  const majorPois = allPois.filter(p => (p.category || p.Category) === 'Quán ăn').length;

  el.innerHTML = `
    <div class="stat-card">
      <div class="stat-icon blue"><i data-lucide="map-pin"></i></div>
      <div class="stat-info"><p class="text-muted">Tổng POI</p><h2 class="stat-val">${totalPois}</h2></div>
    </div>
    <div class="stat-card">
      <div class="stat-icon green"><i data-lucide="share-2"></i></div>
      <div class="stat-info"><p class="text-muted">Hành trình Tour</p><h2 class="stat-val">${totalTours}</h2></div>
    </div>
    <div class="stat-card">
      <div class="stat-icon orange"><i data-lucide="activity"></i></div>
      <div class="stat-info"><p class="text-muted">Tương tác</p><h2 class="stat-val">${totalInteractions}</h2></div>
    </div>
    <div class="stat-card">
      <div class="stat-icon" style="background:#fef2f2;color:#ef4444"><i data-lucide="utensils"></i></div>
      <div class="stat-info"><p class="text-muted">Quán ăn chính</p><h2 class="stat-val">${majorPois}</h2></div>
    </div>`;
  lucide.createIcons();
}
function renderHistory() {
  const tbody = document.getElementById('history-tbody'); if (!tbody) return;
  tbody.innerHTML = historyData.map(h => `
    <tr>
      <td style="font-weight:700">${h.RestaurantName || 'Hệ thống'}</td>
      <td><span class="badge ${h.EventType==='Click'?'badge-success':'badge-danger'}">${h.EventType || 'View'}</span></td>
      <td style="font-family:monospace;font-size:12px">POI ID: ${h.RestaurantId}</td>
      <td>${new Date(h.Timestamp).toLocaleString('vi-VN')}</td>
      <td><span style="color:#10b981">● Completed</span></td>
    </tr>`).join('');
}
function renderDashboardRecent() {
  const tbody = document.getElementById('dashboard-recent-tbody'); if (!tbody) return;
  tbody.innerHTML = historyData.slice(0, 5).map(h => `<tr><td><strong>${h.RestaurantName || 'POI'}</strong></td><td>${h.EventType}</td><td class="text-muted">${new Date(h.Timestamp).toLocaleTimeString()}</td></tr>`).join('');
}
function initCharts() {
  const ctx = document.getElementById('mainChart')?.getContext('2d');
  const ctxPie = document.getElementById('pieChart')?.getContext('2d');
  if (!ctx || !ctxPie) return;
  if (mainChart) mainChart.destroy(); if (pieChart) pieChart.destroy();
  
  const labels = historyData.slice(0, 7).map(h => new Date(h.Timestamp).toLocaleTimeString());
  const data = historyData.slice(0, 7).map(() => Math.floor(Math.random() * 50) + 10);

  mainChart = new Chart(ctx, { type: 'line', data: { labels, datasets: [{ label: 'Lượt tương tác', data, borderColor: '#2563eb', fill: true, backgroundColor: 'rgba(37, 99, 235, 0.1)', tension: 0.4 }] } });
  
  const cats = {}; allPois.forEach(p => { const c = p.category || p.Category || 'Quán ăn'; cats[c] = (cats[c] || 0) + 1; });
  pieChart = new Chart(ctxPie, { type: 'doughnut', data: { labels: Object.keys(cats), datasets: [{ data: Object.values(cats), backgroundColor: ['#2563eb', '#10b981', '#f59e0b', '#ef4444', '#6366f1'] }] } });
}

// ══ AUDIO & TRANS ══
function renderAudio() {
  const el = document.getElementById('audio-list'); if (!el) return;
  el.innerHTML = allPois.map(p => {
    const audioUrl = p.audioUrl || p.AudioUrl || (p.audioFile || p.AudioFile ? `${BASE_URL}/uploads/${p.audioFile || p.AudioFile}` : '');
    return `
    <div class="card" style="display:flex; align-items:center; gap:20px; margin-bottom:12px">
      <div style="background:#eff6ff;padding:12px;border-radius:10px;color:#2563eb"><i data-lucide="music"></i></div>
      <div style="flex:1"><strong>${p.name || p.Name}</strong><br><small>${p.category || p.Category}</small></div>
      ${audioUrl ? `<audio controls style="height:32px"><source src="${audioUrl}"></audio>` : '<span class="badge badge-danger">Thiếu File</span>'}
    </div>`;
  }).join('');
  lucide.createIcons();
}
function renderTrans() {
  const tbody = document.getElementById('trans-tbody'); if (!tbody) return;
  tbody.innerHTML = allPois.map(p => `<tr><td><strong>${p.name || p.Name}</strong></td><td><div class="trans-snippet">${p.ttsScript||'—'}</div></td><td><div class="trans-snippet">${p.ttsScriptEn||'—'}</div></td><td><div class="trans-snippet">${p.ttsScriptZh||'—'}</div></td><td><button class="btn btn-ghost" onclick="openEditPoiForm(${JSON.stringify(p).replace(/"/g, '&quot;')})">Sửa</button></td></tr>`).join('');
}

// ══ AI TRANSLATION ══
async function autoTranslate(targetLang, btn) {
    const srcName = document.getElementById('poi-name-vi').value;
    const srcDesc = document.getElementById('poi-desc-vi').value;
    
    if (!srcName && !srcDesc) {
        showToast('Vui lòng nhập nội dung Tiếng Việt trước khi dịch', 'warning');
        return;
    }

    const originalHtml = btn.innerHTML;
    btn.innerHTML = '<i data-lucide="loader-2" class="spin"></i> Đang dịch...';
    btn.disabled = true;
    lucide.createIcons();

    try {
        if (srcName) {
            const translatedName = await translateText(srcName, targetLang);
            document.getElementById(`poi-name-${targetLang}`).value = translatedName;
        }
        if (srcDesc) {
            const translatedDesc = await translateText(srcDesc, targetLang);
            document.getElementById(`poi-desc-${targetLang}`).value = translatedDesc;
        }
        showToast(`✅ Đã dịch xong sang ${targetLang.toUpperCase()}`, 'success');
    } catch (e) {
        console.error('Translation error:', e);
        showToast('❌ Lỗi dịch thuật. Vui lòng thử lại sau.', 'danger');
    } finally {
        btn.innerHTML = originalHtml;
        btn.disabled = false;
        lucide.createIcons();
    }
}

async function translateText(text, targetLang) {
    // Sử dụng Public Google Translate API (gtx endpoint)
    const url = `https://translate.googleapis.com/translate_a/single?client=gtx&sl=vi&tl=${targetLang}&dt=t&q=${encodeURI(text)}`;
    const res = await fetch(url);
    const json = await res.json();
    
    // Gộp các mảng kết quả dịch (đối với văn bản dài)
    let translated = "";
    if (json && json[0]) {
        json[0].forEach(part => {
            if (part[0]) translated += part[0];
        });
    }
    return translated || text;
}

// ══ HELPERS ══
function getImgUrl(p) { const img = p.imageUrl || p.ImageUrl; if (!img) return 'https://via.placeholder.com/400x200?text=No+Image'; if (img.startsWith('http') || img.startsWith('data:')) return img; return `${BASE_URL}/${img}`; }
function showToast(msg, type='success') { const container = document.getElementById('toast-container'); const toast = document.createElement('div'); toast.className = `toast ${type}`; toast.innerHTML = `<i data-lucide="${type==='success'?'check-circle':'alert-circle'}"></i> <span>${msg}</span>`; container.appendChild(toast); lucide.createIcons(); setTimeout(() => toast.remove(), 3000); }
function logout() { sessionStorage.removeItem('cms_logged_in'); window.location.href='login.html'; }
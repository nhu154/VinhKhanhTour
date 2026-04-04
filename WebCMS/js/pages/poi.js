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
  if (q) list = list.filter(p => textMatch(p.name||p.Name, q));

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
      <img src="${getImgUrl(p)}" onerror="this.onerror=null; this.src='data:image/svg+xml;charset=utf-8,'+encodeURIComponent('<svg xmlns=\\'http://www.w3.org/2000/svg\\' width=\\'40\\' height=\\'40\\'><rect width=\\'100%\\' height=\\'100%\\' fill=\\'#f1f5f9\\'/><text x=\\'50%\\' y=\\'50%\\' fill=\\'#94a3b8\\' font-size=\\'14\\' font-family=\\'sans-serif\\' text-anchor=\\'middle\\' dy=\\'5\\'>?</text></svg>')"
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
  const _role = sessionStorage.getItem('cms_role') || 'user';
  document.getElementById('btn-save-poi').innerHTML = _role === 'owner'
    ? '<i data-lucide="send"></i> Gửi yêu cầu tạo' : '<i data-lucide="plus"></i> Tạo điểm mới';
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
  document.getElementById('poi-description').value = p.description || p.Description || '';
  const _roleEdit = sessionStorage.getItem('cms_role') || 'user';
  document.getElementById('btn-save-poi').innerHTML = _roleEdit === 'owner'
    ? '<i data-lucide="send"></i> Gửi yêu cầu sửa' : '<i data-lucide="save"></i> Lưu thay đổi';
  document.getElementById('btn-delete-poi').style.display = _roleEdit === 'admin' ? 'flex' : 'none';

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
  const role = sessionStorage.getItem('cms_role') || 'user';
  if (role === 'user') { showToast('⛔ Bạn không có quyền thực hiện thao tác này', 'danger'); return; }

  const id = document.getElementById('poi-id').value;
  const nameVi = document.getElementById('poi-lang-vi-name')?.value;
  if (!nameVi) { showToast('Vui lòng nhập Tên điểm', 'warning'); return; }

  // Chủ quán → gửi yêu cầu phê duyệt thay vì lưu thẳng
  if (role === 'owner') {
    const btn = document.getElementById('btn-save-poi');
    btn.innerHTML = '<i data-lucide="loader-2" class="spin"></i> Đang gửi...';
    btn.disabled = true;
    try {
      const getTts = (code) => document.getElementById(`poi-lang-${code}-tts`)?.value || '';
      const poiData = {
        Name: nameVi,
        Description: getTts('vi'),
        Category: document.getElementById('poi-category').value,
        Latitude: parseFloat(document.getElementById('poi-lat').value)||0,
        Longitude: parseFloat(document.getElementById('poi-lng').value)||0,
        Rating: parseFloat(document.getElementById('poi-rating').value)||4.0,
        OpenHours: document.getElementById('poi-hours').value,
        Address: document.getElementById('poi-address').value||'Vĩnh Khánh, Phường 8, Quận 4',
        Radius: parseInt(document.getElementById('poi-radius').value)||50,
        ImageUrl: document.getElementById('poi-image-url')?.value||'',
        TtsScript: getTts('vi'), TtsScriptEn: getTts('en'), TtsScriptZh: getTts('zh'),
      };
      const action = id ? 'update_info' : 'create_poi';
      const locName = nameVi;
      const ok = await submitForApproval(poiData, action, id ? parseInt(id) : null, locName);
      if (ok) {
        showToast('📨 Đã gửi yêu cầu! Chờ admin phê duyệt', 'info');
        closePoiForm();
      } else {
        showToast('❌ Lỗi gửi yêu cầu', 'danger');
      }
    } finally {
      const btn2 = document.getElementById('btn-save-poi');
      if (btn2) { btn2.innerHTML = id ? '<i data-lucide="save"></i> Lưu thay đổi' : '<i data-lucide="plus"></i> Tạo điểm mới'; btn2.disabled = false; lucide.createIcons(); }
    }
    return;
  }

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
    Description: document.getElementById('poi-description')?.value || getTts('vi'),
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
      if (activePage === 'page-audio') { if(typeof renderAudioPage === 'function') renderAudioPage(); }
    }
  } catch(e) { showToast('❌ Lỗi khi lưu dữ liệu', 'danger'); }
  finally {
    btn.innerHTML = id ? '<i data-lucide="save"></i> Lưu thay đổi' : '<i data-lucide="plus"></i> Tạo điểm mới';
    btn.disabled = false; lucide.createIcons();
  }
}

async function deletePoiData() {
  const role = sessionStorage.getItem('cms_role') || 'user';
  if (role !== 'admin') { showToast('⛔ Chỉ Admin mới có quyền xóa địa điểm', 'danger'); return; }
  const id = document.getElementById('poi-id').value;
  if (!id) return;
  if (!(await showConfirm('Xóa địa điểm', '⚠️ Bạn có chắc chắn muốn xóa địa điểm này? Thao tác không thể hoàn tác.', 'danger'))) return;
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


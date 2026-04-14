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
    const imgUrl = t.ImageUrl||t.img||'';
    const imgSrc = imgUrl ? (imgUrl.startsWith('http') || imgUrl.startsWith('data:') ? imgUrl : `${BASE_URL}/${imgUrl}`) : 'https://via.placeholder.com/400x160/f1f5f9/94a3b8?text=Tour';
    return `
    <div class="tour-card">
      <div style="position:relative">
        <img class="tour-card-img" src="${imgSrc}" onerror="this.src='https://via.placeholder.com/400x160/f1f5f9/94a3b8?text=Tour'">
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

// ── Drop & Preview Tour Image ──
function previewTourImage(input) {
  if (!input.files || !input.files[0]) return;
  const file = input.files[0];
  if (file.size > 3*1024*1024) { showToast('File quá lớn (Tối đa 3MB)','warning'); return; }
  
  const reader = new FileReader();
  reader.onload = function(e) {
    const base64 = e.target.result;
    document.getElementById('tour-img').value = base64;
    document.getElementById('tour-image-preview').src = base64;
    document.getElementById('tour-image-placeholder').style.display = 'none';
    document.getElementById('tour-image-preview').style.display = 'block';
    document.getElementById('tour-image-filename').textContent = `📎 ${file.name}`;
  };
  reader.readAsDataURL(file);
}

function handleTourImageDrop(event) {
  event.preventDefault();
  event.stopPropagation();
  const dropzone = document.getElementById('tour-image-dropzone');
  if (dropzone) { dropzone.style.borderColor = '#cbd5e1'; dropzone.style.background = '#f8fafc'; }
  const file = event.dataTransfer?.files?.[0];
  if (!file) {
    const url = event.dataTransfer.getData('text/plain') || event.dataTransfer.getData('text/uri-list');
    if (url && (url.startsWith('http') || url.startsWith('data:'))) {
      previewTourUrl(url);
      document.getElementById('tour-img').value = url;
    }
    return;
  }
  if (!file.type.startsWith('image/')) return;
  previewTourImage({ files: [file] });
}

function clearTourImage() {
  document.getElementById('tour-image-file').value = '';
  document.getElementById('tour-img').value = '';
  document.getElementById('tour-image-preview').src = '';
  document.getElementById('tour-image-preview').style.display = 'none';
  document.getElementById('tour-image-placeholder').style.display = 'block';
  document.getElementById('tour-image-filename').textContent = '';
}

function previewTourUrl(url) {
  if (!url || url.startsWith('data:')) return;
  const preview = document.getElementById('tour-image-preview');
  if (!url.startsWith('http') && !url.startsWith('/')) return;
  preview.src = url.startsWith('http') ? url : `${BASE_URL}/${url}`;
  preview.style.display = 'block';
  document.getElementById('tour-image-placeholder').style.display = 'none';
  document.getElementById('tour-image-filename').textContent = `🔗 URL Ảnh`;
}

function editTour(idx) {
  const t = tours[idx]; resetTourForm();
  document.getElementById('tour-id').value = t.Id||t.id||'';
  document.getElementById('tour-name').value = t.Name||t.name||'';
  document.getElementById('tour-desc').value = t.Description||t.desc||'';
  document.getElementById('tour-duration').value = t.Duration||t.duration||'';
  document.getElementById('tour-rating').value = t.Rating||t.rating||4.0;
  document.getElementById('tour-emoji').value = t.Emoji||t.emoji||'🍜';
  
  const imgUrl = t.ImageUrl||t.img||'';
  document.getElementById('tour-img').value = imgUrl;
  const nameEnEl = document.getElementById('tour-name-en'); if(nameEnEl) nameEnEl.value = t.NameEn||t.nameEn||'';
  const nameZhEl = document.getElementById('tour-name-zh'); if(nameZhEl) nameZhEl.value = t.NameZh||t.nameZh||'';
  const nameKoEl = document.getElementById('tour-name-ko'); if(nameKoEl) nameKoEl.value = t.NameKo||t.nameKo||'';
  const descEnEl = document.getElementById('tour-desc-en'); if(descEnEl) descEnEl.value = t.DescEn||t.descEn||'';
  const descZhEl = document.getElementById('tour-desc-zh'); if(descZhEl) descZhEl.value = t.DescZh||t.descZh||'';
  const descKoEl = document.getElementById('tour-desc-ko'); if(descKoEl) descKoEl.value = t.DescKo||t.descKo||'';
  if (imgUrl) {
    const src = imgUrl.startsWith('http') || imgUrl.startsWith('data:') ? imgUrl : `${BASE_URL}/${imgUrl}`;
    document.getElementById('tour-image-preview').src = src;
    document.getElementById('tour-image-preview').style.display = 'block';
    document.getElementById('tour-image-placeholder').style.display = 'none';
  }
  
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
  
  // Parse rating correctly to handle "," inputs in some browsers
  const ratingStr = document.getElementById('tour-rating').value.replace(',', '.');
  const parsedRating = parseFloat(ratingStr) || 4.0;

  const body = {
    Name: name,
    NameEn: document.getElementById('tour-name-en')?.value || '',
    NameZh: document.getElementById('tour-name-zh')?.value || '',
    NameKo: document.getElementById('tour-name-ko')?.value || '',
    Description: document.getElementById('tour-desc').value,
    DescEn: document.getElementById('tour-desc-en')?.value || '',
    DescZh: document.getElementById('tour-desc-zh')?.value || '',
    DescKo: document.getElementById('tour-desc-ko')?.value || '',
    Duration: document.getElementById('tour-duration').value,
    Rating: parsedRating,
    Emoji: document.getElementById('tour-emoji').value||'🍜',
    ImageUrl: document.getElementById('tour-img').value,
    Pois: JSON.stringify(selectedPois), IsActive: true
  };
  try {
    const method=id?'PUT':'POST', url=id?`${API}/tours/${id}`:`${API}/tours`;
    await fetch(url,{method,headers:getAdminHeaders(),body:JSON.stringify(body)});
    showToast('✅ Đã lưu Tour!','success'); closeTourModal(); await loadTours(); renderTours(); renderStatsCards();
  } catch(e) { showToast('❌ Lỗi khi lưu Tour','danger'); }
}

async function deleteTour(id) {
  if (!(await showConfirm('Xóa Tour', 'Bạn có chắc chắn muốn xóa hành trình tour này không?', 'danger'))) return;
  try {
    const res = await fetch(`${API}/tours/${id}`,{method:'DELETE',headers:getAdminHeaders()});
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    showToast('🗑️ Đã xóa Tour','success');
    await loadTours();
    renderTours();
    renderStatsCards();
  } catch(e) {
    console.error('[deleteTour]', e);
    showToast('❌ Lỗi khi xóa: ' + e.message,'danger');
  }
}

function closeTourModal() { document.getElementById('tour-form-panel').classList.remove('open'); document.getElementById('panel-overlay-tour').style.display='none'; }
function resetTourForm() {
  ['tour-id','tour-name','tour-name-en','tour-name-zh','tour-name-ko','tour-desc','tour-desc-en','tour-desc-zh','tour-desc-ko','tour-duration','tour-img','tour-emoji'].forEach(id=>{const el=document.getElementById(id);if(el)el.value='';});
  const r=document.getElementById('tour-rating');if(r)r.value=4.0; selectedPois=[];
  clearTourImage();
}
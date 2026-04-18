// ══ AUTO TRANSLATE LANG ══

function removeVietnameseTones(str) {
    str = str.replace(/à|á|ạ|ả|ã|â|ầ|ấ|ậ|ẩ|ẫ|ă|ằ|ắ|ặ|ẳ|ẵ/g,"a"); 
    str = str.replace(/è|é|ẹ|ẻ|ẽ|ê|ề|ế|ệ|ể|ễ/g,"e"); 
    str = str.replace(/ì|í|ị|ỉ|ĩ/g,"i"); 
    str = str.replace(/ò|ó|ọ|ỏ|õ|ô|ồ|ố|ộ|ổ|ỗ|ơ|ờ|ớ|ợ|ở|ỡ/g,"o"); 
    str = str.replace(/ù|ú|ụ|ủ|ũ|ư|ừ|ứ|ự|ử|ữ/g,"u"); 
    str = str.replace(/ỳ|ý|ỵ|ỷ|ỹ/g,"y"); 
    str = str.replace(/đ/g,"d");
    str = str.replace(/À|Á|Ạ|Ả|Ã|Â|Ầ|Ấ|Ậ|Ẩ|Ẫ|Ă|Ằ|Ắ|Ặ|Ẳ|Ẵ/g, "A");
    str = str.replace(/È|É|Ẹ|Ẻ|Ẽ|Ê|Ề|Ế|Ệ|Ể|Ễ/g, "E");
    str = str.replace(/Ì|Í|Ị|Ỉ|Ĩ/g, "I");
    str = str.replace(/Ò|Ó|Ọ|Ỏ|Õ|Ô|Ồ|Ố|Ộ|Ổ|Ỗ|Ơ|Ờ|Ớ|Ợ|Ở|Ỡ/g, "O");
    str = str.replace(/Ù|Ú|Ụ|Ủ|Ũ|Ư|Ừ|Ứ|Ự|Ử|Ữ/g, "U");
    str = str.replace(/Ỳ|Ý|Ỵ|Ỷ|Ỹ/g, "Y");
    str = str.replace(/Đ/g, "D");
    return str;
}

async function autoTranslateLang(targetLang, btn) {
  const langs = getLangs();
  const defLang = langs.find(l => l.isDefault) || langs[0];
  const srcTts = document.getElementById(`poi-lang-${defLang.code}-tts`)?.value || '';
  const srcName = document.getElementById(`poi-lang-${defLang.code}-name`)?.value || '';
  if (!srcTts && !srcName) { showToast(`Nhập nội dung ${defLang.flag} ${defLang.name} trước`, 'warning'); return; }

  const orig = btn.innerHTML; btn.innerHTML = '<i data-lucide="loader-2" class="spin"></i> Đang dịch...'; btn.disabled = true; lucide.createIcons();
  try {
    if (srcName) {
      // Dịch Tên thì bỏ dấu thay vì Google Translate để giữ đúng định danh quán
      const t = removeVietnameseTones(srcName);
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

  // Đồng bộ lên server
  fetch(`${API}/languages`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ code, name, flag })
  }).catch(e => console.warn('[Lang] API sync thất bại:', e));

  showToast(`Đã thêm ngôn ngữ ${flag} ${name}`, 'success');
  closeLangPanel();
  renderLangList();
  renderTransList();
  renderAudioPage(); // Cập nhật Audio Manager ngay
}

async function deleteLang(idx) {
  const langs = getLangs();
  const lang = langs[idx];
  if (!(await showConfirm('Xóa ngôn ngữ', `Bạn có chắc muốn xóa ngôn ngữ "${lang.name}"?`, 'warning'))) return;
  langs.splice(idx, 1);
  saveLangs(langs);

  // Đồng bộ lên server
  fetch(`${API}/languages/${lang.code}`, { method: 'DELETE' })
    .catch(e => console.warn('[Lang] API delete thất bại:', e));

  showToast('Đã xóa ngôn ngữ', 'success');
  renderLangList();
  renderTransList();
  renderAudioPage(); // Cập nhật Audio Manager ngay
}

// Load ngôn ngữ từ API khi vào trang (merge vào localStorage)
async function loadLangsFromApi() {
  try {
    const res = await fetch(`${API}/languages`);
    if (!res.ok) return;
    const serverLangs = await res.json();
    if (!Array.isArray(serverLangs) || serverLangs.length === 0) return;

    const mapped = serverLangs.map(l => ({
      code: (l.code || l.Code || '').toLowerCase(),
      name: l.name || l.Name || '',
      flag: l.flag || l.Flag || '🌐',
      isDefault: !!(l.isDefault ?? l.IsDefault)
    })).filter(l => l.code);

    saveLangs(mapped);
    renderLangList();
    renderAudioPage();
  } catch (e) {
    console.warn('[Lang] Load từ API thất bại:', e);
  }
}

// ══ BẢNG DỊCH (DƯỚI QUẢN LÝ NGÔN NGỮ) ══
function renderTransList() {
  const tbody = document.getElementById('trans-tbody');
  if (!tbody) return;
  const q = (document.getElementById('search-trans-input')?.value || '').toLowerCase();
  
  const langs = getLangs();
  let html = '';
  allPois.filter(p => textMatch(p.name||p.Name, q)).forEach((p) => {
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
      const opacity = hasText ? '1' : '0.4';
      
      return `<div style="display:inline-flex;align-items:center;opacity:${opacity};margin-right:8px;background:#f1f5f9;padding:2px 8px;border-radius:12px;font-size:12px;border:1px solid #cbd5e1">
                <span style="font-size:14px;margin-right:4px">${l.flag}</span> <span style="font-weight:600;color:var(--text-main)">${l.code.toUpperCase()}</span>
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


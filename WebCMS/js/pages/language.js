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


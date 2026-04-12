// PHÊ DUYỆT (APPROVAL WORKFLOW)
// ══════════════════════════════════════════════════
let allApprovals = [];
let approvalStatusFilter = '';

async function loadApprovals() {
  const role = sessionStorage.getItem('cms_role') || 'user';
  if (role !== 'admin') return;
  try {
    const res = await fetch(`${API}/approvals`);
    allApprovals = await res.json();
    const pending = allApprovals.filter(a => (a.Status||a.status) === 'pending').length;
    const badge = document.getElementById('approval-badge');
    if (badge) {
      badge.textContent = pending;
      badge.style.display = pending > 0 ? 'inline-flex' : 'none';
    }
  } catch(e) { allApprovals = []; }
}

async function loadPendingBadge() {
  try {
    const res = await fetch(`${API}/approvals/count/pending`);
    const data = await res.json();
    const badge = document.getElementById('approval-badge');
    if (badge) {
      badge.textContent = data.count || 0;
      badge.style.display = (data.count > 0) ? 'inline-flex' : 'none';
    }
  } catch(e) {}
}

function filterApproval(status, el) {
  approvalStatusFilter = status;
  document.querySelectorAll('#page-approvals .filter-chip').forEach(c => c.classList.remove('active'));
  if (el) el.classList.add('active');
  renderApprovalList();
}

function renderApprovalList() {
  const container = document.getElementById('approval-list');
  if (!container) return;
  let list = allApprovals;
  if (approvalStatusFilter) list = list.filter(a => (a.Status||a.status) === approvalStatusFilter);

  if (!list.length) {
    container.innerHTML = `<div class="empty-state"><i data-lucide="check-circle"></i><p>${approvalStatusFilter === 'pending' ? 'Không có yêu cầu nào đang chờ duyệt' : 'Chưa có dữ liệu'}</p></div>`;
    lucide.createIcons(); return;
  }

  const ACTION_LABEL = {
    create_poi:   '<span style="display:flex;align-items:center;gap:4px"><i data-lucide="plus-circle" style="width:12px;height:12px"></i> Tạo POI mới</span>',
    update_info:  '<span style="display:flex;align-items:center;gap:4px"><i data-lucide="edit-3" style="width:12px;height:12px"></i> Sửa thông tin</span>',
    update_audio: '<span style="display:flex;align-items:center;gap:4px"><i data-lucide="mic" style="width:12px;height:12px"></i> Sửa Audio/TTS</span>',
    update_image: '<span style="display:flex;align-items:center;gap:4px"><i data-lucide="image" style="width:12px;height:12px"></i> Đổi ảnh</span>'
  };
  const STATUS_BADGE = {
    pending:  '<span class="badge badge-warning" style="display:inline-flex;align-items:center;gap:4px"><i data-lucide="clock" style="width:12px;height:12px"></i> Chờ duyệt</span>',
    approved: '<span class="badge badge-success" style="display:inline-flex;align-items:center;gap:4px"><i data-lucide="check-circle" style="width:12px;height:12px"></i> Đã duyệt</span>',
    rejected: '<span class="badge badge-danger" style="display:inline-flex;align-items:center;gap:4px"><i data-lucide="x-circle" style="width:12px;height:12px"></i> Từ chối</span>'
  };

  container.innerHTML = list.map(a => {
    const id       = a.Id || a.id;
    const action   = a.Action || a.action || '';
    const status   = a.Status || a.status || 'pending';
    const user     = a.UserName || a.userName || '—';
    const locName  = a.LocationName || a.locationName || '(POI mới)';
    const created  = new Date(a.CreatedAt || a.createdAt).toLocaleString('vi-VN');
    const note     = a.AdminNote || a.adminNote || '';
    const isPending = status === 'pending';

    return `
    <div style="background:var(--card);border:1px solid var(--border);border-radius:var(--radius-lg);padding:18px;margin-bottom:12px;transition:.2s"
         onmouseover="this.style.borderColor='#bfdbfe'" onmouseout="this.style.borderColor='var(--border)'">
      <div style="display:flex;align-items:flex-start;gap:14px">
        <div style="width:42px;height:42px;background:${isPending?'#fff7ed':'#f8fafc'};border:1px solid ${isPending?'#fed7aa':'var(--border)'};border-radius:10px;display:flex;align-items:center;justify-content:center;color:${isPending?'#f97316':'#64748b'};flex-shrink:0">
          ${action.includes('audio')?'<i data-lucide="mic"></i>':action.includes('create')?'<i data-lucide="map-pin"></i>':action.includes('image')?'<i data-lucide="image"></i>':'<i data-lucide="edit-3"></i>'}
        </div>
        <div style="flex:1;min-width:0">
          <div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;margin-bottom:4px">
            <span style="font-weight:700;font-size:14px">${locName}</span>
            ${STATUS_BADGE[status] || ''}
            <span class="badge badge-neutral" style="font-size:10px">${ACTION_LABEL[action]||action}</span>
          </div>
          <div style="font-size:12px;color:var(--text-muted);display:flex;align-items:center;gap:10px">
            <span style="display:flex;align-items:center;gap:4px"><i data-lucide="user" style="width:13px;height:13px"></i> <strong>${user}</strong></span>
            <span style="color:var(--border)">|</span>
            <span style="display:flex;align-items:center;gap:4px"><i data-lucide="calendar" style="width:13px;height:13px"></i> ${created}</span>
          </div>
          ${note ? `<div style="margin-top:8px;font-size:12px;background:#f8fafc;padding:8px 12px;border-radius:6px;color:var(--text-muted);display:flex;align-items:flex-start;gap:6px"><i data-lucide="message-square" style="width:14px;height:14px;margin-top:2px;color:#94a3b8"></i> <div>${note}</div></div>` : ''}
        </div>
        <div style="display:flex;gap:6px;flex-shrink:0">
          <button class="btn btn-ghost btn-sm" onclick="viewApprovalDetail(${id})">
            <i data-lucide="eye"></i> Xem
          </button>
          ${isPending ? `
          <button class="btn btn-primary btn-sm" onclick="quickApprove(${id})">
            <i data-lucide="check"></i> Duyệt
          </button>
          <button class="btn btn-danger-outline btn-sm" onclick="quickReject(${id})">
            <i data-lucide="x"></i> Từ chối
          </button>` : ''}
        </div>
      </div>
    </div>`;
  }).join('');
  lucide.createIcons();
}

async function viewApprovalDetail(id) {
  const a = allApprovals.find(x => (x.Id||x.id) === id);
  if (!a) return;
  const newData = JSON.parse(a.RequestData || a.requestData || '{}');
  const oldData = JSON.parse(a.OldData || a.oldData || '{}');
  const isPending = (a.Status||a.status) === 'pending';

  const diffField = (label, key) => {
    const nv = newData[key] || '—';
    const ov = oldData[key];
    const changed = ov && ov !== nv;
    return `<div style="margin-bottom:10px">
      <div style="font-size:11px;font-weight:600;color:var(--text-muted);margin-bottom:4px;text-transform:uppercase">${label}</div>
      ${changed ? `<div style="font-size:11px;color:#dc2626;text-decoration:line-through;background:#fef2f2;padding:4px 8px;border-radius:4px;margin-bottom:3px">${ov}</div>` : ''}
      <div style="font-size:13px;background:${changed?'#f0fdf4':'#f8fafc'};padding:6px 10px;border-radius:6px;color:${changed?'#15803d':'var(--text-main)'}">${nv}</div>
    </div>`;
  };

  const body = document.getElementById('approval-panel-body');
  body.innerHTML = `
    <div style="margin-bottom:16px;padding:12px;background:#fffbeb;border:1px solid #fde68a;border-radius:10px;font-size:12px">
      <strong>Người gửi:</strong> ${a.UserName||a.userName} &nbsp;·&nbsp;
      <strong>Thao tác:</strong> ${a.Action||a.action} &nbsp;·&nbsp;
      <strong>Địa điểm:</strong> ${a.LocationName||a.locationName||'(mới)'}
    </div>
    ${diffField('Tên điểm','Name')}
    ${diffField('Mô tả','Description')}
    ${diffField('Danh mục','Category')}
    ${diffField('Địa chỉ','Address')}
    ${diffField('Giờ mở cửa','OpenHours')}
    ${diffField('Rating','Rating')}
    ${diffField('TTS Tiếng Việt','TtsScript')}
    ${diffField('TTS English','TtsScriptEn')}
    ${diffField('TTS 中文','TtsScriptZh')}
    ${newData.ImageUrl && newData.ImageUrl !== oldData.ImageUrl ? `
    <div style="margin-bottom:10px">
      <div style="font-size:11px;font-weight:600;color:var(--text-muted);margin-bottom:4px;text-transform:uppercase">Ảnh mới</div>
      <img src="${newData.ImageUrl.startsWith('http')||newData.ImageUrl.startsWith('data:') ? newData.ImageUrl : BASE_URL+'/'+newData.ImageUrl}"
           style="max-width:100%;border-radius:8px;max-height:180px;object-fit:cover">
    </div>` : ''}
    ${isPending ? `
    <div style="margin-top:16px">
      <label style="font-size:12px;font-weight:600;color:var(--text-muted);display:block;margin-bottom:6px">Ghi chú (tùy chọn)</label>
      <textarea class="form-control" id="approval-note" rows="2" placeholder="Lý do từ chối hoặc ghi chú..."></textarea>
    </div>` : (a.AdminNote||a.adminNote ? `<div style="margin-top:12px;padding:10px;background:#f8fafc;border-radius:8px;font-size:12px"><strong>Ghi chú admin:</strong> ${a.AdminNote||a.adminNote}</div>` : '')}
  `;

  const footer = document.getElementById('approval-panel-footer');
  footer.innerHTML = isPending ? `
    <button class="btn btn-ghost" onclick="closeApprovalPanel()" style="flex:1">Đóng</button>
    <button class="btn btn-danger-outline" onclick="rejectApproval(${id})" style="flex:1"><i data-lucide="x"></i> Từ chối</button>
    <button class="btn btn-primary" onclick="approveApproval(${id})" style="flex:2"><i data-lucide="check"></i> Duyệt & Áp dụng</button>
  ` : `<button class="btn btn-ghost" onclick="closeApprovalPanel()" style="width:100%">Đóng</button>`;

  document.getElementById('approval-panel-title').textContent = `Yêu cầu #${id}`;
  document.getElementById('approval-panel').classList.add('open');
  document.getElementById('panel-overlay-approval').style.display = 'block';
  lucide.createIcons();
}

function closeApprovalPanel() {
  document.getElementById('approval-panel').classList.remove('open');
  document.getElementById('panel-overlay-approval').style.display = 'none';
}

async function approveApproval(id) {
  const note = document.getElementById('approval-note')?.value || '';
  await reviewApproval(id, 'approved', note);
}
async function rejectApproval(id) {
  const note = document.getElementById('approval-note')?.value || '';
  if (!note) { showToast('Nhập lý do từ chối', 'warning'); return; }
  await reviewApproval(id, 'rejected', note);
}
async function quickApprove(id) { await reviewApproval(id, 'approved', ''); }
async function quickReject(id) {
  const note = await showPrompt('Từ chối yêu cầu', 'Vui lòng nhập lý do từ chối:');
  if (note === null) return;
  await reviewApproval(id, 'rejected', note);
}

async function reviewApproval(id, status, note) {
  try {
    const res = await fetch(`${API}/approvals/${id}/review`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ Status: status, AdminNote: note, ReviewedBy: 1 })
    });
    const data = await res.json();
    showToast(data.message || (status==='approved'?'✅ Đã duyệt!':'❌ Đã từ chối'), status==='approved'?'success':'warning');
    closeApprovalPanel();
    await loadApprovals();
    renderApprovalList();
    if (status === 'approved') { await loadPois(); renderMarkers(); }
  } catch(e) { showToast('❌ Lỗi kết nối API', 'danger'); }
}

// ── Chủ quán gửi yêu cầu thay vì lưu thẳng ──
async function submitForApproval(poiData, action, locationId, locationName) {
  const userId   = parseInt(sessionStorage.getItem('cms_userid') || '0');
  const userName = sessionStorage.getItem('cms_fullname') || 'Chủ quán';

  // Lấy data cũ nếu update
  let oldData = '{}';
  if (locationId) {
    const old = allPois.find(p => (p.id||p.Id) === locationId);
    if (old) oldData = JSON.stringify(old);
  }

  const body = {
    UserId: userId, UserName: userName,
    LocationId: locationId || null, LocationName: locationName || '',
    Action: action,
    RequestData: JSON.stringify(poiData),
    OldData: oldData
  };

  const res = await fetch(`${API}/approvals`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body)
  });
  return res.ok;
}

// ── Load my locations cho chủ quán ──
async function loadMyLocations() {
  let userId = parseInt(sessionStorage.getItem('cms_userid') || '0');

  // Nếu userId = 0 (login cũ chưa có id), thử resolve từ username qua API
  if (!userId || userId === 0) {
    const username = sessionStorage.getItem('cms_username') || '';
    if (username) {
      try {
        const res = await fetch(`${API}/auth/users`);
        const users = await res.json();
        const me = users.find(u =>
          (u.username || u.Username || '').toLowerCase() === username.toLowerCase()
        );
        if (me) {
          userId = me.id || me.Id || 0;
          sessionStorage.setItem('cms_userid', String(userId));
        }
      } catch(e) { console.warn('loadMyLocations: không resolve được userId', e); }
    }
    if (!userId) {
      console.warn('loadMyLocations: userId vẫn = 0, bỏ qua');
      return;
    }
  }

  try {
    const res = await fetch(`${API}/approvals/my-locations/${userId}`);
    const myLocs = await res.json();
    // Override allPois với chỉ quán của mình
    allPois = Array.isArray(myLocs) ? myLocs : [];
    renderMarkers();
    updatePOIBadge();
    // Trigger owner dashboard render if on that page
    const activePage = document.querySelector('.main-content.active')?.id;
    if (activePage === 'page-owner-dashboard') renderOwnerDashboard();
  } catch(e) { console.error('loadMyLocations:', e); }
}

async function clearApprovalsHistory() {
  if (!(await showConfirm('Xóa lịch sử phê duyệt', 'Bạn có chắc muốn XÓA TẤT CẢ yêu cầu phê duyệt? Hành động này không thể hoàn tác.', 'danger'))) return;
  
  try {
    const res = await fetch(`${API}/approvals/clear`, { method: 'DELETE' });
    const data = await res.json();
    showToast(data.message || 'Đã dọn sạch danh sách phê duyệt', 'success');
    
    // Reset local data and UI
    allApprovals = [];
    renderApprovalList();
    
    // Update badge (should be 0 now)
    const badge = document.getElementById('approval-badge');
    if (badge) {
      badge.textContent = '0';
      badge.style.display = 'none';
    }
  } catch (e) {
    showToast('❌ Lỗi khi dọn dẹp dữ liệu', 'danger');
  }
}
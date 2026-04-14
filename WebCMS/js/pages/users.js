/**
 * ══ USERS DOMAIN ══
 * Handles fetching, rendering, and managing CMS user accounts.
 */

let allUsers = [];
let _activeUserRole = ''; // '' = Tất cả, 'admin', 'user'

async function loadUsers() {
  try {
    const res = await fetch(`${API}/auth/users`);
    if (!res.ok) throw new Error('Failed to fetch users');
    allUsers = await res.json();
    renderUserTable();
    updateUserBadge();
  } catch (err) {
    console.error('loadUsers:', err);
    showToast('Không tải được danh sách người dùng', 'error');
  }
}

function renderUserTable() {
  const tbody = document.getElementById('user-tbody');
  if (!tbody) return;

  const q = (document.getElementById('user-search')?.value || '').toLowerCase().trim();
  let filtered = allUsers;

  // Apply role filter
  if (_activeUserRole) {
    filtered = filtered.filter(u => {
      let r = (u.role || u.Role || 'user').toLowerCase().trim();
      // In UI, 'owner' is grouped under 'user'
      if (r === 'owner') r = 'user';
      return r === _activeUserRole;
    });
  }

  // Apply search query
  if (q) {
    filtered = filtered.filter(u => 
      textMatch(u.fullname || u.FullName, q) || 
      textMatch(u.username || u.Username, q)
    );
  }

  if (!filtered.length) {
    tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;padding:40px;color:var(--text-muted)">Không tìm thấy người dùng</td></tr>`;
    return;
  }

  tbody.innerHTML = filtered.map(u => {
    const role = u.role || u.Role || 'user';
    const id = u.id || u.Id;
    const roleLabel = role === 'admin' ? '👑 Admin' : '👤 User';
    const statusLabel = u.isActive !== false ? '<span class="badge badge-success">Sẵn sàng</span>' : '<span class="badge badge-danger">Đã khóa</span>';
    
    return `
      <tr>
        <td>
          <div style="display:flex;align-items:center;gap:10px">
            <div style="width:32px;height:32px;border-radius:50%;background:#f1f5f9;display:flex;align-items:center;justify-content:center;font-weight:700;color:#64748b;font-size:12px">
              ${(u.fullname || u.FullName || '?')[0].toUpperCase()}
            </div>
            <div style="font-weight:600;font-size:13px">${u.fullname || u.FullName}</div>
          </div>
        </td>
        <td style="font-family:monospace;font-size:12px;color:var(--text-muted)">${u.username || u.Username}</td>
        <td><span style="font-size:12px">${roleLabel}</span></td>
        <td>${statusLabel}</td>
        <td style="font-size:12px;color:var(--text-muted)">${new Date(u.createdAt || u.CreatedAt || Date.now()).toLocaleDateString('vi-VN')}</td>
        <td>
          <button class="btn btn-ghost btn-sm" onclick="openEditUserBox(${JSON.stringify(u).replace(/"/g,'&quot;')})">
            <i data-lucide="edit-3"></i>
          </button>
        </td>
      </tr>`;
  }).join('');
  lucide.createIcons();
}

function filterUserRole(role, el) {
  _activeUserRole = role;
  document.querySelectorAll('#page-users .filter-chip').forEach(c => c.classList.remove('active'));
  if (el) el.classList.add('active');
  renderUserTable();
}

function updateUserBadge() {
  const badge = document.getElementById('user-badge');
  if (badge) badge.textContent = allUsers ? allUsers.length : 0;
}

function openUserPanel() {
  document.getElementById('user-id').value = '';
  document.getElementById('user-fullname').value = '';
  document.getElementById('user-username').value = '';
  document.getElementById('user-username').disabled = false;
  document.getElementById('user-password').value = '';
  document.getElementById('user-password-edit').value = '';
  document.getElementById('user-role').value = 'user';
  
  document.getElementById('user-pw-group').style.display = 'block';
  document.getElementById('user-pw-hint').style.display = 'none';
  document.getElementById('user-panel-title').textContent = 'Thêm người dùng';
  document.getElementById('btn-delete-user').style.display = 'none';
  
  document.getElementById('user-form-panel').classList.add('open');
  document.getElementById('panel-overlay-user').style.display = 'block';
}

function openEditUserBox(u) {
  document.getElementById('user-id').value = u.Id || u.id;
  document.getElementById('user-fullname').value = u.FullName || u.fullname || '';
  document.getElementById('user-username').value = u.Username || u.username || '';
  document.getElementById('user-username').disabled = true; // Cannot edit username
  document.getElementById('user-password').value = '';
  document.getElementById('user-password-edit').value = '';
  document.getElementById('user-role').value = (u.Role || u.role || 'user').toLowerCase().trim();
  
  document.getElementById('user-pw-group').style.display = 'none';
  document.getElementById('user-pw-hint').style.display = 'block';
  document.getElementById('user-panel-title').textContent = 'Chỉnh sửa người dùng';
  document.getElementById('btn-delete-user').style.display = 'block';
  
  document.getElementById('user-form-panel').classList.add('open');
  document.getElementById('panel-overlay-user').style.display = 'block';
}

function closeUserPanel() {
  document.getElementById('user-form-panel').classList.remove('open');
  document.getElementById('panel-overlay-user').style.display = 'none';
}

async function saveUser() {
  const id = document.getElementById('user-id').value;
  const fullName = document.getElementById('user-fullname').value.trim();
  const username = document.getElementById('user-username').value.trim();
  const role = document.getElementById('user-role').value;
  let password = '';

  if (!id) {
    password = document.getElementById('user-password').value;
    if (!username || !password || !fullName) {
      showToast('Vui lòng điền đủ thông tin', 'warning');
      return;
    }
  } else {
    password = document.getElementById('user-password-edit').value;
    if (!fullName) {
      showToast('Vui lòng nhập họ tên', 'warning');
      return;
    }
  }

  const payload = {
    Username: username,
    Password: password,
    FullName: fullName,
    Role: role
  };

  try {
    const method = id ? 'PUT' : 'POST';
    const url = id ? `${API}/auth/users/${id}` : `${API}/auth/register`;
    const res = await fetch(url, {
      method: method,
      headers: getAdminHeaders(),
      body: JSON.stringify(payload)
    });

    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Lỗi lưu người dùng');
    
    showToast(id ? 'Đã cập nhật người dùng' : 'Tạo người dùng thành công', 'success');
    closeUserPanel();
    await loadUsers();
  } catch (err) {
    showToast(err.message, 'danger');
  }
}

async function deleteUser() {
  const id = document.getElementById('user-id').value;
  if (!id) return;
  if (!(await showConfirm('Xóa người dùng', 'Bạn có chắc chắn muốn xóa người dùng này? Tài khoản sẽ bị gỡ khỏi hệ thống.', 'danger'))) return;
  
  try {
    const res = await fetch(`${API}/auth/users/${id}`, { method: 'DELETE', headers: getAdminHeaders() });
    if (!res.ok) throw new Error('Lỗi xóa người dùng');
    
    showToast('Đã xóa người dùng', 'success');
    closeUserPanel();
    await loadUsers();
  } catch (err) {
    showToast('Khoing thể xóa người dùng', 'danger');
  }
}

function togglePw() {
  const input = document.getElementById('user-password');
  const eye = document.getElementById('pw-eye');
  if (input.type === 'password') {
    input.type = 'text';
    eye.setAttribute('data-lucide', 'eye-off');
  } else {
    input.type = 'password';
    eye.setAttribute('data-lucide', 'eye');
  }
  lucide.createIcons();
}

function togglePwEdit() {
  const input = document.getElementById('user-password-edit');
  const eye = document.getElementById('pw-eye-edit');
  if (input.type === 'password') {
    input.type = 'text';
    eye.setAttribute('data-lucide', 'eye-off');
  } else {
    input.type = 'password';
    eye.setAttribute('data-lucide', 'eye');
  }
  lucide.createIcons();
}

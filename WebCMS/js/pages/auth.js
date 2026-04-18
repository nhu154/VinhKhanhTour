/**
 * ══ AUTH DOMAIN ══
 * Handles session management, role-based UI permissions, and logout.
 */

async function doLogout() {
  if (await showConfirm('Đăng xuất', 'Bạn có chắc chắn muốn thoát khỏi hệ thống?', 'warning')) {
    // ── Unregister user from active users tracking ──
    unregisterActiveUser();
    sessionStorage.clear();
    window.location.href = 'login.html';
  }
}

function updateSidebarUser() {
  const role     = sessionStorage.getItem('cms_role') || 'user';
  const fullname = sessionStorage.getItem('cms_fullname') || 'Admin';
  const username = sessionStorage.getItem('cms_username') || 'admin';
  const nameEl   = document.getElementById('sb-name');
  const roleEl   = document.getElementById('sb-role');
  const avatarEl = document.getElementById('sb-avatar');
  
  if (nameEl) nameEl.textContent = fullname;
  if (roleEl) {
    const labels = { admin: '👑 Super Admin', owner: '🏪 Chủ quán', user: '👤 Người dùng' };
    roleEl.textContent = labels[role] || role;
  }
  if (avatarEl) {
    const initials = fullname.split(' ').map(w => w[0]).slice(-2).join('').toUpperCase() || username[0].toUpperCase();
    avatarEl.textContent = initials;
    avatarEl.style.background = role === 'admin' ? '#2563eb' : role === 'owner' ? '#10b981' : '#64748b';
  }
}

function applyRolePermissions() {
  const role = sessionStorage.getItem('cms_role') || 'user';
  
  // Menu items visibility
  const menuItems = {
    'menu-dashboard':  ['admin', 'user'],
    'menu-owner-dashboard': ['owner'],
    'menu-poi':        ['admin', 'owner'],
    'menu-tour':       ['admin'],
    'menu-users':      ['admin'],
    'menu-approvals':  ['admin'],
    'menu-lang':       ['admin'],
    'menu-audio':      ['admin', 'owner'],
    'menu-history':    ['admin', 'user']
  };

  Object.entries(menuItems).forEach(([id, roles]) => {
    const el = document.getElementById(id);
    if (el) el.style.display = roles.includes(role) ? 'flex' : 'none';
  });

  // Action buttons visibility
  const adminOnly = ['btn-delete-poi', 'btn-delete-user'];
  adminOnly.forEach(id => {
    const el = document.getElementById(id);
    if (el) el.style.display = role === 'admin' ? 'block' : 'none';
  });
}

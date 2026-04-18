// ══ PAGE NAV ══
function switchPage(pageId, navEl) {
  document.querySelectorAll('.main-content').forEach(p => p.classList.remove('active'));
  const target = document.getElementById(pageId);
  if (target) target.classList.add('active');
  document.querySelectorAll('.menu-item').forEach(m => m.classList.remove('active'));
  if (navEl) navEl.classList.add('active');

  const titleMap = {
    'page-owner-dashboard': 'Quán của tôi',
    'page-dashboard':       'Tổng quan hệ thống',
    'page-poi':             'Bản đồ & Quản lý địa điểm',
    'page-tour':            'Quản lý hành trình Tour',
    'page-trans':           'Quản lý nội dung đa ngữ',
    'page-history':         'Lịch sử sử dụng',
    'page-lang':            'Quản lý ngôn ngữ',
    'page-approvals':       'Phê duyệt yêu cầu',
    'page-users':           'Quản lý người dùng',
    'page-audio':           'Quản lý Audio & Thuyết minh',
    'page-analytics':       'Phân tích chuyên sâu',
  };
  document.getElementById('current-page-title').textContent = titleMap[pageId] || 'Dashboard';

  const _role = sessionStorage.getItem('cms_role') || 'user';
  const _ownerNeedLoad = _role === 'owner' && (!window.allPois || window.allPois.length === 0);

  if (pageId === 'page-poi') {
    setTimeout(() => { if (typeof map !== 'undefined' && map) google.maps.event.trigger(map, 'resize'); }, 200);
    if (_ownerNeedLoad) {
      loadMyLocations().then(() => renderPoiTable());
    } else {
      renderPoiTable();
    }
  }

  if (pageId === 'page-audio') {
    if (_ownerNeedLoad) {
      loadMyLocations().then(() => renderAudioPage());
    } else {
      renderAudioPage();
    }
  }

  if (pageId === 'page-trans')           renderTrans();
  if (pageId === 'page-history')         renderHistory();
  if (pageId === 'page-lang')            { renderLangList(); loadLangsFromApi(); }
  if (pageId === 'page-approvals')       loadApprovals().then(renderApprovalList);
  if (pageId === 'page-users') {
    if (typeof allUsers !== 'undefined' && allUsers.length > 0) {
      renderUserTable();
    } else {
      loadUsers();
    }
  }
  if (pageId === 'page-tour')            renderTours();
  if (pageId === 'page-dashboard')       loadStats().then(() => { renderStatsCards(); initCharts(); renderDashboardRecent(); });
  if (pageId === 'page-owner-dashboard') renderOwnerDashboard();

  // Trigger Google Maps resize khi switch sang trang heatmap
  if (pageId === 'page-analytics') {
    setTimeout(() => {
      if (typeof _gHeatMap !== 'undefined' && _gHeatMap)
        google.maps.event.trigger(_gHeatMap, 'resize');
    }, 250);
  }
}

// ══ ACTIVE USERS TOOLTIP ══
function setupActiveUsersTooltip() {
  const pill = document.getElementById('active-users-pill');
  const tooltip = document.getElementById('active-users-tooltip');
  
  if (!pill || !tooltip) return;
  
  // Toggle tooltip khi click
  pill.addEventListener('click', (e) => {
    e.stopPropagation();
    tooltip.style.display = tooltip.style.display === 'none' ? 'block' : 'none';
    updateActiveUsersDetail();
  });
  
  // Đóng tooltip khi click ngoài
  document.addEventListener('click', (e) => {
    if (!pill.contains(e.target) && !tooltip.contains(e.target)) {
      tooltip.style.display = 'none';
    }
  });
  
  // Cập nhật danh sách khi có active users update
  document.addEventListener('activeUsersUpdated', () => {
    updateActiveUsersDetail();
  });

  setTimeout(updateActiveUsersDetail, 100);
  // Tự cập nhật mỗi 30 giây để đồng bộ với stat card
  setInterval(async () => {
    await fetchRealActiveUsers();
    const countEl = document.getElementById('active-users-count');
    if (countEl) countEl.textContent = (appUsersListCache || []).length;
    // Đồng bộ stat card trên dashboard nếu đang hiển thị
    const statEl = document.querySelector('[data-active-users-count]');
    if (statEl) statEl.textContent = (appUsersListCache || []).length;
  }, 30000);
}

let appUsersListCache = [];

async function fetchRealActiveUsers() {
  try {
    const res = await fetch(`${API}/tracking/online-users`);
    if (!res.ok) throw new Error('Network error');
    appUsersListCache = await res.json();
  } catch (e) {
    console.error('Không thể lấy danh sách active user từ API:', e);
  }
}

async function updateActiveUsersDetail() {
  try {
    await fetchRealActiveUsers();
    const usersList = appUsersListCache || [];
    
    // Sắp xếp theo loginTime gần nhất
    usersList.sort((a,b) => b.loginTime - a.loginTime);

    const detailEl = document.getElementById('users-list-detail');
    if (!detailEl) return;

    // Đè số count hiển thị ở pill button bên ngoài luôn
    const countEl = document.getElementById('active-users-count');
    if(countEl) countEl.textContent = usersList.length;
    
    if (!usersList || usersList.length === 0) {
      detailEl.innerHTML = '<p style="color:var(--text-muted);padding:8px">Không có người dùng hoạt động trên App</p>';
      return;
    }
    
    detailEl.innerHTML = usersList.map((user, idx) => {
      const isAnon = user.isAnonymous;
      const displayName = isAnon ? "Người dùng ẩn danh" : (user.username || "Unknown");
      
      const badgeStyle = isAnon 
        ? 'background:#f1f5f9;color:#64748b;padding:4px 8px;border-radius:6px;font-size:10px;font-weight:700'
        : 'background:#dcfce7;color:#166534;padding:4px 8px;border-radius:6px;font-size:10px;font-weight:700';
      const badgeText = isAnon ? 'ẨN DANH' : 'CÓ TÀI KHOẢN';
      
      const onlineTime = new Date().getTime() - user.loginTime;
      const minutes = Math.floor(onlineTime / 60000);
      const timeStr = minutes < 1 ? 'Vừa mới online' : `${minutes} phút trước`;
      
      const avatarContent = isAnon ? '👤' : displayName.charAt(0).toUpperCase();
      const avatarBg = isAnon ? '#f1f5f9' : '#e0e7ff';
      const avatarColor = isAnon ? '#94a3b8' : '#4338ca';
      
      return `<div style="padding:12px;border-bottom:1px solid #f8fafc;display:flex;justify-content:space-between;align-items:center;gap:12px">
        <div style="flex:1;min-width:0;display:flex;align-items:center;gap:10px">
          <div style="width:36px;height:36px;border-radius:50%;background:${avatarBg};color:${avatarColor};display:flex;align-items:center;justify-content:center;font-weight:bold;font-size:14px;flex-shrink:0">
            ${avatarContent}
          </div>
          <div style="flex:1;min-width:0">
            <div style="font-weight:700;font-size:13px;color:var(--text-main);white-space:nowrap;overflow:hidden;text-overflow:ellipsis">${displayName}</div>
            <div style="font-size:11px;color:var(--text-muted);margin-top:3px">${timeStr}</div>
          </div>
        </div>
        <div>
          <span style="${badgeStyle}">${badgeText}</span>
        </div>
      </div>`;
    }).join('');
  } catch(e) {
    console.error('updateActiveUsersDetail error:', e);
    const detailEl = document.getElementById('users-list-detail');
    if (detailEl) detailEl.innerHTML = '<p style="color:var(--text-muted);padding:8px">Lỗi tải danh sách</p>';
  }
}
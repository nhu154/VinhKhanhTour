// ══ PAGE NAV ══
function switchPage(pageId, navEl) {
  document.querySelectorAll('.main-content').forEach(p => p.classList.remove('active'));
  const target = document.getElementById(pageId);
  if (target) target.classList.add('active');
  document.querySelectorAll('.menu-item').forEach(m => m.classList.remove('active'));
  if (navEl) navEl.classList.add('active');

  const titleMap = {
    'page-owner-dashboard': 'Quán của tôi',
    'page-dashboard': 'Tổng quan hệ thống',
    'page-poi':       'Bản đồ & Quản lý địa điểm',
    'page-tour':      'Quản lý hành trình Tour',
    'page-trans':     'Quản lý nội dung đa ngữ',
    'page-history':   'Nhật ký hành trình khách hàng',
    'page-lang':      'Quản lý ngôn ngữ',
    'page-approvals': 'Phê duyệt yêu cầu',
    'page-users':     'Quản lý người dùng',
    'page-audio':     'Quản lý Audio & Thuyết minh',
  };
  document.getElementById('current-page-title').textContent = titleMap[pageId] || 'Dashboard';

  if (pageId === 'page-poi')       { setTimeout(() => { if (map) google.maps.event.trigger(map, 'resize'); }, 200); renderPoiTable(); }
  if (pageId === 'page-trans')     renderTrans();
  if (pageId === 'page-history')   renderHistory();
  if (pageId === 'page-lang')      { renderLangList(); }
  if (pageId === 'page-approvals') { loadApprovals().then(renderApprovalList); }
  if (pageId === 'page-users')     { loadUsers(); }
  if (pageId === 'page-audio')     renderAudioPage();
  if (pageId === 'page-tour')      renderTours();
  if (pageId === 'page-dashboard') loadStats().then(() => { renderStatsCards(); initCharts(); renderDashboardRecent(); });
}


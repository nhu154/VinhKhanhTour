// ══ INIT ══
document.addEventListener('DOMContentLoaded', () => {
  setupAccordions();
  setupHistorySearch();
  initApp();
});

async function initApp() {
  applyRolePermissions();
  updateSidebarUser();
  showSkeleton();

  const role = sessionStorage.getItem('cms_role') || 'user';

  if (role === 'owner') {
    // ── CHỦ QUÁN: chỉ load data liên quan đến quán của họ ──
    await loadMyLocations();          // load quán của mình (override allPois)
    await loadAnalytics();            // analytics (sẽ filter theo quán mình)
    // Chủ quán vào trang "Quán của tôi" thay vì dashboard chung
    lucide.createIcons();
    switchPage('page-owner-dashboard', document.getElementById('menu-owner-dashboard'));

  } else if (role === 'admin') {
    // ── ADMIN: load tất cả ──
    await Promise.all([loadPois(), loadTours(), loadAnalytics(), loadStats(), loadUsers()]);
    await loadApprovals();
    lucide.createIcons();
    switchPage('page-dashboard', document.getElementById('menu-dashboard'));
    // Poll badge pending mỗi 30s
    setInterval(loadPendingBadge, 30000);

  } else {
    // ── USER THƯỜNG: chỉ xem ──
    await Promise.all([loadAnalytics(), loadStats()]);
    lucide.createIcons();
    switchPage('page-dashboard', document.getElementById('menu-dashboard'));
  }
}

function textMatch(str, q) {
  if (!q) return true;
  if (!str) return false;
  
  const normTone = s => String(s).normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd').replace(/Đ/g, 'D').toLowerCase();
  
  const qWords = String(q).trim().split(/\s+/);
  const sNorm = ' ' + normTone(str) + ' ';
  const sRaw = ' ' + String(str).toLowerCase() + ' ';
  
  return qWords.every(w => {
    const isPlain = !/[àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ]/i.test(w);
    if (isPlain) {
      return sNorm.includes(' ' + normTone(w));
    } else {
      const hasTones = /[\u0300\u0301\u0303\u0309\u0323]/i.test(w.normalize('NFD'));
      if (!hasTones) {
         const removeTones = x => String(x).normalize('NFD').replace(/[\u0300\u0301\u0303\u0309\u0323]/g, '').normalize('NFC').toLowerCase();
         return (' ' + removeTones(str) + ' ').includes(' ' + removeTones(w));
      } else {
         return sRaw.includes(' ' + w.toLowerCase());
      }
    }
  });
}

function showSkeleton() {
  const stats = document.getElementById('dashboard-stats');
  if (stats) stats.innerHTML = Array(4).fill(`
    <div class="stat-card">
      <div class="skeleton" style="width:48px;height:48px;border-radius:12px"></div>
      <div style="flex:1">
        <div class="skeleton" style="height:12px;width:60%;margin-bottom:8px"></div>
        <div class="skeleton" style="height:28px;width:40%"></div>
      </div>
    </div>`).join('');
}

function setupAccordions() {
  document.addEventListener('click', e => {
    const title = e.target.closest('.section-title');
    if (title) title.parentElement.classList.toggle('closed');
  });
}

function setupHistorySearch() {
  const inp = document.getElementById('history-search');
  if (inp) inp.addEventListener('input', e => filterHistory(e.target.value));
}


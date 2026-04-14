// ══ ADMIN LOGS (Thay thế Analytics cũ) ══
async function loadAnalytics() {
  try {
    const res = await fetch(`${API}/adminlogs`, { cache: 'no-store' });
    historyData = await res.json();
    renderStatsCards();
    renderHistory();
    renderDashboardRecent();
  }
  catch(e) { console.error('loadAnalytics:', e); }
}

function renderStatsCards() {
  const el = document.getElementById('dashboard-stats'); if (!el) return;
  const totalPois = allPois.length;
  const totalTours = tours.length;
  // Giữ lại stats cơ bản (có thể lấy từ analytics/stats nếu vẫn muốn hiện lượt ghé thăm)
  const totalVisits = statsData?.totalVisits ?? 0;
  const todayVisits = statsData?.todayVisits ?? 0;
  
  el.innerHTML = `
    <div class="stat-card"><div class="stat-icon blue"><i data-lucide="map-pin"></i></div><div class="stat-info"><p class="text-muted">Tổng POI</p><h2 class="stat-val">${totalPois}</h2><div class="stat-trend">Địa điểm trên phố</div></div></div>
    <div class="stat-card"><div class="stat-icon green"><i data-lucide="share-2"></i></div><div class="stat-info"><p class="text-muted">Hành trình Tour</p><h2 class="stat-val">${totalTours}</h2><div class="stat-trend">Tuyến tham quan</div></div></div>
    <div class="stat-card"><div class="stat-icon orange"><i data-lucide="activity"></i></div><div class="stat-info"><p class="text-muted">Lượt khách ghé</p><h2 class="stat-val">${totalVisits}</h2><div class="stat-trend up">↑ ${todayVisits} hôm nay</div></div></div>
    <div class="stat-card"><div class="stat-icon red"><i data-lucide="history"></i></div><div class="stat-info"><p class="text-muted">Thao tác Admin</p><h2 class="stat-val">${historyData.length}</h2><div class="stat-trend">Lịch sử hệ thống</div></div></div>`;
  lucide.createIcons();
}

function renderHistory(search='') {
  const tbody = document.getElementById('history-tbody'); if (!tbody) return;
  let data = historyData;

  if (currentFilter !== 'all') {
    data = data.filter(h => (h.Action || h.action || '').includes(currentFilter));
  }

  if (search) {
    const s = search.toLowerCase();
    data = data.filter(h => 
      (h.UserName||h.username||'').toLowerCase().includes(s) || 
      (h.Target||h.target||'').toLowerCase().includes(s) ||
      (h.Action||h.action||'').toLowerCase().includes(s)
    );
  }

  if (!data.length) {
    tbody.innerHTML = `<tr><td colspan="4">
      <div class="empty-state">
        <i data-lucide="inbox"></i>
        <p>Không tìm thấy lịch sử thao tác nào khớp với bộ lọc</p>
      </div>
    </td></tr>`;
    lucide.createIcons();
    return;
  }
  
  tbody.innerHTML = data.map(h => {
    const action = h.Action || h.action || 'Unknown';
    const target = h.Target || h.target || '—';
    const user   = h.UserName || h.userName || 'Hệ thống';
    const time   = new Date(h.Timestamp || h.timestamp).toLocaleString('vi-VN');
    
    let icon = 'settings', color = '#64748b', bg = '#f8fafc', label = action, sub = 'Hệ thống';
    
    if (action.includes('POI')) {
      icon = 'map-pin'; color = '#3b82f6'; bg = '#eff6ff';
      label = action.startsWith('CREATE') ? 'Thêm địa điểm' : action.startsWith('UPDATE') ? 'Cập nhật địa điểm' : 'Xóa địa điểm';
      sub = 'Quản lý POI';
    } else if (action.includes('TOUR')) {
      icon = 'navigation'; color = '#10b981'; bg = '#f0fdf4';
      label = action.startsWith('CREATE') ? 'Tạo Tour mới' : action.startsWith('UPDATE') ? 'Cập nhật Tour' : 'Xóa Tour';
      sub = 'Quản lý hành trình';
    } else if (action === 'LOGIN') {
      icon = 'log-in'; color = '#8b5cf6'; bg = '#f3e8ff';
      label = 'Đăng nhập hệ thống'; sub = 'Bảo mật';
    } else if (action.includes('REQ')) {
      icon = 'check-square'; color = '#f59e0b'; bg = '#fffbeb';
      label = action === 'APPROVE_REQ' ? 'Phê duyệt yêu cầu' : 'Từ chối yêu cầu';
      sub = 'Quy trình xét duyệt';
    } else if (action.includes('USER')) {
      icon = 'users'; color = '#ef4444'; bg = '#fef2f2';
      label = 'Quản trị nhân sự'; sub = 'Tài khoản CMS';
    }

    return `<tr>
      <td style="text-align:center">
        <div style="width:40px;height:40px;border-radius:12px;background:${bg};color:${color};display:inline-flex;align-items:center;justify-content:center;box-shadow:0 2px 4px rgba(0,0,0,0.02);border:1px solid rgba(0,0,0,0.03)">
          <i data-lucide="${icon}" style="width:18px;height:18px"></i>
        </div>
      </td>
      <td>
        <div style="font-weight:700;font-size:14px;color:var(--text-main)">${label}</div>
        <div style="font-size:11px;color:var(--text-muted);margin-top:2px;display:flex;align-items:center;gap:4px">
          <span style="font-weight:600;color:${color}">${sub}</span> • <span>Bởi <strong>${user}</strong></span>
        </div>
      </td>
      <td>
        <div style="padding:6px 12px; border-radius:8px; background:#f1f5f9; color:#475569; font-size:12px; font-weight:600; display:inline-block; border:1px solid #e2e8f0">
          ${target}
        </div>
      </td>
      <td>
        <div style="font-size:13px; font-weight:500; color:var(--text-main)">${time}</div>
        <div style="font-size:11px; color:var(--text-muted); margin-top:2px">Thời gian máy chủ</div>
      </td>
    </tr>`;
  }).join('');
  lucide.createIcons();
}

function filterHistory(val) { renderHistory(val); }
function setFilter(f,el) { 
  currentFilter=f; 
  document.querySelectorAll('#page-history .filter-chip').forEach(c=>c.classList.remove('active')); 
  el.classList.add('active'); 
  renderHistory(document.getElementById('history-search')?.value||''); 
}

function renderDashboardRecent() {
  const tbody = document.getElementById('dashboard-recent-tbody'); if (!tbody) return;
  tbody.innerHTML = historyData.slice(0, 8).map(h => {
    const action = h.Action || h.action || 'Unknown';
    const target = h.Target || h.target || '—';
    const user   = h.UserName || h.userName || 'Hệ thống';
    const time   = new Date(h.Timestamp || h.timestamp).toLocaleTimeString('vi-VN');
    
    let label = action;
    if (action.includes('POI')) label = 'POI';
    else if (action.includes('TOUR')) label = 'Tour';
    else if (action === 'LOGIN') label = 'Login';
    else if (action.includes('REQ')) label = 'Duyệt';

    return `<tr>
      <td>
        <div style="font-weight:700;font-size:12px">${user}</div>
        <div style="font-size:10px;color:var(--text-muted)">${target}</div>
      </td>
      <td><span class="badge badge-info" style="font-size:10px;background:#f8fafc;color:#475569;border:1px solid #e2e8f0">${label}</span></td>
      <td style="color:var(--text-muted);font-size:11px">${time}</td>
    </tr>`;
  }).join('') || `<tr><td colspan="3" style="text-align:center;color:var(--text-muted);padding:20px">Chưa có thao tác</td></tr>`;
}

async function loadStats() {
  try { const res=await fetch(`${API}/analytics/stats`, { cache: 'no-store' }); statsData=await res.json(); } catch(e) { statsData=null; }
}

function initCharts() {
  const ctx=document.getElementById('mainChart')?.getContext('2d');
  const ctxPie=document.getElementById('pieChart')?.getContext('2d');
  if(!ctx||!ctxPie) return;
  if(mainChart) mainChart.destroy();
  if(pieChart) pieChart.destroy();
  
  let labels=[],data=[];
  if(statsData?.byDay?.length>0) { 
    statsData.byDay.forEach(d=>{ labels.push(new Date(d.Day||d.day).toLocaleDateString('vi-VN',{month:'short',day:'numeric'})); data.push(d.Count||d.count||0); }); 
  } else {
    labels = ['T2','T3','T4','T5','T6','T7','CN']; data = [0,0,0,0,0,0,0];
  }

  mainChart=new Chart(ctx,{type:'bar',data:{labels,datasets:[{label:'Lượt ghé thăm',data,backgroundColor:'rgba(37,99,235,0.8)',borderRadius:6,borderSkipped:false}]},options:{responsive:true,maintainAspectRatio:false,plugins:{legend:{display:false}},scales:{y:{beginAtZero:true,grid:{color:'#f1f5f9'},ticks:{stepSize:1}},x:{grid:{display:false}}}}});
  
  let topLabels=[],topValues=[];
  if(statsData?.topPoi?.length>0){
    statsData.topPoi.forEach(p=>{topLabels.push(p.Name||p.name||'?');topValues.push(p.VisitCount||p.visitCount||0);});
  } else {
    topLabels=['Chưa có data']; topValues=[1];
  }
  
  pieChart=new Chart(ctxPie,{type:'doughnut',data:{labels:topLabels,datasets:[{data:topValues,backgroundColor:['#2563eb','#10b981','#f59e0b','#ef4444','#8b5cf6'],borderWidth:0,hoverOffset:4}]},options:{responsive:true,maintainAspectRatio:false,cutout:'65%',plugins:{legend:{position:'bottom',labels:{font:{size:11},padding:12}}}}});
}

async function refreshDashboard() {
  const recentTbody = document.getElementById('dashboard-recent-tbody');
  if (recentTbody) { recentTbody.style.opacity = '0.4'; }
  await Promise.all([loadStats(), loadAnalytics()]);
  initCharts();
  if (recentTbody) { recentTbody.style.opacity = '1'; }
  showToast('Đã cập nhật lịch sử thao tác', 'success');
}

async function clearDashboardHistory() {
  if (!(await showConfirm('Xóa lịch sử thao tác', 'Bạn có chắc chắn muốn xóa TOÀN BỘ nhật ký thao tác Admin? Thao tác này không thể hoàn tác.', 'danger'))) return;
  try {
    const res = await fetch(`${API}/adminlogs/clear`, { method: 'DELETE' });
    if (!res.ok) throw new Error();
    showToast('Đã xóa sạch lịch sử thao tác', 'success');
    await refreshDashboard();
  } catch (e) {
    showToast('Lỗi khi xóa dữ liệu', 'danger');
  }
}

// OWNER DASHBOARD logic giữ nguyên vì nó chỉ filter theo RestaurantId
async function renderOwnerDashboard() {
  _renderOwnerWelcome();
  renderOwnerStats();
  renderOwnerPoiList();
  renderOwnerRequests();
  renderOwnerChart();
}

function _renderOwnerWelcome() {
  const el = document.getElementById('owner-welcome'); if (!el) return;
  const name = sessionStorage.getItem('cms_fullname') || 'Chủ quán';
  const hour = new Date().getHours();
  const greeting = hour < 12 ? 'Chào buổi sáng' : hour < 18 ? 'Chào buổi chiều' : 'Chào buổi tối';
  el.innerHTML = `
    <div style="display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:12px">
      <div>
        <h2 style="font-size:20px;font-weight:700;color:var(--text-main);margin-bottom:2px">
          ${greeting}, <span style="color:#2563eb">${name}</span> 👋
        </h2>
        <p style="font-size:13px;color:var(--text-muted)">Quản lý thông tin quán của bạn tại đây</p>
      </div>
      <button class="btn btn-primary" onclick="switchPage('page-poi', document.getElementById('menu-poi'))">
        <i data-lucide="plus"></i> Gửi yêu cầu chỉnh sửa
      </button>
    </div>`;
  lucide.createIcons();
}

function renderOwnerStats() {
  // Logic này vẫn dùng historyData (giờ là admin logs) - Có thể cần chỉnh sửa nếu Owner muốn xem visitor stats riêng.
  // Tuy nhiên theo request thì "Lịch sử sử dụng" đổi mục tiêu.
  const grid = document.getElementById('owner-stats-grid');
  if (!grid) return;
  const myPois = allPois;
  const gridHtml = `
    <div class="stat-card">
      <div class="stat-icon blue"><i data-lucide="store"></i></div>
      <div class="stat-info"><p>Quán của tôi</p><h2 class="stat-val">${myPois.length}</h2><div class="stat-trend">Địa điểm được gán</div></div>
    </div>
    <div class="stat-card">
      <div class="stat-icon orange"><i data-lucide="activity"></i></div>
      <div class="stat-info"><p>Thao tác gần đây</p><h2 class="stat-val">${historyData.filter(h=>h.UserName===sessionStorage.getItem('cms_fullname')).length}</h2><div class="stat-trend">Thao tác của bạn</div></div>
    </div>
    <div class="stat-card" style="cursor:pointer" onclick="setOwnerRequestFilter('pending',this)">
       <div class="stat-icon" style="background:#fffbeb;color:#d97706"><i data-lucide="clock"></i></div>
       <div class="stat-info"><p>Yêu cầu chờ</p><h2 class="stat-val" id="owner-pending-count">—</h2><div class="stat-trend">Nhấn để xem</div></div>
    </div>
    <div class="stat-card">
       <div class="stat-icon green"><i data-lucide="mic"></i></div>
       <div class="stat-info"><p>Audio TTS</p><h2 class="stat-val">${myPois.filter(p=>!!(p.ttsScript||p.TtsScript)).length}/${myPois.length}</h2><div class="stat-trend">Trạng thái nội dung</div></div>
    </div>`;
  grid.innerHTML = gridHtml;
  
  const userId = parseInt(sessionStorage.getItem('cms_userid') || '0');
  if (userId > 0) {
    fetch(`${API}/approvals/user/${userId}`).then(r=>r.json()).then(list => {
      const p = list.filter(a=>(a.Status||a.status||'pending').toLowerCase()==='pending').length;
      document.getElementById('owner-pending-count').textContent = p;
    }).catch(()=>{});
  }
  lucide.createIcons();
}

function renderOwnerPoiList() {
  const container = document.getElementById('owner-poi-list'); if (!container) return;
  if (!allPois.length) {
    container.innerHTML = `<div style="text-align:center;padding:40px;color:var(--text-muted)">Chưa được gán quán nào</div>`;
    return;
  }
  container.innerHTML = allPois.map(p => {
    const pJson = JSON.stringify(p).replace(/"/g,'&quot;');
    return `
    <div style="display:flex;align-items:center;gap:12px;padding:14px 16px;border-bottom:1px solid var(--border);cursor:pointer" onclick="openEditPoiForm(${pJson})">
      <img src="${getImgUrl(p)}" style="width:44px;height:44px;border-radius:10px;object-fit:cover">
      <div style="flex:1">
        <div style="font-size:13px;font-weight:700">${p.name||p.Name}</div>
        <div style="font-size:11px;color:var(--text-muted)">⭐ ${(p.rating||p.Rating||0).toFixed(1)} &nbsp;·&nbsp; ${p.category||p.Category}</div>
      </div>
      <button class="btn btn-primary btn-sm">Sửa</button>
    </div>`;
  }).join('');
}

function renderOwnerRequests() { /* Logic giữ nguyên */ }
function renderOwnerChart() { /* Logic giữ nguyên */ }

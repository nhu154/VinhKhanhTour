// ══ ANALYTICS ══
async function loadAnalytics() {
  try { const res=await fetch(`${API}/analytics`, { cache: 'no-store' }); historyData=await res.json(); renderStatsCards(); renderHistory(); renderDashboardRecent(); }
  catch(e) { console.error('loadAnalytics:',e); }
}

function renderStatsCards() {
  const el = document.getElementById('dashboard-stats'); if (!el) return;
  const totalPois = allPois.length;
  const totalTours = tours.length;
  const totalVisits = statsData?.totalVisits ?? historyData.length;
  const todayVisits = statsData?.todayVisits ?? historyData.filter(h => new Date(h.Timestamp||h.timestamp).toDateString()===new Date().toDateString()).length;
  const weekVisits = statsData?.weekVisits ?? 0;
  const majorPois = allPois.filter(p=>(p.category||p.Category)==='Quán ăn').length;
  el.innerHTML = `
    <div class="stat-card"><div class="stat-icon blue"><i data-lucide="map-pin"></i></div><div class="stat-info"><p class="text-muted">Tổng POI</p><h2 class="stat-val">${totalPois}</h2><div class="stat-trend up">↑ ${majorPois} quán ăn</div></div></div>
    <div class="stat-card"><div class="stat-icon green"><i data-lucide="share-2"></i></div><div class="stat-info"><p class="text-muted">Hành trình Tour</p><h2 class="stat-val">${totalTours}</h2><div class="stat-trend" style="color:var(--text-muted)">Tuyến tham quan</div></div></div>
    <div class="stat-card"><div class="stat-icon orange"><i data-lucide="activity"></i></div><div class="stat-info"><p class="text-muted">Lượt ghé thăm</p><h2 class="stat-val">${totalVisits}</h2><div class="stat-trend up">↑ ${todayVisits} hôm nay · ${weekVisits} tuần này</div></div></div>
    <div class="stat-card"><div class="stat-icon red"><i data-lucide="languages"></i></div><div class="stat-info"><p class="text-muted">Ngôn ngữ</p><h2 class="stat-val">${getLangs().length}</h2><div class="stat-trend" style="color:var(--text-muted)">${getLangs().map(l=>l.flag).join(' ')}</div></div></div>`;
  lucide.createIcons();
}

function renderHistory(filter='') {
  const tbody = document.getElementById('history-tbody'); if (!tbody) return;
  let data = historyData;

  // Owner chỉ thấy lịch sử quán của mình
  const role = sessionStorage.getItem('cms_role') || 'user';
  if (role === 'owner') {
    const myIds = allPois.map(p => p.id||p.Id);
    data = data.filter(h => myIds.includes(h.RestaurantId||h.restaurantId));
  }

  if (currentFilter!=='all') data=data.filter(h=>(h.EventType||h.eventType||'').toLowerCase()===currentFilter);
  if (filter) data=data.filter(h=>(h.RestaurantName||h.restaurantName||'').toLowerCase().includes(filter.toLowerCase()));
  if (!data.length) { tbody.innerHTML=`<tr><td colspan="4" style="text-align:center;padding:40px;color:var(--text-muted)">Chưa có dữ liệu</td></tr>`; return; }
  tbody.innerHTML = data.map(h => {
    const evt=(h.EventType||h.eventType||'visit').toLowerCase();
    const name=h.RestaurantName||h.restaurantName||'—';
    const time=new Date(h.Timestamp||h.timestamp).toLocaleString('vi-VN');
    
    let icon = 'activity', action = 'Chưa xác định', color = '#64748b', bg = '#f8fafc';
    if(evt.includes('enter') || evt.includes('visit')) {
        icon = 'navigation'; action = 'Vừa đi ngang qua / Ghé thăm'; color = '#10b981'; bg = '#f0fdf4';
    } else if(evt.includes('click')) {
        icon = 'mouse-pointer'; action = 'Bấm xem chi tiết'; color = '#3b82f6'; bg = '#eff6ff';
    } else if(evt.includes('exit')) {
        icon = 'log-out'; action = 'Rời khỏi khu vực'; color = '#f59e0b'; bg = '#fffbeb';
    } else if(evt.includes('audio') || evt.includes('play')) {
        icon = 'volume-2'; action = 'Nghe thuyết minh audio'; color = '#8b5cf6'; bg = '#f3e8ff';
    }

    return `<tr>
      <td>
        <div style="width:36px;height:36px;border-radius:10px;background:${bg};color:${color};display:flex;align-items:center;justify-content:center">
          <i data-lucide="${icon}" style="width:16px;height:16px"></i>
        </div>
      </td>
      <td><span style="font-weight:600;font-size:13px">${action}</span></td>
      <td><strong>${name}</strong></td>
      <td style="font-size:12px;color:var(--text-muted)">${time}</td>
    </tr>`;
  }).join('');
  lucide.createIcons();
}

function filterHistory(val) { renderHistory(val); }
function setFilter(f,el) { currentFilter=f; document.querySelectorAll('#page-history .filter-chip').forEach(c=>c.classList.remove('active')); el.classList.add('active'); renderHistory(document.getElementById('history-search')?.value||''); }

function renderDashboardRecent() {
  const tbody=document.getElementById('dashboard-recent-tbody'); if(!tbody) return;
  tbody.innerHTML=historyData.slice(0,5).map(h=>{
    const evt=(h.EventType||h.eventType||'visit').toLowerCase();
    let action = 'Tương tác';
    if(evt.includes('enter') || evt.includes('visit')) action = 'Ghé thăm';
    else if(evt.includes('click')) action = 'Xem thông tin';
    else if(evt.includes('exit')) action = 'Rời khỏi';
    return `<tr><td><strong>${h.RestaurantName||h.restaurantName||'POI'}</strong></td><td><span class="badge badge-info" style="font-size:11px;background:#f8fafc;color:#475569;border:1px solid #e2e8f0;font-weight:500">${action}</span></td><td style="color:var(--text-muted);font-size:12px">${new Date(h.Timestamp||h.timestamp).toLocaleTimeString('vi-VN')}</td></tr>`
  }).join('')||`<tr><td colspan="3" style="text-align:center;color:var(--text-muted);padding:20px">Chưa có hoạt động</td></tr>`;
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
  if(statsData?.byDay?.length>0) { statsData.byDay.forEach(d=>{ labels.push(new Date(d.Day||d.day).toLocaleDateString('vi-VN',{month:'short',day:'numeric'})); data.push(d.Count||d.count||0); }); }
  else { const hc={}; historyData.forEach(h=>{const hr=new Date(h.Timestamp||h.timestamp).getHours();hc[hr]=(hc[hr]||0)+1;}); Object.keys(hc).forEach(h=>{labels.push(`${h}:00`);data.push(hc[h]);}); if(!labels.length){labels.push('Chưa có data');data.push(0);} }
  mainChart=new Chart(ctx,{type:'bar',data:{labels,datasets:[{label:'Lượt ghé thăm',data,backgroundColor:'rgba(37,99,235,0.8)',borderRadius:6,borderSkipped:false}]},options:{responsive:true,maintainAspectRatio:false,plugins:{legend:{display:false}},scales:{y:{beginAtZero:true,grid:{color:'#f1f5f9'},ticks:{stepSize:1}},x:{grid:{display:false}}}}});
  let topLabels=[],topValues=[];
  if(statsData?.topPoi?.length>0){statsData.topPoi.forEach(p=>{topLabels.push(p.Name||p.name||'?');topValues.push(p.VisitCount||p.visitCount||0);});}
  else{const rc={};historyData.forEach(h=>{const n=h.RestaurantName||h.restaurantName||'Khác';rc[n]=(rc[n]||0)+1;});const top=Object.entries(rc).sort((a,b)=>b[1]-a[1]).slice(0,5);top.forEach(([n,v])=>{topLabels.push(n);topValues.push(v);});}
  if(!topLabels.length){topLabels.push('Chưa có data');topValues.push(1);}
  pieChart=new Chart(ctxPie,{type:'doughnut',data:{labels:topLabels,datasets:[{data:topValues,backgroundColor:['#2563eb','#10b981','#f59e0b','#ef4444','#8b5cf6'],borderWidth:0,hoverOffset:4}]},options:{responsive:true,maintainAspectRatio:false,cutout:'65%',plugins:{legend:{position:'bottom',labels:{font:{size:11},padding:12}}}}});
}

async function refreshDashboard() {
  const recentTbody = document.getElementById('dashboard-recent-tbody');
  if (recentTbody) {
    recentTbody.style.transition = 'none';
    recentTbody.style.opacity = '0.4';
  }
  
  // Ensure the browser renders the opacity change
  await new Promise(r => setTimeout(r, 400));
  
  await Promise.all([loadStats(), loadAnalytics()]);
  initCharts();
  
  if (recentTbody) {
    recentTbody.style.transition = 'opacity 0.3s ease-in';
    recentTbody.style.opacity = '1';
  }
  showToast('Đã làm mới dữ liệu', 'success');
}

async function clearDashboardHistory() {
  if (!(await showConfirm('Xóa lịch sử', 'Bạn có chắc chắn muốn làm sạch (reset) toàn bộ dữ liệu hoạt động gần nhất?', 'danger'))) return;
  try {
    const res = await fetch(`${API}/analytics/clear`, { method: 'DELETE' });
    if (!res.ok) throw new Error();
    showToast('Đã reset toàn bộ lịch sử', 'success');
    await refreshDashboard();
  } catch (e) {
    showToast('Lỗi khi xóa dữ liệu', 'danger');
  }
}

// OWNER DASHBOARD — chỉ hiện data quán của mình
// ══════════════════════════════════════════════════
let ownerChart = null;
let ownerRequestFilter = 'all'; // 'all' | 'pending' | 'approved' | 'rejected'

async function renderOwnerDashboard() {
  _renderOwnerWelcome();
  renderOwnerStats();
  renderOwnerPoiList();
  renderOwnerRequests();
  renderOwnerChart();
}

// ── Chào tên chủ quán ──
function _renderOwnerWelcome() {
  const el = document.getElementById('owner-welcome');
  if (!el) return;
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
  const grid = document.getElementById('owner-stats-grid');
  if (!grid) return;
  const myPois = allPois;
  const myPoisIds = myPois.map(p => p.id||p.Id);
  const myVisits = historyData.filter(h => myPoisIds.includes(h.RestaurantId||h.restaurantId));
  const today = myVisits.filter(h =>
    new Date(h.Timestamp||h.timestamp).toDateString() === new Date().toDateString()
  ).length;
  const week = myVisits.filter(h => {
    const d = new Date(h.Timestamp||h.timestamp);
    const now = new Date();
    return (now - d) / 864e5 <= 7;
  }).length;
  const hasAudio = myPois.filter(p => !!(p.ttsScript||p.TtsScript)).length;
  const audioOk = myPois.length > 0 && hasAudio === myPois.length;
  const userId = parseInt(sessionStorage.getItem('cms_userid') || '0');

  grid.innerHTML = `
    <div class="stat-card">
      <div class="stat-icon blue"><i data-lucide="store"></i></div>
      <div class="stat-info">
        <p class="text-muted">Quán của tôi</p>
        <h2 class="stat-val">${myPois.length}</h2>
        <div class="stat-trend" style="color:var(--text-muted)">Địa điểm được gán</div>
      </div>
    </div>
    <div class="stat-card">
      <div class="stat-icon orange"><i data-lucide="activity"></i></div>
      <div class="stat-info">
        <p class="text-muted">Lượt ghé thăm</p>
        <h2 class="stat-val">${myVisits.length}</h2>
        <div class="stat-trend up">↑ ${today} hôm nay &nbsp;·&nbsp; ${week} tuần này</div>
      </div>
    </div>
    <div class="stat-card" style="cursor:pointer" onclick="setOwnerRequestFilter('pending',this)">
      <div class="stat-icon" style="background:#fef3c7;color:#d97706"><i data-lucide="clock"></i></div>
      <div class="stat-info">
        <p class="text-muted">Yêu cầu đang chờ</p>
        <h2 class="stat-val" id="owner-pending-count">—</h2>
        <div class="stat-trend" style="color:#d97706">Nhấn để xem</div>
      </div>
    </div>
    <div class="stat-card">
      <div class="stat-icon green"><i data-lucide="mic"></i></div>
      <div class="stat-info">
        <p class="text-muted">Audio TTS</p>
        <h2 class="stat-val">${hasAudio}<span style="font-size:14px;font-weight:500;color:var(--text-muted)">/${myPois.length}</span></h2>
        <div class="stat-trend ${audioOk ? 'up' : ''}">${audioOk ? '✅ Đầy đủ 3 ngôn ngữ' : '⚠️ Còn thiếu script'}</div>
      </div>
    </div>`;

  if (userId > 0) {
    fetch(`${API}/approvals/user/${userId}`)
      .then(r => r.json())
      .then(list => {
        const pending = list.filter(a => (a.Status||a.status) === 'pending').length;
        const el = document.getElementById('owner-pending-count');
        if (el) el.textContent = pending;
      }).catch(() => {});
  } else {
    const el = document.getElementById('owner-pending-count');
    if (el) el.textContent = '0';
  }
  lucide.createIcons();
}

function renderOwnerPoiList() {
  const container = document.getElementById('owner-poi-list');
  if (!container) return;
  if (!allPois.length) {
    container.innerHTML = `
      <div style="text-align:center;padding:40px 20px;color:var(--text-muted)">
        <div style="font-size:40px;margin-bottom:12px">🏪</div>
        <div style="font-weight:600;margin-bottom:6px">Chưa được gán quán nào</div>
        <div style="font-size:12px">Vui lòng liên hệ Admin để được gán địa điểm</div>
      </div>`;
    return;
  }
  container.innerHTML = allPois.map(p => {
    const hasVi = !!(p.ttsScript||p.TtsScript);
    const hasEn = !!(p.ttsScriptEn||p.TtsScriptEn);
    const hasZh = !!(p.ttsScriptZh||p.TtsScriptZh);
    const visits = historyData.filter(h => (h.RestaurantId||h.restaurantId) === (p.id||p.Id)).length;
    const pJson = JSON.stringify(p).replace(/"/g,'&quot;');
    return `
    <div style="display:flex;align-items:center;gap:12px;padding:14px 16px;border-bottom:1px solid var(--border);cursor:pointer;transition:.15s"
         onmouseover="this.style.background='#f8faff'" onmouseout="this.style.background=''"
         onclick="openEditPoiForm(${pJson})">
      <img src="${getImgUrl(p)}" onerror="this.src='https://via.placeholder.com/44?text=?'"
           style="width:48px;height:48px;border-radius:10px;object-fit:cover;flex-shrink:0;border:1px solid var(--border)">
      <div style="flex:1;min-width:0">
        <div style="font-size:13px;font-weight:700;margin-bottom:3px">${p.name||p.Name}</div>
        <div style="font-size:11px;color:var(--text-muted)">
          ⭐ ${(p.rating||p.Rating||0).toFixed(1)} &nbsp;·&nbsp; 🕒 ${p.openHours||p.OpenHours||'Chưa cập nhật'} &nbsp;·&nbsp; 👣 ${visits} lượt ghé
        </div>
      </div>
      <div style="display:flex;gap:4px;margin-right:4px">
        <span title="VI" style="width:22px;height:22px;border-radius:50%;background:${hasVi?'#22c55e':'#e2e8f0'};display:flex;align-items:center;justify-content:center;font-size:9px;font-weight:700;color:${hasVi?'#fff':'#94a3b8'}">VI</span>
        <span title="EN" style="width:22px;height:22px;border-radius:50%;background:${hasEn?'#22c55e':'#e2e8f0'};display:flex;align-items:center;justify-content:center;font-size:9px;font-weight:700;color:${hasEn?'#fff':'#94a3b8'}">EN</span>
        <span title="ZH" style="width:22px;height:22px;border-radius:50%;background:${hasZh?'#22c55e':'#e2e8f0'};display:flex;align-items:center;justify-content:center;font-size:9px;font-weight:700;color:${hasZh?'#fff':'#94a3b8'}">ZH</span>
      </div>
      <button class="btn btn-primary btn-sm" style="white-space:nowrap" onclick="event.stopPropagation();openEditPoiForm(${pJson})">
        <i data-lucide="edit-3"></i> Sửa
      </button>
    </div>`;
  }).join('');
  lucide.createIcons();
}

function setOwnerRequestFilter(filter) {
  ownerRequestFilter = filter;
  document.querySelectorAll('.owner-req-chip').forEach(c => c.classList.remove('active'));
  const chip = document.querySelector(`.owner-req-chip[data-filter="${filter}"]`);
  if (chip) chip.classList.add('active');
  renderOwnerRequests();
}

function renderOwnerRequests() {
  const container = document.getElementById('owner-requests-list');
  if (!container) return;
  const userId = parseInt(sessionStorage.getItem('cms_userid') || '0');

  if (userId === 0) {
    container.innerHTML = `<div style="text-align:center;padding:20px;color:var(--text-muted);font-size:13px">
      ⚠️ Không xác định được tài khoản. Vui lòng đăng xuất và đăng nhập lại.
    </div>`;
    return;
  }

  container.innerHTML = `<div style="text-align:center;padding:20px;color:var(--text-muted);font-size:13px">Đang tải...</div>`;

  fetch(`${API}/approvals/user/${userId}`)
    .then(r => r.json())
    .then(list => {
      const filtered = ownerRequestFilter === 'all'
        ? list
        : list.filter(a => (a.Status||a.status||'pending').toLowerCase() === ownerRequestFilter);

      if (!filtered.length) {
        container.innerHTML = `<div style="text-align:center;padding:24px;color:var(--text-muted);font-size:13px">
          ${ownerRequestFilter === 'all' ? '📭 Chưa có yêu cầu nào' : 'Không có yêu cầu ở trạng thái này'}
        </div>`;
        return;
      }

      const STATUS = { pending:'⏳ Chờ duyệt', approved:'✅ Đã duyệt', rejected:'❌ Từ chối' };
      const COLORS = { pending:'#d97706', approved:'#10b981', rejected:'#ef4444' };
      const BG     = { pending:'#fffbeb', approved:'#f0fdf4', rejected:'#fef2f2' };
      const ACTION_LABELS = { create_poi:'➕ Thêm mới', update_info:'✏️ Cập nhật', update_audio:'🎙️ Audio' };

      container.innerHTML = filtered.map(a => {
        const status  = (a.Status||a.status||'pending').toLowerCase();
        const created = new Date(a.CreatedAt||a.createdAt).toLocaleString('vi-VN',{day:'2-digit',month:'2-digit',year:'numeric',hour:'2-digit',minute:'2-digit'});
        const actionLabel = ACTION_LABELS[a.Action||a.action] || '📝 Yêu cầu';
        const note = a.AdminNote||a.adminNote;
        return `
        <div style="padding:12px 16px;border-bottom:1px solid var(--border)">
          <div style="display:flex;align-items:flex-start;gap:10px">
            <div style="flex:1;min-width:0">
              <div style="font-size:13px;font-weight:700;margin-bottom:2px">${a.LocationName||a.locationName||'POI mới'}</div>
              <div style="font-size:11px;color:var(--text-muted);margin-bottom:${note?'6px':'0'}">${actionLabel} &nbsp;·&nbsp; ${created}</div>
              ${note ? `<div style="font-size:11px;background:#fef9ec;border:1px solid #fde68a;border-radius:6px;padding:5px 8px;color:#92400e">💬 ${note}</div>` : ''}
            </div>
            <span style="font-size:11px;font-weight:600;padding:3px 8px;border-radius:20px;white-space:nowrap;background:${BG[status]||'#f8fafc'};color:${COLORS[status]||'#64748b'};border:1px solid ${COLORS[status]||'#e2e8f0'}40">
              ${STATUS[status]||status}
            </span>
          </div>
        </div>`;
      }).join('');
    })
    .catch(() => {
      container.innerHTML = `<div style="text-align:center;padding:20px;color:#ef4444;font-size:13px">
        ❌ Không tải được dữ liệu. Kiểm tra kết nối API.
      </div>`;
    });
}

function renderOwnerChart() {
  const ctx = document.getElementById('ownerChart')?.getContext('2d');
  if (!ctx) return;
  if (ownerChart) ownerChart.destroy();
  const myPoisIds = allPois.map(p => p.id||p.Id);
  const myVisits = historyData.filter(h => myPoisIds.includes(h.RestaurantId||h.restaurantId));

  // 7 ngày gần nhất
  const today = new Date();
  const days = Array.from({length:7}, (_,i) => {
    const d = new Date(today); d.setDate(d.getDate()-6+i);
    return d.toLocaleDateString('vi-VN',{day:'2-digit',month:'2-digit'});
  });
  const counts = {};
  days.forEach(d => counts[d] = 0);
  myVisits.forEach(h => {
    const d = new Date(h.Timestamp||h.timestamp).toLocaleDateString('vi-VN',{day:'2-digit',month:'2-digit'});
    if (d in counts) counts[d]++;
  });

  ownerChart = new Chart(ctx, {
    type: 'bar',
    data: {
      labels: days,
      datasets: [{
        label: 'Lượt ghé thăm',
        data: days.map(d => counts[d]),
        backgroundColor: days.map((_,i) => i===6 ? 'rgba(37,99,235,0.9)' : 'rgba(16,185,129,0.7)'),
        borderRadius: 6,
        borderSkipped: false
      }]
    },
    options: {
      responsive: true, maintainAspectRatio: false,
      plugins: { legend:{display:false}, tooltip:{callbacks:{title:t=>`Ngày ${t[0].label}`}} },
      scales: { y:{beginAtZero:true,grid:{color:'#f1f5f9'},ticks:{stepSize:1}}, x:{grid:{display:false}} }
    }
  });
}
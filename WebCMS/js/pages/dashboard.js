// ══ ANALYTICS ══
async function loadAnalytics() {
  try { const res=await fetch(`${API}/analytics`); historyData=await res.json(); renderStatsCards(); renderHistory(); renderDashboardRecent(); }
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
    
    let icon = 'activity', action = 'Tương tác ẩn', color = '#64748b', bg = '#f8fafc';
    if(evt.includes('enter')) {
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
    if(evt.includes('enter')) action = 'Ghé thăm';
    else if(evt.includes('click')) action = 'Xem thông tin';
    else if(evt.includes('exit')) action = 'Rời khỏi';
    return `<tr><td><strong>${h.RestaurantName||h.restaurantName||'POI'}</strong></td><td><span class="badge badge-info" style="font-size:11px;background:#f8fafc;color:#475569;border:1px solid #e2e8f0;font-weight:500">${action}</span></td><td style="color:var(--text-muted);font-size:12px">${new Date(h.Timestamp||h.timestamp).toLocaleTimeString('vi-VN')}</td></tr>`
  }).join('')||`<tr><td colspan="3" style="text-align:center;color:var(--text-muted);padding:20px">Chưa có hoạt động</td></tr>`;
}

async function loadStats() {
  try { const res=await fetch(`${API}/analytics/stats`); statsData=await res.json(); } catch(e) { statsData=null; }
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

// OWNER DASHBOARD — chỉ hiện data quán của mình
// ══════════════════════════════════════════════════
let ownerChart = null;

async function renderOwnerDashboard() {
  renderOwnerStats();
  renderOwnerPoiList();
  renderOwnerRequests();
  renderOwnerChart();
}

function renderOwnerStats() {
  const grid = document.getElementById('owner-stats-grid');
  if (!grid) return;
  const myPois = allPois;
  const myVisits = historyData.filter(h => {
    const pid = h.RestaurantId || h.restaurantId;
    return myPois.some(p => (p.id||p.Id) === pid);
  });
  const today = myVisits.filter(h =>
    new Date(h.Timestamp||h.timestamp).toDateString() === new Date().toDateString()
  ).length;

  // Đếm TTS đã có
  const hasAudio = myPois.filter(p => !!(p.ttsScript||p.TtsScript)).length;

  grid.innerHTML = `
    <div class="stat-card">
      <div class="stat-icon blue"><i data-lucide="map-pin"></i></div>
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
        <div class="stat-trend up">↑ ${today} hôm nay</div>
      </div>
    </div>
    <div class="stat-card">
      <div class="stat-icon green"><i data-lucide="mic"></i></div>
      <div class="stat-info">
        <p class="text-muted">Audio TTS</p>
        <h2 class="stat-val">${hasAudio}/${myPois.length}</h2>
        <div class="stat-trend ${hasAudio===myPois.length?'up':''}">${hasAudio===myPois.length?'✅ Đầy đủ':'⚠️ Chưa đủ'}</div>
      </div>
    </div>`;
  lucide.createIcons();
}

function renderOwnerPoiList() {
  const container = document.getElementById('owner-poi-list');
  if (!container) return;
  if (!allPois.length) {
    container.innerHTML = `<div style="text-align:center;padding:30px;color:var(--text-muted)">
      Bạn chưa được gán quán nào. Liên hệ Admin để được gán!
    </div>`;
    return;
  }
  container.innerHTML = allPois.map(p => {
    const hasVi = !!(p.ttsScript||p.TtsScript);
    const hasEn = !!(p.ttsScriptEn||p.TtsScriptEn);
    const hasZh = !!(p.ttsScriptZh||p.TtsScriptZh);
    const visits = historyData.filter(h => (h.RestaurantId||h.restaurantId) === (p.id||p.Id)).length;
    return `
    <div style="display:flex;align-items:center;gap:12px;padding:12px 16px;border-bottom:1px solid var(--border);cursor:pointer;transition:.15s"
         onmouseover="this.style.background='#f8faff'" onmouseout="this.style.background=''"
         onclick="openEditPoiForm(${JSON.stringify(p).replace(/"/g,'&quot;')})">
      <img src="${getImgUrl(p)}" onerror="this.src='https://via.placeholder.com/40?text=?'"
           style="width:44px;height:44px;border-radius:8px;object-fit:cover;flex-shrink:0">
      <div style="flex:1;min-width:0">
        <div style="font-size:13px;font-weight:600">${p.name||p.Name}</div>
        <div style="font-size:11px;color:var(--text-muted);margin-top:2px">
          ⭐ ${(p.rating||p.Rating||0).toFixed(1)} &nbsp;·&nbsp; 🕒 ${p.openHours||p.OpenHours||'—'} &nbsp;·&nbsp; 👣 ${visits} lượt
        </div>
      </div>
      <div style="display:flex;gap:4px">
        <span title="VI" style="width:22px;height:22px;border-radius:50%;background:${hasVi?'#22c55e':'#e2e8f0'};display:flex;align-items:center;justify-content:center;font-size:9px;font-weight:700;color:${hasVi?'#fff':'#94a3b8'}">VI</span>
        <span title="EN" style="width:22px;height:22px;border-radius:50%;background:${hasEn?'#22c55e':'#e2e8f0'};display:flex;align-items:center;justify-content:center;font-size:9px;font-weight:700;color:${hasEn?'#fff':'#94a3b8'}">EN</span>
        <span title="ZH" style="width:22px;height:22px;border-radius:50%;background:${hasZh?'#22c55e':'#e2e8f0'};display:flex;align-items:center;justify-content:center;font-size:9px;font-weight:700;color:${hasZh?'#fff':'#94a3b8'}">ZH</span>
      </div>
      <button class="btn btn-primary btn-sm" onclick="event.stopPropagation();openEditPoiForm(${JSON.stringify(p).replace(/"/g,'&quot;')})">
        <i data-lucide="edit-3"></i> Sửa
      </button>
    </div>`;
  }).join('');
  lucide.createIcons();
}

function renderOwnerRequests() {
  const container = document.getElementById('owner-requests-list');
  if (!container) return;
  const userId = parseInt(sessionStorage.getItem('cms_userid') || '0');

  fetch(`${API}/approvals/user/${userId}`)
    .then(r => r.json())
    .then(list => {
      if (!list.length) {
        container.innerHTML = `<div style="text-align:center;padding:20px;color:var(--text-muted);font-size:13px">Chưa có yêu cầu nào</div>`;
        return;
      }
      const STATUS = { pending:'⏳ Chờ duyệt', approved:'✅ Đã duyệt', rejected:'❌ Từ chối' };
      const BADGE  = { pending:'badge-warning', approved:'badge-success', rejected:'badge-danger' };
      container.innerHTML = list.slice(0,5).map(a => {
        const status = a.Status||a.status||'pending';
        const created = new Date(a.CreatedAt||a.createdAt).toLocaleDateString('vi-VN');
        return `<div style="display:flex;align-items:center;gap:10px;padding:10px 0;border-bottom:1px solid var(--border)">
          <div style="flex:1;min-width:0">
            <div style="font-size:12px;font-weight:600">${a.LocationName||a.locationName||'POI mới'}</div>
            <div style="font-size:11px;color:var(--text-muted)">${created}</div>
          </div>
          <span class="badge ${BADGE[status]||'badge-neutral'}" style="font-size:10px;white-space:nowrap">${STATUS[status]||status}</span>
          ${status==='rejected'&&(a.AdminNote||a.adminNote)?`<span title="${a.AdminNote||a.adminNote}" style="cursor:help;font-size:12px">💬</span>`:''}
        </div>`;
      }).join('');
    })
    .catch(() => {
      container.innerHTML = `<div style="text-align:center;padding:20px;color:var(--text-muted);font-size:13px">Không tải được dữ liệu</div>`;
    });
}

function renderOwnerChart() {
  const ctx = document.getElementById('ownerChart')?.getContext('2d');
  if (!ctx) return;
  if (ownerChart) ownerChart.destroy();
  const myPoisIds = allPois.map(p => p.id||p.Id);
  const myVisits = historyData.filter(h => myPoisIds.includes(h.RestaurantId||h.restaurantId));
  const counts = {};
  myVisits.forEach(h => {
    const d = new Date(h.Timestamp||h.timestamp).toLocaleDateString('vi-VN',{month:'short',day:'numeric'});
    counts[d] = (counts[d]||0) + 1;
  });
  const labels = Object.keys(counts);
  const data   = Object.values(counts);
  if (!labels.length) { labels.push('Chưa có data'); data.push(0); }
  ownerChart = new Chart(ctx, {
    type: 'bar',
    data: { labels, datasets: [{ label:'Lượt ghé thăm', data, backgroundColor:'rgba(16,185,129,0.8)', borderRadius:6 }] },
    options: { responsive:true, maintainAspectRatio:false, plugins:{legend:{display:false}},
      scales: { y:{beginAtZero:true,grid:{color:'#f1f5f9'}}, x:{grid:{display:false}} } }
  });
}

function applyPagePermissions(role) {
  // Các nút chỉ admin mới được dùng
  const adminOnlyBtns = [
    'btn-delete-poi',   // xóa địa điểm
    'btn-delete-user',  // xóa user
  ];
  adminOnlyBtns.forEach(id => {
    const el = document.getElementById(id);
    if (el && role !== 'admin') el.style.display = 'none';
  });
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
    const labels = { admin:'👑 Super Admin', owner:'🏪 Chủ quán', user:'👤 Người dùng' };
    roleEl.textContent = labels[role] || role;
  }
  if (avatarEl) {
    const initials = fullname.split(' ').map(w=>w[0]).slice(-2).join('').toUpperCase() || username[0].toUpperCase();
    avatarEl.textContent = initials;
    avatarEl.style.background = role==='admin' ? '#2563eb' : role==='owner' ? '#10b981' : '#64748b';
  }
}

// ══════════════════════════════════════════════════

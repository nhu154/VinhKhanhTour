// ══════════════════════════════════════════════════════════════════
//  ANALYTICS + HEATMAP  —  VinhKhanhTour WebCMS
//  Nâng cấp: layer chọn, popup chi tiết, stats panel, toggle,
//  clustering, heat intensity slider, poi markers, mini-chart
// ══════════════════════════════════════════════════════════════════

// ── State ─────────────────────────────────────────────────────────
let _avgDurationData = null;
let _heatmapData     = null;
let _heatmapDays     = 7;

let _gHeatMap     = null;
let _heatmapLayer = null;
let _poiMarkers   = [];
let _infoWindow   = null;
let _heatVisible  = true;
let _poisVisible  = true;
let _heatRadius   = 45;
let _heatOpacity  = 0.8;
let _currentLayer = 'combined'; // combined | checkin | view | booking

// Khu vực Vĩnh Khánh, Q4, TP.HCM
const VK_CENTER = { lat: 10.7614, lng: 106.7045 };
const VK_BOUNDS = { north: 10.7655, south: 10.7578, east: 106.7082, west: 106.7002 };

// ── Wait helper ───────────────────────────────────────────────────
function _waitForMaps(cb, attempt) {
  attempt = attempt || 0;
  if (attempt > 40) { console.error('[Heatmap] Timeout'); return; }
  const ok = typeof google !== 'undefined'
    && google.maps?.Map
    && google.maps?.visualization?.HeatmapLayer;
  ok ? cb() : setTimeout(() => _waitForMaps(cb, attempt + 1), 400);
}

// ══════════════════════════════════════════════════════════════════
//  ENTRY POINT
// ══════════════════════════════════════════════════════════════════
async function loadDeepAnalytics() {
  renderAvgDurationSkeleton();
  _renderHeatmapShell();
  try {
    await Promise.all([fetchAvgDuration(), fetchHeatmap(_heatmapDays)]);
  } catch(e) { console.warn('[Analytics]', e); }
  renderAvgDurationTable();
  _waitForMaps(initHeatmapMap);
}

// ══════════════════════════════════════════════════════════════════
//  FETCH
// ══════════════════════════════════════════════════════════════════
async function fetchAvgDuration() {
  try {
    const r = await fetch(`${API}/analytics/avg-duration`, { cache: 'no-store' });
    _avgDurationData = await r.json();
  } catch(e) {
    console.error('[Analytics] avg-duration:', e);
    _avgDurationData = [];
  }
}

async function fetchHeatmap(days) {
  days = days || _heatmapDays || 7;
  try {
    const url = `${API}/analytics/heatmap?days=${days}&layer=${_currentLayer}`;
    const r = await fetch(url, { cache: 'no-store' });
    if (!r.ok) throw new Error(`HTTP ${r.status}`);
    const data = await r.json();
    _heatmapData = Array.isArray(data) ? data : [];
    console.log(`[Heatmap] ${_heatmapData.length} điểm (${days}d, layer=${_currentLayer})`);
  } catch(e) {
    console.error('[Heatmap] fetch:', e);
    _heatmapData = [];
  }
}

// ══════════════════════════════════════════════════════════════════
//  AVG DURATION TABLE
// ══════════════════════════════════════════════════════════════════
function renderAvgDurationSkeleton() {
  const el = document.getElementById('analytics-duration-body');
  if (!el) return;
  el.innerHTML = Array(5).fill(0).map(() => `
    <tr>
      <td><div style="height:14px;background:var(--border);border-radius:4px;width:140px;animation:pulse 1.5s infinite"></div></td>
      <td><div style="height:14px;background:var(--border);border-radius:4px;width:50px;animation:pulse 1.5s infinite"></div></td>
      <td><div style="height:14px;background:var(--border);border-radius:4px;width:80px;animation:pulse 1.5s infinite"></div></td>
      <td><div style="height:8px;background:var(--border);border-radius:99px;animation:pulse 1.5s infinite"></div></td>
    </tr>`).join('');
}

function renderAvgDurationTable() {
  const el = document.getElementById('analytics-duration-body');
  if (!el) return;
  if (!_avgDurationData?.length) {
    el.innerHTML = `<tr><td colspan="3" style="text-align:center;padding:40px;color:var(--text-muted)">
      <div style="font-size:28px;margin-bottom:8px">🎵</div>
      Chưa có dữ liệu thời gian nghe.<br>
      <span style="font-size:12px">App cần ghi lại thời lượng khi phát audio.</span>
    </td></tr>`;
    return;
  }
  const maxAvg = Math.max(..._avgDurationData.map(d => d.avgSeconds || d.AvgSeconds || 0), 1);
  el.innerHTML = _avgDurationData.slice(0, 10).map((d, i) => {
    const name     = d.poiName     || d.PoiName     || `POI #${d.poiId || d.PoiId}`;
    const plays    = d.playCount   || d.PlayCount   || 0;
    const avgSec   = d.avgSeconds  || d.AvgSeconds  || 0;
    const totalSec = d.totalSeconds|| d.TotalSeconds|| 0;
    const pct      = Math.round((avgSec / maxAvg) * 100);
    
    // Determine color based on engagement
    let color = '#d1d5db'; // muted
    let label = 'Ngắn';
    if (avgSec >= 60) { color = '#10b981'; label = 'Sâu'; } // Green (Deep)
    else if (avgSec >= 30) { color = '#3b82f6'; label = 'Vừa'; } // Blue (Medium)
    else if (avgSec > 5) { color = '#f59e0b'; label = 'Lướt'; } // Orange (Brief)

    return `<tr>
      <td>
        <div style="font-weight:700;font-size:13px;color:var(--text-main);margin-bottom:2px">${name}</div>
        <div style="font-size:11px;color:var(--text-muted)">${plays} lượt · Tổng ${fmtSec(totalSec)}</div>
      </td>
      <td style="text-align:center">
        <span style="font-size:16px;font-weight:800;color:${color}">${fmtSec(avgSec)}</span>
      </td>
      <td>
        <div style="display:flex;align-items:center;gap:8px">
          <div style="flex:1;height:6px;background:#f1f5f9;border-radius:10px;overflow:hidden">
            <div style="width:${pct}%;height:100%;background:${color};border-radius:10px;transition:width .8s ease"></div>
          </div>
          <span style="font-size:10px;font-weight:700;color:${color};width:32px">${label}</span>
        </div>
      </td>
    </tr>`;
  }).join('');
}

function fmtSec(s) {
  if (!s || s < 1) return '0s';
  if (s < 60) return `${Math.round(s)}s`;
  return `${Math.floor(s/60)}m${Math.round(s%60)}s`;
}

// ══════════════════════════════════════════════════════════════════
//  HEATMAP SHELL — render khung UI (chỉ 1 lần)
// ══════════════════════════════════════════════════════════════════
function _renderHeatmapShell() {
  const container = document.getElementById('heatmap-container');
  if (!container || container.dataset.ready) return;
  container.dataset.ready = '1';

  container.innerHTML = `
    <div style="position:relative;width:100%;height:100%;background:#e2e8f0;overflow:hidden">
      
      <!-- ── Map Wrap ── -->
      <div id="heatmap-wrap" style="width:100%;height:100%;z-index:1"></div>

      <!-- ── Floating Top Toolbar ── -->
      <div style="position:absolute;top:20px;left:20px;right:20px;z-index:10;display:flex;justify-content:space-between;align-items:center;pointer-events:none">
        
        <!-- Left: Title & Refresh -->
        <div style="pointer-events:auto;background:rgba(255,255,255,0.85);backdrop-filter:blur(12px);padding:10px 20px;border-radius:16px;box-shadow:0 8px 32px rgba(0,0,0,0.1);border:1px solid rgba(255,255,255,0.5);display:flex;align-items:center;gap:16px">
          <div>
            <h3 style="margin:0;font-size:16px;font-weight:800;color:var(--text-main)">🗺 Heatmap Vĩnh Khánh</h3>
            <p id="heatmap-point-count" style="margin:2px 0 0;font-size:11px;color:var(--text-muted)">Đang tải tối ưu dữ liệu...</p>
          </div>
          <button onclick="refreshHeatmap()" title="Làm mới dữ liệu" style="width:36px;height:36px;border-radius:10px;border:1px solid var(--border);background:#fff;cursor:pointer;display:flex;align-items:center;justify-content:center;color:var(--text-muted);transition:.2s">
            <i data-lucide="refresh-cw" style="width:16px;height:16px"></i>
          </button>
          <button onclick="clearAllAnalytics()" title="Xóa toàn bộ thống kê" style="width:36px;height:36px;border-radius:10px;border:1px solid rgba(239, 68, 68, 0.2);background:rgba(239, 68, 68, 0.05);cursor:pointer;display:flex;align-items:center;justify-content:center;color:#ef4444;transition:.2s">
            <i data-lucide="trash-2" style="width:16px;height:16px"></i>
          </button>
        </div>

        <!-- Right: Layer & Time Controls -->
        <div style="pointer-events:auto;background:rgba(255,255,255,0.85);backdrop-filter:blur(12px);padding:6px;border-radius:16px;box-shadow:0 8px 32px rgba(0,0,0,0.1);border:1px solid rgba(255,255,255,0.5);display:flex;align-items:center;gap:6px">
          
          <!-- Days Selection -->
          <div style="display:flex;gap:2px;background:rgba(0,0,0,0.05);border-radius:10px;padding:3px">
            <button class="hm-chip hm-day active" data-days="7"  onclick="changeHeatmapDays(7,this)">7N</button>
            <button class="hm-chip hm-day"        data-days="30" onclick="changeHeatmapDays(30,this)">30N</button>
            <button class="hm-chip hm-day"        data-days="90" onclick="changeHeatmapDays(90,this)">90N</button>
          </div>

          <div style="width:1px;height:24px;background:rgba(0,0,0,0.1)"></div>

          <!-- Layer Select -->
          <select id="hm-layer-select" onchange="changeHeatmapLayer(this.value)"
            style="font-size:12px;padding:6px 12px;border-radius:10px;border:none;background:transparent;cursor:pointer;color:var(--text-main);font-weight:700;outline:none;appearance:none;padding-right:24px;background-image:url('data:image/svg+xml;utf8,<svg width=\"24\" height=\"24\" fill=\"none\" stroke=\"%2394a3b8\" stroke-width=\"2\" viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path stroke-linecap=\"round\" stroke-linejoin=\"round\" d=\"m19 9-7 7-7-7\"></path></svg>');background-repeat:no-repeat;background-position:right center;background-size:14px">
            <option value="combined">Tổng hợp</option>
            <option value="checkin">Check-in</option>
            <option value="view">Lượt nghe</option>
          </select>
        </div>
      </div>

      <!-- ── Side Panel (Stats) ── -->
      <div style="position:absolute;top:100px;right:20px;z-index:10;width:280px;display:flex;flex-direction:column;gap:12px">
        
        <!-- Summary Stats Card -->
        <div style="background:rgba(255,255,255,0.9);backdrop-filter:blur(16px);border-radius:20px;padding:20px;box-shadow:0 12px 40px rgba(0,0,0,0.12);border:1px solid rgba(255,255,255,0.6)">
          <div style="display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:12px">
            <div style="font-size:11px;font-weight:700;text-transform:uppercase;letter-spacing:1px;color:var(--text-muted)">Tổng tương tác</div>
            <div style="background:#dcfce7;color:#15803d;padding:2px 8px;border-radius:20px;font-size:10px;font-weight:700">LIVE</div>
          </div>
          <div style="font-size:32px;font-weight:900;color:#1e3a8a;letter-spacing:-1px" class="hm-total-val">—</div>
          <div style="font-size:12px;color:var(--text-muted);margin-top:4px">Dữ liệu <span id="hm-stat-days">7</span> ngày qua</div>
          
          <div style="margin:16px 0;height:1px;background:linear-gradient(90deg, transparent, rgba(0,0,0,0.06), transparent)"></div>
          
          <div style="font-size:11px;font-weight:700;text-transform:uppercase;letter-spacing:1px;color:var(--text-muted);margin-bottom:6px">Khu vực nhộn nhịp nhất</div>
          <div id="hm-stat-hotname" style="font-size:14px;font-weight:700;color:var(--text-main);margin-bottom:2px">Đang xác định...</div>
          <div id="hm-stat-hotweight" style="font-size:11px;color:var(--primary);font-weight:600">...</div>
        </div>

      </div>

      <!-- Intensity Controls removed per user request -->

    </div>`;

  // Inject styles
  if (!document.getElementById('hm-styles')) {
    const s = document.createElement('style');
    s.id = 'hm-styles';
    s.textContent = `
      .hm-chip {
        font-size: 12px; font-weight: 500; padding: 4px 10px; border: none;
        border-radius: 6px; cursor: pointer; background: transparent;
        color: var(--text-muted); transition: all .15s;
      }
      .hm-chip.active {
        background: var(--card); color: var(--primary);
        box-shadow: 0 1px 4px rgba(0,0,0,0.1); font-weight: 600;
      }
      .hm-chip:hover:not(.active) { color: var(--text-main); }
      .hm-poi-row {
        display: flex; align-items: center; gap: 8px; cursor: pointer;
        padding: 4px 0; transition: opacity .15s;
      }
      .hm-poi-row:hover { opacity: .75; }
      @keyframes hm-spin { to { transform: rotate(360deg) } }
    `;
    document.head.appendChild(s);
  }

  if (typeof lucide !== 'undefined') lucide.createIcons();
}

// ══════════════════════════════════════════════════════════════════
//  MAP INIT
// ══════════════════════════════════════════════════════════════════
function initHeatmapMap() {
  const wrap = document.getElementById('heatmap-wrap');
  if (!wrap) return;

  if (!_gHeatMap) {
    wrap.innerHTML = '';
    const div = document.createElement('div');
    div.style.cssText = 'width:100%;height:100%';
    wrap.appendChild(div);

    _gHeatMap = new google.maps.Map(div, {
      center:            VK_CENTER,
      zoom:              17,
      minZoom:           14,
      maxZoom:           21,
      mapTypeId:         'roadmap',
      mapTypeControl:    true,
      mapTypeControlOptions: {
        style: google.maps.MapTypeControlStyle.DROPDOWN_MENU,
        mapTypeIds: ['roadmap', 'satellite']
      },
      streetViewControl: false,
      fullscreenControl: true,
      zoomControl:       true,
      gestureHandling:   'cooperative',
      restriction: { latLngBounds: VK_BOUNDS, strictBounds: false },
      styles: _mapStyle()
    });

    _infoWindow = new google.maps.InfoWindow();

    // Zoom → adjust heatmap radius
    _gHeatMap.addListener('zoom_changed', () => {
      const z = _gHeatMap.getZoom();
      // Auto-adjust but only if zoom changed significantly and user isn't overriding
      const autoR = Math.max(15, Math.min(180, z * 4));
      const slider = document.getElementById('hm-radius-range');
      const valEl = document.getElementById('hm-radius-val');
      if (slider && !slider.dataset.manual) { 
        slider.value = autoR; 
        if (valEl) valEl.textContent = `${Math.round(autoR)}px`; 
        _heatRadius = autoR;
        if (_heatmapLayer) _heatmapLayer.set('radius', _heatRadius);
      }
    });

    _drawVKOverlay();
    _drawPoiMarkers();
  }

  _applyHeatLayer();
  _updateSidebarStats();
}

// ══════════════════════════════════════════════════════════════════
//  MAP STYLE
// ══════════════════════════════════════════════════════════════════
function _mapStyle() {
  return [
    { featureType:'poi',       elementType:'labels',   stylers:[{visibility:'off'}] },
    { featureType:'transit',                           stylers:[{visibility:'off'}] },
    { featureType:'road',      elementType:'geometry', stylers:[{color:'#f5f5f5'}] },
    { featureType:'road.arterial', elementType:'geometry', stylers:[{color:'#ffffff'}] },
    { featureType:'water',     elementType:'geometry', stylers:[{color:'#c9e8f5'}] },
    { featureType:'landscape', elementType:'geometry', stylers:[{color:'#f8fafc'}] },
    { featureType:'road', elementType:'labels.text.fill', stylers:[{color:'#666'}] },
  ];
}

// ══════════════════════════════════════════════════════════════════
//  VK OVERLAY (đường phố + label)
// ══════════════════════════════════════════════════════════════════
function _drawVKOverlay() {
  if (!_gHeatMap) return;

  new google.maps.Rectangle({
    bounds: VK_BOUNDS, map: _gHeatMap,
    fillColor: '#2563eb', fillOpacity: 0.04,
    strokeColor: '#2563eb', strokeWeight: 1.5, strokeOpacity: 0.35, zIndex: 1
  });

  new google.maps.Polyline({
    path: [
      {lat:10.7582,lng:106.7010},{lat:10.7591,lng:106.7018},{lat:10.7600,lng:106.7027},
      {lat:10.7609,lng:106.7035},{lat:10.7618,lng:106.7043},{lat:10.7627,lng:106.7051},
      {lat:10.7636,lng:106.7059},{lat:10.7645,lng:106.7068},
    ],
    map: _gHeatMap, geodesic: true,
    strokeColor: '#1e40af', strokeOpacity: 0.5, strokeWeight: 2.5, zIndex: 2
  });
}

// ══════════════════════════════════════════════════════════════════
//  POI MARKERS từ allPois
// ══════════════════════════════════════════════════════════════════
function _drawPoiMarkers() {
  if (!_gHeatMap) return;
  _poiMarkers.forEach(m => m.setMap(null));
  _poiMarkers = [];

  const pois = typeof allPois !== 'undefined' ? allPois : [];
  if (!pois.length) return;

  pois.forEach(p => {
    const lat = parseFloat(p.latitude  || p.Latitude  || 0);
    const lng = parseFloat(p.longitude || p.Longitude || 0);
    if (!lat || !lng) return;

    const marker = new google.maps.Marker({
      position: { lat, lng },
      map: _gHeatMap,
      title: p.name || p.Name || '',
      zIndex: 10,
      icon: {
        path: google.maps.SymbolPath.CIRCLE,
        scale: 7,
        fillColor: '#2563eb',
        fillOpacity: 0.95,
        strokeColor: '#ffffff',
        strokeWeight: 2
      }
    });

    const visits = typeof historyData !== 'undefined'
      ? historyData.filter(h => (h.RestaurantId||h.restaurantId) === (p.id||p.Id)).length
      : 0;

    marker.addListener('click', () => {
      const content = `
        <div style="font-family:'Outfit',sans-serif;padding:4px;min-width:200px">
          <div style="font-weight:700;font-size:14px;color:#0f172a;margin-bottom:4px">${p.name||p.Name||'POI'}</div>
          <div style="font-size:12px;color:#64748b;margin-bottom:8px">${p.category||p.Category||''}</div>
          <div style="display:flex;gap:12px;font-size:12px">
            <span>⭐ <strong>${(p.rating||p.Rating||0).toFixed(1)}</strong></span>
            <span>👣 <strong>${visits}</strong> lượt ghé</span>
          </div>
          ${p.openHours||p.OpenHours ? `<div style="font-size:11px;color:#64748b;margin-top:6px">🕒 ${p.openHours||p.OpenHours}</div>` : ''}
          ${p.address||p.Address ? `<div style="font-size:11px;color:#64748b;margin-top:3px">📍 ${p.address||p.Address}</div>` : ''}
        </div>`;
      _infoWindow.setContent(content);
      _infoWindow.open(_gHeatMap, marker);
    });

    _poiMarkers.push(marker);
  });
}

// ══════════════════════════════════════════════════════════════════
//  HEATMAP LAYER
// ══════════════════════════════════════════════════════════════════
function _applyHeatLayer() {
  if (!_gHeatMap) return;
  if (_heatmapLayer) { _heatmapLayer.setMap(null); _heatmapLayer = null; }

  const old = document.getElementById('hm-no-data');
  if (old) old.remove();

  if (!_heatmapData?.length) { _showNoData(); return; }

  const maxW = Math.max(..._heatmapData.map(p => +(p.weight||p.Weight||1)), 1);
  const pts = _heatmapData.map(p => ({
    location: new google.maps.LatLng(
      parseFloat(p.lat||p.Lat),
      parseFloat(p.lng||p.Lng)
    ),
    weight: (+(p.weight||p.Weight||1)) / maxW
  }));

  _heatmapLayer = new google.maps.visualization.HeatmapLayer({
    data:     pts,
    map:      _heatVisible ? _gHeatMap : null,
    radius:   _heatRadius,
    opacity:  _heatOpacity,
    gradient: [
      'rgba(0,0,0,0)',
      'rgba(59,130,246,0.5)',
      'rgba(34,197,94,0.65)',
      'rgba(234,179,8,0.8)',
      'rgba(249,115,22,0.95)',
      'rgba(239,68,68,1.0)'
    ]
  });

  // Fit bounds
  try {
    const b = new google.maps.LatLngBounds();
    pts.forEach(p => b.extend(p.location));
    if (!b.isEmpty()) {
      _gHeatMap.fitBounds(b, { top:60, right:60, bottom:60, left:60 });
      google.maps.event.addListenerOnce(_gHeatMap, 'idle', () => {
        const z = _gHeatMap.getZoom();
        if (z > 19) _gHeatMap.setZoom(19);
        if (z < 15) _gHeatMap.setZoom(15);
      });
    }
  } catch(_) {}
}

// ══════════════════════════════════════════════════════════════════
//  SIDEBAR STATS
// ══════════════════════════════════════════════════════════════════
function _updateSidebarStats() {
  const unitText = _currentLayer === 'combined' ? 'tương tác' : 
                   _currentLayer === 'checkin' ? 'check-in' : 
                   _currentLayer === 'view' ? 'lượt nghe' : 'tương tác';

  // Total
  const unitLabelEl = document.getElementById('hm-stat-unit-label');
  if (unitLabelEl) unitLabelEl.textContent = unitText;
  
  const daysEl  = document.getElementById('hm-stat-days');
  if (daysEl) daysEl.textContent = _heatmapDays;

  // Find total
  const total = _heatmapData?.reduce((s,p) => s + (+(p.weight||p.Weight||1)), 0) || 0;
  const totalValEl = document.querySelector('#hm-stat-total .hm-total-val');
  if (totalValEl) totalValEl.textContent = total > 0 ? total.toLocaleString('vi-VN') : '0';

  // Hotspot
  if (_heatmapData?.length) {
    const poiGroups = {};
    _heatmapData.forEach(p => {
      const name = _findPoiName(p);
      if (!name) return;
      const w = +(p.weight||p.Weight||1);
      if (!poiGroups[name]) poiGroups[name] = { name, weight: w };
      else poiGroups[name].weight += w;
    });
    
    const nameEl   = document.getElementById('hm-stat-hotname');
    const weightEl = document.getElementById('hm-stat-hotweight');
    
    if (Object.keys(poiGroups).length > 0) {
      const sorted = Object.values(poiGroups).sort((a,b) => b.weight - a.weight);
      const top = sorted[0];

      if (nameEl)   nameEl.textContent   = top.name;
      if (weightEl) weightEl.textContent = `${Math.round(top.weight * 100) / 100} điểm tổng hợp`;
    } else {
      if (nameEl)   nameEl.textContent   = "Chưa xác định";
      if (weightEl) weightEl.textContent = "...";
    }
  }

  // Top 5
  _renderTopList();

  // Update count text
  const countEl = document.getElementById('heatmap-point-count');
  if (countEl) {
    if (_heatmapData?.length) {
      const totalPts = _heatmapData.length;
      const totalW   = Math.round(_heatmapData.reduce((s,p) => s + (+(p.weight||p.Weight||1)), 0));
      countEl.innerHTML = `<span style="color:var(--primary);font-weight:600">${totalPts} cụm vị trí</span> &nbsp;·&nbsp; <strong>${totalW}</strong> ${unitText} &nbsp;·&nbsp; ${_heatmapDays} ngày gần đây`;
    } else {
      countEl.textContent = 'Chưa có dữ liệu — heatmap hiển thị khi có du khách tương tác hoặc di chuyển trong phố.';
    }
  }
}

function _findPoiName(pt) {
  const lat = parseFloat(pt.lat||pt.Lat||0);
  const lng = parseFloat(pt.lng||pt.Lng||0);
  if (typeof allPois === 'undefined' || !allPois.length) return null;
  
  let best = null, bestDist = Infinity;
  allPois.forEach(p => {
    const plat = parseFloat(p.latitude||p.Latitude||0);
    const plng = parseFloat(p.longitude||p.Longitude||0);
    // Simple Euclidean distance for performance; 0.001 deg is roughly 100m
    const d = Math.hypot(lat-plat, lng-plng);
    if (d < bestDist) { bestDist = d; best = p; }
  });

  // Threshold: Nếu khoảng cách > 0.002 (khoảng 200m) thì coi như ở ngoài khu vực tham quan
  // Điều này ngăn việc test App từ xa làm sai lệch thống kê quán nhộn nhịp nhất.
  if (bestDist > 0.002) return null;

  return best?.name||best?.Name||null;
}

function _renderTopList() {
  const el = document.getElementById('hm-top-list');
  if (!el || !_heatmapData?.length) return;
  
  const poiGroups = {};
  _heatmapData.forEach(p => {
    const name = _findPoiName(p);
    if (!name) return;
    const w = +(p.weight||p.Weight||1);
    if (!poiGroups[name]) {
      poiGroups[name] = { name: name, weight: w, lat: parseFloat(p.lat||p.Lat||0), lng: parseFloat(p.lng||p.Lng||0) };
    } else {
      poiGroups[name].weight += w;
    }
  });

  const sorted = Object.values(poiGroups).sort((a,b) => b.weight - a.weight).slice(0,5);
  const maxW = sorted[0].weight || 1;
  const labels = ['1.','2.','3.','4.','5.'];

  el.innerHTML = sorted.map((g,i) => {
    const pct  = Math.min(100, Math.round(g.weight / maxW * 100));
    const colors = ['#ef4444','#f97316','#eab308','#22c55e','#3b82f6'];
    const wStr = Math.round(g.weight);
    return `
      <div class="hm-poi-row" onclick="_panToPoint(${g.lat},${g.lng})">
        <span style="font-size:13px;width:20px;flex-shrink:0">${labels[i]}</span>
        <div style="flex:1;min-width:0">
          <div style="font-size:12px;font-weight:600;color:var(--text-main);white-space:nowrap;overflow:hidden;text-overflow:ellipsis">${g.name}</div>
          <div style="height:4px;border-radius:99px;background:var(--border);margin-top:3px;overflow:hidden">
            <div style="height:100%;width:${pct}%;background:${colors[i]};border-radius:99px;transition:width .6s ease"></div>
          </div>
        </div>
        <span style="font-size:11px;color:var(--text-muted);flex-shrink:0;margin-left:4px">${wStr}</span>
      </div>`;
  }).join('');
}


function _panToPoint(lat, lng) {
  if (!_gHeatMap) return;
  _gHeatMap.panTo({ lat: parseFloat(lat), lng: parseFloat(lng) });
  _gHeatMap.setZoom(19);
}

// ══════════════════════════════════════════════════════════════════
//  NO DATA overlay
// ══════════════════════════════════════════════════════════════════
function _showNoData() {
  const wrap = document.getElementById('heatmap-wrap');
  if (!wrap) return;
  const el = document.createElement('div');
  el.id = 'hm-no-data';
  el.style.cssText = `
    position:absolute;top:50%;left:50%;transform:translate(-50%,-50%);
    background:rgba(255,255,255,.96);backdrop-filter:blur(12px);
    border-radius:20px;padding:28px 36px;text-align:center;
    box-shadow:0 12px 40px rgba(0,0,0,0.15);z-index:20;
    border:1px solid rgba(37,99,235,.12);min-width:260px;max-width:320px`;
  el.innerHTML = `
    <div style="font-size:44px;margin-bottom:12px">🛰️</div>
    <div style="font-size:15px;font-weight:700;color:#0f172a;margin-bottom:8px">Chưa có dữ liệu GPS</div>
    <div style="font-size:12px;color:#64748b;line-height:1.7">
      Heatmap sẽ tự động hiển thị khi du khách<br>
      di chuyển trong phố <strong style="color:#2563eb">Vĩnh Khánh, Q4</strong><br>
      bằng app và GPS được ghi lại.
    </div>`;
  wrap.appendChild(el);
}

// ══════════════════════════════════════════════════════════════════
//  CONTROLS
// ══════════════════════════════════════════════════════════════════
async function changeHeatmapDays(days, el) {
  _heatmapDays = days;
  document.querySelectorAll('.hm-day').forEach(c => c.classList.toggle('active', parseInt(c.dataset.days) === days));
  // Legacy support
  document.querySelectorAll('.heatmap-day-chip').forEach(c => c.classList.toggle('active', parseInt(c.dataset.days) === days));
  const countEl = document.getElementById('heatmap-point-count');
  if (countEl) countEl.textContent = 'Đang tải...';
  await fetchHeatmap(days);
  _applyHeatLayer();
  _updateSidebarStats();
}

async function changeHeatmapLayer(layer) {
  _currentLayer = layer;
  const countEl = document.getElementById('heatmap-point-count');
  if (countEl) countEl.textContent = 'Đang tải...';
  await fetchHeatmap(_heatmapDays);
  _applyHeatLayer();
  _updateSidebarStats();
}

function toggleHeatLayer() {
  _heatVisible = !_heatVisible;
  if (_heatmapLayer) _heatmapLayer.setMap(_heatVisible ? _gHeatMap : null);
  const dot = document.getElementById('heat-toggle-dot');
  if (dot) dot.style.background = _heatVisible ? '#22c55e' : '#94a3b8';
  const btn = document.getElementById('btn-toggle-heat');
  if (btn) btn.style.opacity = _heatVisible ? '1' : '0.55';
}

function togglePoiLayer() {
  _poisVisible = !_poisVisible;
  _poiMarkers.forEach(m => m.setMap(_poisVisible ? _gHeatMap : null));
  const dot = document.getElementById('poi-toggle-dot');
  if (dot) dot.style.background = _poisVisible ? '#22c55e' : '#94a3b8';
  const btn = document.getElementById('btn-toggle-poi');
  if (btn) btn.style.opacity = _poisVisible ? '1' : '0.55';
}

function onRadiusChange(val) {
  _heatRadius = +val;
  const valEl = document.getElementById('hm-radius-val');
  if (valEl) valEl.textContent = `${val}px`;
  const slider = document.getElementById('hm-radius-range');
  if (slider) slider.dataset.manual = "true"; // User interaction detected
  if (_heatmapLayer) _heatmapLayer.set('radius', _heatRadius);
}

function onOpacityChange(val) {
  _heatOpacity = val / 100;
  const valEl = document.getElementById('hm-opacity-val');
  if (valEl) valEl.textContent = `${val}%`;
  if (_heatmapLayer) _heatmapLayer.set('opacity', _heatOpacity);
}

async function refreshHeatmap() {
  const countEl = document.getElementById('heatmap-point-count');
  if (countEl) countEl.textContent = 'Đang tải...';
  await fetchHeatmap(_heatmapDays);
  _applyHeatLayer();
  _updateSidebarStats();
  if (typeof showToast === 'function') showToast('Đã làm mới heatmap', 'success');
}

async function clearAllAnalytics() {
  const ok = await showConfirm(
    'Hành động dữ dội', 
    'Bạn có chắc chắn muốn xóa TOÀN BỘ dữ liệu thống kê (Heatmap, Lịch sử, Thời gian nghe) trên Server không?\n\nHành động này không thể hoàn tác!',
    'danger'
  );
  if (!ok) return;

  try {
    const r = await fetch(`${API}/analytics/clear`, { method: 'DELETE' });
    if (r.ok) {
      if (typeof showToast === 'function') showToast('Đã xóa sạch dữ liệu thống kê!', 'success');
      refreshHeatmap();
    } else {
      throw new Error(`Server returned ${r.status}`);
    }
  } catch(e) {
    console.error('[Analytics] clear error:', e);
    if (typeof showToast === 'function') showToast('Lỗi khi xóa dữ liệu!', 'error');
  }
}

function exportHeatmapCsv() {
  if (!_heatmapData?.length) {
    if (typeof showToast === 'function') showToast('Chưa có dữ liệu để xuất!', 'warning');
    return;
  }
  const rows = [['lat','lng','weight','poi_name']];
  _heatmapData.forEach(p =>
    rows.push([p.lat||p.Lat, p.lng||p.Lng, p.weight||p.Weight||1, _findPoiName(p)])
  );
  const csv  = rows.map(r => r.join(',')).join('\n');
  const blob = new Blob([csv], {type:'text/csv;charset=utf-8;'});
  const url  = URL.createObjectURL(blob);
  const a    = Object.assign(document.createElement('a'), {
    href: url,
    download: `heatmap_vinhkhanh_${_currentLayer}_${_heatmapDays}d_${new Date().toISOString().slice(0,10)}.csv`
  });
  a.click();
  URL.revokeObjectURL(url);
}

// ══════════════════════════════════════════════════════════════════
//  RESIZE
// ══════════════════════════════════════════════════════════════════
window.addEventListener('resize', () => {
  if (_gHeatMap) {
    const page = document.getElementById('page-analytics');
    if (page?.classList.contains('active'))
      google.maps.event.trigger(_gHeatMap, 'resize');
  }
});
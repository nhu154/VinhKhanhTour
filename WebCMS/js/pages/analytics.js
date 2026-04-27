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
    const url = `${API}/analytics/heatmap?days=${days}`;
    const r = await fetch(url, { cache: 'no-store' });
    if (!r.ok) throw new Error(`HTTP ${r.status}`);
    const data = await r.json();
    _heatmapData = Array.isArray(data) ? data : [];
    console.log(`[Heatmap] ${_heatmapData.length} điểm (${days}d, GPS only)`);
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
    const name     = d.poiName || d.PoiName || (d.poiId && d.poiId > 0 ? `Địa điểm #${d.poiId}` : 'Sự kiện hệ thống');
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
    <div style="position:relative;width:100%;height:100%;background:#f1f5f9;overflow:hidden">
      <div id="heatmap-wrap" style="width:100%;height:100%;z-index:1"></div>
      <div style="position:absolute;top:16px;left:16px;right:16px;z-index:10;display:flex;justify-content:space-between;align-items:flex-start;gap:12px;pointer-events:none">
        <div style="pointer-events:auto;background:rgba(13,17,35,0.92);backdrop-filter:blur(20px);padding:12px 18px;border-radius:16px;box-shadow:0 8px 32px rgba(0,0,0,0.5);border:1px solid rgba(255,255,255,0.08);display:flex;align-items:center;gap:14px">
          <div style="width:38px;height:38px;background:linear-gradient(135deg,#16a34a,#22c55e);border-radius:11px;display:flex;align-items:center;justify-content:center;font-size:19px;flex-shrink:0">📍</div>
          <div>
            <div style="font-size:14px;font-weight:800;color:#fff;letter-spacing:-0.3px">Mật độ Người Dùng Trực Tiếp</div>
            <div id="heatmap-point-count" style="font-size:11px;color:rgba(255,255,255,0.45);margin-top:2px">Đang tải...</div>
          </div>
          <div style="display:flex;gap:6px;margin-left:6px">
            <button onclick="refreshHeatmap()" title="Làm mới" style="width:34px;height:34px;border-radius:9px;border:1px solid rgba(255,255,255,0.1);background:rgba(255,255,255,0.05);cursor:pointer;display:flex;align-items:center;justify-content:center;color:rgba(255,255,255,0.55);font-size:15px">↻</button>
            <button onclick="clearAllAnalytics()" title="Xóa dữ liệu" style="width:34px;height:34px;border-radius:9px;border:1px solid rgba(239,68,68,0.3);background:rgba(239,68,68,0.08);cursor:pointer;display:flex;align-items:center;justify-content:center;color:#f87171;font-size:14px">🗑</button>
          </div>
        </div>
      </div>
      <div style="position:absolute;top:88px;right:16px;z-index:10;width:264px;display:flex;flex-direction:column;gap:10px">
        <div style="background:rgba(13,17,35,0.92);backdrop-filter:blur(20px);border-radius:18px;padding:18px;box-shadow:0 12px 40px rgba(0,0,0,0.5);border:1px solid rgba(255,255,255,0.08)">
          <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:14px">
            <div style="font-size:10px;font-weight:700;text-transform:uppercase;letter-spacing:1.5px;color:rgba(255,255,255,0.38)">📍 Người Dùng Trực Tiếp</div>
            <div style="display:flex;align-items:center;gap:6px">
              <div style="width:7px;height:7px;background:#22c55e;border-radius:50%;animation:hm-pulse 2s infinite"></div>
              <span id="hm-live-count" style="font-size:22px;font-weight:900;color:#22c55e;letter-spacing:-1px">0</span>
              <button onclick="toggleLiveLayer()" title="Ẩn/hiện vị trí" style="margin-left:4px;width:26px;height:26px;border-radius:7px;border:1px solid rgba(255,255,255,0.1);background:rgba(255,255,255,0.04);cursor:pointer;display:flex;align-items:center;justify-content:center">
                <span id="live-toggle-dot" style="display:inline-block;width:8px;height:8px;background:#22c55e;border-radius:50%"></span>
              </button>
            </div>
          </div>
          <div style="font-size:11px;color:rgba(255,255,255,0.35);margin-bottom:14px">người đang di chuyển trong khu vực</div>
          <div style="margin-bottom:12px;height:1px;background:rgba(255,255,255,0.06)"></div>
          <div id="hm-live-list" style="display:flex;flex-direction:column;gap:6px;max-height:260px;overflow-y:auto">
            <div style="font-size:12px;color:rgba(255,255,255,0.28);text-align:center;padding:16px 0">Chưa có người dùng online</div>
          </div>
          <div style="margin-top:10px;font-size:10px;color:rgba(255,255,255,0.2);text-align:center">↻ Tự động làm mới mỗi 10 giây</div>
        </div>
      </div>
      <div style="position:absolute;bottom:22px;left:50%;transform:translateX(-50%);z-index:10;background:rgba(13,17,35,0.92);backdrop-filter:blur(20px);padding:9px 20px;border-radius:40px;box-shadow:0 8px 32px rgba(0,0,0,0.5);border:1px solid rgba(255,255,255,0.08);display:flex;align-items:center;gap:14px">
        <span style="font-size:10px;font-weight:700;color:rgba(255,255,255,0.38);text-transform:uppercase;letter-spacing:1px">Mức độ đông đúc</span>
        <div style="display:flex;align-items:center;gap:6px">
          <span style="font-size:10px;color:rgba(255,255,255,0.38)">Thấp</span>
          <div style="width:110px;height:7px;border-radius:99px;background:linear-gradient(90deg,rgba(59,130,246,0.75),rgba(34,197,94,0.85),rgba(234,179,8,0.9),rgba(249,115,22,1),rgba(239,68,68,1))"></div>
          <span style="font-size:10px;color:rgba(255,255,255,0.38)">Cao</span>
        </div>
        <div style="width:1px;height:14px;background:rgba(255,255,255,0.1)"></div>
        <div style="display:flex;align-items:center;gap:6px">
          <div style="width:8px;height:8px;background:#3b82f6;border-radius:50%;border:2px solid rgba(255,255,255,0.7)"></div>
          <span style="font-size:10px;color:rgba(255,255,255,0.45)">Quán ăn / Điểm tham quan</span>
        </div>
      </div>
    </div>`;
  if (!document.getElementById('hm-styles')) {
    const s = document.createElement('style');
    s.id = 'hm-styles';
    s.textContent = `
      .hm-chip { font-size:12px;font-weight:600;padding:6px 14px;border:none;border-radius:9px;cursor:pointer;background:transparent;color:rgba(255,255,255,0.38);transition:all .15s; }
      .hm-chip.active { background:rgba(37,99,235,0.9);color:#fff;box-shadow:0 2px 8px rgba(37,99,235,0.5); }
      .hm-chip:hover:not(.active) { color:rgba(255,255,255,0.8);background:rgba(255,255,255,0.07); }
      .hm-poi-row { display:flex;align-items:center;gap:8px;cursor:pointer;padding:6px 8px;border-radius:10px;transition:background .15s; }
      .hm-poi-row:hover { background:rgba(255,255,255,0.06); }
      @keyframes hm-pulse { 0%,100%{opacity:1} 50%{opacity:0.35} }
    `;
    document.head.appendChild(s);
  }
  if (typeof lucide !== 'undefined') lucide.createIcons();
}
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
  _startLiveUsersTracking();
}

// ══════════════════════════════════════════════════════════════════
//  MAP STYLE
// ══════════════════════════════════════════════════════════════════
function _mapStyle() {
  return [
    {featureType:'poi',elementType:'labels',stylers:[{visibility:'off'}]},
    {featureType:'transit',stylers:[{visibility:'off'}]},
    {featureType:'road',elementType:'geometry',stylers:[{color:'#f5f5f5'}]},
    {featureType:'road.arterial',elementType:'geometry',stylers:[{color:'#ffffff'}]},
    {featureType:'road.highway',elementType:'geometry',stylers:[{color:'#dadada'}]},
    {featureType:'water',elementType:'geometry',stylers:[{color:'#c9e8f5'}]},
    {featureType:'landscape',elementType:'geometry',stylers:[{color:'#f8fafc'}]},
    {featureType:'road',elementType:'labels.text.fill',stylers:[{color:'#757575'}]},
    {featureType:'administrative',elementType:'geometry.stroke',stylers:[{color:'#c9c9c9'}]},
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
  const daysEl = document.getElementById('hm-stat-days');
  if (daysEl) daysEl.textContent = _heatmapDays;

  const total = _heatmapData?.reduce((acc, p) => acc + (+(p.weight||p.Weight||1)), 0) || 0;
  const totalValEl = document.querySelector('.hm-total-val');
  if (totalValEl) totalValEl.textContent = total > 0 ? total.toLocaleString('vi-VN') : '0';

  const hotspotEl = document.getElementById('hm-stat-hotspots');
  if (hotspotEl) hotspotEl.textContent = (_heatmapData?.length || 0).toLocaleString('vi-VN');

  const peakEl = document.getElementById('hm-stat-peak');
  if (peakEl && _heatmapData?.length) {
    const peak = Math.max(..._heatmapData.map(p => +(p.weight||p.Weight||1)));
    peakEl.textContent = peak.toLocaleString('vi-VN');
  }

  const countEl = document.getElementById('heatmap-point-count');
  if (countEl) {
    if (_heatmapData?.length) {
      const hs = _heatmapData.length;
      const tw = Math.round(total);
      countEl.innerHTML = `<span style="color:#60a5fa;font-weight:600">${hs.toLocaleString('vi-VN')} khu vực có khách</span>&nbsp;·&nbsp;<strong>${tw.toLocaleString('vi-VN')}</strong> lượt ghi nhận vị trí&nbsp;·&nbsp;${_heatmapDays} ngày gần đây`;
    } else {
      countEl.textContent = 'Chưa có dữ liệu — bản đồ sẽ hiển thị khi có khách sử dụng app và đi trong phố Vĩnh Khánh.';
    }
  }
  _renderTopList();
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
  if (!el) return;
  if (!_heatmapData?.length) {
    el.innerHTML = '<div style="font-size:12px;color:rgba(255,255,255,0.28);text-align:center;padding:12px 0">Chưa có dữ liệu</div>';
    const hs = document.getElementById('hm-stat-hotspots');
    const pk = document.getElementById('hm-stat-peak');
    if (hs) hs.textContent = '0';
    if (pk) pk.textContent = '0';
    return;
  }
  const poiGroups = {};
  _heatmapData.forEach(p => {
    const name = _findPoiName(p);
    if (!name) return;
    const w = +(p.weight||p.Weight||1);
    const lat = parseFloat(p.lat||p.Lat||0), lng = parseFloat(p.lng||p.Lng||0);
    if (!poiGroups[name]) poiGroups[name] = { name, weight: w, lat, lng };
    else poiGroups[name].weight += w;
  });
  const sorted = Object.values(poiGroups).sort((a,b) => b.weight - a.weight).slice(0,5);
  if (!sorted.length) { el.innerHTML = '<div style="font-size:12px;color:rgba(255,255,255,0.28);text-align:center;padding:12px 0">Không có dữ liệu POI</div>'; return; }
  const maxW = sorted[0].weight || 1;
  const rankColors = ['#ef4444','#f97316','#eab308','#22c55e','#3b82f6'];
  const rankIcons  = ['🥇','🥈','🥉','4️⃣','5️⃣'];
  el.innerHTML = sorted.map((g, i) => {
    const pct  = Math.min(100, Math.round(g.weight / maxW * 100));
    const wStr = Math.round(g.weight).toLocaleString('vi-VN');
    return `<div class="hm-poi-row" onclick="_panToPoint(${g.lat},${g.lng})">
      <span style="font-size:16px;width:22px;flex-shrink:0;text-align:center">${rankIcons[i]}</span>
      <div style="flex:1;min-width:0">
        <div style="font-size:12px;font-weight:600;color:#e2e8f0;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;margin-bottom:4px">${g.name}</div>
        <div style="height:4px;border-radius:99px;background:rgba(255,255,255,0.08);overflow:hidden">
          <div style="height:100%;width:${pct}%;background:${rankColors[i]};border-radius:99px;transition:width .7s ease"></div>
        </div>
      </div>
      <span style="font-size:11px;font-weight:700;color:${rankColors[i]};flex-shrink:0;margin-left:8px">${wStr}</span>
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
    <div style="font-size:15px;font-weight:700;color:#0f172a;margin-bottom:8px">Chưa có khách nào đi qua</div>
    <div style="font-size:12px;color:#64748b;line-height:1.7">
      Bản đồ sẽ hiển thị khi có khách<br>
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
      // Reset UI ngay lập tức trước khi fetch lại
      _heatmapData = [];
      const tv = document.querySelector('.hm-total-val');
      if (tv) tv.textContent = '0';
      const hs = document.getElementById('hm-stat-hotspots');
      const pk = document.getElementById('hm-stat-peak');
      if (hs) hs.textContent = '0';
      if (pk) pk.textContent = '0';
      const tl = document.getElementById('hm-top-list');
      if (tl) tl.innerHTML = '<div style="font-size:12px;color:rgba(255,255,255,0.28);text-align:center;padding:12px 0">Chưa có dữ liệu</div>';
      const pc = document.getElementById('heatmap-point-count');
      if (pc) pc.textContent = 'Chưa có dữ liệu';
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
    download: `heatmap_vinhkhanh_gps_${_heatmapDays}d_${new Date().toISOString().slice(0,10)}.csv`
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

// ══════════════════════════════════════════════════════════════════
//  REALTIME LIVE USERS LAYER
//  Polling vị trí GPS người dùng đang online, hiển thị marker trực tiếp
// ══════════════════════════════════════════════════════════════════

let _liveUserMarkers    = {};   // id → google.maps.Marker
let _liveTrackingTimer  = null;
let _liveVisible        = true;

/**
 * Bắt đầu polling vị trí user mỗi 10 giây
 */
function _startLiveUsersTracking() {
  _fetchAndRenderLiveUsers();
  if (_liveTrackingTimer) clearInterval(_liveTrackingTimer);
  _liveTrackingTimer = setInterval(_fetchAndRenderLiveUsers, 10000);
}

async function _fetchAndRenderLiveUsers() {
  try {
    const r = await fetch(`${API}/tracking/live-locations`, { cache: 'no-store' });
    if (!r.ok) return;
    const users = await r.json();
    _renderLiveUserMarkers(users);
    _updateLivePanel(users);
  } catch(e) {
    console.warn('[Live] fetch error:', e);
  }
}

function _renderLiveUserMarkers(users) {
  if (!_gHeatMap) return;

  const activeIds = new Set(users.map(u => u.id));

  // Xóa marker user đã offline
  Object.keys(_liveUserMarkers).forEach(id => {
    if (!activeIds.has(id)) {
      _liveUserMarkers[id].setMap(null);
      delete _liveUserMarkers[id];
    }
  });

  users.forEach((u, idx) => {
    const lat = parseFloat(u.lat);
    const lng = parseFloat(u.lng);
    if (!lat || !lng) return;

    const pos      = { lat, lng };
    const isRecent = u.secondsAgo < 60;
    const color    = u.isAnonymous ? '#f59e0b' : '#22c55e';

    const icon = {
      path:         google.maps.SymbolPath.CIRCLE,
      scale:        9,
      fillColor:    color,
      fillOpacity:  isRecent ? 1 : 0.45,
      strokeColor:  '#ffffff',
      strokeWeight: 2.5
    };

    if (_liveUserMarkers[u.id]) {
      _liveUserMarkers[u.id].setPosition(pos);
      _liveUserMarkers[u.id].setIcon(icon);
    } else {
      const marker = new google.maps.Marker({
        position: pos,
        map:      _liveVisible ? _gHeatMap : null,
        title:    u.label,
        zIndex:   100,
        icon
      });

      marker.addListener('click', () => {
        if (!_infoWindow) return;
        const agoText = u.secondsAgo < 60
          ? `${u.secondsAgo} giây trước`
          : `${Math.round(u.secondsAgo / 60)} phút trước`;
        _infoWindow.setContent(`
          <div style="padding:10px 14px;min-width:170px;font-family:system-ui,sans-serif">
            <div style="font-weight:700;font-size:14px;margin-bottom:6px">
              ${u.isAnonymous ? '👤' : '🧑'} ${u.label}
            </div>
            <div style="font-size:12px;color:#64748b;margin-bottom:4px">
              🕐 Cập nhật: ${agoText}
            </div>
            <div style="font-size:11px;color:#94a3b8">
              📍 ${lat.toFixed(5)}, ${lng.toFixed(5)}
            </div>
          </div>`);
        _infoWindow.open(_gHeatMap, marker);
      });

      _liveUserMarkers[u.id] = marker;
    }
  });
}

function _updateLivePanel(users) {
  const countEl = document.getElementById('hm-live-count');
  const listEl  = document.getElementById('hm-live-list');
  const subtitleEl = document.getElementById('heatmap-point-count');
  if (countEl) countEl.textContent = users.length;
  if (subtitleEl) {
    subtitleEl.textContent = users.length > 0
      ? `${users.length} người dùng đang trực tuyến`
      : 'Chưa có người dùng online';
  }
  if (!listEl) return;

  if (!users.length) {
    listEl.innerHTML = '<div style="font-size:12px;color:rgba(255,255,255,0.28);text-align:center;padding:10px 0">Chưa có người dùng online</div>';
    return;
  }

  listEl.innerHTML = users.map((u, i) => {
    const agoText = u.secondsAgo < 60
      ? `${u.secondsAgo}s trước`
      : `${Math.round(u.secondsAgo / 60)} phút trước`;
    const dotColor = u.secondsAgo < 60 ? '#22c55e' : '#f59e0b';
    return `
      <div class="hm-poi-row" onclick="_panToPoint(${u.lat},${u.lng})">
        <span style="font-size:16px;flex-shrink:0">${u.isAnonymous ? '👤' : '🧑'}</span>
        <div style="flex:1;min-width:0">
          <div style="font-size:12px;font-weight:600;color:#e2e8f0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">${u.label}</div>
          <div style="font-size:10px;color:rgba(255,255,255,0.35)">${agoText}</div>
        </div>
        <div style="width:8px;height:8px;border-radius:50%;background:${dotColor};flex-shrink:0"></div>
      </div>`;
  }).join('');
}

/**
 * Ẩn/hiện toàn bộ live user markers trên bản đồ
 */
function toggleLiveLayer() {
  _liveVisible = !_liveVisible;
  Object.values(_liveUserMarkers).forEach(m => m.setMap(_liveVisible ? _gHeatMap : null));
  const dot = document.getElementById('live-toggle-dot');
  if (dot) dot.style.background = _liveVisible ? '#22c55e' : '#94a3b8';
}
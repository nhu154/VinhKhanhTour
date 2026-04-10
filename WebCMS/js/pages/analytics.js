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
    el.innerHTML = `<tr><td colspan="4" style="text-align:center;padding:40px;color:var(--text-muted)">
      <div style="font-size:28px;margin-bottom:8px">🎵</div>
      Chưa có dữ liệu thời gian nghe.<br>
      <span style="font-size:12px">App cần ghi durationSeconds khi phát audio.</span>
    </td></tr>`;
    return;
  }
  const maxAvg = Math.max(..._avgDurationData.map(d => d.avgSeconds || d.AvgSeconds || 0), 1);
  el.innerHTML = _avgDurationData.map((d, i) => {
    const name     = d.poiName     || d.PoiName     || `POI #${d.poiId || d.PoiId}`;
    const plays    = d.playCount   || d.PlayCount   || 0;
    const avgSec   = d.avgSeconds  || d.AvgSeconds  || 0;
    const totalSec = d.totalSeconds|| d.TotalSeconds|| 0;
    const pct      = Math.round((avgSec / maxAvg) * 100);
    const medal    = i === 0 ? '🥇' : i === 1 ? '🥈' : i === 2 ? '🥉' : `${i+1}.`;
    const barColor = avgSec >= 60 ? '#16a34a' : avgSec >= 30 ? '#2563eb' : '#f59e0b';
    return `<tr>
      <td><span style="margin-right:6px">${medal}</span><span style="font-weight:600;font-size:13px">${name}</span></td>
      <td style="text-align:center"><span style="font-size:13px;font-weight:500">${plays}</span><span style="font-size:11px;color:var(--text-muted)"> lần</span></td>
      <td style="text-align:center">
        <span style="font-size:14px;font-weight:700;color:${barColor}">${fmtSec(avgSec)}</span>
        <div style="font-size:10px;color:var(--text-muted)">tổng ${fmtSec(totalSec)}</div>
      </td>
      <td style="min-width:120px">
        <div style="background:var(--bg-secondary,#f1f5f9);border-radius:99px;height:8px;overflow:hidden">
          <div style="height:100%;background:${barColor};border-radius:99px;width:${pct}%;transition:.6s ease"></div>
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
    <!-- ── Toolbar ── -->
    <div style="display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:16px;flex-wrap:wrap;gap:12px">
      <div>
        <h3 style="margin:0;display:flex;align-items:center;gap:8px;font-size:16px;font-weight:700">
          🗺 Heatmap vị trí du khách
          <span style="font-size:11px;font-weight:500;background:#eff6ff;color:#2563eb;padding:3px 8px;border-radius:20px;border:1px solid #bfdbfe">Vĩnh Khánh · Q4</span>
        </h3>
        <p id="heatmap-point-count" style="font-size:12px;color:var(--text-muted);margin:5px 0 0">Đang tải...</p>
      </div>

      <div style="display:flex;gap:6px;flex-wrap:wrap;align-items:center">
        <!-- Time range -->
        <div style="display:flex;gap:4px;background:var(--bg,#f1f5f9);border-radius:8px;padding:3px">
          <button class="hm-chip hm-day active" data-days="7"  onclick="changeHeatmapDays(7,this)">7 ngày</button>
          <button class="hm-chip hm-day"        data-days="30" onclick="changeHeatmapDays(30,this)">30 ngày</button>
          <button class="hm-chip hm-day"        data-days="90" onclick="changeHeatmapDays(90,this)">90 ngày</button>
        </div>

        <div style="width:1px;height:24px;background:var(--border)"></div>

        <!-- Layer picker -->
        <select id="hm-layer-select" onchange="changeHeatmapLayer(this.value)"
          style="font-size:13px;padding:6px 12px;border-radius:8px;border:1px solid var(--border);background:var(--card);cursor:pointer;color:var(--text-main);font-weight:600;min-width:110px;outline:none;appearance:none;background-image: url('data:image/svg+xml;utf8,<svg width=\"24\" height=\"24\" fill=\"none\" stroke=\"%2394a3b8\" stroke-width=\"2\" viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path stroke-linecap=\"round\" stroke-linejoin=\"round\" d=\"m19 9-7 7-7-7\"></path></svg>');background-repeat:no-repeat;background-position:right 8px center;background-size:14px;padding-right:28px;transition:all .2s">
          <option value="combined">Tổng hợp</option>
          <option value="checkin">Check-in</option>
          <option value="view">Lượt xem tĩnh</option>
        </select>

        <div style="width:1px;height:24px;background:var(--border)"></div>

        <!-- Toggle buttons -->
        <button id="btn-toggle-heat" onclick="toggleHeatLayer()"
          title="Bật/tắt heatmap"
          style="display:flex;align-items:center;gap:5px;padding:5px 10px;border-radius:8px;border:1px solid var(--border);background:var(--card);cursor:pointer;font-size:12px;font-weight:500;color:var(--text-main);transition:all .2s">
          🔥 Heatmap <span id="heat-toggle-dot" style="width:8px;height:8px;border-radius:50%;background:#22c55e;display:inline-block"></span>
        </button>
        <button id="btn-toggle-poi" onclick="togglePoiLayer()"
          title="Bật/tắt markers POI"
          style="display:flex;align-items:center;gap:5px;padding:5px 10px;border-radius:8px;border:1px solid var(--border);background:var(--card);cursor:pointer;font-size:12px;font-weight:500;color:var(--text-main);transition:all .2s">
          📍 POI <span id="poi-toggle-dot" style="width:8px;height:8px;border-radius:50%;background:#22c55e;display:inline-block"></span>
        </button>

        <div style="width:1px;height:24px;background:var(--border)"></div>

        <button onclick="refreshHeatmap()" title="Tải lại"
          style="display:flex;align-items:center;gap:5px;padding:5px 10px;border-radius:8px;border:1px solid var(--border);background:var(--card);cursor:pointer;font-size:12px;color:var(--text-muted)">
          <i data-lucide="refresh-cw" style="width:13px;height:13px"></i>
        </button>
        <button onclick="exportHeatmapCsv()" title="Xuất CSV"
          style="display:flex;align-items:center;gap:5px;padding:5px 10px;border-radius:8px;border:1px solid var(--border);background:var(--card);cursor:pointer;font-size:12px;color:var(--text-muted)">
          <i data-lucide="download" style="width:13px;height:13px"></i> CSV
        </button>
      </div>
    </div>

    <!-- ── Layout: map + sidebar ── -->
    <div style="display:grid;grid-template-columns:1fr 260px;gap:14px;align-items:start">

      <!-- Map -->
      <div style="position:relative">
        <div id="heatmap-wrap"
          style="width:100%;height:500px;border-radius:12px;background:#f1f5f9;overflow:hidden;
            position:relative;border:1px solid var(--border);box-shadow:inset 0 2px 8px rgba(0,0,0,0.04)">
        </div>

        <!-- Intensity controls (bottom-left overlay) -->
        <div style="position:absolute;bottom:14px;left:14px;z-index:10;
          background:rgba(255,255,255,0.96);border-radius:10px;padding:10px 14px;
          box-shadow:0 4px 16px rgba(0,0,0,0.12);border:1px solid var(--border);min-width:180px">
          <div style="font-size:11px;font-weight:700;color:var(--text-muted);margin-bottom:8px;text-transform:uppercase;letter-spacing:.5px">Điều chỉnh</div>
          <div style="display:flex;align-items:center;gap:8px;margin-bottom:6px">
            <span style="font-size:11px;color:var(--text-muted);width:52px">Bán kính</span>
            <input type="range" min="15" max="90" value="45" id="hm-radius-range"
              oninput="onRadiusChange(this.value)"
              style="flex:1;accent-color:#2563eb;cursor:pointer">
            <span id="hm-radius-val" style="font-size:11px;font-weight:600;color:#2563eb;width:28px">45px</span>
          </div>
          <div style="display:flex;align-items:center;gap:8px">
            <span style="font-size:11px;color:var(--text-muted);width:52px">Độ mờ</span>
            <input type="range" min="20" max="100" value="80" id="hm-opacity-range"
              oninput="onOpacityChange(this.value)"
              style="flex:1;accent-color:#2563eb;cursor:pointer">
            <span id="hm-opacity-val" style="font-size:11px;font-weight:600;color:#2563eb;width:28px">80%</span>
          </div>
        </div>

        <!-- Legend (bottom-right overlay) -->
        <div style="position:absolute;bottom:14px;right:14px;z-index:10;
          background:rgba(255,255,255,0.96);border-radius:10px;padding:10px 14px;
          box-shadow:0 4px 16px rgba(0,0,0,0.12);border:1px solid var(--border)">
          <div style="font-size:11px;font-weight:700;color:var(--text-muted);margin-bottom:6px;text-transform:uppercase;letter-spacing:.5px">Mật độ</div>
          <div style="display:flex;flex-direction:column;gap:3px">
            <div style="display:flex;align-items:center;gap:6px">
              <span style="width:12px;height:12px;border-radius:3px;background:#ef4444;display:inline-block;flex-shrink:0"></span>
              <span style="font-size:11px;color:var(--text-main)">Rất đông</span>
            </div>
            <div style="display:flex;align-items:center;gap:6px">
              <span style="width:12px;height:12px;border-radius:3px;background:#f97316;display:inline-block;flex-shrink:0"></span>
              <span style="font-size:11px;color:var(--text-main)">Đông</span>
            </div>
            <div style="display:flex;align-items:center;gap:6px">
              <span style="width:12px;height:12px;border-radius:3px;background:#eab308;display:inline-block;flex-shrink:0"></span>
              <span style="font-size:11px;color:var(--text-main)">Trung bình</span>
            </div>
            <div style="display:flex;align-items:center;gap:6px">
              <span style="width:12px;height:12px;border-radius:3px;background:#22c55e;display:inline-block;flex-shrink:0"></span>
              <span style="font-size:11px;color:var(--text-main)">Ít</span>
            </div>
            <div style="display:flex;align-items:center;gap:6px">
              <span style="width:12px;height:12px;border-radius:3px;background:#3b82f6;display:inline-block;flex-shrink:0"></span>
              <span style="font-size:11px;color:var(--text-main)">Rất ít</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Sidebar stats -->
      <div style="display:flex;flex-direction:column;gap:10px">
        <!-- Stats cards -->
        <div id="hm-stat-total" style="background:var(--card);border-radius:10px;padding:14px;border:1px solid var(--border)">
          <div style="font-size:11px;color:var(--text-muted);font-weight:600;text-transform:uppercase;letter-spacing:.5px;margin-bottom:6px">Tổng <span id="hm-stat-unit-label">tương tác</span></div>
          <div style="font-size:26px;font-weight:800;color:#2563eb" class="hm-total-val">—</div>
          <div style="font-size:11px;color:var(--text-muted);margin-top:2px">trong <span id="hm-stat-days">7</span> ngày qua</div>
        </div>
        <div id="hm-stat-hotzone" style="background:var(--card);border-radius:10px;padding:14px;border:1px solid var(--border)">
          <div style="font-size:11px;color:var(--text-muted);font-weight:600;text-transform:uppercase;letter-spacing:.5px;margin-bottom:6px">Điểm nóng nhất</div>
          <div style="font-size:14px;font-weight:700;color:var(--text-main)" id="hm-stat-hotname">—</div>
          <div style="font-size:11px;color:var(--text-muted);margin-top:3px" id="hm-stat-hotweight">—</div>
        </div>

        <!-- Top 5 list -->
        <div style="background:var(--card);border-radius:10px;padding:14px;border:1px solid var(--border)">
          <div style="font-size:11px;color:var(--text-muted);font-weight:600;text-transform:uppercase;letter-spacing:.5px;margin-bottom:10px">Top vùng hoạt động</div>
          <div id="hm-top-list" style="display:flex;flex-direction:column;gap:6px">
            <div style="height:12px;background:var(--border);border-radius:4px;animation:pulse 1.5s infinite"></div>
            <div style="height:12px;background:var(--border);border-radius:4px;animation:pulse 1.5s infinite;width:80%"></div>
            <div style="height:12px;background:var(--border);border-radius:4px;animation:pulse 1.5s infinite;width:60%"></div>
          </div>
        </div>

        <!-- Hourly mini chart -->
        <div style="background:var(--card);border-radius:10px;padding:14px;border:1px solid var(--border)">
          <div style="font-size:11px;color:var(--text-muted);font-weight:600;text-transform:uppercase;letter-spacing:.5px;margin-bottom:10px">Giờ hoạt động cao điểm</div>
          <canvas id="hm-hour-chart" height="90"></canvas>
        </div>

        <!-- Info -->
        <div style="background:#eff6ff;border-radius:10px;padding:12px;border:1px solid #bfdbfe">
          <div style="font-size:11px;color:#1e40af;line-height:1.6">
            <i data-lucide="info" style="width:12px;height:12px;display:inline-block;vertical-align:middle;margin-right:4px"></i>
            GPS ghi ẩn danh mỗi khi du khách di chuyển trong phố. Dữ liệu cập nhật theo thời gian thực.
          </div>
        </div>
      </div>
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
      const autoR = Math.max(15, Math.min(90, z * 3.5));
      const slider = document.getElementById('hm-radius-range');
      if (slider) { slider.value = autoR; document.getElementById('hm-radius-val').textContent = `${Math.round(autoR)}px`; }
      _heatRadius = autoR;
      if (_heatmapLayer) _heatmapLayer.set('radius', _heatRadius);
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
      const w = +(p.weight||p.Weight||1);
      if (!poiGroups[name]) poiGroups[name] = { name, weight: w };
      else poiGroups[name].weight += w;
    });
    const sorted = Object.values(poiGroups).sort((a,b) => b.weight - a.weight);
    const top = sorted[0];

    const nameEl   = document.getElementById('hm-stat-hotname');
    const weightEl = document.getElementById('hm-stat-hotweight');
    if (nameEl)   nameEl.textContent   = top.name;
    if (weightEl) weightEl.textContent = `${Math.round(top.weight * 100) / 100} điểm tổng hợp`;
  }

  // Top 5
  _renderTopList();

  // Hour chart
  _renderHourChart();

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
  if (typeof allPois === 'undefined') return `Khu vực ngoài tuyến`;
  let best = null, bestDist = Infinity;
  allPois.forEach(p => {
    const plat = parseFloat(p.latitude||p.Latitude||0);
    const plng = parseFloat(p.longitude||p.Longitude||0);
    const d = Math.hypot(lat-plat, lng-plng);
    if (d < bestDist) { bestDist = d; best = p; }
  });
  return best?.name||best?.Name||'Khu vực ngoài tuyến';
}

function _renderTopList() {
  const el = document.getElementById('hm-top-list');
  if (!el || !_heatmapData?.length) return;
  
  const poiGroups = {};
  _heatmapData.forEach(p => {
    const name = _findPoiName(p);
    const w = +(p.weight||p.Weight||1);
    if (!poiGroups[name]) {
      poiGroups[name] = { name: name, weight: w, lat: parseFloat(p.lat||p.Lat||0), lng: parseFloat(p.lng||p.Lng||0) };
    } else {
      poiGroups[name].weight += w;
    }
  });

  const sorted = Object.values(poiGroups).sort((a,b) => b.weight - a.weight).slice(0,5);
  const maxW = sorted[0].weight || 1;
  const medals = ['🥇','🥈','🥉','4.','5.'];

  el.innerHTML = sorted.map((g,i) => {
    const pct  = Math.min(100, Math.round(g.weight / maxW * 100));
    const colors = ['#ef4444','#f97316','#eab308','#22c55e','#3b82f6'];
    const wStr = Math.round(g.weight);
    return `
      <div class="hm-poi-row" onclick="_panToPoint(${g.lat},${g.lng})">
        <span style="font-size:13px;width:20px;flex-shrink:0">${medals[i]}</span>
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

// Lưu Chart instance ngoài canvas để destroy đúng cách
let _hmHourChartInstance = null;

function _renderHourChart() {
  const canvas = document.getElementById('hm-hour-chart');
  if (!canvas || typeof Chart === 'undefined') return;

  // Destroy instance cũ đúng cách (KHÔNG dùng canvas._hmChart)
  if (_hmHourChartInstance) {
    _hmHourChartInstance.destroy();
    _hmHourChartInstance = null;
  }

  // Build hourly distribution nếu API trả về field hour/Hour
  const hours = Array(24).fill(0);
  (_heatmapData || []).forEach(p => {
    const h = p.hour ?? p.Hour;
    if (h !== undefined && h >= 0 && h < 24) {
      hours[h] += +(p.weight || p.Weight || 1);
    }
  });

  // Biểu đồ thực tế phụ thuộc vào hours (nếu không có ai online, mảng rỗng)
  const chartData = hours;

  const peakHour = chartData.indexOf(Math.max(...chartData));
  const bgColors = chartData.map((_, i) =>
    i === peakHour ? '#2563eb' : 'rgba(37,99,235,0.22)'
  );

  // Dùng aspectRatio thay vì maintainAspectRatio:false để không cần wrapper div có height
  canvas.removeAttribute('height');
  canvas.removeAttribute('style');

  _hmHourChartInstance = new Chart(canvas.getContext('2d'), {
    type: 'bar',
    data: {
      labels: Array.from({length: 24}, (_, i) => i % 6 === 0 ? `${i}h` : ''),
      datasets: [{
        data: chartData,
        backgroundColor: bgColors,
        borderRadius: 3,
        borderSkipped: false
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: true,
      aspectRatio: 2.6,
      plugins: {
        legend: { display: false },
        tooltip: {
          callbacks: {
            title: items => `${items[0].dataIndex}:00 – ${items[0].dataIndex + 1}:00`,
            label: item  => ` ${Math.round(item.raw)} lượt`
          }
        }
      },
      scales: {
        y: { display: false, beginAtZero: true },
        x: {
          grid: { display: false },
          ticks: { font: { size: 10 }, color: '#94a3b8', maxRotation: 0 }
        }
      }
    }
  });
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
  document.getElementById('hm-radius-val').textContent = `${val}px`;
  if (_heatmapLayer) _heatmapLayer.set('radius', _heatRadius);
}

function onOpacityChange(val) {
  _heatOpacity = val / 100;
  document.getElementById('hm-opacity-val').textContent = `${val}%`;
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
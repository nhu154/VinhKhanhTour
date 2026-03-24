// Data Mocking
const mockData = {
    pois: [
        { id: 'P01', name: 'Quán Ốc Vũ', lat: 10.7621, lng: 106.7023, radius: 50, priority: 10, img: 'https://images.unsplash.com/photo-1555939594-58d7cb561ad1?auto=format&fit=crop&w=150&q=80', audio: 'oc_vu_vi.mp3', trans: 'ok' },
        { id: 'P02', name: 'Thế Giới Bò', lat: 10.7618, lng: 106.7033, radius: 40, priority: 8, img: 'https://images.unsplash.com/photo-1544025162-81111421550a?auto=format&fit=crop&w=150&q=80', audio: '-', trans: 'pending' },
        { id: 'P03', name: 'Chè Xô Viết', lat: 10.7631, lng: 106.7041, radius: 30, priority: 5, img: 'https://images.unsplash.com/photo-1563805042-7684c8a9e9ce?auto=format&fit=crop&w=150&q=80', audio: 'che_xv_vi.mp3', trans: 'ok' },
        { id: 'P04', name: 'Sushi Nhí', lat: 10.7601, lng: 106.7011, radius: 60, priority: 7, img: 'https://images.unsplash.com/photo-1579871494447-9811cf80d66c?auto=format&fit=crop&w=150&q=80', audio: '-', trans: 'pending' }
    ],
    tours: [
        { id: 'T01', name: 'Tour Hải Sản Vĩnh Khánh', desc: 'Khám phá thiên đường ốc và hải sản tươi rực rỡ nhất quận 4.', count: 12, img: 'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=600&q=80' },
        { id: 'T02', name: 'Tour Ăn Vặt Lề Đường', desc: 'Trải nghiệm ẩm thực hè phố đậm chất Sài Gòn độc tôn.', count: 8, img: 'https://images.unsplash.com/photo-1574484284002-952d92456975?auto=format&fit=crop&w=600&q=80' },
        { id: 'T03', name: 'Tour Nướng & Bia', desc: 'Dành cho hội bạn nhậu với các quán BBQ đỉnh cao nhất tuyến đường.', count: 5, img: 'https://images.unsplash.com/photo-1544025162-81111421550a?auto=format&fit=crop&w=600&q=80' }
    ]
};

// Routing Logic
const navItems = document.querySelectorAll('.nav-item');
const panels = document.querySelectorAll('.panel');
const pageTitle = document.getElementById('page-title');

navItems.forEach(item => {
    item.addEventListener('click', () => {
        // Update Nav
        navItems.forEach(n => n.classList.remove('active'));
        item.classList.add('active');
        
        // Update Title
        pageTitle.textContent = item.textContent.trim().replace(/^[\u2700-\u27BF]|[\uE000-\uF8FF]|\uD83C[\uDC00-\uDFFF]|\uD83D[\uDC00-\uDFFF]|[\u2011-\u26FF]|\uD83E[\uDD10-\uDDFF]/g, '').trim();
        
        // Update Panel
        const target = item.getAttribute('data-target');
        panels.forEach(p => p.classList.remove('active'));
        document.getElementById(target).classList.add('active');
    });
});

// POI Manager
const poiManager = {
    render: function() {
        const tbody = document.querySelector('#poi-table tbody');
        tbody.innerHTML = '';
        mockData.pois.forEach(p => {
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td style="font-weight:700; color:var(--primary-light)">${p.id}</td>
                <td><img src="${p.img}" class="poi-thumb"></td>
                <td style="font-weight:600; font-size:15px">${p.name}</td>
                <td><code style="background:rgba(255,255,255,0.05);padding:6px 10px;border-radius:6px;font-family:monospace;color:#90caf9">${p.lat}, ${p.lng}</code></td>
                <td>${p.radius}m</td>
                <td><span style="background:rgba(255,202,40,0.15);color:var(--accent);padding:4px 10px;border-radius:6px;font-weight:600">${p.priority}</span></td>
                <td>
                    <button class="action-btn" onclick="poiManager.showModal('${p.id}')">✏️ Cập nhật</button>
                    <button class="action-btn delete" onclick="alert('Xóa ${p.name}?')">🗑️</button>
                </td>
            `;
            tbody.appendChild(tr);
        });
    },
    showModal: function(id) {
        if(id) {
            const poi = mockData.pois.find(x => x.id === id);
            document.getElementById('poi-name').value = poi.name;
            document.getElementById('poi-coords').value = `${poi.lat}, ${poi.lng}`;
            document.getElementById('poi-priority').value = poi.priority;
            document.getElementById('poi-radius').value = poi.radius;
        } else {
            document.getElementById('poi-name').value = '';
            document.getElementById('poi-coords').value = '';
            document.getElementById('poi-priority').value = '50';
            document.getElementById('poi-radius').value = '50';
            document.getElementById('poi-desc').value = '';
        }
        document.getElementById('poi-modal').classList.add('active');
    },
    closeModal: function() {
        document.getElementById('poi-modal').classList.remove('active');
    },
    saveData: function() {
        alert('Đã đồng bộ dữ liệu tĩnh cục bộ!');
        this.closeModal();
    }
};

// Audio Manager
const audioManager = {
    render: function() {
        const tbody = document.querySelector('#audio-table tbody');
        tbody.innerHTML = '';
        mockData.pois.forEach(p => {
            const tr = document.createElement('tr');
            const hasAudio = p.audio !== '-';
            tr.innerHTML = `
                <td style="font-weight:600; font-size:15px">${p.name}</td>
                <td>${hasAudio ? `🔊 <span style="color:#64B5F6">${p.audio}</span>` : '<span style="color:var(--text-muted)">Chưa có file</span>'}</td>
                <td><span style="color:var(--text-muted);font-style:italic">${hasAudio ? 'Dữ liệu thoại tự động...' : 'Nhấn để định cấu hình...'}</span></td>
                <td>
                    ${hasAudio 
                        ? '<span class="status-badge" style="background:rgba(0,230,118,0.15);color:var(--success);">Đã đồng bộ</span>' 
                        : '<span class="status-badge" style="background:rgba(255,82,82,0.15);color:var(--danger);">Thiếu file</span>'}
                </td>
                <td>
                    <button class="action-btn">🎙️ Upload tệp</button>
                    <button class="action-btn">✍️ Ghi đè chữ</button>
                </td>
            `;
            tbody.appendChild(tr);
        });
    }
};

// Translation Manager
const transManager = {
    render: function() {
        const tbody = document.querySelector('#trans-table tbody');
        tbody.innerHTML = '';
        mockData.pois.forEach(p => {
            const tr = document.createElement('tr');
            const isOk = p.trans === 'ok';
            tr.innerHTML = `
                <td style="font-weight:600; font-size:15px">POI: ${p.name}</td>
                <td>Văn bản hiển thị / TTS</td>
                <td style="color:#B0BEC5">${isOk ? 'Bản dịch đã hoàn tất và được duyệt 100%.' : 'Đang chờ dịch thuật bổ sung nội dung...'()}</td>
                <td>
                    ${isOk 
                        ? '<span class="status-badge" style="background:rgba(0,230,118,0.15);color:var(--success);">Đã dịch (100%)</span>' 
                        : '<span class="status-badge" style="background:rgba(255,202,40,0.15);color:var(--accent);">Cần cập nhật</span>'}
                </td>
                <td><button class="action-btn">🌐 Quản lý từ vựng</button></td>
            `;
            tbody.appendChild(tr);
        });
    }
};

// Tour Manager
const tourManager = {
    render: function() {
        const grid = document.getElementById('tour-grid');
        grid.innerHTML = '';
        mockData.tours.forEach(t => {
            const div = document.createElement('div');
            div.className = 'tour-card';
            div.innerHTML = `
                <img src="${t.img}" class="tour-cover">
                <div class="tour-info">
                    <h3>${t.name}</h3>
                    <p>${t.desc}</p>
                    <div class="tour-meta">
                        <span class="poi-count">📍 Bao gồm ${t.count} mục</span>
                        <span class="edit-action">✏️ Cấu hình</span>
                    </div>
                </div>
            `;
            grid.appendChild(div);
        });
    }
};

document.querySelector('.sidebar-footer button').addEventListener('click', () => {
    alert("Khởi động ứng dụng Vĩnh Khánh Tour MAUI Mobile...");
});

// Initialize Framework
window.onload = () => {
    poiManager.render();
    audioManager.render();
    transManager.render();
    tourManager.render();
    
    // Animate Chart randomly on load for effects
    setTimeout(() => {
        document.querySelectorAll('.mock-chart .bar').forEach(bar => {
            const currentHeight = parseInt(bar.style.height);
            bar.style.height = '0%';
            setTimeout(() => { bar.style.height = currentHeight + '%'; }, 100);
        });
    }, 500);
};

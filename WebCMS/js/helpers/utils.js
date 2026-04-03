// ══ EXPORT ══
function exportDataToCSharp() {
  if (!allPois.length) { showToast('Chưa có dữ liệu POI','warning'); return; }
  const v=(p,k1,k2)=>((p[k1]||p[k2]||'').toString().replace(/"/g,'\\"').replace(/\n/g,' '));
  let code=`// ═══════════════════════════════════════\n// CODE TỰ ĐỘNG SINH TỪ VĨNH KHÁNH CMS\n// Paste vào App.xaml.cs → InitializeSampleData()\n// ═══════════════════════════════════════\n\n`;
  allPois.forEach(p=>{
    code+=`await Database.SaveRestaurantAsync(new Restaurant\n{\n`;
    code+=`    Name        = "${v(p,'name','Name')}",\n`;
    code+=`    Description = "${v(p,'description','Description')}",\n`;
    code+=`    Category    = "${v(p,'category','Category')}",\n`;
    code+=`    Latitude    = ${p.latitude||p.Latitude||0},\n`;
    code+=`    Longitude   = ${p.longitude||p.Longitude||0},\n`;
    code+=`    Address     = "${v(p,'address','Address')}",\n`;
    code+=`    ImageUrl    = "${v(p,'imageUrl','ImageUrl')}",\n`;
    code+=`    Rating      = ${p.rating||p.Rating||4.0},\n`;
    code+=`    OpenHours   = "${v(p,'openHours','OpenHours')}",\n`;
    code+=`    AudioFile   = "${v(p,'audioFile','AudioFile')}",\n`;
    code+=`    TtsScript   = "${v(p,'ttsScript','TtsScript')}",\n`;
    code+=`    TtsScriptEn = "${v(p,'ttsScriptEn','TtsScriptEn')}",\n`;
    code+=`    TtsScriptZh = "${v(p,'ttsScriptZh','TtsScriptZh')}"\n`;
    code+=`});\n\n`;
  });
  document.getElementById('export-code-area').textContent=code;
  document.getElementById('modal-export').style.display='flex';
}

function copyExportCode() { navigator.clipboard.writeText(document.getElementById('export-code-area').textContent).then(()=>showToast('📋 Đã copy code!','success')); }

// ══ HELPERS ══
function getImgUrl(p) {
  const img=p.imageUrl||p.ImageUrl;
  if(!img) return 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="260" height="130"><rect width="100%" height="100%" fill="%23f1f5f9"/><text x="50%" y="50%" fill="%2394a3b8" font-size="20" font-family="sans-serif" text-anchor="middle" dy="7">No Image</text></svg>';
  if(img.startsWith('http')||img.startsWith('data:')) return img;
  return `${BASE_URL}/${img}`;
}

function showToast(msg,type='success') {
  const container=document.getElementById('toast-container');
  const toast=document.createElement('div');
  toast.className=`toast ${type}`;
  const icon=type==='success'?'check-circle':type==='danger'?'x-circle':type==='warning'?'alert-triangle':'info';
  toast.innerHTML=`<i data-lucide="${icon}"></i><span>${msg}</span>`;
  container.appendChild(toast); lucide.createIcons();
  setTimeout(()=>{toast.style.opacity='0';toast.style.transform='translateX(100%)';toast.style.transition='0.3s';setTimeout(()=>toast.remove(),300);},3000);
}

// ══════════════════════════════════════

using System.Text;
using System.Text.Json;
using VinhKhanhTour.Models;
using VinhKhanhTour.Services;

namespace VinhKhanhTour
{
    public class MapPage : ContentPage
    {
        private readonly WebView _webView;
        private readonly Label _statusLabel;
        private readonly GeofencingService _geofencing = new();
        private Location? _userLocation;
        private Restaurant? _nearestRestaurant;
        private List<Restaurant> _restaurants = new();
        private readonly Dictionary<int, DateTime> _lastNotified = new();
        private const int COOLDOWN = 5;
        private const string KEY = "AIzaSyCqEET9xuXB2sGAByb-5zGALGamJ2bwbxc";

        private static readonly Dictionary<string, string> Imgs = new()
        {
            ["Ốc Oanh"] = "oc_oanh.jpg",
            ["Ốc Sáu Nở"] = "oc_sauno.jpg",
            ["Ốc Thảo"] = "oc_thao.jpg",
            ["Lãng Quán"] = "langquan.jpeg",
            ["Ớt Xiêm Quán"] = "otxiemquan.jpg",
            ["Bún Cá Châu Đốc"] = "buncachaudoc.jpg",
            ["Chilli Lẩu Nướng Quán"] = "chililaunuong.jpg",
            ["Thế Giới Bò"] = "thegioibo.jpg",
            ["Cơm Cháy Kho Quẹt"] = "comchaykhoquet.jpg",
            ["Bò Lá Lốt Cô Út"] = "bolalotcout.jpg",
            ["Bún Thịt Nướng Cô Nga"] = "bunthitnuongconga.jpg",
        };

        private static string GetImg(string name)
        {
            foreach (var kv in Imgs)
                if (name.Contains(kv.Key) || kv.Key.Contains(name))
                    return "file:///android_asset/" + kv.Value;
            return "";
        }

        public MapPage()
        {
            Title = "Bản đồ";
            _webView = new WebView { HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill };
            _webView.Navigated += OnNavigated;
            _webView.Navigating += OnNavigating;

            _statusLabel = new Label
            {
                Text = "Đang tải...",
                BackgroundColor = Color.FromArgb("#1a1a2e"),
                TextColor = Colors.White,
                Padding = new Thickness(16, 8),
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center
            };

            var grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };
            grid.Add(_webView, 0, 0);
            grid.Add(_statusLabel, 0, 1);
            Content = grid;
            _ = InitAsync();
        }

        private async Task InitAsync()
        {
            _restaurants = await App.Database.GetRestaurantsAsync();
            if (!_htmlLoaded)
            {
                _htmlLoaded = true;
                var data = BuildJson();
                var html = GetHtml(data);
                _webView.Source = new HtmlWebViewSource { Html = html };
            }
        }

        private void OnNavigating(object? sender, WebNavigatingEventArgs e)
        {
            if (!e.Url.StartsWith("maui://")) return;
            e.Cancel = true;
            var uri = new Uri(e.Url);
            var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var ic = System.Globalization.CultureInfo.InvariantCulture;

            switch (uri.Host.ToLower())
            {
                case "locationupdated":
                    if (double.TryParse(q["lat"], System.Globalization.NumberStyles.Float, ic, out double lat) &&
                        double.TryParse(q["lng"], System.Globalization.NumberStyles.Float, ic, out double lng))
                    {
                        _userLocation = new Location(lat, lng);
                        MainThread.BeginInvokeOnMainThread(() => _ = CheckNearbyAsync(_userLocation));
                    }
                    break;
                case "routerequested":
                    var d = Uri.UnescapeDataString(q["data"] ?? "");
                    MainThread.BeginInvokeOnMainThread(() => _ = DrawRouteAsync(d));
                    break;
                case "statusupdate":
                    var m = Uri.UnescapeDataString(q["msg"] ?? "");
                    MainThread.BeginInvokeOnMainThread(() => _statusLabel.Text = m);
                    break;
            }
        }

        private void OnNavigated(object? sender, WebNavigatedEventArgs e)
        {
            if (e.Result != WebNavigationResult.Success) return;
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(3000);
                _statusLabel.Text = "✅ Bản đồ sẵn sàng";
            });
        }

        private string BuildJson()
        {
            var ic = System.Globalization.CultureInfo.InvariantCulture;
            var sb = new StringBuilder("[");
            for (int i = 0; i < _restaurants.Count; i++)
            {
                var r = _restaurants[i];
                var n = r.Name.Replace("\"", "\\\"").Replace("'", "\\'");
                var de = r.Description.Replace("\"", "\\\"").Replace("'", "\\'");
                var a = r.Address.Replace("\"", "\\\"").Replace("'", "\\'");
                var h = r.OpenHours.Replace("\"", "\\\"").Replace("'", "\\'");
                var img = GetImg(r.Name);
                if (i > 0) sb.Append(',');
                sb.Append("{\"id\":" + r.Id);
                sb.Append(",\"lat\":" + r.Latitude.ToString(ic));
                sb.Append(",\"lng\":" + r.Longitude.ToString(ic));
                sb.Append(",\"name\":\"" + n + "\"");
                sb.Append(",\"desc\":\"" + de + "\"");
                sb.Append(",\"addr\":\"" + a + "\"");
                sb.Append(",\"rating\":" + r.Rating.ToString(ic));
                sb.Append(",\"hours\":\"" + h + "\"");
                sb.Append(",\"img\":\"" + img + "\"}");
            }
            sb.Append("]");
            return sb.ToString();
        }

        private string GetHtml(string data)
        {
            var lines = new List<string>();

            lines.Add("<!DOCTYPE html>");
            lines.Add("<html><head>");
            lines.Add("<meta charset='utf-8'>");
            lines.Add("<meta name='viewport' content='width=device-width,initial-scale=1.0'>");
            lines.Add("<style>");
            lines.Add("*{box-sizing:border-box;margin:0;padding:0}");
            lines.Add("body,html{height:100%;font-family:Roboto,sans-serif;overflow:hidden}");
            lines.Add("#map{position:fixed;top:0;left:0;right:0;bottom:0;z-index:1}");
            lines.Add("#topbar{position:fixed;top:12px;left:12px;right:12px;z-index:1000}");
            lines.Add("#sb{background:white;border-radius:24px;padding:11px 16px;display:flex;align-items:center;gap:10px;box-shadow:0 2px 12px rgba(0,0,0,.18)}");
            lines.Add("#sb span{flex:1;font-size:15px;color:#aaa}");
            lines.Add("#chips{display:flex;gap:8px;margin-top:10px;overflow-x:auto;padding-bottom:2px}");
            lines.Add("#chips::-webkit-scrollbar{display:none}");
            lines.Add(".chip{flex-shrink:0;padding:7px 14px;border-radius:20px;border:none;cursor:pointer;font-size:13px;font-weight:600;background:white;color:#555;box-shadow:0 1px 6px rgba(0,0,0,.1)}");
            lines.Add(".chip.on{background:#FF6B6B;color:white}");
            lines.Add("#btnG{position:fixed;right:14px;bottom:290px;z-index:1000;width:44px;height:44px;border-radius:50%;background:white;border:none;cursor:pointer;font-size:18px;box-shadow:0 2px 10px rgba(0,0,0,.18)}");
            lines.Add("#panel{position:fixed;bottom:0;left:0;right:0;z-index:900;background:white;border-radius:20px 20px 0 0;box-shadow:0 -3px 20px rgba(0,0,0,.1);height:50%;display:flex;flex-direction:column}");
            lines.Add(".hdl{width:36px;height:4px;background:#ddd;border-radius:4px;margin:10px auto 6px;flex-shrink:0}");
            lines.Add(".ptit{font-size:12px;font-weight:700;color:#999;padding:0 16px 8px;flex-shrink:0;text-transform:uppercase;letter-spacing:.5px}");
            lines.Add("#lst{flex:1;overflow-y:auto;padding:0 12px 12px}");
            lines.Add("#lst::-webkit-scrollbar{display:none}");
            lines.Add(".pc{display:flex;gap:12px;align-items:center;background:white;border-radius:14px;padding:10px;margin-bottom:10px;box-shadow:0 1px 8px rgba(0,0,0,.07);cursor:pointer;border:2px solid transparent}");
            lines.Add(".pc.on{border-color:#FF6B6B}");
            lines.Add(".ci{width:72px;height:72px;border-radius:12px;object-fit:cover;flex-shrink:0;background:#f0f0f0}");
            lines.Add(".cph{width:72px;height:72px;border-radius:12px;flex-shrink:0;display:flex;align-items:center;justify-content:center;font-size:32px}");
            lines.Add(".cn{flex:1;min-width:0}");
            lines.Add(".cna{font-size:15px;font-weight:700;color:#1a1a2e;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;margin-bottom:3px}");
            lines.Add(".cde{font-size:12px;color:#888;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;margin-bottom:6px}");
            lines.Add(".cm{display:flex;gap:8px;align-items:center;flex-wrap:wrap}");
            lines.Add(".cst{font-size:12px;color:#f39c12;font-weight:700}");
            lines.Add(".cdi{font-size:11px;color:#1a73e8;font-weight:700;background:#E8F0FE;padding:2px 7px;border-radius:10px}");
            lines.Add(".chr{font-size:11px;color:#aaa}");
            lines.Add(".car{font-size:18px;color:#ddd;flex-shrink:0}");
            lines.Add("#sheet{position:fixed;bottom:0;left:0;right:0;z-index:9000;background:white;border-radius:20px 20px 0 0;box-shadow:0 -4px 24px rgba(0,0,0,.18);transform:translateY(100%);transition:transform .3s cubic-bezier(.4,0,.2,1);max-height:70vh;overflow-y:auto}");
            lines.Add("#sheet.open{transform:translateY(0)}");
            lines.Add(".sh{width:36px;height:4px;background:#e0e0e0;border-radius:4px;margin:10px auto 0}");
            lines.Add(".siw{margin:12px 18px 0;height:160px;border-radius:14px;overflow:hidden}");
            lines.Add(".simg{width:100%;height:100%;object-fit:cover}");
            lines.Add(".sph{width:100%;height:100%;display:flex;align-items:center;justify-content:center;font-size:52px}");
            lines.Add(".sin{padding:12px 18px 0}");
            lines.Add(".sbg{display:inline-block;background:#fff0f0;color:#FF6B6B;font-size:11px;font-weight:700;padding:3px 10px;border-radius:20px;margin-bottom:6px}");
            lines.Add(".snm{font-size:20px;font-weight:700;color:#1a1a2e;margin-bottom:4px}");
            lines.Add(".srt{font-size:13px;color:#f39c12;font-weight:700;margin-bottom:12px}");
            lines.Add(".sdv{height:1px;background:#f0f0f0;margin:0 18px}");
            lines.Add(".sac{display:flex;gap:10px;padding:12px 18px}");
            lines.Add(".bdr{flex:1;padding:12px;background:#1a73e8;color:white;border:none;border-radius:12px;font-size:14px;font-weight:700;cursor:pointer}");
            lines.Add(".bcl{width:48px;background:#f5f5f5;color:#555;border:none;border-radius:12px;font-size:17px;cursor:pointer}");
            lines.Add(".sdt{padding:0 18px 4px}");
            lines.Add(".sro{display:flex;gap:12px;padding:9px 0;border-bottom:1px solid #f5f5f5;font-size:13px;color:#444}");
            lines.Add(".sro:last-child{border-bottom:none}");
            lines.Add(".sic{font-size:16px;width:22px;text-align:center}");
            lines.Add("#rs{position:fixed;bottom:0;left:0;right:0;z-index:9000;background:white;border-radius:20px 20px 0 0;box-shadow:0 -4px 24px rgba(0,0,0,.18);transform:translateY(100%);transition:transform .3s cubic-bezier(.4,0,.2,1);padding:16px 18px 28px}");
            lines.Add("#rs.open{transform:translateY(0)}");
            lines.Add(".rh{width:36px;height:4px;background:#e0e0e0;border-radius:4px;margin:0 auto 14px}");
            lines.Add(".rrw{display:flex;align-items:center;gap:12px;margin-bottom:14px}");
            lines.Add(".ric{width:48px;height:48px;border-radius:12px;background:#1a73e8;display:flex;align-items:center;justify-content:center;font-size:22px}");
            lines.Add(".rnm{font-size:16px;font-weight:700;color:#1a1a2e}");
            lines.Add(".rsb{font-size:12px;color:#888;margin-top:2px}");
            lines.Add(".rst{display:flex;gap:10px;margin-bottom:14px}");
            lines.Add(".rsc{flex:1;background:#f0f6ff;border-radius:12px;padding:12px;text-align:center}");
            lines.Add(".rvl{font-size:20px;font-weight:800;color:#1a73e8}");
            lines.Add(".rll{font-size:11px;color:#888;margin-top:2px}");
            lines.Add(".ben{width:100%;padding:12px;background:white;color:#d93025;border:2px solid #d93025;border-radius:12px;font-size:14px;font-weight:700;cursor:pointer}");
            lines.Add("#tst{position:fixed;top:100px;left:50%;transform:translateX(-50%);background:rgba(26,26,46,.88);color:white;padding:8px 18px;border-radius:20px;font-size:12px;display:none;z-index:9999;white-space:nowrap}");
            lines.Add("</style></head><body>");
            lines.Add("<div id='map'></div>");
            lines.Add("<div id='tst'></div>");
            lines.Add("<div id='topbar'>");
            lines.Add("  <div id='sb'><span>&#128269;</span><span>Tim quan an Vinh Khanh...</span></div>");
            lines.Add("  <div id='chips'>");
            lines.Add("    <button class='chip on' onclick='filt(\"all\",this)'>Tat ca</button>");
            lines.Add("    <button class='chip' onclick='filt(\"oc\",this)'>Oc</button>");
            lines.Add("    <button class='chip' onclick='filt(\"nuong\",this)'>Nuong</button>");
            lines.Add("    <button class='chip' onclick='filt(\"bun\",this)'>Bun</button>");
            lines.Add("    <button class='chip' onclick='filt(\"bo\",this)'>Bo</button>");
            lines.Add("    <button class='chip' onclick='filt(\"com\",this)'>Com</button>");
            lines.Add("  </div>");
            lines.Add("</div>");
            lines.Add("<button id='btnG' onclick='toU()'>&#128205;</button>");
            lines.Add("<div id='panel'>");
            lines.Add("  <div class='hdl'></div>");
            lines.Add("  <div class='ptit' id='ptit'>11 dia diem</div>");
            lines.Add("  <div id='lst'></div>");
            lines.Add("</div>");
            lines.Add("<div id='sheet'>");
            lines.Add("  <div class='sh'></div>");
            lines.Add("  <div class='siw'>");
            lines.Add("    <img id='simg' class='simg' src='' onerror=\"this.style.display='none';document.getElementById('sph').style.display='flex'\">");
            lines.Add("    <div id='sph' class='sph' style='display:none'></div>");
            lines.Add("  </div>");
            lines.Add("  <div class='sin'>");
            lines.Add("    <div class='sbg'>QUAN AN - VINH KHANH</div>");
            lines.Add("    <div class='snm' id='snm'></div>");
            lines.Add("    <div class='srt' id='srt'></div>");
            lines.Add("  </div>");
            lines.Add("  <div class='sac'>");
            lines.Add("    <button class='bdr' onclick='reqR()'>Chi duong den day</button>");
            lines.Add("    <button class='bcl' onclick='closeS()'>X</button>");
            lines.Add("  </div>");
            lines.Add("  <div class='sdv'></div>");
            lines.Add("  <div class='sdt'>");
            lines.Add("    <div class='sro'><span class='sic'>&#128172;</span><span id='sde'></span></div>");
            lines.Add("    <div class='sro'><span class='sic'>&#128205;</span><span id='sad'></span></div>");
            lines.Add("    <div class='sro'><span class='sic'>&#128336;</span><span id='shr'></span></div>");
            lines.Add("  </div>");
            lines.Add("</div>");
            lines.Add("<div id='rs'>");
            lines.Add("  <div class='rh'></div>");
            lines.Add("  <div class='rrw'>");
            lines.Add("    <div class='ric'>&#128506;</div>");
            lines.Add("    <div><div class='rnm' id='rnm'></div><div class='rsb'>Dan duong di bo</div></div>");
            lines.Add("  </div>");
            lines.Add("  <div class='rst'>");
            lines.Add("    <div class='rsc'><div class='rvl' id='rdst'></div><div class='rll'>Khoang cach</div></div>");
            lines.Add("    <div class='rsc'><div class='rvl' id='rtim'></div><div class='rll'>Thoi gian</div></div>");
            lines.Add("  </div>");
            lines.Add("  <button class='ben' onclick='endR()'>X Ket thuc dan duong</button>");
            lines.Add("</div>");

            // JavaScript
            lines.Add("<script>");
            lines.Add("var map,uMk,uCir,rLn,poi={},uP=null,cur=null;");
            lines.Add("var ALL=" + data + ";");
            lines.Add("var fil=ALL;");
            lines.Add("var CTR={lat:10.7615,lng:106.7045};");
            lines.Add("var EM={oc:'&#129450;',lau:'&#127858;',nuong:'&#128293;',bun:'&#127836;',com:'&#127834;',bo:'&#129385;'};");
            lines.Add("var BG={oc:'#FFE8E8',lau:'#FFF0E0',nuong:'#FFF0E0',bun:'#E8FFE8',com:'#FFF8E0',bo:'#FFE8F0'};");
            lines.Add("function em(n){n=n.toLowerCase();for(var k in EM)if(n.includes(k))return EM[k];return '&#127836;';}");
            lines.Add("function bg(n){n=n.toLowerCase();for(var k in BG)if(n.includes(k))return BG[k];return '#EEF0FF';}");
            lines.Add("function st(r){var s='',f=Math.round(r);for(var i=0;i<5;i++)s+=i<f?'&#9733;':'&#9734;';return s+' '+r;}");
            lines.Add("function fd(d){return d<1000?Math.round(d)+'m':(d/1000).toFixed(1)+'km';}");
            lines.Add("function ds(a,b,c,d){var R=6371000,dL=(c-a)*Math.PI/180,dG=(d-b)*Math.PI/180,x=Math.sin(dL/2)*Math.sin(dL/2)+Math.cos(a*Math.PI/180)*Math.cos(c*Math.PI/180)*Math.sin(dG/2)*Math.sin(dG/2);return R*2*Math.atan2(Math.sqrt(x),Math.sqrt(1-x));}");
            lines.Add("function toast(m){var t=document.getElementById('tst');t.textContent=m;t.style.display='block';setTimeout(function(){t.style.display='none';},2500);}");
            lines.Add("function stu(m){window.location.href='maui://statusupdate?msg='+encodeURIComponent(m);}");
            lines.Add("function toU(){if(uP)map.panTo(uP);else toast('Chua co GPS');}");
            lines.Add("function filt(k,btn){document.querySelectorAll('.chip').forEach(function(c){c.classList.remove('on');});btn.classList.add('on');fil=k==='all'?ALL:ALL.filter(function(r){return r.name.toLowerCase().replace(/[àáâãäå]/g,'a').replace(/[đ]/g,'d').replace(/[èéê]/g,'e').replace(/[ìí]/g,'i').replace(/[òóôõö]/g,'o').replace(/[ùúü]/g,'u').replace(/[ýÿ]/g,'y').includes(k);});bld();Object.keys(poi).forEach(function(id){poi[id].mk.setVisible(!!fil.find(function(r){return r.id==id;}));});document.getElementById('ptit').textContent=fil.length+' dia diem';}");
            lines.Add("function bld(){var el=document.getElementById('lst');el.innerHTML='';fil.forEach(function(r){var d=uP?ds(uP.lat,uP.lng,r.lat,r.lng):null;var dh=d?'<span class=\"cdi\">'+fd(d)+'</span>':'';var ih=r.img?'<img class=\"ci\" src=\"'+r.img+'\" onerror=\"this.style.display=\\'none\\'\">':'<div class=\"cph\" style=\"background:'+bg(r.name)+'\">'+em(r.name)+'</div>';var div=document.createElement('div');div.className='pc';div.id='c'+r.id;div.innerHTML=ih+'<div class=\"cn\"><div class=\"cna\">'+r.name+'</div><div class=\"cde\">'+r.desc+'</div><div class=\"cm\"><span class=\"cst\">'+st(r.rating)+'</span>'+dh+'<span class=\"chr\">'+r.hours+'</span></div></div><span class=\"car\">&#8250;</span>';div.onclick=function(){pick(r);};el.appendChild(div);});}");
            lines.Add("function pick(r){cur=r;Object.values(poi).forEach(function(e){e.mk.setIcon(mkI(false));});document.querySelectorAll('.pc').forEach(function(c){c.classList.remove('on');});if(poi[r.id])poi[r.id].mk.setIcon(mkI(true));var c=document.getElementById('c'+r.id);if(c)c.classList.add('on');map.panTo({lat:r.lat,lng:r.lng});opS(r);}");
            lines.Add("function opS(r){var si=document.getElementById('simg'),sp=document.getElementById('sph');if(r.img){si.src=r.img;si.style.display='block';sp.style.display='none';}else{si.style.display='none';sp.innerHTML=em(r.name);sp.style.background=bg(r.name);sp.style.display='flex';}document.getElementById('snm').textContent=r.name;document.getElementById('srt').innerHTML=st(r.rating);document.getElementById('sde').textContent=r.desc;document.getElementById('sad').textContent=r.addr;document.getElementById('shr').textContent=r.hours;document.getElementById('sheet').classList.add('open');document.getElementById('panel').style.display='none';}");
            lines.Add("function closeS(){document.getElementById('sheet').classList.remove('open');document.getElementById('panel').style.display='flex';cur=null;Object.values(poi).forEach(function(e){e.mk.setIcon(mkI(false));});document.querySelectorAll('.pc').forEach(function(c){c.classList.remove('on');});}");
            lines.Add("function reqR(){if(!cur||!uP){toast('Chua co GPS');return;}toast('Dang tai lo trinh...');window.location.href='maui://routerequested?data='+encodeURIComponent(JSON.stringify({lat:cur.lat,lng:cur.lng,name:cur.name}));}");
            lines.Add("function shwR(n,d,t){document.getElementById('rnm').textContent=n;document.getElementById('rdst').textContent=d;document.getElementById('rtim').textContent=t;document.getElementById('rs').classList.add('open');document.getElementById('sheet').classList.remove('open');document.getElementById('panel').style.display='none';}");
            lines.Add("function endR(){if(rLn){rLn.setMap(null);rLn=null;}document.getElementById('rs').classList.remove('open');document.getElementById('panel').style.display='flex';stu('Ban do san sang');}");
            lines.Add("function dec(enc){var pts=[],i=0,len=enc.length,lat=0,lng=0;while(i<len){var b,s=0,r=0;do{b=enc.charCodeAt(i++)-63;r|=(b&0x1f)<<s;s+=5;}while(b>=0x20);lat+=((r&1)?~(r>>1):(r>>1));s=0;r=0;do{b=enc.charCodeAt(i++)-63;r|=(b&0x1f)<<s;s+=5;}while(b>=0x20);lng+=((r&1)?~(r>>1):(r>>1));pts.push({lat:lat/1e5,lng:lng/1e5});}return pts;}");
            lines.Add("function drR(enc,n,d,t){if(rLn){rLn.setMap(null);rLn=null;}var path=dec(enc);rLn=new google.maps.Polyline({path:path,geodesic:true,strokeColor:'#1a73e8',strokeWeight:6,strokeOpacity:0.9});rLn.setMap(map);var b=new google.maps.LatLngBounds();path.forEach(function(p){b.extend(p);});map.fitBounds(b,{padding:100});shwR(n,d,t);stu(n+' '+d);}");
            lines.Add("function mkI(a){var c=a?'#e53935':'#FF6B6B',s=a?40:34;var svg='<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"'+s+'\" height=\"'+(s*1.3)+'\"><path d=\"M'+(s/2)+' 0C'+(s*.22)+' 0 0 '+(s*.22)+' 0 '+(s/2)+'c0 '+(s*.4)+' '+(s/2)+' '+(s*.7)+' '+(s/2)+' '+(s*.7)+'S'+s+' '+(s*.9)+' '+s+' '+(s/2)+'C'+s+' '+(s*.22)+' '+(s*.78)+' 0 '+(s/2)+' 0z\" fill=\"'+c+'\" stroke=\"white\" stroke-width=\"2\"/><text x=\"'+(s/2)+'\" y=\"'+(s*.62)+'\" text-anchor=\"middle\" font-size=\"'+(s*.38)+'\" fill=\"white\">&#127836;</text></svg>';return {url:'data:image/svg+xml;charset=UTF-8,'+encodeURIComponent(svg),scaledSize:new google.maps.Size(s,s*1.3),anchor:new google.maps.Point(s/2,s*1.3)};}");
            lines.Add("function sPos(lat,lng){uP={lat:lat,lng:lng};if(uMk){uMk.setPosition(uP);uCir.setCenter(uP);}else{uMk=new google.maps.Marker({position:uP,map:map,zIndex:1000,icon:{path:google.maps.SymbolPath.CIRCLE,scale:11,fillColor:'#4285F4',fillOpacity:1,strokeColor:'white',strokeWeight:3}});uCir=new google.maps.Circle({center:uP,radius:25,map:map,fillColor:'#4285F4',fillOpacity:0.15,strokeColor:'#4285F4',strokeOpacity:0.4,strokeWeight:1.5});map.panTo(uP);}bld();window.location.href='maui://locationupdated?lat='+lat+'&lng='+lng;}");
            lines.Add("function updateUserLocation(lat,lng){sPos(lat,lng);return 'ok';}");
            lines.Add("function highlightPOI(id){var e=poi[id];if(e)pick(e.data);}");
            lines.Add("function showDistance(n,d){toast(n+' '+d+'m');}");
            lines.Add("function gps(){if(!navigator.geolocation)return;navigator.geolocation.watchPosition(function(p){sPos(p.coords.latitude,p.coords.longitude);},function(){},{enableHighAccuracy:true,timeout:10000,maximumAge:3000});}");
            lines.Add("function initMap(){map=new google.maps.Map(document.getElementById('map'),{center:CTR,zoom:17,mapTypeControl:false,streetViewControl:false,fullscreenControl:false,zoomControlOptions:{position:google.maps.ControlPosition.RIGHT_CENTER},styles:[{featureType:'poi',elementType:'labels',stylers:[{visibility:'off'}]}]});map.addListener('click',function(){closeS();});ALL.forEach(function(r){var mk=new google.maps.Marker({position:{lat:r.lat,lng:r.lng},map:map,title:r.name,icon:mkI(false)});mk.addListener('click',function(){pick(r);});poi[r.id]={mk:mk,data:r};});bld();setTimeout(gps,1000);}");
            lines.Add("</script>");
            lines.Add("<script src='https://maps.googleapis.com/maps/api/js?key=" + KEY + "&callback=initMap' async defer></script>");
            lines.Add("</body></html>");

            return string.Join("\n", lines);
        }

        private async Task CheckNearbyAsync(Location location)
        {
            if (_restaurants.Count == 0) return;
            Restaurant? nearest = null;
            double minDist = double.MaxValue;
            foreach (var r in _restaurants)
            {
                double d = _geofencing.CalculateDistance(location.Latitude, location.Longitude, r.Latitude, r.Longitude);
                if (d < minDist) { minDist = d; nearest = r; }
            }
            if (nearest == null) return;
            _nearestRestaurant = nearest;
            MainThread.BeginInvokeOnMainThread(() => _statusLabel.Text = $"Gan nhat: {nearest.Name} ({minDist:F0}m)");
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await _webView.EvaluateJavaScriptAsync($"showDistance('{nearest.Name.Replace("'", "\\'")}','{minDist:F0}');"));
            if (minDist > 50) return;
            if (_lastNotified.TryGetValue(nearest.Id, out DateTime last) &&
                (DateTime.Now - last).TotalMinutes < COOLDOWN) return;
            _lastNotified[nearest.Id] = DateTime.Now;
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await _webView.EvaluateJavaScriptAsync($"highlightPOI({nearest.Id});"));
            await SpeakAsync(nearest);
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayAlert("Da den gan!", $"{nearest.Name}\n{nearest.Description}", "OK"));
            await App.Database.SaveVisitAsync(new VisitHistory { RestaurantId = nearest.Id, VisitedAt = DateTime.Now });
        }

        private static async Task SpeakAsync(Restaurant r)
        {
            try
            {
                await TextToSpeech.SpeakAsync(
                    $"Ban dang den gan {r.Name}. {r.Description}. Danh gia {r.Rating} sao.",
                    new SpeechOptions { Volume = 1.0f, Pitch = 1.0f });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"TTS: {ex.Message}"); }
        }

        private async Task DrawRouteAsync(string json)
        {
            if (_userLocation == null) return;
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var info = JsonSerializer.Deserialize<RouteRequest>(json, opts);
                if (info == null) return;
                var ic = System.Globalization.CultureInfo.InvariantCulture;
                var origin = $"{_userLocation.Latitude.ToString(ic)},{_userLocation.Longitude.ToString(ic)}";
                var dest = $"{info.Lat.ToString(ic)},{info.Lng.ToString(ic)}";
                var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={origin}&destination={dest}&mode=walking&language=vi&key={KEY}";
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var resp = await http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(resp);
                var root = doc.RootElement;
                var st = root.GetProperty("status").GetString();
                if (st != "OK")
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                        await DisplayAlert("Loi", $"Directions: {st}", "OK"));
                    return;
                }
                var route = root.GetProperty("routes")[0];
                var leg = route.GetProperty("legs")[0];
                var poly = route.GetProperty("overview_polyline").GetProperty("points").GetString() ?? "";
                var dTxt = leg.GetProperty("distance").GetProperty("text").GetString() ?? "";
                var tTxt = leg.GetProperty("duration").GetProperty("text").GetString() ?? "";
                var esc = poly.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "");
                var name = info.Name.Replace("'", "\\'");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await _webView.EvaluateJavaScriptAsync($"drR('{esc}','{name}','{dTxt}','{tTxt}');"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Route: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await DisplayAlert("Loi", "Khong the tai lo trinh.", "OK"));
            }
        }
    }
}
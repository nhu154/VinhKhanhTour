// ══ AUDIO MANAGEMENT PAGE ══

// Script ~30 giây (Google TTS Wavenet ~130-150 tiếng mỗi phút ở 0.95x speed)
const RICH_SCRIPTS = {
  "Ốc Oanh": {
    vi: "Chào mừng bạn đến Ốc Oanh — linh hồn phố ẩm thực Vĩnh Khánh hơn mười năm qua! Quán nổi tiếng với ốc hương nướng tiêu xanh, ốc mỡ hấp sả gừng và ốc len xào dừa béo ngậy. Mỗi con ốc tươi chọn lọc từ miền Tây mỗi sáng, đảm bảo chất lượng tuyệt đối. Thực đơn hơn hai mươi món, nước chấm bí truyền độc quyền. Mở cửa từ bốn giờ chiều đến mười một giờ đêm. Đặt chỗ sớm kẻo hết!",
    en: "Welcome to Oc Oanh — the soul of Vinh Khanh food street for over a decade! Famous for pepper-grilled rock snails, lemongrass-steamed butter snails, and rich coconut-sauteed mud creepers, all hand-picked fresh every morning from the Mekong Delta. Our menu features twenty shellfish preparations with a secret house dipping sauce. Open four in the afternoon until eleven at night. Book early — always packed!",
    zh: "欢迎来到'欧莺'——永庆街美食灵魂超过十年！以胡椒烤香螺、香茅蒸蜗牛和椰汁炒泥螺著称，每日清晨从湄公河精选食材。菜单超过二十种贝类料理，搭配秘制蘸酱。下午四点至晚上十一点营业，请提前预订！"
  },
  "Ốc Sáu Nở": {
    vi: "Bạn đang đến Ốc Sáu Nở — biểu tượng hải sản Sài Gòn nhiều thập kỷ! Thử ngay ốc bươu nhồi thịt hấp gừng, ốc nhảy xào me chua ngọt và mâm ốc tổng hợp năm loại đặc biệt. Nước chấm bí truyền gia truyền là linh hồn của quán. Ngồi vỉa hè Vĩnh Khánh, nhâm nhi ly mát cùng mâm ốc bốc khói — trải nghiệm Sài Gòn đích thực. Mở từ ba giờ chiều đến mười một giờ đêm!",
    en: "Approaching Oc Sau No — a Saigon seafood icon for decades! Must-try: ginger-steamed stuffed snails, tamarind-glazed jumping snails, and the five-variety shellfish platter. Their secret dipping sauce keeps regulars coming back. Sitting on the lively Vinh Khanh sidewalk with an ice-cold drink alongside steaming shellfish — this is authentic Saigon street food. Open three in the afternoon until eleven at night!",
    zh: "欢迎来到'六绽海鲜'——西贡数十年的海鲜传奇！必点：姜汁蒸肉馅田螺、酸甜罗望子炒蜗牛和五种贝类拼盘。秘制蘸酱代代相传，令老顾客念念不忘。坐在永庆街边，冷饮配热螺，尽享西贡街头真味。每天下午三点至晚上十一点营业！"
  },
  "Ốc Thảo": {
    vi: "Phía trước là Ốc Thảo — một trong những quán ốc lâu đời nhất Vĩnh Khánh! Điểm đặc biệt là gia vị độc đáo kết hợp truyền thống Nam Bộ và sáng tạo hiện đại. Thử ốc nướng muối ớt giòn tan, ốc sò hấp bia thơm nồng và ốc de nấu tiêu đen đậm đà. Chị Thảo luôn đích thân đứng bếp đảm bảo hương vị chuẩn nhất. Không khí thân thiện, giá hợp lý. Mở từ bốn giờ chiều đến mười giờ rưỡi tối!",
    en: "Just ahead is Oc Thao — one of Vinh Khanh's oldest and most beloved shellfish spots! Known for blending traditional Southern spices with modern creativity. Try salt-chili grilled snails, beer-steamed clams, and savory black pepper mud snails. Ms. Thao personally oversees every dish from the kitchen. Friendly atmosphere, affordable prices. Open four in the afternoon until half past ten at night!",
    zh: "前方是'草海鲜'——永庆街历史最悠久的贝壳餐厅之一！以传统南越香料与现代创意的独特融合闻名。必尝盐辣烤螺、啤酒蒸蛤蜊和黑椒焖泥螺。草姐每天亲自掌厨，品质始终如一。氛围友好，价格亲民。下午四点至晚上十点半营业！"
  },
  "Lãng Quán": {
    vi: "Chào bạn đến Lãng Quán — không gian ẩm thực trẻ trung sáng tạo giữa phố Vĩnh Khánh! Không chỉ là quán ăn — đây là nơi bạn bè hội tụ và tạo kỷ niệm. Thực đơn đa dạng từ ăn vặt Sài Gòn đến lẩu riêu cua, bò lúc lắc tiêu xanh, gà nướng mật ong. Cuối tuần có âm nhạc live tạo không gian lãng mạn. Phục vụ từ năm giờ chiều đến mười một giờ đêm. Hẹn gặp bạn tại Lãng Quán!",
    en: "Welcome to Lang Quan — a vibrant creative dining space in the heart of Vinh Khanh! More than a restaurant — a gathering place for friends and memories. The menu spans Saigon street snacks to crab roe hotpot, green pepper beef, and honey-glazed grilled chicken. Weekend evenings feature live acoustic music for a romantic atmosphere. Open five in the afternoon until eleven at night. See you at Lang Quan!",
    zh: "欢迎来到'浪漫小馆'——永庆街充满青春创意的餐饮空间！不只是餐厅，更是朋友聚会创造回忆之地。菜单从西贡小吃到蟹黄火锅、青椒牛肉、蜂蜜烤鸡应有尽有。周末有现场原声音乐，浪漫氛围满满。下午五点至晚上十一点营业，期待与您相聚！"
  },
  "Ớt Xiêm Quán": {
    vi: "Ớt Xiêm Quán đang hiện ra trước mắt bạn — thiên đường của những tín đồ vị cay kiểu Khmer Nam Bộ! Ớt xiêm xanh nhỏ nhưng cực cay là linh hồn mọi món. Đừng bỏ qua gà nướng mật ong giòn vàng, tôm rang muối ớt giòn rụm và lẩu thái chua cay đậm vị. Nhân viên sẵn sàng tư vấn mức cay phù hợp. Chuẩn bị khăn giấy và chinh phục ẩm thực cay nhất Vĩnh Khánh! Mở từ mười một giờ sáng đến chín giờ tối.",
    en: "Ot Xiem Quan is right before you — paradise for lovers of bold Khmer-style fiery flavors! The small but ferocious Siam chili pepper defines every dish. Don't miss honey-glazed chili grilled chicken, crunchy salt-chili shrimp, and the sour-spicy Thai-style hotpot. Staff always help you find your ideal heat level. Grab your tissues and conquer the spiciest food on Vinh Khanh street! Open eleven in the morning until nine at night.",
    zh: "朝天椒餐厅就在眼前——辣味爱好者的天堂，以越南南部高棉风味为灵感！个头小却无比辣的暹罗朝天椒贯穿所有菜肴。必尝蜂蜜烤鸡、椒盐炸虾和酸辣泰式火锅。员工随时协助您选择合适辣度。备好纸巾，挑战永庆街最辣美食！上午十一点至晚上九点营业。"
  },
  "Bún Cá Châu Đốc - Dì Tư": {
    vi: "Mùi mắm cá linh và ruốc Châu Đốc chính gốc đang lan tỏa từ bếp Dì Tư! Đây là địa chỉ hiếm tại Sài Gòn giữ nguyên hương vị bún cá An Giang. Nước lèo hầm cá lóc tươi cùng sả gừng tạo mùi thơm đặc trưng. Chan vào tô bún, thêm rau đắng, bắp chuối và cá chiên vàng — bữa sáng hoàn hảo! Mở từ sáu giờ sáng đến hai giờ chiều. Đến sớm kẻo hết!",
    en: "The sweet aroma of linh fish paste and authentic Chau Doc shrimp paste drifts from Auntie Tu's kitchen! One of Saigon's rare spots preserving the original An Giang fish noodle flavors. The broth is slow-simmered with fresh snakehead fish, lemongrass and ginger. Ladled over soft rice noodles with bitter greens, banana blossom and crispy fish — the perfect breakfast! Open six in the morning until two in the afternoon. Come early before it sells out!",
    zh: "四姨厨房飘来正宗朱笃虾酱的甘美香气！西贡难得保留安江原味鱼粉的珍贵地址。汤底以新鲜乌鱼、香茅姜慢火熬制，香气独特无误。浇在米粉上，配苦菜、芭蕉花和炸鱼片——完美早餐！早上六点至下午两点营业，早来早得，卖完即止！"
  },
  "Chilli Lẩu Nướng Quán": {
    vi: "Chào mừng đến Chilli Lẩu Nướng — buffet lẩu nướng tự chọn hấp dẫn giữa phố Vĩnh Khánh! Tự do chọn bò Mỹ thái mỏng, hải sản tươi và rau củ đa dạng để nướng và nhúng lẩu. Nồi lẩu thái chua cay hầm xương cả ngày là điểm nhấn đặc biệt. Giá hợp lý, không gian rộng rãi, thân thiện với sinh viên. Mở từ mười giờ sáng đến mười một giờ đêm mỗi ngày. Rủ bạn bè đến cùng!",
    en: "Welcome to Chilli Hotpot and BBQ — an all-you-can-eat buffet in the heart of Vinh Khanh! Choose from premium US beef, fresh shrimp, crab, squid, and seasonal vegetables to grill and dip in broth. The signature sour-spicy Thai hotpot, simmered all day, is the star of every visit. Student-friendly prices, spacious seating. Open ten in the morning until eleven at night, seven days a week. Bring your crew!",
    zh: "欢迎来到辣椒火锅烤肉——永庆街物超所值的自助烤涮体验！自由挑选优质美国牛肉、新鲜海鲜和各色蔬菜，自助烤涮随心所欲。全天慢火熬制的酸辣泰式火锅是每次必点亮点。价格亲民，空间宽敞，深受学生喜爱。每天上午十点至晚上十一点，欢迎携友同来！"
  },
  "Thế Giới Bò": {
    vi: "Bạn đã đến Thế Giới Bò — vương quốc của tín đồ thịt bò tại Vĩnh Khánh! Gần ba mươi món bò từ truyền thống Việt đến sáng tạo Tây phương. Nổi bật là bò tái chanh lá quế, bò nhúng giấm sôi sùng sục và bò lúc lắc tiêu đen với khoai tây chiên. Nguyên liệu nhập nguồn uy tín, đảm bảo sạch tươi. Không gian industrial hiện đại, ánh đèn warm tone. Mở từ mười giờ sáng đến mười giờ tối!",
    en: "You've arrived at The World of Beef — the ultimate beef kingdom on Vinh Khanh street! Nearly thirty preparations spanning Vietnamese tradition to Western fusion. Standouts include rare beef with lime and basil, bubbling vinegar fondue, and irresistible black pepper stir-fry with crispy fries. All beef sourced from trusted suppliers for guaranteed freshness. Modern industrial decor, warm inviting lighting. Open ten in the morning until ten at night!",
    zh: "您已踏入'牛肉世界'——永庆街牛肉爱好者的终极王国！近三十种料理从越式传统到西式创新应有尽有。亮点包括柠檬香草腌牛肉、醋涮牛肉火锅和黑椒炒牛肉配薯条。食材来自信誉供应商，品质有保障。工业风装潢，灯光温馨。每天上午十点至晚上十点营业！"
  },
  "Cơm Cháy Kho Quet": {
    vi: "Tiếng giòn rụm và mùi khói than — dấu hiệu bạn đang đến Cơm Cháy Kho Quẹt! Cơm cháy giòn tan từ đáy nồi đất nung, ăn kèm kho quẹt thịt ba chỉ và tép bạc đậm đà. Sự kết hợp cơm cháy giòn và kho quẹt ngọt mặn tạo hương vị mộc mạc khó quên. Còn có trứng ốp la, tôm kho tàu và canh chua cá kèo Nam Bộ. Mở từ mười giờ sáng đến chín giờ tối. Ghé ngay kẻo hết!",
    en: "That crackling sound and charcoal aroma mean you're near Com Chay Kho Quet! Crispy rice scraped from clay pot bottoms, served with thick pork belly and shrimp braising sauce. The contrast of crunchy rice with savory-sweet sauce creates a humble yet unforgettable flavor. Also serving fried eggs, caramelized braised shrimp, and Southern Vietnamese sour catfish soup. Open ten in the morning until nine at night. Come before the crispy rice runs out!",
    zh: "酥脆声和炭火烟香——您正在靠近'锅巴蘸酱'！从陶土锅底精心刮下的酥脆锅巴，搭配五花肉银虾浓稠蘸酱。酥脆锅巴与咸甜蘸酱的完美对比，朴实而令人难忘。另有荷包蛋、焦糖焖虾和南越酸鱼汤。上午十点至晚上九点营业，锅巴卖完即止！"
  },
  "Bò Lá Lốt Cô Út": {
    vi: "Hương thơm nồng nàn trong không khí — đó là mùi bò lá lốt nướng than của Cô Út! Hơn mười lăm năm, Cô Út nhóm bếp than mỗi ngày tạo ra những cuộn bò lá lốt vàng ươm thơm phức. Thịt bò xay pha sả tiêu tỏi và mắm ruốc, cuộn lá lốt tươi nướng đỏ than. Chấm nước mắm chua ngọt gừng hoặc muối tiêu chanh — mỗi miếng đều tuyệt vời. Còn có nem nướng sả và bún thịt nướng. Mở từ ba giờ chiều đến mười một giờ đêm!",
    en: "That seductive aroma in the evening air is Auntie Ut's famous lolot leaf beef rolls! For over fifteen years she lights the charcoal grill daily, crafting golden fragrant beef rolls nobody stops at just one. Ground beef seasoned with lemongrass, pepper, garlic and shrimp paste, wrapped in fresh lolot leaves, grilled over glowing charcoal. Dip in ginger fish sauce or salt-pepper-lime — extraordinary every bite. Also serving lemongrass sausage and grilled pork noodles. Open three until eleven at night!",
    zh: "空气中弥漫着迷人香气——那是幺姑著名蒌叶烤牛肉卷的气息！十五年来每天点燃炭炉，精心制作金黄牛肉卷，令人无法只吃一串。牛肉末调入香茅、胡椒、大蒜和虾酱，紧裹新鲜蒌叶，炭火烤制。蘸姜汁鱼露或椒盐柠檬汁，每口都是享受。另有香茅烤肠和烤肉米粉。下午三点至晚上十一点营业！"
  },
  "Bún Thịt Nướng Cô Nga": {
    vi: "Bạn đang đứng trước Bún Thịt Nướng Cô Nga — tiệm bún yêu thích Vĩnh Khánh hơn hai mươi năm! Cô Nga ướp thịt bằng sả mật ong ngũ vị hương và nước cốt dừa, nướng than đến vàng thơm. Tô bún hoàn hảo: bún trắng mềm, thịt nướng thái lát, chả giò giòn và nước mắm chua ngọt. Giá bình dân, phục vụ nhanh và luôn tươi cười. Mở từ sáu giờ sáng đến tám giờ tối. Đến sớm thưởng thức bữa sáng hoàn hảo!",
    en: "You're at Ms. Nga's Grilled Pork Noodles — Vinh Khanh's beloved bun thit nuong stall for over twenty years! Ms. Nga marinates pork daily with lemongrass, honey, five-spice and coconut milk, grilled over charcoal until golden. A perfect bowl: soft rice noodles, sliced grilled pork, crispy spring rolls, fresh herbs, and sweet-sour fish sauce. Affordable, fast service, warm smiles. Open six in the morning until eight at night. Come early for the best breakfast on Vinh Khanh!",
    zh: "您正站在阿娥烤肉米粉前——永庆街深受喜爱超过二十年的米粉摊！阿娥每天用香茅、蜂蜜、五香粉和椰奶腌制猪肉，炭火烤至金黄焦香。完美一碗：柔软米粉、薄切烤肉、酥脆炸春卷和酸甜鱼露。价格亲民，服务快捷，笑容温暖。早上六点至晚上八点营业，早来享用永庆最美早餐！"
  }
};

// ══ STATE & RENDER ════════════════════════════════════════════════════════

let _audioLangFilter = 'all';

function renderAudioPage() {
  renderAudioStats();
  renderAudioLangTabs();
  renderAudioTable();
}

function getPoiTts(p, langCode) {
  if (langCode === 'vi') return p.ttsScript || p.TtsScript || '';
  if (langCode === 'en') return p.ttsScriptEn || p.TtsScriptEn || '';
  if (langCode === 'zh') return p.ttsScriptZh || p.TtsScriptZh || '';
  try { const t = JSON.parse(p.translations || p.Translations || '{}'); return t[langCode]?.tts || ''; }
  catch { return ''; }
}

const LANG_PALETTE = [
  { color: '#16a34a', bg: '#f0fdf4' }, { color: '#d97706', bg: '#fffbeb' },
  { color: '#dc2626', bg: '#fef2f2' }, { color: '#7c3aed', bg: '#f5f3ff' },
  { color: '#0891b2', bg: '#ecfeff' }, { color: '#db2777', bg: '#fdf2f8' },
];

function renderAudioStats() {
  const bar = document.getElementById('audio-stats-bar');
  if (!bar || !allPois.length) return;
  const total = allPois.length;
  const langs = getLangs();
  const pct = n => Math.round(n / total * 100);
  const card = (icon, label, count, color, bg) => `
    <div style="background:var(--card,#fff);border:1px solid var(--border,#e2e8f0);border-radius:14px;padding:16px;display:flex;flex-direction:column;gap:8px">
      <div style="display:flex;align-items:center;gap:8px;font-size:12px;font-weight:600;color:${color}">
        <div style="width:32px;height:32px;background:${bg};border-radius:8px;display:flex;align-items:center;justify-content:center;font-size:16px">${icon}</div>
        ${label}
      </div>
      <div style="font-size:26px;font-weight:800;color:var(--text-main,#0f172a)">${count}<span style="font-size:13px;font-weight:500;color:var(--text-muted,#94a3b8)">/${total}</span></div>
      <div style="background:var(--bg-secondary,#f1f5f9);border-radius:99px;height:6px;overflow:hidden">
        <div style="height:100%;background:${color};border-radius:99px;width:${pct(count)}%;transition:.5s"></div>
      </div>
      <div style="font-size:11px;color:var(--text-muted,#94a3b8)">${pct(count)}% hoàn thành</div>
    </div>`;
  const audioFileCount = allPois.filter(p => !!(p.audioUrl || p.AudioUrl)).length;
  bar.style.gridTemplateColumns = `repeat(${2 + langs.length}, 1fr)`;
  bar.innerHTML =
    card('📍', 'Tổng địa điểm', total, '#2563eb', '#eff6ff') +
    card('🎵', 'File Audio', audioFileCount, '#7c3aed', '#f5f3ff') +
    langs.map((l, i) => {
      const { color, bg } = LANG_PALETTE[i % LANG_PALETTE.length];
      const count = allPois.filter(p => !!getPoiTts(p, l.code)).length;
      return card(l.flag, l.name, count, color, bg);
    }).join('');
}

function renderAudioLangTabs() {
  const el = document.getElementById('audio-lang-tabs');
  if (!el) return;
  const langs = getLangs();
  const tabs = [
    { code: 'all',  label: 'Tất cả', icon: 'layers' },
    { code: 'file', label: '🎵 File', icon: null },
    ...langs.map(l => ({ code: l.code, label: `${l.flag} ${l.code.toUpperCase()}`, icon: null }))
  ];
  el.innerHTML = tabs.map(t => `
    <button class="filter-chip ${_audioLangFilter === t.code ? 'active' : ''}"
      onclick="_audioLangFilter='${t.code}';renderAudioLangTabs();renderAudioTable()">
      ${t.icon ? `<i data-lucide="${t.icon}" style="width:13px;height:13px"></i>` : ''} ${t.label}
    </button>`).join('');
  lucide.createIcons();
}

function renderAudioTable() {
  const tbody = document.getElementById('audio-tbody');
  const thead = document.getElementById('audio-thead');
  if (!tbody) return;
  const langs = getLangs();
  const q = (document.getElementById('audio-search')?.value || '').toLowerCase();
  const list = allPois.filter(p => textMatch(p.name || p.Name, q));

  // Tab ngôn ngữ cụ thể → chỉ hiện cột đó; tab all/file → hiện tất cả
  const isLangTab    = _audioLangFilter !== 'all' && _audioLangFilter !== 'file';
  const showFile     = !isLangTab;
  const visibleLangs = isLangTab ? langs.filter(l => l.code === _audioLangFilter) : langs;

  if (thead) {
    thead.innerHTML = `<tr>
      <th style="width:52px"></th>
      <th>Địa điểm</th>
      ${showFile ? `<th style="text-align:center;width:155px">🎵 File Audio</th>` : ''}
      ${visibleLangs.map(l => `<th style="text-align:center">${l.flag} ${l.name}</th>`).join('')}
      <th style="width:56px;text-align:right">Sửa</th>
    </tr>`;
  }

  if (!list.length) {
    tbody.innerHTML = `<tr><td colspan="${3 + visibleLangs.length + (showFile ? 1 : 0)}"
      style="text-align:center;padding:40px;color:var(--text-muted)">Không tìm thấy địa điểm</td></tr>`;
    return;
  }

  const ttsCell = (text, langCode) => {
    if (!text) return `<td style="text-align:center">
      <span style="font-size:12px;color:#f87171;background:#fef2f2;padding:4px 10px;border-radius:20px;border:1px solid #fecaca">✗ Thiếu</span>
    </td>`;
    const safe = escapeSq(text);
    return `<td style="text-align:center">
      <div style="display:inline-flex;gap:4px;align-items:center">
        <button class="tts-play-btn"
          style="background:#f0fdf4;border:1px solid #bbf7d0;border-radius:20px;padding:4px 11px;font-size:12px;color:#16a34a;gap:5px;display:inline-flex;align-items:center;min-width:74px;justify-content:center;cursor:pointer"
          onclick="toggleAudioCell('${safe}','${langCode}',this)">
          <i data-lucide="play" style="width:13px;height:13px"></i> Nghe
        </button>
        <button class="tts-stop-btn"
          style="background:#fef2f2;border:1px solid #fecaca;border-radius:20px;padding:4px 7px;font-size:12px;color:#dc2626;display:inline-flex;align-items:center;cursor:pointer;opacity:0;pointer-events:none;transition:opacity .15s"
          title="Dừng & về đầu" onclick="stopAndResetCell(this)">
          <i data-lucide="square" style="width:12px;height:12px"></i>
        </button>
      </div>
    </td>`;
  };

  tbody.innerHTML = list.map(p => {
    const id       = p.id   || p.Id;
    const name     = p.name || p.Name    || '—';
    const img      = p.imageUrl || p.ImageUrl || '';
    const audioUrl = p.audioUrl || p.AudioUrl || '';
    const imgSrc   = img
      ? (img.startsWith('http') || img.startsWith('data:') ? img : `${BASE_URL}/${img}`)
      : 'https://via.placeholder.com/44?text=?';
    const langCells = visibleLangs.map(l => ttsCell(getPoiTts(p, l.code), l.code)).join('');
    return `<tr>
      <td><img src="${imgSrc}" onerror="this.src='https://via.placeholder.com/44?text=?'"
        style="width:44px;height:44px;border-radius:8px;object-fit:cover;border:1px solid var(--border,#e2e8f0)"></td>
      <td>
        <div style="font-weight:600;font-size:13px">${name}</div>
        <div style="font-size:11px;color:var(--text-muted,#94a3b8)">⭐ ${(p.rating || p.Rating || 0).toFixed(1)} · ${p.openHours || p.OpenHours || '—'}</div>
      </td>
      ${showFile ? `<td style="text-align:center">${audioFileCell(id, name, audioUrl)}</td>` : ''}
      ${langCells}
      <td style="text-align:right">
        <button class="btn btn-ghost btn-sm" onclick='openEditPoiForm(allPois.find(x=>(x.id||x.Id)==${id}))'>
          <i data-lucide="edit-3"></i>
        </button>
      </td>
    </tr>`;
  }).join('');
  lucide.createIcons();
}

// ══ FILE AUDIO ════════════════════════════════════════════════════════════

function audioFileCell(poiId, poiName, audioUrl) {
  if (!audioUrl) {
    return `<div style="display:flex;flex-direction:column;gap:4px;align-items:center">
      <span style="font-size:11px;color:#f87171;background:#fef2f2;padding:3px 8px;border-radius:20px;border:1px solid #fecaca">✗ Chưa có</span>
      <button style="font-size:11px;padding:3px 10px;border-radius:20px;background:#eff6ff;border:1px solid #bfdbfe;color:#2563eb;display:inline-flex;align-items:center;gap:4px;cursor:pointer"
        onclick="triggerAudioUpload(${poiId},'${escapeSq(poiName)}')">
        <i data-lucide="upload" style="width:12px;height:12px"></i> Upload
      </button>
    </div>`;
  }
  const fullUrl = audioUrl.startsWith('http') ? audioUrl : `${BASE_URL}/${audioUrl}`;
  return `<div style="display:flex;flex-direction:column;gap:4px;align-items:center">
    <span style="font-size:11px;color:#16a34a;background:#f0fdf4;padding:3px 8px;border-radius:20px;border:1px solid #bbf7d0">✓ Có file</span>
    <div style="display:flex;gap:4px">
      <button id="play-file-${poiId}"
        style="font-size:11px;padding:3px 8px;border-radius:20px;background:#f0fdf4;border:1px solid #bbf7d0;color:#16a34a;display:inline-flex;align-items:center;gap:3px;cursor:pointer"
        onclick="previewUploadedFile('${fullUrl}',${poiId},this)">
        <i data-lucide="play" style="width:12px;height:12px"></i>
      </button>
      <button style="font-size:11px;padding:3px 8px;border-radius:20px;background:#eff6ff;border:1px solid #bfdbfe;color:#2563eb;display:inline-flex;align-items:center;cursor:pointer"
        onclick="triggerAudioUpload(${poiId},'${escapeSq(poiName)}')">
        <i data-lucide="refresh-cw" style="width:12px;height:12px"></i>
      </button>
      <button style="font-size:11px;padding:3px 8px;border-radius:20px;background:#fef2f2;border:1px solid #fecaca;color:#dc2626;display:inline-flex;align-items:center;cursor:pointer"
        onclick="deleteAudioFile(${poiId},'${escapeSq(poiName)}')">
        <i data-lucide="trash-2" style="width:12px;height:12px"></i>
      </button>
    </div>
  </div>`;
}

function triggerAudioUpload(poiId, poiName) {
  let inp = document.getElementById('_hidden_audio_input');
  if (!inp) {
    inp = document.createElement('input');
    inp.type = 'file'; inp.id = '_hidden_audio_input';
    inp.accept = '.mp3,.m4a,.wav,.ogg'; inp.style.display = 'none';
    document.body.appendChild(inp);
  }
  inp.value = '';
  inp.onchange = () => doUploadAudioFile(poiId, poiName, inp.files[0]);
  inp.click();
}

async function doUploadAudioFile(poiId, poiName, file) {
  if (!file) return;
  if (file.size > 10 * 1024 * 1024) { showToast('File quá lớn (tối đa 10MB)', 'danger'); return; }
  showToast('\u23f3 Đang upload...', 'info');
  const fd = new FormData();
  fd.append('audioFile', file);
  fd.append('poiId', String(poiId));
  try {
    const res = await fetch(`${API}/audio/upload`, { method: 'POST', body: fd });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Lỗi upload');
    showToast(`\u2705 Upload thành công: ${poiName}`, 'success');
    const poi = allPois.find(p => (p.id || p.Id) === poiId);
    if (poi) { poi.AudioUrl = data.url; poi.audioUrl = data.url; }
    renderAudioTable(); renderAudioStats();
  } catch (err) { showToast(`\u274c Upload thất bại: ${err.message}`, 'danger'); }
}

let _uploadedAudio = null;
let _uploadedPlayBtn = null;

function previewUploadedFile(url, poiId, btn) {
  if (_uploadedPlayBtn === btn && _uploadedAudio && !_uploadedAudio.paused) {
    _uploadedAudio.pause();
    btn.innerHTML = '<i data-lucide="play" style="width:12px;height:12px"></i>';
    lucide.createIcons(); return;
  }
  stopCurrentAudio();
  if (_uploadedAudio) { _uploadedAudio.pause(); _uploadedAudio = null; }
  if (_uploadedPlayBtn && _uploadedPlayBtn !== btn) {
    _uploadedPlayBtn.innerHTML = '<i data-lucide="play" style="width:12px;height:12px"></i>';
    lucide.createIcons();
  }
  _uploadedAudio = new Audio(url); _uploadedPlayBtn = btn;
  btn.innerHTML = '<i data-lucide="pause" style="width:12px;height:12px"></i>';
  lucide.createIcons();
  _uploadedAudio.onended = () => {
    btn.innerHTML = '<i data-lucide="play" style="width:12px;height:12px"></i>';
    lucide.createIcons(); _uploadedAudio = null; _uploadedPlayBtn = null;
  };
  _uploadedAudio.onerror = () => {
    showToast('Không thể phát file audio', 'danger');
    btn.innerHTML = '<i data-lucide="play" style="width:12px;height:12px"></i>';
    lucide.createIcons();
  };
  _uploadedAudio.play();
}

async function deleteAudioFile(poiId, poiName) {
  if (!confirm(`Xóa file audio của "${poiName}"?`)) return;
  try {
    const res = await fetch(`${API}/audio/${poiId}`, { method: 'DELETE' });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Lỗi xóa');
    showToast(`\u2705 Đã xóa: ${poiName}`, 'success');
    const poi = allPois.find(p => (p.id || p.Id) === poiId);
    if (poi) { poi.AudioUrl = ''; poi.audioUrl = ''; }
    renderAudioTable(); renderAudioStats();
  } catch (err) { showToast(`\u274c Xóa thất bại: ${err.message}`, 'danger'); }
}

// ══ TTS PLAYBACK ══════════════════════════════════════════════════════════

let _activeAudioBtn    = null;
let _isSpeechSynthesis = false;

function setBtnState(btn, state) {
  if (!btn) return;
  const S = {
    idle:    { html: '<i data-lucide="play"     style="width:13px;height:13px"></i> Nghe', bg: '#f0fdf4', bd: '#bbf7d0', cl: '#16a34a' },
    loading: { html: '<i data-lucide="loader-2" class="spin" style="width:13px;height:13px"></i> Tải',  bg: '#eff6ff', bd: '#bfdbfe', cl: '#2563eb' },
    playing: { html: '<i data-lucide="pause"    style="width:13px;height:13px"></i> Dừng', bg: '#fffbeb', bd: '#fde68a', cl: '#d97706' },
    paused:  { html: '<i data-lucide="play"     style="width:13px;height:13px"></i> Tiếp', bg: '#f0fdf4', bd: '#bbf7d0', cl: '#16a34a' },
  };
  const s = S[state] || S.idle;
  btn.innerHTML = s.html;
  btn.style.background  = s.bg;
  btn.style.borderColor = s.bd;
  btn.style.color       = s.cl;
  const stopBtn = btn.parentElement?.querySelector('.tts-stop-btn');
  if (stopBtn) {
    const active = state === 'playing' || state === 'paused' || state === 'loading';
    stopBtn.style.opacity       = active ? '1' : '0';
    stopBtn.style.pointerEvents = active ? 'auto' : 'none';
  }
  lucide.createIcons();
}

async function toggleAudioCell(text, langCode, btn) {
  if (_activeAudioBtn === btn) {
    if (_isSpeechSynthesis) {
      if (window.speechSynthesis.paused)        { window.speechSynthesis.resume(); setBtnState(btn, 'playing'); }
      else if (window.speechSynthesis.speaking) { window.speechSynthesis.pause();  setBtnState(btn, 'paused');  }
    } else if (_ttsAudio) {
      if (_ttsAudio.paused) { _ttsAudio.play();  setBtnState(btn, 'playing'); }
      else                  { _ttsAudio.pause(); setBtnState(btn, 'paused');  }
    }
    return;
  }
  stopCurrentAudio();
  _activeAudioBtn = btn;
  setBtnState(btn, 'loading');
  try   { await playTtsAudio(text, langCode); setBtnState(btn, 'playing'); }
  catch { stopCurrentAudio(); showToast('Lỗi phát âm thanh', 'danger'); }
}

function stopCurrentAudio() {
  window.speechSynthesis.cancel();
  if (_ttsAudio) { _ttsAudio.pause(); _ttsAudio.currentTime = 0; _ttsAudio = null; }
  _isSpeechSynthesis = false;
  if (_activeAudioBtn) { setBtnState(_activeAudioBtn, 'idle'); _activeAudioBtn = null; }
}

function stopAndResetCell(stopBtn) { stopCurrentAudio(); }

function escapeSq(s) {
  return (s || '').replace(/\\/g, '\\\\').replace(/'/g, "\\'").replace(/"/g, '&quot;').replace(/\n/g, ' ').replace(/\r/g, '');
}

// ══ GOOGLE CLOUD TTS (primary) + Web Speech API (fallback) ══
const GOOGLE_TTS_KEY = 'AIzaSyAMX0XgjmNv2O4Twk_CBBmjzDwopqtuexE';
const GOOGLE_TTS_VOICE_MAP = {
  'vi': { languageCode: 'vi-VN', name: 'vi-VN-Wavenet-A', ssmlGender: 'FEMALE' },
  'en': { languageCode: 'en-US', name: 'en-US-Wavenet-F', ssmlGender: 'FEMALE' },
  'zh': { languageCode: 'cmn-CN', name: 'cmn-CN-Wavenet-A', ssmlGender: 'FEMALE' },
  'ja': { languageCode: 'ja-JP', name: 'ja-JP-Wavenet-A', ssmlGender: 'FEMALE' },
  'ko': { languageCode: 'ko-KR', name: 'ko-KR-Wavenet-A', ssmlGender: 'FEMALE' },
  'fr': { languageCode: 'fr-FR', name: 'fr-FR-Wavenet-A', ssmlGender: 'FEMALE' },
};

let _ttsAudio = null; // audio element hiện tại để có thể dừng

async function playTtsAudio(text, langCode) {
  if (!text) return;
  // Reset trạng thái flag
  _isSpeechSynthesis = false;

  try {
    await playGoogleTts(text, langCode);
  } catch (err) {
    console.warn('[TTS] Google Cloud TTS thất bại, fallback Web Speech API:', err.message);
    _isSpeechSynthesis = true;
    playWebSpeechTts(text, langCode);
  }
}

async function playGoogleTts(text, langCode) {
  const voiceConfig = GOOGLE_TTS_VOICE_MAP[langCode];
  if (!voiceConfig) throw new Error(`Không có voice map cho ${langCode}`);

  const body = {
    input: { text },
    voice: voiceConfig,
    audioConfig: { audioEncoding: 'MP3', speakingRate: 0.95, pitch: 0.0 }
  };

  const res = await fetch(
    `https://texttospeech.googleapis.com/v1/text:synthesize?key=${GOOGLE_TTS_KEY}`,
    { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }
  );

  if (!res.ok) {
    const errText = await res.text();
    throw new Error(`Google TTS API lỗi ${res.status}: ${errText}`);
  }

  const data = await res.json();
  const audioBase64 = data.audioContent;
  if (!audioBase64) throw new Error('audioContent trống');

  // Decode base64 → blob → phát
  const byteChars = atob(audioBase64);
  const byteArr = new Uint8Array(byteChars.length);
  for (let i = 0; i < byteChars.length; i++) byteArr[i] = byteChars.charCodeAt(i);
  const blob = new Blob([byteArr], { type: 'audio/mpeg' });
  const url = URL.createObjectURL(blob);

  // Quan trọng: Lưu url để revoke khi audio kết thúc
  _ttsAudio = new Audio(url);
  _ttsAudio.onended = () => { 
    URL.revokeObjectURL(url); 
    _ttsAudio = null; 
    if (_activeAudioBtn) { setBtnState(_activeAudioBtn, 'idle'); _activeAudioBtn = null; }
  };
  await _ttsAudio.play();
  console.log(`[TTS] Google Cloud TTS [${langCode}]: OK`);
}

function playWebSpeechTts(text, langCode) {
  const localeMap = { 'vi':'vi-VN', 'en':'en-US', 'zh':'zh-CN', 'ja':'ja-JP', 'ko':'ko-KR', 'fr':'fr-FR', 'th':'th-TH', 'ru':'ru-RU' };
  const utterance = new SpeechSynthesisUtterance(text);
  utterance.lang = localeMap[langCode] || langCode;

  const voices = window.speechSynthesis.getVoices();
  if (voices.length > 0) {
    const targetLang = (localeMap[langCode] || langCode).toLowerCase();
    let voice = voices.find(v => v.lang.toLowerCase().startsWith(targetLang));
    if (!voice && langCode === 'vi') {
      voice = voices.find(v => textMatch(v.name, 'vietnamese') || textMatch(v.name, 'viet'));
    }
    if (voice) utterance.voice = voice;
  }
  
  utterance.onend = () => {
    _isSpeechSynthesis = false;
    if (_activeAudioBtn) { setBtnState(_activeAudioBtn, 'idle'); _activeAudioBtn = null; }
  };
  
  window.speechSynthesis.speak(utterance);
}

// Preload Web Speech voices khi trang load
if (typeof window.speechSynthesis !== 'undefined') {
  window.speechSynthesis.onvoiceschanged = () => window.speechSynthesis.getVoices();
  window.speechSynthesis.getVoices();
}
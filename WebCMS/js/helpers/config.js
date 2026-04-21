const API = 'http://192.168.1.233:5256/api', BASE_URL = 'http://192.168.1.233:5256';
let map, markers = [], allPois = [], tours = [], historyData = [];
let mainChart, pieChart;
let selectedPois = [];
let currentFilter = 'all';
let statsData = null;
let poiCatFilter = '';

// ══ NGÔN NGỮ - lưu trong localStorage ══
const DEFAULT_LANGS = [
  { code: 'vi', name: 'Tiếng Việt', flag: '🇻🇳', isDefault: true },
  { code: 'en', name: 'English',    flag: '🇺🇸', isDefault: false },
  { code: 'zh', name: '中文',        flag: '🇨🇳', isDefault: false },
];
function getLangs() {
  try {
    const saved = localStorage.getItem('cms_languages');
    return saved ? JSON.parse(saved) : DEFAULT_LANGS;
  } catch { return DEFAULT_LANGS; }
}
function saveLangs(langs) {
  localStorage.setItem('cms_languages', JSON.stringify(langs));
}


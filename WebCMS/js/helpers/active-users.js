/**
 * ══ ACTIVE USERS TRACKING (CMS Admin Sessions) ══
 * Theo dõi admin/owner đang mở tab WebCMS.
 * Lưu trong localStorage — ĐỘC LẬP với "App đang online" (app mobile, lấy từ API /tracking/online-users)
 */

const ACTIVE_USERS_KEY = 'cms_active_users';
const ACTIVE_USER_TIMEOUT = 30 * 60 * 1000; // 30 phút timeout
const UPDATE_INTERVAL = 5 * 60 * 1000; // Cập nhật mỗi 5 phút

/**
 * Lấy danh sách người dùng đang hoạt động
 */
function getActiveUsers() {
  try {
    const data = localStorage.getItem(ACTIVE_USERS_KEY);
    return data ? JSON.parse(data) : {};
  } catch {
    return {};
  }
}

/**
 * Lưu danh sách người dùng đang hoạt động
 */
function saveActiveUsers(users) {
  try {
    localStorage.setItem(ACTIVE_USERS_KEY, JSON.stringify(users));
  } catch (e) {
    console.error('saveActiveUsers:', e);
  }
}

/**
 * Thêm người dùng vào danh sách hoạt động (khi login)
 * @param {string} username - Tên đăng nhập
 * @param {string} fullName - Tên đầy đủ
 * @param {string} role - Vai trò (admin, owner, user)
 * @param {boolean} isAnonymous - Nếu true, sẽ hiển thị "Người dùng ẩn danh"
 */
function registerActiveUser(username, fullName, role, isAnonymous = false) {
  const users = getActiveUsers();
  const sessionId = sessionStorage.getItem('cms_session_id') || generateSessionId();
  
  // Lưu session ID
  if (!sessionStorage.getItem('cms_session_id')) {
    sessionStorage.setItem('cms_session_id', sessionId);
  }
  
  users[sessionId] = {
    username: username,
    fullName: fullName,
    role: role,
    isAnonymous: isAnonymous,
    loginTime: new Date().getTime(),
    lastActiveTime: new Date().getTime(),
    displayName: isAnonymous ? 'Người dùng ẩn danh' : fullName || username
  };
  
  saveActiveUsers(users);
  
  // Xóa inactive users
  cleanupInactiveUsers();
  
  // Cập nhật active user count
  updateActiveUserCount();
}

/**
 * Cập nhật thời gian hoạt động cuối cùng của user
 */
function updateUserActivity() {
  const sessionId = sessionStorage.getItem('cms_session_id');
  if (!sessionId) return;
  
  const users = getActiveUsers();
  if (users[sessionId]) {
    users[sessionId].lastActiveTime = new Date().getTime();
    saveActiveUsers(users);
  }
}

/**
 * Xóa người dùng khỏi danh sách hoạt động (khi logout)
 */
function unregisterActiveUser() {
  const sessionId = sessionStorage.getItem('cms_session_id');
  if (!sessionId) return;
  
  const users = getActiveUsers();
  delete users[sessionId];
  saveActiveUsers(users);
  
  sessionStorage.removeItem('cms_session_id');
  updateActiveUserCount();
}

/**
 * Xóa những người dùng không hoạt động trong thời gian quy định
 */
function cleanupInactiveUsers() {
  const users = getActiveUsers();
  const now = new Date().getTime();
  let changed = false;
  
  Object.keys(users).forEach(sessionId => {
    const user = users[sessionId];
    if (now - user.lastActiveTime > ACTIVE_USER_TIMEOUT) {
      delete users[sessionId];
      changed = true;
    }
  });
  
  if (changed) {
    saveActiveUsers(users);
    updateActiveUserCount();
  }
}

/**
 * Lấy số lượng người dùng đang hoạt động
 */
function getActiveUserCount() {
  cleanupInactiveUsers();
  return Object.keys(getActiveUsers()).length;
}

/**
 * Cập nhật hiển thị active user count
 */
function updateActiveUserCount() {
  try {
    const count = getActiveUserCount();
    const displayText = getActiveUsersDisplayText();
    
    // Cập nhật các element có attribute data-active-users-count
    // (dành cho stat card CMS admin, KHÔNG phải pill “App đang online”)
    const countEls = document.querySelectorAll('[data-active-users-count]');
    countEls.forEach(el => {
      // Chỉ cập nhật nếu element này được đánh dấu là “cms admin tracking”
      if (el.hasAttribute('data-cms-admin-count')) {
        el.textContent = count || '0';
        el.setAttribute('data-count', count || '0');
      }
    });
    
    // Cập nhật các element có attribute data-active-users-text (hiển thị danh sách)
    const textEls = document.querySelectorAll('[data-active-users-text]');
    textEls.forEach(el => {
      el.textContent = displayText;
    });
    
    // Lưu ý: #active-users-count (pill header “App đang online”) ĐƯỢC CẬP NHẬT RIÊNG
    // bởi fetchRealActiveUsers() trong layout.js (lấy từ API /tracking/online-users)
    // KHÔNG ghi đè ở đây để tránh hiển thị admin CMS làm “App user”
    
    // Phát custom event để update components
    document.dispatchEvent(new CustomEvent('activeUsersUpdated', { detail: { count, displayText } }));
  } catch(e) {
    console.error('updateActiveUserCount error:', e);
  }
}

/**
 * Sinh session ID ngẫu nhiên
 */
function generateSessionId() {
  return 'session_' + Math.random().toString(36).substr(2, 9) + '_' + Date.now();
}

/**
 * Khởi tạo active users tracking
 */
function initActiveUsersTracking() {
  // Cập nhật hoạt động khi user tương tác
  ['mousedown', 'keydown', 'touchstart', 'scroll', 'click'].forEach(event => {
    document.addEventListener(event, () => {
      updateUserActivity();
    }, { passive: true });
  });
  
  // Kiểm tra và xóa inactive users mỗi 5 phút
  setInterval(cleanupInactiveUsers, UPDATE_INTERVAL);
  
  // Xóa user khỏi danh sách active khi đóng tab/window
  window.addEventListener('beforeunload', () => {
    unregisterActiveUser();
  });
  
  // Update count mỗi 10 giây
  setInterval(updateActiveUserCount, 10000);
  
  // Initial count
  updateActiveUserCount();
}

/**
 * Lấy danh sách chi tiết user đang hoạt động
 */
function getActiveUsersList() {
  cleanupInactiveUsers();
  return getActiveUsers();
}

/**
 * Lấy text hiển thị danh sách người dùng hoạt động
 * @returns {string} Text có dạng "Người A, Người B, Người dùng ẩn danh (2)"
 */
function getActiveUsersDisplayText() {
  try {
    const users = getActiveUsersList();
    const usersList = Object.values(users);
    
    if (!usersList || usersList.length === 0) {
      return 'Không có người dùng';
    }
    
    // Nhóm người dùng: người dùng bình thường và người dùng ẩn danh
    const namedUsers = usersList.filter(u => u && !u.isAnonymous);
    const guestCount = usersList.filter(u => u && u.isAnonymous).length;
    
    let displayParts = [];
    
    // Hiển thị tên những người dùng không ẩn danh (tối đa 2 người)
    namedUsers.slice(0, 2).forEach(u => {
      displayParts.push((u.displayName || u.fullName || u.username || 'User').substring(0, 15));
    });
    
    // Thêm người dùng ẩn danh nếu có
    if (guestCount > 0) {
      displayParts.push(`Anonymous${guestCount > 1 ? ' (' + guestCount + ')' : ''}`);
    }
    
    // Nếu có người dùng khác
    if (namedUsers.length > 2) {
      displayParts.push('...');
    }
    
    return displayParts.join(', ') || 'Không có người dùng';
  } catch(e) {
    console.error('getActiveUsersDisplayText error:', e);
    return 'Error';
  }
}

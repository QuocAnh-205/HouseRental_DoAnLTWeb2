import { createContext, useContext, useEffect, useLayoutEffect, useRef, useState, type FormEvent } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faBars, faBell, faBullhorn, faChevronDown, faCircleUser, faClock, faComments, faHeart, faHouseChimney, faListCheck, faLocationDot,
  faMagnifyingGlass, faMoon, faNewspaper, faPenToSquare, faRightFromBracket, faSun, faTag, faUser, faXmark,
} from '@fortawesome/free-solid-svg-icons';
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { api } from '../api/client';
import { propertyTypeLabel, roleLabel } from '../api/labels';
import type { PropertyType, UnreadCount } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { Avatar, Toasts } from './ui';
import { ACCOUNT_MENU } from '../accountMenu';
import { propertyTypeIcon } from '../icons';
import { useTheme } from '../theme';

interface Unread { notifications: number; messages: number }
const UnreadContext = createContext<Unread>({ notifications: 0, messages: 0 });
/** Số tin nhắn / thông báo chưa đọc (dùng ở thanh bên khu vực tài khoản) */
export const useUnread = () => useContext(UnreadContext);


const TYPES: PropertyType[] = ['Room', 'Apartment', 'House', 'Villa', 'Office'];

/** Quận / huyện của Đà Nẵng (dữ liệu tin đăng lấy từ Nhà Tốt khu vực Đà Nẵng) */
export const DISTRICTS = ['Hải Châu', 'Thanh Khê', 'Sơn Trà', 'Ngũ Hành Sơn', 'Liên Chiểu', 'Cẩm Lệ', 'Hòa Vang'];

/** Trang chủ có hero tràn viền */
const isLanding = (path: string) => path === '/';

// Placeholder dài nhất còn vừa ô nhập (ô co giãn theo tên quận và độ rộng màn hình) → không bao giờ bị cắt chữ
const SEARCH_PLACEHOLDERS = ['Tìm phòng trọ, căn hộ, tên đường...', 'Tìm phòng trọ, căn hộ...', 'Tìm phòng, căn hộ...', 'Tìm phòng trọ...', 'Tìm kiếm...'];

function useFittingPlaceholder(options: string[], trigger: unknown) {
  const ref = useRef<HTMLInputElement>(null);
  const [text, setText] = useState(options[0]);
  const fitRef = useRef<() => void>(() => {});
  useLayoutEffect(() => {
    const el = ref.current;
    const ctx = document.createElement('canvas').getContext('2d');
    if (!el || !ctx) return;
    const fit = () => {
      const cs = getComputedStyle(el);
      ctx.font = `${cs.fontWeight} ${cs.fontSize} ${cs.fontFamily}`;
      const room = el.clientWidth - parseFloat(cs.paddingLeft) - parseFloat(cs.paddingRight) - 2;
      setText(options.find(o => ctx.measureText(o).width <= room) ?? options[options.length - 1]);
    };
    fitRef.current = fit;
    fit();
    const ro = new ResizeObserver(fit);
    ro.observe(el);
    document.fonts?.ready.then(fit);
    return () => ro.disconnect();
  }, [options]);
  // Đổi quận → ô khu vực đổi độ rộng → đo lại ngay sau khi DOM cập nhật, trước khi vẽ
  useLayoutEffect(() => { fitRef.current(); }, [trigger]);
  return { ref, text };
}

export default function Layout() {
  const { user, logout } = useAuth();
  const { theme, toggle: toggleTheme } = useTheme();
  const navigate = useNavigate();
  const location = useLocation();
  const [menuOpen, setMenuOpen] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [unread, setUnread] = useState({ notifications: 0, messages: 0 });
  const [scrolled, setScrolled] = useState(false);
  const [catOpen, setCatOpen] = useState(false);
  const [keyword, setKeyword] = useState('');
  const [district, setDistrict] = useState('');
  const menuRef = useRef<HTMLDivElement>(null);
  const searchPh = useFittingPlaceholder(SEARCH_PLACEHOLDERS, district);
  const catRef = useRef<HTMLDivElement>(null);

  // Đếm thông báo / tin nhắn chưa đọc, cập nhật mỗi 30 giây và khi chuyển trang
  useEffect(() => {
    if (!user) return;
    let alive = true;
    const load = async () => {
      try {
        const [n, m] = await Promise.all([
          api.get<UnreadCount>('/api/notifications/unread-count'),
          api.get<UnreadCount>('/api/conversations/unread-count'),
        ]);
        if (alive) setUnread({ notifications: n.unreadCount, messages: m.unreadCount });
      } catch { /* bỏ qua */ }
    };
    load();
    const timer = setInterval(load, 30_000);
    const onRefresh = () => load();
    window.addEventListener('unread-changed', onRefresh);
    return () => { alive = false; clearInterval(timer); window.removeEventListener('unread-changed', onRefresh); };
  }, [user, location.pathname]);

  useEffect(() => {
    const close = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) setMenuOpen(false);
      if (catRef.current && !catRef.current.contains(e.target as Node)) setCatOpen(false);
    };
    const onScroll = () => setScrolled(window.scrollY > 8);
    document.addEventListener('mousedown', close);
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => { document.removeEventListener('mousedown', close); window.removeEventListener('scroll', onScroll); };
  }, []);

  useEffect(() => {
    setMenuOpen(false);
    setMobileOpen(false);
    setCatOpen(false);
    if (!location.hash) {
      window.scrollTo({ top: 0 });
      return;
    }
    // Có #vị-trí (vd mở từ thông báo: /properties/5#reviews): chờ trang tải dữ liệu rồi cuộn tới đúng phần đó
    window.scrollTo({ top: 0 });
    const target = decodeURIComponent(location.hash.slice(1));
    let tries = 0;
    const timer = setInterval(() => {
      const el = document.getElementById(target);
      if (el || ++tries > 40) {
        clearInterval(timer);
        if (el) {
          el.scrollIntoView({ behavior: 'smooth', block: 'start' });
          el.classList.add('is-highlight');
          setTimeout(() => el.classList.remove('is-highlight'), 2600);
        }
      }
    }, 100);
    return () => clearInterval(timer);
  }, [location.pathname, location.hash]);

  const menu = user ? ACCOUNT_MENU[user.role][0].items : [];
  const postLink = user?.role === 'Landlord' ? '/dang-tin' : user ? null : '/register?role=Landlord';

  const submitSearch = (e: FormEvent) => {
    e.preventDefault();
    const p = new URLSearchParams();
    if (keyword.trim()) p.set('keyword', keyword.trim());
    if (district) p.set('district', district);
    navigate(`/search?${p}`);
  };

  return (
    <UnreadContext.Provider value={unread}>
      <header className={`nt-header ${scrolled ? 'scrolled' : ''}`}>
        <div className="container nt-header-inner">
          <button className="nt-icon nav-burger" onClick={() => setMobileOpen(o => !o)} aria-label="Menu">
            <FontAwesomeIcon icon={mobileOpen ? faXmark : faBars} />
          </button>
          <Link to="/" className="brand">
            <span className="brand-mark"><FontAwesomeIcon icon={faHouseChimney} /></span>
            <span>Tổ<b>Ấm</b></span>
          </Link>

          <div className="nt-cat" ref={catRef}>
            <button className="nt-cat-btn" onClick={() => setCatOpen(o => !o)}>
              <FontAwesomeIcon icon={faBars} /> <span>Danh mục</span> <FontAwesomeIcon icon={faChevronDown} className="caret" />
            </button>
            {catOpen && (
              <div className="dropdown nt-cat-menu">
                <div className="nt-cat-title">Cho thuê tại Đà Nẵng</div>
                {TYPES.map(t => (
                  <Link key={t} to={`/search?type=${t}`}>
                    <FontAwesomeIcon icon={propertyTypeIcon[t]} fixedWidth /> Thuê {propertyTypeLabel[t].toLowerCase()}
                  </Link>
                ))}
                <hr />
                <Link to="/search?sortBy=newest"><FontAwesomeIcon icon={faClock} fixedWidth /> Tin mới nhất</Link>
                <Link to="/search?type=Room&maxPrice=2000000"><FontAwesomeIcon icon={faTag} fixedWidth /> Phòng trọ dưới 2 triệu</Link>
              </div>
            )}
          </div>

          <NavLink to="/ban-tin" className="nt-text-link nav-feed" title="Bản tin: tin cho thuê mới được duyệt">
            <FontAwesomeIcon icon={faNewspaper} /> <span>Bản tin</span>
          </NavLink>

          <form className="nt-search" onSubmit={submitSearch}>
            {/* Nhãn hiển thị co giãn theo chữ; select trong suốt phủ lên trên để giữ dropdown gốc */}
            <label className="nt-search-loc" title="Chọn khu vực">
              <FontAwesomeIcon icon={faLocationDot} className="loc-pin" />
              <span className="loc-text">{district || 'Đà Nẵng'}</span>
              <FontAwesomeIcon icon={faChevronDown} className="loc-caret" />
              <select value={district} onChange={e => setDistrict(e.target.value)} aria-label="Khu vực">
                <option value="">Đà Nẵng</option>
                {DISTRICTS.map(d => <option key={d} value={d}>{d}</option>)}
              </select>
            </label>
            <input ref={searchPh.ref} placeholder={searchPh.text} title="Tìm phòng trọ, căn hộ, tên đường..." value={keyword} onChange={e => setKeyword(e.target.value)} />
            <button className="nt-search-btn" aria-label="Tìm kiếm">
              <FontAwesomeIcon icon={faMagnifyingGlass} /><span>Tìm kiếm</span>
            </button>
          </form>

          <div className="nav-right">
            <button className="nt-icon theme-toggle" onClick={toggleTheme}
              aria-label={theme === 'dark' ? 'Chuyển sang giao diện sáng' : 'Chuyển sang giao diện tối'}
              title={theme === 'dark' ? 'Giao diện sáng' : 'Giao diện tối'}>
              <FontAwesomeIcon icon={theme === 'dark' ? faSun : faMoon} />
            </button>
            <NavLink to={user ? '/favorites' : '/login'} className="nt-icon" title="Tin đã lưu"><FontAwesomeIcon icon={faHeart} /></NavLink>
            {user && (
              <>
                <NavLink to="/messages" className="nt-icon" title="Tin nhắn">
                  <FontAwesomeIcon icon={faComments} />{unread.messages > 0 && <span className="dot">{unread.messages}</span>}
                </NavLink>
                <NavLink to="/notifications" className="nt-icon" title="Thông báo">
                  <FontAwesomeIcon icon={faBell} />{unread.notifications > 0 && <span className="dot">{unread.notifications}</span>}
                </NavLink>
              </>
            )}
            {user?.role === 'Landlord' && (
              <NavLink to="/landlord/properties" className="nt-text-link hide-md"><FontAwesomeIcon icon={faListCheck} /> Quản lý tin</NavLink>
            )}
            {user ? (
              <div className="user-menu" ref={menuRef}>
                <button className="user-btn" onClick={() => setMenuOpen(o => !o)}>
                  <Avatar name={user.fullName} url={user.avatarUrl} size={30} />
                  <span className="user-name">{user.fullName}</span>
                  <span className="caret"><FontAwesomeIcon icon={faChevronDown} /></span>
                </button>
                {menuOpen && (
                  <div className="dropdown">
                    <div className="dropdown-head">
                      <Avatar name={user.fullName} url={user.avatarUrl} size={40} />
                      <div>
                        <strong>{user.fullName}</strong>
                        <span className="muted small">{roleLabel[user.role]} · {user.email}</span>
                      </div>
                    </div>
                    {menu.map(item => <Link key={item.to} to={item.to}><FontAwesomeIcon icon={item.icon} fixedWidth /> {item.label}</Link>)}
                    <hr />
                    <Link to={`/users/${user.id}`}><FontAwesomeIcon icon={faNewspaper} fixedWidth /> Trang cá nhân{user.role === 'Landlord' ? ' & bản tin' : ''}</Link>
                    <Link to="/profile"><FontAwesomeIcon icon={faUser} fixedWidth /> Hồ sơ & mật khẩu</Link>
                    <button onClick={async () => { await logout(); navigate('/'); }}>
                      <FontAwesomeIcon icon={faRightFromBracket} fixedWidth /> Đăng xuất
                    </button>
                  </div>
                )}
              </div>
            ) : (
              <Link to="/login" className="nt-text-link"><FontAwesomeIcon icon={faCircleUser} /> Đăng nhập</Link>
            )}
            {postLink && (
              <Link to={postLink} className="nt-post-btn"><FontAwesomeIcon icon={faPenToSquare} /> ĐĂNG TIN</Link>
            )}
          </div>
        </div>
        <nav className={`nt-mobile-nav ${mobileOpen ? 'open' : ''}`}>
          <NavLink to="/ban-tin"><FontAwesomeIcon icon={faNewspaper} fixedWidth /> Bản tin</NavLink>
          <NavLink to="/search" end><FontAwesomeIcon icon={faMagnifyingGlass} fixedWidth /> Tất cả tin cho thuê</NavLink>
          {TYPES.map(t => (
            <Link key={t} to={`/search?type=${t}`}><FontAwesomeIcon icon={propertyTypeIcon[t]} fixedWidth /> Thuê {propertyTypeLabel[t].toLowerCase()}</Link>
          ))}
          {!user && <Link to="/register"><FontAwesomeIcon icon={faUser} fixedWidth /> Đăng ký tài khoản</Link>}
        </nav>
      </header>
      <main className={isLanding(location.pathname) ? 'is-landing' : ''}>
        <Outlet />
      </main>
      <Footer />
      <Toasts />
    </UnreadContext.Provider>
  );
}

function Footer() {
  const { user } = useAuth();
  return (
    <footer className="footer">
      <div className="container footer-grid">
        <div className="footer-brand">
          <Link to="/" className="brand light">
            <span className="brand-mark"><FontAwesomeIcon icon={faHouseChimney} /></span>
            <span>Tổ<b>Ấm</b></span>
          </Link>
          <p>Nền tảng kết nối người thuê và chủ nhà: tin đăng được kiểm duyệt, đặt lịch xem nhà, ký hợp đồng, nhận hóa đơn và thanh toán ngay trên một trang web.</p>
        </div>
        <div>
          <h4>Tìm nhà</h4>
          <Link to="/search?type=Room">Phòng trọ</Link>
          <Link to="/search?type=Apartment">Căn hộ</Link>
          <Link to="/search?type=House">Nhà nguyên căn</Link>
          <Link to="/search?type=Villa">Biệt thự</Link>
          <Link to="/search?type=Office">Văn phòng</Link>
        </div>
        <div>
          <h4>Người thuê</h4>
          <Link to="/ban-tin">Bản tin</Link>
          <Link to="/search?sortBy=newest">Tin mới nhất</Link>
          <Link to="/search?sortBy=rating_desc">Được đánh giá cao</Link>
          <Link to="/search?sortBy=price_asc">Giá tốt nhất</Link>
          <Link to={user ? '/favorites' : '/login'}>Nhà đã lưu</Link>
          <Link to={user ? '/appointments' : '/login'}>Lịch hẹn xem nhà</Link>
        </div>
        <div>
          <h4>Chủ nhà</h4>
          <Link to={user?.role === 'Landlord' ? '/dang-tin' : '/register?role=Landlord'}>
            <FontAwesomeIcon icon={faBullhorn} /> Đăng tin miễn phí
          </Link>
          <Link to={user?.role === 'Landlord' ? '/landlord/properties' : '/login'}>Quản lý tin đăng</Link>
          <Link to={user?.role === 'Landlord' ? '/invoices' : '/login'}>Lập hóa đơn điện nước</Link>
          <Link to={user?.role === 'Landlord' ? '/dashboard' : '/login'}>Thống kê doanh thu</Link>
        </div>
      </div>
      <div className="footer-bottom">
        <div className="container">© {new Date().getFullYear()} TổẤm · Frontend cho HouseRentalAPI</div>
      </div>
    </footer>
  );
}

/** Gọi sau khi đọc thông báo / tin nhắn để cập nhật số trên thanh điều hướng */
export const notifyUnreadChanged = () => window.dispatchEvent(new Event('unread-changed'));

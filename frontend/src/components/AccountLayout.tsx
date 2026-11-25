import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faMagnifyingGlass, faPlus, faRightFromBracket } from '@fortawesome/free-solid-svg-icons';
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { roleLabel } from '../api/labels';
import { useAuth } from '../auth/AuthContext';
import { ACCOUNT_MENU } from '../accountMenu';
import { useUnread } from './Layout';
import { Avatar, Loading } from './ui';

/** Khung khu vực tài khoản: thanh bên trái + nội dung */
export default function AccountLayout() {
  const { user, loading, logout } = useAuth();
  const unread = useUnread();
  const navigate = useNavigate();

  if (loading) return <Loading />;
  if (!user) return <Outlet />; // RequireAuth trong từng route sẽ chuyển về trang đăng nhập

  return (
    <div className="container account-shell">
      <aside className="account-side">
        <div className="side-user">
          <Avatar name={user.fullName} url={user.avatarUrl} size={48} />
          <div className="side-user-info">
            <strong className="ellipsis">{user.fullName}</strong>
            <span className={`role-tag role-${user.role.toLowerCase()}`}>{roleLabel[user.role]}</span>
          </div>
        </div>

        {user.role === 'Landlord' && (
          <Link to="/dang-tin" className="btn btn-primary btn-block side-cta"><FontAwesomeIcon icon={faPlus} /> Đăng tin cho thuê</Link>
        )}
        {user.role === 'Tenant' && (
          <Link to="/search" className="btn btn-primary btn-block side-cta"><FontAwesomeIcon icon={faMagnifyingGlass} /> Tìm nhà để thuê</Link>
        )}

        <nav className="side-nav">
          {ACCOUNT_MENU[user.role].map(group => (
            <div key={group.title} className="side-group">
              <div className="side-group-title">{group.title}</div>
              {group.items.map(item => {
                const count = item.badge ? unread[item.badge] : 0;
                return (
                  <NavLink key={item.to} to={item.to} end={item.to === '/landlord/properties'} className="side-link">
                    <FontAwesomeIcon icon={item.icon} fixedWidth />
                    <span>{item.label}</span>
                    {count > 0 && <span className="side-badge">{count}</span>}
                  </NavLink>
                );
              })}
            </div>
          ))}
          <button className="side-link side-logout" onClick={async () => { await logout(); navigate('/'); }}>
            <FontAwesomeIcon icon={faRightFromBracket} fixedWidth /><span>Đăng xuất</span>
          </button>
        </nav>
      </aside>
      <div className="account-main">
        <Outlet />
      </div>
    </div>
  );
}

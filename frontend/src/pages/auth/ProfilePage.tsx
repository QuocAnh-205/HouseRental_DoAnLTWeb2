import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faCalendarDays, faEnvelope, faEye, faFloppyDisk, faIdCard, faKey, faPhone, faRightFromBracket, faShieldHalved,
} from '@fortawesome/free-solid-svg-icons';
import { api, tokenStore } from '../../api/client';
import { fmtDate, fmtDateTime, roleLabel } from '../../api/labels';
import type { UserDto } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { Avatar, ImageUploadButton, PasswordInput } from '../../components/ui';
import { toast, useAction } from '../../hooks';

export default function ProfilePage() {
  const { user, setUser, logout } = useAuth();
  const navigate = useNavigate();
  const { busy, run } = useAction();
  const [fullName, setFullName] = useState(user!.fullName);
  const [phoneNumber, setPhone] = useState(user!.phoneNumber ?? '');
  const [avatarUrl, setAvatarUrl] = useState(user!.avatarUrl ?? '');
  const [pw, setPw] = useState({ currentPassword: '', newPassword: '', confirmNewPassword: '' });

  const saveProfile = async (e: FormEvent) => {
    e.preventDefault();
    const updated = await run(() => api.put<UserDto>('/api/users/me', {
      fullName, phoneNumber: phoneNumber || null, avatarUrl: avatarUrl || null,
    }));
    if (updated) {
      setUser(updated);
      toast('Đã cập nhật hồ sơ');
    }
  };

  const changePassword = async (e: FormEvent) => {
    e.preventDefault();
    if (pw.newPassword !== pw.confirmNewPassword) return toast('Mật khẩu xác nhận không khớp', 'error');
    const ok = await run(() => api.put('/api/auth/change-password', pw));
    if (ok !== undefined) {
      toast('Đã đổi mật khẩu');
      setPw({ currentPassword: '', newPassword: '', confirmNewPassword: '' });
    }
  };

  const logoutAll = async () => {
    if (!confirm('Đăng xuất khỏi tất cả thiết bị?')) return;
    await run(() => api.post('/api/auth/logout-all'));
    tokenStore.clear();
    await logout();
    navigate('/login');
  };

  return (
    <div className="container narrow profile-page">
      <div className="profile-cover">
        <div className="profile-cover-bg" />
        <div className="profile-cover-body">
          <div className="profile-avatar">
            <Avatar name={fullName || user!.fullName} url={avatarUrl || undefined} size={96} />
          </div>
          <div className="grow">
            <h1>{user!.fullName}</h1>
            <div className="profile-meta">
              <span className={`role-tag role-${user!.role.toLowerCase()}`}>{roleLabel[user!.role]}</span>
              <span><FontAwesomeIcon icon={faEnvelope} /> {user!.email}</span>
              {user!.phoneNumber && <span><FontAwesomeIcon icon={faPhone} /> {user!.phoneNumber}</span>}
              <span><FontAwesomeIcon icon={faCalendarDays} /> Tham gia {fmtDate(user!.createdAt)}</span>
            </div>
          </div>
          {user!.role !== 'Admin' && (
            <Link to={`/users/${user!.id}`} className="btn btn-ghost btn-sm"><FontAwesomeIcon icon={faEye} /> Xem hồ sơ công khai</Link>
          )}
        </div>
      </div>

      <div className="grid-2 gap-lg mt">
        <form className="card pad" onSubmit={saveProfile}>
          <h3 className="card-title"><FontAwesomeIcon icon={faIdCard} /> Thông tin cá nhân</h3>
          <div className="avatar-row">
            <Avatar name={fullName || user!.fullName} url={avatarUrl || undefined} size={64} />
            <div>
              <ImageUploadButton multiple={false} label="Đổi ảnh đại diện" onUploaded={urls => setAvatarUrl(urls[0])} />
              {avatarUrl && <button type="button" className="link-btn small" onClick={() => setAvatarUrl('')}>Xóa ảnh</button>}
            </div>
          </div>
          <label className="field"><span>Email</span><input value={user!.email} disabled /></label>
          <label className="field"><span>Họ tên</span>
            <input required value={fullName} onChange={e => setFullName(e.target.value)} />
          </label>
          <label className="field"><span>Số điện thoại</span>
            <input value={phoneNumber} onChange={e => setPhone(e.target.value)} placeholder="Chủ nhà / người thuê sẽ liên hệ qua số này" />
          </label>
          <button className="btn btn-primary" disabled={busy}><FontAwesomeIcon icon={faFloppyDisk} /> Lưu thay đổi</button>
        </form>

        <div>
          <form className="card pad" onSubmit={changePassword}>
            <h3 className="card-title"><FontAwesomeIcon icon={faKey} /> Đổi mật khẩu</h3>
            <label className="field"><span>Mật khẩu hiện tại</span>
              <PasswordInput required value={pw.currentPassword} onChange={v => setPw({ ...pw, currentPassword: v })} />
            </label>
            <label className="field"><span>Mật khẩu mới</span>
              <PasswordInput required minLength={6} autoComplete="new-password" showStrength value={pw.newPassword} onChange={v => setPw({ ...pw, newPassword: v })} />
            </label>
            <label className="field"><span>Nhập lại mật khẩu mới</span>
              <PasswordInput required autoComplete="new-password" value={pw.confirmNewPassword} onChange={v => setPw({ ...pw, confirmNewPassword: v })} />
            </label>
            <button className="btn btn-primary" disabled={busy}>Đổi mật khẩu</button>
          </form>
          <div className="card pad mt danger-zone">
            <h3 className="card-title"><FontAwesomeIcon icon={faShieldHalved} /> Bảo mật</h3>
            <p className="muted small">Đăng nhập gần nhất: <strong>{fmtDateTime(user!.lastLoginAt)}</strong></p>
            <p className="muted small">Nếu bạn nghi ngờ có người khác dùng tài khoản, hãy đổi mật khẩu và thu hồi mọi phiên đăng nhập.</p>
            <button className="btn btn-danger" onClick={logoutAll} disabled={busy}><FontAwesomeIcon icon={faRightFromBracket} /> Đăng xuất mọi thiết bị</button>
          </div>
        </div>
      </div>
    </div>
  );
}

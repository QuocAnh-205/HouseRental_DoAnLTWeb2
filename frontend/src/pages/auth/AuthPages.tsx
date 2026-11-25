import { useState, type FormEvent } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faKey, faHouseUser, faArrowLeft, faHouseChimney, faCircleCheck } from '@fortawesome/free-solid-svg-icons';
import type { ReactNode } from 'react';
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { api, errorMessage, requestRaw } from '../../api/client';
import type { ForgotPasswordResultDto } from '../../api/types';
import { useAuth, type RegisterInput } from '../../auth/AuthContext';
import { toast } from '../../hooks';
import { PasswordInput } from '../../components/ui';

const DEMO_ACCOUNTS = [
  { label: 'Admin', email: 'admin@houserental.vn', password: 'Admin@123' },
  { label: 'Chủ nhà', email: 'landlord@houserental.vn', password: 'Landlord@123' },
  { label: 'Người thuê', email: 'tenant@houserental.vn', password: 'Tenant@123' },
];

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from ?? '/';
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError('');
    try {
      const user = await login(email, password);
      toast(`Xin chào, ${user.fullName}!`);
      navigate(from === '/' ? (user.role === 'Tenant' ? '/' : '/dashboard') : from, { replace: true });
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <AuthShell>
      <form className="card auth-card" onSubmit={submit}>
        <h2>Đăng nhập</h2>
        {error && <div className="alert alert-error">{error}</div>}
        <label className="field"><span>Email</span>
          <input type="email" required value={email} onChange={e => setEmail(e.target.value)} autoFocus />
        </label>
        <label className="field"><span>Mật khẩu</span>
          <PasswordInput required value={password} onChange={setPassword} />
        </label>
        <button className="btn btn-primary btn-block" disabled={busy}>{busy ? 'Đang đăng nhập...' : 'Đăng nhập'}</button>
        <div className="auth-links">
          <Link to="/forgot-password">Quên mật khẩu?</Link>
          <span>Chưa có tài khoản? <Link to="/register">Đăng ký</Link></span>
        </div>
        <div className="demo-accounts">
          <span className="muted small">Tài khoản mẫu:</span>
          {DEMO_ACCOUNTS.map(a => (
            <button type="button" key={a.email} className="chip" onClick={() => { setEmail(a.email); setPassword(a.password); }}>
              {a.label}
            </button>
          ))}
        </div>
      </form>
    </AuthShell>
  );
}

export function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [form, setForm] = useState<RegisterInput>({
    fullName: '', email: '', password: '', confirmPassword: '', phoneNumber: '',
    role: params.get('role') === 'Landlord' ? 'Landlord' : 'Tenant',
  });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const set = <K extends keyof RegisterInput>(k: K, v: RegisterInput[K]) => setForm(f => ({ ...f, [k]: v }));

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (form.password !== form.confirmPassword) return setError('Mật khẩu xác nhận không khớp');
    setBusy(true);
    setError('');
    try {
      await register(form);
      toast('Đăng ký thành công!');
      navigate(form.role === 'Landlord' ? '/dang-tin' : '/search');
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <AuthShell>
      <form className="card auth-card" onSubmit={submit}>
        <h2>Tạo tài khoản</h2>
        {error && <div className="alert alert-error">{error}</div>}
        <div className="role-picker">
          {(['Tenant', 'Landlord'] as const).map(r => (
            <button type="button" key={r} className={`role-option ${form.role === r ? 'active' : ''}`} onClick={() => set('role', r)}>
              <span className="role-icon"><FontAwesomeIcon icon={r === 'Tenant' ? faKey : faHouseUser} /></span>
              <strong>{r === 'Tenant' ? 'Tôi muốn thuê nhà' : 'Tôi là chủ nhà'}</strong>
            </button>
          ))}
        </div>
        <label className="field"><span>Họ tên</span>
          <input required maxLength={100} value={form.fullName} onChange={e => set('fullName', e.target.value)} />
        </label>
        <label className="field"><span>Email</span>
          <input type="email" required value={form.email} onChange={e => set('email', e.target.value)} />
        </label>
        <label className="field"><span>Số điện thoại</span>
          <input type="tel" value={form.phoneNumber} onChange={e => set('phoneNumber', e.target.value)} />
        </label>
        <div className="grid-2">
          <label className="field"><span>Mật khẩu</span>
            <PasswordInput required minLength={6} autoComplete="new-password" showStrength value={form.password} onChange={v => set('password', v)} />
          </label>
          <label className="field"><span>Nhập lại mật khẩu</span>
            <PasswordInput required autoComplete="new-password" value={form.confirmPassword} onChange={v => set('confirmPassword', v)} />
          </label>
        </div>
        <button className="btn btn-primary btn-block" disabled={busy}>{busy ? 'Đang tạo...' : 'Đăng ký'}</button>
        <div className="auth-links"><span>Đã có tài khoản? <Link to="/login">Đăng nhập</Link></span></div>
      </form>
    </AuthShell>
  );
}

export function ForgotPasswordPage() {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [step, setStep] = useState<'request' | 'reset'>(params.get('token') ? 'reset' : 'request');
  const [email, setEmail] = useState(params.get('email') ?? '');
  const [token, setToken] = useState(params.get('token') ?? '');
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [info, setInfo] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const requestCode = async (e: FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError('');
    try {
      const res = await requestRaw<ForgotPasswordResultDto>('/api/auth/forgot-password', { method: 'POST', body: { email } });
      // Môi trường Development: backend trả luôn mã đặt lại
      if (res.data?.resetToken) setToken(res.data.resetToken);
      setInfo(res.message ?? 'Đã gửi mã đặt lại mật khẩu tới email của bạn.');
      setStep('reset');
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  const reset = async (e: FormEvent) => {
    e.preventDefault();
    if (password !== confirm) return setError('Mật khẩu xác nhận không khớp');
    setBusy(true);
    setError('');
    try {
      await api.post('/api/auth/reset-password', { email, token, newPassword: password, confirmNewPassword: confirm });
      toast('Đặt lại mật khẩu thành công, hãy đăng nhập lại.');
      navigate('/login');
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <AuthShell>
      {step === 'request' ? (
        <form className="card auth-card" onSubmit={requestCode}>
          <h2>Quên mật khẩu</h2>
          <p className="muted">Nhập email đã đăng ký, chúng tôi sẽ gửi mã đặt lại mật khẩu.</p>
          {error && <div className="alert alert-error">{error}</div>}
          <label className="field"><span>Email</span>
            <input type="email" required value={email} onChange={e => setEmail(e.target.value)} autoFocus />
          </label>
          <button className="btn btn-primary btn-block" disabled={busy}>Gửi mã</button>
          <div className="auth-links">
            <Link to="/login"><FontAwesomeIcon icon={faArrowLeft} /> Quay lại đăng nhập</Link>
            <button type="button" className="link-btn" onClick={() => setStep('reset')}>Tôi đã có mã</button>
          </div>
        </form>
      ) : (
        <form className="card auth-card" onSubmit={reset}>
          <h2>Đặt lại mật khẩu</h2>
          {info && <div className="alert alert-info">{info}</div>}
          {error && <div className="alert alert-error">{error}</div>}
          <label className="field"><span>Email</span>
            <input type="email" required value={email} onChange={e => setEmail(e.target.value)} />
          </label>
          <label className="field"><span>Mã đặt lại</span>
            <input required value={token} onChange={e => setToken(e.target.value)} />
          </label>
          <div className="grid-2">
            <label className="field"><span>Mật khẩu mới</span>
              <PasswordInput required minLength={6} autoComplete="new-password" showStrength value={password} onChange={setPassword} />
            </label>
            <label className="field"><span>Nhập lại</span>
              <PasswordInput required autoComplete="new-password" value={confirm} onChange={setConfirm} />
            </label>
          </div>
          <button className="btn btn-primary btn-block" disabled={busy}>Đặt lại mật khẩu</button>
          <div className="auth-links">
            <button type="button" className="link-btn" onClick={() => setStep('request')}><FontAwesomeIcon icon={faArrowLeft} /> Gửi lại mã</button>
          </div>
        </form>
      )}
    </AuthShell>
  );
}

const PERKS = [
  'Phòng trọ, căn hộ, nhà nguyên căn đều được kiểm duyệt trước khi đăng',
  'Đặt lịch xem nhà và gửi yêu cầu thuê chỉ vài cú nhấp',
  'Nhắn tin trực tiếp với chủ nhà, không qua trung gian',
  'Hóa đơn điện nước rõ ràng, thanh toán và lưu lịch sử trực tuyến',
];

/** Khung trang tài khoản: form bên trái, lợi ích bên phải */
function AuthShell({ children }: { children: ReactNode }) {
  return (
    <div className="auth-page">
      <div className="auth-form-col">{children}</div>
      <div className="auth-side">
        <div className="auth-side-inner">
          <Link to="/" className="brand light">
            <span className="brand-mark"><FontAwesomeIcon icon={faHouseChimney} /></span>
            <span>Tổ<b>Ấm</b></span>
          </Link>
          <h2>Thuê nhà dễ dàng,<br />ở là ưng.</h2>
          <ul>
            {PERKS.map(p => <li key={p}><FontAwesomeIcon icon={faCircleCheck} /> {p}</li>)}
          </ul>
        </div>
      </div>
    </div>
  );
}

import { useEffect, useRef, useState, type KeyboardEvent as ReactKeyboardEvent, type ReactNode } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCheck, faInbox, faStar, faChevronLeft, faChevronRight, faXmark, faCloudArrowUp, faEye, faEyeSlash } from '@fortawesome/free-solid-svg-icons';
import type { IconDefinition } from '@fortawesome/fontawesome-svg-core';
import { Link, Navigate, useLocation } from 'react-router-dom';
import { api, errorMessage } from '../api/client';
import { statusTone } from '../api/labels';
import type { PagedResult, UploadResultDto, UserRole } from '../api/types';
import { useAuth } from '../auth/AuthContext';

export function Badge({ status, label }: { status: string; label: string }) {
  return <span className={`badge badge-${statusTone(status)}`}>{label}</span>;
}

export function Loading({ text = 'Đang tải...' }: { text?: string }) {
  return <div className="loading"><span className="spinner" /> {text}</div>;
}

export function ErrorBox({ error, onRetry }: { error: string; onRetry?: () => void }) {
  return (
    <div className="alert alert-error">
      {error}
      {onRetry && <button className="btn btn-sm btn-ghost" onClick={onRetry}>Thử lại</button>}
    </div>
  );
}

export function Empty({ text, children }: { text: string; children?: ReactNode }) {
  return (
    <div className="empty">
      <div className="empty-icon"><FontAwesomeIcon icon={faInbox} /></div>
      <p>{text}</p>
      {children}
    </div>
  );
}

export function Stars({ value, size = 14 }: { value: number; size?: number }) {
  const full = Math.round(value);
  return (
    <span className="stars" style={{ fontSize: size }} title={`${value.toFixed(1)}/5`}>
      {[1, 2, 3, 4, 5].map(n => <FontAwesomeIcon key={n} icon={faStar} className={n <= full ? '' : 'stars-off'} />)}
    </span>
  );
}

export function StarInput({ value, onChange }: { value: number; onChange: (v: number) => void }) {
  return (
    <span className="stars star-input">
      {[1, 2, 3, 4, 5].map(n => (
        <button type="button" key={n} className={n <= value ? '' : 'stars-off'} onClick={() => onChange(n)}><FontAwesomeIcon icon={faStar} /></button>
      ))}
    </span>
  );
}

export function Pagination<T>({ data, onPage }: { data: PagedResult<T> | null; onPage: (p: number) => void }) {
  if (!data || data.totalPages <= 1) return null;
  const pages: number[] = [];
  const from = Math.max(1, data.page - 2);
  const to = Math.min(data.totalPages, data.page + 2);
  for (let i = from; i <= to; i++) pages.push(i);
  return (
    <div className="pagination">
      <button className="btn btn-sm btn-ghost" disabled={!data.hasPreviousPage} onClick={() => onPage(data.page - 1)}><FontAwesomeIcon icon={faChevronLeft} /> Trước</button>
      {from > 1 && <span className="muted">…</span>}
      {pages.map(p => (
        <button key={p} className={`btn btn-sm ${p === data.page ? 'btn-primary' : 'btn-ghost'}`} onClick={() => onPage(p)}>{p}</button>
      ))}
      {to < data.totalPages && <span className="muted">…</span>}
      <button className="btn btn-sm btn-ghost" disabled={!data.hasNextPage} onClick={() => onPage(data.page + 1)}>Sau <FontAwesomeIcon icon={faChevronRight} /></button>
      <span className="muted small">{data.totalCount} kết quả</span>
    </div>
  );
}

export function Modal({ title, open, onClose, children, width = 520 }: {
  title: string; open: boolean; onClose: () => void; children: ReactNode; width?: number;
}) {
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose();
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [open, onClose]);
  if (!open) return null;
  return (
    <div className="modal-backdrop" onMouseDown={e => e.target === e.currentTarget && onClose()}>
      <div className="modal" style={{ maxWidth: width }}>
        <div className="modal-header">
          <h3>{title}</h3>
          <button className="icon-btn" onClick={onClose} aria-label="Đóng"><FontAwesomeIcon icon={faXmark} /></button>
        </div>
        <div className="modal-body">{children}</div>
      </div>
    </div>
  );
}

/** Hộp thoại nhập lý do / ghi chú rồi xác nhận */
export function PromptModal({ open, title, label, required, confirmText = 'Xác nhận', danger, onClose, onSubmit }: {
  open: boolean; title: string; label: string; required?: boolean; confirmText?: string; danger?: boolean;
  onClose: () => void; onSubmit: (value: string) => Promise<unknown> | void;
}) {
  const [value, setValue] = useState('');
  const [busy, setBusy] = useState(false);
  useEffect(() => { if (open) setValue(''); }, [open]);
  return (
    <Modal open={open} title={title} onClose={onClose}>
      <form onSubmit={async e => {
        e.preventDefault();
        setBusy(true);
        try { await onSubmit(value.trim()); } finally { setBusy(false); }
      }}>
        <label className="field">
          <span>{label}</span>
          <textarea rows={3} value={value} required={required} onChange={e => setValue(e.target.value)} autoFocus />
        </label>
        <div className="form-actions">
          <button type="button" className="btn btn-ghost" onClick={onClose}>Hủy</button>
          <button className={`btn ${danger ? 'btn-danger' : 'btn-primary'}`} disabled={busy}>{confirmText}</button>
        </div>
      </form>
    </Modal>
  );
}

export function Toasts() {
  const [items, setItems] = useState<{ id: number; type: string; text: string }[]>([]);
  const nextId = useRef(0);
  useEffect(() => {
    const handler = (e: Event) => {
      const { type, text } = (e as CustomEvent).detail;
      const id = ++nextId.current;
      setItems(list => [...list, { id, type, text }]);
      setTimeout(() => setItems(list => list.filter(t => t.id !== id)), 4000);
    };
    window.addEventListener('toast', handler);
    return () => window.removeEventListener('toast', handler);
  }, []);
  return (
    <div className="toasts">
      {items.map(t => (
        <div key={t.id} className={`toast toast-${t.type}`} onClick={() => setItems(l => l.filter(x => x.id !== t.id))}>
          {t.text}
        </div>
      ))}
    </div>
  );
}

export function RequireAuth({ roles, children }: { roles?: UserRole[]; children: ReactNode }) {
  const { user, loading } = useAuth();
  const location = useLocation();
  if (loading) return <Loading />;
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />;
  if (roles && !roles.includes(user.role)) {
    return <div className="container"><ErrorBox error="Bạn không có quyền truy cập trang này." /></div>;
  }
  return <>{children}</>;
}

export function Avatar({ name, url, size = 36 }: { name: string; url?: string | null; size?: number }) {
  const [broken, setBroken] = useState(false);
  if (url && !broken) return <img className="avatar" src={url} alt={name} style={{ width: size, height: size }} onError={() => setBroken(true)} />;
  const initials = name.split(' ').filter(Boolean).slice(-2).map(w => w[0]).join('').toUpperCase();
  return <span className="avatar avatar-text" style={{ width: size, height: size, fontSize: size * 0.4 }}>{initials}</span>;
}

const MAX_IMAGE_BYTES = 5 * 1024 * 1024;
const IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];
const toastError = (text: string) => window.dispatchEvent(new CustomEvent('toast', { detail: { type: 'error', text } }));

/** Kiểm tra ảnh trước khi gửi (giống quy tắc của server) → báo lỗi ngay, không tốn thời gian upload */
function validateImage(file: File): string | null {
  if (!IMAGE_TYPES.includes(file.type) && !/\.(jpe?g|png|webp|gif)$/i.test(file.name))
    return 'chỉ chấp nhận ảnh .jpg, .png, .webp, .gif';
  if (file.size > MAX_IMAGE_BYTES)
    return `ảnh ${(file.size / 1024 / 1024).toFixed(1).replace('.', ',')}MB vượt quá giới hạn 5MB`;
  return null;
}

/** Upload một hoặc nhiều ảnh lên /api/uploads/images, trả về URL */
export function ImageUploadButton({ onUploaded, multiple = true, label = 'Tải ảnh lên' }: {
  onUploaded: (urls: string[]) => void; multiple?: boolean; label?: string;
}) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const handle = async (files: FileList | null) => {
    if (!files?.length) return;
    setBusy(true);
    const urls: string[] = [];
    for (const file of Array.from(files)) {
      const invalid = validateImage(file);
      if (invalid) { toastError(`${file.name}: ${invalid}`); continue; }
      const form = new FormData();
      form.append('file', file);
      try {
        const res = await api.post<UploadResultDto>('/api/uploads/images', form);
        urls.push(res.url);
      } catch (err) {
        toastError(`${file.name}: ${errorMessage(err)}`);
      }
    }
    setBusy(false);
    if (inputRef.current) inputRef.current.value = '';
    if (urls.length) onUploaded(urls);
  };
  return (
    <>
      <input ref={inputRef} type="file" accept="image/jpeg,image/png,image/webp,image/gif" multiple={multiple} hidden
        onChange={e => handle(e.target.files)} />
      <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => inputRef.current?.click()}>
        <FontAwesomeIcon icon={faCloudArrowUp} /> {busy ? 'Đang tải...' : label}
      </button>
    </>
  );
}

/** Ô thống kê: icon màu + số lớn, bấm được nếu có `to` */
export function StatCard({ label, value, hint, tone = 'orange', icon, to }: {
  label: string; value: ReactNode; hint?: ReactNode; tone?: 'orange' | 'blue' | 'green' | 'purple' | 'gold' | 'red' | 'warn';
  icon?: IconDefinition; to?: string;
}) {
  const body = (
    <>
      {icon && <span className="stat-icon"><FontAwesomeIcon icon={icon} /></span>}
      <div className="stat-text">
        <div className="stat-label">{label}</div>
        <div className="stat-value">{value}</div>
        {hint && <div className="stat-hint">{hint}</div>}
      </div>
    </>
  );
  const cls = `stat-card tone-${tone === 'warn' ? 'gold' : tone}`;
  return to ? <Link to={to} className={`${cls} clickable`}>{body}</Link> : <div className={cls}>{body}</div>;
}

export function Tabs<T extends string>({ value, options, onChange }: {
  value: T; options: { value: T; label: string }[]; onChange: (v: T) => void;
}) {
  return (
    <div className="tabs">
      {options.map(o => (
        <button key={o.value} className={`tab ${o.value === value ? 'active' : ''}`} onClick={() => onChange(o.value)}>
          {o.label}
        </button>
      ))}
    </div>
  );
}

export function PageHeader({ title, subtitle, actions, icon }: {
  title: string; subtitle?: ReactNode; actions?: ReactNode; icon?: IconDefinition;
}) {
  return (
    <div className="page-header">
      <div className="page-title">
        {icon && <span className="page-icon"><FontAwesomeIcon icon={icon} /></span>}
        <div>
          <h1>{title}</h1>
          {subtitle && <p className="muted">{subtitle}</p>}
        </div>
      </div>
      {actions && <div className="page-actions">{actions}</div>}
    </div>
  );
}

/** Thanh tiến trình các bước (hợp đồng, lịch hẹn...) */
export function Stepper({ steps, current, failed }: { steps: string[]; current: number; failed?: string }) {
  return (
    <ol className={`stepper ${failed ? 'is-failed' : ''}`}>
      {steps.map((s, i) => (
        <li key={s} className={i < current ? 'done' : i === current ? 'current' : ''}>
          <span className="step-dot">{i < current ? <FontAwesomeIcon icon={faCheck} /> : i + 1}</span>
          <span className="step-label">{i === current && failed ? failed : s}</span>
        </li>
      ))}
    </ol>
  );
}

/** Thanh phần trăm (đã thanh toán / tổng) */
export function Progress({ value, max, tone = 'green' }: { value: number; max: number; tone?: 'green' | 'orange' }) {
  const pct = max > 0 ? Math.min(100, (value / max) * 100) : 0;
  return <div className={`progress progress-${tone}`}><div style={{ width: `${pct}%` }} /></div>;
}

/** Chấm điểm độ mạnh mật khẩu từ 0 (rỗng) đến 4 (rất mạnh) */
export function passwordScore(value: string): number {
  if (!value) return 0;
  let score = 0;
  if (value.length >= 6) score++;
  if (value.length >= 10) score++;
  if (/[a-z]/.test(value) && /[A-Z]/.test(value)) score++;
  if (/\d/.test(value) && /[^A-Za-z0-9]/.test(value)) score++;
  return Math.max(1, Math.min(4, score));
}

const STRENGTH_LABELS = ['', 'Yếu', 'Trung bình', 'Khá mạnh', 'Rất mạnh'];

/**
 * Ô nhập mật khẩu có nút ẩn/hiện, cảnh báo khi bật Caps Lock
 * và (tùy chọn) thanh đo độ mạnh mật khẩu.
 */
export function PasswordInput({ value, onChange, showStrength, autoComplete = 'current-password', ...rest }: {
  value: string;
  onChange: (value: string) => void;
  showStrength?: boolean;
  autoComplete?: string;
  required?: boolean;
  minLength?: number;
  autoFocus?: boolean;
  placeholder?: string;
  id?: string;
}) {
  const [visible, setVisible] = useState(false);
  const [capsOn, setCapsOn] = useState(false);
  const score = passwordScore(value);
  const checkCaps = (e: ReactKeyboardEvent<HTMLInputElement>) => setCapsOn(e.getModifierState?.('CapsLock') ?? false);

  return (
    <>
      <span className="password-field">
        <input
          {...rest}
          type={visible ? 'text' : 'password'}
          value={value}
          autoComplete={autoComplete}
          onChange={e => onChange(e.target.value)}
          onKeyDown={checkCaps}
          onKeyUp={checkCaps}
          onBlur={() => setCapsOn(false)}
        />
        <button
          type="button"
          className="password-toggle"
          onClick={() => setVisible(v => !v)}
          aria-label={visible ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
          aria-pressed={visible}
          title={visible ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
        >
          <FontAwesomeIcon icon={visible ? faEyeSlash : faEye} fixedWidth />
        </button>
      </span>
      {capsOn && <small className="password-hint warn">Caps Lock đang bật</small>}
      {showStrength && value && (
        <span className={`password-meter s${score}`} aria-live="polite">
          <span className="password-bars">{[1, 2, 3, 4].map(i => <i key={i} className={i <= score ? 'on' : ''} />)}</span>
          <small>{STRENGTH_LABELS[score]}</small>
        </span>
      )}
    </>
  );
}

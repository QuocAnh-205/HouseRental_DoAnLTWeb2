import { useState, type FormEvent } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCheck, faFlag, faHouseCircleCheck, faUsers, faXmark } from '@fortawesome/free-solid-svg-icons';
import { Link, useSearchParams } from 'react-router-dom';
import { api } from '../../api/client';
import {
  fmtDate, fmtDateTime, money, propertyStatusLabel, propertyTypeLabel, reportReasonLabel, reportStatusLabel, roleLabel,
} from '../../api/labels';
import type {
  PagedResult, PropertyStatus, PropertySummaryDto, ReportDto, ReportStatus, UserDto, UserRole,
} from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { Avatar, Badge, Empty, ErrorBox, Loading, Modal, PageHeader, Pagination, PromptModal, Tabs } from '../../components/ui';
import { toast, useAction, useApi, useHighlight } from '../../hooks';

// ===================== Người dùng =====================

export function AdminUsersPage() {
  const { user: me } = useAuth();
  const { busy, run } = useAction();
  const [keyword, setKeyword] = useState('');
  const [query, setQuery] = useState<{ keyword: string; role: UserRole | ''; isActive: string; page: number }>({
    keyword: '', role: '', isActive: '', page: 1,
  });
  const { data, loading, error, reload } = useApi(
    () => api.get<PagedResult<UserDto>>('/api/users', { ...query, pageSize: 15 }), [JSON.stringify(query)],
  );

  const toggleActive = async (u: UserDto) => {
    if (!confirm(`${u.isActive ? 'Khóa' : 'Mở khóa'} tài khoản ${u.email}?`)) return;
    const res = await run(() => api.patch(`/api/users/${u.id}/status`, { isActive: !u.isActive }));
    if (res) { toast('Đã cập nhật trạng thái'); reload(); }
  };

  const changeRole = async (u: UserDto, role: UserRole) => {
    if (role === u.role || !confirm(`Đổi vai trò ${u.email} thành ${roleLabel[role]}?`)) return;
    const res = await run(() => api.patch(`/api/users/${u.id}/role`, { role }));
    if (res) { toast('Đã đổi vai trò'); reload(); }
  };

  return (
    <div className="container">
      <PageHeader icon={faUsers} title="Quản lý người dùng" subtitle="Khóa / mở khóa tài khoản và phân quyền vai trò" />
      <form className="filter-bar" onSubmit={(e: FormEvent) => { e.preventDefault(); setQuery(q => ({ ...q, keyword, page: 1 })); }}>
        <input placeholder="Tìm theo tên, email, SĐT..." value={keyword} onChange={e => setKeyword(e.target.value)} />
        <select value={query.role} onChange={e => setQuery(q => ({ ...q, role: e.target.value as UserRole | '', page: 1 }))}>
          <option value="">Mọi vai trò</option>
          {Object.entries(roleLabel).map(([v, l]) => <option key={v} value={v}>{l}</option>)}
        </select>
        <select value={query.isActive} onChange={e => setQuery(q => ({ ...q, isActive: e.target.value, page: 1 }))}>
          <option value="">Mọi trạng thái</option>
          <option value="true">Đang hoạt động</option>
          <option value="false">Bị khóa</option>
        </select>
        <button className="btn btn-primary">Tìm</button>
      </form>
      {error && <ErrorBox error={error} onRetry={reload} />}
      {loading && !data ? <Loading /> : data?.items.length === 0 ? <Empty text="Không có người dùng phù hợp." /> : (
        <div className="table-wrap">
          <table className="table">
            <thead><tr><th>Người dùng</th><th>SĐT</th><th>Vai trò</th><th>Trạng thái</th><th>Đăng nhập gần nhất</th><th>Ngày tạo</th><th></th></tr></thead>
            <tbody>
              {data?.items.map(u => (
                <tr key={u.id}>
                  <td>
                    <div className="user-cell">
                      <Avatar name={u.fullName} url={u.avatarUrl} size={32} />
                      <div><Link to={`/users/${u.id}`}>{u.fullName}</Link><div className="muted small">{u.email}</div></div>
                    </div>
                  </td>
                  <td>{u.phoneNumber ?? '—'}</td>
                  <td>
                    <select value={u.role} disabled={busy || u.id === me!.id} onChange={e => changeRole(u, e.target.value as UserRole)}>
                      {Object.entries(roleLabel).map(([v, l]) => <option key={v} value={v}>{l}</option>)}
                    </select>
                  </td>
                  <td><Badge status={u.isActive ? 'Available' : 'Rejected'} label={u.isActive ? 'Hoạt động' : 'Bị khóa'} /></td>
                  <td className="small">{fmtDateTime(u.lastLoginAt)}</td>
                  <td className="small">{fmtDate(u.createdAt)}</td>
                  <td>
                    {u.id !== me!.id && (
                      <button className={`btn btn-sm ${u.isActive ? 'btn-ghost danger' : 'btn-primary'}`} disabled={busy} onClick={() => toggleActive(u)}>
                        {u.isActive ? 'Khóa' : 'Mở khóa'}
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <Pagination data={data} onPage={p => setQuery(q => ({ ...q, page: p }))} />
    </div>
  );
}

// ===================== Duyệt tin =====================

export function AdminPropertiesPage() {
  const [params, setParams] = useSearchParams();
  // Mặc định hiện tin chờ duyệt; "all" = mọi trạng thái
  const rawStatus = params.get('status') ?? 'PendingApproval';
  const status = rawStatus === 'all' ? undefined : rawStatus;
  const page = Number(params.get('page') ?? 1);
  const [keyword, setKeyword] = useState(params.get('keyword') ?? '');
  const { busy, run } = useAction();
  const [rejecting, setRejecting] = useState<PropertySummaryDto | null>(null);
  const { data, loading, error, reload } = useApi(
    () => api.get<PagedResult<PropertySummaryDto>>('/api/properties/manage', {
      status, keyword: params.get('keyword') ?? undefined, page, pageSize: 15,
    }),
    [params.toString()],
  );
  const highlight = useHighlight(data);

  const update = (patch: Record<string, string>) => {
    const next = new URLSearchParams(params);
    Object.entries(patch).forEach(([k, v]) => (v ? next.set(k, v) : next.delete(k)));
    if (!('page' in patch)) next.delete('page');
    setParams(next);
  };

  const setStatus = async (p: PropertySummaryDto, s: PropertyStatus, reason?: string) => {
    const res = await run(() => api.patch(`/api/properties/${p.id}/status`, { status: s, reason }));
    if (res) { toast(`Đã chuyển "${p.title}" sang ${propertyStatusLabel[s]}`); setRejecting(null); reload(); }
  };

  const remove = async (p: PropertySummaryDto) => {
    if (!confirm(`Xóa vĩnh viễn tin "${p.title}"?`)) return;
    const ok = await run(() => api.del(`/api/properties/${p.id}`));
    if (ok !== undefined) { toast('Đã xóa tin'); reload(); }
  };

  const tabs = [
    ...(Object.keys(propertyStatusLabel) as PropertyStatus[]).map(s => ({ value: s, label: propertyStatusLabel[s] })),
    { value: 'all', label: 'Tất cả' },
  ];

  return (
    <div className="container">
      <PageHeader icon={faHouseCircleCheck} title="Quản lý tin đăng" subtitle="Duyệt tin mới, ẩn hoặc gỡ tin vi phạm" />
      <form className="filter-bar" onSubmit={e => { e.preventDefault(); update({ keyword }); }}>
        <input placeholder="Tìm theo tiêu đề, địa chỉ..." value={keyword} onChange={e => setKeyword(e.target.value)} />
        <button className="btn btn-primary">Tìm</button>
      </form>
      <Tabs<string> value={rawStatus} options={tabs} onChange={v => update({ status: v })} />
      {error && <ErrorBox error={error} onRetry={reload} />}
      {loading && !data ? <Loading /> : data?.items.length === 0 ? <Empty text="Không có tin đăng nào." /> : (
        <div className="table-wrap">
          <table className="table">
            <thead><tr><th>Tin đăng</th><th>Chủ nhà</th><th>Giá</th><th>Ngày đăng</th><th>Trạng thái</th><th></th></tr></thead>
            <tbody>
              {data?.items.map(p => (
                <tr key={p.id} data-highlight={p.id} className={highlight === p.id ? 'is-highlight' : ''}>
                  <td>
                    <div className="user-cell">
                      {p.thumbnailUrl && <img className="table-thumb" src={p.thumbnailUrl} alt="" />}
                      <div>
                        <Link to={`/properties/${p.id}`}>{p.title}</Link>
                        <div className="muted small">{propertyTypeLabel[p.type]} · {p.area} m² · {p.district}, {p.city}</div>
                      </div>
                    </div>
                  </td>
                  <td><Link to={`/users/${p.landlordId}`}>{p.landlordName}</Link></td>
                  <td>{money(p.pricePerMonth)}</td>
                  <td className="small">{fmtDate(p.createdAt)}</td>
                  <td><Badge status={p.status} label={propertyStatusLabel[p.status]} /></td>
                  <td className="nowrap">
                    {(p.status === 'PendingApproval' || p.status === 'Rejected' || p.status === 'Hidden') && (
                      <button className="btn btn-sm btn-primary" disabled={busy} onClick={() => setStatus(p, 'Available')}>
                        {p.status === 'Hidden' ? 'Hiện' : 'Duyệt'}
                      </button>
                    )}
                    {p.status === 'PendingApproval' && (
                      <button className="btn btn-sm btn-ghost danger" disabled={busy} onClick={() => setRejecting(p)}>Từ chối</button>
                    )}
                    {p.status === 'Available' && (
                      <button className="btn btn-sm btn-ghost" disabled={busy} onClick={() => setStatus(p, 'Hidden')}>Ẩn</button>
                    )}
                    <button className="btn btn-sm btn-ghost danger" disabled={busy} onClick={() => remove(p)}>Xóa</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <Pagination data={data} onPage={p => update({ page: String(p) })} />
      {rejecting && (
        <PromptModal open title={`Từ chối tin "${rejecting.title}"`} label="Lý do từ chối (gửi cho chủ nhà)" required danger confirmText="Từ chối"
          onClose={() => setRejecting(null)} onSubmit={reason => setStatus(rejecting, 'Rejected', reason)} />
      )}
    </div>
  );
}

// ===================== Báo cáo vi phạm =====================

export function AdminReportsPage() {
  const [params] = useSearchParams();
  const [status, setStatus] = useState<ReportStatus | ''>('Pending');
  const [page, setPage] = useState(1);
  const propertyId = params.get('propertyId') ?? undefined;
  const [handling, setHandling] = useState<ReportDto | null>(null);
  const { data, loading, error, reload } = useApi(
    () => api.get<PagedResult<ReportDto>>('/api/reports', { status, propertyId, page, pageSize: 15 }), [status, page, propertyId],
  );

  const tabs = [
    ...(Object.keys(reportStatusLabel) as ReportStatus[]).map(s => ({ value: s, label: reportStatusLabel[s] })),
    { value: '' as const, label: 'Tất cả' },
  ];

  return (
    <div className="container">
      <PageHeader icon={faFlag} title="Báo cáo vi phạm" subtitle={propertyId ? `Lọc theo tin #${propertyId}` : undefined} />
      <Tabs value={status} options={tabs} onChange={v => { setStatus(v); setPage(1); }} />
      {error && <ErrorBox error={error} onRetry={reload} />}
      {loading && !data ? <Loading /> : data?.items.length === 0 ? <Empty text="Không có báo cáo." /> : (
        <div className="table-wrap">
          <table className="table">
            <thead><tr><th>Tin đăng</th><th>Người báo cáo</th><th>Lý do</th><th>Ngày gửi</th><th>Trạng thái</th><th></th></tr></thead>
            <tbody>
              {data?.items.map(r => (
                <tr key={r.id}>
                  <td><Link to={`/properties/${r.propertyId}`}>{r.propertyTitle}</Link></td>
                  <td>{r.reporterName}</td>
                  <td>{reportReasonLabel[r.reason]}{r.description && <div className="muted small">{r.description}</div>}</td>
                  <td className="small">{fmtDateTime(r.createdAt)}</td>
                  <td>
                    <Badge status={r.status} label={reportStatusLabel[r.status]} />
                    {r.adminNote && <div className="muted small">{r.adminNote}</div>}
                  </td>
                  <td>{r.status === 'Pending' && <button className="btn btn-sm btn-primary" onClick={() => setHandling(r)}>Xử lý</button>}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <Pagination data={data} onPage={setPage} />
      {handling && <HandleReportModal report={handling} onClose={() => setHandling(null)} onDone={() => { setHandling(null); reload(); }} />}
    </div>
  );
}

function HandleReportModal({ report, onClose, onDone }: { report: ReportDto; onClose: () => void; onDone: () => void }) {
  const { busy, run } = useAction();
  const [status, setStatus] = useState<'Resolved' | 'Dismissed'>('Resolved');
  const [adminNote, setNote] = useState('');
  const [hideProperty, setHide] = useState(true);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    const res = await run(() => api.patch(`/api/reports/${report.id}`, {
      status, adminNote: adminNote || undefined, hideProperty: status === 'Resolved' && hideProperty,
    }));
    if (res) { toast('Đã xử lý báo cáo'); onDone(); }
  };

  return (
    <Modal open title="Xử lý báo cáo" onClose={onClose}>
      <form onSubmit={submit}>
        <p><strong>{report.propertyTitle}</strong><br /><span className="muted">{reportReasonLabel[report.reason]} — {report.description ?? 'không có mô tả'}</span></p>
        <div className="role-picker">
          <button type="button" className={`role-option ${status === 'Resolved' ? 'active' : ''}`} onClick={() => setStatus('Resolved')}>
            <strong><FontAwesomeIcon icon={faCheck} /> Chấp nhận</strong><span className="small muted">Báo cáo đúng</span>
          </button>
          <button type="button" className={`role-option ${status === 'Dismissed' ? 'active' : ''}`} onClick={() => setStatus('Dismissed')}>
            <strong><FontAwesomeIcon icon={faXmark} /> Bác bỏ</strong><span className="small muted">Báo cáo không đúng</span>
          </button>
        </div>
        {status === 'Resolved' && (
          <label className="check"><input type="checkbox" checked={hideProperty} onChange={e => setHide(e.target.checked)} /> Ẩn tin đăng vi phạm</label>
        )}
        <label className="field"><span>Ghi chú của quản trị viên</span>
          <textarea rows={3} maxLength={500} value={adminNote} onChange={e => setNote(e.target.value)} />
        </label>
        <div className="form-actions">
          <button type="button" className="btn btn-ghost" onClick={onClose}>Hủy</button>
          <button className="btn btn-primary" disabled={busy}>Lưu</button>
        </div>
      </form>
    </Modal>
  );
}

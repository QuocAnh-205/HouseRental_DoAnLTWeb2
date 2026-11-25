import { useState, type ReactNode } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faArrowRight, faBolt, faCalendarCheck, faCalendarDays, faChevronRight, faComment, faCreditCard, faDroplet,
  faFileInvoiceDollar, faHouse, faLocationDot, faPrint, faUser,
} from '@fortawesome/free-solid-svg-icons';
import { faClock } from '@fortawesome/free-regular-svg-icons';
import { Link, useParams } from 'react-router-dom';
import { api } from '../api/client';
import {
  appointmentStatusLabel, ensureUtc, fmtDate, fmtDateTime, invoiceStatusLabel, money, paymentStatusLabel, paymentTypeLabel,
} from '../api/labels';
import type {
  AppointmentDto, AppointmentStatus, InvoiceDto, InvoiceStatus, PagedResult, PaymentDto, PaymentStatus, PaymentType,
} from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { Badge, Empty, ErrorBox, Loading, PageHeader, Pagination, Progress, PromptModal, Tabs } from '../components/ui';
import { toast, useAction, useApi, useHighlight } from '../hooks';
import { PaymentsTable } from './BookingsPages';

// ===================== Hóa đơn =====================

export function InvoicesPage() {
  const { user } = useAuth();
  const isTenant = user!.role === 'Tenant';
  const [status, setStatus] = useState<InvoiceStatus | ''>('');
  const [year, setYear] = useState('');
  const [page, setPage] = useState(1);
  const { data, loading, error, reload } = useApi(
    () => api.get<PagedResult<InvoiceDto>>(isTenant ? '/api/invoices/mine' : '/api/invoices/received', { status, year, page, pageSize: 10 }),
    [status, year, page],
  );

  const tabs = [{ value: '' as const, label: 'Tất cả' },
    ...(Object.keys(invoiceStatusLabel) as InvoiceStatus[]).map(s => ({ value: s, label: invoiceStatusLabel[s] }))];

  return (
    <div className="container">
      <PageHeader icon={faFileInvoiceDollar} title={isTenant ? 'Hóa đơn của tôi' : 'Hóa đơn đã lập'}
        subtitle={isTenant ? 'Tiền thuê, điện, nước hằng tháng của bạn' : 'Hóa đơn hằng tháng bạn đã lập cho người thuê'}
        actions={
          <select value={year} onChange={e => { setYear(e.target.value); setPage(1); }}>
            <option value="">Mọi năm</option>
            {[0, 1, 2].map(i => new Date().getFullYear() - i).map(y => <option key={y} value={y}>{y}</option>)}
          </select>
        } />
      <Tabs value={status} options={tabs} onChange={v => { setStatus(v); setPage(1); }} />
      {error && <ErrorBox error={error} onRetry={reload} />}
      {loading && !data ? <Loading /> : data?.items.length === 0 ? <Empty text="Chưa có hóa đơn." /> : (
        <div className="record-list">
          {data?.items.map(inv => (
            <Link key={inv.id} to={`/invoices/${inv.id}`} className={`record-card status-${inv.status.toLowerCase()}`}>
              <span className="record-icon period"><small>Tháng</small><strong>{inv.month}</strong><small>{inv.year}</small></span>
              <div className="record-main">
                <div className="record-title">
                  <strong>{inv.propertyTitle}</strong>
                  <Badge status={inv.status} label={invoiceStatusLabel[inv.status]} />
                </div>
                <div className="record-meta">
                  {!isTenant && <span><FontAwesomeIcon icon={faUser} /> {inv.tenantName}</span>}
                  <span><FontAwesomeIcon icon={faCalendarDays} /> Hạn {fmtDate(inv.dueDate)}</span>
                  <span><FontAwesomeIcon icon={faBolt} /> {inv.electricityNewIndex - inv.electricityOldIndex} kWh</span>
                  <span><FontAwesomeIcon icon={faDroplet} /> {inv.waterNewIndex - inv.waterOldIndex} m³</span>
                </div>
                <div className="record-progress">
                  <Progress value={inv.paidAmount} max={inv.totalAmount} />
                  <span className="small muted">Đã trả {money(inv.paidAmount)}</span>
                </div>
              </div>
              <div className="record-amount">
                <strong>{money(inv.totalAmount)}</strong>
                {inv.remainingAmount > 0 && inv.status !== 'Cancelled'
                  ? <span className="text-red">Còn {money(inv.remainingAmount)}</span>
                  : <span className="text-green">{inv.status === 'Cancelled' ? 'Đã hủy' : 'Đã trả đủ'}</span>}
              </div>
              <FontAwesomeIcon icon={faChevronRight} className="record-arrow" />
            </Link>
          ))}
        </div>
      )}
      <Pagination data={data} onPage={setPage} />
    </div>
  );
}

export function InvoiceDetailPage() {
  const { id } = useParams();
  const { data: inv, loading, error } = useApi(() => api.get<InvoiceDto>(`/api/invoices/${id}`), [id]);
  if (loading && !inv) return <Loading />;
  if (error || !inv) return <div className="container"><ErrorBox error={error ?? 'Không tìm thấy hóa đơn'} /></div>;

  const arrow = <FontAwesomeIcon icon={faArrowRight} className="muted" />;
  const rows: [string, ReactNode, number][] = [
    ['Tiền thuê nhà', '', inv.rentAmount],
    ['Tiền điện', <>{inv.electricityOldIndex} {arrow} {inv.electricityNewIndex} = {inv.electricityNewIndex - inv.electricityOldIndex} kWh × {money(inv.electricityUnitPrice)}</>, inv.electricityAmount],
    ['Tiền nước', <>{inv.waterOldIndex} {arrow} {inv.waterNewIndex} = {inv.waterNewIndex - inv.waterOldIndex} m³ × {money(inv.waterUnitPrice)}</>, inv.waterAmount],
    ['Phí dịch vụ', '', inv.serviceFee],
    ['Phí khác', '', inv.otherFee],
  ];

  return (
    <div className="container narrow">
      <div className="breadcrumb">
        <Link to="/invoices">Hóa đơn</Link> / <Link to={`/bookings/${inv.bookingId}`}>Hợp đồng #{inv.bookingId}</Link> / <span>HĐ #{inv.id}</span>
      </div>
      <div className="card invoice">
        <div className="invoice-banner">
          <div>
            <span className="kicker light"><FontAwesomeIcon icon={faFileInvoiceDollar} /> Hóa đơn #{inv.id}</span>
            <h1>Tháng {inv.month}/{inv.year}</h1>
            <div>{inv.propertyTitle} · {inv.tenantName}</div>
          </div>
          <div className="invoice-total">
            <span>Tổng cộng</span>
            <strong>{money(inv.totalAmount)}</strong>
            <Badge status={inv.status} label={invoiceStatusLabel[inv.status]} />
          </div>
        </div>
        <div className="pad">
        <div className="invoice-paybar">
          <div className="small"><span>Đã thanh toán {money(inv.paidAmount)}</span><span>Còn lại <strong className={inv.remainingAmount > 0 ? 'text-red' : 'text-green'}>{money(inv.remainingAmount)}</strong></span></div>
          <Progress value={inv.paidAmount} max={inv.totalAmount} />
        </div>
        <table className="table invoice-table">
          <thead><tr><th>Khoản</th><th>Chi tiết</th><th className="right">Thành tiền</th></tr></thead>
          <tbody>
            {rows.map(([label, detail, amount]) => (
              <tr key={label}><td>{label}</td><td className="muted small">{detail}</td><td className="right">{money(amount)}</td></tr>
            ))}
          </tbody>
          <tfoot>
            <tr><td colSpan={2}><strong>Tổng cộng</strong></td><td className="right"><strong>{money(inv.totalAmount)}</strong></td></tr>
            <tr><td colSpan={2}>Đã thanh toán</td><td className="right text-green">{money(inv.paidAmount)}</td></tr>
            <tr><td colSpan={2}>Còn lại</td><td className="right text-red"><strong>{money(inv.remainingAmount)}</strong></td></tr>
          </tfoot>
        </table>
        <div className="muted small">Hạn thanh toán: <strong>{fmtDate(inv.dueDate)}</strong> · Lập lúc {fmtDateTime(inv.createdAt)}</div>
        {inv.note && <p className="prewrap">Ghi chú: {inv.note}</p>}
        <div className="form-actions">
          <button className="btn btn-ghost" onClick={() => window.print()}><FontAwesomeIcon icon={faPrint} /> In hóa đơn</button>
          <Link className="btn btn-primary" to={`/bookings/${inv.bookingId}`}>Đến hợp đồng để thanh toán</Link>
        </div>
        </div>
      </div>
    </div>
  );
}

// ===================== Thanh toán =====================

export function PaymentsPage() {
  const { user } = useAuth();
  const isTenant = user!.role === 'Tenant';
  const { busy, run } = useAction();
  const [status, setStatus] = useState<PaymentStatus | ''>('');
  const [type, setType] = useState<PaymentType | ''>('');
  const [page, setPage] = useState(1);
  const { data, loading, error, reload } = useApi(
    () => api.get<PagedResult<PaymentDto>>(isTenant ? '/api/payments/mine' : '/api/payments/received', { status, type, page, pageSize: 10 }),
    [status, type, page],
  );

  const update = async (p: PaymentDto, s: 'Success' | 'Failed' | 'Refunded') => {
    if (!confirm(`Cập nhật giao dịch ${p.transactionCode} thành "${paymentStatusLabel[s]}"?`)) return;
    const res = await run(() => api.patch(`/api/payments/${p.id}/status`, { status: s }));
    if (res) { toast('Đã cập nhật thanh toán'); reload(); }
  };

  const tabs = [{ value: '' as const, label: 'Tất cả' },
    ...(Object.keys(paymentStatusLabel) as PaymentStatus[]).map(s => ({ value: s, label: paymentStatusLabel[s] }))];

  return (
    <div className="container">
      <PageHeader icon={faCreditCard} title={isTenant ? 'Lịch sử thanh toán' : 'Thanh toán nhận được'}
        subtitle={isTenant ? 'Mọi khoản bạn đã thanh toán, kèm mã giao dịch' : 'Xác nhận các khoản người thuê đã chuyển cho bạn'}
        actions={
          <select value={type} onChange={e => { setType(e.target.value as PaymentType | ''); setPage(1); }}>
            <option value="">Mọi loại</option>
            {Object.entries(paymentTypeLabel).map(([v, l]) => <option key={v} value={v}>{l}</option>)}
          </select>
        } />
      <Tabs value={status} options={tabs} onChange={v => { setStatus(v); setPage(1); }} />
      {error && <ErrorBox error={error} onRetry={reload} />}
      {loading && !data ? <Loading /> : (
        <PaymentsTable items={data?.items ?? []} canManage={!isTenant} busy={busy} onUpdate={update} showProperty />
      )}
      <Pagination data={data} onPage={setPage} />
    </div>
  );
}

// ===================== Lịch hẹn xem nhà =====================

export function AppointmentsPage() {
  const { user } = useAuth();
  const isTenant = user!.role === 'Tenant';
  const { busy, run } = useAction();
  const [status, setStatus] = useState<AppointmentStatus | ''>('');
  const [page, setPage] = useState(1);
  const [prompt, setPrompt] = useState<{ a: AppointmentDto; action: 'confirm' | 'reject' | 'cancel' } | null>(null);
  const { data, loading, error, reload } = useApi(
    () => api.get<PagedResult<AppointmentDto>>(isTenant ? '/api/appointments/mine' : '/api/appointments/received', { status, page, pageSize: 10 }),
    [status, page],
  );
  const highlight = useHighlight(data);

  const act = async (a: AppointmentDto, action: string, note?: string) => {
    const res = await run(() => api.patch(`/api/appointments/${a.id}/${action}`, note ? { note } : undefined));
    if (res) { toast('Đã cập nhật lịch hẹn'); setPrompt(null); reload(); }
  };

  const tabs = [{ value: '' as const, label: 'Tất cả' },
    ...(Object.keys(appointmentStatusLabel) as AppointmentStatus[]).map(s => ({ value: s, label: appointmentStatusLabel[s] }))];

  const promptText = {
    confirm: { title: 'Xác nhận lịch hẹn', label: 'Ghi chú cho người thuê (không bắt buộc)', btn: 'Xác nhận' },
    reject: { title: 'Từ chối lịch hẹn', label: 'Lý do từ chối', btn: 'Từ chối' },
    cancel: { title: 'Hủy lịch hẹn', label: 'Lý do hủy (không bắt buộc)', btn: 'Hủy lịch' },
  };

  return (
    <div className="container">
      <PageHeader icon={faCalendarCheck} title={isTenant ? 'Lịch hẹn xem nhà của tôi' : 'Lịch hẹn xem nhà'}
        subtitle={isTenant ? 'Bạn sẽ được nhắc trước 24 giờ' : 'Khách đặt lịch xem các căn nhà của bạn'} />
      <Tabs value={status} options={tabs} onChange={v => { setStatus(v); setPage(1); }} />
      {error && <ErrorBox error={error} onRetry={reload} />}
      {loading && !data ? <Loading /> : data?.items.length === 0 ? <Empty text="Chưa có lịch hẹn nào." /> : (
        <div className="list">
          {data?.items.map(a => (
            <div key={a.id} data-highlight={a.id} className={`card pad list-item appt status-${a.status.toLowerCase()} ${highlight === a.id ? 'is-highlight' : ''}`}>
              <div className="date-box">
                <span>{new Date(ensureUtc(a.scheduledAt)).toLocaleDateString('vi-VN', { weekday: 'short' })}</span>
                <strong>{new Date(ensureUtc(a.scheduledAt)).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' })}</strong>
              </div>
              <div className="grow">
                <div className="list-title">
                  <Link to={`/properties/${a.propertyId}`}>{a.propertyTitle}</Link>{' '}
                  <Badge status={a.status} label={appointmentStatusLabel[a.status]} />
                </div>
                <div className="muted small"><FontAwesomeIcon icon={faLocationDot} /> {a.propertyAddress}</div>
                <div className="small"><FontAwesomeIcon icon={faClock} /> {fmtDateTime(a.scheduledAt)}</div>
                {!isTenant && <div className="small"><FontAwesomeIcon icon={faUser} /> {a.tenantName} {a.tenantPhone && `· ${a.tenantPhone}`}</div>}
                {a.note && <div className="small"><FontAwesomeIcon icon={faComment} /> {a.note}</div>}
                {a.landlordNote && <div className="small"><FontAwesomeIcon icon={faHouse} /> Chủ nhà: {a.landlordNote}</div>}
              </div>
              <div className="list-actions">
                {!isTenant && a.status === 'Pending' && (
                  <>
                    <button className="btn btn-sm btn-primary" disabled={busy} onClick={() => setPrompt({ a, action: 'confirm' })}>Xác nhận</button>
                    <button className="btn btn-sm btn-ghost danger" disabled={busy} onClick={() => setPrompt({ a, action: 'reject' })}>Từ chối</button>
                  </>
                )}
                {!isTenant && a.status === 'Confirmed' && (
                  <button className="btn btn-sm btn-primary" disabled={busy} onClick={() => act(a, 'complete')}>Đã dẫn xem</button>
                )}
                {(a.status === 'Pending' || a.status === 'Confirmed') && (
                  <button className="btn btn-sm btn-ghost" disabled={busy} onClick={() => setPrompt({ a, action: 'cancel' })}>Hủy</button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
      <Pagination data={data} onPage={setPage} />
      {prompt && (
        <PromptModal open title={promptText[prompt.action].title} label={promptText[prompt.action].label}
          confirmText={promptText[prompt.action].btn} danger={prompt.action !== 'confirm'}
          onClose={() => setPrompt(null)} onSubmit={note => act(prompt.a, prompt.action, note)} />
      )}
    </div>
  );
}

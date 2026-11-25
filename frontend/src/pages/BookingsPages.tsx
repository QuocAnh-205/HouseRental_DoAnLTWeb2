import { useState, type FormEvent } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faArrowRight, faCalendarDays, faCheck, faChevronRight, faFileContract, faLocationDot, faUser, faXmark, faStar, faBolt,
  faDroplet, faPlus,
} from '@fortawesome/free-solid-svg-icons';
import { Link, useParams } from 'react-router-dom';
import { api } from '../api/client';
import {
  bookingStatusLabel, fmtDate, fmtDateTime, invoiceStatusLabel, money, paymentMethodLabel, paymentStatusLabel,
  paymentTypeLabel, todayIso,
} from '../api/labels';
import type {
  BookingDto, BookingStatus, InvoiceDto, PagedResult, PaymentDto, PaymentMethod, PaymentType,
} from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { Badge, Empty, ErrorBox, Loading, Modal, PageHeader, Pagination, Progress, PromptModal, Stepper, Tabs } from '../components/ui';
import { toast, useAction, useApi } from '../hooks';

const STATUS_TABS: { value: BookingStatus | ''; label: string }[] = [
  { value: '', label: 'Tất cả' },
  ...(['Pending', 'Approved', 'Completed', 'Rejected', 'Cancelled'] as BookingStatus[])
    .map(s => ({ value: s, label: bookingStatusLabel[s] })),
];

export function BookingsPage() {
  const { user } = useAuth();
  const [status, setStatus] = useState<BookingStatus | ''>('');
  const [page, setPage] = useState(1);
  const endpoint = user!.role === 'Tenant' ? '/api/bookings/mine' : user!.role === 'Landlord' ? '/api/bookings/received' : '/api/bookings';
  const title = user!.role === 'Tenant' ? 'Yêu cầu thuê của tôi' : user!.role === 'Landlord' ? 'Yêu cầu thuê nhận được' : 'Tất cả hợp đồng';

  const { data, loading, error, reload } = useApi(
    () => api.get<PagedResult<BookingDto>>(endpoint, { status, page, pageSize: 10 }), [status, page],
  );

  return (
    <div className="container">
      <PageHeader icon={faFileContract} title={title} subtitle="Theo dõi yêu cầu thuê, hợp đồng, hóa đơn và thanh toán" />
      <Tabs value={status} options={STATUS_TABS} onChange={v => { setStatus(v); setPage(1); }} />
      {error && <ErrorBox error={error} onRetry={reload} />}
      {loading && !data ? <Loading /> : data?.items.length === 0 ? (
        <Empty text="Chưa có yêu cầu thuê nào.">{user!.role === 'Tenant' && <Link to="/search" className="btn btn-primary">Tìm nhà ngay</Link>}</Empty>
      ) : (
        <div className="record-list">
          {data?.items.map(b => (
            <Link key={b.id} to={`/bookings/${b.id}`} className={`record-card status-${b.status.toLowerCase()}`}>
              <span className="record-icon"><FontAwesomeIcon icon={faFileContract} /></span>
              <div className="record-main">
                <div className="record-title">
                  <strong>{b.propertyTitle}</strong>
                  <Badge status={b.status} label={bookingStatusLabel[b.status]} />
                </div>
                <div className="record-meta">
                  <span><FontAwesomeIcon icon={faLocationDot} /> {b.propertyAddress}</span>
                  {user!.role !== 'Tenant' && <span><FontAwesomeIcon icon={faUser} /> {b.tenantName}{b.tenantPhone && ` · ${b.tenantPhone}`}</span>}
                  <span><FontAwesomeIcon icon={faCalendarDays} /> {fmtDate(b.startDate)} → {fmtDate(b.endDate)} ({b.durationMonths} tháng)</span>
                </div>
              </div>
              <div className="record-amount">
                <strong>{money(b.monthlyPrice)}</strong>
                <span>/tháng · #{b.id}</span>
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

export function BookingDetailPage() {
  const { id } = useParams();
  const bookingId = Number(id);
  const { user } = useAuth();
  const { busy, run } = useAction();
  const [modal, setModal] = useState<'reject' | 'invoice' | 'payment' | null>(null);
  const [payForInvoice, setPayForInvoice] = useState<InvoiceDto | null>(null);

  const booking = useApi(() => api.get<BookingDto>(`/api/bookings/${bookingId}`), [bookingId]);
  const invoices = useApi(() => api.get<InvoiceDto[]>(`/api/bookings/${bookingId}/invoices`), [bookingId]);
  const payments = useApi(() => api.get<PaymentDto[]>(`/api/bookings/${bookingId}/payments`), [bookingId]);

  const b = booking.data;
  if (booking.loading && !b) return <Loading />;
  if (booking.error || !b) return <div className="container"><ErrorBox error={booking.error ?? 'Không tìm thấy'} /></div>;

  const isLandlord = user!.id === b.landlordId || user!.role === 'Admin';
  const isTenant = user!.id === b.tenantId;
  const reloadAll = () => { booking.reload(); invoices.reload(); payments.reload(); };

  const act = async (action: string, msg: string, body?: unknown) => {
    const res = await run(() => api.patch<BookingDto>(`/api/bookings/${b.id}/${action}`, body));
    if (res) { toast(msg); booking.setData(res); reloadAll(); }
  };

  const updatePayment = async (p: PaymentDto, status: 'Success' | 'Failed' | 'Refunded') => {
    const label = { Success: 'xác nhận đã nhận tiền', Failed: 'đánh dấu thất bại', Refunded: 'hoàn tiền' }[status];
    if (!confirm(`Bạn chắc muốn ${label} cho giao dịch ${p.transactionCode}?`)) return;
    const res = await run(() => api.patch(`/api/payments/${p.id}/status`, { status }));
    if (res) { toast('Đã cập nhật thanh toán'); payments.reload(); invoices.reload(); }
  };

  const cancelInvoice = async (inv: InvoiceDto) => {
    if (!confirm(`Hủy hóa đơn tháng ${inv.month}/${inv.year}?`)) return;
    const res = await run(() => api.patch(`/api/invoices/${inv.id}/cancel`));
    if (res) { toast('Đã hủy hóa đơn'); invoices.reload(); }
  };

  const paidSum = (payments.data ?? []).filter(p => p.status === 'Success').reduce((s, p) => s + p.amount, 0);

  return (
    <div className="container">
      <div className="breadcrumb"><Link to="/bookings">Hợp đồng</Link> / <span>#{b.id}</span></div>
      <div className="contract-hero">
        <div className="contract-hero-top">
          <div>
            <span className="kicker">Hợp đồng #{b.id}</span>
            <h1><Link to={`/properties/${b.propertyId}`}>{b.propertyTitle}</Link></h1>
            <p className="muted"><FontAwesomeIcon icon={faLocationDot} /> {b.propertyAddress}</p>
          </div>
          <Badge status={b.status} label={bookingStatusLabel[b.status]} />
        </div>
        <Stepper
          steps={['Gửi yêu cầu', 'Chủ nhà duyệt', 'Đang thuê', 'Kết thúc']}
          current={{ Pending: 1, Approved: 2, Completed: 4, Rejected: 1, Cancelled: 1 }[b.status]}
          failed={b.status === 'Rejected' ? 'Bị từ chối' : b.status === 'Cancelled' ? 'Đã hủy' : undefined}
        />
        {b.status === 'Approved' && (() => {
          const start = new Date(b.startDate).getTime();
          const end = new Date(b.endDate).getTime();
          const pct = Math.max(0, Math.min(1, (Date.now() - start) / (end - start)));
          const daysLeft = Math.max(0, Math.ceil((end - Date.now()) / 86_400_000));
          return (
            <div className="contract-progress">
              <div className="small"><span>{Date.now() < start ? `Bắt đầu ngày ${fmtDate(b.startDate)}` : `Đã thuê ${Math.round(pct * 100)}% thời hạn`}</span><span>Còn {daysLeft} ngày</span></div>
              <Progress value={pct} max={1} tone="orange" />
            </div>
          );
        })()}
      </div>

      <div className="detail-layout">
        <div>
          <div className="mini-facts">
            <div><span>Giá thuê</span><strong>{money(b.monthlyPrice)}</strong><small>mỗi tháng</small></div>
            <div><span>Tiền cọc</span><strong>{money(b.deposit)}</strong><small>hoàn lại khi kết thúc</small></div>
            <div><span>Tổng tiền thuê</span><strong>{money(b.totalAmount)}</strong><small>{b.durationMonths} tháng</small></div>
            <div className="good"><span>Đã thanh toán</span><strong>{money(paidSum)}</strong><small>giao dịch thành công</small></div>
          </div>

          <div className="card pad mt">
            <h3>Thông tin hợp đồng</h3>
            <dl className="info-grid">
              <dt>Người thuê</dt><dd>{b.tenantName} {b.tenantPhone && <span className="muted">· {b.tenantPhone}</span>}</dd>
              <dt>Thời gian</dt><dd>{fmtDate(b.startDate)} <FontAwesomeIcon icon={faArrowRight} className="muted" /> {fmtDate(b.endDate)} ({b.durationMonths} tháng)</dd>
              <dt>Ngày gửi yêu cầu</dt><dd>{fmtDateTime(b.createdAt)}</dd>
              {b.note && <><dt>Lời nhắn</dt><dd className="prewrap">{b.note}</dd></>}
              {b.rejectReason && <><dt>Lý do từ chối</dt><dd className="text-red">{b.rejectReason}</dd></>}
            </dl>
          </div>

          <div className="card pad mt anchor-section" id="invoices">
            <div className="section-head">
              <h3>Hóa đơn hàng tháng</h3>
              {isLandlord && b.status === 'Approved' && (
                <button className="btn btn-primary btn-sm" onClick={() => setModal('invoice')}><FontAwesomeIcon icon={faPlus} /> Lập hóa đơn</button>
              )}
            </div>
            {invoices.data?.length === 0 && <p className="muted">Chưa có hóa đơn.</p>}
            {invoices.data && invoices.data.length > 0 && (
              <div className="table-wrap">
                <table className="table">
                  <thead><tr><th>Kỳ</th><th>Tổng</th><th>Đã trả</th><th>Còn lại</th><th>Hạn</th><th>Trạng thái</th><th></th></tr></thead>
                  <tbody>
                    {invoices.data.map(inv => (
                      <tr key={inv.id}>
                        <td><Link to={`/invoices/${inv.id}`}>Tháng {inv.month}/{inv.year}</Link></td>
                        <td>{money(inv.totalAmount)}</td>
                        <td>{money(inv.paidAmount)}</td>
                        <td>{money(inv.remainingAmount)}</td>
                        <td>{fmtDate(inv.dueDate)}</td>
                        <td><Badge status={inv.status} label={invoiceStatusLabel[inv.status]} /></td>
                        <td className="nowrap">
                          {isTenant && b.status === 'Approved' && inv.remainingAmount > 0 && inv.status !== 'Cancelled' && (
                            <button className="btn btn-sm btn-primary" onClick={() => { setPayForInvoice(inv); setModal('payment'); }}>Thanh toán</button>
                          )}
                          {isLandlord && inv.paidAmount === 0 && inv.status !== 'Cancelled' && (
                            <button className="btn btn-sm btn-ghost danger" disabled={busy} onClick={() => cancelInvoice(inv)}>Hủy</button>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          <div className="card pad mt anchor-section" id="payments">
            <div className="section-head">
              <h3>Lịch sử thanh toán</h3>
              {isTenant && b.status === 'Approved' && (
                <button className="btn btn-primary btn-sm" onClick={() => { setPayForInvoice(null); setModal('payment'); }}><FontAwesomeIcon icon={faPlus} /> Thanh toán</button>
              )}
            </div>
            <PaymentsTable
              items={payments.data ?? []}
              canManage={isLandlord}
              busy={busy}
              onUpdate={updatePayment}
            />
          </div>
        </div>

        <aside className="detail-side">
          <div className="card pad sticky stack action-card">
            <h3>Thao tác</h3>
            {isLandlord && b.status === 'Pending' && (
              <>
                <button className="btn btn-primary btn-block" disabled={busy}
                  onClick={() => confirm('Duyệt yêu cầu này? Các yêu cầu trùng thời gian sẽ tự động bị từ chối.') && act('approve', 'Đã duyệt yêu cầu thuê')}>
                  <FontAwesomeIcon icon={faCheck} /> Duyệt yêu cầu
                </button>
                <button className="btn btn-danger btn-block" disabled={busy} onClick={() => setModal('reject')}><FontAwesomeIcon icon={faXmark} /> Từ chối</button>
              </>
            )}
            {isLandlord && b.status === 'Approved' && (
              <button className="btn btn-ghost btn-block" disabled={busy}
                onClick={() => confirm('Kết thúc hợp đồng này?') && act('complete', 'Đã kết thúc hợp đồng')}>
                Kết thúc hợp đồng
              </button>
            )}
            {(isTenant || user!.role === 'Admin') && (b.status === 'Pending' || (b.status === 'Approved' && b.startDate > todayIso())) && (
              <button className="btn btn-danger btn-block" disabled={busy}
                onClick={() => confirm('Hủy yêu cầu thuê này?') && act('cancel', 'Đã hủy yêu cầu thuê')}>
                Hủy yêu cầu
              </button>
            )}
            <Link className="btn btn-ghost btn-block" to={`/properties/${b.propertyId}`}>Xem căn nhà</Link>
            {isTenant && b.status === 'Completed' && (
              <Link className="btn btn-ghost btn-block" to={`/properties/${b.propertyId}`}><FontAwesomeIcon icon={faStar} /> Viết đánh giá</Link>
            )}
          </div>
        </aside>
      </div>

      <PromptModal
        open={modal === 'reject'} title="Từ chối yêu cầu thuê" label="Lý do từ chối" required danger confirmText="Từ chối"
        onClose={() => setModal(null)}
        onSubmit={async reason => { await act('reject', 'Đã từ chối yêu cầu', { reason }); setModal(null); }}
      />
      {modal === 'invoice' && (
        <CreateInvoiceModal open booking={b} lastInvoice={invoices.data?.find(i => i.status !== 'Cancelled')}
          onClose={() => setModal(null)} onCreated={() => { setModal(null); invoices.reload(); }} />
      )}
      {modal === 'payment' && (
        <CreatePaymentModal booking={b} invoice={payForInvoice} hasPayments={(payments.data ?? []).length > 0}
          onClose={() => setModal(null)} onCreated={() => { setModal(null); payments.reload(); invoices.reload(); }} />
      )}
    </div>
  );
}

export function PaymentsTable({ items, canManage, busy, onUpdate, showProperty }: {
  items: PaymentDto[]; canManage: boolean; busy?: boolean; showProperty?: boolean;
  onUpdate?: (p: PaymentDto, status: 'Success' | 'Failed' | 'Refunded') => void;
}) {
  if (items.length === 0) return <p className="muted">Chưa có giao dịch nào.</p>;
  return (
    <div className="table-wrap">
      <table className="table">
        <thead>
          <tr>
            <th>Mã GD</th>{showProperty && <th>Nhà</th>}<th>Loại</th><th>Số tiền</th><th>Phương thức</th>
            <th>Thời gian</th><th>Trạng thái</th>{canManage && <th></th>}
          </tr>
        </thead>
        <tbody>
          {items.map(p => (
            <tr key={p.id}>
              <td className="mono small">{p.transactionCode}</td>
              {showProperty && <td><Link to={`/bookings/${p.bookingId}`}>{p.propertyTitle ?? `Hợp đồng #${p.bookingId}`}</Link></td>}
              <td>{paymentTypeLabel[p.type]}{p.invoiceId && <div className="small"><Link to={`/invoices/${p.invoiceId}`}>HĐ #{p.invoiceId}</Link></div>}</td>
              <td><strong>{money(p.amount)}</strong></td>
              <td>{paymentMethodLabel[p.method]}</td>
              <td className="small">{fmtDateTime(p.paidAt ?? p.createdAt)}</td>
              <td><Badge status={p.status} label={paymentStatusLabel[p.status]} /></td>
              {canManage && (
                <td className="nowrap">
                  {p.status === 'Pending' && (
                    <>
                      <button className="btn btn-sm btn-primary" disabled={busy} onClick={() => onUpdate?.(p, 'Success')}>Xác nhận</button>
                      <button className="btn btn-sm btn-ghost danger" disabled={busy} onClick={() => onUpdate?.(p, 'Failed')}>Thất bại</button>
                    </>
                  )}
                  {p.status === 'Success' && (
                    <button className="btn btn-sm btn-ghost" disabled={busy} onClick={() => onUpdate?.(p, 'Refunded')}>Hoàn tiền</button>
                  )}
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function CreateInvoiceModal({ open, booking, lastInvoice, onClose, onCreated }: {
  open: boolean; booking: BookingDto; lastInvoice?: InvoiceDto; onClose: () => void; onCreated: () => void;
}) {
  const { busy, run } = useAction();
  const now = new Date();
  // Kỳ tiếp theo sau hóa đơn gần nhất, nếu chưa có thì lấy tháng hiện tại
  const nextMonth = lastInvoice ? (lastInvoice.month % 12) + 1 : now.getMonth() + 1;
  const nextYear = lastInvoice ? lastInvoice.year + (lastInvoice.month === 12 ? 1 : 0) : now.getFullYear();
  const [f, setF] = useState({
    month: nextMonth, year: nextYear,
    electricityOldIndex: lastInvoice?.electricityNewIndex ?? 0, electricityNewIndex: lastInvoice?.electricityNewIndex ?? 0,
    electricityUnitPrice: lastInvoice?.electricityUnitPrice ?? 3500,
    waterOldIndex: lastInvoice?.waterNewIndex ?? 0, waterNewIndex: lastInvoice?.waterNewIndex ?? 0,
    waterUnitPrice: lastInvoice?.waterUnitPrice ?? 15000,
    serviceFee: lastInvoice?.serviceFee ?? 0, otherFee: 0, dueDate: '', note: '',
  });
  const num = (k: keyof typeof f) => ({
    value: f[k] as number,
    onChange: (e: React.ChangeEvent<HTMLInputElement>) => setF({ ...f, [k]: Number(e.target.value) }),
  });

  const elec = Math.max(0, f.electricityNewIndex - f.electricityOldIndex) * f.electricityUnitPrice;
  const water = Math.max(0, f.waterNewIndex - f.waterOldIndex) * f.waterUnitPrice;
  const total = booking.monthlyPrice + elec + water + f.serviceFee + f.otherFee;

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    const res = await run(() => api.post(`/api/bookings/${booking.id}/invoices`, {
      ...f, dueDate: f.dueDate || undefined, note: f.note || undefined,
    }));
    if (res) { toast('Đã lập hóa đơn'); onCreated(); }
  };

  return (
    <Modal open={open} title="Lập hóa đơn tháng" onClose={onClose} width={640}>
      <form onSubmit={submit}>
        <div className="grid-3">
          <label className="field"><span>Tháng</span><input type="number" min={1} max={12} required {...num('month')} /></label>
          <label className="field"><span>Năm</span><input type="number" min={2000} max={2100} required {...num('year')} /></label>
          <label className="field"><span>Hạn thanh toán</span>
            <input type="date" value={f.dueDate} onChange={e => setF({ ...f, dueDate: e.target.value })} />
          </label>
        </div>
        <fieldset><legend><FontAwesomeIcon icon={faBolt} /> Điện</legend>
          <div className="grid-3">
            <label className="field"><span>Chỉ số cũ</span><input type="number" min={0} {...num('electricityOldIndex')} /></label>
            <label className="field"><span>Chỉ số mới</span><input type="number" min={0} {...num('electricityNewIndex')} /></label>
            <label className="field"><span>Đơn giá / kWh</span><input type="number" min={0} {...num('electricityUnitPrice')} /></label>
          </div>
        </fieldset>
        <fieldset><legend><FontAwesomeIcon icon={faDroplet} /> Nước</legend>
          <div className="grid-3">
            <label className="field"><span>Chỉ số cũ</span><input type="number" min={0} {...num('waterOldIndex')} /></label>
            <label className="field"><span>Chỉ số mới</span><input type="number" min={0} {...num('waterNewIndex')} /></label>
            <label className="field"><span>Đơn giá / m³</span><input type="number" min={0} {...num('waterUnitPrice')} /></label>
          </div>
        </fieldset>
        <div className="grid-2">
          <label className="field"><span>Phí dịch vụ (wifi, rác, xe...)</span><input type="number" min={0} {...num('serviceFee')} /></label>
          <label className="field"><span>Phí khác</span><input type="number" min={0} {...num('otherFee')} /></label>
        </div>
        <label className="field"><span>Ghi chú</span>
          <input maxLength={500} value={f.note} onChange={e => setF({ ...f, note: e.target.value })} />
        </label>
        <div className="summary-box">
          <div><span>Tiền thuê</span><strong>{money(booking.monthlyPrice)}</strong></div>
          <div><span>Điện</span><strong>{money(elec)}</strong></div>
          <div><span>Nước</span><strong>{money(water)}</strong></div>
          <div className="total"><span>Tổng (tạm tính)</span><strong>{money(total)}</strong></div>
        </div>
        <div className="form-actions">
          <button type="button" className="btn btn-ghost" onClick={onClose}>Hủy</button>
          <button className="btn btn-primary" disabled={busy}>Lập hóa đơn</button>
        </div>
      </form>
    </Modal>
  );
}

function CreatePaymentModal({ booking, invoice, hasPayments, onClose, onCreated }: {
  booking: BookingDto; invoice: InvoiceDto | null; hasPayments: boolean;
  onClose: () => void; onCreated: () => void;
}) {
  const { busy, run } = useAction();
  const defaultType: PaymentType = invoice || hasPayments ? 'MonthlyRent' : 'Deposit';
  const [type, setType] = useState<PaymentType>(defaultType);
  const [method, setMethod] = useState<PaymentMethod>('BankTransfer');
  const [amount, setAmount] = useState(
    invoice ? invoice.remainingAmount : defaultType === 'Deposit' ? booking.deposit : booking.monthlyPrice,
  );
  const [note, setNote] = useState('');

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    const res = await run(() => api.post(`/api/bookings/${booking.id}/payments`, {
      amount, type, method, invoiceId: invoice?.id, note: note || undefined,
    }));
    if (res) { toast('Đã tạo thanh toán, chờ chủ nhà xác nhận'); onCreated(); }
  };

  return (
    <Modal open title={invoice ? `Thanh toán hóa đơn tháng ${invoice.month}/${invoice.year}` : 'Tạo thanh toán'} onClose={onClose}>
      <form onSubmit={submit}>
        {!invoice && (
          <label className="field"><span>Loại thanh toán</span>
            <select value={type} onChange={e => {
              const t = e.target.value as PaymentType;
              setType(t);
              setAmount(t === 'Deposit' ? booking.deposit : booking.monthlyPrice);
            }}>
              {Object.entries(paymentTypeLabel).map(([v, l]) => <option key={v} value={v}>{l}</option>)}
            </select>
          </label>
        )}
        <div className="grid-2">
          <label className="field"><span>Số tiền (đ)</span>
            <input type="number" min={1000} required value={amount} onChange={e => setAmount(Number(e.target.value))} />
          </label>
          <label className="field"><span>Phương thức</span>
            <select value={method} onChange={e => setMethod(e.target.value as PaymentMethod)}>
              {Object.entries(paymentMethodLabel).map(([v, l]) => <option key={v} value={v}>{l}</option>)}
            </select>
          </label>
        </div>
        <label className="field"><span>Ghi chú</span>
          <input maxLength={500} value={note} onChange={e => setNote(e.target.value)} placeholder="Ví dụ: đã chuyển khoản qua Vietcombank" />
        </label>
        <p className="muted small">Thanh toán sẽ ở trạng thái “Chờ xác nhận” cho đến khi chủ nhà xác nhận đã nhận tiền.</p>
        <div className="form-actions">
          <button type="button" className="btn btn-ghost" onClick={onClose}>Hủy</button>
          <button className="btn btn-primary" disabled={busy}>Xác nhận thanh toán {money(amount)}</button>
        </div>
      </form>
    </Modal>
  );
}

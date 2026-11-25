import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import type { IconDefinition } from '@fortawesome/fontawesome-svg-core';
import {
  faArrowRight, faBuildingUser, faCalendarCheck, faCircleExclamation, faCreditCard, faEye, faFileContract,
  faFileInvoiceDollar, faFlag, faHeart, faHouseCircleCheck, faMagnifyingGlass, faMoneyBillTrendUp, faPlus, faStar,
  faUserLock, faUsers, faWallet,
} from '@fortawesome/free-solid-svg-icons';
import { api } from '../api/client';
import {
  appointmentStatusLabel, bookingStatusLabel, ensureUtc, fmtDate, fmtDateTime, invoiceStatusLabel, money,
  paymentMethodLabel, propertyStatusLabel, propertyTypeLabel, reportReasonLabel, roleLabel, shortMoney,
} from '../api/labels';
import type {
  AdminStatisticsDto, AppointmentDto, BookingDto, InvoiceDto, LandlordStatisticsDto, PagedResult, PaymentDto,
  PropertySummaryDto, ReportDto, TenantStatisticsDto,
} from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { PropertyCard } from '../components/PropertyCard';
import { BreakdownList, RevenueChart } from '../components/RevenueChart';
import { Avatar, Badge, ErrorBox, Loading, Progress, StatCard } from '../components/ui';
import { useApi } from '../hooks';

const greeting = () => {
  const h = new Date().getHours();
  return h < 11 ? 'Chào buổi sáng' : h < 14 ? 'Chào buổi trưa' : h < 18 ? 'Chào buổi chiều' : 'Chào buổi tối';
};

export default function DashboardPage() {
  const { user } = useAuth();
  return (
    <div className="dashboard">
      <div className="welcome-banner">
        <div>
          <span className="kicker light">{roleLabel[user!.role]}</span>
          <h1>{greeting()}, {user!.fullName}!</h1>
          <p>{user!.role === 'Tenant' ? 'Theo dõi hợp đồng, lịch xem nhà và hóa đơn của bạn ở một nơi.'
            : user!.role === 'Landlord' ? 'Đây là tình hình cho thuê của bạn hôm nay.'
            : 'Tổng quan hoạt động của toàn hệ thống.'}</p>
        </div>
        <Avatar name={user!.fullName} url={user!.avatarUrl} size={72} />
      </div>
      {user!.role === 'Admin' && <AdminDashboard />}
      {user!.role === 'Landlord' && <LandlordDashboard />}
      {user!.role === 'Tenant' && <TenantDashboard />}
    </div>
  );
}

/** Khung danh sách ngắn trên dashboard */
function Panel<T>({ title, icon, to, items, empty, render, action }: {
  title: string; icon: IconDefinition; to: string; items: T[] | undefined | null; empty: string;
  render: (item: T) => ReactNode; action?: ReactNode;
}) {
  return (
    <section className="panel">
      <div className="panel-head">
        <h3><span className="panel-icon"><FontAwesomeIcon icon={icon} /></span>{title}</h3>
        <Link to={to} className="see-all small">Xem tất cả <FontAwesomeIcon icon={faArrowRight} /></Link>
      </div>
      {!items ? <div className="panel-empty muted small">Đang tải...</div>
        : items.length === 0 ? <div className="panel-empty muted small">{empty}{action && <div className="mt-sm">{action}</div>}</div>
        : <div className="panel-list">{items.map(render)}</div>}
    </section>
  );
}

const list = <T,>(path: string, query: Record<string, string | number>) =>
  api.get<PagedResult<T>>(path, query).then(r => r.items);

function DateChip({ value }: { value: string }) {
  const d = new Date(ensureUtc(value));
  return (
    <span className="date-chip">
      <strong>{d.getDate()}</strong>
      <span>Th{d.getMonth() + 1}</span>
    </span>
  );
}

// ===================== Người thuê =====================

function TenantDashboard() {
  const { data: s, loading, error, reload } = useApi(() => api.get<TenantStatisticsDto>('/api/statistics/tenant'));
  const { data: contracts } = useApi(() => list<BookingDto>('/api/bookings/mine', { status: 'Approved', pageSize: 3 }));
  const { data: pendingBookings } = useApi(() => list<BookingDto>('/api/bookings/mine', { status: 'Pending', pageSize: 3 }));
  const { data: appts } = useApi(() => list<AppointmentDto>('/api/appointments/mine', { pageSize: 10 }));
  const { data: invoices } = useApi(async () => {
    const [unpaid, partial, overdue] = await Promise.all(['Unpaid', 'PartiallyPaid', 'Overdue']
      .map(status => list<InvoiceDto>('/api/invoices/mine', { status, pageSize: 5 })));
    return [...overdue, ...partial, ...unpaid].slice(0, 4);
  });
  const { data: suggestions } = useApi(() => list<PropertySummaryDto>('/api/properties', { sortBy: 'newest', pageSize: 4 }));

  if (loading && !s) return <Loading />;
  if (error || !s) return <ErrorBox error={error ?? ''} onRetry={reload} />;

  const upcoming = appts?.filter(a => (a.status === 'Pending' || a.status === 'Confirmed') && new Date(ensureUtc(a.scheduledAt)) > new Date()).slice(0, 3);

  return (
    <>
      <div className="stat-grid">
        <StatCard icon={faFileContract} tone="blue" label="Hợp đồng đang thuê" value={s.activeContracts} hint={`${s.pendingBookings} yêu cầu chờ duyệt`} to="/bookings" />
        <StatCard icon={faCalendarCheck} tone="purple" label="Lịch xem sắp tới" value={s.upcomingAppointments} to="/appointments" />
        <StatCard icon={faFileInvoiceDollar} tone={s.unpaidInvoices ? 'red' : 'green'} label="Hóa đơn cần thanh toán" value={s.unpaidInvoices}
          hint={s.outstandingAmount > 0 ? `Còn nợ ${money(s.outstandingAmount)}` : 'Bạn đã thanh toán đủ'} to="/invoices" />
        <StatCard icon={faWallet} tone="green" label="Đã thanh toán" value={shortMoney(s.totalPaid)} hint={money(s.totalPaid)} to="/payments" />
        <StatCard icon={faHeart} tone="orange" label="Nhà đã lưu" value={s.favorites} to="/favorites" />
      </div>

      {s.unpaidInvoices > 0 && (
        <div className="action-alert">
          <FontAwesomeIcon icon={faCircleExclamation} />
          <div><strong>Bạn có {s.unpaidInvoices} hóa đơn chưa thanh toán</strong><span>Tổng cộng {money(s.outstandingAmount)}. Thanh toán đúng hạn để tránh bị tính quá hạn.</span></div>
          <Link to="/invoices" className="btn btn-primary btn-sm">Thanh toán ngay</Link>
        </div>
      )}

      <div className="panel-grid">
        <Panel title="Hợp đồng của bạn" icon={faFileContract} to="/bookings" items={contracts && pendingBookings ? [...contracts, ...pendingBookings].slice(0, 3) : null}
          empty="Bạn chưa thuê căn nào." action={<Link to="/search" className="btn btn-primary btn-sm"><FontAwesomeIcon icon={faMagnifyingGlass} /> Tìm nhà</Link>}
          render={b => (
            <Link key={b.id} to={`/bookings/${b.id}`} className="panel-row">
              <span className="row-icon blue"><FontAwesomeIcon icon={faFileContract} /></span>
              <div className="grow">
                <strong className="ellipsis">{b.propertyTitle}</strong>
                <span className="muted small">{fmtDate(b.startDate)} → {fmtDate(b.endDate)} · {money(b.monthlyPrice)}/th</span>
              </div>
              <Badge status={b.status} label={bookingStatusLabel[b.status]} />
            </Link>
          )} />

        <Panel title="Lịch xem nhà sắp tới" icon={faCalendarCheck} to="/appointments" items={upcoming}
          empty="Không có lịch hẹn nào sắp tới."
          render={a => (
            <Link key={a.id} to="/appointments" className="panel-row">
              <DateChip value={a.scheduledAt} />
              <div className="grow">
                <strong className="ellipsis">{a.propertyTitle}</strong>
                <span className="muted small">{fmtDateTime(a.scheduledAt)}</span>
              </div>
              <Badge status={a.status} label={appointmentStatusLabel[a.status]} />
            </Link>
          )} />

        <Panel title="Hóa đơn cần thanh toán" icon={faFileInvoiceDollar} to="/invoices" items={invoices}
          empty="Tuyệt vời! Bạn không có hóa đơn nào cần thanh toán."
          render={inv => (
            <Link key={inv.id} to={`/invoices/${inv.id}`} className="panel-row">
              <span className={`row-icon ${inv.status === 'Overdue' ? 'red' : 'gold'}`}><FontAwesomeIcon icon={faFileInvoiceDollar} /></span>
              <div className="grow">
                <strong className="ellipsis">Tháng {inv.month}/{inv.year} · {inv.propertyTitle}</strong>
                <Progress value={inv.paidAmount} max={inv.totalAmount} />
                <span className="muted small">Còn {money(inv.remainingAmount)} · hạn {fmtDate(inv.dueDate)}</span>
              </div>
              <Badge status={inv.status} label={invoiceStatusLabel[inv.status]} />
            </Link>
          )} />
      </div>

      {suggestions && suggestions.length > 0 && (
        <section className="mt-lg">
          <div className="section-title">
            <div><span className="kicker">Gợi ý cho bạn</span><h2>Nhà mới đăng</h2></div>
            <Link to="/search" className="see-all">Xem thêm <FontAwesomeIcon icon={faArrowRight} /></Link>
          </div>
          <div className="property-grid">{suggestions.map(p => <PropertyCard key={p.id} p={p} />)}</div>
        </section>
      )}
    </>
  );
}

// ===================== Chủ nhà =====================

function LandlordDashboard() {
  const { data: s, loading, error, reload } = useApi(() => api.get<LandlordStatisticsDto>('/api/statistics/landlord'));
  const { data: bookings } = useApi(() => list<BookingDto>('/api/bookings/received', { status: 'Pending', pageSize: 4 }));
  const { data: appts } = useApi(() => list<AppointmentDto>('/api/appointments/received', { status: 'Pending', pageSize: 4 }));
  const { data: payments } = useApi(() => list<PaymentDto>('/api/payments/received', { status: 'Pending', pageSize: 4 }));

  if (loading && !s) return <Loading />;
  if (error || !s) return <ErrorBox error={error ?? ''} onRetry={reload} />;

  const todo = s.pendingBookings + s.pendingAppointments + (payments?.length ?? 0);

  return (
    <>
      <div className="stat-grid">
        <StatCard icon={faMoneyBillTrendUp} tone="green" label="Tổng doanh thu" value={shortMoney(s.totalRevenue)} hint={money(s.totalRevenue)} to="/payments" />
        <StatCard icon={faFileContract} tone="blue" label="Hợp đồng đang hiệu lực" value={s.activeContracts} hint={`${s.pendingBookings} yêu cầu chờ duyệt`} to="/bookings" />
        <StatCard icon={faFileInvoiceDollar} tone={s.unpaidInvoices ? 'gold' : 'green'} label="Hóa đơn chưa thu" value={s.unpaidInvoices} hint={`Công nợ ${money(s.outstandingAmount)}`} to="/invoices" />
        <StatCard icon={faBuildingUser} tone="orange" label="Tin đăng" value={s.totalProperties} hint={<><FontAwesomeIcon icon={faEye} /> {s.totalViews} lượt xem</>} to="/landlord/properties" />
        <StatCard icon={faStar} tone="purple" label="Đánh giá trung bình" value={s.totalReviews ? s.averageRating.toFixed(1) : '—'} hint={`${s.totalReviews} đánh giá`} />
      </div>

      {todo > 0 && (
        <div className="action-alert">
          <FontAwesomeIcon icon={faCircleExclamation} />
          <div><strong>Bạn có {todo} việc cần xử lý</strong><span>Khách đang chờ bạn phản hồi. Phản hồi nhanh giúp chốt hợp đồng sớm hơn.</span></div>
        </div>
      )}

      <div className="panel-grid">
        <Panel title="Yêu cầu thuê chờ duyệt" icon={faFileContract} to="/bookings" items={bookings} empty="Không có yêu cầu nào đang chờ."
          render={b => (
            <Link key={b.id} to={`/bookings/${b.id}`} className="panel-row">
              <Avatar name={b.tenantName} size={36} />
              <div className="grow">
                <strong className="ellipsis">{b.tenantName}</strong>
                <span className="muted small ellipsis">{b.propertyTitle} · {b.durationMonths} tháng từ {fmtDate(b.startDate)}</span>
              </div>
              <span className="btn btn-sm btn-outline">Xem</span>
            </Link>
          )} />
        <Panel title="Lịch xem chờ xác nhận" icon={faCalendarCheck} to="/appointments" items={appts} empty="Không có lịch hẹn nào đang chờ."
          render={a => (
            <Link key={a.id} to="/appointments" className="panel-row">
              <DateChip value={a.scheduledAt} />
              <div className="grow">
                <strong className="ellipsis">{a.tenantName}</strong>
                <span className="muted small ellipsis">{fmtDateTime(a.scheduledAt)} · {a.propertyTitle}</span>
              </div>
            </Link>
          )} />
        <Panel title="Thanh toán chờ xác nhận" icon={faCreditCard} to="/payments" items={payments} empty="Không có giao dịch nào chờ xác nhận."
          render={p => (
            <Link key={p.id} to={`/bookings/${p.bookingId}`} className="panel-row">
              <span className="row-icon green"><FontAwesomeIcon icon={faCreditCard} /></span>
              <div className="grow">
                <strong>{money(p.amount)}</strong>
                <span className="muted small ellipsis">{paymentMethodLabel[p.method]} · {p.propertyTitle}</span>
              </div>
              <span className="btn btn-sm btn-outline">Xác nhận</span>
            </Link>
          )} />
      </div>

      <div className="grid-dash mt">
        <RevenueChart data={s.revenueByMonth} />
        <BreakdownList title="Tin đăng theo trạng thái" data={s.propertiesByStatus} labels={propertyStatusLabel} />
      </div>
      <div className="quick-links mt">
        <Link className="btn btn-primary" to="/dang-tin"><FontAwesomeIcon icon={faPlus} /> Đăng tin mới</Link>
      </div>
    </>
  );
}

// ===================== Quản trị =====================

function AdminDashboard() {
  const { data: s, loading, error, reload } = useApi(() => api.get<AdminStatisticsDto>('/api/statistics/admin'));
  const { data: pending } = useApi(() => list<PropertySummaryDto>('/api/properties/manage', { status: 'PendingApproval', pageSize: 5 }));
  const { data: reports } = useApi(() => list<ReportDto>('/api/reports', { status: 'Pending', pageSize: 5 }));

  if (loading && !s) return <Loading />;
  if (error || !s) return <ErrorBox error={error ?? ''} onRetry={reload} />;

  return (
    <>
      <div className="stat-grid">
        <StatCard icon={faUsers} tone="blue" label="Người dùng" value={s.totalUsers} hint={<><FontAwesomeIcon icon={faUserLock} /> {s.lockedUsers} bị khóa</>} to="/admin/users" />
        <StatCard icon={faHouseCircleCheck} tone="orange" label="Tin đăng" value={s.totalProperties} hint={`${s.propertiesByStatus.PendingApproval ?? 0} tin chờ duyệt`} to="/admin/properties" />
        <StatCard icon={faFileContract} tone="purple" label="Hợp đồng" value={s.totalBookings} to="/bookings" />
        <StatCard icon={faFlag} tone={s.pendingReports ? 'red' : 'green'} label="Báo cáo chờ xử lý" value={s.pendingReports} to="/admin/reports" />
        <StatCard icon={faMoneyBillTrendUp} tone="green" label="Tổng giao dịch" value={shortMoney(s.totalRevenue)} hint={money(s.totalRevenue)} />
      </div>

      <div className="panel-grid two">
        <Panel title="Tin chờ duyệt" icon={faHouseCircleCheck} to="/admin/properties" items={pending} empty="Không có tin nào chờ duyệt."
          render={p => (
            <Link key={p.id} to={`/properties/${p.id}`} className="panel-row">
              {p.thumbnailUrl ? <img className="row-thumb" src={p.thumbnailUrl} alt="" /> : <span className="row-icon"><FontAwesomeIcon icon={faHouseCircleCheck} /></span>}
              <div className="grow">
                <strong className="ellipsis">{p.title}</strong>
                <span className="muted small ellipsis">{propertyTypeLabel[p.type]} · {p.landlordName} · {money(p.pricePerMonth)}</span>
              </div>
            </Link>
          )} />
        <Panel title="Báo cáo vi phạm mới" icon={faFlag} to="/admin/reports" items={reports} empty="Không có báo cáo nào chờ xử lý."
          render={r => (
            <Link key={r.id} to="/admin/reports" className="panel-row">
              <span className="row-icon red"><FontAwesomeIcon icon={faFlag} /></span>
              <div className="grow">
                <strong className="ellipsis">{r.propertyTitle}</strong>
                <span className="muted small ellipsis">{reportReasonLabel[r.reason]} · {r.reporterName}</span>
              </div>
            </Link>
          )} />
      </div>

      <RevenueChart data={s.revenueByMonth} title="Giá trị giao dịch 12 tháng gần nhất" />
      <div className="grid-3 gap-lg mt">
        <BreakdownList title="Người dùng theo vai trò" data={s.usersByRole} labels={roleLabel} />
        <BreakdownList title="Tin đăng theo trạng thái" data={s.propertiesByStatus} labels={propertyStatusLabel} />
        <BreakdownList title="Hợp đồng theo trạng thái" data={s.bookingsByStatus} labels={bookingStatusLabel} />
      </div>
    </>
  );
}

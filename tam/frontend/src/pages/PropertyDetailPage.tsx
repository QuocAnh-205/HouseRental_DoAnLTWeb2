import { useEffect, useRef, useState, type FormEvent } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faArrowUpRightFromSquare, faBath, faBed, faBuildingUser, faCalendarCheck, faCalendarDays, faChevronLeft,
  faChevronRight, faCircleCheck, faCircleInfo, faCommentDots, faEnvelope, faEye, faFileSignature, faFlag,
  faHandHoldingDollar, faHeart as faHeartSolid, faHouseCircleCheck, faImages, faLocationDot, faMapLocationDot,
  faMoneyBillWave, faPenToSquare, faPhone, faRulerCombined, faShareNodes, faShieldHalved, faStar, faTag,
} from '@fortawesome/free-solid-svg-icons';
import { faHeart as faHeartRegular } from '@fortawesome/free-regular-svg-icons';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api } from '../api/client';
import {
  fmtDate, money, propertyStatusLabel, propertyTypeLabel, reportReasonLabel, shortMoney, timeAgo, todayIso,
} from '../api/labels';
import type {
  PagedResult, PropertyDetailDto, PropertySummaryDto, PublicProfileDto, ReportReason, ReviewDto, ReviewEligibilityDto,
} from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { imgFallback, isNewListing, placeholderImage, PropertyCard } from '../components/PropertyCard';
import { Avatar, Badge, ErrorBox, Loading, Modal, Pagination, StarInput, Stars } from '../components/ui';
import { useFavorites } from '../favorites';
import { amenityIcon, propertyTypeIcon } from '../icons';
import { toast, useAction, useApi } from '../hooks';

const DURATIONS = [1, 3, 6, 12, 24, 36];

export default function PropertyDetailPage() {
  const { id } = useParams();
  const propertyId = Number(id);
  const { user } = useAuth();
  const navigate = useNavigate();
  const favorites = useFavorites();
  const [modal, setModal] = useState<'appointment' | 'report' | 'gallery' | null>(null);
  const [galleryIndex, setGalleryIndex] = useState(0);
  const [showFullDesc, setShowFullDesc] = useState(false);
  const bookingRef = useRef<HTMLDivElement>(null);

  const { data: p, loading, error, reload } = useApi(
    () => api.get<PropertyDetailDto>(`/api/properties/${propertyId}`), [propertyId, user?.id],
  );
  const { data: similar } = useApi(
    () => api.get<PropertySummaryDto[]>(`/api/properties/${propertyId}/similar`, { count: 4 }), [propertyId],
  );
  const landlordId = p?.landlord.id;
  const { data: host } = useApi(
    () => landlordId ? api.get<PublicProfileDto>(`/api/users/${landlordId}/profile`) : Promise.resolve(null), [landlordId],
  );

  useEffect(() => { if (p) favorites.sync(p.id, p.isFavorited); }, [p]); // eslint-disable-line react-hooks/exhaustive-deps

  if (loading && !p) return <Loading />;
  if (error || !p) return <div className="container"><ErrorBox error={error ?? 'Không tìm thấy nhà'} onRetry={reload} /></div>;

  const isOwner = user?.id === p.landlord.id;
  const isAdmin = user?.role === 'Admin';
  const canRent = (!user || user.role === 'Tenant') && p.status === 'Available';
  const images = [...p.images].sort((a, b) => Number(b.isThumbnail) - Number(a.isThumbnail));
  const fav = favorites.isFavorite(p.id);
  const fullAddress = [p.address, p.ward, p.district, p.city].filter(Boolean).join(', ');
  const pricePerM2 = p.area > 0 ? p.pricePerMonth / p.area : 0;
  const depositMonths = p.pricePerMonth > 0 ? p.deposit / p.pricePerMonth : 0;
  const longDesc = p.description.length > 420;

  const requireLogin = () => {
    if (!user) {
      navigate('/login', { state: { from: `/properties/${propertyId}` } });
      return false;
    }
    return true;
  };

  const openGallery = (i: number) => { setGalleryIndex(i); setModal('gallery'); };

  // Vào thẳng khung chat với chủ nhà về căn nhà này
  const messageLandlord = () => {
    if (!requireLogin()) return;
    const q = new URLSearchParams({ to: String(p.landlord.id), property: String(p.id), title: p.title });
    navigate(`/messages?${q}`);
  };

  const share = async () => {
    const url = window.location.href;
    try {
      if (navigator.share) await navigator.share({ title: p.title, url });
      else { await navigator.clipboard.writeText(url); toast('Đã sao chép liên kết'); }
    } catch { /* người dùng hủy */ }
  };

  const highlights: string[] = [];
  if (p.status === 'Available') highlights.push('Còn trống, có thể đặt lịch xem và gửi yêu cầu thuê ngay');
  if (pricePerM2 > 0) highlights.push(`Giá chỉ khoảng ${Math.round(pricePerM2 / 1000).toLocaleString('vi-VN')}.000đ mỗi m²`);
  if (depositMonths > 0) highlights.push(`Tiền cọc tương đương ${+depositMonths.toFixed(1)} tháng tiền thuê`);
  else highlights.push('Không yêu cầu đặt cọc');
  if (p.amenities.length) highlights.push(`${p.amenities.length} tiện ích đi kèm: ${p.amenities.slice(0, 3).join(', ')}${p.amenities.length > 3 ? '...' : ''}`);
  if (p.reviewCount > 0) highlights.push(`Được ${p.reviewCount} người thuê chấm ${p.averageRating.toFixed(1)}/5 sao`);
  if (p.viewCount >= 50) highlights.push(`Đang được quan tâm: ${p.viewCount} lượt xem`);

  return (
    <div className="detail-page">
      <div className="container">
        <div className="breadcrumb">
          <Link to="/">Trang chủ</Link> / <Link to="/search">Tìm nhà</Link> / <Link to={`/search?city=${encodeURIComponent(p.city)}`}>{p.city}</Link>
          {' / '}<Link to={`/search?city=${encodeURIComponent(p.city)}&district=${encodeURIComponent(p.district)}`}>{p.district}</Link>
        </div>

        {p.status !== 'Available' && (
          <div className={`alert ${p.status === 'Rejected' ? 'alert-error' : 'alert-info'}`}>
            <span><FontAwesomeIcon icon={faCircleInfo} /> Trạng thái tin: <Badge status={p.status} label={propertyStatusLabel[p.status]} />
              {p.rejectReason && <> — Lý do: {p.rejectReason}</>}</span>
          </div>
        )}

        {/* ===== Tiêu đề ===== */}
        <div className="detail-header">
          <div>
            <div className="detail-badges">
              <span className="pill pill-soft"><FontAwesomeIcon icon={propertyTypeIcon[p.type]} /> {propertyTypeLabel[p.type]}</span>
              {p.status === 'Available' && <span className="pill pill-green"><FontAwesomeIcon icon={faShieldHalved} /> Tin đã kiểm duyệt</span>}
              {isNewListing(p.createdAt) && <span className="pill pill-accent">Mới đăng</span>}
            </div>
            <h1 className="detail-title">{p.title}</h1>
            <div className="detail-meta">
              <span><FontAwesomeIcon icon={faLocationDot} /> {fullAddress}</span>
              {p.reviewCount > 0 && <span className="rating-chip"><FontAwesomeIcon icon={faStar} /> {p.averageRating.toFixed(1)} <a href="#reviews">({p.reviewCount} đánh giá)</a></span>}
              <span><FontAwesomeIcon icon={faEye} /> {p.viewCount} lượt xem</span>
              <span>Đăng {fmtDate(p.createdAt)}</span>
            </div>
          </div>
          <div className="detail-actions">
            <button className="btn btn-ghost btn-sm" onClick={share}><FontAwesomeIcon icon={faShareNodes} /> Chia sẻ</button>
            <button className={`btn btn-ghost btn-sm ${fav ? 'is-fav' : ''}`} onClick={() => favorites.toggle(p.id)}>
              <FontAwesomeIcon icon={fav ? faHeartSolid : faHeartRegular} /> {fav ? 'Đã lưu' : 'Lưu'}
            </button>
          </div>
        </div>

        {/* ===== Bộ ảnh ===== */}
        <div className={`mosaic mosaic-${Math.min(images.length, 5)}`}>
          {images.length === 0 && <div className="mosaic-empty"><img src={placeholderImage} alt="" /></div>}
          {images.slice(0, 5).map((img, i) => (
            <button key={img.id} className={`mosaic-item m${i}`} onClick={() => openGallery(i)}>
              <img src={img.url} alt={`${p.title} – ảnh ${i + 1}`} onError={imgFallback} />
            </button>
          ))}
          {images.length > 1 && (
            <button className="mosaic-all" onClick={() => openGallery(0)}><FontAwesomeIcon icon={faImages} /> Xem {images.length} ảnh</button>
          )}
        </div>

        <div className="detail-layout">
          <div className="detail-main">
            {/* Thông số nhanh */}
            <div className="facts">
              <Fact icon={faMoneyBillWave} label="Giá thuê" value={`${shortMoney(p.pricePerMonth)}/th`} accent />
              <Fact icon={faRulerCombined} label="Diện tích" value={`${p.area} m²`} />
              <Fact icon={faBed} label="Phòng ngủ" value={String(p.bedrooms)} />
              <Fact icon={faBath} label="Phòng tắm" value={String(p.bathrooms)} />
              <Fact icon={faHandHoldingDollar} label="Tiền cọc" value={p.deposit > 0 ? shortMoney(p.deposit) : 'Không cọc'} />
              <Fact icon={propertyTypeIcon[p.type]} label="Loại hình" value={propertyTypeLabel[p.type]} />
            </div>

            <section className="detail-section">
              <h2>Điểm nổi bật</h2>
              <ul className="highlights">
                {highlights.map(h => <li key={h}><FontAwesomeIcon icon={faCircleCheck} /> {h}</li>)}
              </ul>
            </section>

            <section className="detail-section">
              <h2>Mô tả chi tiết</h2>
              <div className={`description ${longDesc && !showFullDesc ? 'clamped' : ''}`}>
                {p.description || <span className="muted">Chủ nhà chưa viết mô tả. Hãy nhắn tin để hỏi thêm thông tin nhé.</span>}
              </div>
              {longDesc && (
                <button className="link-btn" onClick={() => setShowFullDesc(s => !s)}>{showFullDesc ? 'Thu gọn' : 'Xem thêm'}</button>
              )}
            </section>

            {p.amenities.length > 0 && (
              <section className="detail-section">
                <h2>Tiện ích có sẵn</h2>
                <div className="amenity-grid">
                  {p.amenities.map(a => (
                    <div key={a} className="amenity"><span className="amenity-icon"><FontAwesomeIcon icon={amenityIcon(a)} /></span>{a}</div>
                  ))}
                </div>
              </section>
            )}

            <section className="detail-section">
              <h2>Chi phí khi dọn vào</h2>
              <div className="cost-table">
                <div><span>Tiền thuê tháng đầu</span><strong>{money(p.pricePerMonth)}</strong></div>
                <div><span>Tiền đặt cọc</span><strong>{money(p.deposit)}</strong></div>
                <div className="total"><span>Cần chuẩn bị khi nhận nhà</span><strong>{money(p.pricePerMonth + p.deposit)}</strong></div>
              </div>
              <p className="muted small"><FontAwesomeIcon icon={faCircleInfo} /> Tiền điện, nước tính theo chỉ số thực tế trên hóa đơn chủ nhà lập mỗi tháng, bạn xem và thanh toán ngay trên web.</p>
            </section>

            <section className="detail-section">
              <h2>Vị trí</h2>
              <p><FontAwesomeIcon icon={faLocationDot} className="text-accent" /> {fullAddress}</p>
              {p.latitude != null && p.longitude != null ? (
                <>
                  <iframe
                    className="map"
                    title="Bản đồ"
                    loading="lazy"
                    src={`https://www.openstreetmap.org/export/embed.html?bbox=${p.longitude - 0.01},${p.latitude - 0.006},${p.longitude + 0.01},${p.latitude + 0.006}&layer=mapnik&marker=${p.latitude},${p.longitude}`}
                  />
                  <a className="small" target="_blank" rel="noreferrer" href={`https://www.google.com/maps?q=${p.latitude},${p.longitude}`}>
                    <FontAwesomeIcon icon={faMapLocationDot} /> Chỉ đường bằng Google Maps <FontAwesomeIcon icon={faArrowUpRightFromSquare} />
                  </a>
                </>
              ) : (
                <a className="btn btn-ghost btn-sm" target="_blank" rel="noreferrer" href={`https://www.google.com/maps/search/${encodeURIComponent(fullAddress)}`}>
                  <FontAwesomeIcon icon={faMapLocationDot} /> Xem trên Google Maps
                </a>
              )}
            </section>

            {/* Chủ nhà */}
            <section className="detail-section host-card">
              <div className="host-main">
                <Avatar name={p.landlord.fullName} url={p.landlord.avatarUrl} size={64} />
                <div>
                  <span className="kicker">Chủ nhà</span>
                  <h3><Link to={`/users/${p.landlord.id}`}>{p.landlord.fullName}</Link></h3>
                  {host && (
                    <div className="host-stats">
                      <span><FontAwesomeIcon icon={faBuildingUser} /> {host.activeListings} tin đang cho thuê</span>
                      {host.totalReviews > 0 && <span><FontAwesomeIcon icon={faStar} className="stars" /> {host.averageRating.toFixed(1)} ({host.totalReviews} đánh giá)</span>}
                      <span><FontAwesomeIcon icon={faCalendarDays} /> Tham gia {fmtDate(host.joinedAt)}</span>
                    </div>
                  )}
                </div>
              </div>
              <div className="host-contact">
                {user ? (
                  <>
                    {p.landlord.phoneNumber && <a className="btn btn-ghost btn-sm" href={`tel:${p.landlord.phoneNumber}`}><FontAwesomeIcon icon={faPhone} /> {p.landlord.phoneNumber}</a>}
                    {p.landlord.email && <a className="btn btn-ghost btn-sm" href={`mailto:${p.landlord.email}`}><FontAwesomeIcon icon={faEnvelope} /> Email</a>}
                  </>
                ) : (
                  <Link to="/login" state={{ from: `/properties/${p.id}` }} className="btn btn-ghost btn-sm"><FontAwesomeIcon icon={faPhone} /> Đăng nhập để xem số điện thoại</Link>
                )}
                {!isOwner && <button className="btn btn-primary btn-sm" onClick={messageLandlord}><FontAwesomeIcon icon={faCommentDots} /> Nhắn tin</button>}
              </div>
            </section>

            <Reviews property={p} onChanged={reload} />

            {user && !isOwner && (
              <button className="link-btn danger small report-link" onClick={() => setModal('report')}>
                <FontAwesomeIcon icon={faFlag} /> Báo cáo tin đăng sai sự thật hoặc lừa đảo
              </button>
            )}
          </div>

          {/* ===== Thẻ đặt thuê ===== */}
          <aside className="detail-side" ref={bookingRef} id="booking">
            <div className="booking-card">
              <div className="booking-price">
                <strong>{money(p.pricePerMonth)}</strong><span>/tháng</span>
              </div>
              {p.reviewCount > 0 && (
                <div className="booking-rating"><Stars value={p.averageRating} /> {p.averageRating.toFixed(1)} · {p.reviewCount} đánh giá</div>
              )}

              {(isOwner || isAdmin) ? (
                <div className="stack mt">
                  <Link className="btn btn-primary btn-block" to={`/landlord/properties/${p.id}/edit`}><FontAwesomeIcon icon={faPenToSquare} /> Sửa tin đăng</Link>
                  {isAdmin ? <Link className="btn btn-ghost btn-block" to="/admin/properties">Quản lý tin</Link>
                    : <Link className="btn btn-ghost btn-block" to="/bookings">Xem yêu cầu thuê</Link>}
                </div>
              ) : canRent ? (
                <BookingForm property={p} requireLogin={requireLogin} onAppointment={() => requireLogin() && setModal('appointment')} />
              ) : (
                <div className="booking-closed">
                  <FontAwesomeIcon icon={faHouseCircleCheck} />
                  <p>{p.status === 'Rented' ? 'Căn này đã có người thuê.' : user?.role === 'Landlord' ? 'Tài khoản chủ nhà không thể gửi yêu cầu thuê.' : 'Tin này hiện không nhận yêu cầu thuê.'}</p>
                  <button className="btn btn-ghost btn-block" onClick={() => favorites.toggle(p.id)}>
                    <FontAwesomeIcon icon={fav ? faHeartSolid : faHeartRegular} /> {fav ? 'Đã lưu' : 'Lưu lại để theo dõi'}
                  </button>
                  <a href="#similar" className="btn btn-primary btn-block">Xem nhà tương tự</a>
                </div>
              )}

              {!isOwner && (
                <button className="btn btn-ghost btn-block mt-sm" onClick={messageLandlord}>
                  <FontAwesomeIcon icon={faCommentDots} /> Hỏi chủ nhà
                </button>
              )}
            </div>

            <ul className="trust-list">
              <li><FontAwesomeIcon icon={faShieldHalved} /><div><strong>Tin đăng đã kiểm duyệt</strong><span>Quản trị viên duyệt trước khi hiển thị</span></div></li>
              <li><FontAwesomeIcon icon={faFileSignature} /><div><strong>Giá được chốt khi gửi yêu cầu</strong><span>Không lo tăng giá bất ngờ</span></div></li>
              <li><FontAwesomeIcon icon={faTag} /><div><strong>Đặt lịch xem nhà miễn phí</strong><span>Được nhắc lịch trước 24 giờ</span></div></li>
            </ul>
          </aside>
        </div>

        {similar && similar.length > 0 && (
          <section className="section-tight" id="similar">
            <div className="section-title">
              <div><span className="kicker">Có thể bạn thích</span><h2>Nhà tương tự gần đây</h2></div>
              <Link to={`/search?type=${p.type}&city=${encodeURIComponent(p.city)}`} className="see-all">Xem thêm</Link>
            </div>
            <div className="property-grid">{similar.map(s => <PropertyCard key={s.id} p={s} />)}</div>
          </section>
        )}
      </div>

      {/* Thanh đặt thuê cố định trên di động */}
      {canRent && (
        <div className="mobile-cta">
          <div><strong>{shortMoney(p.pricePerMonth)}</strong><span>/tháng</span></div>
          <button className="btn btn-primary" onClick={() => bookingRef.current?.scrollIntoView({ behavior: 'smooth' })}>Đặt thuê ngay</button>
        </div>
      )}

      <Modal open={modal === 'gallery'} title={`${p.title} (${galleryIndex + 1}/${images.length})`} onClose={() => setModal(null)} width={1000}>
        {images.length > 0 && (
          <Lightbox urls={images.map(i => i.url)} index={galleryIndex} onIndex={setGalleryIndex} />
        )}
      </Modal>
      <AppointmentModal open={modal === 'appointment'} propertyId={p.id} onClose={() => setModal(null)} />
      <ReportModal open={modal === 'report'} propertyId={p.id} onClose={() => setModal(null)} />
    </div>
  );
}

function Fact({ icon, label, value, accent }: { icon: Parameters<typeof FontAwesomeIcon>[0]['icon']; label: string; value: string; accent?: boolean }) {
  return (
    <div className={`fact ${accent ? 'accent' : ''}`}>
      <span className="fact-icon"><FontAwesomeIcon icon={icon} /></span>
      <div><span>{label}</span><strong>{value}</strong></div>
    </div>
  );
}

function Lightbox({ urls, index, onIndex }: { urls: string[]; index: number; onIndex: (i: number) => void }) {
  const go = (d: number) => onIndex((index + d + urls.length) % urls.length);
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'ArrowLeft') go(-1);
      if (e.key === 'ArrowRight') go(1);
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  });
  return (
    <div className="lightbox">
      <div className="lightbox-main">
        <img src={urls[index]} alt="" onError={imgFallback} />
        {urls.length > 1 && (
          <>
            <button className="gallery-nav prev" onClick={() => go(-1)}><FontAwesomeIcon icon={faChevronLeft} /></button>
            <button className="gallery-nav next" onClick={() => go(1)}><FontAwesomeIcon icon={faChevronRight} /></button>
          </>
        )}
      </div>
      {urls.length > 1 && (
        <div className="gallery-thumbs">
          {urls.map((u, i) => <img key={u + i} src={u} alt="" className={i === index ? 'active' : ''} onClick={() => onIndex(i)} onError={imgFallback} />)}
        </div>
      )}
    </div>
  );
}

function BookingForm({ property, requireLogin, onAppointment }: {
  property: PropertyDetailDto; requireLogin: () => boolean; onAppointment: () => void;
}) {
  const navigate = useNavigate();
  const { busy, run } = useAction();
  const [startDate, setStartDate] = useState(todayIso());
  const [durationMonths, setDuration] = useState(6);
  const [note, setNote] = useState('');
  const [showNote, setShowNote] = useState(false);
  const total = property.pricePerMonth * durationMonths;

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!requireLogin()) return;
    const res = await run(() => api.post<{ id: number }>('/api/bookings', { propertyId: property.id, startDate, durationMonths, note: note || undefined }));
    if (res) {
      toast('Đã gửi yêu cầu thuê! Chủ nhà sẽ phản hồi sớm.');
      navigate(`/bookings/${res.id}`);
    }
  };

  return (
    <form className="booking-form" onSubmit={submit}>
      <div className="booking-inputs">
        <label>
          <span>Ngày dọn vào</span>
          <input type="date" required min={todayIso()} value={startDate} onChange={e => setStartDate(e.target.value)} />
        </label>
        <label>
          <span>Thời hạn thuê</span>
          <select value={durationMonths} onChange={e => setDuration(Number(e.target.value))}>
            {DURATIONS.map(m => <option key={m} value={m}>{m >= 12 && m % 12 === 0 ? `${m / 12} năm (${m} tháng)` : `${m} tháng`}</option>)}
          </select>
        </label>
      </div>
      {showNote ? (
        <textarea rows={2} maxLength={1000} placeholder="Lời nhắn cho chủ nhà (số người ở, giờ giấc, thú cưng...)" value={note} onChange={e => setNote(e.target.value)} />
      ) : (
        <button type="button" className="link-btn small" onClick={() => setShowNote(true)}>+ Thêm lời nhắn cho chủ nhà</button>
      )}

      <button className="btn btn-primary btn-block btn-lg" disabled={busy}>
        <FontAwesomeIcon icon={faFileSignature} /> {busy ? 'Đang gửi...' : 'Gửi yêu cầu thuê'}
      </button>
      <button type="button" className="btn btn-outline btn-block" onClick={onAppointment}>
        <FontAwesomeIcon icon={faCalendarCheck} /> Đặt lịch xem nhà trước
      </button>
      <p className="booking-note">Bạn chưa phải trả tiền ngay. Chủ nhà duyệt yêu cầu xong mới thanh toán.</p>

      <div className="booking-breakdown">
        <div><span>{money(property.pricePerMonth)} × {durationMonths} tháng</span><span>{money(total)}</span></div>
        <div><span>Tiền đặt cọc</span><span>{money(property.deposit)}</span></div>
        <div className="total"><span>Thanh toán khi nhận nhà</span><strong>{money(property.pricePerMonth + property.deposit)}</strong></div>
      </div>
    </form>
  );
}

function AppointmentModal({ open, propertyId, onClose }: { open: boolean; propertyId: number; onClose: () => void }) {
  const navigate = useNavigate();
  const { busy, run } = useAction();
  const [when, setWhen] = useState('');
  const [note, setNote] = useState('');

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    const res = await run(() => api.post('/api/appointments', {
      propertyId, scheduledAt: new Date(when).toISOString(), note: note || undefined,
    }));
    if (res) {
      toast('Đã gửi lịch hẹn, chờ chủ nhà xác nhận');
      navigate('/appointments');
    }
  };

  return (
    <Modal open={open} title="Đặt lịch xem nhà" onClose={onClose}>
      <form onSubmit={submit}>
        <p className="muted small">Hẹn sau thời điểm hiện tại ít nhất 1 giờ và trong vòng 60 ngày. Bạn sẽ được nhắc trước 24 giờ.</p>
        <label className="field"><span>Thời gian hẹn</span>
          <input type="datetime-local" required value={when} onChange={e => setWhen(e.target.value)} />
        </label>
        <label className="field"><span>Ghi chú</span>
          <textarea rows={3} maxLength={500} value={note} onChange={e => setNote(e.target.value)} placeholder="Ví dụ: tôi muốn xem cả chỗ để xe" />
        </label>
        <div className="form-actions">
          <button type="button" className="btn btn-ghost" onClick={onClose}>Hủy</button>
          <button className="btn btn-primary" disabled={busy}>Đặt lịch</button>
        </div>
      </form>
    </Modal>
  );
}

function ReportModal({ open, propertyId, onClose }: { open: boolean; propertyId: number; onClose: () => void }) {
  const { busy, run } = useAction();
  const [reason, setReason] = useState<ReportReason>('FakeInformation');
  const [description, setDescription] = useState('');

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    const res = await run(() => api.post(`/api/properties/${propertyId}/reports`, { reason, description: description || undefined }));
    if (res) {
      toast('Cảm ơn bạn đã báo cáo, quản trị viên sẽ xem xét');
      onClose();
    }
  };

  return (
    <Modal open={open} title="Báo cáo tin vi phạm" onClose={onClose}>
      <form onSubmit={submit}>
        <label className="field"><span>Lý do</span>
          <select value={reason} onChange={e => setReason(e.target.value as ReportReason)}>
            {Object.entries(reportReasonLabel).map(([v, l]) => <option key={v} value={v}>{l}</option>)}
          </select>
        </label>
        <label className="field"><span>Mô tả thêm</span>
          <textarea rows={3} maxLength={1000} value={description} onChange={e => setDescription(e.target.value)} />
        </label>
        <div className="form-actions">
          <button type="button" className="btn btn-ghost" onClick={onClose}>Hủy</button>
          <button className="btn btn-danger" disabled={busy}>Gửi báo cáo</button>
        </div>
      </form>
    </Modal>
  );
}

function Reviews({ property, onChanged }: { property: PropertyDetailDto; onChanged: () => void }) {
  const { user } = useAuth();
  const { busy, run } = useAction();
  const [page, setPage] = useState(1);
  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState('');
  const [editing, setEditing] = useState<ReviewDto | null>(null);
  const [replying, setReplying] = useState<ReviewDto | null>(null);
  const [replyText, setReplyText] = useState('');

  const { data, reload } = useApi(
    () => api.get<PagedResult<ReviewDto>>(`/api/properties/${property.id}/reviews`, { page, pageSize: 5 }),
    [property.id, page],
  );
  // Phân bố số sao (tối đa 100 đánh giá gần nhất)
  const { data: all, reload: reloadAll } = useApi(
    () => property.reviewCount > 0
      ? api.get<PagedResult<ReviewDto>>(`/api/properties/${property.id}/reviews`, { page: 1, pageSize: 100 })
      : Promise.resolve(null),
    [property.id, property.reviewCount],
  );
  const dist = [5, 4, 3, 2, 1].map(star => ({ star, count: all?.items.filter(r => r.rating === star).length ?? 0 }));
  const distTotal = all?.items.length || 1;

  // Hỏi backend: người thuê này đã từng thuê căn nhà chưa, đã viết đánh giá chưa
  // → chỉ hiện form cho người đủ điều kiện (tránh gõ xong mới bị 403), và tìm đúng đánh giá của mình dù nằm ở trang khác.
  const { data: eligibility, reload: reloadEligibility } = useApi(
    () => user?.role === 'Tenant'
      ? api.get<ReviewEligibilityDto>(`/api/properties/${property.id}/reviews/eligibility`)
      : Promise.resolve(null),
    [property.id, user?.id, user?.role],
  );

  const canReply = user && (user.id === property.landlord.id || user.role === 'Admin');
  const myReview = eligibility?.myReview ?? all?.items.find(r => r.tenantId === user?.id);
  const canWrite = !!eligibility?.canReview;

  const refresh = () => { reload(); reloadAll(); reloadEligibility(); onChanged(); };

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    const res = editing
      ? await run(() => api.put(`/api/reviews/${editing.id}`, { rating, comment: comment || undefined }))
      : await run(() => api.post(`/api/properties/${property.id}/reviews`, { rating, comment: comment || undefined }));
    if (res) {
      toast(editing ? 'Đã cập nhật đánh giá' : 'Cảm ơn bạn đã đánh giá');
      setEditing(null);
      setComment('');
      setRating(5);
      refresh();
    }
  };

  const remove = async (r: ReviewDto) => {
    if (!confirm('Xóa đánh giá này?')) return;
    const ok = await run(() => api.del(`/api/reviews/${r.id}`));
    if (ok !== undefined) { toast('Đã xóa đánh giá'); refresh(); }
  };

  const sendReply = async (e: FormEvent) => {
    e.preventDefault();
    if (!replying) return;
    const ok = await run(() => api.put(`/api/reviews/${replying.id}/reply`, { reply: replyText }));
    if (ok) { toast('Đã phản hồi'); setReplying(null); reload(); }
  };

  return (
    <section className="detail-section" id="reviews">
      <h2>Đánh giá từ người thuê</h2>

      {property.reviewCount > 0 ? (
        <div className="review-summary">
          <div className="review-score">
            <strong>{property.averageRating.toFixed(1)}</strong>
            <Stars value={property.averageRating} size={18} />
            <span className="muted small">{property.reviewCount} đánh giá</span>
          </div>
          <div className="review-dist">
            {dist.map(d => (
              <div key={d.star} className="dist-row">
                <span>{d.star} <FontAwesomeIcon icon={faStar} /></span>
                <div className="breakdown-track"><div className="breakdown-fill gold" style={{ width: `${(d.count / distTotal) * 100}%` }} /></div>
                <span className="muted small">{d.count}</span>
              </div>
            ))}
          </div>
        </div>
      ) : (
        <p className="muted">Chưa có đánh giá nào. Chỉ người đã thuê căn này mới có thể đánh giá, nên mọi nhận xét ở đây đều là trải nghiệm thật.</p>
      )}

      {user?.role === 'Tenant' && (editing || (canWrite && !myReview)) && (
        <form className="review-form" onSubmit={submit}>
          <div className="muted small">{editing ? 'Sửa đánh giá của bạn' : 'Bạn đã thuê nhà này? Chia sẻ trải nghiệm để giúp người thuê sau.'}</div>
          <StarInput value={rating} onChange={setRating} />
          <textarea rows={2} maxLength={1000} placeholder="Nhận xét của bạn..." value={comment} onChange={e => setComment(e.target.value)} />
          <div className="form-actions">
            {editing && <button type="button" className="btn btn-ghost btn-sm" onClick={() => { setEditing(null); setComment(''); }}>Hủy</button>}
            <button className="btn btn-primary btn-sm" disabled={busy}>{editing ? 'Lưu' : 'Gửi đánh giá'}</button>
          </div>
        </form>
      )}

      <div className="reviews">
        {data?.items.map(r => (
          <div key={r.id} className="review">
            <Avatar name={r.tenantName} url={r.tenantAvatarUrl} size={44} />
            <div className="review-body">
              <div className="review-head">
                <strong>{r.tenantName}</strong>
                <span className="pill pill-green tiny"><FontAwesomeIcon icon={faCircleCheck} /> Đã thuê</span>
                <span className="muted small">{timeAgo(r.createdAt)}{r.updatedAt && ' · đã sửa'}</span>
              </div>
              <Stars value={r.rating} />
              {r.comment && <p>{r.comment}</p>}
              {r.landlordReply && (
                <div className="review-reply"><strong>Phản hồi của chủ nhà:</strong> {r.landlordReply}</div>
              )}
              <div className="review-actions">
                {user?.id === r.tenantId && (
                  <button className="link-btn small" onClick={() => { setEditing(r); setRating(r.rating); setComment(r.comment ?? ''); }}>Sửa</button>
                )}
                {(user?.id === r.tenantId || user?.role === 'Admin') && (
                  <button className="link-btn small danger" onClick={() => remove(r)}>Xóa</button>
                )}
                {canReply && (
                  <button className="link-btn small" onClick={() => { setReplying(r); setReplyText(r.landlordReply ?? ''); }}>
                    {r.landlordReply ? 'Sửa phản hồi' : 'Phản hồi'}
                  </button>
                )}
              </div>
              {replying?.id === r.id && (
                <form className="reply-form" onSubmit={sendReply}>
                  <textarea rows={2} required maxLength={1000} value={replyText} onChange={e => setReplyText(e.target.value)} autoFocus />
                  <div className="form-actions">
                    <button type="button" className="btn btn-ghost btn-sm" onClick={() => setReplying(null)}>Hủy</button>
                    <button className="btn btn-primary btn-sm" disabled={busy}>Gửi phản hồi</button>
                  </div>
                </form>
              )}
            </div>
          </div>
        ))}
      </div>
      <Pagination data={data} onPage={setPage} />
    </section>
  );
}

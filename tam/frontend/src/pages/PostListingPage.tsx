import { useEffect, useRef, useState, type ReactNode } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faArrowLeft, faArrowRight, faBullhorn, faCamera, faCircleCheck, faCircleInfo, faHouseCircleCheck, faImages, faListCheck,
  faLocationCrosshairs, faLocationDot, faMagnifyingGlassLocation, faNewspaper, faPaperPlane, faPlus, faStar, faTag, faXmark,
} from '@fortawesome/free-solid-svg-icons';
import { faCircle } from '@fortawesome/free-regular-svg-icons';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { api } from '../api/client';
import { AMENITIES, money, propertyTypeLabel, shortMoney } from '../api/labels';
import type { PropertyDetailDto, PropertyForm, PropertySummaryDto, PropertyType } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { DISTRICTS } from '../components/Layout';
import { imgFallback, PropertyCard } from '../components/PropertyCard';
import { ImageUploadButton, Progress } from '../components/ui';
import { geocode } from '../geo';
import { useAction } from '../hooks';
import { amenityIcon, propertyTypeHint, propertyTypeIcon } from '../icons';

const TYPES: PropertyType[] = ['Room', 'Apartment', 'House', 'Villa', 'Office'];
const DA_NANG_CENTER: [number, number] = [16.0544, 108.2022];

const EMPTY: PropertyForm = {
  title: '', description: '', address: '', ward: '', district: '', city: 'Đà Nẵng',
  latitude: null, longitude: null, type: 'Room', area: 20, bedrooms: 1, bathrooms: 1,
  pricePerMonth: 3_000_000, deposit: 3_000_000, amenities: [], imageUrls: [],
};

const STEPS = [
  { key: 'basic', label: 'Loại nhà & giá', icon: faTag },
  { key: 'location', label: 'Vị trí', icon: faLocationDot },
  { key: 'detail', label: 'Mô tả & tiện ích', icon: faListCheck },
  { key: 'photos', label: 'Hình ảnh', icon: faImages },
  { key: 'review', label: 'Xem lại & đăng', icon: faHouseCircleCheck },
] as const;

const DRAFT_KEY = 'toam-post-draft';

/**
 * Trang đăng tin cho thuê riêng (/dang-tin): form chia 5 bước, kiểm tra từng bước,
 * chọn vị trí trên bản đồ, xem trước tin trực tiếp và tự lưu nháp để không mất dữ liệu khi lỡ tải lại trang.
 */
export default function PostListingPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const { busy, run } = useAction();
  const [step, setStep] = useState(0);
  const [errors, setErrors] = useState<string[]>([]);
  const [created, setCreated] = useState<PropertyDetailDto | null>(null);
  const [customAmenity, setCustomAmenity] = useState('');
  const [form, setForm] = useState<PropertyForm>(() => {
    try { return { ...EMPTY, ...JSON.parse(localStorage.getItem(DRAFT_KEY) ?? '{}') }; } catch { return EMPTY; }
  });

  // Tự lưu nháp
  useEffect(() => { if (!created) localStorage.setItem(DRAFT_KEY, JSON.stringify(form)); }, [form, created]);
  useEffect(() => { window.scrollTo({ top: 0, behavior: 'smooth' }); }, [step, created]);

  const set = <K extends keyof PropertyForm>(k: K, v: PropertyForm[K]) => setForm(f => ({ ...f, [k]: v }));
  const images = form.imageUrls ?? [];

  const stepErrors = (i: number): string[] => {
    const e: string[] = [];
    if (i === 0) {
      if (form.pricePerMonth < 100_000) e.push('Giá thuê tối thiểu 100.000 đ');
      if (form.deposit < 0) e.push('Tiền cọc không hợp lệ');
      if (!(form.area > 0)) e.push('Diện tích phải lớn hơn 0');
    }
    if (i === 1) {
      if (!form.address.trim()) e.push('Nhập số nhà, tên đường');
      if (!form.district.trim()) e.push('Chọn quận / huyện');
      if (!form.city.trim()) e.push('Nhập tỉnh / thành phố');
    }
    if (i === 2) {
      if (form.title.trim().length < 10) e.push('Tiêu đề cần ít nhất 10 ký tự');
      if (form.description.trim().length < 30) e.push('Mô tả cần ít nhất 30 ký tự để người thuê hiểu rõ căn nhà');
    }
    if (i === 3 && images.length === 0) e.push('Thêm ít nhất 1 ảnh thật của căn nhà');
    return e;
  };

  const goTo = (target: number) => {
    // Chỉ được đi tới khi các bước trước đã hợp lệ
    for (let i = 0; i < target; i++) {
      const e = stepErrors(i);
      if (e.length) { setStep(i); setErrors(e); return; }
    }
    setErrors([]);
    setStep(target);
  };

  const submit = async () => {
    for (let i = 0; i < 4; i++) {
      const e = stepErrors(i);
      if (e.length) { setStep(i); setErrors(e); return; }
    }
    const res = await run(() => api.post<PropertyDetailDto>('/api/properties', {
      ...form,
      title: form.title.trim(),
      description: form.description.trim(),
      ward: form.ward || undefined,
      latitude: form.latitude ?? undefined,
      longitude: form.longitude ?? undefined,
    }));
    if (res) {
      localStorage.removeItem(DRAFT_KEY);
      setCreated(res);
    }
  };

  const checks = [
    { label: 'Tiêu đề rõ ràng (≥ 10 ký tự)', ok: form.title.trim().length >= 10 },
    { label: 'Mô tả chi tiết (≥ 100 ký tự)', ok: form.description.trim().length >= 100 },
    { label: 'Địa chỉ đầy đủ', ok: !!(form.address && form.district && form.city) },
    { label: 'Đã ghim vị trí trên bản đồ', ok: form.latitude != null && form.longitude != null },
    { label: 'Ít nhất 3 tiện ích', ok: form.amenities.length >= 3 },
    { label: 'Ít nhất 3 ảnh', ok: images.length >= 3 },
  ];
  const done = checks.filter(c => c.ok).length;

  const preview: PropertySummaryDto = {
    id: 0, title: form.title || 'Tiêu đề tin đăng của bạn', address: form.address,
    district: form.district || 'Quận', city: form.city || 'Đà Nẵng', type: form.type, area: form.area,
    bedrooms: form.bedrooms, bathrooms: form.bathrooms, pricePerMonth: form.pricePerMonth, status: 'PendingApproval',
    thumbnailUrl: images[0], averageRating: 0, reviewCount: 0, viewCount: 0, imageCount: images.length,
    landlordId: user?.id ?? 0, landlordName: user?.fullName ?? '', createdAt: new Date().toISOString(),
  };

  if (created) return <PostSuccess property={created} userId={user!.id} onAnother={() => { setForm(EMPTY); setCreated(null); setStep(0); }} />;

  return (
    <div className="post-page">
      <section className="post-hero">
        <div className="container">
          <span className="eyebrow-soft"><FontAwesomeIcon icon={faBullhorn} /> Đăng tin miễn phí</span>
          <h1>Cho thuê nhà nhanh hơn với TổẤm</h1>
          <p>Điền 5 bước đơn giản. Tin được quản trị viên duyệt rồi hiển thị cho mọi người trên trang tìm kiếm, bản đồ và bản tin trên trang cá nhân của bạn.</p>
        </div>
      </section>

      <div className="container post-wrap">
        <ol className="post-stepper" aria-label="Các bước đăng tin">
          {STEPS.map((s, i) => (
            <li key={s.key} className={i === step ? 'current' : i < step ? 'done' : ''}>
              <button type="button" onClick={() => goTo(i)} aria-current={i === step ? 'step' : undefined}>
                <span className="post-stepper-dot">{i < step ? <FontAwesomeIcon icon={faCircleCheck} /> : <FontAwesomeIcon icon={s.icon} />}</span>
                <span className="post-stepper-label"><small>Bước {i + 1}</small>{s.label}</span>
              </button>
            </li>
          ))}
        </ol>

        <div className="post-grid">
          <div className="post-main card pad">
            {errors.length > 0 && (
              <div className="alert alert-error" role="alert">
                <ul className="error-list">{errors.map(e => <li key={e}>{e}</li>)}</ul>
              </div>
            )}

            {step === 0 && (
              <StepSection title="Bạn cho thuê loại nhà nào?" hint="Chọn đúng loại để người thuê tìm thấy tin của bạn">
                <div className="type-choice">
                  {TYPES.map(t => (
                    <button type="button" key={t} className={`type-option ${form.type === t ? 'active' : ''}`} onClick={() => set('type', t)} aria-pressed={form.type === t}>
                      <span className="type-option-icon"><FontAwesomeIcon icon={propertyTypeIcon[t]} /></span>
                      <strong>{propertyTypeLabel[t]}</strong>
                      <small>{propertyTypeHint[t]}</small>
                    </button>
                  ))}
                </div>
                <div className="grid-3 mt">
                  <MoneyField label="Giá thuê / tháng *" value={form.pricePerMonth} onChange={v => set('pricePerMonth', v)} />
                  <MoneyField label="Tiền cọc" value={form.deposit} onChange={v => set('deposit', v)}
                    quick={[1, 2].map(m => ({ label: `${m} tháng`, value: form.pricePerMonth * m }))} />
                  <label className="field"><span>Diện tích (m²) *</span>
                    <input type="number" min={1} step="0.1" value={form.area} onChange={e => set('area', Number(e.target.value))} />
                  </label>
                  <Counter label="Phòng ngủ" value={form.bedrooms} onChange={v => set('bedrooms', v)} />
                  <Counter label="Phòng tắm / WC" value={form.bathrooms} onChange={v => set('bathrooms', v)} />
                </div>
              </StepSection>
            )}

            {step === 1 && (
              <StepSection title="Căn nhà ở đâu?" hint="Ghim đúng vị trí để tin hiện trên bản đồ “Nhà cho thuê gần bạn”">
                <label className="field"><span>Số nhà, tên đường *</span>
                  <input maxLength={300} value={form.address} onChange={e => set('address', e.target.value)} placeholder="Ví dụ: 54 Nguyễn Lương Bằng" />
                </label>
                <div className="grid-3">
                  <label className="field"><span>Phường / Xã</span>
                    <input maxLength={100} value={form.ward ?? ''} onChange={e => set('ward', e.target.value)} placeholder="Hòa Khánh Bắc" />
                  </label>
                  <label className="field"><span>Quận / Huyện *</span>
                    <input list="district-list" maxLength={100} value={form.district} onChange={e => set('district', e.target.value)} placeholder="Chọn hoặc nhập" />
                    <datalist id="district-list">{DISTRICTS.map(d => <option key={d} value={d} />)}</datalist>
                  </label>
                  <label className="field"><span>Tỉnh / Thành phố *</span>
                    <input maxLength={100} value={form.city} onChange={e => set('city', e.target.value)} />
                  </label>
                </div>
                <LocationPicker
                  address={[form.address, form.ward, form.district, form.city].filter(Boolean).join(', ')}
                  lat={form.latitude ?? null}
                  lng={form.longitude ?? null}
                  onChange={(lat, lng) => setForm(f => ({ ...f, latitude: lat, longitude: lng }))}
                />
              </StepSection>
            )}

            {step === 2 && (
              <StepSection title="Giới thiệu căn nhà" hint="Tiêu đề ngắn gọn, mô tả trung thực giúp tin được duyệt nhanh và có khách sớm">
                <label className="field"><span>Tiêu đề tin *</span>
                  <input maxLength={200} value={form.title} onChange={e => set('title', e.target.value)}
                    placeholder={`Ví dụ: ${propertyTypeLabel[form.type]} ${form.area}m² ${form.district ? `gần trung tâm ${form.district}` : 'full nội thất'}`} />
                  <small className="muted">{form.title.length}/200 ký tự</small>
                </label>
                <label className="field"><span>Mô tả chi tiết *</span>
                  <textarea rows={7} maxLength={4000} value={form.description} onChange={e => set('description', e.target.value)}
                    placeholder={'• Nội thất có sẵn: giường, tủ, máy lạnh...\n• Giờ giấc, chỗ để xe, an ninh\n• Chi phí điện, nước, dịch vụ\n• Tiện ích xung quanh: chợ, trường học, siêu thị...'} />
                  <small className="muted">{form.description.length}/4000 ký tự</small>
                </label>
                <div className="field"><span>Tiện ích ({form.amenities.length} đã chọn)</span>
                  <div className="chips-row">
                    {[...new Set([...AMENITIES, ...form.amenities])].map(a => {
                      const on = form.amenities.includes(a);
                      return (
                        <button type="button" key={a} className={`chip ${on ? 'active' : ''}`} aria-pressed={on}
                          onClick={() => set('amenities', on ? form.amenities.filter(x => x !== a) : [...form.amenities, a])}>
                          <FontAwesomeIcon icon={amenityIcon(a)} /> {a}
                        </button>
                      );
                    })}
                  </div>
                  <div className="inline-add">
                    <input placeholder="Tiện ích khác, ví dụ: Camera an ninh" value={customAmenity} onChange={e => setCustomAmenity(e.target.value)}
                      onKeyDown={e => {
                        if (e.key !== 'Enter') return;
                        e.preventDefault();
                        const v = customAmenity.trim();
                        if (v && !form.amenities.includes(v)) set('amenities', [...form.amenities, v]);
                        setCustomAmenity('');
                      }} />
                    <span className="muted small">Enter để thêm</span>
                  </div>
                </div>
              </StepSection>
            )}

            {step === 3 && (
              <StepSection title="Thêm hình ảnh" hint="Ảnh đầu tiên là ảnh đại diện. Nên có ít nhất 3 ảnh thật, đủ sáng (tối đa 20 ảnh, mỗi ảnh ≤ 5MB)">
                <div className="photo-grid">
                  {images.map((url, i) => (
                    <div key={url} className={`photo-tile ${i === 0 ? 'cover' : ''}`}>
                      <img src={url} alt="" onError={imgFallback} />
                      {i === 0 && <span className="image-badge"><FontAwesomeIcon icon={faStar} /> Ảnh đại diện</span>}
                      <div className="image-actions">
                        {i > 0 && (
                          <button type="button" title="Đặt làm ảnh đại diện" aria-label="Đặt làm ảnh đại diện"
                            onClick={() => set('imageUrls', [url, ...images.filter(u => u !== url)])}><FontAwesomeIcon icon={faStar} /></button>
                        )}
                        <button type="button" title="Xóa ảnh" aria-label="Xóa ảnh" onClick={() => set('imageUrls', images.filter(u => u !== url))}>
                          <FontAwesomeIcon icon={faXmark} />
                        </button>
                      </div>
                    </div>
                  ))}
                  {images.length < 20 && (
                    <div className="photo-tile add">
                      <FontAwesomeIcon icon={faCamera} className="photo-add-icon" />
                      <ImageUploadButton label={images.length ? 'Thêm ảnh' : 'Tải ảnh lên'}
                        onUploaded={urls => set('imageUrls', [...images, ...urls].slice(0, 20))} />
                    </div>
                  )}
                </div>
              </StepSection>
            )}

            {step === 4 && (
              <StepSection title="Kiểm tra lại trước khi đăng" hint="Bấm vào từng bước ở trên nếu muốn sửa">
                <dl className="review-list">
                  <dt>Loại nhà</dt><dd>{propertyTypeLabel[form.type]} · {form.area} m² · {form.bedrooms} PN · {form.bathrooms} WC</dd>
                  <dt>Giá thuê</dt><dd><strong className="text-primary">{money(form.pricePerMonth)}/tháng</strong> · cọc {money(form.deposit)}</dd>
                  <dt>Địa chỉ</dt><dd>{[form.address, form.ward, form.district, form.city].filter(Boolean).join(', ')}</dd>
                  <dt>Vị trí bản đồ</dt><dd>{form.latitude != null ? `${form.latitude.toFixed(5)}, ${form.longitude?.toFixed(5)}` : <span className="muted">Chưa ghim (tin sẽ không hiện trên bản đồ)</span>}</dd>
                  <dt>Tiêu đề</dt><dd>{form.title}</dd>
                  <dt>Mô tả</dt><dd className="prewrap">{form.description}</dd>
                  <dt>Tiện ích</dt><dd>{form.amenities.join(', ') || <span className="muted">Chưa chọn</span>}</dd>
                  <dt>Hình ảnh</dt><dd>{images.length} ảnh</dd>
                </dl>
                <div className="alert alert-info"><FontAwesomeIcon icon={faCircleInfo} /> Sau khi đăng, tin ở trạng thái <strong>Chờ duyệt</strong>. Quản trị viên duyệt xong, tin sẽ hiển thị công khai.</div>
              </StepSection>
            )}

            <div className="post-nav">
              {step > 0
                ? <button type="button" className="btn btn-ghost" onClick={() => { setErrors([]); setStep(step - 1); }}><FontAwesomeIcon icon={faArrowLeft} /> Quay lại</button>
                : <button type="button" className="btn btn-ghost" onClick={() => navigate(-1)}>Hủy</button>}
              {step < STEPS.length - 1
                ? <button type="button" className="btn btn-primary" onClick={() => goTo(step + 1)}>Tiếp tục <FontAwesomeIcon icon={faArrowRight} /></button>
                : <button type="button" className="btn btn-primary btn-lg" disabled={busy} onClick={submit}><FontAwesomeIcon icon={faPaperPlane} /> {busy ? 'Đang đăng...' : 'Đăng tin'}</button>}
            </div>
          </div>

          <aside className="post-aside">
            <div className="preview-label">Xem trước trên trang tìm kiếm</div>
            <PropertyCard p={preview} />
            <div className="card pad checklist">
              <div className="checklist-head"><strong>Độ hoàn thiện tin</strong><span className="small">{done}/{checks.length}</span></div>
              <Progress value={done} max={checks.length} tone="orange" />
              <ul>{checks.map(c => <li key={c.label} className={c.ok ? 'ok' : ''}><FontAwesomeIcon icon={c.ok ? faCircleCheck : faCircle} /> {c.label}</li>)}</ul>
              <p className="muted small">Bản nháp được tự động lưu trên máy này.</p>
            </div>
          </aside>
        </div>
      </div>
    </div>
  );
}

function StepSection({ title, hint, children }: { title: string; hint: string; children: ReactNode }) {
  return (
    <section className="step-section">
      <h2>{title}</h2>
      <p className="muted">{hint}</p>
      {children}
    </section>
  );
}

/** Ô nhập tiền có hiển thị dạng "3,5 triệu" và các nút chọn nhanh */
function MoneyField({ label, value, onChange, quick }: {
  label: string; value: number; onChange: (v: number) => void; quick?: { label: string; value: number }[];
}) {
  return (
    <label className="field"><span>{label}</span>
      <input type="number" min={0} step={50_000} value={value} onChange={e => onChange(Number(e.target.value))} />
      <small className="muted">{value > 0 ? `≈ ${shortMoney(value)}` : '—'}
        {quick?.map(q => <button type="button" key={q.label} className="link-btn ml" onClick={() => onChange(q.value)}>{q.label}</button>)}
      </small>
    </label>
  );
}

function Counter({ label, value, onChange }: { label: string; value: number; onChange: (v: number) => void }) {
  return (
    <div className="field"><span>{label}</span>
      <div className="counter">
        <button type="button" onClick={() => onChange(Math.max(0, value - 1))} aria-label={`Giảm ${label}`}>−</button>
        <output>{value}</output>
        <button type="button" onClick={() => onChange(Math.min(50, value + 1))} aria-label={`Tăng ${label}`}><FontAwesomeIcon icon={faPlus} /></button>
      </div>
    </div>
  );
}

/** Bản đồ để ghim vị trí: bấm lên bản đồ, kéo ghim, tìm theo địa chỉ đã nhập hoặc dùng vị trí hiện tại */
function LocationPicker({ address, lat, lng, onChange }: {
  address: string; lat: number | null; lng: number | null; onChange: (lat: number, lng: number) => void;
}) {
  const el = useRef<HTMLDivElement>(null);
  const mapRef = useRef<L.Map | null>(null);
  const markerRef = useRef<L.Marker | null>(null);
  const onChangeRef = useRef(onChange);
  onChangeRef.current = onChange;
  const [message, setMessage] = useState('');
  const [searching, setSearching] = useState(false);

  const pin = (la: number, ln: number) => onChangeRef.current(+la.toFixed(6), +ln.toFixed(6));

  useEffect(() => {
    if (!el.current || mapRef.current) return;
    const map = L.map(el.current, { scrollWheelZoom: false }).setView(lat != null && lng != null ? [lat, lng] : DA_NANG_CENTER, lat != null ? 16 : 13);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19, attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
    }).addTo(map);
    map.on('click', e => pin(e.latlng.lat, e.latlng.lng));
    mapRef.current = map;
    return () => { map.remove(); mapRef.current = null; markerRef.current = null; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Đồng bộ ghim với toạ độ trong form
  useEffect(() => {
    const map = mapRef.current;
    if (!map) return;
    if (lat == null || lng == null) { markerRef.current?.remove(); markerRef.current = null; return; }
    if (!markerRef.current) {
      markerRef.current = L.marker([lat, lng], {
        draggable: true,
        icon: L.divIcon({ className: 'map-pin', html: '<span></span>', iconSize: [30, 30], iconAnchor: [15, 30] }),
      }).addTo(map);
      markerRef.current.on('dragend', () => { const p = markerRef.current!.getLatLng(); pin(p.lat, p.lng); });
    } else {
      markerRef.current.setLatLng([lat, lng]);
    }
  }, [lat, lng]);

  const findAddress = async () => {
    if (!address.trim()) { setMessage('Hãy nhập địa chỉ ở trên trước.'); return; }
    setSearching(true);
    setMessage('');
    const place = await geocode(address);
    setSearching(false);
    if (!place) { setMessage('Không tìm thấy địa chỉ, hãy bấm trực tiếp lên bản đồ để ghim vị trí.'); return; }
    pin(place.lat, place.lng);
    mapRef.current?.setView([place.lat, place.lng], 17);
    setMessage(`Đã ghim tại: ${place.label}. Kéo ghim nếu chưa đúng vị trí.`);
  };

  const myLocation = () => navigator.geolocation?.getCurrentPosition(
    pos => { pin(pos.coords.latitude, pos.coords.longitude); mapRef.current?.setView([pos.coords.latitude, pos.coords.longitude], 17); setMessage('Đã ghim theo vị trí hiện tại của bạn.'); },
    () => setMessage('Không lấy được vị trí. Hãy cho phép truy cập vị trí hoặc bấm lên bản đồ.'),
    { enableHighAccuracy: true, timeout: 10_000 },
  );

  return (
    <div className="field location-picker"><span>Vị trí trên bản đồ</span>
      <div className="location-tools">
        <button type="button" className="btn btn-ghost btn-sm" onClick={findAddress} disabled={searching}>
          <FontAwesomeIcon icon={faMagnifyingGlassLocation} /> {searching ? 'Đang tìm...' : 'Tìm theo địa chỉ đã nhập'}
        </button>
        <button type="button" className="btn btn-ghost btn-sm" onClick={myLocation}><FontAwesomeIcon icon={faLocationCrosshairs} /> Vị trí hiện tại</button>
        <span className="muted small">{lat != null ? `${lat.toFixed(5)}, ${lng?.toFixed(5)}` : 'Hoặc bấm lên bản đồ để ghim'}</span>
      </div>
      <div className="location-map" ref={el} />
      {message && <small className="muted">{message}</small>}
    </div>
  );
}

function PostSuccess({ property, userId, onAnother }: { property: PropertyDetailDto; userId: number; onAnother: () => void }) {
  return (
    <div className="container post-success">
      <div className="card pad post-success-card">
        <span className="post-success-icon"><FontAwesomeIcon icon={faCircleCheck} /></span>
        <h1>Đăng tin thành công!</h1>
        <p>Tin <strong>“{property.title}”</strong> đang <strong>chờ quản trị viên duyệt</strong>.
          Bạn sẽ nhận thông báo khi tin được duyệt; sau đó tin xuất hiện trên trang tìm kiếm, bản đồ và bản tin của bạn.</p>
        <div className="post-success-actions">
          <Link to={`/users/${userId}`} className="btn btn-primary"><FontAwesomeIcon icon={faNewspaper} /> Xem bản tin của tôi</Link>
          <Link to="/landlord/properties" className="btn btn-ghost">Quản lý tin đăng</Link>
          <button type="button" className="btn btn-ghost" onClick={onAnother}><FontAwesomeIcon icon={faPlus} /> Đăng tin khác</button>
        </div>
      </div>
    </div>
  );
}

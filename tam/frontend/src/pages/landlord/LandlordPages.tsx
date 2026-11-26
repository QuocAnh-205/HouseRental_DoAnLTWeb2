import { useEffect, useState, type FormEvent } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faBuildingUser, faBullhorn, faCircleCheck, faEye, faFloppyDisk, faLocationCrosshairs, faPaperPlane, faPenToSquare, faPlus,
  faStar, faXmark,
} from '@fortawesome/free-solid-svg-icons';
import { faCircle } from '@fortawesome/free-regular-svg-icons';
import { amenityIcon } from '../../icons';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api, errorMessage } from '../../api/client';
import { AMENITIES, propertyStatusLabel, propertyTypeLabel } from '../../api/labels';
import type {
  PagedResult, PropertyDetailDto, PropertyForm, PropertyImageDto, PropertyStatus, PropertySummaryDto, PropertyType,
} from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { imgFallback, PropertyCard } from '../../components/PropertyCard';
import { Empty, ErrorBox, ImageUploadButton, Loading, PageHeader, Pagination, Progress, Tabs } from '../../components/ui';
import { toast, useAction, useApi } from '../../hooks';

export function MyPropertiesPage() {
  const [status, setStatus] = useState<PropertyStatus | ''>('');
  const [page, setPage] = useState(1);
  const { busy, run } = useAction();
  const { data, loading, error, reload } = useApi(
    () => api.get<PagedResult<PropertySummaryDto>>('/api/properties/mine', { status, page, pageSize: 12 }), [status, page],
  );

  const tabs = [{ value: '' as const, label: 'Tất cả' },
    ...(Object.keys(propertyStatusLabel) as PropertyStatus[]).map(s => ({ value: s, label: propertyStatusLabel[s] }))];

  const toggle = async (p: PropertySummaryDto) => {
    const next = p.status === 'Hidden' ? 'Available' : 'Hidden';
    const res = await run(() => api.patch(`/api/properties/${p.id}/status`, { status: next }));
    if (res) { toast(next === 'Hidden' ? 'Đã ẩn tin' : 'Đã hiện lại tin'); reload(); }
  };

  const remove = async (p: PropertySummaryDto) => {
    if (!confirm(`Xóa tin "${p.title}"? Thao tác không thể hoàn tác.`)) return;
    const ok = await run(() => api.del(`/api/properties/${p.id}`));
    if (ok !== undefined) { toast('Đã xóa tin đăng'); reload(); }
  };

  return (
    <div className="container">
      <PageHeader icon={faBuildingUser} title="Nhà của tôi" subtitle="Tin mới đăng cần quản trị viên duyệt trước khi hiển thị công khai"
        actions={<Link to="/dang-tin" className="btn btn-primary"><FontAwesomeIcon icon={faPlus} /> Đăng tin mới</Link>} />
      <Tabs value={status} options={tabs} onChange={v => { setStatus(v); setPage(1); }} />
      {error && <ErrorBox error={error} onRetry={reload} />}
      {loading && !data ? <Loading /> : data?.items.length === 0 ? (
        <Empty text="Chưa có tin đăng nào."><Link to="/dang-tin" className="btn btn-primary">Đăng tin đầu tiên</Link></Empty>
      ) : (
        <div className="property-grid">
          {data?.items.map(p => (
            <PropertyCard key={p.id} p={p} showStatus actions={
              <>
                <Link className="btn btn-sm btn-ghost" to={`/landlord/properties/${p.id}/edit`}>Sửa</Link>
                {(p.status === 'Available' || p.status === 'Hidden') && (
                  <button className="btn btn-sm btn-ghost" disabled={busy} onClick={() => toggle(p)}>
                    {p.status === 'Hidden' ? 'Hiện' : 'Ẩn'}
                  </button>
                )}
                <button className="btn btn-sm btn-ghost danger" disabled={busy} onClick={() => remove(p)}>Xóa</button>
              </>
            } />
          ))}
        </div>
      )}
      <Pagination data={data} onPage={setPage} />
    </div>
  );
}

const EMPTY_FORM: PropertyForm = {
  title: '', description: '', address: '', ward: '', district: '', city: 'Đà Nẵng',
  latitude: null, longitude: null, type: 'Room', area: 20, bedrooms: 1, bathrooms: 1,
  pricePerMonth: 3_000_000, deposit: 3_000_000, amenities: [], imageUrls: [],
};

export function PropertyFormPage() {
  const { id } = useParams();
  const isEdit = !!id;
  const navigate = useNavigate();
  const { user } = useAuth();
  const { busy, run } = useAction();
  const [form, setForm] = useState<PropertyForm>(EMPTY_FORM);
  const [images, setImages] = useState<PropertyImageDto[]>([]);
  const [customAmenity, setCustomAmenity] = useState('');
  const [loadError, setLoadError] = useState('');
  const [loading, setLoading] = useState(isEdit);
  const [status, setStatus] = useState<PropertyStatus | null>(null);

  useEffect(() => {
    if (!id) return;
    api.get<PropertyDetailDto>(`/api/properties/${id}`)
      .then(p => {
        setForm({
          title: p.title, description: p.description, address: p.address, ward: p.ward ?? '', district: p.district,
          city: p.city, latitude: p.latitude ?? null, longitude: p.longitude ?? null, type: p.type, area: p.area,
          bedrooms: p.bedrooms, bathrooms: p.bathrooms, pricePerMonth: p.pricePerMonth, deposit: p.deposit,
          amenities: p.amenities,
        });
        setImages(p.images);
        setStatus(p.status);
        if (user?.role !== 'Admin' && p.landlord.id !== user?.id) setLoadError('Bạn không có quyền sửa tin này');
      })
      .catch(err => setLoadError(errorMessage(err)))
      .finally(() => setLoading(false));
  }, [id, user]);

  const set = <K extends keyof PropertyForm>(k: K, v: PropertyForm[K]) => setForm(f => ({ ...f, [k]: v }));
  const numField = (k: 'area' | 'bedrooms' | 'bathrooms' | 'pricePerMonth' | 'deposit') => ({
    value: form[k], onChange: (e: React.ChangeEvent<HTMLInputElement>) => set(k, Number(e.target.value)),
  });

  const toggleAmenity = (a: string) =>
    set('amenities', form.amenities.includes(a) ? form.amenities.filter(x => x !== a) : [...form.amenities, a]);

  const useMyLocation = () => {
    navigator.geolocation?.getCurrentPosition(
      pos => setForm(f => ({ ...f, latitude: +pos.coords.latitude.toFixed(6), longitude: +pos.coords.longitude.toFixed(6) })),
      () => toast('Không lấy được vị trí', 'error'),
    );
  };

  // ===== Ảnh khi sửa tin: gọi API trực tiếp =====
  const addImages = async (urls: string[]) => {
    if (!isEdit) {
      set('imageUrls', [...(form.imageUrls ?? []), ...urls].slice(0, 20));
      return;
    }
    for (const url of urls) {
      const img = await run(() => api.post<PropertyImageDto>(`/api/properties/${id}/images`, { url, isThumbnail: images.length === 0 }));
      if (img) setImages(list => [...list, img]);
    }
  };

  const removeImage = async (img: PropertyImageDto) => {
    const ok = await run(() => api.del(`/api/properties/${id}/images/${img.id}`));
    if (ok !== undefined) {
      const res = await api.get<PropertyDetailDto>(`/api/properties/${id}`);
      setImages(res.images);
    }
  };

  const setThumbnail = async (img: PropertyImageDto) => {
    const ok = await run(() => api.patch(`/api/properties/${id}/images/${img.id}/thumbnail`));
    if (ok !== undefined) setImages(list => list.map(x => ({ ...x, isThumbnail: x.id === img.id })));
  };

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    const body = {
      ...form,
      ward: form.ward || undefined,
      latitude: form.latitude ?? undefined,
      longitude: form.longitude ?? undefined,
    };
    const res = isEdit
      ? await run(() => api.put<PropertyDetailDto>(`/api/properties/${id}`, body))
      : await run(() => api.post<PropertyDetailDto>('/api/properties', body));
    if (res) {
      toast(isEdit ? 'Đã cập nhật tin đăng' : 'Đã đăng tin, vui lòng chờ quản trị viên duyệt');
      navigate(user?.role === 'Admin' ? `/properties/${res.id}` : '/landlord/properties');
    }
  };

  if (loading) return <Loading />;
  if (loadError) return <div className="container"><ErrorBox error={loadError} /></div>;

  const imageCount = isEdit ? images.length : (form.imageUrls?.length ?? 0);
  const preview: PropertySummaryDto = {
    id: Number(id) || 0,
    title: form.title || 'Tiêu đề tin đăng của bạn',
    address: form.address, district: form.district || 'Quận', city: form.city || 'Thành phố',
    type: form.type, area: form.area, bedrooms: form.bedrooms, bathrooms: form.bathrooms,
    pricePerMonth: form.pricePerMonth, status: status ?? 'PendingApproval',
    thumbnailUrl: isEdit ? (images.find(i => i.isThumbnail) ?? images[0])?.url : form.imageUrls?.[0],
    averageRating: 0, reviewCount: 0, viewCount: 0,
    landlordId: user?.id ?? 0, landlordName: user?.fullName ?? '', createdAt: new Date().toISOString(),
  };
  const checks = [
    { label: 'Tiêu đề từ 10 ký tự', ok: form.title.trim().length >= 10 },
    { label: 'Mô tả chi tiết (từ 100 ký tự)', ok: form.description.trim().length >= 100 },
    { label: 'Địa chỉ đầy đủ', ok: !!(form.address && form.district && form.city) },
    { label: 'Có tọa độ bản đồ', ok: form.latitude != null && form.longitude != null },
    { label: 'Ít nhất 3 tiện ích', ok: form.amenities.length >= 3 },
    { label: 'Ít nhất 3 ảnh', ok: imageCount >= 3 },
  ];
  const doneCount = checks.filter(c => c.ok).length;

  return (
    <div className="container">
      <PageHeader icon={isEdit ? faPenToSquare : faBullhorn} title={isEdit ? 'Sửa tin đăng' : 'Đăng tin cho thuê'}
        subtitle={status ? `Trạng thái hiện tại: ${propertyStatusLabel[status]}` : 'Điền đầy đủ thông tin để tin được duyệt nhanh hơn'} />
      <form className="form-layout" onSubmit={submit}>
        <div className="form-col">
        <section className="card pad form-section">
        <h3 className="card-title"><span className="step-num">1</span> Thông tin cơ bản</h3>
        <label className="field"><span>Tiêu đề *</span>
          <input required minLength={10} maxLength={200} value={form.title} onChange={e => set('title', e.target.value)}
            placeholder="Ví dụ: Căn hộ 2PN view biển, full nội thất" />
        </label>
        <div className="grid-3">
          <label className="field"><span>Loại nhà *</span>
            <select value={form.type} onChange={e => set('type', e.target.value as PropertyType)}>
              {Object.entries(propertyTypeLabel).map(([v, l]) => <option key={v} value={v}>{l}</option>)}
            </select>
          </label>
          <label className="field"><span>Giá thuê / tháng (đ) *</span><input type="number" required min={100000} step={50000} {...numField('pricePerMonth')} /></label>
          <label className="field"><span>Tiền cọc (đ)</span><input type="number" min={0} step={50000} {...numField('deposit')} /></label>
          <label className="field"><span>Diện tích (m²) *</span><input type="number" required min={1} step="0.1" {...numField('area')} /></label>
          <label className="field"><span>Phòng ngủ</span><input type="number" min={0} max={50} {...numField('bedrooms')} /></label>
          <label className="field"><span>Phòng tắm</span><input type="number" min={0} max={50} {...numField('bathrooms')} /></label>
        </div>
        <label className="field"><span>Mô tả</span>
          <textarea rows={6} maxLength={4000} value={form.description} onChange={e => set('description', e.target.value)}
            placeholder="Mô tả nội thất, giờ giấc, chỗ để xe, khu vực xung quanh... Mô tả càng chi tiết càng dễ có khách." />
          <small className="muted">{form.description.length}/4000 ký tự</small>
        </label>

        </section>
        <section className="card pad form-section">
        <h3 className="card-title"><span className="step-num">2</span> Địa chỉ</h3>
        <label className="field"><span>Số nhà, tên đường *</span>
          <input required maxLength={300} value={form.address} onChange={e => set('address', e.target.value)} />
        </label>
        <div className="grid-3">
          <label className="field"><span>Phường / Xã</span><input maxLength={100} value={form.ward} onChange={e => set('ward', e.target.value)} /></label>
          <label className="field"><span>Quận / Huyện *</span><input required maxLength={100} value={form.district} onChange={e => set('district', e.target.value)} /></label>
          <label className="field"><span>Tỉnh / Thành phố *</span><input required maxLength={100} value={form.city} onChange={e => set('city', e.target.value)} /></label>
        </div>
        <div className="grid-3">
          <label className="field"><span>Vĩ độ</span>
            <input type="number" step="any" min={-90} max={90} value={form.latitude ?? ''} onChange={e => set('latitude', e.target.value === '' ? null : Number(e.target.value))} />
          </label>
          <label className="field"><span>Kinh độ</span>
            <input type="number" step="any" min={-180} max={180} value={form.longitude ?? ''} onChange={e => set('longitude', e.target.value === '' ? null : Number(e.target.value))} />
          </label>
          <div className="field"><span>&nbsp;</span><button type="button" className="btn btn-ghost" onClick={useMyLocation}><FontAwesomeIcon icon={faLocationCrosshairs} /> Dùng vị trí hiện tại</button></div>
        </div>

        </section>
        <section className="card pad form-section">
        <h3 className="card-title"><span className="step-num">3</span> Tiện ích</h3>
        <div className="chips-row">
          {[...new Set([...AMENITIES, ...form.amenities])].map(a => (
            <button type="button" key={a} className={`chip ${form.amenities.includes(a) ? 'active' : ''}`} onClick={() => toggleAmenity(a)}><FontAwesomeIcon icon={amenityIcon(a)} /> {a}</button>
          ))}
        </div>
        <div className="inline-add">
          <input placeholder="Tiện ích khác..." value={customAmenity} onChange={e => setCustomAmenity(e.target.value)}
            onKeyDown={e => {
              if (e.key === 'Enter') {
                e.preventDefault();
                const v = customAmenity.trim();
                if (v && !form.amenities.includes(v)) set('amenities', [...form.amenities, v]);
                setCustomAmenity('');
              }
            }} />
          <span className="muted small">Nhấn Enter để thêm</span>
        </div>

        </section>
        <section className="card pad form-section">
        <h3 className="card-title"><span className="step-num">4</span> Hình ảnh <span className="muted small">(ảnh có dấu sao là ảnh đại diện, tối đa 20)</span></h3>
        <div className="image-grid">
          {isEdit ? images.map(img => (
            <div key={img.id} className={`image-tile ${img.isThumbnail ? 'thumb' : ''}`}>
              <img src={img.url} alt="" onError={imgFallback} />
              {img.isThumbnail && <span className="image-badge">Ảnh đại diện</span>}
              <div className="image-actions">
                {!img.isThumbnail && <button type="button" onClick={() => setThumbnail(img)} disabled={busy} title="Đặt làm ảnh đại diện"><FontAwesomeIcon icon={faStar} /></button>}
                <button type="button" onClick={() => removeImage(img)} disabled={busy} title="Xóa ảnh"><FontAwesomeIcon icon={faXmark} /></button>
              </div>
            </div>
          )) : (form.imageUrls ?? []).map((url, i) => (
            <div key={url} className={`image-tile ${i === 0 ? 'thumb' : ''}`}>
              <img src={url} alt="" onError={imgFallback} />
              {i === 0 && <span className="image-badge">Ảnh đại diện</span>}
              <div className="image-actions">
                {i > 0 && <button type="button" onClick={() => set('imageUrls', [url, ...(form.imageUrls ?? []).filter(u => u !== url)])} title="Đặt làm ảnh đại diện"><FontAwesomeIcon icon={faStar} /></button>}
                <button type="button" onClick={() => set('imageUrls', (form.imageUrls ?? []).filter(u => u !== url))} title="Xóa ảnh"><FontAwesomeIcon icon={faXmark} /></button>
              </div>
            </div>
          ))}
          <div className="image-tile add">
            <ImageUploadButton onUploaded={addImages} />
          </div>
        </div>

        </section>
        </div>

        <aside className="form-aside">
          <div className="preview-label"><FontAwesomeIcon icon={faEye} /> Xem trước tin đăng</div>
          <PropertyCard p={preview} />
          <div className="card pad checklist">
            <div className="checklist-head">
              <strong>Độ hoàn thiện tin</strong>
              <span className="small">{doneCount}/{checks.length}</span>
            </div>
            <Progress value={doneCount} max={checks.length} tone="orange" />
            <ul>
              {checks.map(c => (
                <li key={c.label} className={c.ok ? 'ok' : ''}><FontAwesomeIcon icon={c.ok ? faCircleCheck : faCircle} /> {c.label}</li>
              ))}
            </ul>
            <p className="muted small">Tin đầy đủ thông tin và nhiều ảnh thật thường được duyệt nhanh và có khách sớm hơn.</p>
          </div>
          <div className="form-aside-actions">
            <button className="btn btn-primary btn-block btn-lg" disabled={busy}>
              <FontAwesomeIcon icon={isEdit ? faFloppyDisk : faPaperPlane} /> {isEdit ? 'Lưu thay đổi' : 'Đăng tin'}
            </button>
            <button type="button" className="btn btn-ghost btn-block" onClick={() => navigate(-1)}>Hủy</button>
          </div>
        </aside>
      </form>
    </div>
  );
}

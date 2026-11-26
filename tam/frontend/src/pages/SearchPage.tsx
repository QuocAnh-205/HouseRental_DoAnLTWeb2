import { Fragment, useEffect, useRef, useState, type FormEvent, type ReactNode } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faBars, faChevronDown, faChevronUp, faGrip, faList, faLocationCrosshairs, faMagnifyingGlass, faRotateLeft,
  faSliders, faXmark,
} from '@fortawesome/free-solid-svg-icons';
import { api } from '../api/client';
import { AMENITIES, propertyTypeLabel, shortMoney, SORT_OPTIONS } from '../api/labels';
import type { PagedResult, PropertyStatsDto, PropertySummaryDto, PropertyType } from '../api/types';
import { ListingRow, ListingRowSkeleton } from '../components/ListingRow';
import { PropertyCard, PropertyCardSkeleton } from '../components/PropertyCard';
import { Empty, ErrorBox, Pagination } from '../components/ui';
import { amenityIcon, propertyTypeIcon } from '../icons';
import { useApi } from '../hooks';

const FILTER_KEYS = ['keyword', 'city', 'district', 'type', 'minPrice', 'maxPrice', 'minArea', 'maxArea', 'minBedrooms', 'minBathrooms'] as const;
type FilterKey = (typeof FILTER_KEYS)[number];
type Patch = Record<string, string | string[] | undefined>;

const TYPES = Object.keys(propertyTypeLabel) as PropertyType[];

const PRICE_CHIPS = [
  { label: 'Dưới 1 triệu', min: '', max: '1000000' },
  { label: '1 - 2 triệu', min: '1000000', max: '2000000' },
  { label: '2 - 3 triệu', min: '2000000', max: '3000000' },
  { label: '3 - 5 triệu', min: '3000000', max: '5000000' },
  { label: '5 - 7 triệu', min: '5000000', max: '7000000' },
  { label: '7 - 15 triệu', min: '7000000', max: '15000000' },
  { label: 'Trên 15 triệu', min: '15000000', max: '' },
];

const AREA_CHIPS = [
  { label: 'Dưới 20 m²', min: '', max: '20' },
  { label: '20 - 30 m²', min: '20', max: '30' },
  { label: '30 - 50 m²', min: '30', max: '50' },
  { label: '50 - 100 m²', min: '50', max: '100' },
  { label: 'Trên 100 m²', min: '100', max: '' },
];

const FILTER_LABEL: Record<FilterKey, (v: string) => string> = {
  keyword: v => `"${v}"`,
  city: v => v,
  district: v => v,
  type: v => propertyTypeLabel[v as PropertyType] ?? v,
  minPrice: v => `Từ ${shortMoney(+v)}`,
  maxPrice: v => `Đến ${shortMoney(+v)}`,
  minArea: v => `Từ ${v} m²`,
  maxArea: v => `Đến ${v} m²`,
  minBedrooms: v => `${v}+ phòng ngủ`,
  minBathrooms: v => `${v}+ phòng tắm`,
};

const rangeLabel = (min: string | null, max: string | null, fmt: (v: number) => string) =>
  min && max ? `${fmt(+min)} - ${fmt(+max)}` : min ? `Từ ${fmt(+min)}` : max ? `Dưới ${fmt(+max)}` : '';

const VIEW_KEY = 'hr_search_view';

export default function SearchPage() {
  const [params, setParams] = useSearchParams();
  const page = Number(params.get('page') ?? 1);
  const sortBy = params.get('sortBy') ?? 'newest';
  const amenities = params.getAll('amenities');
  const near = !!(params.get('lat') && params.get('lng'));
  const type = params.get('type') as PropertyType | null;
  const city = params.get('city');
  const [keyword, setKeyword] = useState(params.get('keyword') ?? '');
  const [view, setView] = useState<'list' | 'grid'>(() => (localStorage.getItem(VIEW_KEY) as 'list' | 'grid') ?? 'list');
  const [drawer, setDrawer] = useState(false);

  useEffect(() => { setKeyword(params.get('keyword') ?? ''); }, [params]);
  useEffect(() => { localStorage.setItem(VIEW_KEY, view); }, [view]);

  const { data, loading, error, reload } = useApi(
    () => api.get<PagedResult<PropertySummaryDto>>('/api/properties', {
      ...Object.fromEntries(FILTER_KEYS.map(k => [k, params.get(k) ?? undefined])),
      amenities,
      latitude: params.get('lat') ?? undefined,
      longitude: params.get('lng') ?? undefined,
      radiusKm: near ? (params.get('radius') ?? 5) : undefined,
      sortBy,
      page,
      pageSize: 15,
    }),
    [params.toString()],
  );

  // Số tin theo loại nhà (dải danh mục) và theo quận (cột link bên phải)
  const { data: typeStats } = useApi(
    () => api.get<PropertyStatsDto>('/api/properties/stats', { city: city ?? undefined }), [city],
  );
  const { data: districtStats } = useApi(
    () => api.get<PropertyStatsDto>('/api/properties/stats', { city: city ?? undefined, type: type ?? undefined }), [city, type],
  );
  const areaCounts = (districtStats?.byDistrict ?? []).map(x => [x.key, x.count] as const);
  const typeCount = (t: PropertyType) => typeStats?.byType.find(x => x.key === t)?.count ?? 0;

  const apply = (patch: Patch) => {
    const next = new URLSearchParams(params);
    for (const [k, v] of Object.entries(patch)) {
      next.delete(k);
      if (Array.isArray(v)) v.forEach(x => next.append(k, x));
      else if (v) next.set(k, v);
    }
    if (!('page' in patch)) next.delete('page');
    setParams(next);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };
  const reset = () => setParams(new URLSearchParams());

  const findNearMe = () => {
    if (!navigator.geolocation) return alert('Trình duyệt không hỗ trợ định vị');
    navigator.geolocation.getCurrentPosition(
      pos => apply({ lat: pos.coords.latitude.toFixed(6), lng: pos.coords.longitude.toFixed(6), radius: params.get('radius') ?? '5', sortBy: 'distance' }),
      () => alert('Không lấy được vị trí của bạn'),
    );
  };

  const activeChips = FILTER_KEYS.filter(k => params.get(k));
  const filterCount = activeChips.length + amenities.length + (near ? 1 : 0);
  const typeName = type ? propertyTypeLabel[type].toLowerCase() : 'nhà';
  const district = params.get('district');
  const where = [district, city ?? 'Đà Nẵng'].filter(Boolean).join(', ');
  const now = new Date();
  const title = `Thuê ${typeName} ${where} giá rẻ, mới nhất T${now.getMonth() + 1}/${now.getFullYear()}`;
  const listLink = (patch: Record<string, string>) => {
    const p = new URLSearchParams();
    if (type) p.set('type', type);
    if (city) p.set('city', city);
    Object.entries(patch).forEach(([k, v]) => v && p.set(k, v));
    return `/search?${p}`;
  };

  return (
    <div className="container search-page">
      {/* ===== Khung đầu: tiêu đề + thanh lọc ===== */}
      <div className="breadcrumb">
        <Link to="/">TổẤm</Link> <span>›</span> <Link to="/search">Đà Nẵng</Link>
        {type && <> <span>›</span> <Link to={`/search?type=${type}`}>Thuê {typeName}</Link></>}
        {district && <> <span>›</span> <span>{district}</span></>}
      </div>
      <section className="search-top card">
        <h1 className="search-title">{title}</h1>
        <p className="search-intro">
          {data
            ? <>Hiện có <strong>{data.totalCount.toLocaleString('vi-VN')}</strong> tin cho thuê {typeName} tại {where}, cập nhật ngày {now.toLocaleDateString('vi-VN')}.
              Xem ảnh thật, nhắn tin chủ nhà, đặt lịch xem và gửi yêu cầu thuê ngay trên web.</>
            : 'Đang tải danh sách tin đăng...'}
        </p>

        <form className="search-bar" onSubmit={(e: FormEvent) => { e.preventDefault(); apply({ keyword: keyword.trim() || undefined }); }}>
          <FontAwesomeIcon icon={faMagnifyingGlass} className="search-bar-icon" />
          <input placeholder="Tìm theo tên đường, khu vực, tiêu đề..." title="Tìm theo tên đường, khu vực, tiêu đề..."
            aria-label="Từ khóa tìm kiếm" value={keyword} onChange={e => setKeyword(e.target.value)} />
          {keyword && (
            <button type="button" className="search-bar-clear" aria-label="Xóa từ khóa"
              onClick={() => { setKeyword(''); if (params.get('keyword')) apply({ keyword: undefined }); }}>
              <FontAwesomeIcon icon={faXmark} />
            </button>
          )}
          <button className="search-bar-btn">
            <FontAwesomeIcon icon={faMagnifyingGlass} /><span>Tìm kiếm</span>
          </button>
        </form>

        <div className="type-strip">
          <button className={!type ? 'on' : ''} onClick={() => apply({ type: undefined })}>
            Tất cả <span>{typeStats?.total ?? ''}</span>
          </button>
          {TYPES.map(t => (
            <button key={t} className={type === t ? 'on' : ''} onClick={() => apply({ type: t })}>
              <FontAwesomeIcon icon={propertyTypeIcon[t]} /> {propertyTypeLabel[t]} <span>{typeStats ? typeCount(t) : ''}</span>
            </button>
          ))}
        </div>

        <div className="filter-row">
          <button className={`fchip ${filterCount ? 'on' : ''}`} onClick={() => setDrawer(true)}>
            <FontAwesomeIcon icon={faSliders} /> Lọc{filterCount ? ` (${filterCount})` : ''}
          </button>

          <Dropdown label={type ? propertyTypeLabel[type] : 'Loại nhà'} active={!!type}>
            {close => (
              <div className="dd-list">
                <button className={!type ? 'on' : ''} onClick={() => { apply({ type: undefined }); close(); }}>Tất cả loại nhà</button>
                {TYPES.map(t => (
                  <button key={t} className={type === t ? 'on' : ''} onClick={() => { apply({ type: t }); close(); }}>
                    <FontAwesomeIcon icon={propertyTypeIcon[t]} fixedWidth /> {propertyTypeLabel[t]}
                  </button>
                ))}
              </div>
            )}
          </Dropdown>

          <Dropdown label={rangeLabel(params.get('minPrice'), params.get('maxPrice'), shortMoney) || 'Giá thuê'} active={!!(params.get('minPrice') || params.get('maxPrice'))}>
            {close => <RangeForm unit="đ" step={500000} presets={PRICE_CHIPS} min={params.get('minPrice')} max={params.get('maxPrice')}
              onApply={(min, max) => { apply({ minPrice: min, maxPrice: max }); close(); }} />}
          </Dropdown>

          <Dropdown label={rangeLabel(params.get('minArea'), params.get('maxArea'), v => `${v} m²`) || 'Diện tích'} active={!!(params.get('minArea') || params.get('maxArea'))}>
            {close => <RangeForm unit="m²" step={5} presets={AREA_CHIPS} min={params.get('minArea')} max={params.get('maxArea')}
              onApply={(min, max) => { apply({ minArea: min, maxArea: max }); close(); }} />}
          </Dropdown>

          <Dropdown label={params.get('minBedrooms') ? `${params.get('minBedrooms')}+ phòng ngủ` : 'Phòng ngủ'} active={!!params.get('minBedrooms')}>
            {close => (
              <div className="dd-pad">
                <div className="seg">
                  {['', '1', '2', '3', '4'].map(n => (
                    <button key={n} type="button" className={(params.get('minBedrooms') ?? '') === n ? 'active' : ''}
                      onClick={() => { apply({ minBedrooms: n || undefined }); close(); }}>{n ? `${n}+` : 'Bất kỳ'}</button>
                  ))}
                </div>
              </div>
            )}
          </Dropdown>

          <Dropdown label={amenities.length ? `Tiện ích (${amenities.length})` : 'Tiện ích'} active={amenities.length > 0} wide>
            {close => <AmenityForm initial={amenities} onApply={list => { apply({ amenities: list }); close(); }} />}
          </Dropdown>

          <Dropdown label={where || 'Khu vực'} active={!!where}>
            {close => <AreaForm city={city ?? ''} district={params.get('district') ?? ''}
              onApply={(c, d) => { apply({ city: c || undefined, district: d || undefined }); close(); }}
              onNearMe={() => { findNearMe(); close(); }} />}
          </Dropdown>

          {filterCount > 0 && <button className="fchip-clear" onClick={reset}><FontAwesomeIcon icon={faRotateLeft} /> Xóa lọc</button>}
        </div>

        <div className="price-chips">
          <span>Giá thuê:</span>
          {PRICE_CHIPS.map(c => {
            const on = (params.get('minPrice') ?? '') === c.min && (params.get('maxPrice') ?? '') === c.max;
            return (
              <button key={c.label} className={`pchip ${on ? 'on' : ''}`}
                onClick={() => apply(on ? { minPrice: undefined, maxPrice: undefined } : { minPrice: c.min || undefined, maxPrice: c.max || undefined })}>
                {c.label}
              </button>
            );
          })}
        </div>
      </section>

      <div className="search-grid">
        {/* ===== Danh sách tin ===== */}
        <section className="results card">
          <div className="results-head">
            <div className="results-count">
              <span>{data ? <><strong>{data.totalCount}</strong> tin đăng</> : 'Đang tải...'}</span>
              {(activeChips.length > 0 || amenities.length > 0) && (
                <div className="active-chips">
                  {activeChips.map(k => (
                    <span key={k} className="chip active">
                      {FILTER_LABEL[k](params.get(k)!)}
                      <button className="chip-x" onClick={() => apply({ [k]: undefined })} aria-label="Bỏ lọc"><FontAwesomeIcon icon={faXmark} /></button>
                    </span>
                  ))}
                  {amenities.map(a => (
                    <span key={a} className="chip active">
                      {a}
                      <button className="chip-x" onClick={() => apply({ amenities: amenities.filter(x => x !== a) })}><FontAwesomeIcon icon={faXmark} /></button>
                    </span>
                  ))}
                </div>
              )}
            </div>
            <div className="results-tools">
              {near && (
                <select className="sort-select" value={params.get('radius') ?? '5'} onChange={e => apply({ radius: e.target.value })}>
                  {['1', '2', '5', '10', '20'].map(r => <option key={r} value={r}>Bán kính {r} km</option>)}
                </select>
              )}
              <select className="sort-select" value={sortBy} onChange={e => apply({ sortBy: e.target.value })}>
                {SORT_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
                {near && <option value="distance">Gần nhất</option>}
              </select>
              <div className="view-toggle">
                <button className={view === 'list' ? 'on' : ''} onClick={() => setView('list')} title="Dạng danh sách"><FontAwesomeIcon icon={faList} /></button>
                <button className={view === 'grid' ? 'on' : ''} onClick={() => setView('grid')} title="Dạng lưới"><FontAwesomeIcon icon={faGrip} /></button>
              </div>
            </div>
          </div>

          {error && <ErrorBox error={error} onRetry={reload} />}
          {loading && !data ? (
            view === 'list'
              ? <div className="listing-list">{Array.from({ length: 4 }, (_, i) => <ListingRowSkeleton key={i} />)}</div>
              : <div className="property-grid">{Array.from({ length: 6 }, (_, i) => <PropertyCardSkeleton key={i} />)}</div>
          ) : data && data.items.length === 0 ? (
            <Empty text="Chưa có tin nào khớp với bộ lọc. Thử nới rộng khoảng giá hoặc bỏ bớt tiện ích nhé.">
              <button className="btn btn-primary" onClick={reset}><FontAwesomeIcon icon={faRotateLeft} /> Xóa bộ lọc</button>
            </Empty>
          ) : view === 'list' ? (
            <div className={`listing-list ${loading ? 'is-loading' : ''}`}>
              {data?.items.map((p, i) => (
                <Fragment key={p.id}>
                  <ListingRow p={p} />
                  {i === 4 && data.items.length > 6 && (
                    <div className="inline-picker">
                      <strong>Bạn cần tìm giá thuê nào?</strong>
                      <div className="price-chips">
                        <span>Giá thuê:</span>
                        {PRICE_CHIPS.map(c => (
                          <button key={c.label} className="pchip" onClick={() => apply({ minPrice: c.min || undefined, maxPrice: c.max || undefined })}>{c.label}</button>
                        ))}
                      </div>
                    </div>
                  )}
                </Fragment>
              ))}
            </div>
          ) : (
            <div className={`property-grid ${loading ? 'is-loading' : ''}`}>
              {data?.items.map(p => <PropertyCard key={p.id} p={p} />)}
            </div>
          )}
          <Pagination data={data} onPage={p => apply({ page: String(p) })} />
        </section>

        {/* ===== Cột link bên phải ===== */}
        <aside className="search-aside">
          <LinkGroup title={`Thuê ${typeName} theo giá`}>
            {PRICE_CHIPS.map(c => <Link key={c.label} to={listLink({ minPrice: c.min, maxPrice: c.max })}>Thuê {typeName} giá {c.label.toLowerCase()}</Link>)}
          </LinkGroup>
          <LinkGroup title={`Thuê ${typeName} theo diện tích`}>
            {AREA_CHIPS.map(c => <Link key={c.label} to={listLink({ minArea: c.min, maxArea: c.max })}>{type ? propertyTypeLabel[type] : 'Nhà'} {c.label.toLowerCase()}</Link>)}
          </LinkGroup>
          {areaCounts.length > 0 && (
            <LinkGroup title={`Thuê ${typeName} theo quận ở ${city ?? 'Đà Nẵng'}`}>
              {areaCounts.map(([name, count]) => (
                <Link key={name} to={listLink({ district: name })} className={district === name ? 'on' : ''}>
                  Thuê {typeName} {name} <span className="muted">({count})</span>
                </Link>
              ))}
            </LinkGroup>
          )}
          <LinkGroup title="Thuê theo loại hình">
            {TYPES.map(t => (
              <Link key={t} to={`/search?type=${t}${city ? `&city=${encodeURIComponent(city)}` : ''}`}>
                <FontAwesomeIcon icon={propertyTypeIcon[t]} fixedWidth /> {propertyTypeLabel[t]}{city ? ` ${city}` : ''}
              </Link>
            ))}
          </LinkGroup>
          <LinkGroup title="Theo tiện ích" collapsed>
            {AMENITIES.slice(0, 10).map(a => (
              <Link key={a} to={`${listLink({})}&amenities=${encodeURIComponent(a)}`}><FontAwesomeIcon icon={amenityIcon(a)} fixedWidth /> {typeName[0].toUpperCase() + typeName.slice(1)} có {a.toLowerCase()}</Link>
            ))}
          </LinkGroup>
        </aside>
      </div>

      {drawer && (
        <FilterDrawer params={params} onClose={() => setDrawer(false)} onReset={() => { reset(); setDrawer(false); }}
          onApply={patch => { apply(patch); setDrawer(false); }} onNearMe={() => { findNearMe(); setDrawer(false); }} />
      )}
    </div>
  );
}

// ===================== Thành phần phụ =====================

function Dropdown({ label, active, wide, children }: {
  label: string; active?: boolean; wide?: boolean; children: (close: () => void) => ReactNode;
}) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    if (!open) return;
    const onDown = (e: MouseEvent) => { if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false); };
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && setOpen(false);
    document.addEventListener('mousedown', onDown);
    document.addEventListener('keydown', onKey);
    return () => { document.removeEventListener('mousedown', onDown); document.removeEventListener('keydown', onKey); };
  }, [open]);
  return (
    <div className="dd" ref={ref}>
      <button type="button" className={`fchip ${active ? 'on' : ''} ${open ? 'open' : ''}`} onClick={() => setOpen(o => !o)}>
        <span className="fchip-label">{label}</span> <FontAwesomeIcon icon={open ? faChevronUp : faChevronDown} className="fchip-caret" />
      </button>
      {open && <div className={`dd-menu ${wide ? 'wide' : ''}`}>{children(() => setOpen(false))}</div>}
    </div>
  );
}

function RangeForm({ unit, step, presets, min, max, onApply }: {
  unit: string; step: number; presets: { label: string; min: string; max: string }[];
  min: string | null; max: string | null; onApply: (min?: string, max?: string) => void;
}) {
  const [lo, setLo] = useState(min ?? '');
  const [hi, setHi] = useState(max ?? '');
  return (
    <form className="dd-pad" onSubmit={e => { e.preventDefault(); onApply(lo || undefined, hi || undefined); }}>
      <div className="range-row">
        <input type="number" min={0} step={step} placeholder={`Từ (${unit})`} value={lo} onChange={e => setLo(e.target.value)} autoFocus />
        <span>–</span>
        <input type="number" min={0} step={step} placeholder={`Đến (${unit})`} value={hi} onChange={e => setHi(e.target.value)} />
      </div>
      <div className="chips-row tight">
        {presets.map(p => (
          <button type="button" key={p.label} className={`chip ${lo === p.min && hi === p.max ? 'active' : ''}`}
            onClick={() => { setLo(p.min); setHi(p.max); }}>{p.label}</button>
        ))}
      </div>
      <div className="dd-actions">
        <button type="button" className="btn btn-ghost btn-sm" onClick={() => onApply(undefined, undefined)}>Bỏ chọn</button>
        <button className="btn btn-primary btn-sm">Áp dụng</button>
      </div>
    </form>
  );
}

function AmenityForm({ initial, onApply }: { initial: string[]; onApply: (list: string[]) => void }) {
  const [sel, setSel] = useState(initial);
  return (
    <div className="dd-pad">
      <div className="amenity-checks">
        {AMENITIES.map(a => (
          <label key={a} className={`amenity-check ${sel.includes(a) ? 'active' : ''}`}>
            <input type="checkbox" checked={sel.includes(a)} onChange={() => setSel(s => s.includes(a) ? s.filter(x => x !== a) : [...s, a])} />
            <FontAwesomeIcon icon={amenityIcon(a)} /> {a}
          </label>
        ))}
      </div>
      <div className="dd-actions">
        <button type="button" className="btn btn-ghost btn-sm" onClick={() => onApply([])}>Bỏ chọn</button>
        <button type="button" className="btn btn-primary btn-sm" onClick={() => onApply(sel)}>Áp dụng</button>
      </div>
    </div>
  );
}

function AreaForm({ city, district, onApply, onNearMe }: {
  city: string; district: string; onApply: (city: string, district: string) => void; onNearMe: () => void;
}) {
  const [c, setC] = useState(city);
  const [d, setD] = useState(district);
  return (
    <form className="dd-pad" onSubmit={e => { e.preventDefault(); onApply(c.trim(), d.trim()); }}>
      <label className="field"><span>Tỉnh / Thành phố</span><input value={c} onChange={e => setC(e.target.value)} placeholder="Ví dụ: Đà Nẵng" autoFocus /></label>
      <label className="field"><span>Quận / Huyện</span><input value={d} onChange={e => setD(e.target.value)} placeholder="Ví dụ: Hải Châu" /></label>
      <button type="button" className="btn btn-ghost btn-sm btn-block" onClick={onNearMe}><FontAwesomeIcon icon={faLocationCrosshairs} /> Tìm quanh vị trí của tôi</button>
      <div className="dd-actions">
        <button type="button" className="btn btn-ghost btn-sm" onClick={() => onApply('', '')}>Bỏ chọn</button>
        <button className="btn btn-primary btn-sm">Áp dụng</button>
      </div>
    </form>
  );
}

function LinkGroup({ title, children, collapsed }: { title: string; children: ReactNode; collapsed?: boolean }) {
  const [open, setOpen] = useState(!collapsed);
  return (
    <div className="link-group card">
      <button className="link-group-head" onClick={() => setOpen(o => !o)}>
        <span>{title}</span><FontAwesomeIcon icon={open ? faChevronUp : faChevronDown} />
      </button>
      {open && <div className="link-group-body">{children}</div>}
    </div>
  );
}

/** Ngăn kéo lọc đầy đủ (nút "Lọc") */
function FilterDrawer({ params, onClose, onApply, onReset, onNearMe }: {
  params: URLSearchParams; onClose: () => void; onApply: (patch: Patch) => void; onReset: () => void; onNearMe: () => void;
}) {
  const [f, setF] = useState<Record<FilterKey, string>>(() => Object.fromEntries(FILTER_KEYS.map(k => [k, params.get(k) ?? ''])) as Record<FilterKey, string>);
  const [sel, setSel] = useState(params.getAll('amenities'));
  const set = (k: FilterKey, v: string) => setF(x => ({ ...x, [k]: v }));
  return (
    <>
      <div className="filter-backdrop show" onClick={onClose} />
      <aside className="filter-drawer">
        <div className="filter-panel-head">
          <h3><FontAwesomeIcon icon={faBars} /> Bộ lọc</h3>
          <button className="icon-btn" onClick={onClose}><FontAwesomeIcon icon={faXmark} /></button>
        </div>
        <div className="filter-group">
          <h4>Loại nhà</h4>
          <div className="type-pills">
            <button type="button" className={`type-pill ${!f.type ? 'active' : ''}`} onClick={() => set('type', '')}>Tất cả</button>
            {TYPES.map(t => (
              <button type="button" key={t} className={`type-pill ${f.type === t ? 'active' : ''}`} onClick={() => set('type', t)}>
                <FontAwesomeIcon icon={propertyTypeIcon[t]} /> {propertyTypeLabel[t]}
              </button>
            ))}
          </div>
        </div>
        <div className="filter-group">
          <h4>Khu vực</h4>
          <input placeholder="Tỉnh / Thành phố" value={f.city} onChange={e => set('city', e.target.value)} />
          <input placeholder="Quận / Huyện" value={f.district} onChange={e => set('district', e.target.value)} />
          <button type="button" className="btn btn-ghost btn-sm btn-block" onClick={onNearMe}><FontAwesomeIcon icon={faLocationCrosshairs} /> Tìm quanh vị trí của tôi</button>
        </div>
        <div className="filter-group">
          <h4>Giá thuê / tháng</h4>
          <div className="range-row">
            <input type="number" min={0} step={500000} placeholder="Từ" value={f.minPrice} onChange={e => set('minPrice', e.target.value)} />
            <span>–</span>
            <input type="number" min={0} step={500000} placeholder="Đến" value={f.maxPrice} onChange={e => set('maxPrice', e.target.value)} />
          </div>
        </div>
        <div className="filter-group">
          <h4>Diện tích (m²)</h4>
          <div className="range-row">
            <input type="number" min={0} placeholder="Từ" value={f.minArea} onChange={e => set('minArea', e.target.value)} />
            <span>–</span>
            <input type="number" min={0} placeholder="Đến" value={f.maxArea} onChange={e => set('maxArea', e.target.value)} />
          </div>
        </div>
        <div className="filter-group">
          <h4>Phòng ngủ</h4>
          <div className="seg">
            {['', '1', '2', '3', '4'].map(n => (
              <button type="button" key={n} className={f.minBedrooms === n ? 'active' : ''} onClick={() => set('minBedrooms', n)}>{n ? `${n}+` : 'Bất kỳ'}</button>
            ))}
          </div>
          <h4>Phòng tắm</h4>
          <div className="seg">
            {['', '1', '2', '3'].map(n => (
              <button type="button" key={n} className={f.minBathrooms === n ? 'active' : ''} onClick={() => set('minBathrooms', n)}>{n ? `${n}+` : 'Bất kỳ'}</button>
            ))}
          </div>
        </div>
        <div className="filter-group">
          <h4>Tiện ích</h4>
          <div className="amenity-checks">
            {AMENITIES.map(a => (
              <label key={a} className={`amenity-check ${sel.includes(a) ? 'active' : ''}`}>
                <input type="checkbox" checked={sel.includes(a)} onChange={() => setSel(s => s.includes(a) ? s.filter(x => x !== a) : [...s, a])} />
                <FontAwesomeIcon icon={amenityIcon(a)} /> {a}
              </label>
            ))}
          </div>
        </div>
        <div className="filter-actions">
          <button type="button" className="btn btn-ghost" onClick={onReset}><FontAwesomeIcon icon={faRotateLeft} /> Đặt lại</button>
          <button type="button" className="btn btn-primary" onClick={() => onApply({ ...Object.fromEntries(FILTER_KEYS.map(k => [k, f[k] || undefined])), amenities: sel })}>
            Xem kết quả
          </button>
        </div>
      </aside>
    </>
  );
}

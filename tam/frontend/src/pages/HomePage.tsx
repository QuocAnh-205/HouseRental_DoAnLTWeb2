import { lazy, Suspense, useEffect, useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faArrowRight, faCalendarCheck, faChevronDown, faComments, faFileInvoiceDollar, faLocationDot, faMagnifyingGlass,
  faPenToSquare, faShieldHalved,
} from '@fortawesome/free-solid-svg-icons';
import { api } from '../api/client';
import { propertyTypeLabel } from '../api/labels';
import type { PagedResult, PropertyStatsDto, PropertySummaryDto, PropertyType } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { DISTRICTS } from '../components/Layout';
import { imgFallback, placeholderImage, PropertyCard, PropertyCardSkeleton } from '../components/PropertyCard';
import { propertyTypeIcon } from '../icons';
import { useApi } from '../hooks';
import { Loading } from '../components/ui';

// Bản đồ (Leaflet) tải riêng để không làm chậm lần mở trang đầu tiên
const NearbyMap = lazy(() => import('../components/NearbyMap'));

const TYPES: PropertyType[] = ['Room', 'Apartment', 'House', 'Villa', 'Office'];

const PRICE_LINKS = [
  { label: 'Dưới 1,5 triệu', q: 'maxPrice=1500000' },
  { label: '1,5 - 2 triệu', q: 'minPrice=1500000&maxPrice=2000000' },
  { label: '2 - 3 triệu', q: 'minPrice=2000000&maxPrice=3000000' },
  { label: '3 - 5 triệu', q: 'minPrice=3000000&maxPrice=5000000' },
  { label: 'Trên 5 triệu', q: 'minPrice=5000000' },
];

type HeroTab = 'Room' | 'Apartment' | 'House' | '';

const HERO_TABS: HeroTab[] = ['Room', 'Apartment', 'House', ''];

// Nội dung khối tìm kiếm thay đổi theo tab đang chọn
const HERO: Record<HeroTab, { title: string; noun: string; placeholder: string; quick: { label: string; q: string }[] }> = {
  Room: {
    title: 'Thuê phòng trọ ở Đà Nẵng',
    noun: 'phòng trọ',
    placeholder: 'Nhập tên đường, phường, trường học...',
    quick: [
      { label: 'Phòng có gác lửng', q: 'amenities=Gác lửng' },
      { label: 'Phòng có máy lạnh', q: 'amenities=Điều hòa' },
      { label: 'Dưới 2 triệu', q: 'maxPrice=2000000' },
      { label: 'Gần trường Đại học', q: 'keyword=đại học' },
      { label: 'Gần biển', q: 'keyword=biển' },
    ],
  },
  Apartment: {
    title: 'Thuê căn hộ ở Đà Nẵng',
    noun: 'căn hộ',
    placeholder: 'Nhập tên dự án, đường, phường...',
    quick: [
      { label: 'Căn hộ mini', q: 'maxPrice=6000000' },
      { label: 'Căn hộ 1 phòng ngủ', q: 'minBedrooms=1' },
      { label: 'Căn hộ 2 phòng ngủ', q: 'minBedrooms=2' },
      { label: 'Căn hộ view biển', q: 'keyword=biển' },
      { label: 'Có hồ bơi', q: 'keyword=hồ bơi' },
    ],
  },
  House: {
    title: 'Thuê nhà nguyên căn ở Đà Nẵng',
    noun: 'nhà nguyên căn',
    placeholder: 'Nhập tên đường, phường, khu dân cư...',
    quick: [
      { label: 'Nhà 2 phòng ngủ', q: 'minBedrooms=2' },
      { label: 'Nhà 3 phòng ngủ trở lên', q: 'minBedrooms=3' },
      { label: 'Dưới 8 triệu', q: 'maxPrice=8000000' },
      { label: 'Nhà mặt tiền', q: 'keyword=mặt tiền' },
      { label: 'Gần biển', q: 'keyword=biển' },
    ],
  },
  '': {
    title: 'Thuê phòng trọ, nhà ở Đà Nẵng',
    noun: 'tin cho thuê',
    placeholder: 'Nhập tên đường, phường, trường học...',
    quick: [
      { label: 'Phòng có gác lửng', q: 'type=Room&amenities=Gác lửng' },
      { label: 'Phòng có máy lạnh', q: 'type=Room&amenities=Điều hòa' },
      { label: 'Gần biển', q: 'keyword=biển' },
      { label: 'Gần trường Đại học', q: 'keyword=đại học' },
      { label: 'Căn hộ mini', q: 'type=Apartment&maxPrice=6000000' },
      { label: 'Nhà nguyên căn', q: 'type=House' },
    ],
  },
};

const withType = (t: HeroTab, q: string) => (t && !q.includes('type=') ? `type=${t}&${q}` : q);

const PAGE = 20;

export default function HomePage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [keyword, setKeyword] = useState('');
  const [district, setDistrict] = useState('');
  const [type, setType] = useState<HeroTab>('Room');
  const [items, setItems] = useState<PropertySummaryDto[]>([]);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [covers, setCovers] = useState<Partial<Record<PropertyType, string>>>({});
  const [showAbout, setShowAbout] = useState(false);

  const { data: stats } = useApi(() => api.get<PropertyStatsDto>('/api/properties/stats', { city: 'Đà Nẵng' }), []);
  const { data: roomStats } = useApi(() => api.get<PropertyStatsDto>('/api/properties/stats', { city: 'Đà Nẵng', type: 'Room' }), []);
  const { data: cheap } = useApi(
    () => api.get<PagedResult<PropertySummaryDto>>('/api/properties', { type: 'Room', maxPrice: 2_000_000, sortBy: 'newest', pageSize: 10 }), [],
  );
  // Thống kê và danh sách tin đăng mới đều lọc theo tab đang chọn
  const { data: tabStats } = useApi(() => api.get<PropertyStatsDto>('/api/properties/stats', { city: 'Đà Nẵng', type }), [type]);
  const { data: first, loading } = useApi(
    () => api.get<PagedResult<PropertySummaryDto>>('/api/properties', { type, sortBy: 'newest', pageSize: PAGE, page: 1 }), [type],
  );
  const hero = HERO[type];

  useEffect(() => {
    if (!first) return;
    setItems(first.items);
    setPage(1);
    setHasMore(first.totalPages > 1);
  }, [first]);

  // Ảnh đại diện cho từng danh mục: ảnh của tin xem nhiều nhất thuộc loại đó
  useEffect(() => {
    TYPES.forEach(t => {
      api.get<PagedResult<PropertySummaryDto>>('/api/properties', { type: t, sortBy: 'most_viewed', pageSize: 1 })
        .then(r => { const url = r.items[0]?.thumbnailUrl; if (url) setCovers(c => ({ ...c, [t]: url })); })
        .catch(() => { /* bỏ qua */ });
    });
  }, []);

  const loadMore = async () => {
    setLoadingMore(true);
    try {
      const next = await api.get<PagedResult<PropertySummaryDto>>('/api/properties', { type, sortBy: 'newest', pageSize: PAGE, page: page + 1 });
      setItems(list => [...list, ...next.items.filter(x => !list.some(y => y.id === x.id))]);
      setPage(next.page);
      setHasMore(next.page < next.totalPages);
    } finally {
      setLoadingMore(false);
    }
  };

  const submit = (e: FormEvent) => {
    e.preventDefault();
    const p = new URLSearchParams();
    if (type) p.set('type', type);
    if (district) p.set('district', district);
    if (keyword.trim()) p.set('keyword', keyword.trim());
    navigate(`/search?${p}`);
  };

  const typeCount = (t: PropertyType) => stats?.byType.find(x => x.key === t)?.count;
  const districtCount = (d: string) => roomStats?.byDistrict.find(x => x.key === d)?.count ?? 0;
  const now = new Date();

  return (
    <div className="nt-home">
      {/* ===== BANNER + TÌM KIẾM ===== */}
      <section className="nt-hero">
        <div className="container">
          <div className="nt-hero-box">
            <div className="nt-hero-copy">
              <h1>{hero.title}</h1>
              <p>
                {tabStats ? <><strong>{tabStats.total.toLocaleString('vi-VN')}</strong> {hero.noun} đang hiển thị</> : 'Đang tải tin đăng...'}
                {' '}· Cập nhật {now.toLocaleDateString('vi-VN')}
              </p>
              <form className="nt-hero-search" onSubmit={submit}>
                <div className="nt-hero-tabs">
                  {HERO_TABS.map(t => (
                    <button type="button" key={t || 'all'} className={type === t ? 'on' : ''} aria-pressed={type === t} onClick={() => setType(t)}>
                      {t && <FontAwesomeIcon icon={propertyTypeIcon[t]} />} {t ? propertyTypeLabel[t] : 'Tất cả'}
                      {t && typeCount(t) !== undefined && <small className="nt-tab-count">{typeCount(t)}</small>}
                      {!t && stats && <small className="nt-tab-count">{stats.total}</small>}
                    </button>
                  ))}
                </div>
                <div className="nt-hero-row">
                  <label className="nt-hero-loc">
                    <FontAwesomeIcon icon={faLocationDot} />
                    <select value={district} onChange={e => setDistrict(e.target.value)}>
                      <option value="">Toàn Đà Nẵng</option>
                      {DISTRICTS.map(d => <option key={d} value={d}>{d}</option>)}
                    </select>
                  </label>
                  <input placeholder={hero.placeholder} value={keyword} onChange={e => setKeyword(e.target.value)} />
                  <button className="btn btn-primary"><FontAwesomeIcon icon={faMagnifyingGlass} /> Tìm kiếm</button>
                </div>
              </form>
              <div className="nt-quick">
                {hero.quick.map(q => <Link key={q.label} to={`/search?${withType(type, q.q)}`}>{q.label}</Link>)}
              </div>
            </div>
            <div className="nt-hero-art" aria-hidden>
              {(first?.items ?? []).filter(p => p.thumbnailUrl).slice(0, 4).map(p => (
                <img key={p.id} src={p.thumbnailUrl} alt="" onError={imgFallback} />
              ))}
            </div>
          </div>
        </div>
      </section>

      <div className="container">
        {/* ===== DANH MỤC ===== */}
        <section className="nt-box">
          <h2 className="nt-box-title">Khám phá danh mục</h2>
          <div className="nt-cats">
            {TYPES.map(t => (
              <Link key={t} to={`/search?type=${t}`} className="nt-cat-tile">
                <span className="nt-cat-img">
                  <img src={covers[t] ?? placeholderImage} alt={propertyTypeLabel[t]} onError={imgFallback} />
                  <span className="nt-cat-icon"><FontAwesomeIcon icon={propertyTypeIcon[t]} /></span>
                </span>
                <strong>{propertyTypeLabel[t]}</strong>
                <span className="muted small">{typeCount(t) ?? '…'} tin</span>
              </Link>
            ))}
          </div>
        </section>

        {/* ===== BẢN ĐỒ NHÀ GẦN BẠN ===== */}
        <Suspense fallback={<section className="nt-box"><Loading text="Đang tải bản đồ..." /></section>}>
          <NearbyMap />
        </Suspense>

        {/* ===== KHU VỰC ===== */}
        <section className="nt-box">
          <div className="nt-box-head">
            <h2 className="nt-box-title">Thuê phòng trọ theo quận ở Đà Nẵng</h2>
            <Link to="/search?type=Room" className="see-all">Xem tất cả <FontAwesomeIcon icon={faArrowRight} /></Link>
          </div>
          <div className="nt-districts">
            {DISTRICTS.map(d => (
              <Link key={d} to={`/search?type=Room&district=${encodeURIComponent(d)}`} className="nt-district">
                <FontAwesomeIcon icon={faLocationDot} />
                <span><strong>{d}</strong><small>{districtCount(d)} tin</small></span>
              </Link>
            ))}
          </div>
          <div className="nt-price-links">
            <span>Phòng trọ theo giá:</span>
            {PRICE_LINKS.map(p => <Link key={p.label} to={`/search?type=Room&${p.q}`}>{p.label}</Link>)}
          </div>
        </section>

        {/* ===== PHÒNG GIÁ RẺ ===== */}
        {cheap && cheap.items.length > 0 && (
          <section className="nt-box">
            <div className="nt-box-head">
              <h2 className="nt-box-title">Phòng trọ giá dưới 2 triệu</h2>
              <Link to="/search?type=Room&maxPrice=2000000" className="see-all">Xem thêm {cheap.totalCount} tin <FontAwesomeIcon icon={faArrowRight} /></Link>
            </div>
            <div className="nt-strip">
              {cheap.items.map(p => <PropertyCard key={p.id} p={p} />)}
            </div>
          </section>
        )}

        {/* ===== TIN ĐĂNG MỚI ===== */}
        <section className="nt-box">
          <div className="nt-box-head">
            <h2 className="nt-box-title">{type ? `${propertyTypeLabel[type]} mới nhất` : 'Tin đăng mới nhất'}</h2>
            <Link to={`/search?${withType(type, 'sortBy=newest')}`} className="see-all">Xem tất cả <FontAwesomeIcon icon={faArrowRight} /></Link>
          </div>
          <div className="nt-grid">
            {loading && items.length === 0
              ? Array.from({ length: 10 }, (_, i) => <PropertyCardSkeleton key={i} />)
              : items.map(p => <PropertyCard key={p.id} p={p} />)}
          </div>
          {!loading && first && first.items.length === 0 && <p className="muted center-text">Chưa có tin đăng nào{type ? ` loại ${propertyTypeLabel[type].toLowerCase()}` : ''}.</p>}
          {hasMore && (
            <div className="center-text mt">
              <button className="btn btn-ghost nt-more" onClick={loadMore} disabled={loadingMore}>
                {loadingMore ? 'Đang tải...' : <>Xem thêm tin <FontAwesomeIcon icon={faChevronDown} /></>}
              </button>
            </div>
          )}
        </section>

        {/* ===== CHỦ NHÀ ===== */}
        {(!user || user.role === 'Landlord') && (
          <section className="nt-box nt-post-banner">
            <div>
              <h2>Bạn có phòng trống cần cho thuê?</h2>
              <p>Đăng tin miễn phí, nhận lịch hẹn xem phòng, ký hợp đồng và lập hóa đơn điện nước ngay trên TổẤm.</p>
            </div>
            <Link to={user?.role === 'Landlord' ? '/dang-tin' : '/register?role=Landlord'} className="nt-post-btn lg">
              <FontAwesomeIcon icon={faPenToSquare} /> ĐĂNG TIN NGAY
            </Link>
          </section>
        )}

        {/* ===== VÌ SAO ===== */}
        <section className="nt-box nt-features">
          {[
            { icon: faShieldHalved, title: 'Tin đã kiểm duyệt', text: 'Tin đăng được quản trị viên duyệt trước khi hiển thị.' },
            { icon: faCalendarCheck, title: 'Hẹn xem phòng', text: 'Đặt lịch xem phòng, chủ nhà xác nhận trực tuyến.' },
            { icon: faComments, title: 'Chat với chủ nhà', text: 'Hỏi giá, thương lượng trực tiếp không qua trung gian.' },
            { icon: faFileInvoiceDollar, title: 'Hóa đơn minh bạch', text: 'Tiền phòng, điện, nước tách bạch, thanh toán online.' },
          ].map(f => (
            <div key={f.title} className="nt-feature">
              <span><FontAwesomeIcon icon={f.icon} /></span>
              <div><strong>{f.title}</strong><p>{f.text}</p></div>
            </div>
          ))}
        </section>

        {/* ===== GIỚI THIỆU (kiểu khối SEO cuối trang) ===== */}
        <section className="nt-box nt-about">
          <h2 className="nt-box-title">Thuê phòng trọ Đà Nẵng giá rẻ, mới nhất T{now.getMonth() + 1}/{now.getFullYear()}</h2>
          <div className={`nt-about-text ${showAbout ? 'open' : ''}`}>
            <p>
              Đà Nẵng là thành phố có nhu cầu thuê phòng trọ lớn nhờ lượng sinh viên các trường Đại học Đông Á, Đại học Bách khoa,
              Đại học Kinh tế, Duy Tân... và người lao động đến làm việc. Các khu vực được tìm nhiều là Hải Châu, Thanh Khê
              (trung tâm), Liên Chiểu (gần làng đại học), Ngũ Hành Sơn và Sơn Trà (gần biển Mỹ Khê).
            </p>
            <p>
              Giá phòng trọ phổ biến từ 1,5 đến 3 triệu đồng/tháng cho phòng 15 - 25 m²; phòng có gác lửng, máy lạnh, nóng lạnh
              hoặc căn hộ mini đầy đủ nội thất có giá từ 3 đến 6 triệu đồng/tháng. Trên TổẤm, bạn có thể lọc theo quận, khoảng giá,
              diện tích, tiện ích, lưu tin yêu thích, nhắn tin với chủ nhà và đặt lịch xem phòng.
            </p>
          </div>
          <button className="link-btn" onClick={() => setShowAbout(s => !s)}>{showAbout ? 'Thu gọn' : 'Xem thêm'}</button>
        </section>
      </div>
    </div>
  );
}

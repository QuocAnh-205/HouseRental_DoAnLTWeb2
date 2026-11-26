import { Link } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faFire, faHeart as faHeartSolid, faHouse, faImage, faLocationDot } from '@fortawesome/free-solid-svg-icons';
import { faHeart as faHeartRegular } from '@fortawesome/free-regular-svg-icons';
import { ensureUtc, propertyStatusLabel, propertyTypeLabel, shortMoney, timeAgo } from '../api/labels';
import type { PropertySummaryDto } from '../api/types';
import { useFavorites } from '../favorites';
import { Badge } from './ui';

// Ảnh giữ chỗ: icon nhà của Font Awesome vẽ giữa nền xám
const [houseW, houseH, , , housePath] = faHouse.icon;
const PLACEHOLDER = 'data:image/svg+xml;utf8,' + encodeURIComponent(
  `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 300"><rect width="400" height="300" fill="#f1f5f9"/>`
  + `<svg x="170" y="120" width="60" height="60" viewBox="0 0 ${houseW} ${houseH}"><path fill="#cbd5e1" d="${housePath}"/></svg></svg>`,
);

export const imgFallback = (e: React.SyntheticEvent<HTMLImageElement>) => {
  e.currentTarget.src = PLACEHOLDER;
};
export const placeholderImage = PLACEHOLDER;

const DAY = 86_400_000;
export const isNewListing = (createdAt: string) => Date.now() - new Date(ensureUtc(createdAt)).getTime() < 2 * DAY;
export const isTopRated = (p: { averageRating: number; reviewCount: number }) => p.reviewCount > 0 && p.averageRating >= 4.5;

export function FavoriteButton({ id, className = '' }: { id: number; className?: string }) {
  const { isFavorite, toggle } = useFavorites();
  const fav = isFavorite(id);
  return (
    <button
      type="button"
      className={`heart-btn ${fav ? 'active' : ''} ${className}`}
      title={fav ? 'Bỏ lưu' : 'Lưu nhà này'}
      aria-pressed={fav}
      onClick={e => { e.preventDefault(); e.stopPropagation(); toggle(id); }}
    >
      <FontAwesomeIcon icon={fav ? faHeartSolid : faHeartRegular} />
    </button>
  );
}

/** "2,3 triệu/tháng" như trên Nhà Tốt */
export const priceText = (v: number) => `${shortMoney(v)}/tháng`;

/** Thẻ tin dạng lưới (kiểu Nhà Tốt): ảnh, số ảnh, tiêu đề 2 dòng, giá đỏ, khu vực · thời gian */
export function PropertyCard({ p, showStatus, actions }: {
  p: PropertySummaryDto; showStatus?: boolean; actions?: React.ReactNode;
}) {
  return (
    <article className="property-card">
      <Link to={`/properties/${p.id}`} className="property-thumb">
        <img src={p.thumbnailUrl || PLACEHOLDER} alt={p.title} loading="lazy" onError={imgFallback} />
        {isTopRated(p) && <span className="nt-flag"><FontAwesomeIcon icon={faFire} /> Được yêu thích</span>}
        {(p.imageCount ?? 0) > 0 && <span className="nt-photo-count"><FontAwesomeIcon icon={faImage} /> {p.imageCount}</span>}
        {showStatus
          ? <span className="thumb-status"><Badge status={p.status} label={propertyStatusLabel[p.status]} /></span>
          : <FavoriteButton id={p.id} className="thumb-heart" />}
      </Link>
      <div className="property-body">
        <Link to={`/properties/${p.id}`} className="property-title">{p.title}</Link>
        <div className="property-sub">
          {propertyTypeLabel[p.type]} · {p.area} m²{p.type !== 'Room' && p.bedrooms > 0 ? ` · ${p.bedrooms} PN` : ''}
        </div>
        <div className="property-price">{priceText(p.pricePerMonth)}</div>
        <div className="property-foot">
          <span className="ellipsis"><FontAwesomeIcon icon={faLocationDot} /> {p.district}</span>
          <span className="nowrap">{timeAgo(p.createdAt)}</span>
        </div>
        {actions && <div className="property-actions">{actions}</div>}
      </div>
    </article>
  );
}

/** Khung xương khi đang tải danh sách nhà */
export function PropertyCardSkeleton() {
  return (
    <div className="property-card skeleton-card" aria-hidden>
      <div className="property-thumb skeleton" />
      <div className="property-body">
        <div className="skeleton line w-40" />
        <div className="skeleton line w-90" />
        <div className="skeleton line w-70" />
        <div className="skeleton line w-50" />
      </div>
    </div>
  );
}

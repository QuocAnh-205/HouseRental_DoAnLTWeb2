import { Link } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faFire, faHeart as faHeartSolid, faImage, faLocationDot, faStar } from '@fortawesome/free-solid-svg-icons';
import { faHeart as faHeartRegular } from '@fortawesome/free-regular-svg-icons';
import { propertyTypeLabel, timeAgo } from '../api/labels';
import type { PropertySummaryDto } from '../api/types';
import { useFavorites } from '../favorites';
import { imgFallback, isTopRated, placeholderImage, priceText } from './PropertyCard';
import { Avatar } from './ui';

/** Tin đăng dạng hàng ngang kiểu Nhà Tốt: ảnh vuông bên trái, thông tin bên phải */
export function ListingRow({ p }: { p: PropertySummaryDto }) {
  const { isFavorite, toggle } = useFavorites();
  const fav = isFavorite(p.id);
  const perM2 = p.area > 0 ? p.pricePerMonth / p.area : 0;

  return (
    <article className="listing-row">
      <Link to={`/properties/${p.id}`} className="listing-thumb">
        <img src={p.thumbnailUrl || placeholderImage} alt={p.title} loading="lazy" onError={imgFallback} />
        {isTopRated(p) && <span className="nt-flag"><FontAwesomeIcon icon={faFire} /> Được yêu thích</span>}
        {(p.imageCount ?? 0) > 0 && <span className="nt-photo-count"><FontAwesomeIcon icon={faImage} /> {p.imageCount}</span>}
      </Link>
      <div className="listing-info">
        <Link to={`/properties/${p.id}`} className="listing-title">{p.title}</Link>
        <div className="listing-sub">
          {propertyTypeLabel[p.type]}
          {p.type !== 'Room' && p.bedrooms > 0 && <> · {p.bedrooms} PN</>}
          {p.type !== 'Room' && p.bathrooms > 0 && <> · {p.bathrooms} WC</>}
          {p.reviewCount > 0 && <> · <FontAwesomeIcon icon={faStar} className="stars" /> {p.averageRating.toFixed(1)} ({p.reviewCount})</>}
        </div>
        <div className="listing-price">
          <strong>{priceText(p.pricePerMonth)}</strong>
          <span className="listing-area">{p.area} m²</span>
          {perM2 > 0 && <span className="listing-unit">{(perM2 / 1_000_000).toFixed(2).replace('.', ',')} tr/m²</span>}
        </div>
        <div className="listing-loc">
          <FontAwesomeIcon icon={faLocationDot} /> {[p.ward, p.district].filter(Boolean).join(', ')}
        </div>
        <div className="listing-foot">
          <Link to={`/users/${p.landlordId}`} className="listing-host">
            <Avatar name={p.landlordName} url={p.landlordAvatarUrl} size={22} />
            <span className="ellipsis">{p.landlordName}</span>
          </Link>
          <span className="listing-time">{timeAgo(p.createdAt)}</span>
          <button type="button" className={`save-btn ${fav ? 'active' : ''}`} onClick={() => toggle(p.id)} title={fav ? 'Bỏ lưu' : 'Lưu tin'}>
            <FontAwesomeIcon icon={fav ? faHeartSolid : faHeartRegular} />
          </button>
        </div>
      </div>
    </article>
  );
}

export function ListingRowSkeleton() {
  return (
    <div className="listing-row" aria-hidden>
      <div className="listing-thumb skeleton" />
      <div className="listing-info">
        <div className="skeleton line w-90" />
        <div className="skeleton line w-40" />
        <div className="skeleton line w-50" />
        <div className="skeleton line w-70" />
      </div>
    </div>
  );
}

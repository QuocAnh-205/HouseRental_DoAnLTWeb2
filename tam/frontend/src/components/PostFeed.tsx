import { useCallback, useEffect, useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faBath, faBed, faCircleExclamation, faEye, faLocationDot, faPenToSquare, faRulerCombined, faStar,
} from '@fortawesome/free-solid-svg-icons';
import { api, errorMessage } from '../api/client';
import { fmtDateTime, propertyStatusLabel, propertyTypeLabel, shortMoney, timeAgo } from '../api/labels';
import type { PagedResult, PropertyPostDto } from '../api/types';
import { amenityIcon, propertyTypeIcon } from '../icons';
import { imgFallback } from './PropertyCard';
import { Avatar, Badge, Empty, ErrorBox, Loading } from './ui';

const PAGE_SIZE = 5;

/**
 * Danh sách tin đăng dạng bài viết (bản tin), tải thêm khi bấm "Xem thêm".
 * Dùng cho Bản tin chung (/ban-tin, endpoint /api/properties/feed) và bản tin trên trang cá nhân
 * (/api/users/{id}/posts — chính chủ thấy cả tin chờ duyệt / bị từ chối / đã ẩn).
 */
export default function PostFeed({ endpoint, query, isOwner = false, emptyText, emptyAction, countLabel = 'tin đã đăng' }: {
  endpoint: string;
  query?: Record<string, string | number | undefined>;
  isOwner?: boolean;
  emptyText: string;
  emptyAction?: ReactNode;
  countLabel?: string;
}) {
  const [posts, setPosts] = useState<PropertyPostDto[]>([]);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [hasMore, setHasMore] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async (nextPage: number) => {
    setLoading(true);
    setError('');
    try {
      const res = await api.get<PagedResult<PropertyPostDto>>(endpoint, { ...query, page: nextPage, pageSize: PAGE_SIZE });
      setPosts(list => nextPage === 1 ? res.items : [...list, ...res.items.filter(x => !list.some(y => y.id === x.id))]);
      setPage(nextPage);
      setTotal(res.totalCount);
      setHasMore(res.hasNextPage);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [endpoint, JSON.stringify(query)]);

  useEffect(() => { load(1); }, [load]);

  if (error && posts.length === 0) return <ErrorBox error={error} onRetry={() => load(1)} />;
  if (loading && posts.length === 0) return <Loading text="Đang tải bản tin..." />;
  if (posts.length === 0) {
    return <Empty text={emptyText}>{emptyAction}</Empty>;
  }

  return (
    <div className="post-feed">
      <div className="post-feed-count muted small">{total.toLocaleString('vi-VN')} {countLabel}{isOwner && ' (chỉ bạn thấy các tin chưa được duyệt)'}</div>
      {posts.map(p => <PostCard key={p.id} post={p} isOwner={isOwner} />)}
      {hasMore && (
        <button className="btn btn-ghost btn-block" disabled={loading} onClick={() => load(page + 1)}>
          {loading ? 'Đang tải...' : 'Xem thêm tin cũ hơn'}
        </button>
      )}
    </div>
  );
}

function PostCard({ post: p, isOwner }: { post: PropertyPostDto; isOwner: boolean }) {
  const publicVisible = p.status === 'Available' || p.status === 'Rented';
  const extra = p.imageCount - p.imageUrls.length;
  const location = [p.address, p.ward, p.district, p.city].filter(Boolean).join(', ');

  return (
    <article className={`post-card ${publicVisible ? '' : 'is-private'}`}>
      <header className="post-head">
        <Link to={`/users/${p.landlordId}`} aria-label={`Trang cá nhân của ${p.landlordName}`}>
          <Avatar name={p.landlordName} url={p.landlordAvatarUrl} size={44} />
        </Link>
        <div className="post-author">
          <Link to={`/users/${p.landlordId}`} className="post-author-name">{p.landlordName}</Link>
          <span className="muted small" title={`Đăng ${fmtDateTime(p.createdAt)}`}>
            đã đăng tin cho thuê <FontAwesomeIcon icon={propertyTypeIcon[p.type]} /> {propertyTypeLabel[p.type].toLowerCase()} · {timeAgo(p.publishedAt ?? p.createdAt)}
          </span>
        </div>
        {p.status !== 'Available' && <Badge status={p.status} label={propertyStatusLabel[p.status]} />}
      </header>

      {isOwner && p.status === 'Rejected' && (
        <div className="alert alert-error post-alert">
          <span><FontAwesomeIcon icon={faCircleExclamation} /> Tin bị từ chối{p.rejectReason ? `: ${p.rejectReason}` : ''}. Hãy sửa tin để gửi duyệt lại.</span>
        </div>
      )}
      {isOwner && p.status === 'PendingApproval' && (
        <div className="alert alert-info post-alert">Tin đang chờ quản trị viên duyệt, sau khi duyệt mọi người sẽ thấy tin này.</div>
      )}

      <div className="post-body">
        <Link to={`/properties/${p.id}`} className="post-title">{p.title}</Link>
        <div className="post-price">
          <strong>{shortMoney(p.pricePerMonth)}/tháng</strong>
          <span><FontAwesomeIcon icon={faRulerCombined} /> {p.area} m²</span>
          {p.bedrooms > 0 && <span><FontAwesomeIcon icon={faBed} /> {p.bedrooms} PN</span>}
          {p.bathrooms > 0 && <span><FontAwesomeIcon icon={faBath} /> {p.bathrooms} WC</span>}
        </div>
        <div className="post-location muted small"><FontAwesomeIcon icon={faLocationDot} /> {location}</div>
        {p.excerpt && <p className="post-excerpt">{p.excerpt}</p>}
        {p.amenities.length > 0 && (
          <div className="post-amenities">
            {p.amenities.map(a => <span key={a} className="chip static"><FontAwesomeIcon icon={amenityIcon(a)} /> {a}</span>)}
          </div>
        )}
      </div>

      {p.imageUrls.length > 0 && (
        <Link to={`/properties/${p.id}`} className={`post-gallery n${Math.min(p.imageUrls.length, 4)}`} aria-label={`Xem ảnh "${p.title}"`}>
          {p.imageUrls.map((url, i) => (
            <span key={url} className="post-photo">
              <img src={url} alt="" loading="lazy" onError={imgFallback} />
              {i === p.imageUrls.length - 1 && extra > 0 && <span className="post-more">+{extra}</span>}
            </span>
          ))}
        </Link>
      )}

      <footer className="post-foot">
        <span className="muted small"><FontAwesomeIcon icon={faEye} /> {p.viewCount} lượt xem</span>
        {p.reviewCount > 0 && <span className="muted small"><FontAwesomeIcon icon={faStar} className="text-gold" /> {p.averageRating.toFixed(1)} ({p.reviewCount})</span>}
        <span className="grow" />
        {isOwner && (
          <Link to={`/landlord/properties/${p.id}/edit`} className="btn btn-sm btn-ghost"><FontAwesomeIcon icon={faPenToSquare} /> Sửa tin</Link>
        )}
        <Link to={`/properties/${p.id}`} className="btn btn-sm btn-primary">Xem chi tiết</Link>
      </footer>
    </article>
  );
}

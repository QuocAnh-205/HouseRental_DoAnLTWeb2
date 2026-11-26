import { useState } from 'react';
import { Link } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faBullhorn, faCircleCheck, faMagnifyingGlass, faMapLocationDot, faNewspaper, faShieldHalved,
} from '@fortawesome/free-solid-svg-icons';
import { api } from '../api/client';
import { propertyTypeLabel } from '../api/labels';
import type { PropertyStatsDto, PropertyType } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import PostFeed from '../components/PostFeed';
import { useApi } from '../hooks';
import { propertyTypeIcon } from '../icons';

const TYPES: PropertyType[] = ['Room', 'Apartment', 'House', 'Villa', 'Office'];

/**
 * Bản tin chung (/ban-tin): mọi tin cho thuê đã được quản trị viên duyệt, hiển thị dạng bài viết.
 * Tin vừa được duyệt nằm trên cùng, ai cũng xem được (kể cả khách chưa đăng nhập).
 */
export default function NewsFeedPage() {
  const { user } = useAuth();
  const [type, setType] = useState<PropertyType | ''>('');
  const { data: stats } = useApi(() => api.get<PropertyStatsDto>('/api/properties/stats'), []);
  const typeCount = (t: PropertyType) => stats?.byType.find(x => x.key === t)?.count;

  return (
    <div className="feed-page">
      <section className="post-hero">
        <div className="container">
          <span className="eyebrow-soft"><FontAwesomeIcon icon={faNewspaper} /> Bản tin</span>
          <h1>Tin cho thuê mới nhất</h1>
          <p>Mọi tin đăng đều được quản trị viên kiểm duyệt. Tin vừa được duyệt sẽ xuất hiện ở đầu bản tin để tất cả mọi người cùng xem.</p>
        </div>
      </section>

      <div className="container feed-layout">
        <main className="feed-main">
          <div className="feed-filters" role="group" aria-label="Lọc theo loại nhà">
            <button type="button" className={`chip ${type === '' ? 'active' : ''}`} onClick={() => setType('')}>
              Tất cả {stats && <span className="muted">{stats.total.toLocaleString('vi-VN')}</span>}
            </button>
            {TYPES.map(t => (
              <button type="button" key={t} className={`chip ${type === t ? 'active' : ''}`} onClick={() => setType(t)}>
                <FontAwesomeIcon icon={propertyTypeIcon[t]} /> {propertyTypeLabel[t]} {typeCount(t) != null && <span className="muted">{typeCount(t)}</span>}
              </button>
            ))}
          </div>

          <PostFeed
            key={type}
            endpoint="/api/properties/feed"
            query={{ type: type || undefined }}
            countLabel="tin đang cho thuê"
            emptyText="Chưa có tin nào thuộc loại này."
          />
        </main>

        <aside className="feed-aside">
          <div className="card pad feed-cta">
            {user?.role === 'Landlord' ? (
              <>
                <h3>Bạn có nhà cho thuê?</h3>
                <p className="muted small">Đăng tin miễn phí, sau khi được duyệt tin sẽ hiện ngay trên bản tin này.</p>
                <Link to="/dang-tin" className="btn btn-primary btn-block"><FontAwesomeIcon icon={faBullhorn} /> Đăng tin mới</Link>
                <Link to={`/users/${user.id}`} className="btn btn-ghost btn-block mt-sm">Bản tin của tôi</Link>
              </>
            ) : !user ? (
              <>
                <h3>Bạn là chủ nhà?</h3>
                <p className="muted small">Tạo tài khoản chủ nhà để đăng tin cho thuê miễn phí.</p>
                <Link to="/register?role=Landlord" className="btn btn-primary btn-block"><FontAwesomeIcon icon={faBullhorn} /> Đăng ký đăng tin</Link>
              </>
            ) : (
              <>
                <h3>Tìm nhà nhanh hơn</h3>
                <p className="muted small">Lọc theo giá, diện tích, tiện ích hoặc tìm quanh vị trí của bạn.</p>
                <Link to="/search" className="btn btn-primary btn-block"><FontAwesomeIcon icon={faMagnifyingGlass} /> Tìm kiếm nâng cao</Link>
              </>
            )}
          </div>
          <div className="card pad">
            <ul className="trust-list">
              <li><FontAwesomeIcon icon={faShieldHalved} /><div><strong>Tin đã kiểm duyệt</strong><span>Quản trị viên duyệt trước khi hiển thị</span></div></li>
              <li><FontAwesomeIcon icon={faMapLocationDot} /><div><strong>Xem trên bản đồ</strong><span><Link to="/">Nhà cho thuê gần bạn</Link> ở trang chủ</span></div></li>
              <li><FontAwesomeIcon icon={faCircleCheck} /><div><strong>Nhắn tin trực tiếp</strong><span>Hỏi chủ nhà ngay trong hệ thống</span></div></li>
            </ul>
          </div>
        </aside>
      </div>
    </div>
  );
}

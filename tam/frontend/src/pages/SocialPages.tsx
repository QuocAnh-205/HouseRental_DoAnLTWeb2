import { useEffect, useRef, useState, type FormEvent } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faHouse, faArrowLeft, faGear, faFileSignature, faCreditCard, faFileInvoiceDollar, faCalendarDays, faStar, faFlag, faCheckDouble, faXmark, faHeartCrack, faCommentDots, faComments, faBell, faHeart, faHeadset, faBullhorn, faArrowRight } from '@fortawesome/free-solid-svg-icons';
import type { IconDefinition } from '@fortawesome/fontawesome-svg-core';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { api, errorMessage } from '../api/client';
import {
  ensureUtc, fmtDate, fmtDateTime, notificationRoute, notificationTypeLabel, reportReasonLabel, reportStatusLabel, roleLabel, timeAgo,
} from '../api/labels';
import type {
  ConversationDto, MessageDto, NotificationDto, NotificationType, PagedResult, PropertySummaryDto, PublicProfileDto, ReportDto,
} from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { notifyUnreadChanged } from '../components/Layout';
import { PropertyCard } from '../components/PropertyCard';
import PostFeed from '../components/PostFeed';
import { Avatar, Badge, Empty, ErrorBox, Loading, PageHeader, Pagination, PromptModal, Stars, Tabs } from '../components/ui';
import { toast, useAction, useApi } from '../hooks';

// ===================== Tin nhắn =====================

export function MessagesPage() {
  const { id } = useParams();
  const activeId = id ? Number(id) : null;
  const navigate = useNavigate();
  const { user } = useAuth();
  const [params] = useSearchParams();
  const [supportOpen, setSupportOpen] = useState(false);
  const { data: convs, loading, reload } = useApi(
    () => api.get<PagedResult<ConversationDto>>('/api/conversations', { pageSize: 50 }), [],
  );
  const isAdmin = user?.role === 'Admin';

  // /messages?to=<userId>[&property=<id>&title=<tiêu đề>]: mở thẳng khung chat với người đó
  const toId = Number(params.get('to')) || null;
  const draftPropertyId = Number(params.get('property')) || null;
  const existing = toId && convs ? findConversation(convs.items, toId, draftPropertyId) : undefined;
  const draftTo = toId && toId !== user?.id && convs && !existing && !activeId ? toId : null;

  useEffect(() => {
    if (existing && !activeId) navigate(`/messages/${existing.id}`, { replace: true });
  }, [existing, activeId, navigate]);

  const contactSupport = async (content: string) => {
    try {
      const c = await api.post<ConversationDto>('/api/conversations/support', { content });
      setSupportOpen(false);
      toast('Đã gửi tin nhắn tới quản trị viên');
      await reload();
      navigate(`/messages/${c.id}`);
    } catch (err) {
      toast(errorMessage(err), 'error');
    }
  };

  // Làm mới danh sách mỗi 15 giây
  useEffect(() => {
    const t = setInterval(reload, 15_000);
    return () => clearInterval(t);
  }, [reload]);

  const active = convs?.items.find(c => c.id === activeId);

  return (
    <div className="container">
      <PageHeader
        icon={faComments}
        title="Tin nhắn"
        subtitle={isAdmin ? 'Hỗ trợ người thuê và chủ nhà' : 'Trao đổi với chủ nhà, người thuê và quản trị viên'}
        actions={!isAdmin && (
          <button className="btn btn-primary" onClick={() => setSupportOpen(true)}>
            <FontAwesomeIcon icon={faHeadset} /> Nhắn tin cho quản trị viên
          </button>
        )}
      />
      <PromptModal
        open={supportOpen}
        title="Nhắn tin cho quản trị viên"
        label="Bạn cần hỗ trợ gì? (thanh toán, duyệt tin, báo lỗi...)"
        required
        confirmText="Gửi tin nhắn"
        onClose={() => setSupportOpen(false)}
        onSubmit={contactSupport}
      />
      <div className={`chat-layout ${activeId || draftTo ? 'has-active' : ''}`}>
        <div className="card chat-list">
          {loading && !convs ? <Loading /> : convs?.items.length === 0 ? (
            <Empty text={isAdmin ? 'Chưa có người dùng nào cần hỗ trợ.' : 'Chưa có cuộc trò chuyện nào. Hãy nhắn tin cho chủ nhà từ trang chi tiết nhà, hoặc liên hệ quản trị viên.'} />
          ) : convs?.items.map(c => (
            <button key={c.id} className={`chat-item ${c.id === activeId ? 'active' : ''}`} onClick={() => navigate(`/messages/${c.id}`)}>
              <Avatar name={c.otherParticipant.fullName} url={c.otherParticipant.avatarUrl} size={40} />
              <div className="grow">
                <div className="chat-item-head">
                  <strong>{c.otherParticipant.fullName}{c.otherParticipant.role === 'Admin' && <span className="role-tag role-admin chat-role">Hỗ trợ</span>}</strong>
                  <span className="muted small">{timeAgo(c.lastMessageAt)}</span>
                </div>
                {c.propertyTitle && <div className="small text-primary ellipsis"><FontAwesomeIcon icon={faHouse} /> {c.propertyTitle}</div>}
                <div className={`small ellipsis ${c.unreadCount ? 'bold' : 'muted'}`}>{c.lastMessage}</div>
              </div>
              {c.unreadCount > 0 && <span className="dot static">{c.unreadCount}</span>}
            </button>
          ))}
        </div>
        <div className="card chat-panel">
          {activeId ? (
            <ChatThread key={activeId} conversationId={activeId} conversation={active} onSent={reload} onBack={() => navigate('/messages')} />
          ) : draftTo ? (
            <DraftThread
              key={`${draftTo}-${draftPropertyId}`}
              recipientId={draftTo}
              propertyId={draftPropertyId}
              propertyTitle={params.get('title')}
              onCreated={async c => { await reload(); navigate(`/messages/${c.id}`, { replace: true }); }}
              onBack={() => navigate('/messages')}
            />
          ) : (
            toId && !convs ? <Loading /> :
            <div className="chat-placeholder muted">Chọn một cuộc trò chuyện để bắt đầu</div>
          )}
        </div>
      </div>
    </div>
  );
}

/** Cuộc trò chuyện đã có với người này (ưu tiên đúng căn nhà nếu đang hỏi về một căn nhà) */
function findConversation(items: ConversationDto[], userId: number, propertyId: number | null) {
  const withUser = items.filter(c => c.otherParticipant.id === userId);
  if (propertyId) return withUser.find(c => c.propertyId === propertyId);
  return withUser.find(c => !c.propertyId) ?? withUser[0];
}

const PROPERTY_SUGGESTIONS = [
  'Căn này còn trống không ạ?',
  'Giá có bao gồm phí dịch vụ không ạ?',
  'Tôi có thể xem nhà vào cuối tuần không?',
  'Có chỗ để xe máy không ạ?',
];

/** Khung chat mới với một người chưa từng nhắn: gõ tin đầu tiên là tạo cuộc trò chuyện */
function DraftThread({ recipientId, propertyId, propertyTitle, onCreated, onBack }: {
  recipientId: number; propertyId: number | null; propertyTitle: string | null;
  onCreated: (c: ConversationDto) => void; onBack: () => void;
}) {
  const { data: person, error } = useApi(() => api.get<PublicProfileDto>(`/api/users/${recipientId}/profile`), [recipientId]);
  const [text, setText] = useState(propertyId && propertyTitle ? `Chào anh/chị, tôi quan tâm đến căn "${propertyTitle}". ` : '');
  const { busy, run } = useAction();
  const inputRef = useRef<HTMLTextAreaElement>(null);

  useEffect(() => { inputRef.current?.focus(); }, [person]);

  const send = async (e: FormEvent) => {
    e.preventDefault();
    const content = text.trim();
    if (!content) return;
    const c = await run(() => api.post<ConversationDto>('/api/conversations', { recipientId, propertyId: propertyId ?? undefined, content }));
    if (c) onCreated(c);
  };

  return (
    <>
      <div className="chat-header">
        <button className="icon-btn chat-back" onClick={onBack}><FontAwesomeIcon icon={faArrowLeft} /></button>
        {person && (
          <>
            <Avatar name={person.fullName} url={person.avatarUrl} />
            <div>
              <Link to={`/users/${person.id}`}><strong>{person.fullName}</strong></Link>
              <div className="small muted">
                {propertyId && propertyTitle
                  ? <Link to={`/properties/${propertyId}`}><FontAwesomeIcon icon={faHouse} /> {propertyTitle}</Link>
                  : roleLabel[person.role]}
              </div>
            </div>
          </>
        )}
      </div>
      <div className="chat-messages chat-draft">
        {error ? <ErrorBox error={error} /> : person && (
          <div className="chat-draft-intro">
            <Avatar name={person.fullName} url={person.avatarUrl} size={64} />
            <strong>{person.fullName}</strong>
            <span className="muted small">Hai bạn chưa nhắn tin với nhau. Gửi tin nhắn đầu tiên để bắt đầu trò chuyện.</span>
            {propertyId && (
              <div className="chips-row">
                {PROPERTY_SUGGESTIONS.map(q => <button type="button" key={q} className="chip" onClick={() => setText(q)}>{q}</button>)}
              </div>
            )}
          </div>
        )}
      </div>
      <form className="chat-input" onSubmit={send}>
        <textarea ref={inputRef} rows={1} maxLength={2000} placeholder="Nhập tin nhắn..." value={text} onChange={e => setText(e.target.value)}
          disabled={!!error}
          onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send(e); } }} />
        <button className="btn btn-primary" disabled={busy || !text.trim() || !!error}>Gửi</button>
      </form>
    </>
  );
}

function ChatThread({ conversationId, conversation, onSent, onBack }: {
  conversationId: number; conversation?: ConversationDto; onSent: () => void; onBack: () => void;
}) {
  const { user } = useAuth();
  const [messages, setMessages] = useState<MessageDto[]>([]);
  const [olderPage, setOlderPage] = useState<number | null>(null);
  const [text, setText] = useState('');
  const [error, setError] = useState('');
  const { busy, run } = useAction();
  const listRef = useRef<HTMLDivElement>(null);
  const PAGE = 30;

  // Chỉ cuộn khung tin nhắn xuống cuối, không làm cuộn cả trang
  const scrollToBottom = (smooth = false) => setTimeout(() => {
    const el = listRef.current;
    if (el) el.scrollTo({ top: el.scrollHeight, behavior: smooth ? 'smooth' : 'auto' });
  }, 50);

  const merge = (incoming: MessageDto[]) => setMessages(prev => {
    const map = new Map(prev.map(m => [m.id, m]));
    incoming.forEach(m => map.set(m.id, m));
    return [...map.values()].sort((a, b) => a.id - b.id);
  });

  // Tải trang mới nhất (backend tự đánh dấu đã đọc)
  const loadLatest = async (scroll = false) => {
    try {
      const res = await api.get<PagedResult<MessageDto>>(`/api/conversations/${conversationId}/messages`, { page: 1, pageSize: PAGE });
      merge(res.items);
      setOlderPage(prev => prev ?? (res.hasNextPage ? 2 : 0));
      notifyUnreadChanged();
      if (scroll) scrollToBottom();
    } catch (err) {
      setError(String((err as Error).message));
    }
  };

  useEffect(() => {
    loadLatest(true);
    const t = setInterval(() => loadLatest(false), 5_000);
    return () => clearInterval(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [conversationId]);

  const loadOlder = async () => {
    if (!olderPage) return;
    const res = await run(() => api.get<PagedResult<MessageDto>>(`/api/conversations/${conversationId}/messages`, { page: olderPage, pageSize: PAGE }));
    if (res) {
      merge(res.items);
      setOlderPage(res.hasNextPage ? olderPage + 1 : 0);
    }
  };

  const send = async (e: FormEvent) => {
    e.preventDefault();
    const content = text.trim();
    if (!content) return;
    const msg = await run(() => api.post<MessageDto>(`/api/conversations/${conversationId}/messages`, { content }));
    if (msg) {
      setText('');
      merge([msg]);
      onSent();
      scrollToBottom(true);
    }
  };

  return (
    <>
      <div className="chat-header">
        <button className="icon-btn chat-back" onClick={onBack}><FontAwesomeIcon icon={faArrowLeft} /></button>
        {conversation && (
          <>
            <Avatar name={conversation.otherParticipant.fullName} url={conversation.otherParticipant.avatarUrl} />
            <div>
              <Link to={`/users/${conversation.otherParticipant.id}`}><strong>{conversation.otherParticipant.fullName}</strong></Link>
              {conversation.propertyId && (
                <div className="small"><Link to={`/properties/${conversation.propertyId}`}><FontAwesomeIcon icon={faHouse} /> {conversation.propertyTitle}</Link></div>
              )}
            </div>
          </>
        )}
      </div>
      <div className="chat-messages" ref={listRef}>
        {error && <ErrorBox error={error} />}
        {!!olderPage && <button className="btn btn-sm btn-ghost center" disabled={busy} onClick={loadOlder}>Tải tin cũ hơn</button>}
        {messages.map((m, i) => {
          const mine = m.senderId === user!.id;
          const showDate = i === 0 || fmtDate(messages[i - 1].createdAt) !== fmtDate(m.createdAt);
          return (
            <div key={m.id}>
              {showDate && <div className="chat-date">{fmtDate(m.createdAt)}</div>}
              <div className={`bubble-row ${mine ? 'mine' : ''}`}>
                <div className="bubble" title={fmtDateTime(m.createdAt)}>
                  {m.content}
                  <div className="bubble-meta">
                    {new Date(ensureUtc(m.createdAt)).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}
                    {mine && (m.isRead ? ' · Đã xem' : ' · Đã gửi')}
                  </div>
                </div>
              </div>
            </div>
          );
        })}
      </div>
      <form className="chat-input" onSubmit={send}>
        <textarea rows={1} maxLength={2000} placeholder="Nhập tin nhắn..." value={text} onChange={e => setText(e.target.value)}
          onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send(e); } }} />
        <button className="btn btn-primary" disabled={busy || !text.trim()}>Gửi</button>
      </form>
    </>
  );
}

// ===================== Thông báo =====================

export function NotificationsPage() {
  const navigate = useNavigate();
  const { run } = useAction();
  const [filter, setFilter] = useState<'all' | 'unread'>('all');
  const [page, setPage] = useState(1);
  const { data, loading, error, reload, setData } = useApi(
    () => api.get<PagedResult<NotificationDto>>('/api/notifications', { isRead: filter === 'unread' ? false : undefined, page, pageSize: 15 }),
    [filter, page],
  );

  const { user } = useAuth();

  // Bấm vào thông báo: đánh dấu đã đọc (không chờ) rồi mở đúng trang / vị trí liên quan
  const open = (n: NotificationDto) => {
    if (!n.isRead) {
      api.patch(`/api/notifications/${n.id}/read`).then(notifyUnreadChanged).catch(() => { /* bỏ qua */ });
    }
    navigate(notificationRoute(n, user?.role));
  };

  const markAll = async () => {
    const ok = await run(() => api.patch('/api/notifications/read-all'));
    if (ok !== undefined) { toast('Đã đánh dấu tất cả là đã đọc'); notifyUnreadChanged(); reload(); }
  };

  const remove = async (n: NotificationDto) => {
    const ok = await run(() => api.del(`/api/notifications/${n.id}`));
    if (ok !== undefined && data) {
      setData({ ...data, items: data.items.filter(x => x.id !== n.id), totalCount: data.totalCount - 1 });
      notifyUnreadChanged();
    }
  };

  const icon: Record<NotificationType, IconDefinition> = {
    System: faGear, Property: faHouse, Booking: faFileSignature, Payment: faCreditCard,
    Invoice: faFileInvoiceDollar, Appointment: faCalendarDays, Review: faStar, Report: faFlag,
  };

  return (
    <div className="container narrow">
      <PageHeader icon={faBell} title="Thông báo" subtitle="Cập nhật về hợp đồng, lịch hẹn, hóa đơn và thanh toán" actions={<button className="btn btn-ghost btn-sm" onClick={markAll}><FontAwesomeIcon icon={faCheckDouble} /> Đọc tất cả</button>} />
      <Tabs value={filter} options={[{ value: 'all', label: 'Tất cả' }, { value: 'unread', label: 'Chưa đọc' }]}
        onChange={v => { setFilter(v); setPage(1); }} />
      {error && <ErrorBox error={error} onRetry={reload} />}
      {loading && !data ? <Loading /> : data?.items.length === 0 ? <Empty text="Không có thông báo." /> : (
        <div className="notif-groups">
          {groupByDay(data?.items ?? []).map(([label, items]) => (
            <div key={label}>
              <div className="group-label">{label}</div>
              <div className="card notif-list">
                {items.map(n => (
                  <div key={n.id} className={`notif notif-${n.type.toLowerCase()} ${n.isRead ? '' : 'unread'}`}>
                    <span className="notif-icon"><FontAwesomeIcon icon={icon[n.type]} /></span>
                    <button className="notif-body" onClick={() => open(n)} title="Mở nội dung liên quan">
                      <div className="notif-title">{n.title}</div>
                      <div className="small">{n.message}</div>
                      <div className="muted small">{notificationTypeLabel[n.type]} · {timeAgo(n.createdAt)} · <span className="notif-go">Xem chi tiết <FontAwesomeIcon icon={faArrowRight} /></span></div>
                    </button>
                    <button className="icon-btn" title="Xóa" onClick={() => remove(n)}><FontAwesomeIcon icon={faXmark} /></button>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
      <Pagination data={data} onPage={setPage} />
    </div>
  );
}

/** Nhóm thông báo theo ngày: Hôm nay / Hôm qua / Trước đó */
function groupByDay(items: NotificationDto[]): [string, NotificationDto[]][] {
  const startOfToday = new Date(); startOfToday.setHours(0, 0, 0, 0);
  const today = startOfToday.getTime();
  const yesterday = today - 86_400_000;
  const groups: Record<string, NotificationDto[]> = {};
  for (const n of items) {
    const t = new Date(ensureUtc(n.createdAt)).getTime();
    const key = t >= today ? 'Hôm nay' : t >= yesterday ? 'Hôm qua' : 'Trước đó';
    (groups[key] ??= []).push(n);
  }
  return (['Hôm nay', 'Hôm qua', 'Trước đó'] as const).filter(k => groups[k]).map(k => [k, groups[k]]);
}

// ===================== Yêu thích =====================

export function FavoritesPage() {
  const [page, setPage] = useState(1);
  const { run } = useAction();
  const { data, loading, error, reload } = useApi(
    () => api.get<PagedResult<PropertySummaryDto>>('/api/favorites', { page, pageSize: 12 }), [page],
  );
  const remove = async (id: number) => {
    const ok = await run(() => api.del(`/api/favorites/${id}`));
    if (ok !== undefined) { toast('Đã bỏ lưu'); reload(); }
  };
  return (
    <div className="container">
      <PageHeader icon={faHeart} title="Nhà đã lưu" subtitle="Những căn bạn quan tâm, so sánh và quyết định khi sẵn sàng" />
      {error && <ErrorBox error={error} onRetry={reload} />}
      {loading && !data ? <Loading /> : data?.items.length === 0 ? (
        <Empty text="Bạn chưa lưu căn nhà nào."><Link to="/search" className="btn btn-primary">Khám phá ngay</Link></Empty>
      ) : (
        <div className="property-grid">
          {data?.items.map(p => (
            <PropertyCard key={p.id} p={p} actions={<button className="btn btn-sm btn-ghost danger" onClick={() => remove(p.id)}><FontAwesomeIcon icon={faHeartCrack} /> Bỏ lưu</button>} />
          ))}
        </div>
      )}
      <Pagination data={data} onPage={setPage} />
    </div>
  );
}

// ===================== Báo cáo đã gửi =====================

export function MyReportsPage() {
  const [page, setPage] = useState(1);
  const { data, loading, error, reload } = useApi(
    () => api.get<PagedResult<ReportDto>>('/api/reports/mine', { page, pageSize: 10 }), [page],
  );
  return (
    <div className="container">
      <PageHeader icon={faFlag} title="Báo cáo vi phạm đã gửi" subtitle="Theo dõi kết quả xử lý của quản trị viên" />
      {error && <ErrorBox error={error} onRetry={reload} />}
      {loading && !data ? <Loading /> : data?.items.length === 0 ? <Empty text="Bạn chưa gửi báo cáo nào." /> : (
        <div className="table-wrap">
          <table className="table">
            <thead><tr><th>Tin đăng</th><th>Lý do</th><th>Ngày gửi</th><th>Trạng thái</th><th>Phản hồi</th></tr></thead>
            <tbody>
              {data?.items.map(r => (
                <tr key={r.id}>
                  <td><Link to={`/properties/${r.propertyId}`}>{r.propertyTitle}</Link></td>
                  <td>{reportReasonLabel[r.reason]}{r.description && <div className="muted small">{r.description}</div>}</td>
                  <td>{fmtDate(r.createdAt)}</td>
                  <td><Badge status={r.status} label={reportStatusLabel[r.status]} /></td>
                  <td className="small">{r.adminNote ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <Pagination data={data} onPage={setPage} />
    </div>
  );
}

// ===================== Hồ sơ công khai =====================

export function PublicProfilePage() {
  const { id } = useParams();
  const { user } = useAuth();
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [tab, setTab] = useState<'feed' | 'listings'>('feed');
  const profile = useApi(() => api.get<PublicProfileDto>(`/api/users/${id}/profile`), [id]);
  const listings = useApi(() => api.get<PagedResult<PropertySummaryDto>>(`/api/users/${id}/properties`, { page, pageSize: 8 }), [id, page]);
  const p = profile.data;
  const isOwner = !!user && !!p && user.id === p.id;

  if (profile.loading && !p) return <Loading />;
  if (profile.error || !p) return <div className="container"><ErrorBox error={profile.error ?? 'Không tìm thấy'} /></div>;

  return (
    <div className="container">
      <div className="card pad profile-head">
        <Avatar name={p.fullName} url={p.avatarUrl} size={80} />
        <div className="grow">
          <h1>{p.fullName}</h1>
          <div className="muted">{roleLabel[p.role]} · Tham gia {fmtDate(p.joinedAt)}</div>
          {p.role === 'Landlord' && (
            <div className="profile-stats">
              <span><strong>{p.activeListings}</strong> tin đang cho thuê</span>
              <span>{p.totalReviews > 0 ? <><Stars value={p.averageRating} /> {p.averageRating.toFixed(1)} ({p.totalReviews} đánh giá)</> : 'Chưa có đánh giá'}</span>
            </div>
          )}
        </div>
        <div className="profile-actions">
          {user && user.id !== p.id && (
            <button className="btn btn-primary" onClick={() => navigate(`/messages?to=${p.id}`)}>
              <FontAwesomeIcon icon={faCommentDots} /> Nhắn tin
            </button>
          )}
          {isOwner && p.role === 'Landlord' && (
            <Link to="/dang-tin" className="btn btn-primary"><FontAwesomeIcon icon={faBullhorn} /> Đăng tin mới</Link>
          )}
          {isOwner && <Link to="/profile" className="btn btn-ghost"><FontAwesomeIcon icon={faGear} /> Chỉnh sửa hồ sơ</Link>}
        </div>
      </div>
      {p.role === 'Landlord' && (
        <div className="profile-content">
          <Tabs<'feed' | 'listings'>
            value={tab}
            options={[
              { value: 'feed', label: 'Bản tin' },
              { value: 'listings', label: `Nhà đang cho thuê (${p.activeListings})` },
            ]}
            onChange={setTab}
          />
          {tab === 'feed' ? (
            <PostFeed
              endpoint={`/api/users/${p.id}/posts`}
              isOwner={isOwner}
              emptyText={isOwner ? 'Bạn chưa đăng tin nào. Tin bạn đăng sẽ hiện ở đây.' : `${p.fullName} chưa có tin đăng nào.`}
              emptyAction={isOwner && <Link to="/dang-tin" className="btn btn-primary"><FontAwesomeIcon icon={faBullhorn} /> Đăng tin đầu tiên</Link>}
            />
          ) : (
            <>
              {listings.data?.items.length === 0 ? <Empty text="Chưa có nhà nào đang cho thuê." /> : (
                <div className="property-grid">{listings.data?.items.map(x => <PropertyCard key={x.id} p={x} />)}</div>
              )}
              <Pagination data={listings.data} onPage={setPage} />
            </>
          )}
        </div>
      )}
    </div>
  );
}

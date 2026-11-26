import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faDiamondTurnRight, faLocationArrow, faLocationDot, faMagnifyingGlass, faMapLocationDot } from '@fortawesome/free-solid-svg-icons';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { api } from '../api/client';
import { propertyTypeLabel, shortMoney } from '../api/labels';
import type { PagedResult, PropertySummaryDto } from '../api/types';
import { imgFallback, placeholderImage } from './PropertyCard';
import { geocode, type Place } from '../geo';

type NearbyItem = PropertySummaryDto & { distanceKm: number };

/** Trung tâm Đà Nẵng (cầu Rồng) — điểm mặc định khi người dùng chưa chọn vị trí */
const DEFAULT_PLACE: Place = { lat: 16.0611, lng: 108.2272, label: 'Trung tâm Đà Nẵng' };

const RADII = [1, 2, 3, 5, 10];
const MAX_RESULTS = 100; // PageSize tối đa của API

/** Khoảng cách theo đường chim bay (km) */
function haversineKm(a: { lat: number; lng: number }, b: { lat: number; lng: number }) {
  const R = 6371;
  const dLat = (b.lat - a.lat) * Math.PI / 180;
  const dLng = (b.lng - a.lng) * Math.PI / 180;
  const h = Math.sin(dLat / 2) ** 2 + Math.cos(a.lat * Math.PI / 180) * Math.cos(b.lat * Math.PI / 180) * Math.sin(dLng / 2) ** 2;
  return 2 * R * Math.asin(Math.sqrt(h));
}

const fmtKm = (km: number) => (km < 1 ? `${Math.round(km * 1000)} m` : `${km.toFixed(1).replace('.', ',')} km`);

const googleDirections = (from: Place, p: PropertySummaryDto) =>
  `https://www.google.com/maps/dir/?api=1&origin=${from.lat},${from.lng}&destination=${p.latitude},${p.longitude}&travelmode=driving`;

/** Nội dung popup: một tin (ảnh lớn) hoặc danh sách các tin cùng vị trí */
function buildPopup(group: NearbyItem[], from: Place): HTMLElement {
  const root = document.createElement('div');
  root.className = 'map-popup';
  const el = <K extends keyof HTMLElementTagNameMap>(tag: K, className: string, text?: string) => {
    const node = document.createElement(tag);
    node.className = className;
    if (text !== undefined) node.textContent = text;
    return node;
  };
  const image = (url?: string) => {
    const img = document.createElement('img');
    img.alt = '';
    img.loading = 'lazy';
    img.src = url || placeholderImage;
    img.onerror = () => { img.src = placeholderImage; };
    return img;
  };
  const directions = el('a', 'map-popup-dir', 'Chỉ đường (Google Maps)');
  directions.href = googleDirections(from, group[0]);
  directions.target = '_blank';
  directions.rel = 'noreferrer';

  if (group.length === 1) {
    const p = group[0];
    const body = el('div', 'map-popup-body');
    const detail = el('a', 'map-popup-detail', 'Xem chi tiết');
    detail.href = `/properties/${p.id}`;
    const links = el('span', 'map-popup-links');
    links.append(detail, directions);
    body.append(
      el('strong', '', p.title),
      el('span', 'map-popup-price', `${shortMoney(p.pricePerMonth)}/tháng · ${p.area} m²`),
      el('span', 'map-popup-meta', `${propertyTypeLabel[p.type]} · cách ${fmtKm(p.distanceKm)}`),
      links,
    );
    root.append(image(p.thumbnailUrl), body);
    return root;
  }

  root.classList.add('is-group');
  const head = el('div', 'map-popup-head');
  head.append(el('strong', '', `${group.length} tin cho thuê tại đây`), el('span', 'map-popup-meta', `Cách ${fmtKm(group[0].distanceKm)}`));
  const list = el('div', 'map-popup-list');
  [...group].sort((a, b) => a.pricePerMonth - b.pricePerMonth).forEach(p => {
    const row = el('a', 'map-popup-row');
    row.href = `/properties/${p.id}`;
    const info = el('span', 'map-popup-row-info');
    info.append(el('span', 'map-popup-row-title', p.title), el('span', 'map-popup-price', `${shortMoney(p.pricePerMonth)}/tháng · ${p.area} m²`));
    row.append(image(p.thumbnailUrl), info);
    list.append(row);
  });
  const foot = el('div', 'map-popup-links');
  foot.append(directions);
  root.append(head, list, foot);
  return root;
}

/**
 * Bản đồ "Nhà cho thuê gần bạn":
 * người dùng cho phép truy cập vị trí hoặc nhập địa chỉ, hệ thống gọi API tìm nhà theo bán kính
 * (/api/properties?latitude=&longitude=&radiusKm=&sortBy=distance) và đánh dấu từng nhà trên bản đồ.
 */
export default function NearbyMap() {
  const mapEl = useRef<HTMLDivElement>(null);
  const mapRef = useRef<L.Map | null>(null);
  const layerRef = useRef<L.LayerGroup | null>(null);
  const markersRef = useRef(new Map<number, L.Marker>());

  const [place, setPlace] = useState<Place>(DEFAULT_PLACE);
  const [isUserLocation, setIsUserLocation] = useState(false);
  const [radius, setRadius] = useState(3);
  const [query, setQuery] = useState('');
  const [items, setItems] = useState<NearbyItem[]>([]);
  const [truncated, setTruncated] = useState(false);
  const [loading, setLoading] = useState(false);
  const [locating, setLocating] = useState(false);
  const [message, setMessage] = useState('');
  const [activeId, setActiveId] = useState<number | null>(null);

  // Khởi tạo bản đồ một lần
  useEffect(() => {
    if (!mapEl.current || mapRef.current) return;
    const map = L.map(mapEl.current, { zoomControl: true, scrollWheelZoom: false }).setView([DEFAULT_PLACE.lat, DEFAULT_PLACE.lng], 14);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
    }).addTo(map);
    map.on('focus', () => map.scrollWheelZoom.enable());
    map.on('blur', () => map.scrollWheelZoom.disable());
    layerRef.current = L.layerGroup().addTo(map);
    mapRef.current = map;
    return () => { map.remove(); mapRef.current = null; };
  }, []);

  // Gọi API tìm nhà quanh vị trí đang chọn
  const load = useCallback(async (center: Place, km: number) => {
    setLoading(true);
    setMessage('');
    try {
      const res = await api.get<PagedResult<PropertySummaryDto>>('/api/properties', {
        latitude: center.lat, longitude: center.lng, radiusKm: km, sortBy: 'distance', pageSize: MAX_RESULTS,
      });
      const list = res.items
        .filter(p => p.latitude != null && p.longitude != null)
        .map(p => ({ ...p, distanceKm: haversineKm(center, { lat: p.latitude!, lng: p.longitude! }) }))
        .filter(p => p.distanceKm <= km)
        .sort((a, b) => a.distanceKm - b.distanceKm);
      setItems(list);
      setTruncated(res.totalCount > res.items.length);
      if (list.length === 0) setMessage(`Không có nhà cho thuê nào trong bán kính ${km} km. Hãy thử tăng bán kính.`);
    } catch {
      setItems([]);
      setMessage('Không tải được danh sách nhà, vui lòng thử lại.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(place, radius); }, [place, radius, load]);

  // Vẽ lại vòng bán kính, vị trí người dùng và các nhà
  useEffect(() => {
    const map = mapRef.current;
    const layer = layerRef.current;
    if (!map || !layer) return;
    layer.clearLayers();
    markersRef.current.clear();

    const circle = L.circle([place.lat, place.lng], {
      radius: radius * 1000, color: '#f2541b', weight: 1.5, fillColor: '#f2541b', fillOpacity: 0.06,
    }).addTo(layer);
    L.marker([place.lat, place.lng], {
      icon: L.divIcon({ className: 'map-me', html: '<span class="map-me-dot"></span>', iconSize: [22, 22], iconAnchor: [11, 11] }),
      zIndexOffset: 1000,
      keyboard: false,
    }).bindTooltip(isUserLocation ? 'Vị trí của bạn' : place.label, { direction: 'top', offset: [0, -10] }).addTo(layer);

    // Nhiều tin có cùng toạ độ (dữ liệu chỉ chính xác tới phường) -> gộp thành một marker
    const groups = new Map<string, NearbyItem[]>();
    items.forEach(p => {
      const key = `${p.latitude!.toFixed(5)},${p.longitude!.toFixed(5)}`;
      groups.set(key, [...(groups.get(key) ?? []), p]);
    });

    groups.forEach(group => {
      const first = group[0];
      const minPrice = Math.min(...group.map(g => g.pricePerMonth));
      const label = group.length > 1 ? `${group.length} tin · từ ${shortMoney(minPrice)}` : shortMoney(first.pricePerMonth);
      const marker = L.marker([first.latitude!, first.longitude!], {
        icon: L.divIcon({
          className: `map-price${group.length > 1 ? ' is-group' : ''}`,
          html: `<span>${label}</span>`,
          // Kích thước theo nội dung; CSS dịch nhãn giá lên trên điểm toạ độ
          iconSize: null as unknown as L.PointExpression,
          iconAnchor: [0, 0],
        }),
        title: group.length > 1 ? `${group.length} tin cho thuê tại vị trí này` : first.title,
        riseOnHover: true,
      });
      marker.bindPopup(buildPopup(group, place), { maxWidth: 280, minWidth: 260 });
      marker.on('click', () => setActiveId(first.id));
      marker.addTo(layer);
      group.forEach(g => markersRef.current.set(g.id, marker));
    });

    map.fitBounds(circle.getBounds(), { padding: [16, 16] });
  }, [items, place, radius, isUserLocation]);

  const useMyLocation = () => {
    if (!('geolocation' in navigator)) {
      setMessage('Trình duyệt không hỗ trợ định vị. Hãy nhập địa chỉ ở ô bên cạnh.');
      return;
    }
    setLocating(true);
    setMessage('');
    navigator.geolocation.getCurrentPosition(
      pos => {
        setLocating(false);
        setIsUserLocation(true);
        setPlace({ lat: pos.coords.latitude, lng: pos.coords.longitude, label: 'Vị trí của bạn' });
      },
      err => {
        setLocating(false);
        setMessage(err.code === err.PERMISSION_DENIED
          ? 'Bạn đã từ chối quyền truy cập vị trí. Hãy cho phép trong cài đặt trình duyệt hoặc nhập địa chỉ.'
          : 'Không xác định được vị trí hiện tại. Hãy nhập địa chỉ để tìm.');
      },
      { enableHighAccuracy: true, timeout: 10_000, maximumAge: 60_000 },
    );
  };

  const search = async (e: FormEvent) => {
    e.preventDefault();
    const text = query.trim();
    if (!text) return;
    setLoading(true);
    const found = await geocode(text);
    if (!found) {
      setLoading(false);
      setMessage(`Không tìm thấy địa chỉ "${text}" ở Đà Nẵng. Hãy nhập rõ hơn, ví dụ "Bạch Đằng, Hải Châu".`);
      return;
    }
    setIsUserLocation(false);
    setPlace(found);
  };

  const focus = (p: NearbyItem) => {
    setActiveId(p.id);
    const marker = markersRef.current.get(p.id);
    if (marker && mapRef.current) {
      mapRef.current.setView(marker.getLatLng(), Math.max(mapRef.current.getZoom(), 15), { animate: true });
      marker.openPopup();
    }
  };

  return (
    <section className="nt-box nearby">
      <div className="nt-box-head">
        <h2 className="nt-box-title"><FontAwesomeIcon icon={faMapLocationDot} className="text-primary" /> Nhà cho thuê gần bạn</h2>
        <span className="muted small">
          {loading ? 'Đang tìm...' : `${truncated ? `${items.length} nhà gần nhất` : `${items.length} nhà`} trong bán kính ${radius} km quanh `}
          {!loading && <strong>{isUserLocation ? 'vị trí của bạn' : place.label}</strong>}
        </span>
      </div>

      <div className="nearby-controls">
        <button type="button" className="btn btn-primary" onClick={useMyLocation} disabled={locating}>
          <FontAwesomeIcon icon={faLocationArrow} /> {locating ? 'Đang xác định...' : 'Dùng vị trí của tôi'}
        </button>
        <form className="nearby-search" onSubmit={search}>
          <FontAwesomeIcon icon={faLocationDot} />
          <input
            placeholder="Hoặc nhập địa chỉ, ví dụ: 54 Nguyễn Lương Bằng, Liên Chiểu"
            value={query}
            onChange={e => setQuery(e.target.value)}
            aria-label="Địa chỉ cần tìm nhà xung quanh"
          />
          <button className="btn btn-ghost btn-sm" disabled={loading || !query.trim()}><FontAwesomeIcon icon={faMagnifyingGlass} /> Tìm</button>
        </form>
        <div className="seg nearby-radius" role="group" aria-label="Bán kính tìm kiếm">
          {RADII.map(r => (
            <button type="button" key={r} className={r === radius ? 'active' : ''} onClick={() => setRadius(r)}>{r} km</button>
          ))}
        </div>
      </div>

      {message && <div className="alert alert-info nearby-message">{message}</div>}

      <div className="nearby-layout">
        <div className="nearby-map" ref={mapEl} />
        <div className="nearby-list">
          {items.slice(0, 30).map(p => (
            <div key={p.id} className={`nearby-item ${p.id === activeId ? 'active' : ''}`} onMouseEnter={() => setActiveId(p.id)}>
              <button type="button" className="nearby-thumb" onClick={() => focus(p)} aria-label={`Xem "${p.title}" trên bản đồ`}>
                <img src={p.thumbnailUrl || placeholderImage} alt="" onError={imgFallback} loading="lazy" />
              </button>
              <div className="nearby-info">
                <Link to={`/properties/${p.id}`} className="nearby-title">{p.title}</Link>
                <span className="property-price">{shortMoney(p.pricePerMonth)}/tháng</span>
                <span className="muted small">
                  <FontAwesomeIcon icon={faLocationDot} /> {fmtKm(p.distanceKm)} · {p.district}
                </span>
              </div>
              <a className="icon-btn" href={googleDirections(place, p)} target="_blank" rel="noreferrer" title="Chỉ đường trên Google Maps" aria-label="Chỉ đường trên Google Maps">
                <FontAwesomeIcon icon={faDiamondTurnRight} />
              </a>
            </div>
          ))}
          {!loading && items.length === 0 && <div className="muted small nearby-empty">Chưa có kết quả.</div>}
        </div>
      </div>
    </section>
  );
}

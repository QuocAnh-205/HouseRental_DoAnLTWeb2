/** Toạ độ + nhãn hiển thị của một địa điểm */
export interface Place { lat: number; lng: number; label: string }

/** Toạ độ trung tâm các quận, dùng khi dịch vụ tìm địa chỉ không trả kết quả */
export const DISTRICT_CENTERS: Record<string, [number, number]> = {
  'hải châu': [16.0472, 108.2199], 'thanh khê': [16.0640, 108.1880], 'sơn trà': [16.0860, 108.2420],
  'ngũ hành sơn': [16.0000, 108.2500], 'liên chiểu': [16.0750, 108.1500], 'cẩm lệ': [16.0150, 108.1950],
  'hòa vang': [16.0300, 108.1000],
};

/** Đổi địa chỉ người dùng nhập thành toạ độ (OpenStreetMap Nominatim, giới hạn trong Đà Nẵng) */
export async function geocode(query: string): Promise<Place | null> {
  const q = /đà nẵng|da nang/i.test(query) ? query : `${query}, Đà Nẵng`;
  const url = new URL('https://nominatim.openstreetmap.org/search');
  url.search = new URLSearchParams({
    format: 'jsonv2', q, limit: '1', countrycodes: 'vn', 'accept-language': 'vi',
    viewbox: '107.90,16.25,108.35,15.90', bounded: '1',
  }).toString();
  try {
    const res = await fetch(url, { headers: { Accept: 'application/json' } });
    if (res.ok) {
      const [first] = await res.json() as { lat: string; lon: string; display_name: string }[];
      if (first) return { lat: Number(first.lat), lng: Number(first.lon), label: first.display_name.split(',').slice(0, 3).join(',') };
    }
  } catch { /* mất mạng hoặc dịch vụ bận: thử khớp tên quận bên dưới */ }

  const lower = query.toLowerCase();
  const district = Object.keys(DISTRICT_CENTERS).find(d => lower.includes(d));
  if (district) {
    const [lat, lng] = DISTRICT_CENTERS[district];
    return { lat, lng, label: `Quận ${district.replace(/(^|\s)\S/g, c => c.toUpperCase())}` };
  }
  return null;
}

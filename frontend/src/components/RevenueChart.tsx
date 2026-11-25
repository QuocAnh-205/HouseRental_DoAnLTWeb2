import { useState } from 'react';
import { money, shortMoney } from '../api/labels';
import type { MonthlyRevenueDto } from '../api/types';

/** Biểu đồ cột doanh thu theo tháng (một chuỗi, không dùng thư viện) */
export function RevenueChart({ data, title = 'Doanh thu 12 tháng gần nhất' }: { data: MonthlyRevenueDto[]; title?: string }) {
  const [hover, setHover] = useState<number | null>(null);
  const [showTable, setShowTable] = useState(false);
  const max = Math.max(...data.map(d => d.revenue), 0);
  // Làm tròn trục lên mốc đẹp
  const step = niceStep(max / 4);
  const top = Math.max(step * 4, 1);
  const ticks = [0, 1, 2, 3, 4].map(i => i * step);

  return (
    <div className="card pad chart-card">
      <div className="section-head">
        <h3>{title}</h3>
        <button className="link-btn small" onClick={() => setShowTable(s => !s)}>{showTable ? 'Xem biểu đồ' : 'Xem bảng'}</button>
      </div>
      {showTable ? (
        <table className="table">
          <thead><tr><th>Tháng</th><th className="right">Doanh thu</th></tr></thead>
          <tbody>{data.map(d => <tr key={`${d.year}-${d.month}`}><td>{d.month}/{d.year}</td><td className="right">{money(d.revenue)}</td></tr>)}</tbody>
        </table>
      ) : (
        <div className="bar-chart" role="img" aria-label={title}>
          <div className="bar-axis">
            {[...ticks].reverse().map(t => <span key={t}>{t === 0 ? '0' : shortMoney(t).replace(' đ', '')}</span>)}
          </div>
          <div className="bar-plot">
            {ticks.map(t => <div key={t} className="bar-grid" style={{ bottom: `${(t / top) * 100}%` }} />)}
            {data.map((d, i) => (
              <div key={`${d.year}-${d.month}`} className="bar-col"
                onMouseEnter={() => setHover(i)} onMouseLeave={() => setHover(null)}>
                <div className={`bar ${hover === i ? 'active' : ''}`} style={{ height: `${(d.revenue / top) * 100}%` }} />
                {hover === i && (
                  <div className="bar-tip" style={{ bottom: `${Math.min((d.revenue / top) * 100, 85)}%` }}>
                    <span className="muted">Tháng {d.month}/{d.year}</span>
                    <strong>{money(d.revenue)}</strong>
                  </div>
                )}
                <span className="bar-label">T{d.month}</span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function niceStep(raw: number) {
  if (raw <= 0) return 1_000_000;
  const pow = 10 ** Math.floor(Math.log10(raw));
  const n = raw / pow;
  return (n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10) * pow;
}

/** Thanh tỉ lệ theo trạng thái (ví dụ: số tin theo trạng thái) */
export function BreakdownList({ title, data, labels }: {
  title: string; data: Record<string, number>; labels: Record<string, string>;
}) {
  const total = Object.values(data).reduce((s, v) => s + v, 0) || 1;
  return (
    <div className="card pad">
      <h3>{title}</h3>
      <div className="breakdown">
        {Object.entries(data).map(([k, v]) => (
          <div key={k} className="breakdown-row">
            <span className="breakdown-label">{labels[k] ?? k}</span>
            <div className="breakdown-track"><div className="breakdown-fill" style={{ width: `${(v / total) * 100}%` }} /></div>
            <span className="breakdown-value">{v}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

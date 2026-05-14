import { useMemo, useState } from 'react'
import './OverviewPage.css'

const monthlyRevenue = [
  { month: 'Thg 1', revenue: 42_000_000 },
  { month: 'Thg 2', revenue: 55_000_000 },
  { month: 'Thg 3', revenue: 48_000_000 },
  { month: 'Thg 4', revenue: 63_000_000 },
  { month: 'Thg 5', revenue: 71_000_000 },
  { month: 'Thg 6', revenue: 67_000_000 },
  { month: 'Thg 7', revenue: 74_000_000 },
  { month: 'Thg 8', revenue: 69_000_000 },
  { month: 'Thg 9', revenue: 77_000_000 },
  { month: 'Thg 10', revenue: 82_000_000 },
  { month: 'Thg 11', revenue: 79_000_000 },
  { month: 'Thg 12', revenue: 88_000_000 },
]

function formatCurrencyVnd(value) {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)
}

export function OverviewPage() {
  const [range, setRange] = useState(6)
  const [chartType, setChartType] = useState('bar')

  const visibleRevenue = useMemo(() => monthlyRevenue.slice(-range), [range])
  const maxRevenue = Math.max(...visibleRevenue.map((item) => item.revenue))
  const totalRevenue = visibleRevenue.reduce((sum, item) => sum + item.revenue, 0)
  const avgRevenue = Math.round(totalRevenue / visibleRevenue.length)
  const highestMonth = visibleRevenue.find((item) => item.revenue === maxRevenue)?.month

  const linePoints = visibleRevenue
    .map((item, index) => {
      const x = (index / Math.max(visibleRevenue.length - 1, 1)) * 100
      const y = 100 - (item.revenue / maxRevenue) * 100
      return `${x},${y}`
    })
    .join(' ')

  return (
    <section className="overview-page">
      <header className="overview-header">
        <div>
          <span className="overview-eyebrow">Bảng quản trị</span>
          <h1 className="overview-title">Tổng quan doanh thu</h1>
          <p className="overview-subtitle">Biểu đồ doanh thu dùng dữ liệu mẫu để demo giao diện.</p>
        </div>
        <div className="overview-controls">
          <div className="control-group">
            <button
              type="button"
              className={`control-btn${range === 6 ? ' active' : ''}`}
              onClick={() => setRange(6)}
            >
              6 tháng
            </button>
            <button
              type="button"
              className={`control-btn${range === 12 ? ' active' : ''}`}
              onClick={() => setRange(12)}
            >
              12 tháng
            </button>
          </div>
          <div className="control-group">
            <button
              type="button"
              className={`control-btn${chartType === 'bar' ? ' active' : ''}`}
              onClick={() => setChartType('bar')}
            >
              Biểu đồ cột
            </button>
            <button
              type="button"
              className={`control-btn${chartType === 'line' ? ' active' : ''}`}
              onClick={() => setChartType('line')}
            >
              Biểu đồ đường
            </button>
          </div>
        </div>
      </header>

      <div className="overview-metrics">
        <article className="overview-metric-card">
          <p className="metric-label">Tổng doanh thu</p>
          <p className="metric-value">{formatCurrencyVnd(totalRevenue)}</p>
        </article>
        <article className="overview-metric-card">
          <p className="metric-label">Trung bình mỗi tháng</p>
          <p className="metric-value">{formatCurrencyVnd(avgRevenue)}</p>
        </article>
        <article className="overview-metric-card">
          <p className="metric-label">Tháng cao nhất</p>
          <p className="metric-value">
            {highestMonth} - {formatCurrencyVnd(maxRevenue)}
          </p>
        </article>
      </div>

      <article className="overview-chart-card">
        <div className="chart-head">
          <h2>Doanh thu theo tháng</h2>
        </div>

        {chartType === 'bar' ? (
          <div className="bar-chart" role="img" aria-label="Biểu đồ cột doanh thu theo tháng">
            {visibleRevenue.map((item) => {
              const barHeightPercent = Math.max(8, (item.revenue / maxRevenue) * 100)

              return (
                <div className="bar-item" key={item.month}>
                  <span className="bar-value">{formatCurrencyVnd(item.revenue)}</span>
                  <div className="bar-track">
                    <div className="bar-fill" style={{ height: `${barHeightPercent}%` }} />
                  </div>
                  <span className="bar-label">{item.month}</span>
                </div>
              )
            })}
          </div>
        ) : (
          <div className="line-chart-card" role="img" aria-label="Biểu đồ đường doanh thu theo tháng">
            <svg className="line-chart-svg" viewBox="0 0 100 100" preserveAspectRatio="none">
              <polyline className="line-path" points={linePoints} />
              {visibleRevenue.map((item, index) => {
                const x = (index / Math.max(visibleRevenue.length - 1, 1)) * 100
                const y = 100 - (item.revenue / maxRevenue) * 100
                return <circle key={item.month} className="line-point" cx={x} cy={y} r="1.8" />
              })}
            </svg>
            <div className="line-labels">
              {visibleRevenue.map((item) => (
                <div className="line-label-item" key={item.month}>
                  <span>{item.month}</span>
                  <strong>{formatCurrencyVnd(item.revenue)}</strong>
                </div>
              ))}
            </div>
          </div>
        )}
      </article>
    </section>
  )
}

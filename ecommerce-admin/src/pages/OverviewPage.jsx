import { useMemo, useState } from 'react'
import './OverviewPage.css'

const monthlyRevenue = [
  { month: 'Jan', revenue: 42_000_000 },
  { month: 'Feb', revenue: 55_000_000 },
  { month: 'Mar', revenue: 48_000_000 },
  { month: 'Apr', revenue: 63_000_000 },
  { month: 'May', revenue: 71_000_000 },
  { month: 'Jun', revenue: 67_000_000 },
  { month: 'Jul', revenue: 74_000_000 },
  { month: 'Aug', revenue: 69_000_000 },
  { month: 'Sep', revenue: 77_000_000 },
  { month: 'Oct', revenue: 82_000_000 },
  { month: 'Nov', revenue: 79_000_000 },
  { month: 'Dec', revenue: 88_000_000 },
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
          <span className="overview-eyebrow">Aura Mystique</span>
          <h1 className="overview-title">Tong quan doanh thu</h1>
          <p className="overview-subtitle">Bieu do doanh thu voi du lieu hard code de demo giao dien.</p>
        </div>
        <div className="overview-controls">
          <div className="control-group">
            <button
              type="button"
              className={`control-btn${range === 6 ? ' active' : ''}`}
              onClick={() => setRange(6)}
            >
              6 thang
            </button>
            <button
              type="button"
              className={`control-btn${range === 12 ? ' active' : ''}`}
              onClick={() => setRange(12)}
            >
              12 thang
            </button>
          </div>
          <div className="control-group">
            <button
              type="button"
              className={`control-btn${chartType === 'bar' ? ' active' : ''}`}
              onClick={() => setChartType('bar')}
            >
              Bar chart
            </button>
            <button
              type="button"
              className={`control-btn${chartType === 'line' ? ' active' : ''}`}
              onClick={() => setChartType('line')}
            >
              Line chart
            </button>
          </div>
        </div>
      </header>

      <div className="overview-metrics">
        <article className="overview-metric-card">
          <p className="metric-label">Tong doanh thu</p>
          <p className="metric-value">{formatCurrencyVnd(totalRevenue)}</p>
        </article>
        <article className="overview-metric-card">
          <p className="metric-label">Trung binh moi thang</p>
          <p className="metric-value">{formatCurrencyVnd(avgRevenue)}</p>
        </article>
        <article className="overview-metric-card">
          <p className="metric-label">Thang cao nhat</p>
          <p className="metric-value">
            {highestMonth} - {formatCurrencyVnd(maxRevenue)}
          </p>
        </article>
      </div>

      <article className="overview-chart-card">
        <div className="chart-head">
          <h2>Doanh thu theo thang</h2>
        </div>

        {chartType === 'bar' ? (
          <div className="bar-chart" role="img" aria-label="Bieu do cot doanh thu theo thang">
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
          <div className="line-chart-card" role="img" aria-label="Bieu do duong doanh thu theo thang">
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

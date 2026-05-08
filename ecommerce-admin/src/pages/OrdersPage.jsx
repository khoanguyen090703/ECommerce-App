import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiClient, readApiErrorMessage } from '../lib/api'

const ORDER_STATUSES = ['Pending', 'Processing', 'Shipping', 'Delivered', 'Completed', 'Cancelled', 'Returned']

const ORDER_SORT_OPTIONS = [
  { value: 'orderdate_desc', label: 'Ngày đặt mới nhất' },
  { value: 'orderdate', label: 'Ngày đặt cũ nhất' },
  { value: 'totalamount_desc', label: 'Tổng tiền cao đến thấp' },
  { value: 'totalamount', label: 'Tổng tiền thấp đến cao' },
  { value: 'status', label: 'Trạng thái A-Z' },
  { value: 'status_desc', label: 'Trạng thái Z-A' },
]

async function fetchOrders({ pageNumber, pageSize, sortBy, status }) {
  const params = new URLSearchParams()
  params.set('pageNumber', String(pageNumber))
  params.set('pageSize', String(pageSize))
  if (sortBy) params.set('sortBy', sortBy)
  if (status) params.set('status', status)

  try {
    const res = await apiClient.get(`/api/orders?${params.toString()}`)
    return res.data
  } catch (error) {
    throw new Error(readApiErrorMessage(error))
  }
}

function formatDate(iso) {
  if (!iso) return '—'
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return '—'
  return d.toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' })
}

function formatCurrency(amount) {
  const n = Number(amount) || 0
  return `${n.toLocaleString('vi-VN')} ₫`
}

function statusClass(status) {
  switch ((status ?? '').toLowerCase()) {
    case 'pending': return 'orders-status-badge orders-status-badge--pending'
    case 'processing': return 'orders-status-badge orders-status-badge--processing'
    case 'shipping': return 'orders-status-badge orders-status-badge--shipping'
    case 'delivered': return 'orders-status-badge orders-status-badge--delivered'
    case 'completed': return 'orders-status-badge orders-status-badge--completed'
    case 'cancelled': return 'orders-status-badge orders-status-badge--cancelled'
    case 'returned': return 'orders-status-badge orders-status-badge--returned'
    default: return 'orders-status-badge'
  }
}

export function OrdersPage() {
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [sortBy, setSortBy] = useState('orderdate_desc')
  const [statusFilter, setStatusFilter] = useState('')
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      setLoading(true)
      setError(null)
      try {
        const result = await fetchOrders({
          pageNumber,
          pageSize,
          sortBy,
          status: statusFilter || undefined,
        })
        if (!cancelled) setData(result)
      } catch (e) {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : 'Không tải được đơn hàng.')
          setData(null)
        }
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => { cancelled = true }
  }, [pageNumber, pageSize, sortBy, statusFilter])

  const items = data?.items ?? []
  const totalPages = data?.totalPages ?? 0

  return (
    <div className="orders-page categories-page">
      <header className="categories-page-header">
        <div className="categories-page-header-left">
          <span className="eyebrow">ECommerce Admin</span>
          <h1 className="categories-title">Orders</h1>
        </div>
        <div className="categories-page-header-right">
          <div className="categories-toolbar">
            <label className="filter-field">
              <span>Sắp xếp</span>
              <select
                value={sortBy}
                onChange={(e) => {
                  setSortBy(e.target.value)
                  setPageNumber(1)
                }}
              >
                {ORDER_SORT_OPTIONS.map((o) => (
                  <option key={o.value} value={o.value}>{o.label}</option>
                ))}
              </select>
            </label>
            <label className="page-size-field">
              <span>Số dòng</span>
              <select
                value={pageSize}
                onChange={(e) => {
                  setPageSize(Number(e.target.value))
                  setPageNumber(1)
                }}
              >
                {[10, 20, 50].map((n) => (
                  <option key={n} value={n}>{n}</option>
                ))}
              </select>
            </label>
          </div>
          <div className="orders-status-filter-bar" role="tablist" aria-label="Lọc đơn hàng theo trạng thái">
            <button
              type="button"
              className={`orders-status-pill${statusFilter === '' ? ' active' : ''}`}
              onClick={() => {
                setStatusFilter('')
                setPageNumber(1)
              }}
            >
              Tất cả
            </button>
            {ORDER_STATUSES.map((status) => (
              <button
                key={status}
                type="button"
                className={`orders-status-pill${statusFilter === status ? ' active' : ''}`}
                onClick={() => {
                  setStatusFilter(status)
                  setPageNumber(1)
                }}
              >
                {status}
              </button>
            ))}
          </div>
        </div>
      </header>

      <div className="categories-table-card">
        {error && <p className="categories-error">{error}</p>}
        {loading && !data ? (
          <p className="categories-loading">Đang tải…</p>
        ) : (
          <div className="categories-table-scroll">
            <table className="categories-table">
              <thead>
                <tr>
                  <th scope="col">Id</th>
                  <th scope="col">Người nhận</th>
                  <th scope="col">Trạng thái</th>
                  <th scope="col">Thanh toán</th>
                  <th scope="col">Tổng tiền</th>
                  <th scope="col">Ngày đặt</th>
                  <th scope="col">Ngày hoàn tất</th>
                  <th scope="col" className="th-actions">Chi tiết</th>
                </tr>
              </thead>
              <tbody>
                {items.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="categories-empty">
                      Không có đơn hàng.
                    </td>
                  </tr>
                ) : (
                  items.map((row) => (
                    <tr key={row.id}>
                      <td className="td-numeric">{row.id}</td>
                      <td className="td-strong">{row.recipientName || '—'}</td>
                      <td>
                        <span className={statusClass(row.status)}>{row.status || '—'}</span>
                      </td>
                      <td className="td-muted">{row.paymentStatus || '—'}</td>
                      <td className="td-nowrap">{formatCurrency(row.totalAmount)}</td>
                      <td className="td-nowrap">{formatDate(row.orderDate)}</td>
                      <td className="td-nowrap">{formatDate(row.completedDate)}</td>
                      <td className="td-actions">
                        <Link className="btn-secondary orders-view-btn" to={`/orders/${row.id}`}>
                          Xem
                        </Link>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}

        {data && (
          <footer className="categories-pager">
            <button
              type="button"
              className="pager-btn"
              disabled={!data.hasPreviousPage}
              onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
            >
              Trước
            </button>
            <span className="pager-meta">
              Trang {data.pageNumber} / {Math.max(1, totalPages)} · {data.totalCount} đơn hàng
            </span>
            <button
              type="button"
              className="pager-btn"
              disabled={!data.hasNextPage}
              onClick={() => setPageNumber((p) => p + 1)}
            >
              Sau
            </button>
          </footer>
        )}
      </div>
    </div>
  )
}

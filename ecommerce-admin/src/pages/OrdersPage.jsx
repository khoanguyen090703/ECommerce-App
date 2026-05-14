import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiClient, readApiErrorMessage } from '../lib/api'
import {
  canAdminCancelOrder,
  orderPaymentStatusLabelVi,
  orderStatusLabelVi,
} from '../lib/orderLabels'

const ORDER_STATUSES = ['Pending', 'Processing', 'Shipping', 'Delivered', 'Cancelled']

const ORDER_SORT_OPTIONS = [
  { value: 'orderdate_desc', label: 'Ngày đặt mới nhất' },
  { value: 'orderdate', label: 'Ngày đặt cũ nhất' },
  { value: 'totalamount_desc', label: 'Tổng tiền cao đến thấp' },
  { value: 'totalamount', label: 'Tổng tiền thấp đến cao' },
  { value: 'status', label: 'Trạng thái A-Z' },
  { value: 'status_desc', label: 'Trạng thái Z-A' },
]

function IconEllipsisVertical() {
  return (
    <svg className="action-menu-trigger-icon" viewBox="0 0 24 24" aria-hidden="true">
      <path fill="currentColor" d="M12 8a2 2 0 1 0 0-4 2 2 0 0 0 0 4Zm0 2a2 2 0 1 0 0 4 2 2 0 0 0 0-4Zm0 6a2 2 0 1 0 0 4 2 2 0 0 0 0-4Z" />
    </svg>
  )
}

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
    case 'cancelled': return 'orders-status-badge orders-status-badge--cancelled'
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
  const [listVersion, setListVersion] = useState(0)
  const [actionMenu, setActionMenu] = useState(null)
  const [actionSubmitting, setActionSubmitting] = useState(false)
  const [cancelConfirmOrderId, setCancelConfirmOrderId] = useState(null)
  const actionMenuPopoverRef = useRef(null)
  const actionMenuTriggerElRef = useRef(null)

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
  }, [pageNumber, pageSize, sortBy, statusFilter, listVersion])

  useEffect(() => {
    if (!actionMenu) return undefined
    function handleDocMouseDown(e) {
      if (actionMenuPopoverRef.current?.contains(e.target)) return
      if (actionMenuTriggerElRef.current?.contains(e.target)) return
      setActionMenu(null)
      actionMenuTriggerElRef.current = null
    }
    document.addEventListener('mousedown', handleDocMouseDown)
    return () => document.removeEventListener('mousedown', handleDocMouseDown)
  }, [actionMenu])

  useEffect(() => {
    if (!actionMenu) return undefined
    function closeMenu() {
      setActionMenu(null)
      actionMenuTriggerElRef.current = null
    }
    window.addEventListener('scroll', closeMenu, true)
    window.addEventListener('resize', closeMenu)
    return () => {
      window.removeEventListener('scroll', closeMenu, true)
      window.removeEventListener('resize', closeMenu)
    }
  }, [actionMenu])

  function toggleActionMenu(e, row) {
    e.stopPropagation()
    const rect = e.currentTarget.getBoundingClientRect()
    setActionMenu((prev) => {
      if (prev?.row.id === row.id) {
        actionMenuTriggerElRef.current = null
        return null
      }
      actionMenuTriggerElRef.current = e.currentTarget
      return { row, rect }
    })
  }

  async function executeCancel() {
    if (!cancelConfirmOrderId) return
    setActionSubmitting(true)
    try {
      await apiClient.patch(`/api/orders/${cancelConfirmOrderId}/status`, { status: 'Cancelled' })
      setCancelConfirmOrderId(null)
      setListVersion((v) => v + 1)
    } catch (e) {
      window.alert(e instanceof Error ? e.message : readApiErrorMessage(e))
    } finally {
      setActionSubmitting(false)
    }
  }

  function requestCancel(orderId) {
    setActionMenu(null)
    actionMenuTriggerElRef.current = null
    setCancelConfirmOrderId(orderId)
  }

  const items = data?.items ?? []
  const totalPages = data?.totalPages ?? 0

  return (
    <div className="orders-page categories-page">
      {cancelConfirmOrderId && (
        <div
          className="confirm-backdrop"
          role="presentation"
          onClick={() => !actionSubmitting && setCancelConfirmOrderId(null)}
        >
          <div
            className="confirm-dialog"
            role="alertdialog"
            aria-modal="true"
            aria-labelledby="cancel-order-title"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 id="cancel-order-title" className="confirm-dialog-title">Xác nhận hủy đơn</h2>
            <p className="confirm-dialog-body">
              Bạn có chắc muốn hủy đơn hàng <strong>#{cancelConfirmOrderId}</strong>?
            </p>
            <div className="confirm-dialog-actions">
              <button type="button" className="btn-secondary" disabled={actionSubmitting} onClick={() => setCancelConfirmOrderId(null)}>
                Không
              </button>
              <button type="button" className="btn-danger" disabled={actionSubmitting} onClick={() => void executeCancel()}>
                {actionSubmitting ? 'Đang hủy…' : 'Hủy đơn'}
              </button>
            </div>
          </div>
        </div>
      )}

      <header className="categories-page-header">
        <div className="categories-page-header-left">
          <span className="eyebrow">Bảng quản trị</span>
          <h1 className="categories-title">Đơn hàng</h1>
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
                {orderStatusLabelVi(status)}
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
                  <th scope="col">Mã</th>
                  <th scope="col">Người nhận</th>
                  <th scope="col">Trạng thái</th>
                  <th scope="col">Thanh toán</th>
                  <th scope="col">Tổng tiền</th>
                  <th scope="col">Ngày đặt</th>
                  <th scope="col">Ngày hoàn tất</th>
                  <th scope="col" className="th-actions">Thao tác</th>
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
                        <span className={statusClass(row.status)}>{orderStatusLabelVi(row.status)}</span>
                      </td>
                      <td className="td-muted">{orderPaymentStatusLabelVi(row.paymentStatus)}</td>
                      <td className="td-nowrap">{formatCurrency(row.totalAmount)}</td>
                      <td className="td-nowrap">{formatDate(row.orderDate)}</td>
                      <td className="td-nowrap">{formatDate(row.completedDate)}</td>
                      <td className="td-actions">
                        <div className="action-menu">
                          <button
                            type="button"
                            className="action-menu-trigger"
                            aria-label="Mở thao tác"
                            aria-expanded={actionMenu?.row.id === row.id}
                            aria-haspopup="menu"
                            onClick={(e) => toggleActionMenu(e, row)}
                          >
                            <IconEllipsisVertical />
                          </button>
                        </div>
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

      {actionMenu && (
        <ul
          ref={actionMenuPopoverRef}
          className="action-menu-popover"
          role="menu"
          style={{
            top: actionMenu.rect.bottom + 6,
            right: document.documentElement.clientWidth - actionMenu.rect.right,
          }}
        >
          <li role="none">
            <Link
              to={`/orders/${actionMenu.row.id}`}
              className="action-menu-item"
              role="menuitem"
              onClick={() => {
                setActionMenu(null)
                actionMenuTriggerElRef.current = null
              }}
            >
              <span>Xem chi tiết</span>
            </Link>
          </li>
          {canAdminCancelOrder(actionMenu.row.status) && (
            <li role="none">
              <button
                type="button"
                className="action-menu-item action-menu-item--danger"
                role="menuitem"
                disabled={actionSubmitting}
                onClick={() => requestCancel(actionMenu.row.id)}
              >
                <span>Hủy đơn</span>
              </button>
            </li>
          )}
        </ul>
      )}
    </div>
  )
}

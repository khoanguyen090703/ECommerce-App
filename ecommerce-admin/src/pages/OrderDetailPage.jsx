import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { apiClient, readApiErrorMessage, resolveImageUrl } from '../lib/api'
import {
  canAdminCancelOrder,
  getNextAdminOrderStatus,
  nextAdminOrderStatusLabel,
  orderPaymentStatusLabelVi,
  orderStatusLabelVi,
} from '../lib/orderLabels'

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

export function OrderDetailPage() {
  const { orderId } = useParams()
  const id = Number(orderId)
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [actionSubmitting, setActionSubmitting] = useState(false)
  const [showCancelConfirm, setShowCancelConfirm] = useState(false)

  useEffect(() => {
    if (!Number.isFinite(id) || id < 1) {
      setError('Id đơn hàng không hợp lệ.')
      setLoading(false)
      return
    }

    let cancelled = false
    void (async () => {
      setLoading(true)
      setError(null)
      try {
        const res = await apiClient.get(`/api/orders/${id}`)
        if (!cancelled) setData(res.data)
      } catch (e) {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : readApiErrorMessage(e))
          setData(null)
        }
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()

    return () => { cancelled = true }
  }, [id])

  async function updateStatus(nextStatus) {
    setActionSubmitting(true)
    try {
      const res = await apiClient.patch(`/api/orders/${id}/status`, { status: nextStatus })
      setData(res.data)
      return true
    } catch (e) {
      window.alert(e instanceof Error ? e.message : readApiErrorMessage(e))
      return false
    } finally {
      setActionSubmitting(false)
    }
  }

  async function executeCancel() {
    const ok = await updateStatus('Cancelled')
    if (ok) setShowCancelConfirm(false)
  }

  const nextStatus = data ? getNextAdminOrderStatus(data.status) : null

  return (
    <div className="order-detail-page categories-page">
      {showCancelConfirm && (
        <div
          className="confirm-backdrop"
          role="presentation"
          onClick={() => !actionSubmitting && setShowCancelConfirm(false)}
        >
          <div
            className="confirm-dialog"
            role="alertdialog"
            aria-modal="true"
            aria-labelledby="cancel-order-detail-title"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 id="cancel-order-detail-title" className="confirm-dialog-title">Xác nhận hủy đơn</h2>
            <p className="confirm-dialog-body">Bạn có chắc muốn hủy đơn hàng này?</p>
            <div className="confirm-dialog-actions">
              <button type="button" className="btn-secondary" disabled={actionSubmitting} onClick={() => setShowCancelConfirm(false)}>
                Không
              </button>
              <button type="button" className="btn-danger" disabled={actionSubmitting} onClick={() => void executeCancel()}>
                {actionSubmitting ? 'Đang hủy…' : 'Hủy đơn'}
              </button>
            </div>
          </div>
        </div>
      )}

      <div className="product-detail-toolbar">
        <Link to="/orders" className="link-back">
          ← Danh sách đơn hàng
        </Link>
      </div>

      {loading && <p className="categories-loading">Đang tải chi tiết đơn hàng…</p>}
      {error && <p className="categories-error">{error}</p>}

      {!loading && !error && data && (
        <>
          <section className="order-detail-summary categories-table-card">
            <div className="order-detail-header">
              <div>
                <span className="eyebrow">Bảng quản trị</span>
                <h1 className="categories-title">Đơn hàng #{data.id}</h1>
              </div>
              <div className="order-detail-header-actions">
                <span className={statusClass(data.status)}>{orderStatusLabelVi(data.status)}</span>
                <div className="order-detail-action-buttons">
                  {canAdminCancelOrder(data.status) && (
                    <button
                      type="button"
                      className="btn-secondary orders-view-btn orders-view-btn--danger"
                      disabled={actionSubmitting}
                      onClick={() => setShowCancelConfirm(true)}
                    >
                      Hủy đơn
                    </button>
                  )}
                  {nextStatus && (
                    <button
                      type="button"
                      className="btn-primary orders-view-btn"
                      disabled={actionSubmitting}
                      onClick={() => void updateStatus(nextStatus)}
                    >
                      {nextAdminOrderStatusLabel(nextStatus)}
                    </button>
                  )}
                </div>
              </div>
            </div>

            <div className="order-detail-grid">
              <div>
                <strong>Người nhận</strong>
                <p>{data.recipientName || '—'}</p>
              </div>
              <div>
                <strong>Số điện thoại</strong>
                <p>{data.phoneNumber || '—'}</p>
              </div>
              <div>
                <strong>Địa chỉ giao hàng</strong>
                <p>{data.shippingAddress || '—'}</p>
              </div>
              <div>
                <strong>Ngày đặt</strong>
                <p>{formatDate(data.orderDate)}</p>
              </div>
              <div>
                <strong>Ngày hoàn tất</strong>
                <p>{formatDate(data.completedDate)}</p>
              </div>
              <div>
                <strong>Ngày hủy</strong>
                <p>{formatDate(data.cancelledDate)}</p>
              </div>
              <div>
                <strong>Thanh toán</strong>
                <p>{orderPaymentStatusLabelVi(data.paymentStatus)}</p>
              </div>
              <div>
                <strong>Tạm tính</strong>
                <p>{formatCurrency(data.subTotal)}</p>
              </div>
              <div>
                <strong>Phí ship</strong>
                <p>{formatCurrency(data.shippingFee)}</p>
              </div>
              <div>
                <strong>Tổng tiền</strong>
                <p>{formatCurrency(data.totalAmount)}</p>
              </div>
            </div>
          </section>

          <section className="categories-table-card">
            <div className="categories-table-scroll">
              <table className="categories-table">
                <thead>
                  <tr>
                    <th scope="col">Ảnh</th>
                    <th scope="col">Sản phẩm</th>
                    <th scope="col">Số lượng</th>
                    <th scope="col">Đơn giá</th>
                    <th scope="col">Thành tiền</th>
                  </tr>
                </thead>
                <tbody>
                  {(data.orderItems ?? []).length === 0 ? (
                    <tr>
                      <td colSpan={5} className="categories-empty">Không có sản phẩm trong đơn hàng.</td>
                    </tr>
                  ) : (
                    data.orderItems.map((item) => (
                      <tr key={item.id}>
                        <td className="td-thumb">
                          {item.imageUrl ? (
                            <img className="thumb-img" src={resolveImageUrl(item.imageUrl) ?? item.imageUrl} alt="" loading="lazy" />
                          ) : (
                            <span className="thumb-placeholder">—</span>
                          )}
                        </td>
                        <td className="td-strong">{item.productName || '—'}</td>
                        <td className="td-numeric">{item.quantity ?? 0}</td>
                        <td className="td-nowrap">{formatCurrency(item.unitPrice)}</td>
                        <td className="td-nowrap">{formatCurrency((Number(item.quantity) || 0) * (Number(item.unitPrice) || 0))}</td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </section>
        </>
      )}
    </div>
  )
}

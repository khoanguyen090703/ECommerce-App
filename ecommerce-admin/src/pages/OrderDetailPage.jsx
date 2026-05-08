import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { apiClient, readApiErrorMessage, resolveImageUrl } from '../lib/api'

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

export function OrderDetailPage() {
  const { orderId } = useParams()
  const id = Number(orderId)
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

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

  return (
    <div className="order-detail-page categories-page">
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
                <span className="eyebrow">Order</span>
                <h1 className="categories-title">#{data.id}</h1>
              </div>
              <span className={statusClass(data.status)}>{data.status || '—'}</span>
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
                <strong>Thanh toán</strong>
                <p>{data.paymentStatus || '—'}</p>
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

/** Hiển thị tiếng Việt cho giá trị trạng thái từ API (enum dạng chuỗi). */

export const ORDER_STATUS_VI = {
  Pending: 'Chờ xử lý',
  Processing: 'Đang xử lý',
  Shipping: 'Đang giao',
  Delivered: 'Đã giao',
  Cancelled: 'Đã hủy',
}

export function orderStatusLabelVi(status) {
  if (status == null || status === '') return '—'
  return ORDER_STATUS_VI[status] ?? status
}

export const ORDER_PAYMENT_STATUS_VI = {
  Unpaid: 'Chưa thanh toán',
  Paid: 'Đã thanh toán',
  PartiallyRefunded: 'Hoàn tiền một phần',
  FullyRefunded: 'Hoàn tiền toàn bộ',
}

export function orderPaymentStatusLabelVi(status) {
  if (status == null || status === '') return '—'
  return ORDER_PAYMENT_STATUS_VI[status] ?? status
}

export function canAdminCancelOrder(status) {
  return (status ?? '').toLowerCase() === 'pending'
}

export function getNextAdminOrderStatus(status) {
  switch ((status ?? '').toLowerCase()) {
    case 'pending':
      return 'Processing'
    case 'processing':
      return 'Shipping'
    case 'shipping':
      return 'Delivered'
    default:
      return null
  }
}

export function nextAdminOrderStatusLabel(nextStatus) {
  const labels = {
    Processing: 'Chuyển sang đang xử lý',
    Shipping: 'Chuyển sang đang giao',
    Delivered: 'Đánh dấu đã giao',
  }
  return labels[nextStatus] ?? nextStatus
}

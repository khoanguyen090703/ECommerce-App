import { useCallback, useEffect, useRef, useState } from 'react'
import toast from 'react-hot-toast'
import { apiClient, readApiErrorMessage, resolveImageUrl } from '../lib/api'

const STATUS_OPTIONS = [
  { value: '', label: 'Tất cả (Available / Out of stock)' },
  { value: 'Available', label: 'Available' },
  { value: 'OutOfStock', label: 'Out of stock' },
]

const SORT_OPTIONS = [
  { value: 'id', label: 'Id ↑' },
  { value: 'id_desc', label: 'Id ↓' },
  { value: 'name', label: 'Tên A–Z' },
  { value: 'name_desc', label: 'Tên Z–A' },
  { value: 'stock', label: 'Tồn ↑' },
  { value: 'stock_desc', label: 'Tồn ↓' },
  { value: 'status', label: 'Trạng thái A–Z' },
  { value: 'status_desc', label: 'Trạng thái Z–A' },
]

function ThumbnailCell({ imageUrl, name }) {
  const src = resolveImageUrl(imageUrl)
  if (!src) return <span className="thumb-placeholder">—</span>
  return (
    <div className="thumb-wrap">
      <img className="thumb-img" src={src} alt="" loading="lazy" />
      <div className="thumb-preview" aria-hidden="true">
        <img src={src} alt={name || ''} />
      </div>
    </div>
  )
}

export function VariantRestockPage() {
  const [lines, setLines] = useState([])

  const [pickOpen, setPickOpen] = useState(false)
  const [pickerData, setPickerData] = useState(null)
  const [pickerLoading, setPickerLoading] = useState(false)
  const [pickerError, setPickerError] = useState(null)
  const [vPage, setVPage] = useState(1)
  const [vPageSize, setVPageSize] = useState(10)
  const [vSort, setVSort] = useState('id')
  const [vSearch, setVSearch] = useState('')
  const [vDebounced, setVDebounced] = useState('')
  const vSearchTimer = useRef(0)
  const [vStatus, setVStatus] = useState('')

  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    window.clearTimeout(vSearchTimer.current)
    vSearchTimer.current = window.setTimeout(() => {
      setVDebounced(vSearch)
      setVPage(1)
    }, 300)
    return () => window.clearTimeout(vSearchTimer.current)
  }, [vSearch])

  const loadPicker = useCallback(async () => {
    if (!pickOpen) return
    setPickerLoading(true)
    setPickerError(null)
    try {
      const params = new URLSearchParams()
      params.set('pageNumber', String(vPage))
      params.set('pageSize', String(vPageSize))
      params.set('sortBy', vSort)
      if (vDebounced.trim()) params.set('searchTerm', vDebounced.trim())
      if (vStatus) params.set('status', vStatus)
      const res = await apiClient.get(`/api/variants/restock?${params.toString()}`)
      setPickerData(res.data)
    } catch (e) {
      setPickerError(readApiErrorMessage(e))
      setPickerData(null)
    } finally {
      setPickerLoading(false)
    }
  }, [pickOpen, vPage, vPageSize, vSort, vDebounced, vStatus])

  useEffect(() => {
    void loadPicker()
  }, [loadPicker])

  function openPicker() {
    setVPage(1)
    setVSearch('')
    setVDebounced('')
    setVStatus('')
    setVSort('id')
    setPickerData(null)
    setPickerError(null)
    setPickOpen(true)
  }

  function selectVariant(row) {
    if (lines.some((l) => l.id === row.id)) {
      toast.error('Biến thể này đã có trong danh sách nhập kho.')
      return
    }
    setLines((prev) => [
      ...prev,
      {
        id: row.id,
        name: row.name,
        firstImageUrl: row.firstImageUrl,
        stockQuantity: row.stockQuantity,
        status: row.status,
        productId: row.productId,
        productName: row.productName,
        quantityToAdd: '1',
      },
    ])
    setPickOpen(false)
    toast.success('Đã thêm biến thể vào danh sách.')
  }

  function updateLineQuantity(id, value) {
    setLines((prev) => prev.map((l) => (l.id === id ? { ...l, quantityToAdd: value } : l)))
  }

  function removeLine(id) {
    setLines((prev) => prev.filter((l) => l.id !== id))
  }

  async function submitRestock(e) {
    e.preventDefault()
    if (lines.length === 0) {
      toast.error('Thêm ít nhất một biến thể.')
      return
    }
    const items = []
    const err = []
    for (const l of lines) {
      const q = Number(l.quantityToAdd)
      if (Number.isNaN(q) || q < 1) err.push(l.id)
      else items.push({ variantId: l.id, quantityToAdd: q })
    }
    if (err.length) {
      toast.error('Số lượng nhập mỗi dòng phải là số nguyên ≥ 1.')
      return
    }
    setSubmitting(true)
    try {
      await apiClient.post('/api/variants/restock', { items })
      toast.success('Đã cập nhật tồn kho.')
      setLines([])
    } catch (error) {
      toast.error(readApiErrorMessage(error))
    } finally {
      setSubmitting(false)
    }
  }

  const vItems = pickerData?.items ?? []
  const vTotalPages = pickerData?.totalPages ?? 0

  return (
    <div className="categories-page variant-restock-page">
      <header className="categories-page-header">
        <div className="categories-page-header-left">
          <span className="eyebrow">Bảng quản trị</span>
          <h1 className="categories-title">Nhập kho biến thể</h1>
          <p className="td-muted" style={{ marginTop: '0.35rem', maxWidth: '42rem' }}>
            Cộng thêm tồn cho một hoặc nhiều biến thể (chỉ Available / Out of stock).
          </p>
        </div>
        <div className="categories-page-header-right">
          <button type="button" className="btn-primary categories-add-btn" onClick={openPicker}>
            + Thêm biến thể
          </button>
        </div>
      </header>

      {pickOpen && (
        <div className="modal-backdrop modal-backdrop--blocking" role="presentation">
          <div
            className="modal modal--wide variant-restock-picker-modal"
            role="dialog"
            aria-label="Chọn biến thể"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 className="variant-restock-picker-title">Chọn biến thể</h2>
            <div className="product-variants-toolbar variant-restock-picker-toolbar">
              <label className="search-field">
                <span className="sr-only">Tìm kiếm</span>
                <input
                  type="search"
                  placeholder="Tìm theo tên biến thể hoặc sản phẩm…"
                  value={vSearch}
                  onChange={(e) => setVSearch(e.target.value)}
                />
              </label>
              <label className="filter-field">
                <span>Trạng thái</span>
                <select value={vStatus} onChange={(e) => { setVStatus(e.target.value); setVPage(1) }}>
                  {STATUS_OPTIONS.map((o) => (
                    <option key={o.value || 'all'} value={o.value}>
                      {o.label}
                    </option>
                  ))}
                </select>
              </label>
              <label className="filter-field">
                <span>Sắp xếp</span>
                <select value={vSort} onChange={(e) => { setVSort(e.target.value); setVPage(1) }}>
                  {SORT_OPTIONS.map((o) => (
                    <option key={o.value} value={o.value}>
                      {o.label}
                    </option>
                  ))}
                </select>
              </label>
              <label className="page-size-field">
                <span>Số dòng</span>
                <select
                  value={vPageSize}
                  onChange={(e) => {
                    setVPageSize(Number(e.target.value))
                    setVPage(1)
                  }}
                >
                  {[5, 10, 20, 50].map((n) => (
                    <option key={n} value={n}>
                      {n}
                    </option>
                  ))}
                </select>
              </label>
            </div>

            {pickerError && <p className="categories-error variant-restock-picker-error">{pickerError}</p>}
            <div className="variant-restock-picker-body">
              {pickerLoading && !pickerData ? (
                <p className="categories-loading variant-restock-picker-loading">Đang tải…</p>
              ) : (
                <div className="categories-table-scroll variant-restock-picker-table-scroll">
                  <table className="categories-table">
                    <thead>
                      <tr>
                        <th>Ảnh</th>
                        <th>Id</th>
                        <th>Tên biến thể</th>
                        <th>Sản phẩm</th>
                        <th>Tồn</th>
                        <th>Trạng thái</th>
                        <th className="th-actions"> </th>
                      </tr>
                    </thead>
                    <tbody>
                      {vItems.length === 0 ? (
                        <tr>
                          <td colSpan={7} className="td-muted">
                            Không có biến thể phù hợp.
                          </td>
                        </tr>
                      ) : (
                        vItems.map((row) => (
                          <tr key={row.id}>
                            <td>
                              <ThumbnailCell imageUrl={row.firstImageUrl} name={row.name} />
                            </td>
                            <td>{row.id}</td>
                            <td>{row.name}</td>
                            <td>{row.productName}</td>
                            <td className="td-numeric">{row.stockQuantity}</td>
                            <td>{row.status}</td>
                            <td className="th-actions">
                              <button type="button" className="btn-primary" onClick={() => selectVariant(row)}>
                                Chọn
                              </button>
                            </td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>
              )}
            </div>

            {pickerData && (
              <footer className="categories-pager variant-restock-picker-pager">
                <button type="button" className="pager-btn" disabled={!pickerData.hasPreviousPage} onClick={() => setVPage((p) => Math.max(1, p - 1))}>
                  Trước
                </button>
                <span className="pager-meta">
                  Trang {pickerData.pageNumber} / {Math.max(1, vTotalPages)} · {pickerData.totalCount} biến thể
                </span>
                <button type="button" className="pager-btn" disabled={!pickerData.hasNextPage} onClick={() => setVPage((p) => p + 1)}>
                  Sau
                </button>
              </footer>
            )}

            <div className="modal-actions variant-restock-picker-actions">
              <button type="button" className="btn-secondary" onClick={() => setPickOpen(false)}>
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}

      <section className="categories-table-card variant-restock-list-card">
        <div className="variant-restock-section-head">
          <h2 className="variant-restock-section-title">Danh sách nhập kho</h2>
        </div>
        {lines.length === 0 ? (
          <p className="td-muted variant-restock-list-empty">Chưa có biến thể. Bấm &quot;Thêm biến thể&quot; để chọn từ danh sách.</p>
        ) : (
          <form onSubmit={(e) => void submitRestock(e)}>
            <div className="categories-table-scroll">
              <table className="categories-table">
                <thead>
                  <tr>
                    <th>Ảnh</th>
                    <th>Tên</th>
                    <th>Sản phẩm</th>
                    <th>Tồn hiện tại</th>
                    <th>Số lượng nhập thêm</th>
                    <th className="th-actions"> </th>
                  </tr>
                </thead>
                <tbody>
                  {lines.map((l) => (
                    <tr key={l.id}>
                      <td>
                        <ThumbnailCell imageUrl={l.firstImageUrl} name={l.name} />
                      </td>
                      <td>{l.name}</td>
                      <td>{l.productName}</td>
                      <td className="td-numeric">{l.stockQuantity}</td>
                      <td>
                        <input
                          className="restock-qty-input"
                          value={l.quantityToAdd}
                          onChange={(e) => updateLineQuantity(l.id, e.target.value)}
                          inputMode="numeric"
                          min={1}
                          aria-label={`Số lượng nhập cho ${l.name}`}
                        />
                      </td>
                      <td className="th-actions">
                        <button type="button" className="btn-secondary" onClick={() => removeLine(l.id)}>
                          Xóa
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="variant-restock-actions">
              <button type="submit" className="btn-primary variant-restock-submit-btn" disabled={submitting}>
                {submitting ? 'Đang lưu…' : 'Xác nhận nhập kho'}
              </button>
            </div>
          </form>
        )}
      </section>
    </div>
  )
}

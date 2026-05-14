import { useEffect, useRef, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { apiClient, readApiErrorMessage, resolveImageUrl } from '../lib/api'

const PRODUCT_STATUSES = ['Draft', 'Active', 'Inactive', 'Archived']

const PRODUCT_STATUS_VI = {
  Draft: 'Bản nháp',
  Active: 'Đang bán',
  Inactive: 'Ngừng bán',
  Archived: 'Lưu trữ',
}

const VARIANT_STATUS_VI = {
  Available: 'Còn hàng',
  OutOfStock: 'Hết hàng',
  Discontinued: 'Ngừng kinh doanh',
}
async function fetchProducts({
  pageNumber,
  pageSize,
  searchTerm,
  sortBy,
  status,
  brandId,
  categoryId,
  scentFamilyId,
}) {
  const params = new URLSearchParams()
  params.set('pageNumber', String(pageNumber))
  params.set('pageSize', String(pageSize))
  if (searchTerm?.trim()) params.set('searchTerm', searchTerm.trim())
  if (sortBy) params.set('sortBy', sortBy)
  if (status) params.set('status', status)
  if (brandId) params.set('brandId', String(brandId))
  if (categoryId) params.set('categoryId', String(categoryId))
  if (scentFamilyId) params.set('scentFamilyId', String(scentFamilyId))
  try {
    const res = await apiClient.get(`/api/products?${params.toString()}`)
    return res.data
  } catch (error) {
    throw new Error(readApiErrorMessage(error))
  }
}

function IconEllipsisVertical() {
  return (
    <svg className="action-menu-trigger-icon" viewBox="0 0 24 24" aria-hidden="true">
      <path d="M12 8a2 2 0 1 0 0-4 2 2 0 0 0 0 4Zm0 6a2 2 0 1 0 0-4 2 2 0 0 0 0 4Zm0 6a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z" />
    </svg>
  )
}

function IconEye() {
  return (
    <svg className="action-menu-item-icon" viewBox="0 0 24 24" aria-hidden="true">
      <path d="M12 9a3 3 0 1 0 0 6 3 3 0 0 0 0-6Zm0-4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5Zm0 12.5a5 5 0 1 1 0-10 5 5 0 0 1 0 10Z" />
    </svg>
  )
}

function IconPencil() {
  return (
    <svg className="action-menu-item-icon" viewBox="0 0 24 24" aria-hidden="true">
      <path d="M3 17.46v3.04h3.04L17.81 8.73l-3.04-3.04L3 17.46Zm14.71-9.33a.81.81 0 0 0 0-1.15l-1.89-1.89a.81.81 0 0 0-1.15 0l-1.47 1.47 3.04 3.04 1.47-1.47Z" />
    </svg>
  )
}

function IconTrash() {
  return (
    <svg className="action-menu-item-icon action-menu-item-icon--danger" viewBox="0 0 24 24" aria-hidden="true">
      <path d="M6 19a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2V7H6v12ZM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4Z" />
    </svg>
  )
}

function ThumbnailCell({ imageUrl, name }) {
  const src = resolveImageUrl(imageUrl)
  if (!src) {
    return <span className="thumb-placeholder">—</span>
  }
  return (
    <div className="thumb-wrap">
      <img className="thumb-img" src={src} alt="" loading="lazy" />
      <div className="thumb-preview" aria-hidden="true">
        <img src={src} alt={name || 'Product'} />
      </div>
    </div>
  )
}

function SortableTh({ label, columnKey, currentSort, onSort }) {
  const isActive = currentSort === columnKey || currentSort === `${columnKey}_desc`
  const isDesc = currentSort === `${columnKey}_desc`
  return (
    <th scope="col">
      <button type="button" className={`sort-btn${isActive ? ' sort-btn--active' : ''}`} onClick={() => onSort(columnKey)}>
        <span>{label}</span>
        <span className="sort-indicator" aria-hidden="true">
          {isActive ? (isDesc ? '↓' : '↑') : '↕'}
        </span>
      </button>
    </th>
  )
}

function formatDate(iso) {
  if (iso == null || iso === '') return '—'
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return '—'
  return d.toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' })
}

function statusLabel(s) {
  if (s == null) return '—'
  const key = String(s)
  return PRODUCT_STATUS_VI[key] ?? key
}

function variantStatusLabel(s) {
  if (s == null) return '—'
  const key = String(s)
  return VARIANT_STATUS_VI[key] ?? key
}

export function ProductsPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [sortBy, setSortBy] = useState('id_desc')
  const [searchInput, setSearchInput] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [filterStatus, setFilterStatus] = useState('')
  const [filterBrandId, setFilterBrandId] = useState('')
  const [filterCategoryId, setFilterCategoryId] = useState('')
  const [filterScentFamilyId, setFilterScentFamilyId] = useState('')

  const [brands, setBrands] = useState([])
  const [categories, setCategories] = useState([])
  const [scentFamilies, setScentFamilies] = useState([])

  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [listVersion, setListVersion] = useState(0)

  const [expandedProductId, setExpandedProductId] = useState(null)
  const [actionMenu, setActionMenu] = useState(null)
  const actionMenuPopoverRef = useRef(null)
  const actionMenuTriggerElRef = useRef(null)

  const [deleteRow, setDeleteRow] = useState(null)
  const [deleteSubmitting, setDeleteSubmitting] = useState(false)
  const [deleteErrorMessage, setDeleteErrorMessage] = useState(null)

  const [toastMessage, setToastMessage] = useState(null)
  const toastClearRef = useRef(0)
  const searchDebounceRef = useRef(0)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const [bRes, cRes, sRes] = await Promise.all([
          apiClient.get('/api/brands/all'),
          apiClient.get('/api/categories/all'),
          apiClient.get('/api/scentfamilies'),
        ])
        if (cancelled) return
        setBrands(bRes.data)
        setCategories(cRes.data)
        setScentFamilies(sRes.data)
      } catch {
        /* filters optional */
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    let cancelled = false
    void (async () => {
      setLoading(true)
      setError(null)
      try {
        const result = await fetchProducts({
          pageNumber,
          pageSize,
          searchTerm: debouncedSearch,
          sortBy,
          status: filterStatus || undefined,
          brandId: filterBrandId ? Number(filterBrandId) : undefined,
          categoryId: filterCategoryId ? Number(filterCategoryId) : undefined,
          scentFamilyId: filterScentFamilyId ? Number(filterScentFamilyId) : undefined,
        })
        if (cancelled) return
        setData(result)
      } catch (e) {
        if (cancelled) return
        setError(e instanceof Error ? e.message : 'Không tải được sản phẩm.')
        setData(null)
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [
    pageNumber,
    pageSize,
    debouncedSearch,
    sortBy,
    filterStatus,
    filterBrandId,
    filterCategoryId,
    filterScentFamilyId,
    listVersion,
  ])

  useEffect(() => {
    return () => window.clearTimeout(searchDebounceRef.current)
  }, [])

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

  useEffect(() => {
    if (!toastMessage) return undefined
    window.clearTimeout(toastClearRef.current)
    toastClearRef.current = window.setTimeout(() => setToastMessage(null), 4500)
    return () => window.clearTimeout(toastClearRef.current)
  }, [toastMessage])

  useEffect(() => {
    const msg = location.state?.toastMessage
    const refreshList = location.state?.refreshList === true
    const hasMsg = typeof msg === 'string' && msg.trim()
    if (!hasMsg && !refreshList) return
    queueMicrotask(() => {
      if (hasMsg) setToastMessage(msg.trim())
      if (refreshList) {
        setPageNumber(1)
        setListVersion((v) => v + 1)
      }
      navigate('/products', { replace: true, state: {} })
    })
  }, [location.state, navigate])

  function handleSearchChange(value) {
    setSearchInput(value)
    window.clearTimeout(searchDebounceRef.current)
    searchDebounceRef.current = window.setTimeout(() => {
      setDebouncedSearch(value)
      setPageNumber(1)
    }, 350)
  }

  const handleSort = (columnKey) => {
    setSortBy((prev) => {
      if (prev === `${columnKey}_desc`) return columnKey
      return `${columnKey}_desc`
    })
    setPageNumber(1)
  }

  function toggleRowExpand(id) {
    setExpandedProductId((prev) => (prev === id ? null : id))
  }

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

  function requestDelete(row) {
    setActionMenu(null)
    actionMenuTriggerElRef.current = null
    setDeleteRow(row)
  }

  async function executeDelete() {
    if (!deleteRow) return
    setDeleteSubmitting(true)
    try {
      await apiClient.delete(`/api/products/${deleteRow.id}`)
      setDeleteRow(null)
      setToastMessage('Đã xóa sản phẩm.')
      if (data?.items?.length === 1 && pageNumber > 1) setPageNumber((p) => p - 1)
      else setListVersion((v) => v + 1)
    } catch (error) {
      setDeleteRow(null)
      setDeleteErrorMessage(readApiErrorMessage(error))
    } finally {
      setDeleteSubmitting(false)
    }
  }

  const items = data?.items ?? []
  const totalPages = data?.totalPages ?? 0

  return (
    <div className="products-page categories-page">
      {toastMessage && (
        <div className="toast" role="status" aria-live="polite">
          {toastMessage}
        </div>
      )}

      {deleteRow && (
        <div
          className="confirm-backdrop"
          role="presentation"
          onClick={() => !deleteSubmitting && setDeleteRow(null)}
        >
          <div
            className="confirm-dialog"
            role="alertdialog"
            aria-modal="true"
            aria-labelledby="del-prod-title"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 id="del-prod-title" className="confirm-dialog-title">
              Xác nhận xóa
            </h2>
            <p className="confirm-dialog-body">
              Chỉ sản phẩm ở trạng thái <strong>Draft</strong> mới được xóa. Xóa <strong>{deleteRow.name}</strong>?
            </p>
            <div className="confirm-dialog-actions">
              <button type="button" className="btn-secondary" disabled={deleteSubmitting} onClick={() => setDeleteRow(null)}>
                Hủy
              </button>
              <button type="button" className="btn-danger" disabled={deleteSubmitting} onClick={() => void executeDelete()}>
                {deleteSubmitting ? 'Đang xóa…' : 'Xóa'}
              </button>
            </div>
          </div>
        </div>
      )}

      {deleteErrorMessage && (
        <div className="delete-error-backdrop" role="presentation" onClick={() => setDeleteErrorMessage(null)}>
          <div className="delete-error-dialog" role="alertdialog" onClick={(e) => e.stopPropagation()}>
            <h2 className="delete-error-title">Không thể xóa</h2>
            <p className="delete-error-body">{deleteErrorMessage}</p>
            <button type="button" className="delete-error-dismiss" onClick={() => setDeleteErrorMessage(null)}>
              Đã hiểu
            </button>
          </div>
        </div>
      )}

      <header className="categories-page-header">
        <div className="categories-page-header-left">
          <span className="eyebrow">Bảng quản trị</span>
          <h1 className="categories-title">Sản phẩm</h1>
        </div>
        <div className="categories-page-header-right">
          <button type="button" className="btn-primary categories-add-btn" onClick={() => navigate('/products/new')}>
            Thêm sản phẩm
          </button>
          <div className="categories-toolbar products-toolbar">
            <label className="search-field">
              <span className="sr-only">Tìm kiếm</span>
              <input
                type="search"
                placeholder="Tìm theo tên hoặc mô tả…"
                value={searchInput}
                onChange={(e) => handleSearchChange(e.target.value)}
                autoComplete="off"
              />
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
                  <option key={n} value={n}>
                    {n}
                  </option>
                ))}
              </select>
            </label>
          </div>
          <div className="products-filters">
            <label className="filter-field">
              <span>Trạng thái</span>
              <select
                value={filterStatus}
                onChange={(e) => {
                  setFilterStatus(e.target.value)
                  setPageNumber(1)
                }}
              >
                <option value="">Tất cả</option>
                {PRODUCT_STATUSES.map((s) => (
                  <option key={s} value={s}>
                    {PRODUCT_STATUS_VI[s] ?? s}
                  </option>
                ))}
              </select>
            </label>
            <label className="filter-field">
              <span>Thương hiệu</span>
              <select
                value={filterBrandId}
                onChange={(e) => {
                  setFilterBrandId(e.target.value)
                  setPageNumber(1)
                }}
              >
                <option value="">Tất cả</option>
                {brands.map((b) => (
                  <option key={b.id} value={b.id}>
                    {b.name}
                  </option>
                ))}
              </select>
            </label>
            <label className="filter-field">
              <span>Danh mục</span>
              <select
                value={filterCategoryId}
                onChange={(e) => {
                  setFilterCategoryId(e.target.value)
                  setPageNumber(1)
                }}
              >
                <option value="">Tất cả</option>
                {categories.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
            </label>
            <label className="filter-field">
              <span>Nhóm hương</span>
              <select
                value={filterScentFamilyId}
                onChange={(e) => {
                  setFilterScentFamilyId(e.target.value)
                  setPageNumber(1)
                }}
              >
                <option value="">Tất cả</option>
                {scentFamilies.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.name}
                  </option>
                ))}
              </select>
            </label>
          </div>
        </div>
      </header>

      <div className="categories-table-card">
        {error && <p className="categories-error">{error}</p>}
        {loading && !data ? (
          <p className="categories-loading">Đang tải…</p>
        ) : (
          <div className="categories-table-scroll">
            <table className="categories-table products-table">
              <thead>
                <tr>
                  <th scope="col" className="th-expand" aria-label="Mở rộng" />
                  <SortableTh label="Mã" columnKey="id" currentSort={sortBy} onSort={handleSort} />
                  <th scope="col">Ảnh</th>
                  <SortableTh label="Tên" columnKey="name" currentSort={sortBy} onSort={handleSort} />
                  <th scope="col">Danh mục</th>
                  <SortableTh label="Trạng thái" columnKey="status" currentSort={sortBy} onSort={handleSort} />
                  <th scope="col">Biến thể</th>
                  <th scope="col">Đánh giá</th>
                  <SortableTh label="Tạo" columnKey="created" currentSort={sortBy} onSort={handleSort} />
                  <th scope="col" className="th-actions">
                    Thao tác
                  </th>
                </tr>
              </thead>
              <tbody>
                {items.length === 0 ? (
                  <tr>
                    <td colSpan={10} className="categories-empty">
                      Không có sản phẩm.
                    </td>
                  </tr>
                ) : (
                  items.flatMap((row) => {
                    const expanded = expandedProductId === row.id
                    const variants = row.variants ?? []
                    const mainRow = (
                      <tr
                        key={row.id}
                        className={`product-main-row${expanded ? ' product-main-row--expanded' : ''}`}
                        onClick={() => toggleRowExpand(row.id)}
                        role="button"
                        tabIndex={0}
                        onKeyDown={(e) => {
                          if (e.key === 'Enter' || e.key === ' ') {
                            e.preventDefault()
                            toggleRowExpand(row.id)
                          }
                        }}
                      >
                        <td className="td-expand">
                          <span className="expand-chevron" aria-hidden="true">
                            {expanded ? '▼' : '▶'}
                          </span>
                        </td>
                        <td className="td-numeric">{row.id}</td>
                        <td className="td-thumb" onClick={(e) => e.stopPropagation()}>
                          <ThumbnailCell imageUrl={row.imageUrl} name={row.name} />
                        </td>
                        <td className="td-strong">{row.name}</td>
                        <td className="td-muted">{row.categories?.trim() ? row.categories : '—'}</td>
                        <td className="td-nowrap">
                          <span className={`product-status-badge product-status-badge--${String(row.status).toLowerCase()}`}>
                            {statusLabel(row.status)}
                          </span>
                        </td>
                        <td className="td-numeric">{row.totalVariants ?? variants.length}</td>
                        <td className="td-muted">
                          {row.averageRating != null ? Number(row.averageRating).toFixed(1) : '—'} ({row.totalReviews ?? 0})
                        </td>
                        <td className="td-nowrap">{formatDate(row.createdDate)}</td>
                        <td className="td-actions" onClick={(e) => e.stopPropagation()}>
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
                    )
                    const expandRow = expanded ? (
                      <tr key={`${row.id}-variants`} className="product-variant-row">
                        <td colSpan={10} className="product-variant-cell">
                          <div className="product-variant-panel">
                            <p className="product-variant-title">Biến thể ({variants.length})</p>
                            {variants.length === 0 ? (
                              <p className="td-muted">Không có biến thể.</p>
                            ) : (
                              <table className="product-variant-subtable">
                                <thead>
                                  <tr>
                                    <th>Mã</th>
                                    <th>Ảnh</th>
                                    <th>Tên</th>
                                    <th>Giá</th>
                                    <th>Tồn</th>
                                    <th>Trạng thái</th>
                                  </tr>
                                </thead>
                                <tbody>
                                  {variants.map((v) => (
                                    <tr key={v.id}>
                                      <td className="td-numeric">{v.id}</td>
                                      <td>
                                        <ThumbnailCell imageUrl={v.imageUrl} name={v.name} />
                                      </td>
                                      <td className="td-strong">{v.name}</td>
                                      <td className="td-nowrap">{Number(v.price).toLocaleString('vi-VN')} ₫</td>
                                      <td className="td-numeric">{v.stockQuantity}</td>
                                      <td>{variantStatusLabel(v.status)}</td>
                                    </tr>
                                  ))}
                                </tbody>
                              </table>
                            )}
                          </div>
                        </td>
                      </tr>
                    ) : null
                    return expandRow ? [mainRow, expandRow] : [mainRow]
                  })
                )}
              </tbody>
            </table>
          </div>
        )}

        {data && (
          <footer className="categories-pager">
            <button type="button" className="pager-btn" disabled={!data.hasPreviousPage} onClick={() => setPageNumber((p) => Math.max(1, p - 1))}>
              Trước
            </button>
            <span className="pager-meta">
              Trang {data.pageNumber} / {Math.max(1, totalPages)} · {data.totalCount} mục
            </span>
            <button type="button" className="pager-btn" disabled={!data.hasNextPage} onClick={() => setPageNumber((p) => p + 1)}>
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
            <button
              type="button"
              className="action-menu-item"
              role="menuitem"
              onClick={() => {
                const row = actionMenu.row
                setActionMenu(null)
                actionMenuTriggerElRef.current = null
                navigate(`/products/${row.id}`)
              }}
            >
              <IconEye />
              <span>Xem chi tiết</span>
            </button>
          </li>
          <li role="none">
            <button
              type="button"
              className="action-menu-item"
              role="menuitem"
              onClick={() => {
                const row = actionMenu.row
                setActionMenu(null)
                actionMenuTriggerElRef.current = null
                navigate(`/products/${row.id}/edit`)
              }}
            >
              <IconPencil />
              <span>Cập nhật</span>
            </button>
          </li>
          <li role="none">
            <button type="button" className="action-menu-item action-menu-item--danger" role="menuitem" onClick={() => requestDelete(actionMenu.row)}>
              <IconTrash />
              <span>Xóa</span>
            </button>
          </li>
        </ul>
      )}

    </div>
  )
}

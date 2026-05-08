import { useEffect, useState } from 'react'
import { apiClient, readApiErrorMessage, resolveImageUrl } from '../lib/api'

const CUSTOMER_SORT_OPTIONS = [
  { value: 'created_desc', label: 'Tạo mới nhất' },
  { value: 'created', label: 'Tạo cũ nhất' },
  { value: 'fullname', label: 'Họ tên A–Z' },
  { value: 'fullname_desc', label: 'Họ tên Z–A' },
  { value: 'updated_desc', label: 'Cập nhật mới nhất' },
  { value: 'updated', label: 'Cập nhật cũ nhất' },
]

function formatDate(iso) {
  if (iso == null || iso === '') return '—'
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return '—'
  return d.toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' })
}

function AvatarCell({ avatarUrl, fullName }) {
  const src = resolveImageUrl(avatarUrl)
  if (!src) {
    const fallback = (fullName ?? '')
      .trim()
      .split(/\s+/)
      .slice(0, 2)
      .map((p) => p[0]?.toUpperCase() ?? '')
      .join('') || 'U'
    return <span className="customer-avatar customer-avatar--fallback">{fallback}</span>
  }
  return (
    <img className="customer-avatar" src={src} alt={fullName || ''} loading="lazy" />
  )
}

async function fetchCustomers({ pageNumber, pageSize, sortBy, searchTerm }) {
  const params = new URLSearchParams()
  params.set('pageNumber', String(pageNumber))
  params.set('pageSize', String(pageSize))
  params.set('sortBy', sortBy)
  if (searchTerm?.trim()) params.set('searchTerm', searchTerm.trim())
  try {
    const res = await apiClient.get(`/api/customers?${params.toString()}`)
    return res.data
  } catch (error) {
    throw new Error(readApiErrorMessage(error))
  }
}

export function CustomersPage() {
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [sortBy, setSortBy] = useState('created_desc')
  const [searchInput, setSearchInput] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    const t = window.setTimeout(() => {
      setDebouncedSearch(searchInput)
      setPageNumber(1)
    }, 300)
    return () => window.clearTimeout(t)
  }, [searchInput])

  useEffect(() => {
    let cancelled = false
    void (async () => {
      setLoading(true)
      setError(null)
      try {
        const result = await fetchCustomers({
          pageNumber,
          pageSize,
          sortBy,
          searchTerm: debouncedSearch,
        })
        if (!cancelled) setData(result)
      } catch (e) {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : 'Không tải được khách hàng.')
          setData(null)
        }
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [pageNumber, pageSize, sortBy, debouncedSearch])

  const items = data?.items ?? []
  const totalPages = data?.totalPages ?? 0

  return (
    <div className="customers-page categories-page">
      <header className="categories-page-header">
        <div className="categories-page-header-left">
          <span className="eyebrow">ECommerce Admin</span>
          <h1 className="categories-title">Customers</h1>
        </div>
        <div className="categories-page-header-right">
          <div className="categories-toolbar">
            <label className="search-field">
              <span className="sr-only">Tìm khách hàng</span>
              <input
                type="search"
                placeholder="Tìm theo họ tên…"
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
              />
            </label>
            <label className="filter-field">
              <span>Sắp xếp</span>
              <select
                value={sortBy}
                onChange={(e) => {
                  setSortBy(e.target.value)
                  setPageNumber(1)
                }}
              >
                {CUSTOMER_SORT_OPTIONS.map((o) => (
                  <option key={o.value} value={o.value}>
                    {o.label}
                  </option>
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
                  <option key={n} value={n}>
                    {n}
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
            <table className="categories-table">
              <thead>
                <tr>
                  <th scope="col">Avatar</th>
                  <th scope="col">Họ tên</th>
                  <th scope="col">Email</th>
                  <th scope="col">Địa chỉ</th>
                  <th scope="col">Tạo</th>
                  <th scope="col">Cập nhật</th>
                </tr>
              </thead>
              <tbody>
                {items.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="categories-empty">
                      Không có khách hàng.
                    </td>
                  </tr>
                ) : (
                  items.map((row) => (
                    <tr key={row.id}>
                      <td className="td-thumb">
                        <AvatarCell avatarUrl={row.avatarUrl} fullName={row.fullName} />
                      </td>
                      <td className="td-strong">{row.fullName}</td>
                      <td className="td-muted">{row.email?.trim() ? row.email : '—'}</td>
                      <td className="td-muted">{row.address?.trim() ? row.address : '—'}</td>
                      <td className="td-nowrap">{formatDate(row.createdDate)}</td>
                      <td className="td-nowrap">{formatDate(row.updatedDate)}</td>
                    </tr>
                  ))
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
              Trang {data.pageNumber} / {Math.max(1, totalPages)} · {data.totalCount} khách hàng
            </span>
            <button type="button" className="pager-btn" disabled={!data.hasNextPage} onClick={() => setPageNumber((p) => p + 1)}>
              Sau
            </button>
          </footer>
        )}
      </div>
    </div>
  )
}

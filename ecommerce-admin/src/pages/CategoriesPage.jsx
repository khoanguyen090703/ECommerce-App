import { useEffect, useRef, useState } from 'react'
import { apiClient, readApiErrorMessage, resolveImageUrl } from '../lib/api'


function formatDate(iso) {
  if (iso == null || iso === '') return '—'
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return '—'
  return d.toLocaleString('vi-VN', {
    dateStyle: 'short',
    timeStyle: 'short',
  })
}

async function fetchCategories({ pageNumber, pageSize, searchTerm, sortBy }) {
  const params = new URLSearchParams()
  params.set('pageNumber', String(pageNumber))
  params.set('pageSize', String(pageSize))
  if (searchTerm?.trim()) params.set('searchTerm', searchTerm.trim())
  if (sortBy) params.set('sortBy', sortBy)
  try {
    const res = await apiClient.get(`/api/categories?${params.toString()}`)
    return res.data
  } catch (error) {
    throw new Error(readApiErrorMessage(error))
  }
}

const MAX_CATEGORY_NAME_LEN = 200
const MAX_CATEGORY_DESC_LEN = 2000
const MAX_CATEGORY_IMAGE_URL_LEN = 2048
const MAX_CATEGORY_IMAGE_BYTES = 5 * 1024 * 1024
const ALLOWED_CATEGORY_IMAGE_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp', 'image/gif'])

function validateCategoryImageFile(file) {
  if (!ALLOWED_CATEGORY_IMAGE_TYPES.has(file.type)) {
    return 'Chỉ chấp nhận ảnh JPEG, PNG, WebP hoặc GIF.'
  }
  if (file.size > MAX_CATEGORY_IMAGE_BYTES) {
    return 'Ảnh tối đa 5 MB.'
  }
  return null
}

/** Kiểm tra URL ảnh hợp lệ (http/https), khớp validator phía API. */
function isValidHttpImageUrl(s) {
  if (s == null || typeof s !== 'string') return false
  const t = s.trim()
  if (!t || t.length > MAX_CATEGORY_IMAGE_URL_LEN) return false
  try {
    const u = new URL(t)
    return u.protocol === 'http:' || u.protocol === 'https:'
  } catch {
    return false
  }
}

async function fetchCategoryById(id) {
  try {
    const res = await apiClient.get(`/api/categories/${id}`)
    return res.data
  } catch (error) {
    throw new Error(readApiErrorMessage(error))
  }
}

async function uploadCategoryImage(file) {
  const fd = new FormData()
  fd.append('file', file)
  let json
  try {
    const res = await apiClient.post('/api/images/upload', fd, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    json = res.data
  } catch (error) {
    throw new Error(readApiErrorMessage(error))
  }
  const url = json.secureUrl ?? json.SecureUrl
  if (!url || typeof url !== 'string') {
    throw new Error('Máy chủ không trả về đường dẫn ảnh.')
  }
  return url.trim()
}

function IconEllipsisVertical() {
  return (
    <svg className="action-menu-trigger-icon" viewBox="0 0 24 24" aria-hidden="true">
      <path d="M12 8a2 2 0 1 0 0-4 2 2 0 0 0 0 4Zm0 6a2 2 0 1 0 0-4 2 2 0 0 0 0 4Zm0 6a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z" />
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
        <img src={src} alt={name || 'Category'} />
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

export function CategoriesPage() {
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [sortBy, setSortBy] = useState('id_desc')
  const [searchInput, setSearchInput] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')

  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  /** Menu hành động: popover fixed để không làm giãn layout bảng */
  const [actionMenu, setActionMenu] = useState(null)
  const actionMenuPopoverRef = useRef(null)
  const actionMenuTriggerElRef = useRef(null)

  const [deleteConfirmRow, setDeleteConfirmRow] = useState(null)
  const [deleteSubmitting, setDeleteSubmitting] = useState(false)

  const searchDebounceRef = useRef(0)

  const [editCategoryId, setEditCategoryId] = useState(null)
  const [editLoading, setEditLoading] = useState(false)
  const [editFetchError, setEditFetchError] = useState(null)
  const [editBaseline, setEditBaseline] = useState(null)
  const [editName, setEditName] = useState('')
  const [editDescription, setEditDescription] = useState('')
  const [editImageFile, setEditImageFile] = useState(null)
  const [editImagePreviewUrl, setEditImagePreviewUrl] = useState(null)
  const [editErrors, setEditErrors] = useState({})
  const [editCancelConfirmOpen, setEditCancelConfirmOpen] = useState(false)
  const [saving, setSaving] = useState(false)
  const [listVersion, setListVersion] = useState(0)
  const editFileInputRef = useRef(null)

  const [createOpen, setCreateOpen] = useState(false)
  const [createName, setCreateName] = useState('')
  const [createDescription, setCreateDescription] = useState('')
  const [createImageFile, setCreateImageFile] = useState(null)
  const [createImagePreviewUrl, setCreateImagePreviewUrl] = useState(null)
  const [createErrors, setCreateErrors] = useState({})
  const [createSubmitting, setCreateSubmitting] = useState(false)
  const createFileInputRef = useRef(null)

  const [toastMessage, setToastMessage] = useState(null)
  const toastClearRef = useRef(0)

  const [deleteErrorMessage, setDeleteErrorMessage] = useState(null)

  function handleSearchChange(value) {
    setSearchInput(value)
    window.clearTimeout(searchDebounceRef.current)
    searchDebounceRef.current = window.setTimeout(() => {
      setDebouncedSearch(value)
      setPageNumber(1)
    }, 350)
  }

  useEffect(() => {
    let cancelled = false

    void (async () => {
      setLoading(true)
      setError(null)
      try {
        const result = await fetchCategories({
          pageNumber,
          pageSize,
          searchTerm: debouncedSearch,
          sortBy,
        })
        if (cancelled) return
        setData(result)
      } catch (e) {
        if (cancelled) return
        setError(e instanceof Error ? e.message : 'Không tải được danh sách.')
        setData(null)
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()

    return () => {
      cancelled = true
    }
  }, [pageNumber, pageSize, debouncedSearch, sortBy, listVersion])

  useEffect(() => {
    return () => {
      window.clearTimeout(searchDebounceRef.current)
    }
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

  const createPreviewUrlRef = useRef(null)
  useEffect(() => {
    createPreviewUrlRef.current = createImagePreviewUrl
  }, [createImagePreviewUrl])
  const editPreviewUrlRef = useRef(null)
  useEffect(() => {
    editPreviewUrlRef.current = editImagePreviewUrl
  }, [editImagePreviewUrl])
  useEffect(() => {
    return () => {
      const u = createPreviewUrlRef.current
      if (u) URL.revokeObjectURL(u)
      const eu = editPreviewUrlRef.current
      if (eu) URL.revokeObjectURL(eu)
    }
  }, [])

  function resetCreateModalState() {
    setCreateImagePreviewUrl((prev) => {
      if (prev) URL.revokeObjectURL(prev)
      return null
    })
    setCreateName('')
    setCreateDescription('')
    setCreateImageFile(null)
    setCreateErrors({})
    if (createFileInputRef.current) createFileInputRef.current.value = ''
  }

  function openCreateModal() {
    setActionMenu(null)
    actionMenuTriggerElRef.current = null
    resetCreateModalState()
    setCreateOpen(true)
  }

  function closeCreateModal() {
    if (createSubmitting) return
    resetCreateModalState()
    setCreateOpen(false)
  }

  function handleCreateImageChange(e) {
    const file = e.target.files?.[0] ?? null
    setCreateImagePreviewUrl((prev) => {
      if (prev) URL.revokeObjectURL(prev)
      return null
    })
    setCreateImageFile(null)
    setCreateErrors((er) => {
      const next = { ...er }
      delete next.image
      delete next.form
      return next
    })

    if (!file) return

    const imgErr = validateCategoryImageFile(file)
    if (imgErr) {
      setCreateErrors((er) => ({ ...er, image: imgErr }))
      e.target.value = ''
      return
    }
    setCreateImageFile(file)
    setCreateImagePreviewUrl(URL.createObjectURL(file))
  }

  function clearCreateImage() {
    setCreateImagePreviewUrl((prev) => {
      if (prev) URL.revokeObjectURL(prev)
      return null
    })
    setCreateImageFile(null)
    setCreateErrors((er) => {
      const next = { ...er }
      delete next.image
      return next
    })
    if (createFileInputRef.current) createFileInputRef.current.value = ''
  }

  async function submitCreate(e) {
    e.preventDefault()
    const name = createName.trim()
    const description = createDescription.trim()
    const nextErrors = {}
    if (!name) nextErrors.name = 'Vui lòng nhập tên danh mục.'
    else if (name.length > MAX_CATEGORY_NAME_LEN) {
      nextErrors.name = `Tên tối đa ${MAX_CATEGORY_NAME_LEN} ký tự.`
    }
    if (!description) nextErrors.description = 'Vui lòng nhập mô tả.'
    else if (description.length > MAX_CATEGORY_DESC_LEN) {
      nextErrors.description = `Mô tả tối đa ${MAX_CATEGORY_DESC_LEN} ký tự.`
    }
    if (createImageFile) {
      const imgErr = validateCategoryImageFile(createImageFile)
      if (imgErr) nextErrors.image = imgErr
    }

    if (Object.keys(nextErrors).length > 0) {
      setCreateErrors(nextErrors)
      return
    }

    setCreateSubmitting(true)
    setCreateErrors({})
    try {
      let imageUrl = null
      if (createImageFile) {
        imageUrl = await uploadCategoryImage(createImageFile)
      }
      const body = { name, description }
      if (imageUrl) body.imageUrl = imageUrl

      await apiClient.post('/api/categories', body)
      resetCreateModalState()
      setCreateOpen(false)
      setToastMessage('Đã tạo danh mục thành công.')
      setPageNumber(1)
      setListVersion((v) => v + 1)
    } catch (err) {
      setCreateErrors((prev) => ({
        ...prev,
        form: err instanceof Error ? err.message : 'Không tạo được danh mục.',
      }))
    } finally {
      setCreateSubmitting(false)
    }
  }

  const handleSort = (columnKey) => {
    setSortBy((prev) => {
      if (prev === `${columnKey}_desc`) return columnKey
      return `${columnKey}_desc`
    })
    setPageNumber(1)
  }

  function resetEditModalState() {
    setEditImagePreviewUrl((prev) => {
      if (prev) URL.revokeObjectURL(prev)
      return null
    })
    setEditImageFile(null)
    setEditBaseline(null)
    setEditName('')
    setEditDescription('')
    setEditErrors({})
    setEditFetchError(null)
    if (editFileInputRef.current) editFileInputRef.current.value = ''
  }

  function closeEditModal() {
    if (saving) return
    resetEditModalState()
    setEditCategoryId(null)
    setEditCancelConfirmOpen(false)
  }

  function isEditDirty() {
    if (!editBaseline) return false
    if (editImageFile) return true
    if (editName.trim() !== (editBaseline.name ?? '').trim()) return true
    if (editDescription.trim() !== (editBaseline.description ?? '').trim()) return true
    return false
  }

  function requestEditCancel() {
    if (saving) return
    if (!isEditDirty()) {
      closeEditModal()
      return
    }
    setEditCancelConfirmOpen(true)
  }

  const openEdit = (row) => {
    setActionMenu(null)
    actionMenuTriggerElRef.current = null
    resetEditModalState()
    setEditCategoryId(row.id)
    setEditLoading(true)
    setEditFetchError(null)
    void (async () => {
      try {
        const detail = await fetchCategoryById(row.id)
        setEditName(detail.name ?? '')
        setEditDescription(detail.description ?? '')
        setEditBaseline({
          name: detail.name ?? '',
          description: detail.description ?? '',
          imageUrl: typeof detail.imageUrl === 'string' ? detail.imageUrl.trim() : '',
        })
      } catch (e) {
        setEditFetchError(e instanceof Error ? e.message : 'Không tải được danh mục.')
        setEditBaseline(null)
      } finally {
        setEditLoading(false)
      }
    })()
  }

  function handleEditImageChange(e) {
    const file = e.target.files?.[0] ?? null
    setEditImagePreviewUrl((prev) => {
      if (prev) URL.revokeObjectURL(prev)
      return null
    })
    setEditImageFile(null)
    setEditErrors((er) => {
      const next = { ...er }
      delete next.image
      delete next.form
      return next
    })
    if (!file) return
    const imgErr = validateCategoryImageFile(file)
    if (imgErr) {
      setEditErrors((er) => ({ ...er, image: imgErr }))
      e.target.value = ''
      return
    }
    setEditImageFile(file)
    setEditImagePreviewUrl(URL.createObjectURL(file))
  }

  function clearEditImage() {
    setEditImagePreviewUrl((prev) => {
      if (prev) URL.revokeObjectURL(prev)
      return null
    })
    setEditImageFile(null)
    setEditErrors((er) => {
      const next = { ...er }
      delete next.image
      return next
    })
    if (editFileInputRef.current) editFileInputRef.current.value = ''
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
    setDeleteConfirmRow(row)
  }

  const submitEdit = async (e) => {
    e.preventDefault()
    if (editCategoryId == null || !editBaseline) return
    const name = editName.trim()
    const description = editDescription.trim()
    const nextErrors = {}
    if (!name) nextErrors.name = 'Vui lòng nhập tên danh mục.'
    else if (name.length > MAX_CATEGORY_NAME_LEN) {
      nextErrors.name = `Tên tối đa ${MAX_CATEGORY_NAME_LEN} ký tự.`
    }
    if (!description) nextErrors.description = 'Vui lòng nhập mô tả.'
    else if (description.length > MAX_CATEGORY_DESC_LEN) {
      nextErrors.description = `Mô tả tối đa ${MAX_CATEGORY_DESC_LEN} ký tự.`
    }

    let imageUrlToSend = null
    if (editImageFile) {
      const imgErr = validateCategoryImageFile(editImageFile)
      if (imgErr) nextErrors.image = imgErr
    } else {
      const existing = editBaseline.imageUrl?.trim() ?? ''
      if (!existing) {
        nextErrors.image = 'Vui lòng chọn ảnh danh mục (API yêu cầu imageUrl).'
      } else if (!isValidHttpImageUrl(existing)) {
        nextErrors.image = 'URL ảnh hiện tại không hợp lệ. Vui lòng tải ảnh mới.'
      } else {
        imageUrlToSend = existing.trim()
      }
    }

    if (Object.keys(nextErrors).length > 0) {
      setEditErrors(nextErrors)
      return
    }

    setSaving(true)
    setEditErrors({})
    try {
      if (editImageFile) {
        imageUrlToSend = await uploadCategoryImage(editImageFile)
      }
      if (!imageUrlToSend?.trim()) {
        throw new Error('Thiếu đường dẫn ảnh.')
      }
      const trimmedUrl = imageUrlToSend.trim()
      if (!isValidHttpImageUrl(trimmedUrl)) {
        throw new Error('URL ảnh sau khi tải lên không hợp lệ.')
      }

      await apiClient.put(`/api/categories/${editCategoryId}`, {
        name,
        description,
        imageUrl: trimmedUrl,
      })
      closeEditModal()
      setToastMessage('Đã cập nhật danh mục thành công.')
      setListVersion((v) => v + 1)
    } catch (err) {
      setEditErrors((prev) => ({
        ...prev,
        form: err instanceof Error ? err.message : 'Cập nhật thất bại.',
      }))
    } finally {
      setSaving(false)
    }
  }

  const executeDelete = async () => {
    const row = deleteConfirmRow
    if (!row) return
    setDeleteSubmitting(true)
    try {
      await apiClient.delete(`/api/categories/${row.id}`)
      setDeleteConfirmRow(null)
      setToastMessage('Đã xóa danh mục thành công.')
      if (data?.items?.length === 1 && pageNumber > 1) {
        setPageNumber((p) => p - 1)
      } else {
        setListVersion((v) => v + 1)
      }
    } catch (error) {
      setDeleteConfirmRow(null)
      setDeleteErrorMessage(readApiErrorMessage(error))
    } finally {
      setDeleteSubmitting(false)
    }
  }

  const items = data?.items ?? []
  const totalPages = data?.totalPages ?? 0

  return (
    <div className="categories-page">
      {toastMessage && (
        <div className="toast" role="status" aria-live="polite">
          {toastMessage}
        </div>
      )}

      {deleteConfirmRow && (
        <div
          className="confirm-backdrop"
          role="presentation"
          onClick={() => !deleteSubmitting && setDeleteConfirmRow(null)}
        >
          <div
            className="confirm-dialog"
            role="alertdialog"
            aria-modal="true"
            aria-labelledby="delete-confirm-title"
            aria-describedby="delete-confirm-desc"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 id="delete-confirm-title" className="confirm-dialog-title">
              Xác nhận xóa
            </h2>
            <p id="delete-confirm-desc" className="confirm-dialog-body">
              Bạn có chắc muốn xóa danh mục <strong>{deleteConfirmRow.name}</strong>? Thao tác này không thể hoàn tác.
            </p>
            <div className="confirm-dialog-actions">
              <button type="button" className="btn-secondary" disabled={deleteSubmitting} onClick={() => setDeleteConfirmRow(null)}>
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
        <div
          className="delete-error-backdrop"
          role="presentation"
          onClick={() => setDeleteErrorMessage(null)}
        >
          <div
            className="delete-error-dialog"
            role="alertdialog"
            aria-modal="true"
            aria-labelledby="delete-error-title"
            aria-describedby="delete-error-desc"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 id="delete-error-title" className="delete-error-title">
              Không thể xóa danh mục
            </h2>
            <p id="delete-error-desc" className="delete-error-body">
              {deleteErrorMessage}
            </p>
            <button type="button" className="delete-error-dismiss" onClick={() => setDeleteErrorMessage(null)}>
              Đã hiểu
            </button>
          </div>
        </div>
      )}

      <header className="categories-page-header">
        <div className="categories-page-header-left">
          <span className="eyebrow">Bảng quản trị</span>
          <h1 className="categories-title">Danh mục</h1>
        </div>
        <div className="categories-page-header-right">
          <button type="button" className="btn-primary categories-add-btn" onClick={openCreateModal}>
            Thêm danh mục
          </button>
          <div className="categories-toolbar">
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
                  <SortableTh label="Mã" columnKey="id" currentSort={sortBy} onSort={handleSort} />
                  <th scope="col">Ảnh</th>
                  <SortableTh label="Tên" columnKey="name" currentSort={sortBy} onSort={handleSort} />
                  <th scope="col">Mô tả</th>
                  <SortableTh label="Ngày tạo" columnKey="created" currentSort={sortBy} onSort={handleSort} />
                  <th scope="col">Cập nhật</th>
                  <th scope="col" className="th-actions">
                    Thao tác
                  </th>
                </tr>
              </thead>
              <tbody>
                {items.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="categories-empty">
                      Không có danh mục nào.
                    </td>
                  </tr>
                ) : (
                  items.map((row) => (
                    <tr key={row.id}>
                      <td className="td-numeric">{row.id}</td>
                      <td className="td-thumb">
                        <ThumbnailCell imageUrl={row.imageUrl} name={row.name} />
                      </td>
                      <td className="td-strong">{row.name}</td>
                      <td className="td-muted">{row.description?.trim() ? row.description : '—'}</td>
                      <td className="td-nowrap">{formatDate(row.createdDate)}</td>
                      <td className="td-nowrap">{formatDate(row.updatedDate)}</td>
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
            <button type="button" className="action-menu-item" role="menuitem" onClick={() => openEdit(actionMenu.row)}>
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

      {createOpen && (
        <div className="modal-backdrop modal-backdrop--blocking" role="presentation">
          <div
            className="modal modal--wide"
            role="dialog"
            aria-modal="true"
            aria-labelledby="create-cat-title"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 id="create-cat-title">Thêm danh mục</h2>
            <p className="modal-sub">Nhập thông tin theo yêu cầu API. Ảnh chỉ được tải lên khi bạn bấm Tạo.</p>
            <form onSubmit={(e) => void submitCreate(e)} className="modal-form">
              {createErrors.form && <p className="modal-form-error">{createErrors.form}</p>}
              <label className="modal-field">
                Tên <span className="modal-required">*</span>
                <input
                  value={createName}
                  onChange={(e) => {
                    setCreateName(e.target.value)
                    setCreateErrors((er) => {
                      const next = { ...er }
                      delete next.name
                      return next
                    })
                  }}
                  autoComplete="off"
                  maxLength={MAX_CATEGORY_NAME_LEN}
                  aria-invalid={createErrors.name ? 'true' : 'false'}
                />
                {createErrors.name && <span className="modal-field-error">{createErrors.name}</span>}
              </label>
              <label className="modal-field">
                Mô tả <span className="modal-required">*</span>
                <textarea
                  value={createDescription}
                  onChange={(e) => {
                    setCreateDescription(e.target.value)
                    setCreateErrors((er) => {
                      const next = { ...er }
                      delete next.description
                      return next
                    })
                  }}
                  rows={4}
                  maxLength={MAX_CATEGORY_DESC_LEN}
                  aria-invalid={createErrors.description ? 'true' : 'false'}
                />
                {createErrors.description && <span className="modal-field-error">{createErrors.description}</span>}
              </label>
              <div className="modal-field">
                <span className="modal-field-label">Ảnh danh mục</span>
                <p className="modal-hint">Tùy chọn. JPEG, PNG, WebP hoặc GIF, tối đa 5 MB. Xem trước cục bộ; upload khi tạo.</p>
                <input
                  ref={createFileInputRef}
                  type="file"
                  accept="image/jpeg,image/png,image/webp,image/gif"
                  className="modal-file-input"
                  onChange={handleCreateImageChange}
                  aria-invalid={createErrors.image ? 'true' : 'false'}
                />
                {createErrors.image && <span className="modal-field-error">{createErrors.image}</span>}
                {createImagePreviewUrl && (
                  <div className="create-category-preview-wrap">
                    <img className="create-category-preview-img" src={createImagePreviewUrl} alt="Xem trước ảnh danh mục" />
                    <button type="button" className="btn-secondary create-category-remove-img" onClick={clearCreateImage} disabled={createSubmitting}>
                      Gỡ ảnh
                    </button>
                  </div>
                )}
              </div>
              <div className="modal-actions">
                <button type="button" className="btn-secondary" disabled={createSubmitting} onClick={closeCreateModal}>
                  Hủy
                </button>
                <button type="submit" className="btn-primary" disabled={createSubmitting}>
                  {createSubmitting ? 'Đang xử lý…' : 'Tạo danh mục'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {editCancelConfirmOpen && (
        <div
          className="confirm-backdrop confirm-backdrop--over-modal"
          role="presentation"
          onClick={() => !saving && setEditCancelConfirmOpen(false)}
        >
          <div
            className="confirm-dialog"
            role="alertdialog"
            aria-modal="true"
            aria-labelledby="edit-cancel-confirm-title"
            aria-describedby="edit-cancel-confirm-desc"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 id="edit-cancel-confirm-title" className="confirm-dialog-title">
              Hủy chỉnh sửa?
            </h2>
            <p id="edit-cancel-confirm-desc" className="confirm-dialog-body">
              Thông tin bạn đã thay đổi sẽ không được lưu.
            </p>
            <div className="confirm-dialog-actions">
              <button type="button" className="btn-secondary" disabled={saving} onClick={() => setEditCancelConfirmOpen(false)}>
                Tiếp tục chỉnh sửa
              </button>
              <button type="button" className="btn-danger" disabled={saving} onClick={() => closeEditModal()}>
                Hủy thay đổi
              </button>
            </div>
          </div>
        </div>
      )}

      {editCategoryId != null && (
        <div className="modal-backdrop modal-backdrop--blocking" role="presentation">
          <div
            className="modal modal--wide"
            role="dialog"
            aria-modal="true"
            aria-labelledby="edit-cat-title"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 id="edit-cat-title">Cập nhật danh mục</h2>
            <p className="modal-sub">#{editCategoryId}</p>

            {editLoading && <p className="categories-loading">Đang tải chi tiết…</p>}
            {editFetchError && (
              <div className="modal-form">
                <p className="modal-form-error">{editFetchError}</p>
                <div className="modal-actions">
                  <button type="button" className="btn-primary" onClick={() => closeEditModal()}>
                    Đóng
                  </button>
                </div>
              </div>
            )}

            {!editLoading && !editFetchError && editBaseline && (
              <form onSubmit={(e) => void submitEdit(e)} className="modal-form">
                {editErrors.form && <p className="modal-form-error">{editErrors.form}</p>}
                <label className="modal-field">
                  Tên <span className="modal-required">*</span>
                  <input
                    value={editName}
                    onChange={(e) => {
                      setEditName(e.target.value)
                      setEditErrors((er) => {
                        const next = { ...er }
                        delete next.name
                        delete next.form
                        return next
                      })
                    }}
                    autoComplete="off"
                    maxLength={MAX_CATEGORY_NAME_LEN}
                    disabled={saving}
                    aria-invalid={editErrors.name ? 'true' : 'false'}
                  />
                  {editErrors.name && <span className="modal-field-error">{editErrors.name}</span>}
                </label>
                <label className="modal-field">
                  Mô tả <span className="modal-required">*</span>
                  <textarea
                    value={editDescription}
                    onChange={(e) => {
                      setEditDescription(e.target.value)
                      setEditErrors((er) => {
                        const next = { ...er }
                        delete next.description
                        delete next.form
                        return next
                      })
                    }}
                    rows={4}
                    maxLength={MAX_CATEGORY_DESC_LEN}
                    disabled={saving}
                    aria-invalid={editErrors.description ? 'true' : 'false'}
                  />
                  {editErrors.description && <span className="modal-field-error">{editErrors.description}</span>}
                </label>
                <div className="modal-field">
                  <span className="modal-field-label">
                    Ảnh danh mục <span className="modal-required">*</span>
                  </span>
                  <p className="modal-hint">
                    Xem trước bên dưới. Giữ ảnh hiện tại hoặc chọn ảnh mới — ảnh mới chỉ được tải lên khi bạn bấm Lưu.
                  </p>
                  <input
                    ref={editFileInputRef}
                    type="file"
                    accept="image/jpeg,image/png,image/webp,image/gif"
                    className="modal-file-input"
                    onChange={handleEditImageChange}
                    disabled={saving}
                    aria-invalid={editErrors.image ? 'true' : 'false'}
                  />
                  {editErrors.image && <span className="modal-field-error">{editErrors.image}</span>}
                  {(editImagePreviewUrl || editBaseline?.imageUrl) && (
                    <div className="create-category-preview-wrap">
                      <img
                        className="create-category-preview-img"
                        src={editImagePreviewUrl ?? resolveImageUrl(editBaseline.imageUrl) ?? undefined}
                        alt="Xem trước ảnh danh mục"
                      />
                      {editImageFile && (
                        <button type="button" className="btn-secondary create-category-remove-img" onClick={clearEditImage} disabled={saving}>
                          Dùng lại ảnh hiện tại
                        </button>
                      )}
                    </div>
                  )}
                  {!editBaseline.imageUrl && !editImagePreviewUrl && (
                    <p className="modal-hint">Danh mục chưa có ảnh — vui lòng chọn file ảnh.</p>
                  )}
                </div>
                <div className="modal-actions">
                  <button type="button" className="btn-secondary" disabled={saving} onClick={requestEditCancel}>
                    Hủy
                  </button>
                  <button type="submit" className="btn-primary" disabled={saving}>
                    {saving ? 'Đang lưu…' : 'Lưu'}
                  </button>
                </div>
              </form>
            )}
          </div>
        </div>
      )}
    </div>
  )
}

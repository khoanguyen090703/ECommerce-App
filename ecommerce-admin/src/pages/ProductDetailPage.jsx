import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { apiClient, readApiErrorMessage, resolveImageUrl } from '../lib/api'

const VARIANT_FORMATS = [
  { value: 'FullBottle', label: 'Full bottle' },
  { value: 'Mini', label: 'Mini' },
  { value: 'Decant', label: 'Decant' },
]

const VARIANT_SORT_OPTIONS = [
  { value: 'id', label: 'Id ↑' },
  { value: 'id_desc', label: 'Id ↓' },
  { value: 'name', label: 'Tên A–Z' },
  { value: 'name_desc', label: 'Tên Z–A' },
  { value: 'price', label: 'Giá ↑' },
  { value: 'price_desc', label: 'Giá ↓' },
  { value: 'stock', label: 'Tồn ↑' },
  { value: 'stock_desc', label: 'Tồn ↓' },
  { value: 'created_desc', label: 'Tạo mới nhất' },
  { value: 'status', label: 'Trạng thái' },
]

const MAX_IMAGE_BYTES = 5 * 1024 * 1024
const ALLOWED_IMAGE_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp', 'image/gif'])

function validateImageFile(file) {
  if (!ALLOWED_IMAGE_TYPES.has(file.type)) {
    return 'Chỉ chấp nhận ảnh JPEG, PNG, WebP hoặc GIF.'
  }
  if (file.size > MAX_IMAGE_BYTES) {
    return 'Ảnh tối đa 5 MB.'
  }
  return null
}

async function uploadImage(file) {
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
  if (!url || typeof url !== 'string') throw new Error('Máy chủ không trả về đường dẫn ảnh.')
  return url.trim()
}

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

/** Full-screen gallery: large preview + thumbnail strip */
function VariantImageGalleryModal({ urls, initialIndex, onClose, title }) {
  const safeUrls = (urls ?? []).map((u) => resolveImageUrl(u) ?? u).filter(Boolean)
  const [idx, setIdx] = useState(Math.min(initialIndex ?? 0, Math.max(0, safeUrls.length - 1)))

  useEffect(() => {
    setIdx(Math.min(initialIndex ?? 0, Math.max(0, safeUrls.length - 1)))
  }, [initialIndex, safeUrls.length])

  useEffect(() => {
    function onKey(e) {
      if (e.key === 'Escape') onClose()
      if (e.key === 'ArrowLeft') setIdx((i) => Math.max(0, i - 1))
      if (e.key === 'ArrowRight') setIdx((i) => Math.min(safeUrls.length - 1, i + 1))
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose, safeUrls.length])

  if (safeUrls.length === 0) return null

  return (
    <div className="variant-gallery-backdrop" role="presentation" onClick={onClose}>
      <div
        className="variant-gallery-dialog"
        role="dialog"
        aria-modal="true"
        aria-label={title || 'Ảnh biến thể'}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="variant-gallery-header">
          <h2 className="variant-gallery-title">{title || 'Ảnh'}</h2>
          <button type="button" className="variant-gallery-close" onClick={onClose} aria-label="Đóng">
            ×
          </button>
        </div>
        <div className="variant-gallery-main">
          <img src={safeUrls[idx]} alt="" className="variant-gallery-big" />
        </div>
        <div className="variant-gallery-thumbs" role="tablist" aria-label="Chọn ảnh">
          {safeUrls.map((u, i) => (
            <button
              key={`${u}-${i}`}
              type="button"
              role="tab"
              aria-selected={i === idx}
              className={`variant-gallery-thumb${i === idx ? ' variant-gallery-thumb--active' : ''}`}
              onClick={() => setIdx(i)}
            >
              <img src={u} alt="" />
            </button>
          ))}
        </div>
        <p className="variant-gallery-hint">Phím ← → để chuyển ảnh · Esc để đóng</p>
      </div>
    </div>
  )
}

function formatDate(iso) {
  if (iso == null || iso === '') return '—'
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return '—'
  return d.toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' })
}

export function ProductDetailPage() {
  const { productId } = useParams()
  const id = Number(productId)

  const [product, setProduct] = useState(null)
  const [productLoading, setProductLoading] = useState(true)
  const [productError, setProductError] = useState(null)

  const [vPage, setVPage] = useState(1)
  const [vPageSize, setVPageSize] = useState(10)
  const [vSort, setVSort] = useState('id')
  const [vSearch, setVSearch] = useState('')
  const [vDebounced, setVDebounced] = useState('')
  const vSearchTimer = useRef(0)
  const [variantsData, setVariantsData] = useState(null)
  const [variantsLoading, setVariantsLoading] = useState(true)
  const [variantsError, setVariantsError] = useState(null)
  const [listTick, setListTick] = useState(0)

  const [gallery, setGallery] = useState(null)

  const [addOpen, setAddOpen] = useState(false)
  const [addForm, setAddForm] = useState({
    format: 'FullBottle',
    volumn: '100',
    price: '',
    stockQuantity: '1',
    imageFiles: [],
    previewUrls: [],
  })
  const [addErrors, setAddErrors] = useState({})
  const [addSubmitting, setAddSubmitting] = useState(false)
  const addImageInputRef = useRef(null)

  const [editVariant, setEditVariant] = useState(null)
  const [editForm, setEditForm] = useState(null)
  const [editErrors, setEditErrors] = useState({})
  const [editSubmitting, setEditSubmitting] = useState(false)
  const editImageInputRef = useRef(null)

  const [deleteVariant, setDeleteVariant] = useState(null)
  const [deleteSubmitting, setDeleteSubmitting] = useState(false)
  const [deleteErr, setDeleteErr] = useState(null)

  const [toast, setToast] = useState(null)
  const toastT = useRef(0)

  useEffect(() => {
    if (!toast) return undefined
    window.clearTimeout(toastT.current)
    toastT.current = window.setTimeout(() => setToast(null), 4000)
    return () => window.clearTimeout(toastT.current)
  }, [toast])

  useEffect(() => {
    window.clearTimeout(vSearchTimer.current)
    vSearchTimer.current = window.setTimeout(() => {
      setVDebounced(vSearch)
      setVPage(1)
    }, 300)
    return () => window.clearTimeout(vSearchTimer.current)
  }, [vSearch])

  useEffect(() => {
    if (!Number.isFinite(id) || id < 1) {
      setProductError('Id sản phẩm không hợp lệ.')
      setProductLoading(false)
      return
    }
    let cancelled = false
    void (async () => {
      setProductLoading(true)
      setProductError(null)
      try {
        const res = await apiClient.get(`/api/products/${id}?includeVariants=false`)
        const data = res.data
        if (!cancelled) setProduct(data)
      } catch (e) {
        if (!cancelled) setProductError(e instanceof Error ? e.message : 'Không tải được sản phẩm.')
      } finally {
        if (!cancelled) setProductLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [id])

  const loadVariants = useCallback(async () => {
    if (!Number.isFinite(id) || id < 1) return
    setVariantsLoading(true)
    setVariantsError(null)
    try {
      const params = new URLSearchParams()
      params.set('pageNumber', String(vPage))
      params.set('pageSize', String(vPageSize))
      params.set('sortBy', vSort)
      if (vDebounced.trim()) params.set('searchTerm', vDebounced.trim())
      const res = await apiClient.get(`/api/products/${id}/variants?${params.toString()}`)
      setVariantsData(res.data)
    } catch (e) {
      setVariantsError(e instanceof Error ? e.message : 'Không tải biến thể.')
      setVariantsData(null)
    } finally {
      setVariantsLoading(false)
    }
  }, [id, vPage, vPageSize, vSort, vDebounced, listTick])

  useEffect(() => {
    void loadVariants()
  }, [loadVariants])

  const isDraft = product?.status === 'Draft'

  function openAddVariant() {
    setAddErrors({})
    setAddForm({
      format: 'FullBottle',
      volumn: '100',
      price: '',
      stockQuantity: '1',
      imageFiles: [],
      previewUrls: [],
    })
    setAddOpen(true)
  }

  function pickAddVariantImages(e) {
    const files = Array.from(e.target.files ?? [])
    e.target.value = ''
    if (files.length === 0) return
    setAddErrors((prev) => {
      const next = { ...prev }
      delete next.images
      delete next.form
      return next
    })
    setAddForm((f) => {
      const combined = [...f.imageFiles, ...files]
      if (combined.length > 4) {
        setAddErrors((prev) => ({ ...prev, images: 'Tối đa 4 ảnh.' }))
        return f
      }
      for (const file of files) {
        const err = validateImageFile(file)
        if (err) {
          setAddErrors((prev) => ({ ...prev, images: err }))
          return f
        }
      }
      return {
        ...f,
        imageFiles: combined,
        previewUrls: [...f.previewUrls, ...files.map((file) => URL.createObjectURL(file))],
      }
    })
  }

  function removeAddVariantImage(index) {
    setAddForm((f) => {
      if (f.previewUrls[index]) URL.revokeObjectURL(f.previewUrls[index])
      return {
        ...f,
        imageFiles: f.imageFiles.filter((_, i) => i !== index),
        previewUrls: f.previewUrls.filter((_, i) => i !== index),
      }
    })
  }

  async function submitAddVariant(e) {
    e.preventDefault()
    const vol = Number(addForm.volumn)
    const price = Number(addForm.price)
    const stock = Number(addForm.stockQuantity)
    const err = {}
    if (Number.isNaN(vol) || vol < 1 || vol > 200) err.volumn = 'Dung tích phải từ 1 đến 200.'
    if (Number.isNaN(price) || price < 0) err.price = 'Giá không hợp lệ.'
    if (Number.isNaN(stock) || stock < 0) err.stock = 'Tồn không hợp lệ.'
    if (addForm.imageFiles.length === 0) err.images = 'Ít nhất một ảnh.'
    if (addForm.imageFiles.length > 4) err.images = 'Tối đa 4 ảnh.'
    if (Object.keys(err).length) {
      setAddErrors(err)
      return
    }
    setAddSubmitting(true)
    setAddErrors({})
    try {
      const urls = await Promise.all(addForm.imageFiles.map((file) => uploadImage(file)))
      await apiClient.post(`/api/variants/product/${id}`, {
        format: addForm.format,
        volumn: vol,
        price,
        stockQuantity: stock,
        images: urls,
      })
      addForm.previewUrls.forEach((u) => URL.revokeObjectURL(u))
      setAddOpen(false)
      setToast('Đã thêm biến thể.')
      setListTick((t) => t + 1)
    } catch (ex) {
      setAddErrors({ form: ex instanceof Error ? ex.message : 'Thêm thất bại.' })
    } finally {
      setAddSubmitting(false)
    }
  }

  function openEdit(v) {
    setEditErrors({})
    setEditVariant(v)
    setEditForm({
      format: v.format,
      volumn: String(v.volumn),
      unit: v.unit ?? 'ml',
      price: String(v.price),
      stockQuantity: String(v.stockQuantity),
      existingImageUrls: (v.imageUrls ?? []).filter(Boolean),
      newImageFiles: [],
      newPreviewUrls: [],
    })
  }

  function pickEditVariantImages(e) {
    const files = Array.from(e.target.files ?? [])
    e.target.value = ''
    if (files.length === 0) return
    setEditErrors((prev) => {
      const next = { ...prev }
      delete next.images
      delete next.form
      return next
    })
    setEditForm((f) => {
      if (!f) return f
      const combinedNew = [...f.newImageFiles, ...files]
      const total = f.existingImageUrls.length + combinedNew.length
      if (total > 4) {
        setEditErrors((prev) => ({ ...prev, images: 'Tối đa 4 ảnh.' }))
        return f
      }
      for (const file of files) {
        const err = validateImageFile(file)
        if (err) {
          setEditErrors((prev) => ({ ...prev, images: err }))
          return f
        }
      }
      return {
        ...f,
        newImageFiles: combinedNew,
        newPreviewUrls: [...f.newPreviewUrls, ...files.map((file) => URL.createObjectURL(file))],
      }
    })
  }

  function removeEditVariantExistingImage(index) {
    setEditForm((f) => {
      if (!f) return f
      return {
        ...f,
        existingImageUrls: f.existingImageUrls.filter((_, i) => i !== index),
      }
    })
  }

  function removeEditVariantNewImage(index) {
    setEditForm((f) => {
      if (!f) return f
      if (f.newPreviewUrls[index]) URL.revokeObjectURL(f.newPreviewUrls[index])
      return {
        ...f,
        newImageFiles: f.newImageFiles.filter((_, i) => i !== index),
        newPreviewUrls: f.newPreviewUrls.filter((_, i) => i !== index),
      }
    })
  }

  async function submitEditVariant(e) {
    e.preventDefault()
    if (!editVariant || !editForm) return
    const vol = Number(editForm.volumn)
    const price = Number(editForm.price)
    const stock = Number(editForm.stockQuantity)
    const err = {}
    if (Number.isNaN(vol) || vol < 1 || vol > 200) err.volumn = 'Dung tích phải từ 1 đến 200.'
    if (Number.isNaN(price) || price < 0) err.price = 'Giá không hợp lệ.'
    if (Number.isNaN(stock) || stock < 0) err.stock = 'Tồn không hợp lệ.'
    const totalImageCount = editForm.existingImageUrls.length + editForm.newImageFiles.length
    if (totalImageCount === 0) err.images = 'Ít nhất một ảnh.'
    if (totalImageCount > 4) err.images = 'Tối đa 4 ảnh.'
    if (Object.keys(err).length) {
      setEditErrors(err)
      return
    }
    setEditSubmitting(true)
    setEditErrors({})
    try {
      const uploadedUrls = await Promise.all(editForm.newImageFiles.map((file) => uploadImage(file)))
      const imageUrls = [...editForm.existingImageUrls, ...uploadedUrls].filter(Boolean)
      await apiClient.put(`/api/variants/${editVariant.id}`, {
        format: editForm.format,
        volumn: vol,
        unit: editForm.unit?.trim() || 'ml',
        price,
        stockQuantity: stock,
        imageUrls,
      })
      editForm.newPreviewUrls.forEach((u) => URL.revokeObjectURL(u))
      setEditVariant(null)
      setEditForm(null)
      setToast('Đã cập nhật biến thể.')
      setListTick((t) => t + 1)
    } catch (ex) {
      setEditErrors({ form: ex instanceof Error ? ex.message : 'Cập nhật thất bại.' })
    } finally {
      setEditSubmitting(false)
    }
  }

  async function executeDeleteVariant() {
    if (!deleteVariant) return
    setDeleteSubmitting(true)
    setDeleteErr(null)
    try {
      await apiClient.delete(`/api/variants/${deleteVariant.id}`)
      setDeleteVariant(null)
      setToast('Đã xóa biến thể.')
      setListTick((t) => t + 1)
    } catch (error) {
      setDeleteErr(readApiErrorMessage(error))
    } finally {
      setDeleteSubmitting(false)
    }
  }

  const vItems = variantsData?.items ?? []
  const vTotalPages = variantsData?.totalPages ?? 0

  if (!Number.isFinite(id) || id < 1) {
    return (
      <div className="product-detail-page categories-page">
        <p className="categories-error">Id không hợp lệ.</p>
        <Link to="/products" className="link-back">
          ← Danh sách sản phẩm
        </Link>
      </div>
    )
  }

  return (
    <div className="product-detail-page categories-page">
      {toast && (
        <div className="toast" role="status">
          {toast}
        </div>
      )}

      {gallery && (
        <VariantImageGalleryModal urls={gallery.urls} initialIndex={gallery.index} title={gallery.title} onClose={() => setGallery(null)} />
      )}

      {addOpen && (
        <div className="modal-backdrop modal-backdrop--blocking" role="presentation">
          <div className="modal modal--wide" role="dialog" onClick={(e) => e.stopPropagation()}>
            <h2>Thêm biến thể</h2>
            <form onSubmit={(e) => void submitAddVariant(e)} className="modal-form">
              {addErrors.form && <p className="modal-form-error">{addErrors.form}</p>}
              <label className="modal-field">
                Định dạng
                <select value={addForm.format} onChange={(e) => setAddForm((f) => ({ ...f, format: e.target.value }))}>
                  {VARIANT_FORMATS.map((o) => (
                    <option key={o.value} value={o.value}>
                      {o.label}
                    </option>
                  ))}
                </select>
              </label>
              <label className="modal-field">
                Dung tích (số)
                <input value={addForm.volumn} onChange={(e) => setAddForm((f) => ({ ...f, volumn: e.target.value }))} inputMode="numeric" />
                {addErrors.volumn && <span className="modal-field-error">{addErrors.volumn}</span>}
              </label>
              <label className="modal-field">
                Giá
                <input value={addForm.price} onChange={(e) => setAddForm((f) => ({ ...f, price: e.target.value }))} inputMode="decimal" />
                {addErrors.price && <span className="modal-field-error">{addErrors.price}</span>}
              </label>
              <label className="modal-field">
                Tồn kho
                <input
                  value={addForm.stockQuantity}
                  onChange={(e) => setAddForm((f) => ({ ...f, stockQuantity: e.target.value }))}
                  inputMode="numeric"
                />
                {addErrors.stock && <span className="modal-field-error">{addErrors.stock}</span>}
              </label>
              <label className="modal-field">
                Ảnh (ít nhất 1, tối đa 4)
                {addErrors.images && <span className="modal-field-error">{addErrors.images}</span>}
                <div className="product-edit-images-strip">
                  {addForm.previewUrls.map((url, idx) => (
                    <div key={`add-variant-${idx}`} className="product-edit-image-tile">
                      <button type="button" className="product-edit-image-remove" onClick={() => removeAddVariantImage(idx)} aria-label="Xóa ảnh">
                        ×
                      </button>
                      <div className="product-edit-image-thumb">
                        <img src={url} alt="" />
                      </div>
                    </div>
                  ))}
                  <input
                    ref={addImageInputRef}
                    type="file"
                    accept="image/jpeg,image/png,image/webp,image/gif"
                    multiple
                    hidden
                    onChange={pickAddVariantImages}
                  />
                  {addForm.previewUrls.length < 4 && (
                    <button type="button" className="product-edit-image-add" onClick={() => addImageInputRef.current?.click()} aria-label="Thêm ảnh">
                      +
                    </button>
                  )}
                </div>
              </label>
              <div className="modal-actions">
                <button
                  type="button"
                  className="btn-secondary"
                  disabled={addSubmitting}
                  onClick={() => {
                    addForm.previewUrls.forEach((u) => URL.revokeObjectURL(u))
                    setAddOpen(false)
                  }}
                >
                  Hủy
                </button>
                <button type="submit" className="btn-primary" disabled={addSubmitting}>
                  {addSubmitting ? 'Đang lưu…' : 'Thêm'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {editVariant && editForm && (
        <div className="modal-backdrop modal-backdrop--blocking" role="presentation">
          <div className="modal modal--wide" role="dialog" onClick={(e) => e.stopPropagation()}>
            <h2>Sửa biến thể #{editVariant.id}</h2>
            <form onSubmit={(e) => void submitEditVariant(e)} className="modal-form">
              {editErrors.form && <p className="modal-form-error">{editErrors.form}</p>}
              <label className="modal-field">
                Định dạng
                <select value={editForm.format} onChange={(e) => setEditForm((f) => ({ ...f, format: e.target.value }))}>
                  {VARIANT_FORMATS.map((o) => (
                    <option key={o.value} value={o.value}>
                      {o.label}
                    </option>
                  ))}
                </select>
              </label>
              <label className="modal-field">
                Dung tích
                <input value={editForm.volumn} onChange={(e) => setEditForm((f) => ({ ...f, volumn: e.target.value }))} inputMode="numeric" />
                {editErrors.volumn && <span className="modal-field-error">{editErrors.volumn}</span>}
              </label>
              <label className="modal-field">
                Giá
                <input value={editForm.price} onChange={(e) => setEditForm((f) => ({ ...f, price: e.target.value }))} inputMode="decimal" />
                {editErrors.price && <span className="modal-field-error">{editErrors.price}</span>}
              </label>
              <label className="modal-field">
                Tồn kho
                <input
                  value={editForm.stockQuantity}
                  onChange={(e) => setEditForm((f) => ({ ...f, stockQuantity: e.target.value }))}
                  inputMode="numeric"
                />
                {editErrors.stock && <span className="modal-field-error">{editErrors.stock}</span>}
              </label>
              <label className="modal-field">
                Ảnh (ít nhất 1, tối đa 4)
                {editErrors.images && <span className="modal-field-error">{editErrors.images}</span>}
                <div className="product-edit-images-strip">
                  {editForm.existingImageUrls.map((url, idx) => (
                    <div key={`edit-existing-${idx}`} className="product-edit-image-tile">
                      <button
                        type="button"
                        className="product-edit-image-remove"
                        onClick={() => removeEditVariantExistingImage(idx)}
                        aria-label="Xóa ảnh"
                      >
                        ×
                      </button>
                      <div className="product-edit-image-thumb">
                        <img src={resolveImageUrl(url) ?? url} alt="" />
                      </div>
                    </div>
                  ))}
                  {editForm.newPreviewUrls.map((url, idx) => (
                    <div key={`edit-new-${idx}`} className="product-edit-image-tile">
                      <button
                        type="button"
                        className="product-edit-image-remove"
                        onClick={() => removeEditVariantNewImage(idx)}
                        aria-label="Xóa ảnh"
                      >
                        ×
                      </button>
                      <div className="product-edit-image-thumb">
                        <img src={url} alt="" />
                      </div>
                    </div>
                  ))}
                  <input
                    ref={editImageInputRef}
                    type="file"
                    accept="image/jpeg,image/png,image/webp,image/gif"
                    multiple
                    hidden
                    onChange={pickEditVariantImages}
                  />
                  {editForm.existingImageUrls.length + editForm.newPreviewUrls.length < 4 && (
                    <button type="button" className="product-edit-image-add" onClick={() => editImageInputRef.current?.click()} aria-label="Thêm ảnh">
                      +
                    </button>
                  )}
                </div>
              </label>
              <div className="modal-actions">
                <button
                  type="button"
                  className="btn-secondary"
                  disabled={editSubmitting}
                  onClick={() => {
                    editForm?.newPreviewUrls?.forEach((u) => URL.revokeObjectURL(u))
                    setEditVariant(null)
                    setEditForm(null)
                  }}
                >
                  Hủy
                </button>
                <button type="submit" className="btn-primary" disabled={editSubmitting}>
                  {editSubmitting ? 'Đang lưu…' : 'Lưu'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {deleteVariant && (
        <div className="confirm-backdrop confirm-backdrop--over-modal" role="presentation" onClick={() => !deleteSubmitting && setDeleteVariant(null)}>
          <div className="confirm-dialog" role="alertdialog" onClick={(e) => e.stopPropagation()}>
            <h2 className="confirm-dialog-title">Xóa biến thể?</h2>
            <p className="confirm-dialog-body">
              Chỉ khi sản phẩm ở trạng thái <strong>Draft</strong>. Xóa <strong>{deleteVariant.name}</strong>?
            </p>
            {deleteErr && <p className="modal-form-error">{deleteErr}</p>}
            <div className="confirm-dialog-actions">
              <button type="button" className="btn-secondary" disabled={deleteSubmitting} onClick={() => { setDeleteVariant(null); setDeleteErr(null) }}>
                Hủy
              </button>
              <button type="button" className="btn-danger" disabled={deleteSubmitting} onClick={() => void executeDeleteVariant()}>
                {deleteSubmitting ? 'Đang xóa…' : 'Xóa'}
              </button>
            </div>
          </div>
        </div>
      )}

      <div className="product-detail-toolbar">
        <Link to="/products" className="link-back">
          ← Danh sách sản phẩm
        </Link>
        {product && (
          <Link to={`/products/${id}/edit`} className="btn-primary product-detail-edit-link">
            Chỉnh sửa sản phẩm
          </Link>
        )}
      </div>

      {productLoading && <p className="categories-loading">Đang tải sản phẩm…</p>}
      {productError && <p className="categories-error">{productError}</p>}

      {product && (
        <section className="product-detail-card categories-table-card">
          <div className="product-detail-layout">
            <div className="product-detail-hero-col">
              {product.imageUrls?.length > 0 ? (
                <div className="product-detail-hero-frame">
                  <img
                    src={resolveImageUrl(product.imageUrls[0]) ?? product.imageUrls[0]}
                    alt=""
                    className="product-detail-hero-img"
                  />
                </div>
              ) : (
                <div className="product-detail-hero-placeholder">Chưa có ảnh sản phẩm</div>
              )}
            </div>
            <div className="product-detail-info-col">
              <header className="product-detail-card-header">
                <h1 className="product-detail-name">{product.name}</h1>
                <span className={`product-status-badge product-status-badge--${String(product.status).toLowerCase()}`}>
                  {product.status}
                </span>
              </header>
              <p className="product-detail-meta">
                #{product.id} · {product.brandName} · Nồng độ: {product.concentration}
                {product.line ? ` · Dòng: ${product.line}` : ''}
                {product.releaseYear != null ? ` · ${product.releaseYear}` : ''}
              </p>
              <p className="product-detail-desc">{product.description}</p>
              <div className="product-detail-grid">
                <div>
                  <strong>Danh mục</strong>
                  <p>{product.categories || '—'}</p>
                </div>
                <div>
                  <strong>Nhóm hương</strong>
                  <p>{product.scentFamilies || '—'}</p>
                </div>
                <div>
                  <strong>Đánh giá</strong>
                  <p>
                    {product.averageRating} ({product.totalReviews} lượt)
                  </p>
                </div>
                <div>
                  <strong>Ngày tạo / cập nhật</strong>
                  <p>
                    {formatDate(product.createdDate)} · {formatDate(product.updatedDate)}
                  </p>
                </div>
              </div>
            </div>
          </div>
        </section>
      )}

      {product && (
        <section className="product-variants-section">
          <div className="product-variants-header">
            <h2 className="product-variants-title">Biến thể</h2>
            <button type="button" className="btn-primary" onClick={openAddVariant}>
              + Thêm biến thể
            </button>
          </div>

          <div className="product-variants-toolbar">
            <label className="search-field">
              <span className="sr-only">Tìm biến thể</span>
              <input
                type="search"
                placeholder="Tìm theo tên biến thể…"
                value={vSearch}
                onChange={(e) => setVSearch(e.target.value)}
              />
            </label>
            <label className="filter-field">
              <span>Sắp xếp</span>
              <select value={vSort} onChange={(e) => { setVSort(e.target.value); setVPage(1) }}>
                {VARIANT_SORT_OPTIONS.map((o) => (
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

          {variantsError && <p className="categories-error">{variantsError}</p>}
          {variantsLoading && !variantsData ? (
            <p className="categories-loading">Đang tải biến thể…</p>
          ) : (
            <div className="variant-cards">
              {vItems.length === 0 ? (
                <p className="td-muted">Không có biến thể.</p>
              ) : (
                vItems.map((v) => {
                  const imgs = v.imageUrls ?? []
                  const first = imgs[0]
                  return (
                    <article key={v.id} className="variant-card">
                      <div className="variant-card-visual">
                        {first ? (
                          <img className="variant-card-hero" src={resolveImageUrl(first) ?? first} alt="" />
                        ) : (
                          <div className="variant-card-hero variant-card-hero--empty">Không ảnh</div>
                        )}
                        {imgs.length > 0 && (
                          <button
                            type="button"
                            className="btn-secondary variant-card-gallery-btn"
                            onClick={() => setGallery({ urls: imgs, index: 0, title: v.name })}
                          >
                            {imgs.length === 1 ? 'Xem ảnh' : `Xem tất cả ảnh (${imgs.length})`}
                          </button>
                        )}
                      </div>
                      <div className="variant-card-body">
                        <div className="variant-card-head">
                          <h3 className="variant-card-name">{v.name}</h3>
                          <button
                            type="button"
                            className="variant-card-edit-btn"
                            onClick={() => openEdit(v)}
                            aria-label={`Cập nhật biến thể ${v.id}`}
                            title="Cập nhật biến thể"
                          >
                            ✎
                          </button>
                        </div>
                        <dl className="variant-card-dl">
                          <div>
                            <dt>Id</dt>
                            <dd>{v.id}</dd>
                          </div>
                          <div>
                            <dt>Định dạng</dt>
                            <dd>{v.format}</dd>
                          </div>
                          <div>
                            <dt>Dung tích</dt>
                            <dd>
                              {v.volumn} {v.unit}
                            </dd>
                          </div>
                          <div>
                            <dt>Giá</dt>
                            <dd>{Number(v.price).toLocaleString('vi-VN')} ₫</dd>
                          </div>
                          <div>
                            <dt>Tồn kho</dt>
                            <dd>{v.stockQuantity}</dd>
                          </div>
                          <div>
                            <dt>Đã bán</dt>
                            <dd>{v.soldQuantity}</dd>
                          </div>
                          <div>
                            <dt>Trạng thái</dt>
                            <dd>{v.status}</dd>
                          </div>
                          <div>
                            <dt>Mặc định / nổi bật</dt>
                            <dd>{v.isDefault ? 'Có' : 'Không'}</dd>
                          </div>
                          <div>
                            <dt>Tạo / cập nhật</dt>
                            <dd>
                              {formatDate(v.createdDate)} · {formatDate(v.updatedDate)}
                            </dd>
                          </div>
                        </dl>
                      </div>
                    </article>
                  )
                })
              )}
            </div>
          )}

          {variantsData && (
            <footer className="categories-pager">
              <button type="button" className="pager-btn" disabled={!variantsData.hasPreviousPage} onClick={() => setVPage((p) => Math.max(1, p - 1))}>
                Trước
              </button>
              <span className="pager-meta">
                Trang {variantsData.pageNumber} / {Math.max(1, vTotalPages)} · {variantsData.totalCount} biến thể
              </span>
              <button type="button" className="pager-btn" disabled={!variantsData.hasNextPage} onClick={() => setVPage((p) => p + 1)}>
                Sau
              </button>
            </footer>
          )}
        </section>
      )}
    </div>
  )
}

import { useEffect, useRef, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { apiClient, readApiErrorMessage, resolveImageUrl } from '../lib/api'

const CONCENTRATIONS = ['EDP', 'EDT', 'EDC', 'Parfum']

const MAX_PRODUCT_IMAGE_BYTES = 5 * 1024 * 1024
const ALLOWED_PRODUCT_IMAGE_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp', 'image/gif'])
/** Product update API allows exactly one image. */
const MAX_PRODUCT_IMAGES = 1

function previewComputedProductName(brands, brandId, line, concentration) {
  const b = brands.find((x) => String(x.id) === String(brandId))
  const brandName = (b?.name ?? '').trim()
  const linePart = (line ?? '').trim()
  const conc = (concentration ?? '').trim()
  return [brandName, linePart, conc].filter(Boolean).join(' ')
}

function validateProductImageFile(file) {
  if (!ALLOWED_PRODUCT_IMAGE_TYPES.has(file.type)) {
    return 'Chỉ chấp nhận ảnh JPEG, PNG, WebP hoặc GIF.'
  }
  if (file.size > MAX_PRODUCT_IMAGE_BYTES) {
    return 'Ảnh tối đa 5 MB.'
  }
  return null
}

async function uploadProductImage(file) {
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

function IconPlus() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" aria-hidden="true">
      <path fill="currentColor" d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2Z" />
    </svg>
  )
}

export function ProductEditPage() {
  const { productId } = useParams()
  const id = Number(productId)
  const navigate = useNavigate()

  const [brands, setBrands] = useState([])
  const [categories, setCategories] = useState([])
  const [scentFamilies, setScentFamilies] = useState([])

  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState(null)
  const [form, setForm] = useState(null)
  const [detail, setDetail] = useState(null)
  const [errors, setErrors] = useState({})
  const [saving, setSaving] = useState(false)

  const [existingImageUrls, setExistingImageUrls] = useState([])
  const [pendingNewImages, setPendingNewImages] = useState([])
  const [categoryPickerOpen, setCategoryPickerOpen] = useState(false)
  const [scentPickerOpen, setScentPickerOpen] = useState(false)

  const pendingImagesRef = useRef([])
  const newImagesInputRef = useRef(null)

  useEffect(() => {
    pendingImagesRef.current = pendingNewImages
  }, [pendingNewImages])

  useEffect(() => {
    return () => {
      pendingImagesRef.current.forEach((p) => URL.revokeObjectURL(p.previewUrl))
    }
  }, [])

  useEffect(() => {
    void (async () => {
      try {
        const [bRes, cRes, sRes] = await Promise.all([
          apiClient.get('/api/brands/all'),
          apiClient.get('/api/categories/all'),
          apiClient.get('/api/scentfamilies'),
        ])
        setBrands(bRes.data)
        setCategories(cRes.data)
        setScentFamilies(sRes.data)
      } catch {
        /* optional */
      }
    })()
  }, [])

  useEffect(() => {
    if (!Number.isFinite(id) || id < 1) return
    let cancelled = false
    void (async () => {
      setLoading(true)
      setLoadError(null)
      try {
        const res = await apiClient.get(`/api/products/${id}?includeVariants=false`)
        const d = res.data
        if (cancelled) return
        setDetail(d)
        setForm({
          description: d.description ?? '',
          brandId: String(d.brandId ?? ''),
          categoryIds: Array.isArray(d.categoryIds) ? d.categoryIds.map(String) : [],
          scentFamilyIds: Array.isArray(d.scentFamilyIds) ? d.scentFamilyIds.map(String) : [],
          line: d.line ?? '',
          releaseYear: d.releaseYear != null ? String(d.releaseYear) : '',
          concentration: d.concentration ?? 'EDP',
        })
        const urls = Array.isArray(d.imageUrls) ? d.imageUrls.map((u) => (typeof u === 'string' ? u.trim() : '')).filter(Boolean) : []
        setExistingImageUrls(urls)
        setPendingNewImages([])
        setCategoryPickerOpen(false)
        setScentPickerOpen(false)
      } catch (e) {
        if (!cancelled) setLoadError(e instanceof Error ? e.message : 'Không tải sản phẩm.')
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [id])

  function removeCategory(sid) {
    setForm((f) => (f ? { ...f, categoryIds: f.categoryIds.filter((x) => x !== sid) } : f))
  }

  function addCategoryFromSelect(e) {
    const v = e.target.value
    e.target.value = ''
    if (!v || !form) return
    if (form.categoryIds.includes(v)) return
    setForm((f) => (f ? { ...f, categoryIds: [...f.categoryIds, v] } : f))
    setCategoryPickerOpen(false)
  }

  function removeScent(sid) {
    setForm((f) => (f ? { ...f, scentFamilyIds: f.scentFamilyIds.filter((x) => x !== sid) } : f))
  }

  function addScentFromSelect(e) {
    const v = e.target.value
    e.target.value = ''
    if (!v || !form) return
    if (form.scentFamilyIds.includes(v)) return
    setForm((f) => (f ? { ...f, scentFamilyIds: [...f.scentFamilyIds, v] } : f))
    setScentPickerOpen(false)
  }

  function removeExistingImage(index) {
    setExistingImageUrls((list) => list.filter((_, i) => i !== index))
  }

  function removePendingImage(key) {
    setPendingNewImages((list) => {
      const found = list.find((p) => p.key === key)
      if (found) URL.revokeObjectURL(found.previewUrl)
      return list.filter((p) => p.key !== key)
    })
  }

  function onPickNewImages(e) {
    const file = e.target.files?.[0]
    e.target.value = ''
    if (!file) return
    const total = existingImageUrls.length + pendingNewImages.length
    if (total >= MAX_PRODUCT_IMAGES) {
      setErrors((er) => ({ ...er, images: 'Chỉ được phép một ảnh.' }))
      return
    }
    const fe = validateProductImageFile(file)
    if (fe) {
      setErrors((er) => ({ ...er, images: fe }))
      return
    }
    setErrors((er) => {
      const n = { ...er }
      delete n.images
      return n
    })
    setPendingNewImages((prev) => {
      prev.forEach((p) => URL.revokeObjectURL(p.previewUrl))
      return [{ key: `${Date.now()}-${file.name}`, file, previewUrl: URL.createObjectURL(file) }]
    })
  }

  async function submit(e) {
    e.preventDefault()
    if (!form) return
    const description = form.description.trim()
    const brandId = Number(form.brandId)
    const categoryIds = form.categoryIds.map((x) => Number(x)).filter((n) => n > 0)
    const scentFamilyIds = form.scentFamilyIds.map((x) => Number(x)).filter((n) => n > 0)
    const next = {}
    if (!description) next.description = 'Bắt buộc.'
    if (!brandId) next.brandId = 'Chọn thương hiệu.'
    if (categoryIds.length === 0) next.categories = 'Chọn ít nhất một danh mục.'
    if (scentFamilyIds.length === 0) next.scents = 'Chọn ít nhất một nhóm hương.'
    const imageCount = existingImageUrls.length + pendingNewImages.length
    if (imageCount !== 1) {
      next.images = imageCount === 0 ? 'Cần một ảnh.' : 'Chỉ được phép một ảnh.'
    }
    const line = form.line.trim()
    const releaseYear = form.releaseYear.trim() ? Number(form.releaseYear) : null
    if (form.releaseYear.trim() && Number.isNaN(releaseYear)) next.releaseYear = 'Năm không hợp lệ.'
    if (Object.keys(next).length) {
      setErrors(next)
      return
    }
    setSaving(true)
    setErrors({})
    try {
      let uploadedUrls = []
      if (pendingNewImages.length > 0) {
        uploadedUrls = await Promise.all(pendingNewImages.map((p) => uploadProductImage(p.file)))
      }
      const images = [...existingImageUrls.map((u) => u.trim()).filter(Boolean), ...uploadedUrls.map((u) => u.trim()).filter(Boolean)]
      if (images.length !== 1) {
        throw new Error('Cần đúng một ảnh hợp lệ để lưu.')
      }
      await apiClient.put(`/api/products/${id}`, {
        description,
        brandId,
        categoryIds,
        scentFamilyIds,
        line: line || null,
        releaseYear: releaseYear && !Number.isNaN(releaseYear) ? releaseYear : null,
        concentration: form.concentration,
        images,
      })
      navigate(`/products/${id}`)
    } catch (err) {
      setErrors({ form: err instanceof Error ? err.message : 'Cập nhật thất bại.' })
    } finally {
      setSaving(false)
    }
  }

  const derivedName =
    form != null ? previewComputedProductName(brands, form.brandId, form.line, form.concentration) : ''

  const availableCategories = form ? categories.filter((c) => !form.categoryIds.includes(String(c.id))) : []
  const availableScents = form ? scentFamilies.filter((s) => !form.scentFamilyIds.includes(String(s.id))) : []

  if (!Number.isFinite(id) || id < 1) {
    return (
      <div className="product-edit-page categories-page">
        <p className="categories-error">Id không hợp lệ.</p>
        <Link to="/products">← Danh sách</Link>
      </div>
    )
  }

  return (
    <div className="product-edit-page categories-page">
      <div className="product-detail-toolbar">
        <Link to={`/products/${id}`} className="link-back">
          ← Chi tiết sản phẩm
        </Link>
        <Link to="/products" className="link-back">
          Danh sách
        </Link>
      </div>

      <h1 className="categories-title">{detail?.name ?? (loading ? 'Đang tải…' : 'Sản phẩm')}</h1>

      {loading && <p className="categories-loading">Đang tải…</p>}
      {loadError && <p className="categories-error">{loadError}</p>}

      {!loading && form && (
        <div className="categories-table-card product-edit-card">
          <form onSubmit={(e) => void submit(e)} className="modal-form">
            {errors.form && <p className="modal-form-error">{errors.form}</p>}
            <label className="modal-field">
              Tên (xem trước)
              <input readOnly value={derivedName} aria-readonly="true" className="input-readonly" />
            </label>
            <label className="modal-field">
              Mô tả <span className="modal-required">*</span>
              <textarea value={form.description} onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))} rows={5} />
              {errors.description && <span className="modal-field-error">{errors.description}</span>}
            </label>

            <div className="modal-field">
              <span className="modal-field-label">
                Ảnh sản phẩm <span className="modal-required">*</span>
              </span>
              {errors.images && <span className="modal-field-error">{errors.images}</span>}
              <div className="product-edit-images-strip">
                {existingImageUrls.map((url, idx) => (
                  <div key={`ex-${idx}-${url}`} className="product-edit-image-tile">
                    <button
                      type="button"
                      className="product-edit-image-remove"
                      onClick={() => removeExistingImage(idx)}
                      aria-label="Xóa ảnh"
                    >
                      ×
                    </button>
                    <div className="product-edit-image-thumb">
                      <img src={resolveImageUrl(url) ?? url} alt="" />
                    </div>
                  </div>
                ))}
                {pendingNewImages.map((p) => (
                  <div key={p.key} className="product-edit-image-tile">
                    <button
                      type="button"
                      className="product-edit-image-remove"
                      onClick={() => removePendingImage(p.key)}
                      aria-label="Xóa ảnh"
                    >
                      ×
                    </button>
                    <div className="product-edit-image-thumb">
                      <img src={p.previewUrl} alt="" />
                    </div>
                  </div>
                ))}
                <input
                  ref={newImagesInputRef}
                  type="file"
                  accept="image/jpeg,image/png,image/webp,image/gif"
                  hidden
                  onChange={onPickNewImages}
                />
                {existingImageUrls.length + pendingNewImages.length === 0 && (
                  <button
                    type="button"
                    className="product-edit-image-add"
                    onClick={() => newImagesInputRef.current?.click()}
                    aria-label="Thêm ảnh"
                  >
                    <IconPlus />
                  </button>
                )}
              </div>
            </div>

            <label className="modal-field">
              Thương hiệu <span className="modal-required">*</span>
              <select value={form.brandId} onChange={(e) => setForm((f) => ({ ...f, brandId: e.target.value }))}>
                <option value="">—</option>
                {brands.map((b) => (
                  <option key={b.id} value={b.id}>
                    {b.name}
                  </option>
                ))}
              </select>
              {errors.brandId && <span className="modal-field-error">{errors.brandId}</span>}
            </label>

            <div className="modal-field">
              <span className="modal-field-label">
                Danh mục <span className="modal-required">*</span>
              </span>
              <ul className="product-edit-picked-list">
                {form.categoryIds.map((sid) => {
                  const c = categories.find((x) => String(x.id) === sid)
                  return (
                    <li key={sid} className="product-edit-picked-item">
                      <span>{c?.name ?? `#${sid}`}</span>
                      <button type="button" className="product-edit-remove-inline" onClick={() => removeCategory(sid)} aria-label="Xóa danh mục">
                        ×
                      </button>
                    </li>
                  )
                })}
              </ul>
              {availableCategories.length > 0 && (
                <div className="product-edit-add-row">
                  {!categoryPickerOpen ? (
                    <button type="button" className="product-edit-icon-add" onClick={() => setCategoryPickerOpen(true)} aria-label="Thêm danh mục">
                      <IconPlus />
                    </button>
                  ) : (
                    <>
                      <select className="product-edit-add-select" defaultValue="" onChange={addCategoryFromSelect} aria-label="Chọn danh mục thêm">
                        <option value="" disabled>
                          — Chọn danh mục —
                        </option>
                        {availableCategories.map((c) => (
                          <option key={c.id} value={String(c.id)}>
                            {c.name}
                          </option>
                        ))}
                      </select>
                      <button type="button" className="btn-secondary product-edit-picker-cancel" onClick={() => setCategoryPickerOpen(false)}>
                        Đóng
                      </button>
                    </>
                  )}
                </div>
              )}
              {errors.categories && <span className="modal-field-error">{errors.categories}</span>}
            </div>

            <div className="modal-field">
              <span className="modal-field-label">
                Nhóm hương <span className="modal-required">*</span>
              </span>
              <ul className="product-edit-picked-list">
                {form.scentFamilyIds.map((sid) => {
                  const s = scentFamilies.find((x) => String(x.id) === sid)
                  return (
                    <li key={sid} className="product-edit-picked-item">
                      <span>{s?.name ?? `#${sid}`}</span>
                      <button type="button" className="product-edit-remove-inline" onClick={() => removeScent(sid)} aria-label="Xóa nhóm hương">
                        ×
                      </button>
                    </li>
                  )
                })}
              </ul>
              {availableScents.length > 0 && (
                <div className="product-edit-add-row">
                  {!scentPickerOpen ? (
                    <button type="button" className="product-edit-icon-add" onClick={() => setScentPickerOpen(true)} aria-label="Thêm nhóm hương">
                      <IconPlus />
                    </button>
                  ) : (
                    <>
                      <select className="product-edit-add-select" defaultValue="" onChange={addScentFromSelect} aria-label="Chọn nhóm hương thêm">
                        <option value="" disabled>
                          — Chọn nhóm hương —
                        </option>
                        {availableScents.map((s) => (
                          <option key={s.id} value={String(s.id)}>
                            {s.name}
                          </option>
                        ))}
                      </select>
                      <button type="button" className="btn-secondary product-edit-picker-cancel" onClick={() => setScentPickerOpen(false)}>
                        Đóng
                      </button>
                    </>
                  )}
                </div>
              )}
              {errors.scents && <span className="modal-field-error">{errors.scents}</span>}
            </div>

            <label className="modal-field">
              Dòng
              <input value={form.line} onChange={(e) => setForm((f) => ({ ...f, line: e.target.value }))} />
            </label>
            <label className="modal-field">
              Năm phát hành
              <input value={form.releaseYear} onChange={(e) => setForm((f) => ({ ...f, releaseYear: e.target.value }))} inputMode="numeric" />
              {errors.releaseYear && <span className="modal-field-error">{errors.releaseYear}</span>}
            </label>
            <label className="modal-field">
              Nồng độ
              <select value={form.concentration} onChange={(e) => setForm((f) => ({ ...f, concentration: e.target.value }))}>
                {CONCENTRATIONS.map((c) => (
                  <option key={c} value={c}>
                    {c}
                  </option>
                ))}
              </select>
            </label>
            <div className="modal-actions">
              <Link to={`/products/${id}`} className="btn-secondary" style={{ textDecoration: 'none', display: 'inline-block' }}>
                Hủy
              </Link>
              <button type="submit" className="btn-primary" disabled={saving}>
                {saving ? 'Đang lưu…' : 'Lưu'}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  )
}

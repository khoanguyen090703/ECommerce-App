import { useEffect, useRef, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { apiClient, readApiErrorMessage } from '../lib/api'

const CONCENTRATIONS = ['EDP', 'EDT', 'EDC', 'Parfum']
const VARIANT_FORMATS = [
  { value: 'FullBottle', label: 'Full bottle' },
  { value: 'Mini', label: 'Mini' },
  { value: 'Decant', label: 'Decant' },
]

const MAX_PRODUCT_IMAGE_BYTES = 5 * 1024 * 1024
const ALLOWED_PRODUCT_IMAGE_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp', 'image/gif'])

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

function newVariantRow() {
  return {
    id: `v-${Date.now()}-${Math.random().toString(36).slice(2, 9)}`,
    format: 'FullBottle',
    volumn: '100',
    price: '',
    stockQuantity: '1',
    imageFiles: [],
    previewUrls: [],
  }
}

export function ProductCreatePage() {
  const navigate = useNavigate()
  const [step, setStep] = useState(1)
  const [brands, setBrands] = useState([])
  const [categories, setCategories] = useState([])
  const [scentFamilies, setScentFamilies] = useState([])

  const [form, setForm] = useState({
    description: '',
    brandId: '',
    categoryIds: [],
    scentFamilyIds: [],
    line: '',
    releaseYear: '',
    concentration: 'EDP',
  })
  const [pendingProductImage, setPendingProductImage] = useState(null)
  const [categoryPickerOpen, setCategoryPickerOpen] = useState(false)
  const [scentPickerOpen, setScentPickerOpen] = useState(false)
  const [variants, setVariants] = useState([])

  const [errors, setErrors] = useState({})
  const [submitting, setSubmitting] = useState(false)

  const productImageInputRef = useRef(null)
  const variantFileInputRef = useRef(null)
  const variantFileTargetIdRef = useRef(null)
  const pendingProductImageRef = useRef(null)
  const variantsRef = useRef([])

  useEffect(() => {
    pendingProductImageRef.current = pendingProductImage
  }, [pendingProductImage])

  useEffect(() => {
    variantsRef.current = variants
  }, [variants])

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
    return () => {
      const pi = pendingProductImageRef.current
      if (pi?.previewUrl) URL.revokeObjectURL(pi.previewUrl)
      variantsRef.current.forEach((v) => {
        v.previewUrls?.forEach((u) => URL.revokeObjectURL(u))
      })
    }
  }, [])

  function removeCategory(sid) {
    setForm((f) => ({ ...f, categoryIds: f.categoryIds.filter((x) => x !== sid) }))
  }

  function addCategoryFromSelect(e) {
    const v = e.target.value
    e.target.value = ''
    if (!v) return
    if (form.categoryIds.includes(v)) return
    setForm((f) => ({ ...f, categoryIds: [...f.categoryIds, v] }))
    setCategoryPickerOpen(false)
  }

  function removeScent(sid) {
    setForm((f) => ({ ...f, scentFamilyIds: f.scentFamilyIds.filter((x) => x !== sid) }))
  }

  function addScentFromSelect(e) {
    const v = e.target.value
    e.target.value = ''
    if (!v) return
    if (form.scentFamilyIds.includes(v)) return
    setForm((f) => ({ ...f, scentFamilyIds: [...f.scentFamilyIds, v] }))
    setScentPickerOpen(false)
  }

  function onPickProductImage(e) {
    const file = e.target.files?.[0]
    e.target.value = ''
    if (!file) return
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
    setPendingProductImage((prev) => {
      if (prev?.previewUrl) URL.revokeObjectURL(prev.previewUrl)
      return { key: `${Date.now()}`, file, previewUrl: URL.createObjectURL(file) }
    })
  }

  function removeProductImage() {
    setPendingProductImage((prev) => {
      if (prev?.previewUrl) URL.revokeObjectURL(prev.previewUrl)
      return null
    })
  }

  function addVariant() {
    setVariants((v) => [...v, newVariantRow()])
  }

  function removeVariant(id) {
    setVariants((list) => {
      const found = list.find((x) => x.id === id)
      if (found?.previewUrl) URL.revokeObjectURL(found.previewUrl)
      return list.filter((x) => x.id !== id)
    })
  }

  function updateVariant(id, patch) {
    setVariants((list) => list.map((x) => (x.id === id ? { ...x, ...patch } : x)))
  }

  function openVariantImagePicker(variantId) {
    variantFileTargetIdRef.current = variantId
    variantFileInputRef.current?.click()
  }

  function onPickVariantImage(e) {
    const files = Array.from(e.target.files ?? [])
    e.target.value = ''
    const targetId = variantFileTargetIdRef.current
    variantFileTargetIdRef.current = null
    if (files.length === 0 || !targetId) return
    for (const file of files) {
      const fe = validateProductImageFile(file)
      if (fe) {
        setErrors((er) => ({ ...er, variants: fe }))
        return
      }
    }
    setErrors((er) => {
      const n = { ...er }
      delete n.variants
      return n
    })
    setVariants((list) =>
      list.map((v) => {
        if (v.id !== targetId) return v
        const combinedFiles = [...v.imageFiles, ...files]
        if (combinedFiles.length > 4) {
          setErrors((er) => ({ ...er, variants: 'Mỗi biến thể tối đa 4 ảnh.' }))
          return v
        }
        return {
          ...v,
          imageFiles: combinedFiles,
          previewUrls: [...v.previewUrls, ...files.map((file) => URL.createObjectURL(file))],
        }
      }),
    )
  }

  function removeVariantImage(id, index) {
    setVariants((list) =>
      list.map((v) => {
        if (v.id !== id) return v
        const nextFiles = v.imageFiles.filter((_, i) => i !== index)
        const nextPreviews = v.previewUrls.filter((_, i) => i !== index)
        if (v.previewUrls[index]) URL.revokeObjectURL(v.previewUrls[index])
        return { ...v, imageFiles: nextFiles, previewUrls: nextPreviews }
      }),
    )
  }

  function validateStep1() {
    const description = form.description.trim()
    const brandId = Number(form.brandId)
    const categoryIds = form.categoryIds.map((x) => Number(x)).filter((n) => n > 0)
    const scentFamilyIds = form.scentFamilyIds.map((x) => Number(x)).filter((n) => n > 0)
    const next = {}
    if (!description) next.description = 'Bắt buộc.'
    if (!brandId) next.brandId = 'Chọn thương hiệu.'
    if (categoryIds.length === 0) next.categories = 'Chọn ít nhất một danh mục.'
    if (scentFamilyIds.length === 0) next.scents = 'Chọn ít nhất một nhóm hương.'
    if (!pendingProductImage?.file) {
      next.images = 'Cần một ảnh.'
    }
    const releaseYear = form.releaseYear.trim() ? Number(form.releaseYear) : null
    if (form.releaseYear.trim() && Number.isNaN(releaseYear)) next.releaseYear = 'Năm không hợp lệ.'
    return next
  }

  function validateStep2() {
    const next = {}
    if (variants.length === 0) {
      next.variants = 'Cần ít nhất một biến thể.'
      return next
    }
    const pairs = new Map()
    for (let i = 0; i < variants.length; i++) {
      const v = variants[i]
      const vol = Number(v.volumn)
      const price = Number(v.price)
      const stock = Number(v.stockQuantity)
      if (Number.isNaN(vol) || vol < 1 || vol > 200) next[`variant_${v.id}_volumn`] = 'Dung tích phải từ 1 đến 200.'
      if (Number.isNaN(price) || price < 0) next[`variant_${v.id}_price`] = 'Giá không hợp lệ.'
      if (Number.isNaN(stock) || stock < 0) next[`variant_${v.id}_stock`] = 'Tồn không hợp lệ.'
      if (v.imageFiles.length === 0) next[`variant_${v.id}_img`] = 'Cần ít nhất một ảnh biến thể.'
      if (v.imageFiles.length > 4) next[`variant_${v.id}_img`] = 'Mỗi biến thể tối đa 4 ảnh.'
      const key = `${v.format}:${vol}`
      if (pairs.has(key)) {
        next.variantsDup = 'Hai biến thể không được trùng định dạng và dung tích.'
      }
      pairs.set(key, true)
    }
    return next
  }

  function goNextFromStep1() {
    const next = validateStep1()
    if (Object.keys(next).length) {
      setErrors(next)
      return
    }
    setErrors({})
    setStep(2)
  }

  function goNextFromStep2() {
    const next = validateStep2()
    if (Object.keys(next).length) {
      setErrors(next)
      return
    }
    setErrors({})
    setStep(3)
  }

  async function submitAll() {
    const e1 = validateStep1()
    const e2 = validateStep2()
    const merged = { ...e1, ...e2 }
    if (Object.keys(merged).length) {
      setErrors(merged)
      if (Object.keys(e1).length) setStep(1)
      else if (Object.keys(e2).length) setStep(2)
      return
    }
    setSubmitting(true)
    setErrors({})
    try {
      const productUrl = await uploadProductImage(pendingProductImage.file)
      const variantUrls = await Promise.all(
        variants.map((v) => Promise.all(v.imageFiles.map((file) => uploadProductImage(file)))),
      )

      const description = form.description.trim()
      const brandId = Number(form.brandId)
      const categoryIds = form.categoryIds.map((x) => Number(x)).filter((n) => n > 0)
      const scentFamilyIds = form.scentFamilyIds.map((x) => Number(x)).filter((n) => n > 0)
      const line = form.line.trim()
      const releaseYear = form.releaseYear.trim() ? Number(form.releaseYear) : null

      const variantsPayload = variants.map((v, i) => ({
        format: v.format,
        volumn: Number(v.volumn),
        price: Number(v.price),
        stockQuantity: Number(v.stockQuantity),
        images: variantUrls[i],
      }))

      await apiClient.post('/api/products', {
        description,
        images: [productUrl],
        brandId,
        categoryIds,
        scentFamilyIds,
        line: line || null,
        releaseYear: releaseYear && !Number.isNaN(releaseYear) ? releaseYear : null,
        concentration: form.concentration,
        variants: variantsPayload,
      })
      navigate('/products', { state: { toastMessage: 'Đã tạo sản phẩm.', refreshList: true } })
    } catch (err) {
      setErrors({ form: err instanceof Error ? err.message : 'Tạo thất bại.' })
    } finally {
      setSubmitting(false)
    }
  }

  const derivedName = previewComputedProductName(brands, form.brandId, form.line, form.concentration)
  const availableCategories = categories.filter((c) => !form.categoryIds.includes(String(c.id)))
  const availableScents = scentFamilies.filter((s) => !form.scentFamilyIds.includes(String(s.id)))

  return (
    <div className="product-create-page categories-page">
      <div className="product-detail-toolbar">
        <Link to="/products" className="link-back">
          ← Danh sách sản phẩm
        </Link>
      </div>

      <h1 className="categories-title">Thêm sản phẩm</h1>

      <ul className="product-create-steps" aria-label="Tiến trình">
        <li className={step >= 1 ? 'product-create-steps__item--active' : ''}>1. Thông tin</li>
        <li className={step >= 2 ? 'product-create-steps__item--active' : ''}>2. Biến thể</li>
        <li className={step >= 3 ? 'product-create-steps__item--active' : ''}>3. Xem lại</li>
      </ul>

      <div className="categories-table-card product-edit-card">
        {errors.form && <p className="modal-form-error">{errors.form}</p>}

        {step === 1 && (
          <div className="modal-form">
            <label className="modal-field">
              Tên (xem trước)
              <input readOnly value={derivedName} className="input-readonly" aria-readonly="true" />
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
                {pendingProductImage && (
                  <div className="product-edit-image-tile">
                    <button type="button" className="product-edit-image-remove" onClick={removeProductImage} aria-label="Xóa ảnh">
                      ×
                    </button>
                    <div className="product-edit-image-thumb">
                      <img src={pendingProductImage.previewUrl} alt="" />
                    </div>
                  </div>
                )}
                <input ref={productImageInputRef} type="file" accept="image/jpeg,image/png,image/webp,image/gif" hidden onChange={onPickProductImage} />
                {!pendingProductImage && (
                  <button
                    type="button"
                    className="product-edit-image-add"
                    onClick={() => productImageInputRef.current?.click()}
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
              <Link to="/products" className="btn-secondary" style={{ textDecoration: 'none', display: 'inline-block' }}>
                Hủy
              </Link>
              <button type="button" className="btn-primary" onClick={goNextFromStep1}>
                Tiếp theo: biến thể
              </button>
            </div>
          </div>
        )}

        {step === 2 && (
          <div className="modal-form">
            {errors.variants && <span className="modal-field-error">{errors.variants}</span>}
            {errors.variantsDup && <span className="modal-field-error">{errors.variantsDup}</span>}
            <input
              ref={variantFileInputRef}
              type="file"
              accept="image/jpeg,image/png,image/webp,image/gif"
              multiple
              hidden
              onChange={onPickVariantImage}
            />

            <ul className="product-create-variant-list">
              {variants.map((v) => (
                <li key={v.id} className="product-variant-create-card">
                  <div className="product-variant-create-card__head">
                    <span className="product-variant-create-card__title">Biến thể</span>
                    <button type="button" className="btn-secondary" onClick={() => removeVariant(v.id)}>
                      Xóa
                    </button>
                  </div>
                  <div className="product-variant-create-fields">
                    <label className="modal-field">
                      Định dạng
                      <select value={v.format} onChange={(e) => updateVariant(v.id, { format: e.target.value })}>
                        {VARIANT_FORMATS.map((o) => (
                          <option key={o.value} value={o.value}>
                            {o.label}
                          </option>
                        ))}
                      </select>
                    </label>
                    <label className="modal-field">
                      Dung tích
                      <input value={v.volumn} onChange={(e) => updateVariant(v.id, { volumn: e.target.value })} inputMode="numeric" />
                      {errors[`variant_${v.id}_volumn`] && (
                        <span className="modal-field-error">{errors[`variant_${v.id}_volumn`]}</span>
                      )}
                    </label>
                    <label className="modal-field">
                      Giá
                      <input value={v.price} onChange={(e) => updateVariant(v.id, { price: e.target.value })} inputMode="decimal" />
                      {errors[`variant_${v.id}_price`] && <span className="modal-field-error">{errors[`variant_${v.id}_price`]}</span>}
                    </label>
                    <label className="modal-field">
                      Tồn kho
                      <input value={v.stockQuantity} onChange={(e) => updateVariant(v.id, { stockQuantity: e.target.value })} inputMode="numeric" />
                      {errors[`variant_${v.id}_stock`] && (
                        <span className="modal-field-error">{errors[`variant_${v.id}_stock`]}</span>
                      )}
                    </label>
                  </div>
                  <div className="modal-field">
                    <span className="modal-field-label">
                      Ảnh biến thể <span className="modal-required">*</span>
                    </span>
                    {errors[`variant_${v.id}_img`] && <span className="modal-field-error">{errors[`variant_${v.id}_img`]}</span>}
                    <div className="product-edit-images-strip">
                      {v.previewUrls.map((url, idx) => (
                        <div key={`${v.id}-${idx}`} className="product-edit-image-tile">
                          <button
                            type="button"
                            className="product-edit-image-remove"
                            onClick={() => removeVariantImage(v.id, idx)}
                            aria-label="Xóa ảnh"
                          >
                            ×
                          </button>
                          <div className="product-edit-image-thumb">
                            <img src={url} alt="" />
                          </div>
                        </div>
                      ))}
                      {v.previewUrls.length < 4 && (
                        <button
                          type="button"
                          className="product-edit-image-add"
                          onClick={() => openVariantImagePicker(v.id)}
                          aria-label="Thêm ảnh biến thể"
                        >
                          <IconPlus />
                        </button>
                      )}
                    </div>
                  </div>
                </li>
              ))}
            </ul>

            <button type="button" className="btn-secondary" onClick={addVariant}>
              + Thêm biến thể
            </button>

            <div className="modal-actions">
              <button type="button" className="btn-secondary" onClick={() => { setStep(1); setErrors({}) }}>
                Quay lại
              </button>
              <button type="button" className="btn-primary" onClick={goNextFromStep2}>
                Tiếp theo: xem lại
              </button>
            </div>
          </div>
        )}

        {step === 3 && (
          <div className="modal-form product-create-review">
            <h2 className="product-create-review__h">Sản phẩm</h2>
            <p>
              <strong>Tên (xem trước):</strong> {derivedName || '—'}
            </p>
            <p>
              <strong>Mô tả:</strong> {form.description.trim() || '—'}
            </p>
            <p>
              <strong>Thương hiệu:</strong> {brands.find((b) => String(b.id) === form.brandId)?.name ?? '—'}
            </p>
            <p>
              <strong>Danh mục:</strong>{' '}
              {form.categoryIds.map((sid) => categories.find((c) => String(c.id) === sid)?.name ?? sid).join(', ') || '—'}
            </p>
            <p>
              <strong>Nhóm hương:</strong>{' '}
              {form.scentFamilyIds.map((sid) => scentFamilies.find((s) => String(s.id) === sid)?.name ?? sid).join(', ') || '—'}
            </p>
            <p>
              <strong>Dòng:</strong> {form.line.trim() || '—'}
            </p>
            <p>
              <strong>Năm phát hành:</strong> {form.releaseYear.trim() || '—'}
            </p>
            <p>
              <strong>Nồng độ:</strong> {form.concentration}
            </p>
            {pendingProductImage && (
              <div className="product-create-review__img">
                <strong>Ảnh sản phẩm</strong>
                <div className="product-edit-image-thumb" style={{ marginTop: 8 }}>
                  <img src={pendingProductImage.previewUrl} alt="" />
                </div>
              </div>
            )}

            <h2 className="product-create-review__h">Biến thể ({variants.length})</h2>
            <ul className="product-create-review-variants">
              {variants.map((v, idx) => {
                const fmt = VARIANT_FORMATS.find((f) => f.value === v.format)?.label ?? v.format
                return (
                  <li key={v.id} className="product-create-review-variant">
                    <div className="product-create-review-variant__head">
                      <p className="product-create-review-variant__title">Biến thể #{idx + 1}</p>
                    </div>
                    <dl className="product-create-review-variant__meta">
                      <div>
                        <dt>Định dạng</dt>
                        <dd>{fmt}</dd>
                      </div>
                      <div>
                        <dt>Dung tích</dt>
                        <dd>{v.volumn} ml</dd>
                      </div>
                      <div>
                        <dt>Giá</dt>
                        <dd>{Number(v.price).toLocaleString('vi-VN')} ₫</dd>
                      </div>
                      <div>
                        <dt>Tồn kho</dt>
                        <dd>{v.stockQuantity}</dd>
                      </div>
                    </dl>
                    <div className="product-create-review-variant__images">
                      {v.previewUrls.map((url, imageIdx) => (
                        <div key={`${v.id}-review-${imageIdx}`} className="product-edit-image-thumb">
                          <img src={url} alt="" />
                        </div>
                      ))}
                    </div>
                  </li>
                )
              })}
            </ul>

            <div className="modal-actions">
              <button type="button" className="btn-secondary" onClick={() => { setStep(2); setErrors({}) }} disabled={submitting}>
                Quay lại
              </button>
              <button type="button" className="btn-primary" onClick={() => void submitAll()} disabled={submitting}>
                {submitting ? 'Đang tạo…' : 'Tạo sản phẩm'}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

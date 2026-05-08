import './LoginPage.css'
import { useState } from 'react'
import toast from 'react-hot-toast'
import { apiClient, readApiErrorMessage, setAuthTokens } from '../lib/api'
import { useNavigate } from 'react-router-dom'

const LoginPage = () => {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
  })
  const [loading, setLoading] = useState(false)
  const [rememberMe, setRememberMe] = useState(true)

  const navigate = useNavigate()

  const handleInputChange = (e) => {
    const { name, value } = e.target
    setFormData((prev) => ({ ...prev, [name]: value }))
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setLoading(true)

    try {
      const response = await apiClient.post('/api/auth/signin', {
        email: formData.email,
        password: formData.password,
      })

      const { token, refreshToken } = response.data
      setAuthTokens(token, refreshToken, rememberMe)

      toast.success('Đăng nhập thành công')
      navigate('/dashboard', { replace: true })
    } catch (err) {
      let msg
      if (err.response) {
        const status = err.response.status
        if (status === 401) {
          const serverMsg = readApiErrorMessage(err)
          msg = /username or password|not correct/i.test(serverMsg)
            ? 'Email hoặc mật khẩu không chính xác'
            : serverMsg
        } else if (status === 422) {
          msg = 'Dữ liệu không hợp lệ'
        } else {
          msg = readApiErrorMessage(err)
        }
      } else if (err.request) {
        msg = 'Không kết nối được máy chủ'
      } else {
        msg = err.message ? `Đã xảy ra lỗi: ${err.message}` : 'Đã xảy ra lỗi'
      }
      toast.error(msg)
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="login-layout">
      <section className="login-banner" aria-label="Banner Aura Mystique">
        <div className="login-banner-overlay" />
        <div className="login-banner-content">
          <p className="login-tagline">Admin Portal</p>
          <h1>Aura Mystique</h1>
          <p className="login-subtitle">
            Khám phá mùi hương mang dấu ấn riêng của bạn với vẻ đẹp thanh lịch và sang trọng.
          </p>
        </div>
      </section>

      <section className="login-form-side">
        <div className="login-form-card">
          <h2>Chào mừng trở lại</h2>

          <form onSubmit={handleSubmit}>
            <div className="login-form-group">
              <label htmlFor="email">Email</label>
              <input
                type="email"
                id="email"
                name="email"
                placeholder="auramystique@example.com"
                required
                value={formData.email}
                onChange={handleInputChange}
              />
            </div>

            <div className="login-form-group">
              <label htmlFor="password">Mật khẩu</label>
              <input
                type="password"
                id="password"
                name="password"
                placeholder="Nhập mật khẩu của bạn"
                value={formData.password}
                onChange={handleInputChange}
                required
              />
            </div>

            <div className="login-form-row">
              <label className="login-remember">
                <input
                  type="checkbox"
                  name="remember"
                  checked={rememberMe}
                  onChange={(e) => setRememberMe(e.target.checked)}
                />
                <span>Ghi nhớ đăng nhập</span>
              </label>
            </div>

            <button type="submit" className="login-btn" disabled={loading}>
              {loading ? 'Đang đăng nhập...' : 'Đăng nhập'}
            </button>
          </form>
        </div>
      </section>
    </main>
  )
}

export default LoginPage

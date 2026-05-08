import { useEffect, useMemo, useState } from 'react'
import { BrowserRouter, Navigate, NavLink, Route, Routes, useNavigate } from 'react-router-dom'
import adminLogo from './assets/admin-logo.png'
import { Toaster } from 'react-hot-toast'
import { apiClient, getStoredAccessToken, logoutFromServer, resolveImageUrl, setSessionInvalidHandler } from './lib/api'
import { CategoriesPage } from './pages/CategoriesPage'
import { CustomersPage } from './pages/CustomersPage'
import { ProductDetailPage } from './pages/ProductDetailPage'
import { ProductCreatePage } from './pages/ProductCreatePage'
import { ProductEditPage } from './pages/ProductEditPage'
import { ProductsPage } from './pages/ProductsPage'
import { OrderDetailPage } from './pages/OrderDetailPage'
import { OrdersPage } from './pages/OrdersPage'
import  LoginPage  from './pages/LoginPage'
import { OverviewPage } from './pages/OverviewPage'
import './App.css'

const navItems = [
  {
    label: 'Overview',
    to: '/dashboard',
    end: true,
    icon: (
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M4 13h6V4H4v9Zm0 7h6v-5H4v5Zm10 0h6v-9h-6v9Zm0-16v5h6V4h-6Z" />
      </svg>
    ),
  },
  {
    label: 'Customers',
    to: '/customers',
    icon: (
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M16 11c1.66 0 3-1.57 3-3.5S17.66 4 16 4s-3 1.57-3 3.5 1.34 3.5 3 3.5ZM8 11c1.66 0 3-1.57 3-3.5S9.66 4 8 4 5 5.57 5 7.5 6.34 11 8 11Zm0 2c-2.67 0-6 1.34-6 4v2h12v-2c0-2.66-3.33-4-6-4Zm8 0c-.31 0-.65.02-1 .07 1.16.84 2 1.97 2 3.43V19h5v-2c0-2.66-3.33-4-6-4Z" />
      </svg>
    ),
  },
  {
    label: 'Products',
    to: '/products',
    icon: (
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="m21 8.5-9-5-9 5v7l9 5 9-5v-7ZM12 5.78l5.37 2.98L12 11.74 6.63 8.76 12 5.78Zm-7 4.68 6 3.34v4.48l-6-3.34v-4.48Zm8 7.82V13.8l6-3.34v4.48l-6 3.34Z" />
      </svg>
    ),
  },
  {
    label: 'Categories',
    to: '/categories',
    icon: (
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M4 4h7v7H4V4Zm9 0h7v7h-7V4ZM4 13h7v7H4v-7Zm9 0h7v7h-7v-7Z" />
      </svg>
    ),
  },
  {
    label: 'Orders',
    to: '/orders',
    icon: (
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M7 4h10a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2Zm0 2v12h10V6H7Zm2 2h6v2H9V8Zm0 4h6v2H9v-2Z" />
      </svg>
    ),
  },
]

const pageTitles = {
  overview: 'Overview',
  customers: 'Customers',
  products: 'Products',
  categories: 'Categories',
  orders: 'Orders',
}

function Sidebar() {
  return (
    <aside className="sidebar" aria-label="Main navigation">
      <div className="brand">
        <img src={adminLogo} alt="ECommerce Admin" />
      </div>

      <div className="sidebar-divider" />

      <nav className="nav-menu">
        {navItems.map((item) => (
          <NavLink
            key={item.label}
            to={item.to}
            end={item.end}
            className={({ isActive }) => `nav-item${isActive ? ' active' : ''}`}
          >
            <span className="nav-icon">{item.icon}</span>
            <span>{item.label}</span>
          </NavLink>
        ))}
      </nav>
    </aside>
  )
}

function Header() {
  const [isProfileOpen, setIsProfileOpen] = useState(false)
  const [profile, setProfile] = useState({
    fullName: '',
    email: '',
    avatarUrl: '',
  })
  const navigate = useNavigate()

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const res = await apiClient.get('/api/users/me')
        if (cancelled) return
        const payload = res.data ?? {}
        setProfile({
          fullName: payload.fullName ?? payload.name ?? '',
          email: payload.email ?? '',
          avatarUrl: payload.avatarUrl ?? payload.profileImageUrl ?? '',
        })
      } catch {
        if (!cancelled) {
          setProfile({
            fullName: '',
            email: '',
            avatarUrl: '',
          })
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  const initials = useMemo(() => {
    const source = profile.fullName?.trim() || profile.email?.trim()
    if (!source) return 'U'
    return source
      .split(/\s+/)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase() ?? '')
      .join('')
  }, [profile.fullName, profile.email])

  const displayName = profile.fullName?.trim() || 'User'
  const displayEmail = profile.email?.trim() || '—'
  const avatarSrc = resolveImageUrl(profile.avatarUrl)

  async function logout() {
    await logoutFromServer()
    navigate('/login', { replace: true })
  }

  return (
    <header className="top-header">
      <button className="notification-button" type="button" aria-label="Notifications">
        <svg viewBox="0 0 24 24" aria-hidden="true">
          <path d="M12 22a2.5 2.5 0 0 0 2.45-2h-4.9A2.5 2.5 0 0 0 12 22Zm7-6v-5c0-3.07-1.63-5.64-4.5-6.32V4a2.5 2.5 0 0 0-5 0v.68C6.63 5.36 5 7.92 5 11v5l-2 2v1h18v-1l-2-2Z" />
        </svg>
      </button>

      <div className="profile-area">
        <button
          className="avatar-button"
          type="button"
          aria-label="Open profile menu"
          aria-expanded={isProfileOpen}
          onClick={() => setIsProfileOpen((current) => !current)}
        >
          {avatarSrc ? <img src={avatarSrc} alt={displayName} className="avatar-image" /> : initials}
        </button>

        <div className="profile-tooltip" role="tooltip">
          <strong>{displayName}</strong>
          <span>{displayEmail}</span>
        </div>

        {isProfileOpen && (
          <div className="profile-popover">
            <div className="popover-avatar">
              {avatarSrc ? <img src={avatarSrc} alt={displayName} className="avatar-image" /> : initials}
            </div>
            <div>
              <p className="hello-text">Hello {displayName}</p>
              <p className="email-text">{displayEmail}</p>
            </div>
            <button className="logout-button" type="button" onClick={logout}>
              Logout
            </button>
          </div>
        )}
      </div>
    </header>
  )
}

function AdminLayout() {
  return (
    <div className="admin-shell">
      <Sidebar />
      <div className="main-panel">
        <Header />
        <main className="content">
          <Routes>
            <Route path="/" element={<Navigate to="/dashboard" replace />} />
            <Route path="/dashboard" element={<OverviewPage />} />
            <Route path="/customers" element={<CustomersPage />} />
            <Route path="/products" element={<ProductsPage />} />
            <Route path="/products/new" element={<ProductCreatePage />} />
            <Route path="/products/:productId" element={<ProductDetailPage />} />
            <Route path="/products/:productId/edit" element={<ProductEditPage />} />
            <Route path="/categories" element={<CategoriesPage />} />
            <Route path="/orders" element={<OrdersPage />} />
            <Route path="/orders/:orderId" element={<OrderDetailPage />} />
          </Routes>
        </main>
      </div>
    </div>
  )
}

function RootRedirect() {
  return <Navigate to={getStoredAccessToken() ? '/dashboard' : '/login'} replace />
}

function LoginRoute() {
  if (getStoredAccessToken()) {
    return <Navigate to="/dashboard" replace />
  }
  return <LoginPage />
}

function ProtectedAdminLayout() {
  if (!getStoredAccessToken()) {
    return <Navigate to="/login" replace />
  }
  return <AdminLayout />
}

function AppRoutes() {
  const navigate = useNavigate()

  useEffect(() => {
    setSessionInvalidHandler(() => {
      navigate('/login', { replace: true })
    })
    return () => setSessionInvalidHandler(null)
  }, [navigate])

  return (
    <Routes>
      <Route path="/" element={<RootRedirect />} />
      <Route path="/login" element={<LoginRoute />} />
      <Route path="/*" element={<ProtectedAdminLayout />} />
    </Routes>
  )
}

function App() {
  return (
    <BrowserRouter>
      <Toaster
        position="top-center"
        toastOptions={{
          duration: 4000,
          style: {
            background: '#1c1b18',
            color: '#f4efe6',
            border: '1px solid rgba(201, 169, 98, 0.35)',
          },
          success: {
            iconTheme: { primary: '#c9a962', secondary: '#1c1b18' },
          },
          error: {
            iconTheme: { primary: '#c45c5c', secondary: '#1c1b18' },
          },
        }}
      />
      <AppRoutes />
    </BrowserRouter>
  )
}

export default App

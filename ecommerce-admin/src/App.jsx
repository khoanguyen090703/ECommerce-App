import { useState } from 'react'
import { BrowserRouter, NavLink, Route, Routes } from 'react-router-dom'
import adminLogo from './assets/admin-logo.png'
import { CategoriesPage } from './pages/CategoriesPage'
import './App.css'

const user = {
  fullName: 'Nguyen Minh Anh',
  email: 'minhanh.admin@example.com',
  initials: 'MA',
}

const navItems = [
  {
    label: 'Overview',
    to: '/',
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
]

const pageTitles = {
  overview: 'Overview',
  customers: 'Customers',
  products: 'Products',
  categories: 'Categories',
}

function DashboardPage({ title }) {
  return (
    <section className="dashboard-card">
      <span className="eyebrow">ECommerce Admin</span>
      <h1>{title}</h1>
      <p>
        Main content area for the {title.toLowerCase()} page. You can replace this placeholder
        with charts, tables, forms, or page-specific modules later.
      </p>
    </section>
  )
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
          {user.initials}
        </button>

        <div className="profile-tooltip" role="tooltip">
          <strong>{user.fullName}</strong>
          <span>{user.email}</span>
        </div>

        {isProfileOpen && (
          <div className="profile-popover">
            <div className="popover-avatar">{user.initials}</div>
            <div>
              <p className="hello-text">Hello {user.fullName}</p>
              <p className="email-text">{user.email}</p>
            </div>
            <button className="logout-button" type="button">
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
            <Route path="/" element={<DashboardPage title={pageTitles.overview} />} />
            <Route path="/customers" element={<DashboardPage title={pageTitles.customers} />} />
            <Route path="/products" element={<DashboardPage title={pageTitles.products} />} />
            <Route path="/categories" element={<CategoriesPage />} />
          </Routes>
        </main>
      </div>
    </div>
  )
}

function App() {
  return (
    <BrowserRouter>
      <AdminLayout />
    </BrowserRouter>
  )
}

export default App

import { NavLink, Outlet } from 'react-router-dom';

export function Layout() {
  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <nav
        role="navigation"
        aria-label="Main navigation"
        style={{
          backgroundColor: '#1a1a2e',
          color: '#fff',
          padding: '0.75rem 2rem',
          display: 'flex',
          alignItems: 'center',
          gap: '2rem',
        }}
      >
        <strong style={{ fontSize: '1.2rem' }}>SADC Order Management</strong>
        <NavLink to="/customers" style={navLinkStyle}>Customers</NavLink>
        <NavLink to="/orders" style={navLinkStyle}>Orders</NavLink>
        <NavLink to="/reports" style={navLinkStyle}>Reports</NavLink>
      </nav>

      <div style={{ flex: 1, padding: '2rem', maxWidth: 1200, margin: '0 auto', width: '100%' }}>
        <Outlet />
      </div>

      <footer style={{ textAlign: 'center', padding: '1rem', borderTop: '1px solid #ddd', color: '#666' }}>
        SADC Order Management &copy; {new Date().getFullYear()}
      </footer>
    </div>
  );
}

const navLinkStyle: React.CSSProperties = { color: '#ccc', textDecoration: 'none' };

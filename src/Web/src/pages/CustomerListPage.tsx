import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useCustomers } from '../api/customers';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { Pagination } from '../components/Pagination';

export function CustomerListPage() {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const { data, isLoading, error } = useCustomers(search, page);

  return (
    <main>
      <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h1>Customers</h1>
        <Link to="/customers/new" className="btn btn-primary">
          + New Customer
        </Link>
      </header>

      <section aria-label="Search">
        <input
          type="search"
          placeholder="Search by name or email..."
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          aria-label="Search customers"
          style={{ width: '100%', padding: '0.5rem', marginBottom: '1rem' }}
        />
      </section>

      {isLoading && <LoadingSpinner />}
      {error && <p role="alert" className="error">Error loading customers: {String(error)}</p>}

      {data && (
        <>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr>
                <th style={thStyle}>Name</th>
                <th style={thStyle}>Email</th>
                <th style={thStyle}>Country</th>
                <th style={thStyle}>Created</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((c) => (
                <tr key={c.id}>
                  <td style={tdStyle}>
                    <Link to={`/customers/${c.id}`}>{c.name}</Link>
                  </td>
                  <td style={tdStyle}>{c.email}</td>
                  <td style={tdStyle}>{c.countryCode}</td>
                  <td style={tdStyle}>{new Date(c.createdAtUtc).toLocaleDateString()}</td>
                </tr>
              ))}
              {data.items.length === 0 && (
                <tr><td colSpan={4} style={{ ...tdStyle, textAlign: 'center' }}>No customers found</td></tr>
              )}
            </tbody>
          </table>

          <Pagination page={page} totalPages={data.totalPages} onPageChange={setPage} />
        </>
      )}
    </main>
  );
}

const thStyle: React.CSSProperties = { textAlign: 'left', padding: '0.75rem', borderBottom: '2px solid #ddd' };
const tdStyle: React.CSSProperties = { padding: '0.75rem', borderBottom: '1px solid #eee' };

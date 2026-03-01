import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useOrders } from '../api/orders';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { Pagination } from '../components/Pagination';
import type { OrderStatus } from '../types';

const STATUS_OPTIONS: (OrderStatus | '')[] = ['', 'Pending', 'Paid', 'Fulfilled', 'Cancelled'];
const SORT_OPTIONS = [
  { value: '', label: 'Default' },
  { value: 'created', label: 'Created Date' },
  { value: 'total', label: 'Total Amount' },
  { value: 'status', label: 'Status' },
];

export function OrderListPage() {
  const [status, setStatus] = useState<OrderStatus | ''>('');
  const [page, setPage] = useState(1);
  const [sortBy, setSortBy] = useState('');
  const [descending, setDescending] = useState(false);

  const { data, isLoading, error } = useOrders(
    undefined,
    status || undefined,
    page,
    20,
    sortBy || undefined,
    descending,
  );

  return (
    <main>
      <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h1>Orders</h1>
        <Link to="/orders/new" className="btn btn-primary">
          + New Order
        </Link>
      </header>

      <section aria-label="Filters" style={{ display: 'flex', gap: '1rem', marginBottom: '1rem', flexWrap: 'wrap' }}>
        <div>
          <label htmlFor="status-filter">Status: </label>
          <select
            id="status-filter"
            value={status}
            onChange={(e) => { setStatus(e.target.value as OrderStatus | ''); setPage(1); }}
          >
            {STATUS_OPTIONS.map((s) => (
              <option key={s} value={s}>{s || 'All'}</option>
            ))}
          </select>
        </div>
        <div>
          <label htmlFor="sort-filter">Sort by: </label>
          <select id="sort-filter" value={sortBy} onChange={(e) => setSortBy(e.target.value)}>
            {SORT_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>{o.label}</option>
            ))}
          </select>
        </div>
        <div>
          <label>
            <input type="checkbox" checked={descending} onChange={(e) => setDescending(e.target.checked)} />
            {' '}Descending
          </label>
        </div>
      </section>

      {isLoading && <LoadingSpinner />}
      {error && <p role="alert" className="error">Error loading orders: {String(error)}</p>}

      {data && (
        <>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr>
                <th style={thStyle}>Customer</th>
                <th style={thStyle}>Status</th>
                <th style={thStyle}>Currency</th>
                <th style={thStyle}>Total</th>
                <th style={thStyle}>Items</th>
                <th style={thStyle}>Created</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((o) => (
                <tr key={o.id}>
                  <td style={tdStyle}>{o.customerName}</td>
                  <td style={tdStyle}>
                    <span className={`status status-${o.status.toLowerCase()}`}>{o.status}</span>
                  </td>
                  <td style={tdStyle}>{o.currencyCode}</td>
                  <td style={tdStyle}>{o.totalAmount.toFixed(2)}</td>
                  <td style={tdStyle}>{o.lineItemCount}</td>
                  <td style={tdStyle}>
                    <Link to={`/orders/${o.id}`}>
                      {new Date(o.createdAtUtc).toLocaleDateString()}
                    </Link>
                  </td>
                </tr>
              ))}
              {data.items.length === 0 && (
                <tr><td colSpan={6} style={{ ...tdStyle, textAlign: 'center' }}>No orders found</td></tr>
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

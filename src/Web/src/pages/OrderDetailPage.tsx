import { useParams } from 'react-router-dom';
import { useOrder, useUpdateOrderStatus } from '../api/orders';
import { LoadingSpinner } from '../components/LoadingSpinner';
import type { OrderStatus } from '../types';

const VALID_TRANSITIONS: Record<OrderStatus, OrderStatus[]> = {
  Pending: ['Paid', 'Cancelled'],
  Paid: ['Fulfilled', 'Cancelled'],
  Fulfilled: [],
  Cancelled: [],
};

export function OrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: order, isLoading, error } = useOrder(id!);
  const updateStatus = useUpdateOrderStatus(id!);

  async function handleTransition(newStatus: OrderStatus) {
    const idempotencyKey = crypto.randomUUID();
    await updateStatus.mutateAsync({ newStatus, idempotencyKey });
  }

  if (isLoading) return <LoadingSpinner />;
  if (error) return <p role="alert" className="error">Error loading order: {String(error)}</p>;
  if (!order) return <p>Order not found</p>;

  const allowedTransitions = VALID_TRANSITIONS[order.status] ?? [];

  return (
    <main>
      <h1>Order Details</h1>

      <section style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '2rem' }}>
        <div>
          <strong>Order ID:</strong> <span style={{ fontFamily: 'monospace' }}>{order.id}</span>
        </div>
        <div>
          <strong>Customer:</strong> {order.customerName}
        </div>
        <div>
          <strong>Status:</strong>{' '}
          <span className={`status status-${order.status.toLowerCase()}`}>{order.status}</span>
        </div>
        <div>
          <strong>Currency:</strong> {order.currencyCode}
        </div>
        <div>
          <strong>Created:</strong> {new Date(order.createdAtUtc).toLocaleString()}
        </div>
        <div>
          <strong>Updated:</strong> {order.updatedAtUtc ? new Date(order.updatedAtUtc).toLocaleString() : 'N/A'}
        </div>
      </section>

      {allowedTransitions.length > 0 && (
        <section aria-label="Status transitions" style={{ marginBottom: '2rem' }}>
          <strong>Transition to: </strong>
          {allowedTransitions.map((s) => (
            <button
              key={s}
              onClick={() => handleTransition(s)}
              disabled={updateStatus.isPending}
              className="btn"
              style={{ marginRight: '0.5rem' }}
            >
              {s}
            </button>
          ))}
          {updateStatus.isError && <p role="alert" className="error">Failed to update status</p>}
        </section>
      )}

      <h2>Line Items</h2>
      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <thead>
          <tr>
            <th style={thStyle}>SKU</th>
            <th style={thStyle}>Quantity</th>
            <th style={thStyle}>Unit Price</th>
            <th style={thStyle}>Line Total</th>
          </tr>
        </thead>
        <tbody>
          {order.lineItems.map((li) => (
            <tr key={li.id}>
              <td style={tdStyle}>{li.productSku}</td>
              <td style={tdStyle}>{li.quantity}</td>
              <td style={tdStyle}>{order.currencyCode} {li.unitPrice.toFixed(2)}</td>
              <td style={tdStyle}>{order.currencyCode} {li.lineTotal.toFixed(2)}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr>
            <td colSpan={3} style={{ ...thStyle, textAlign: 'right', fontWeight: 'bold' }}>Total:</td>
            <td style={thStyle}><strong>{order.currencyCode} {order.totalAmount.toFixed(2)}</strong></td>
          </tr>
        </tfoot>
      </table>
    </main>
  );
}

const thStyle: React.CSSProperties = { textAlign: 'left', padding: '0.75rem', borderBottom: '2px solid #ddd' };
const tdStyle: React.CSSProperties = { padding: '0.75rem', borderBottom: '1px solid #eee' };

import { useZarReport } from '../api/reports';
import { LoadingSpinner } from '../components/LoadingSpinner';

export function ReportsPage() {
  const { data, isLoading, error } = useZarReport();

  if (isLoading) return <LoadingSpinner message="Loading report..." />;
  if (error) return <p role="alert" className="error">Error loading report: {String(error)}</p>;
  if (!data) return null;

  return (
    <main>
      <h1>ZAR Conversion Report</h1>

      <section style={{ display: 'flex', gap: '2rem', marginBottom: '2rem', flexWrap: 'wrap' }}>
        <div className="card">
          <h3>Grand Total (ZAR)</h3>
          <p style={{ fontSize: '2rem', fontWeight: 'bold' }}>R {data.grandTotalZar.toFixed(2)}</p>
        </div>
        <div className="card">
          <h3>Total Orders</h3>
          <p style={{ fontSize: '2rem', fontWeight: 'bold' }}>{data.totalOrders}</p>
        </div>
        <div className="card">
          <h3>Rounding Strategy</h3>
          <p>{data.roundingStrategy}</p>
        </div>
      </section>

      <h2>Per-Currency Breakdown</h2>
      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <thead>
          <tr>
            <th style={thStyle}>Currency</th>
            <th style={thStyle}>Original Total</th>
            <th style={thStyle}>Exchange Rate</th>
            <th style={thStyle}>Total in ZAR</th>
            <th style={thStyle}>Orders</th>
          </tr>
        </thead>
        <tbody>
          {data.currencyBreakdown.map((cb) => (
            <tr key={cb.currencyCode}>
              <td style={tdStyle}>{cb.currencyCode}</td>
              <td style={tdStyle}>{cb.originalTotal.toFixed(2)}</td>
              <td style={tdStyle}>{cb.exchangeRate.toFixed(4)}</td>
              <td style={tdStyle}>R {cb.totalInZar.toFixed(2)}</td>
              <td style={tdStyle}>{cb.orderCount}</td>
            </tr>
          ))}
          {data.currencyBreakdown.length === 0 && (
            <tr><td colSpan={5} style={{ ...tdStyle, textAlign: 'center' }}>No data available</td></tr>
          )}
        </tbody>
      </table>
    </main>
  );
}

const thStyle: React.CSSProperties = { textAlign: 'left', padding: '0.75rem', borderBottom: '2px solid #ddd' };
const tdStyle: React.CSSProperties = { padding: '0.75rem', borderBottom: '1px solid #eee' };

export function LoadingSpinner({ message = 'Loading...' }: { message?: string }) {
  return (
    <div role="status" aria-live="polite" style={{ padding: '2rem', textAlign: 'center' }}>
      <div className="spinner" aria-hidden="true" />
      <p>{message}</p>
    </div>
  );
}

interface PaginationProps {
  page: number;
  totalPages: number;
  onPageChange: (page: number) => void;
}

export function Pagination({ page, totalPages, onPageChange }: PaginationProps) {
  return (
    <nav aria-label="Pagination" style={{ display: 'flex', gap: '0.5rem', justifyContent: 'center', margin: '1rem 0' }}>
      <button
        onClick={() => onPageChange(page - 1)}
        disabled={page <= 1}
        aria-label="Previous page"
      >
        &laquo; Previous
      </button>
      <span aria-current="page" style={{ padding: '0.5rem 1rem' }}>
        Page {page} of {totalPages}
      </span>
      <button
        onClick={() => onPageChange(page + 1)}
        disabled={page >= totalPages}
        aria-label="Next page"
      >
        Next &raquo;
      </button>
    </nav>
  );
}

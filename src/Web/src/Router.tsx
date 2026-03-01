import { lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Layout } from './Layout';
import { LoadingSpinner } from './components/LoadingSpinner';

// Code splitting with lazy routes
const CustomerListPage = lazy(() => import('./pages/CustomerListPage').then(m => ({ default: m.CustomerListPage })));
const CreateCustomerPage = lazy(() => import('./pages/CreateCustomerPage').then(m => ({ default: m.CreateCustomerPage })));
const OrderListPage = lazy(() => import('./pages/OrderListPage').then(m => ({ default: m.OrderListPage })));
const CreateOrderPage = lazy(() => import('./pages/CreateOrderPage').then(m => ({ default: m.CreateOrderPage })));
const OrderDetailPage = lazy(() => import('./pages/OrderDetailPage').then(m => ({ default: m.OrderDetailPage })));
const ReportsPage = lazy(() => import('./pages/ReportsPage').then(m => ({ default: m.ReportsPage })));

export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<Navigate to="/customers" replace />} />
          <Route
            path="/customers"
            element={<Suspense fallback={<LoadingSpinner />}><CustomerListPage /></Suspense>}
          />
          <Route
            path="/customers/new"
            element={<Suspense fallback={<LoadingSpinner />}><CreateCustomerPage /></Suspense>}
          />
          <Route
            path="/customers/:id"
            element={<Suspense fallback={<LoadingSpinner />}><CustomerListPage /></Suspense>}
          />
          <Route
            path="/orders"
            element={<Suspense fallback={<LoadingSpinner />}><OrderListPage /></Suspense>}
          />
          <Route
            path="/orders/new"
            element={<Suspense fallback={<LoadingSpinner />}><CreateOrderPage /></Suspense>}
          />
          <Route
            path="/orders/:id"
            element={<Suspense fallback={<LoadingSpinner />}><OrderDetailPage /></Suspense>}
          />
          <Route
            path="/reports"
            element={<Suspense fallback={<LoadingSpinner />}><ReportsPage /></Suspense>}
          />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

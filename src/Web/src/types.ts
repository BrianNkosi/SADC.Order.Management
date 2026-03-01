// === Customer Types ===
export interface CustomerDto {
  id: string;
  name: string;
  email: string;
  countryCode: string;
  createdAtUtc: string;
}

export interface CreateCustomerRequest {
  name: string;
  email: string;
  countryCode: string;
}

// === Order Types ===
export type OrderStatus = 'Pending' | 'Paid' | 'Fulfilled' | 'Cancelled';

export interface OrderLineItemDto {
  id: string;
  productSku: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface OrderDto {
  id: string;
  customerId: string;
  customerName: string;
  status: OrderStatus;
  currencyCode: string;
  totalAmount: number;
  lineItems: OrderLineItemDto[];
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface OrderSummaryDto {
  id: string;
  customerName: string;
  status: OrderStatus;
  currencyCode: string;
  totalAmount: number;
  lineItemCount: number;
  createdAtUtc: string;
}

export interface CreateOrderLineItemRequest {
  productSku: string;
  quantity: number;
  unitPrice: number;
}

export interface CreateOrderRequest {
  customerId: string;
  currencyCode: string;
  lineItems: CreateOrderLineItemRequest[];
}

export interface UpdateOrderStatusRequest {
  newStatus: OrderStatus;
  idempotencyKey?: string;
}

// === Pagination ===
export interface PaginatedList<T> {
  items: T[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// === Reports ===
export interface CurrencyBreakdown {
  currencyCode: string;
  originalTotal: number;
  exchangeRate: number;
  totalInZar: number;
  orderCount: number;
}

export interface ZarReportDto {
  grandTotalZar: number;
  totalOrders: number;
  roundingStrategy: string;
  currencyBreakdown: CurrencyBreakdown[];
}

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from './client';
import type {
  OrderDto,
  OrderSummaryDto,
  CreateOrderRequest,
  UpdateOrderStatusRequest,
  PaginatedList,
  OrderStatus,
} from '../types';

export function useOrders(
  customerId?: string,
  status?: OrderStatus,
  page: number = 1,
  pageSize: number = 20,
  sortBy?: string,
  descending: boolean = false,
) {
  return useQuery({
    queryKey: ['orders', customerId, status, page, pageSize, sortBy, descending],
    queryFn: () => {
      const params = new URLSearchParams();
      if (customerId) params.set('customerId', customerId);
      if (status) params.set('status', status);
      params.set('page', page.toString());
      params.set('pageSize', pageSize.toString());
      if (sortBy) params.set('sortBy', sortBy);
      if (descending) params.set('descending', 'true');
      return api.get<PaginatedList<OrderSummaryDto>>(`/orders?${params}`);
    },
  });
}

export function useOrder(id: string) {
  return useQuery({
    queryKey: ['orders', id],
    queryFn: () => api.get<OrderDto>(`/orders/${id}`),
    enabled: !!id,
  });
}

export function useCreateOrder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateOrderRequest) =>
      api.post<OrderDto>('/orders', data),
    onMutate: async () => {
      // Cancel outgoing list refetches to avoid overwriting optimistic state
      await queryClient.cancelQueries({ queryKey: ['orders'] });
    },
    onSettled: () => {
      // Always refetch orders list after mutation completes (success or error)
      queryClient.invalidateQueries({ queryKey: ['orders'] });
    },
  });
}

export function useUpdateOrderStatus(orderId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateOrderStatusRequest) => {
      const headers: Record<string, string> = {};
      if (data.idempotencyKey) {
        headers['Idempotency-Key'] = data.idempotencyKey;
      }
      return api.put<OrderDto>(`/orders/${orderId}/status`, data, headers);
    },
    onMutate: async (data) => {
      // Cancel any outgoing refetches so they don't overwrite our optimistic update
      await queryClient.cancelQueries({ queryKey: ['orders', orderId] });

      // Snapshot the previous value
      const previousOrder = queryClient.getQueryData<OrderDto>(['orders', orderId]);

      // Optimistically update the cache
      if (previousOrder) {
        queryClient.setQueryData<OrderDto>(['orders', orderId], {
          ...previousOrder,
          status: data.newStatus,
          updatedAtUtc: new Date().toISOString(),
        });
      }

      return { previousOrder };
    },
    onError: (_err, _data, context) => {
      // Roll back to the previous value on error
      if (context?.previousOrder) {
        queryClient.setQueryData(['orders', orderId], context.previousOrder);
      }
    },
    onSettled: () => {
      // Refetch to ensure server state is in sync
      queryClient.invalidateQueries({ queryKey: ['orders'] });
    },
  });
}

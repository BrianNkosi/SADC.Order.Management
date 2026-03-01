import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from './client';
import type {
  CustomerDto,
  CreateCustomerRequest,
  PaginatedList,
} from '../types';

export function useCustomers(search: string, page: number, pageSize: number = 20) {
  return useQuery({
    queryKey: ['customers', search, page, pageSize],
    queryFn: () => {
      const params = new URLSearchParams();
      if (search) params.set('search', search);
      params.set('page', page.toString());
      params.set('pageSize', pageSize.toString());
      return api.get<PaginatedList<CustomerDto>>(`/customers?${params}`);
    },
  });
}

export function useCustomer(id: string) {
  return useQuery({
    queryKey: ['customers', id],
    queryFn: () => api.get<CustomerDto>(`/customers/${id}`),
    enabled: !!id,
  });
}

export function useCreateCustomer() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateCustomerRequest) =>
      api.post<CustomerDto>('/customers', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['customers'] });
    },
  });
}

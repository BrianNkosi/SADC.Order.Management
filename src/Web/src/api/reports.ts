import { useQuery } from '@tanstack/react-query';
import { api } from './client';
import type { ZarReportDto } from '../types';

export function useZarReport() {
  return useQuery({
    queryKey: ['reports', 'zar'],
    queryFn: () => api.get<ZarReportDto>('/reports/orders/zar'),
  });
}

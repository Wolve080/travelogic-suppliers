import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { request, toQueryString } from './client'
import type {
  CreateSupplierRequest,
  PagedResult,
  ReferenceData,
  ServiceRequest,
  ServiceResponse,
  Supplier,
  SupplierListParams,
  SupplierSummary,
  UpdateSupplierRequest,
} from './types'

const base = '/api/v1'

export const supplierApi = {
  list: (params: SupplierListParams) =>
    request<PagedResult<SupplierSummary>>(`${base}/suppliers${toQueryString({ ...params })}`),
  get: (id: string) => request<Supplier>(`${base}/suppliers/${id}`),
  create: (body: CreateSupplierRequest) =>
    request<Supplier>(`${base}/suppliers`, { method: 'POST', body: JSON.stringify(body) }),
  update: (id: string, body: UpdateSupplierRequest) =>
    request<Supplier>(`${base}/suppliers/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  remove: (id: string) => request<void>(`${base}/suppliers/${id}`, { method: 'DELETE' }),
  addService: (supplierId: string, body: ServiceRequest) =>
    request<ServiceResponse>(`${base}/suppliers/${supplierId}/services`, { method: 'POST', body: JSON.stringify(body) }),
  updateService: (supplierId: string, serviceId: string, body: ServiceRequest) =>
    request<ServiceResponse>(`${base}/suppliers/${supplierId}/services/${serviceId}`, {
      method: 'PUT',
      body: JSON.stringify(body),
    }),
  removeService: (supplierId: string, serviceId: string) =>
    request<void>(`${base}/suppliers/${supplierId}/services/${serviceId}`, { method: 'DELETE' }),
  referenceData: () => request<ReferenceData>(`${base}/reference-data`),
}

export const supplierKeys = {
  all: ['suppliers'] as const,
  lists: () => [...supplierKeys.all, 'list'] as const,
  list: (params: SupplierListParams) => [...supplierKeys.lists(), params] as const,
  detail: (id: string) => [...supplierKeys.all, 'detail', id] as const,
}

export function useSuppliers(params: SupplierListParams) {
  return useQuery({
    queryKey: supplierKeys.list(params),
    queryFn: () => supplierApi.list(params),
    // Keep showing the current page while the next one loads, instead of flashing a spinner.
    placeholderData: keepPreviousData,
  })
}

export function useSupplier(id: string) {
  return useQuery({ queryKey: supplierKeys.detail(id), queryFn: () => supplierApi.get(id) })
}

export function useReferenceData() {
  return useQuery({
    queryKey: ['reference-data'],
    queryFn: supplierApi.referenceData,
    staleTime: Infinity,
  })
}

/** Looks up the display label for a reference data value, falling back to the raw value. */
export function useLabels() {
  const { data } = useReferenceData()
  const find = (options: { value: string; label: string }[] | undefined, value: string) =>
    options?.find((o) => o.value === value)?.label ?? value
  return {
    supplierType: (value: string) => find(data?.supplierTypes, value),
    serviceCategory: (value: string) => find(data?.serviceCategories, value),
    pricingUnit: (value: string) => find(data?.pricingUnits, value),
  }
}

export function useCreateSupplier() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: supplierApi.create,
    onSuccess: (supplier) => {
      queryClient.setQueryData(supplierKeys.detail(supplier.id), supplier)
      return queryClient.invalidateQueries({ queryKey: supplierKeys.lists() })
    },
  })
}

export function useUpdateSupplier(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: UpdateSupplierRequest) => supplierApi.update(id, body),
    onSuccess: (supplier) => {
      queryClient.setQueryData(supplierKeys.detail(id), supplier)
      return queryClient.invalidateQueries({ queryKey: supplierKeys.lists() })
    },
  })
}

export function useDeleteSupplier() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: supplierApi.remove,
    onSuccess: (_, id) => {
      queryClient.removeQueries({ queryKey: supplierKeys.detail(id) })
      return queryClient.invalidateQueries({ queryKey: supplierKeys.lists() })
    },
  })
}

/** Add, update or remove a service, then refresh the supplier (its version changes too). */
export function useServiceMutations(supplierId: string) {
  const queryClient = useQueryClient()
  const refresh = () => queryClient.invalidateQueries({ queryKey: supplierKeys.all })

  return {
    add: useMutation({
      mutationFn: (body: ServiceRequest) => supplierApi.addService(supplierId, body),
      onSuccess: refresh,
    }),
    update: useMutation({
      mutationFn: ({ serviceId, body }: { serviceId: string; body: ServiceRequest }) =>
        supplierApi.updateService(supplierId, serviceId, body),
      onSuccess: refresh,
    }),
    remove: useMutation({
      mutationFn: (serviceId: string) => supplierApi.removeService(supplierId, serviceId),
      onSuccess: refresh,
    }),
  }
}

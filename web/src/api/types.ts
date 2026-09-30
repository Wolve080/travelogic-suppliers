// See /openapi/v1.json. Enum values come from /reference-data.

export interface Address {
  line1: string | null
  line2: string | null
  city: string
  region: string | null
  country: string
  postalCode: string | null
}

export interface Contact {
  email: string | null
  phone: string | null
  website: string | null
}

export interface ServiceRequest {
  name: string
  category: string
  description: string | null
  price: number
  currency: string
  pricingUnit: string
  durationMinutes: number | null
  capacity: number | null
}

export interface ServiceResponse extends ServiceRequest {
  id: string
}

export interface CreateSupplierRequest {
  name: string
  type: string
  description: string | null
  contact: Contact
  address: Address
  services: ServiceRequest[]
}

export interface UpdateSupplierRequest {
  name: string
  type: string
  description: string | null
  contact: Contact
  address: Address
  isActive: boolean
  version: string
}

export interface Supplier {
  id: string
  name: string
  type: string
  description: string | null
  contact: Contact
  address: Address
  isActive: boolean
  services: ServiceResponse[]
  createdAtUtc: string
  updatedAtUtc: string | null
  version: string
}

export interface SupplierSummary {
  id: string
  name: string
  type: string
  city: string
  country: string
  isActive: boolean
  serviceCount: number
  serviceCategories: string[]
  createdAtUtc: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type SupplierSortField = 'Name' | 'CreatedAt' | 'City'

export interface SupplierListParams {
  search?: string
  type?: string
  sortBy?: SupplierSortField
  descending?: boolean
  page?: number
  pageSize?: number
}

export interface Option {
  value: string
  label: string
}

export interface ReferenceData {
  supplierTypes: Option[]
  serviceCategories: Option[]
  pricingUnits: Option[]
}

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  code?: string
  correlationId?: string
  errors?: Record<string, string[]>
}

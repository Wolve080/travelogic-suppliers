import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'
import { z } from 'zod'
import { ApiError } from '../../api/client'
import type { CreateSupplierRequest, ServiceRequest, Supplier, UpdateSupplierRequest } from '../../api/types'

// Client side rules mirror the API's validators for fast feedback. The API stays the authority:
// its field errors are mapped back onto the form by applyServerErrors.

const text = (label: string, max: number) =>
  z.string().trim().max(max, `${label} must be ${max} characters or fewer`)
const required = (label: string, max: number) => text(label, max).min(1, `${label} is required`)
const positiveWholeNumber = (label: string) =>
  z
    .string()
    .trim()
    .regex(/^\d*$/, `${label} must be a whole number`)
    .refine((v) => v === '' || Number(v) > 0, `${label} must be more than 0`)

export const serviceSchema = z.object({
  name: required('Service name', 200),
  category: z.string().min(1, 'Choose a category'),
  description: text('Description', 2000),
  price: z
    .string()
    .trim()
    .min(1, 'Price is required')
    .regex(/^\d+(\.\d{1,2})?$/, 'Enter an amount like 1450 or 1450.50'),
  currency: z
    .string()
    .trim()
    .regex(/^[A-Za-z]{3}$/, 'Use a 3 letter currency code, e.g. ZAR'),
  pricingUnit: z.string().min(1, 'Choose how the price is charged'),
  durationMinutes: positiveWholeNumber('Duration'),
  capacity: positiveWholeNumber('Capacity'),
})

const detailsShape = {
  name: required('Supplier name', 200),
  type: z.string().min(1, 'Choose a supplier type'),
  description: text('Description', 2000),
  contact: z.object({
    email: text('Email', 256).refine((v) => v === '' || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v), 'Enter a valid email address'),
    phone: text('Phone', 30).refine((v) => v === '' || /^\+?[0-9 ()-]{7,30}$/.test(v), 'Enter a valid phone number'),
    website: text('Website', 256).refine((v) => v === '' || /^https?:\/\/\S+\.\S+$/i.test(v), 'Enter a full address starting with https://'),
  }),
  address: z.object({
    line1: text('Address line 1', 200),
    line2: text('Address line 2', 200),
    city: required('City', 100),
    region: text('Province / region', 100),
    country: required('Country', 100),
    postalCode: text('Postal code', 20),
  }),
}

export const createSupplierSchema = z
  .object({ ...detailsShape, services: z.array(serviceSchema).max(200) })
  .superRefine((values, ctx) => {
    const seen = new Set<string>()
    values.services.forEach((service, index) => {
      const key = service.name.trim().toLowerCase()
      if (key && seen.has(key)) {
        ctx.addIssue({ code: 'custom', path: ['services', index, 'name'], message: 'Each service needs a different name' })
      }
      seen.add(key)
    })
  })

export const editSupplierSchema = z.object({ ...detailsShape, isActive: z.boolean() })

export type ServiceFormValues = z.infer<typeof serviceSchema>
export type CreateSupplierFormValues = z.infer<typeof createSupplierSchema>
export type EditSupplierFormValues = z.infer<typeof editSupplierSchema>

export const emptyService = (): ServiceFormValues => ({
  name: '',
  category: '',
  description: '',
  price: '',
  currency: 'ZAR',
  pricingUnit: '',
  durationMinutes: '',
  capacity: '',
})

export const emptySupplier = (): CreateSupplierFormValues => ({
  name: '',
  type: '',
  description: '',
  contact: { email: '', phone: '', website: '' },
  address: { line1: '', line2: '', city: '', region: '', country: 'South Africa', postalCode: '' },
  services: [],
})

const orNull = (value: string) => (value.trim() === '' ? null : value.trim())
const numberOrNull = (value: string) => (value.trim() === '' ? null : Number(value))

export function toServiceRequest(values: ServiceFormValues): ServiceRequest {
  return {
    name: values.name.trim(),
    category: values.category,
    description: orNull(values.description),
    price: Number(values.price),
    currency: values.currency.trim().toUpperCase(),
    pricingUnit: values.pricingUnit,
    durationMinutes: numberOrNull(values.durationMinutes),
    capacity: numberOrNull(values.capacity),
  }
}

function toDetails(values: Omit<CreateSupplierFormValues, 'services'>) {
  return {
    name: values.name.trim(),
    type: values.type,
    description: orNull(values.description),
    contact: {
      email: orNull(values.contact.email),
      phone: orNull(values.contact.phone),
      website: orNull(values.contact.website),
    },
    address: {
      line1: orNull(values.address.line1),
      line2: orNull(values.address.line2),
      city: values.address.city.trim(),
      region: orNull(values.address.region),
      country: values.address.country.trim(),
      postalCode: orNull(values.address.postalCode),
    },
  }
}

export function toCreateRequest(values: CreateSupplierFormValues): CreateSupplierRequest {
  return { ...toDetails(values), services: values.services.map(toServiceRequest) }
}

export function toUpdateRequest(values: EditSupplierFormValues, version: string): UpdateSupplierRequest {
  return { ...toDetails(values), isActive: values.isActive, version }
}

export function toEditValues(supplier: Supplier): EditSupplierFormValues {
  return {
    name: supplier.name,
    type: supplier.type,
    description: supplier.description ?? '',
    contact: {
      email: supplier.contact.email ?? '',
      phone: supplier.contact.phone ?? '',
      website: supplier.contact.website ?? '',
    },
    address: {
      line1: supplier.address.line1 ?? '',
      line2: supplier.address.line2 ?? '',
      city: supplier.address.city,
      region: supplier.address.region ?? '',
      country: supplier.address.country,
      postalCode: supplier.address.postalCode ?? '',
    },
    isActive: supplier.isActive,
  }
}

export function toServiceValues(service: ServiceRequest): ServiceFormValues {
  return {
    name: service.name,
    category: service.category,
    description: service.description ?? '',
    price: service.price.toFixed(2),
    currency: service.currency,
    pricingUnit: service.pricingUnit,
    durationMinutes: service.durationMinutes?.toString() ?? '',
    capacity: service.capacity?.toString() ?? '',
  }
}

/**
 * Puts the API's field errors on the matching form fields ("services[0].price" -> "services.0.price")
 * and returns a message for the form as a whole.
 */
export function applyServerErrors<T extends FieldValues>(error: unknown, setError: UseFormSetError<T>): string {
  if (!(error instanceof ApiError)) {
    return 'Something went wrong. Please try again.'
  }

  const fieldErrors = Object.entries(error.fieldErrors)
  for (const [key, messages] of fieldErrors) {
    const path = key.replace(/\[(\d+)\]/g, '.$1') as Path<T>
    setError(path, { type: 'server', message: messages[0] })
  }

  return fieldErrors.length > 0 ? 'Please fix the highlighted fields.' : error.message
}

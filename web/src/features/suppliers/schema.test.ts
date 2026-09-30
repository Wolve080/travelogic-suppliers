import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/client'
import { applyServerErrors, createSupplierSchema, emptyService, emptySupplier, toCreateRequest } from './schema'

const validSupplier = () => ({
  ...emptySupplier(),
  name: '  Table Bay View Hotel ',
  type: 'Accommodation',
  address: { ...emptySupplier().address, city: 'Cape Town' },
  services: [
    { ...emptyService(), name: 'Sea View Room', category: 'Accommodation', price: '3450.50', pricingUnit: 'PerRoomPerNight', capacity: '2', currency: 'zar' },
  ],
})

describe('createSupplierSchema', () => {
  it('accepts a valid supplier', () => {
    expect(createSupplierSchema.safeParse(validSupplier()).success).toBe(true)
  })

  it('reports duplicate service names on the second service', () => {
    const values = validSupplier()
    values.services.push({ ...values.services[0]!, name: 'sea view room' })

    const result = createSupplierSchema.safeParse(values)

    expect(result.success).toBe(false)
    expect(result.error?.issues.map((i) => i.path.join('.'))).toContain('services.1.name')
  })

  it('rejects prices with more than two decimals and malformed contact details', () => {
    const values = validSupplier()
    values.services[0]!.price = '10.555'
    values.contact = { email: 'nope', phone: 'abc', website: 'example.com' }

    const paths = createSupplierSchema.safeParse(values).error?.issues.map((i) => i.path.join('.'))

    expect(paths).toEqual(expect.arrayContaining(['services.0.price', 'contact.email', 'contact.phone', 'contact.website']))
  })
})

describe('toCreateRequest', () => {
  it('trims text, turns blanks into null and converts numbers', () => {
    const request = toCreateRequest(createSupplierSchema.parse(validSupplier()))

    expect(request.name).toBe('Table Bay View Hotel')
    expect(request.description).toBeNull()
    expect(request.contact).toEqual({ email: null, phone: null, website: null })
    expect(request.services[0]).toEqual({
      name: 'Sea View Room',
      category: 'Accommodation',
      description: null,
      price: 3450.5,
      currency: 'ZAR',
      pricingUnit: 'PerRoomPerNight',
      durationMinutes: null,
      capacity: 2,
    })
  })
})

describe('applyServerErrors', () => {
  it('maps API field paths onto form paths', () => {
    const setError = vi.fn()
    const error = new ApiError(400, {
      title: 'One or more fields are invalid.',
      errors: { name: ['Taken'], 'services[0].currency': ['Bad currency'] },
    })

    const message = applyServerErrors(error, setError)

    expect(setError).toHaveBeenCalledWith('name', { type: 'server', message: 'Taken' })
    expect(setError).toHaveBeenCalledWith('services.0.currency', { type: 'server', message: 'Bad currency' })
    expect(message).toBe('Please fix the highlighted fields.')
  })

  it('returns the problem detail when there are no field errors', () => {
    const error = new ApiError(409, { detail: 'A supplier called "X" already exists.', code: 'supplier_name_taken' })

    expect(applyServerErrors(error, vi.fn())).toBe('A supplier called "X" already exists.')
  })
})

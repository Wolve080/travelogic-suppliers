import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { PagedResult, Supplier, SupplierSummary } from '../../api/types'
import { mockApi, renderApp } from '../../test/utils'

afterEach(() => vi.unstubAllGlobals())

const summary = (overrides: Partial<SupplierSummary> = {}): SupplierSummary => ({
  id: 'b1d6c3a2-0000-7000-8000-000000000001',
  name: 'Kruger Horizons Safaris',
  type: 'Safari',
  city: 'Hazyview',
  country: 'South Africa',
  isActive: true,
  serviceCount: 2,
  serviceCategories: ['Activity'],
  createdAtUtc: '2026-09-01T10:00:00Z',
  ...overrides,
})

const page = (items: SupplierSummary[]): PagedResult<SupplierSummary> => ({
  items,
  page: 1,
  pageSize: 12,
  totalCount: items.length,
  totalPages: 1,
})

describe('Supplier list (home screen)', () => {
  it('lists suppliers with their location and service count', async () => {
    mockApi((url) => (url.pathname === '/api/v1/suppliers' ? { status: 200, body: page([summary()]) } : undefined))

    renderApp('/')

    const card = await screen.findByRole('link', { name: /Kruger Horizons Safaris/ })
    expect(within(card).getByText('Hazyview, South Africa')).toBeInTheDocument()
    expect(within(card).getByText('2 services')).toBeInTheDocument()
  })

  it('sends the search to the API after the user stops typing', async () => {
    const fetchMock = mockApi((url) =>
      url.pathname === '/api/v1/suppliers' ? { status: 200, body: page(url.searchParams.get('search') ? [] : [summary()]) } : undefined,
    )
    renderApp('/')
    await screen.findByText('Kruger Horizons Safaris')

    await userEvent.type(screen.getByRole('searchbox', { name: 'Search suppliers' }), 'lodge')

    expect(await screen.findByText('No suppliers match')).toBeInTheDocument()
    const searched = fetchMock.mock.calls.map(([input]) => String(input)).filter((u) => u.includes('search='))
    expect(searched).toEqual([expect.stringContaining('search=lodge')])
  })

  it('invites the user to add a supplier when there are none', async () => {
    mockApi((url) => (url.pathname === '/api/v1/suppliers' ? { status: 200, body: page([]) } : undefined))

    renderApp('/')

    expect(await screen.findByText('No suppliers yet')).toBeInTheDocument()
  })
})

describe('Add supplier', () => {
  it('validates required fields before calling the API', async () => {
    const fetchMock = mockApi(() => undefined)
    renderApp('/suppliers/new')

    await userEvent.click(await screen.findByRole('button', { name: 'Save supplier' }))

    expect(await screen.findByText('Supplier name is required')).toBeInTheDocument()
    expect(screen.getByText('Choose a supplier type')).toBeInTheDocument()
    expect(screen.getByText('City is required')).toBeInTheDocument()
    expect(fetchMock.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(false)
  })

  it('creates a supplier with a service and opens it', async () => {
    let posted: unknown
    const created: Supplier = {
      id: 'new-id',
      name: 'Table Bay View Hotel',
      type: 'Accommodation',
      description: null,
      contact: { email: null, phone: null, website: null },
      address: { line1: null, line2: null, city: 'Cape Town', region: null, country: 'South Africa', postalCode: null },
      isActive: true,
      services: [],
      createdAtUtc: '2026-09-30T10:00:00Z',
      updatedAtUtc: null,
      version: 'AAAAAAAAB9E=',
    }
    mockApi((url, init) => {
      if (url.pathname === '/api/v1/suppliers' && init.method === 'POST') {
        posted = JSON.parse(String(init.body))
        return { status: 201, body: created }
      }
      if (url.pathname === '/api/v1/suppliers') return { status: 200, body: page([]) }
      return undefined
    })
    const { router } = renderApp('/suppliers/new')
    const user = userEvent.setup()

    await user.type(await screen.findByLabelText(/^Name/), 'Table Bay View Hotel')
    await user.selectOptions(screen.getByLabelText(/^Type/), 'Accommodation')
    await user.type(screen.getByLabelText(/^City/), 'Cape Town')
    await user.click(screen.getByRole('button', { name: 'Add service' }))
    await user.type(screen.getByLabelText(/^Service name/), 'Sea View Room')
    await user.selectOptions(screen.getByLabelText(/^Category/), 'Accommodation')
    await user.type(screen.getByLabelText(/^Price/), '3450')
    await user.selectOptions(screen.getByLabelText(/^Charged/), 'PerRoomPerNight')
    await user.click(screen.getByRole('button', { name: 'Save supplier' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/suppliers/new-id'))
    expect(posted).toMatchObject({
      name: 'Table Bay View Hotel',
      type: 'Accommodation',
      address: { city: 'Cape Town', country: 'South Africa' },
      services: [{ name: 'Sea View Room', category: 'Accommodation', price: 3450, currency: 'ZAR', pricingUnit: 'PerRoomPerNight' }],
    })
  })

  it('shows a conflict from the API', async () => {
    mockApi((url, init) =>
      url.pathname === '/api/v1/suppliers' && init.method === 'POST'
        ? { status: 409, body: { status: 409, code: 'supplier_name_taken', detail: 'A supplier called "Dup" already exists.' } }
        : undefined,
    )
    renderApp('/suppliers/new')
    const user = userEvent.setup()

    await user.type(await screen.findByLabelText(/^Name/), 'Dup')
    await user.selectOptions(screen.getByLabelText(/^Type/), 'Safari')
    await user.type(screen.getByLabelText(/^City/), 'Hazyview')
    await user.click(screen.getByRole('button', { name: 'Save supplier' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('A supplier called "Dup" already exists.')
  })
})

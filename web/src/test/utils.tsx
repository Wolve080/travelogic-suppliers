import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { vi } from 'vitest'
import { routes } from '../app/router'
import type { ReferenceData } from '../api/types'

export const referenceData: ReferenceData = {
  supplierTypes: [
    { value: 'Accommodation', label: 'Accommodation' },
    { value: 'Safari', label: 'Safari' },
  ],
  serviceCategories: [
    { value: 'Accommodation', label: 'Accommodation' },
    { value: 'Activity', label: 'Activity' },
  ],
  pricingUnits: [
    { value: 'PerPerson', label: 'Per person' },
    { value: 'PerRoomPerNight', label: 'Per room per night' },
  ],
}

type Handler = (url: URL, init: RequestInit) => { status: number; body?: unknown } | undefined

/** Replaces fetch with a tiny router over the API. Unmatched calls fail the test loudly. */
export function mockApi(handler: Handler) {
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = new URL(String(input), 'http://localhost')
    const response =
      url.pathname === '/api/v1/reference-data' ? { status: 200, body: referenceData } : handler(url, init)
    if (!response) throw new Error(`Unexpected request: ${init.method ?? 'GET'} ${url.pathname}${url.search}`)
    return new Response(response.body === undefined ? null : JSON.stringify(response.body), {
      status: response.status,
      headers: { 'Content-Type': response.status >= 400 ? 'application/problem+json' : 'application/json' },
    })
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

export function renderApp(path: string) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  const router = createMemoryRouter(routes, { initialEntries: [path] })
  return {
    router,
    ...render(
      <QueryClientProvider client={queryClient}>
        <RouterProvider router={router} />
      </QueryClientProvider>,
    ),
  }
}

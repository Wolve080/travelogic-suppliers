import { createBrowserRouter, type RouteObject } from 'react-router'
import { SupplierListPage } from '../features/suppliers/SupplierListPage'
import { Layout, NotFoundPage } from './Layout'

// The home screen loads eagerly. Form pages are split into their own chunks and fetched on navigation,
// so the first load does not pay for the form and validation libraries.
export const routes: RouteObject[] = [
  {
    element: <Layout />,
    children: [
      { index: true, element: <SupplierListPage /> },
      {
        path: 'suppliers/new',
        lazy: async () => ({ Component: (await import('../features/suppliers/CreateSupplierPage')).CreateSupplierPage }),
      },
      {
        path: 'suppliers/:id',
        lazy: async () => ({ Component: (await import('../features/suppliers/SupplierDetailPage')).SupplierDetailPage }),
      },
      {
        path: 'suppliers/:id/edit',
        lazy: async () => ({ Component: (await import('../features/suppliers/EditSupplierPage')).EditSupplierPage }),
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]

export const router = createBrowserRouter(routes)

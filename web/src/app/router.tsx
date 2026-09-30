import { createBrowserRouter, type RouteObject } from 'react-router'
import { SupplierListPage } from '../features/suppliers/SupplierListPage'
import { Layout, NotFoundPage } from './Layout'

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

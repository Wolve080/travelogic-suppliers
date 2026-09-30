import { Plus } from 'lucide-react'
import { Link, Outlet, ScrollRestoration } from 'react-router'
import { ButtonLink } from '../components/ui'

export function Layout() {
  return (
    <div className="min-h-screen">
      <header className="sticky top-0 z-10 border-b border-slate-200 bg-white/85 backdrop-blur">
        <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4 sm:px-6">
          <Link to="/" className="flex items-center gap-3">
            <img src="/logo.png" alt="Travelogic" width={139} height={36} className="h-9 w-auto" />
            <span className="hidden border-l border-slate-200 pl-3 text-sm font-medium text-accent-600 sm:inline">Suppliers</span>
          </Link>
          <ButtonLink to="/suppliers/new" size="sm">
            <Plus className="size-4" aria-hidden /> New supplier
          </ButtonLink>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6 sm:py-10">
        <Outlet />
      </main>
      <ScrollRestoration />
    </div>
  )
}

export function NotFoundPage() {
  return (
    <div className="py-24 text-center">
      <p className="font-display text-5xl font-semibold text-brand-700">404</p>
      <p className="mt-3 text-slate-600">This page doesn't exist.</p>
      <ButtonLink to="/" className="mt-6">
        Back to suppliers
      </ButtonLink>
    </div>
  )
}

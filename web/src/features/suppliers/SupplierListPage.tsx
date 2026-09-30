import { Plus, Search, Store } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useSearchParams } from 'react-router'
import { useReferenceData, useSuppliers } from '../../api/suppliers'
import type { SupplierListParams, SupplierSortField } from '../../api/types'
import { Input, Select } from '../../components/form'
import { Pagination } from '../../components/Pagination'
import { ButtonLink, Card, ErrorState, PageHeader, Spinner } from '../../components/ui'
import { SupplierCard } from './components/SupplierCard'

const PAGE_SIZE = 12

const sortOptions: { value: string; label: string; sortBy: SupplierSortField; descending: boolean }[] = [
  { value: 'name', label: 'Name (A–Z)', sortBy: 'Name', descending: false },
  { value: '-name', label: 'Name (Z–A)', sortBy: 'Name', descending: true },
  { value: '-created', label: 'Newest first', sortBy: 'CreatedAt', descending: true },
  { value: 'city', label: 'City', sortBy: 'City', descending: false },
]

/** Home screen: every supplier, with search, filter, sort and paging kept in the URL so views can be shared. */
export function SupplierListPage() {
  const [params, setParams] = useSearchParams()
  const search = params.get('q') ?? ''
  const type = params.get('type') ?? ''
  const sort = sortOptions.find((o) => o.value === params.get('sort')) ?? sortOptions[0]!
  const page = Math.max(1, Number(params.get('page')) || 1)

  const query: SupplierListParams = {
    search: search || undefined,
    type: type || undefined,
    sortBy: sort.sortBy,
    descending: sort.descending || undefined,
    page,
    pageSize: PAGE_SIZE,
  }
  const { data, isPending, isError, error, refetch, isPlaceholderData } = useSuppliers(query)
  const { data: reference } = useReferenceData()

  const update = (changes: Record<string, string>) =>
    setParams(
      (current) => {
        const next = new URLSearchParams(current)
        for (const [key, value] of Object.entries(changes)) {
          if (value) next.set(key, value)
          else next.delete(key)
        }
        if (!('page' in changes)) next.delete('page')
        return next
      },
      { replace: true },
    )

  // Search as the user types, but only once they pause.
  const [searchInput, setSearchInput] = useState(search)
  const searchTimer = useRef<ReturnType<typeof setTimeout>>(undefined)
  const onSearchChange = (value: string) => {
    setSearchInput(value)
    clearTimeout(searchTimer.current)
    searchTimer.current = setTimeout(() => update({ q: value.trim() }), 300)
  }
  useEffect(() => () => clearTimeout(searchTimer.current), [])

  const filtered = Boolean(search || type)

  return (
    <>
      <PageHeader
        title="Suppliers"
        description="Hotels, safari operators, transport companies and everyone else whose services we sell."
        actions={
          <ButtonLink to="/suppliers/new">
            <Plus className="size-4" aria-hidden /> Add supplier
          </ButtonLink>
        }
      />

      <div className="mb-6 flex flex-col gap-3 sm:flex-row">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-stone-400" aria-hidden />
          <Input
            type="search"
            aria-label="Search suppliers"
            placeholder="Search by name, city or country"
            value={searchInput}
            onChange={(e) => onSearchChange(e.target.value)}
            className="pl-9"
          />
        </div>
        <Select
          aria-label="Filter by type"
          value={type}
          onChange={(e) => update({ type: e.target.value })}
          options={reference?.supplierTypes ?? []}
          placeholder="All types"
          className="sm:w-48"
        />
        <Select
          aria-label="Sort"
          value={sort.value}
          onChange={(e) => update({ sort: e.target.value === 'name' ? '' : e.target.value })}
          options={sortOptions}
          className="sm:w-44"
        />
      </div>

      {isPending ? (
        <Spinner label="Loading suppliers" />
      ) : isError ? (
        <ErrorState error={error} onRetry={() => refetch()} />
      ) : data.items.length === 0 ? (
        <Card className="flex flex-col items-center px-6 py-16 text-center">
          <div className="grid size-12 place-items-center rounded-full bg-brand-50 text-brand-700">
            <Store className="size-6" aria-hidden />
          </div>
          <h2 className="mt-4 font-semibold text-stone-900">{filtered ? 'No suppliers match' : 'No suppliers yet'}</h2>
          <p className="mt-1 max-w-sm text-sm text-stone-500">
            {filtered ? 'Try a different search or clear the filters.' : 'Add your first supplier and the services they offer.'}
          </p>
          {!filtered && (
            <ButtonLink to="/suppliers/new" className="mt-6">
              <Plus className="size-4" aria-hidden /> Add supplier
            </ButtonLink>
          )}
        </Card>
      ) : (
        <>
          <p className="mb-3 text-sm text-stone-500" aria-live="polite">
            {data.totalCount} {data.totalCount === 1 ? 'supplier' : 'suppliers'}
          </p>
          <ul className={`grid gap-4 sm:grid-cols-2 lg:grid-cols-3 ${isPlaceholderData ? 'opacity-60' : ''}`}>
            {data.items.map((supplier) => (
              <li key={supplier.id}>
                <SupplierCard supplier={supplier} />
              </li>
            ))}
          </ul>
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            pageSize={data.pageSize}
            onPageChange={(next) => update({ page: next > 1 ? String(next) : '' })}
          />
        </>
      )}
    </>
  )
}

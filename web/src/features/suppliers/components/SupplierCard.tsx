import { ChevronRight, MapPin } from 'lucide-react'
import { Link } from 'react-router'
import { useLabels } from '../../../api/suppliers'
import type { SupplierSummary } from '../../../api/types'
import { Badge } from '../../../components/ui'
import { SupplierTypeIcon } from './SupplierTypeIcon'

export function SupplierCard({ supplier }: { supplier: SupplierSummary }) {
  const labels = useLabels()

  return (
    <Link
      to={`/suppliers/${supplier.id}`}
      className="group flex h-full flex-col rounded-xl border border-stone-200 bg-white p-5 shadow-sm transition hover:-translate-y-0.5 hover:border-brand-200 hover:shadow-md"
    >
      <div className="flex items-start gap-4">
        <div className="grid size-11 shrink-0 place-items-center rounded-lg bg-brand-50 text-brand-700">
          <SupplierTypeIcon type={supplier.type} className="size-5" />
        </div>
        <div className="min-w-0 flex-1">
          <h2 className="truncate font-semibold text-stone-900 group-hover:text-brand-700">{supplier.name}</h2>
          <p className="text-sm text-stone-500">{labels.supplierType(supplier.type)}</p>
        </div>
        <ChevronRight className="size-5 shrink-0 text-stone-300 transition group-hover:text-brand-500" aria-hidden />
      </div>

      <p className="mt-4 flex items-center gap-1.5 text-sm text-stone-600">
        <MapPin className="size-4 text-stone-400" aria-hidden />
        {supplier.city}, {supplier.country}
      </p>

      <div className="mt-auto flex flex-wrap items-center gap-1.5 pt-4">
        <Badge tone={supplier.serviceCount > 0 ? 'brand' : 'muted'}>
          {supplier.serviceCount} {supplier.serviceCount === 1 ? 'service' : 'services'}
        </Badge>
        {supplier.serviceCategories.map((category) => (
          <Badge key={category}>{labels.serviceCategory(category)}</Badge>
        ))}
        {!supplier.isActive && <Badge tone="accent">Inactive</Badge>}
      </div>
    </Link>
  )
}

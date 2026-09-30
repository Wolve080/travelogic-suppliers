import { Bed, Binoculars, Bus, Compass, Map, Store, Utensils, type LucideIcon } from 'lucide-react'

const typeIcons: Record<string, LucideIcon> = {
  Accommodation: Bed,
  TourOperator: Map,
  Safari: Binoculars,
  Transport: Bus,
  Restaurant: Utensils,
  ActivityProvider: Compass,
}

export function SupplierTypeIcon({ type, className }: { type: string; className?: string }) {
  const Icon = typeIcons[type] ?? Store
  return <Icon className={className} aria-hidden />
}

import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from './ui'

interface PaginationProps {
  page: number
  totalPages: number
  totalCount: number
  pageSize: number
  onPageChange: (page: number) => void
}

export function Pagination({ page, totalPages, totalCount, pageSize, onPageChange }: PaginationProps) {
  if (totalPages <= 1) return null

  const from = (page - 1) * pageSize + 1
  const to = Math.min(page * pageSize, totalCount)

  return (
    <nav aria-label="Pagination" className="mt-8 flex items-center justify-between">
      <p className="text-sm text-stone-500">
        Showing <span className="font-medium text-stone-700">{from}</span>–<span className="font-medium text-stone-700">{to}</span> of{' '}
        <span className="font-medium text-stone-700">{totalCount}</span>
      </p>
      <div className="flex gap-2">
        <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>
          <ChevronLeft className="size-4" aria-hidden /> Previous
        </Button>
        <Button variant="secondary" size="sm" disabled={page >= totalPages} onClick={() => onPageChange(page + 1)}>
          Next <ChevronRight className="size-4" aria-hidden />
        </Button>
      </div>
    </nav>
  )
}

import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowLeft } from 'lucide-react'
import { useState } from 'react'
import { FormProvider, useForm } from 'react-hook-form'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError } from '../../api/client'
import { useSupplier, useUpdateSupplier } from '../../api/suppliers'
import type { Supplier } from '../../api/types'
import { Alert, Button, ButtonLink, Card, ErrorState, PageHeader, Spinner } from '../../components/ui'
import { SupplierDetailsFields } from './components/SupplierDetailsFields'
import { applyServerErrors, editSupplierSchema, toEditValues, toUpdateRequest, type EditSupplierFormValues } from './schema'

export function EditSupplierPage() {
  const { id = '' } = useParams()
  const { data: supplier, isPending, isError, error, refetch } = useSupplier(id)

  if (isPending) return <Spinner label="Loading supplier" />
  if (isError) return <ErrorState error={error} onRetry={() => refetch()} />
  // Keyed on version so the form resets if a newer copy of the supplier is loaded.
  return <EditSupplierForm key={supplier.version} supplier={supplier} />
}

function EditSupplierForm({ supplier }: { supplier: Supplier }) {
  const navigate = useNavigate()
  const updateSupplier = useUpdateSupplier(supplier.id)
  const { refetch } = useSupplier(supplier.id)
  const [formError, setFormError] = useState<string | null>(null)
  const [conflict, setConflict] = useState(false)

  const form = useForm<EditSupplierFormValues>({
    resolver: zodResolver(editSupplierSchema),
    defaultValues: toEditValues(supplier),
  })

  const submit = form.handleSubmit(async (values) => {
    setFormError(null)
    setConflict(false)
    try {
      // The version we loaded goes back to the API, so we cannot silently overwrite someone else's change.
      await updateSupplier.mutateAsync(toUpdateRequest(values, supplier.version))
      navigate(`/suppliers/${supplier.id}`)
    } catch (error) {
      if (error instanceof ApiError && error.code === 'concurrency_conflict') {
        setConflict(true)
      } else {
        setFormError(applyServerErrors(error, form.setError))
      }
      window.scrollTo({ top: 0, behavior: 'smooth' })
    }
  })

  return (
    <>
      <Link to={`/suppliers/${supplier.id}`} className="mb-4 inline-flex items-center gap-1 text-sm text-stone-500 hover:text-stone-800">
        <ArrowLeft className="size-4" aria-hidden /> Back to {supplier.name}
      </Link>
      <PageHeader title={`Edit ${supplier.name}`} description="Services are managed from the supplier's page." />

      <FormProvider {...form}>
        <form onSubmit={submit} noValidate className="space-y-6">
          {conflict && (
            <Alert tone="warning">
              <p className="font-medium">Someone else changed this supplier while you were editing.</p>
              <p className="mt-1">
                Load their changes, then make your edits again.{' '}
                <button type="button" className="font-medium underline" onClick={() => refetch()}>
                  Load latest version
                </button>
              </p>
            </Alert>
          )}
          {formError && <Alert>{formError}</Alert>}

          <Card className="p-6 sm:p-8">
            <SupplierDetailsFields />
            <label className="mt-2 flex items-center gap-3 border-t border-stone-200 pt-6 text-sm">
              <input type="checkbox" {...form.register('isActive')} className="size-4 rounded accent-brand-700" />
              <span>
                <span className="font-medium text-stone-900">Active</span>
                <span className="block text-stone-500">Inactive suppliers stay on record but should not be booked.</span>
              </span>
            </label>
          </Card>

          <div className="flex justify-end gap-2">
            <ButtonLink to={`/suppliers/${supplier.id}`} variant="ghost">
              Cancel
            </ButtonLink>
            <Button type="submit" loading={form.formState.isSubmitting}>
              Save changes
            </Button>
          </div>
        </form>
      </FormProvider>
    </>
  )
}

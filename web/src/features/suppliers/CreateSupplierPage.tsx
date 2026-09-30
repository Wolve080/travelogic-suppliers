import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowLeft, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { FormProvider, useFieldArray, useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router'
import { useCreateSupplier } from '../../api/suppliers'
import { Alert, Button, ButtonLink, Card, PageHeader } from '../../components/ui'
import { ServiceFields } from './components/ServiceFields'
import { SupplierDetailsFields } from './components/SupplierDetailsFields'
import {
  applyServerErrors,
  createSupplierSchema,
  emptyService,
  emptySupplier,
  toCreateRequest,
  type CreateSupplierFormValues,
} from './schema'

/** Captures a supplier and its services in one go, saved in a single API call. */
export function CreateSupplierPage() {
  const navigate = useNavigate()
  const createSupplier = useCreateSupplier()
  const [formError, setFormError] = useState<string | null>(null)

  const form = useForm<CreateSupplierFormValues>({
    resolver: zodResolver(createSupplierSchema),
    defaultValues: emptySupplier(),
  })
  const services = useFieldArray({ control: form.control, name: 'services' })

  const submit = form.handleSubmit(async (values) => {
    setFormError(null)
    try {
      const supplier = await createSupplier.mutateAsync(toCreateRequest(values))
      navigate(`/suppliers/${supplier.id}`, { state: { created: true } })
    } catch (error) {
      setFormError(applyServerErrors(error, form.setError))
      window.scrollTo({ top: 0, behavior: 'smooth' })
    }
  })

  return (
    <>
      <Link to="/" className="mb-4 inline-flex items-center gap-1 text-sm text-stone-500 hover:text-stone-800">
        <ArrowLeft className="size-4" aria-hidden /> All suppliers
      </Link>
      <PageHeader title="Add a supplier" description="Capture the supplier's details and the services they offer." />

      <FormProvider {...form}>
        <form onSubmit={submit} noValidate className="space-y-6">
          {formError && <Alert>{formError}</Alert>}

          <Card className="p-6 sm:p-8">
            <SupplierDetailsFields />
          </Card>

          <Card className="p-6 sm:p-8">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
              <div>
                <h2 className="font-semibold text-stone-900">Services</h2>
                <p className="mt-1 text-sm text-stone-500">
                  What the supplier sells, e.g. a night's accommodation or a half day tour. You can also add these later.
                </p>
              </div>
              <Button variant="secondary" onClick={() => services.append(emptyService())}>
                <Plus className="size-4" aria-hidden /> Add service
              </Button>
            </div>

            {form.formState.errors.services?.message && (
              <p className="mt-4 text-sm text-red-600">{form.formState.errors.services.message}</p>
            )}

            {services.fields.length === 0 ? (
              <p className="mt-6 rounded-lg border border-dashed border-stone-300 px-4 py-8 text-center text-sm text-stone-500">
                No services added yet.
              </p>
            ) : (
              <ol className="mt-6 space-y-4">
                {services.fields.map((field, index) => (
                  <li key={field.id} className="rounded-lg border border-stone-200 bg-stone-50/60 p-4 sm:p-5">
                    <div className="mb-4 flex items-center justify-between">
                      <h3 className="text-sm font-semibold text-stone-700">Service {index + 1}</h3>
                      <Button variant="ghost" size="sm" onClick={() => services.remove(index)} aria-label={`Remove service ${index + 1}`}>
                        <Trash2 className="size-4" aria-hidden /> Remove
                      </Button>
                    </div>
                    <ServiceFields prefix={`services.${index}`} />
                  </li>
                ))}
              </ol>
            )}
          </Card>

          <div className="flex justify-end gap-2">
            <ButtonLink to="/" variant="ghost">
              Cancel
            </ButtonLink>
            <Button type="submit" loading={form.formState.isSubmitting}>
              Save supplier
            </Button>
          </div>
        </form>
      </FormProvider>
    </>
  )
}

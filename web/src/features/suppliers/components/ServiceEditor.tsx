import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { FormProvider, useForm } from 'react-hook-form'
import type { ServiceRequest } from '../../../api/types'
import { Alert, Button } from '../../../components/ui'
import { applyServerErrors, emptyService, serviceSchema, toServiceRequest, toServiceValues, type ServiceFormValues } from '../schema'
import { ServiceFields } from './ServiceFields'

interface ServiceEditorProps {
  initial?: ServiceRequest
  submitLabel: string
  onSubmit: (request: ServiceRequest) => Promise<unknown>
  onCancel: () => void
}

export function ServiceEditor({ initial, submitLabel, onSubmit, onCancel }: ServiceEditorProps) {
  const form = useForm<ServiceFormValues>({
    resolver: zodResolver(serviceSchema),
    defaultValues: initial ? toServiceValues(initial) : emptyService(),
  })
  const [formError, setFormError] = useState<string | null>(null)

  const submit = form.handleSubmit(async (values) => {
    setFormError(null)
    try {
      await onSubmit(toServiceRequest(values))
    } catch (error) {
      setFormError(applyServerErrors(error, form.setError))
    }
  })

  return (
    <FormProvider {...form}>
      <form onSubmit={submit} noValidate className="space-y-4">
        {formError && <Alert>{formError}</Alert>}
        <ServiceFields />
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onClick={onCancel}>
            Cancel
          </Button>
          <Button type="submit" loading={form.formState.isSubmitting}>
            {submitLabel}
          </Button>
        </div>
      </form>
    </FormProvider>
  )
}

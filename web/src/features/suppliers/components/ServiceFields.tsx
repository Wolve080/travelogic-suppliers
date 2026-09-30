import { get, useFormContext } from 'react-hook-form'
import { useReferenceData } from '../../../api/suppliers'
import { Field, Input, Select, TextArea } from '../../../components/form'

/**
 * The inputs for one service. Used for each row of the create form's services list (prefix
 * "services.0") and for the stand-alone add/edit service form (no prefix).
 */
export function ServiceFields({ prefix }: { prefix?: string }) {
  const {
    register,
    formState: { errors },
  } = useFormContext()
  const { data: reference } = useReferenceData()
  const path = (field: string) => (prefix ? `${prefix}.${field}` : field)
  const error = (field: string): string | undefined => get(errors, path(field))?.message

  return (
    <div className="grid gap-4 sm:grid-cols-6">
      <Field label="Service name" required error={error('name')} className="sm:col-span-4">
        <Input {...register(path('name'))} placeholder="e.g. Half Day Game Drive" />
      </Field>
      <Field label="Category" required error={error('category')} className="sm:col-span-2">
        <Select {...register(path('category'))} options={reference?.serviceCategories ?? []} placeholder="Choose…" />
      </Field>
      <Field label="Price" required error={error('price')} className="sm:col-span-2">
        <Input {...register(path('price'))} inputMode="decimal" placeholder="0.00" />
      </Field>
      <Field label="Currency" required error={error('currency')} className="sm:col-span-1">
        <Input {...register(path('currency'))} maxLength={3} className="uppercase" />
      </Field>
      <Field label="Charged" required error={error('pricingUnit')} className="sm:col-span-3">
        <Select {...register(path('pricingUnit'))} options={reference?.pricingUnits ?? []} placeholder="Choose…" />
      </Field>
      <Field label="Duration (minutes)" error={error('durationMinutes')} hint="e.g. 240 for a half day" className="sm:col-span-3">
        <Input {...register(path('durationMinutes'))} inputMode="numeric" />
      </Field>
      <Field label="Max guests" error={error('capacity')} className="sm:col-span-3">
        <Input {...register(path('capacity'))} inputMode="numeric" />
      </Field>
      <Field label="Description" error={error('description')} className="sm:col-span-6">
        <TextArea {...register(path('description'))} rows={2} />
      </Field>
    </div>
  )
}

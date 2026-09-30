import { get, useFormContext } from 'react-hook-form'
import { useReferenceData } from '../../../api/suppliers'
import { Field, FormSection, Input, Select, TextArea } from '../../../components/form'

/** Supplier details, contact and address sections, shared by the create and edit forms. */
export function SupplierDetailsFields() {
  const {
    register,
    formState: { errors },
  } = useFormContext()
  const { data: reference } = useReferenceData()
  const error = (path: string): string | undefined => get(errors, path)?.message

  return (
    <>
      <FormSection title="Supplier" description="Who the supplier is and what kind of business they run.">
        <Field label="Name" required error={error('name')} className="sm:col-span-2">
          <Input {...register('name')} placeholder="e.g. Kruger Horizons Safaris" autoComplete="organization" />
        </Field>
        <Field label="Type" required error={error('type')}>
          <Select {...register('type')} options={reference?.supplierTypes ?? []} placeholder="Choose a type" />
        </Field>
        <div className="hidden sm:block" />
        <Field label="Description" error={error('description')} className="sm:col-span-2">
          <TextArea {...register('description')} placeholder="What makes this supplier special?" />
        </Field>
      </FormSection>

      <FormSection title="Contact" description="How our team reaches the supplier. All optional.">
        <Field label="Email" error={error('contact.email')}>
          <Input {...register('contact.email')} type="email" placeholder="reservations@example.com" autoComplete="email" />
        </Field>
        <Field label="Phone" error={error('contact.phone')}>
          <Input {...register('contact.phone')} type="tel" placeholder="+27 21 555 0100" autoComplete="tel" />
        </Field>
        <Field label="Website" error={error('contact.website')} className="sm:col-span-2">
          <Input {...register('contact.website')} type="url" placeholder="https://example.com" autoComplete="url" />
        </Field>
      </FormSection>

      <FormSection title="Location" description="Where the supplier is based.">
        <Field label="Address line 1" error={error('address.line1')} className="sm:col-span-2">
          <Input {...register('address.line1')} autoComplete="address-line1" />
        </Field>
        <Field label="Address line 2" error={error('address.line2')} className="sm:col-span-2">
          <Input {...register('address.line2')} autoComplete="address-line2" />
        </Field>
        <Field label="City / town" required error={error('address.city')}>
          <Input {...register('address.city')} autoComplete="address-level2" />
        </Field>
        <Field label="Province / region" error={error('address.region')}>
          <Input {...register('address.region')} autoComplete="address-level1" />
        </Field>
        <Field label="Country" required error={error('address.country')}>
          <Input {...register('address.country')} autoComplete="country-name" />
        </Field>
        <Field label="Postal code" error={error('address.postalCode')}>
          <Input {...register('address.postalCode')} autoComplete="postal-code" />
        </Field>
      </FormSection>
    </>
  )
}

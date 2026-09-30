import { useId, type ComponentProps, type ReactElement, type ReactNode, cloneElement } from 'react'

const control =
  'block w-full rounded-lg border-0 bg-white px-3 py-2 text-sm text-slate-900 shadow-sm ring-1 ring-inset ring-slate-300 placeholder:text-slate-400 focus:ring-2 focus:ring-inset focus:ring-brand-500 focus:outline-none aria-[invalid=true]:ring-red-400'

interface FieldProps {
  label: string
  error?: string
  hint?: string
  required?: boolean
  className?: string
  children: ReactElement<{ id?: string; 'aria-invalid'?: boolean; 'aria-describedby'?: string }>
}

export function Field({ label, error, hint, required, className = '', children }: FieldProps) {
  const id = useId()
  const describedBy = error ? `${id}-error` : hint ? `${id}-hint` : undefined

  return (
    <div className={className}>
      <label htmlFor={id} className="mb-1.5 block text-sm font-medium text-slate-700">
        {label}
        {required && <span className="ml-0.5 text-red-500" aria-hidden>*</span>}
      </label>
      {cloneElement(children, { id, 'aria-invalid': Boolean(error), 'aria-describedby': describedBy })}
      {error ? (
        <p id={`${id}-error`} className="mt-1.5 text-sm text-red-600">
          {error}
        </p>
      ) : (
        hint && (
          <p id={`${id}-hint`} className="mt-1.5 text-xs text-slate-500">
            {hint}
          </p>
        )
      )}
    </div>
  )
}

export function Input(props: ComponentProps<'input'>) {
  return <input {...props} className={`${control} ${props.className ?? ''}`} />
}

export function TextArea(props: ComponentProps<'textarea'>) {
  return <textarea rows={3} {...props} className={`${control} ${props.className ?? ''}`} />
}

export function Select({ options, placeholder, ...props }: ComponentProps<'select'> & {
  options: { value: string; label: string }[]
  placeholder?: string
}) {
  return (
    <select {...props} className={`${control} pr-8 ${props.className ?? ''}`}>
      {placeholder !== undefined && <option value="">{placeholder}</option>}
      {options.map((o) => (
        <option key={o.value} value={o.value}>
          {o.label}
        </option>
      ))}
    </select>
  )
}

export function FormSection({ title, description, children }: { title: string; description?: string; children: ReactNode }) {
  return (
    <section className="grid gap-6 border-b border-slate-200 py-8 first:pt-0 last:border-0 md:grid-cols-3">
      <div>
        <h2 className="font-semibold text-slate-900">{title}</h2>
        {description && <p className="mt-1 text-sm text-slate-500">{description}</p>}
      </div>
      <div className="grid gap-5 sm:grid-cols-2 md:col-span-2">{children}</div>
    </section>
  )
}

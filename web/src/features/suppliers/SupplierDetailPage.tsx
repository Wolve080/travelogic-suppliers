import { ArrowLeft, Globe, Mail, MapPin, Pencil, Phone, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router'
import { ApiError } from '../../api/client'
import { useDeleteSupplier, useLabels, useServiceMutations, useSupplier } from '../../api/suppliers'
import type { ServiceResponse, Supplier } from '../../api/types'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { Alert, Badge, Button, ButtonLink, Card, ErrorState, Spinner } from '../../components/ui'
import { ServiceEditor } from './components/ServiceEditor'
import { SupplierTypeIcon } from './components/SupplierTypeIcon'
import { formatDate, formatDuration, formatPrice } from './format'

export function SupplierDetailPage() {
  const { id = '' } = useParams()
  const { data: supplier, isPending, isError, error, refetch } = useSupplier(id)

  if (isPending) return <Spinner label="Loading supplier" />
  if (isError) {
    return error instanceof ApiError && error.status === 404 ? (
      <ErrorState error={new Error('This supplier does not exist, or has been deleted.')} />
    ) : (
      <ErrorState error={error} onRetry={() => refetch()} />
    )
  }
  return <SupplierDetail supplier={supplier} />
}

function SupplierDetail({ supplier }: { supplier: Supplier }) {
  const labels = useLabels()
  const navigate = useNavigate()
  const location = useLocation()
  const justCreated = (location.state as { created?: boolean } | null)?.created === true
  const deleteSupplier = useDeleteSupplier()
  const [confirmDelete, setConfirmDelete] = useState(false)

  const remove = async () => {
    await deleteSupplier.mutateAsync(supplier.id)
    navigate('/', { replace: true })
  }

  return (
    <>
      <Link to="/" className="mb-4 inline-flex items-center gap-1 text-sm text-stone-500 hover:text-stone-800">
        <ArrowLeft className="size-4" aria-hidden /> All suppliers
      </Link>

      {justCreated && (
        <div className="mb-6">
          <Alert tone="success">Supplier saved.</Alert>
        </div>
      )}

      <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div className="flex items-start gap-4">
          <div className="grid size-14 shrink-0 place-items-center rounded-xl bg-brand-700 text-white shadow-sm">
            <SupplierTypeIcon type={supplier.type} className="size-7" />
          </div>
          <div>
            <h1 className="font-display text-3xl font-semibold tracking-tight text-stone-900">{supplier.name}</h1>
            <div className="mt-1.5 flex flex-wrap items-center gap-2 text-sm text-stone-500">
              <span>{labels.supplierType(supplier.type)}</span>
              <span aria-hidden>·</span>
              <span>Added {formatDate(supplier.createdAtUtc)}</span>
              {!supplier.isActive && <Badge tone="accent">Inactive</Badge>}
            </div>
          </div>
        </div>
        <div className="flex gap-2">
          <ButtonLink to={`/suppliers/${supplier.id}/edit`} variant="secondary">
            <Pencil className="size-4" aria-hidden /> Edit
          </ButtonLink>
          <Button variant="secondary" onClick={() => setConfirmDelete(true)} className="text-red-600">
            <Trash2 className="size-4" aria-hidden /> Delete
          </Button>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <Card className="space-y-5 p-6 lg:self-start">
          {supplier.description && <p className="text-sm leading-relaxed text-stone-600">{supplier.description}</p>}
          <dl className="space-y-3 text-sm">
            <InfoRow icon={<MapPin className="size-4" />} label="Address">
              {[supplier.address.line1, supplier.address.line2, supplier.address.city, supplier.address.region, supplier.address.postalCode, supplier.address.country]
                .filter(Boolean)
                .join(', ')}
            </InfoRow>
            {supplier.contact.email && (
              <InfoRow icon={<Mail className="size-4" />} label="Email">
                <a className="text-brand-700 hover:underline" href={`mailto:${supplier.contact.email}`}>
                  {supplier.contact.email}
                </a>
              </InfoRow>
            )}
            {supplier.contact.phone && (
              <InfoRow icon={<Phone className="size-4" />} label="Phone">
                <a className="text-brand-700 hover:underline" href={`tel:${supplier.contact.phone.replace(/\s/g, '')}`}>
                  {supplier.contact.phone}
                </a>
              </InfoRow>
            )}
            {supplier.contact.website && (
              <InfoRow icon={<Globe className="size-4" />} label="Website">
                <a className="break-all text-brand-700 hover:underline" href={supplier.contact.website} target="_blank" rel="noreferrer">
                  {supplier.contact.website.replace(/^https?:\/\//, '')}
                </a>
              </InfoRow>
            )}
          </dl>
        </Card>

        <div className="lg:col-span-2">
          <ServicesPanel supplier={supplier} />
        </div>
      </div>

      <ConfirmDialog
        open={confirmDelete}
        title={`Delete ${supplier.name}?`}
        confirmLabel="Delete supplier"
        loading={deleteSupplier.isPending}
        onConfirm={remove}
        onCancel={() => setConfirmDelete(false)}
      >
        The supplier and its {supplier.services.length} {supplier.services.length === 1 ? 'service' : 'services'} will be removed.
        This cannot be undone.
      </ConfirmDialog>
    </>
  )
}

function InfoRow({ icon, label, children }: { icon: React.ReactNode; label: string; children: React.ReactNode }) {
  return (
    <div className="flex gap-3">
      <dt className="mt-0.5 text-stone-400">
        {icon}
        <span className="sr-only">{label}</span>
      </dt>
      <dd className="text-stone-700">{children}</dd>
    </div>
  )
}

type Editing = { mode: 'add' } | { mode: 'edit'; service: ServiceResponse } | null

function ServicesPanel({ supplier }: { supplier: Supplier }) {
  const labels = useLabels()
  const mutations = useServiceMutations(supplier.id)
  const [editing, setEditing] = useState<Editing>(null)
  const [removing, setRemoving] = useState<ServiceResponse | null>(null)
  const [removeError, setRemoveError] = useState<string | null>(null)

  const remove = async () => {
    if (!removing) return
    setRemoveError(null)
    try {
      await mutations.remove.mutateAsync(removing.id)
    } catch (error) {
      setRemoveError(error instanceof Error ? error.message : 'Could not remove the service.')
    } finally {
      setRemoving(null)
    }
  }

  return (
    <Card>
      <div className="flex items-center justify-between border-b border-stone-200 px-6 py-4">
        <div>
          <h2 className="font-semibold text-stone-900">Services</h2>
          <p className="text-sm text-stone-500">{supplier.services.length} offered</p>
        </div>
        {editing?.mode !== 'add' && (
          <Button size="sm" onClick={() => setEditing({ mode: 'add' })}>
            <Plus className="size-4" aria-hidden /> Add service
          </Button>
        )}
      </div>

      {removeError && (
        <div className="px-6 pt-4">
          <Alert>{removeError}</Alert>
        </div>
      )}

      {editing?.mode === 'add' && (
        <div className="border-b border-stone-200 bg-stone-50/60 px-6 py-5">
          <h3 className="mb-4 text-sm font-semibold text-stone-700">New service</h3>
          <ServiceEditor
            submitLabel="Add service"
            onSubmit={async (request) => {
              await mutations.add.mutateAsync(request)
              setEditing(null)
            }}
            onCancel={() => setEditing(null)}
          />
        </div>
      )}

      {supplier.services.length === 0 && editing?.mode !== 'add' ? (
        <p className="px-6 py-12 text-center text-sm text-stone-500">This supplier has no services yet.</p>
      ) : (
        <ul className="divide-y divide-stone-100">
          {supplier.services.map((service) =>
            editing?.mode === 'edit' && editing.service.id === service.id ? (
              <li key={service.id} className="bg-stone-50/60 px-6 py-5">
                <h3 className="mb-4 text-sm font-semibold text-stone-700">Edit {service.name}</h3>
                <ServiceEditor
                  initial={service}
                  submitLabel="Save changes"
                  onSubmit={async (request) => {
                    await mutations.update.mutateAsync({ serviceId: service.id, body: request })
                    setEditing(null)
                  }}
                  onCancel={() => setEditing(null)}
                />
              </li>
            ) : (
              <li key={service.id} className="group flex flex-col gap-3 px-6 py-4 sm:flex-row sm:items-center">
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <p className="font-medium text-stone-900">{service.name}</p>
                    <Badge>{labels.serviceCategory(service.category)}</Badge>
                  </div>
                  <p className="mt-1 text-sm text-stone-500">
                    {[
                      formatDuration(service.durationMinutes),
                      service.capacity && `up to ${service.capacity} guests`,
                      service.description,
                    ]
                      .filter(Boolean)
                      .join(' · ')}
                  </p>
                </div>
                <div className="text-left sm:text-right">
                  <p className="font-semibold text-stone-900">{formatPrice(service.price, service.currency)}</p>
                  <p className="text-xs text-stone-500">{labels.pricingUnit(service.pricingUnit).toLowerCase()}</p>
                </div>
                <div className="flex gap-1 sm:opacity-0 sm:transition sm:group-focus-within:opacity-100 sm:group-hover:opacity-100">
                  <Button variant="ghost" size="sm" aria-label={`Edit ${service.name}`} onClick={() => setEditing({ mode: 'edit', service })}>
                    <Pencil className="size-4" aria-hidden />
                  </Button>
                  <Button variant="ghost" size="sm" aria-label={`Remove ${service.name}`} onClick={() => setRemoving(service)}>
                    <Trash2 className="size-4 text-red-600" aria-hidden />
                  </Button>
                </div>
              </li>
            ),
          )}
        </ul>
      )}

      <ConfirmDialog
        open={removing !== null}
        title={`Remove ${removing?.name ?? 'service'}?`}
        confirmLabel="Remove service"
        loading={mutations.remove.isPending}
        onConfirm={remove}
        onCancel={() => setRemoving(null)}
      >
        The service will no longer be available to book.
      </ConfirmDialog>
    </Card>
  )
}

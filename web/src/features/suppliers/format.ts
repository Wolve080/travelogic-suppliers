export function formatPrice(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat('en-ZA', { style: 'currency', currency, maximumFractionDigits: 2 }).format(amount)
  } catch {
    return `${currency} ${amount.toFixed(2)}`
  }
}

export function formatDuration(minutes: number | null) {
  if (!minutes) return null
  const days = Math.floor(minutes / 1440)
  const hours = Math.floor((minutes % 1440) / 60)
  const mins = minutes % 60
  return [days && `${days}d`, hours && `${hours}h`, mins && `${mins}m`].filter(Boolean).join(' ')
}

export function formatDate(iso: string) {
  return new Intl.DateTimeFormat('en-ZA', { dateStyle: 'medium' }).format(new Date(iso))
}

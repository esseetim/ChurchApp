import { useCallback, useEffect, useState } from 'react'
import { api } from '../lib/api'
import { DonationStatus, type DonationLedgerItem } from '../lib/contracts'

const today = () => new Date().toISOString().slice(0, 10)

export function LedgerPage() {
  const [startDate, setStartDate] = useState(today())
  const [endDate, setEndDate] = useState(today())
  const [includeVoided, setIncludeVoided] = useState(false)
  const [items, setItems] = useState<DonationLedgerItem[]>([])
  const [status, setStatus] = useState<string | null>(null)

  const loadDonations = useCallback(async () => {
    try {
      const response = await api.getDonations({
        page: 1,
        pageSize: 200,
        startDate,
        endDate,
        includeVoided,
      })
      setItems(response.donations)
      setStatus(`Loaded ${response.donations.length} donation(s).`)
    } catch (error) {
      setStatus((error as Error).message)
    }
  }, [endDate, includeVoided, startDate])

  useEffect(() => {
    void loadDonations()
  }, [loadDonations])

  const voidDonation = async (item: DonationLedgerItem) => {
    const reason = window.prompt('Void reason:')
    if (!reason) {
      return
    }

    try {
      await api.voidDonation(item.id, {
        reason,
        enteredBy: 'volunteer',
        expectedVersion: item.version,
      })
      setStatus(`Voided donation ${item.id}`)
      await loadDonations()
    } catch (error) {
      setStatus((error as Error).message)
    }
  }

  return (
    <div className="space-y-4">
      <h2 className="text-xl font-bold text-slate-900">Donation Ledger</h2>

      <div className="grid gap-3 rounded-xl border border-blue-100 p-4 md:grid-cols-4">
        <input className="rounded-md border border-slate-300 px-3 py-2 text-sm" type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
        <input className="rounded-md border border-slate-300 px-3 py-2 text-sm" type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
        <label className="flex items-center gap-2 text-sm text-slate-700">
          <input type="checkbox" checked={includeVoided} onChange={(e) => setIncludeVoided(e.target.checked)} />
          Include voided
        </label>
        <button className="rounded-md bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-700" onClick={() => void loadDonations()} type="button">
          Refresh Ledger
        </button>
      </div>

      <div className="overflow-x-auto rounded-xl border border-slate-200">
        <table className="min-w-full divide-y divide-slate-200 text-sm">
          <thead className="bg-slate-50">
            <tr>
              <th className="px-3 py-2 text-left font-semibold text-slate-700">Date</th>
              <th className="px-3 py-2 text-left font-semibold text-slate-700">Amount</th>
              <th className="px-3 py-2 text-left font-semibold text-slate-700">Status</th>
              <th className="px-3 py-2 text-left font-semibold text-slate-700">Service</th>
              <th className="px-3 py-2 text-left font-semibold text-slate-700">Action</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 bg-white">
            {items.map((item) => (
              <tr key={item.id}>
                <td className="px-3 py-2">{item.donationDate}</td>
                <td className="px-3 py-2">${item.amount.toFixed(2)}</td>
                <td className="px-3 py-2">{item.status === DonationStatus.Active ? 'Active' : 'Voided'}</td>
                <td className="px-3 py-2">{item.serviceName ?? '-'}</td>
                <td className="px-3 py-2">
                  {item.status === DonationStatus.Active ? (
                    <button className="rounded-md border border-red-300 px-2 py-1 text-xs font-semibold text-red-700 hover:bg-red-50" onClick={() => void voidDonation(item)} type="button">
                      Void
                    </button>
                  ) : (
                    <span className="text-xs text-slate-500">{item.voidReason ?? '-'}</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {status && <p className="rounded-md border border-blue-200 bg-blue-50 px-3 py-2 text-sm text-blue-900">{status}</p>}
    </div>
  )
}

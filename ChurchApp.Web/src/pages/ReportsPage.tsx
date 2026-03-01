import { useState } from 'react'
import type { FormEvent } from 'react'
import { api } from '../lib/api'
import { DonationType, type TimeRangeReportResponse } from '../lib/contracts'

const today = () => new Date().toISOString().slice(0, 10)

const donationTypeLabel = (type: DonationType) => {
  if (type === DonationType.GeneralOffering) return 'General Offering'
  if (type === DonationType.Tithe) return 'Tithe'
  return 'Building Fund'
}

export function ReportsPage() {
  const [startDate, setStartDate] = useState(today())
  const [endDate, setEndDate] = useState(today())
  const [persistReport, setPersistReport] = useState(true)
  const [report, setReport] = useState<TimeRangeReportResponse | null>(null)
  const [status, setStatus] = useState<string | null>(null)

  const runReport = async (event: FormEvent) => {
    event.preventDefault()
    try {
      const response = await api.getTimeRangeReport({ startDate, endDate, persistReport })
      setReport(response)
      setStatus('Report generated.')
    } catch (error) {
      setStatus((error as Error).message)
    }
  }

  return (
    <div className="space-y-4">
      <h2 className="text-xl font-bold text-slate-900">Time-Range Report</h2>

      <form className="grid gap-3 rounded-xl border border-blue-100 p-4 md:grid-cols-4" onSubmit={runReport}>
        <input className="rounded-md border border-slate-300 px-3 py-2 text-sm" type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
        <input className="rounded-md border border-slate-300 px-3 py-2 text-sm" type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
        <label className="flex items-center gap-2 text-sm text-slate-700">
          <input type="checkbox" checked={persistReport} onChange={(e) => setPersistReport(e.target.checked)} />
          Persist report
        </label>
        <button className="rounded-md bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-700" type="submit">Generate</button>
      </form>

      {report && (
        <div className="space-y-3 rounded-xl border border-slate-200 p-4">
          <p className="text-sm text-slate-700">
            <span className="font-semibold">Range:</span> {report.startDate} → {report.endDate}
          </p>
          <p className="text-xl font-bold text-slate-900">${report.totalAmount.toFixed(2)}</p>
          <p className="text-sm text-slate-700">{report.donationCount} total donation(s)</p>

          <div className="overflow-x-auto rounded-lg border border-slate-100">
            <table className="min-w-full divide-y divide-slate-100 text-sm">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-3 py-2 text-left font-semibold text-slate-700">Type</th>
                  <th className="px-3 py-2 text-left font-semibold text-slate-700">Amount</th>
                  <th className="px-3 py-2 text-left font-semibold text-slate-700">Count</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {report.breakdown.map((item) => (
                  <tr key={item.type}>
                    <td className="px-3 py-2">{donationTypeLabel(item.type)}</td>
                    <td className="px-3 py-2">${item.totalAmount.toFixed(2)}</td>
                    <td className="px-3 py-2">{item.donationCount}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {status && <p className="rounded-md border border-blue-200 bg-blue-50 px-3 py-2 text-sm text-blue-900">{status}</p>}
    </div>
  )
}

import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { api } from '../lib/api'
import type { Family, Member, SummaryItem } from '../lib/contracts'

const today = () => new Date().toISOString().slice(0, 10)

export function SummariesPage() {
  const [members, setMembers] = useState<Member[]>([])
  const [families, setFamilies] = useState<Family[]>([])
  const [serviceName, setServiceName] = useState('Sunday Service')
  const [startDate, setStartDate] = useState(today())
  const [endDate, setEndDate] = useState(today())
  const [memberId, setMemberId] = useState('')
  const [familyId, setFamilyId] = useState('')
  const [summaries, setSummaries] = useState<SummaryItem[]>([])
  const [status, setStatus] = useState<string | null>(null)

  useEffect(() => {
    void (async () => {
      const [memberResponse, familyResponse] = await Promise.all([
        api.getMembers({ page: 1, pageSize: 200 }),
        api.getFamilies({ page: 1, pageSize: 200 }),
      ])
      setMembers(memberResponse.members)
      setFamilies(familyResponse.families)
    })()
  }, [])

  const runServiceSummary = async (event: FormEvent) => {
    event.preventDefault()
    try {
      const response = await api.getServiceSummaries({ serviceName, startDate, endDate })
      setSummaries(response.summaries)
      setStatus(`Loaded ${response.summaries.length} service summaries.`)
    } catch (error) {
      setStatus((error as Error).message)
    }
  }

  const runMemberSummary = async (event: FormEvent) => {
    event.preventDefault()
    if (!memberId) {
      setStatus('Select a member first.')
      return
    }
    try {
      const response = await api.getMemberSummaries({ memberId, startDate, endDate })
      setSummaries(response.summaries)
      setStatus(`Loaded ${response.summaries.length} member summaries.`)
    } catch (error) {
      setStatus((error as Error).message)
    }
  }

  const runFamilySummary = async (event: FormEvent) => {
    event.preventDefault()
    if (!familyId) {
      setStatus('Select a family first.')
      return
    }
    try {
      const response = await api.getFamilySummaries({ familyId, startDate, endDate })
      setSummaries(response.summaries)
      setStatus(`Loaded ${response.summaries.length} family summaries.`)
    } catch (error) {
      setStatus((error as Error).message)
    }
  }

  return (
    <div className="space-y-4">
      <h2 className="text-xl font-bold text-slate-900">Summaries</h2>

      <div className="grid gap-4 lg:grid-cols-3">
        <form className="space-y-2 rounded-xl border border-blue-100 p-4" onSubmit={runServiceSummary}>
          <h3 className="font-semibold text-slate-900">Service</h3>
          <input className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" value={serviceName} onChange={(e) => setServiceName(e.target.value)} />
          <input className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
          <input className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
          <button className="rounded-md bg-blue-600 px-3 py-2 text-sm font-semibold text-white hover:bg-blue-700" type="submit">Run Service Summary</button>
        </form>

        <form className="space-y-2 rounded-xl border border-blue-100 p-4" onSubmit={runMemberSummary}>
          <h3 className="font-semibold text-slate-900">Member</h3>
          <select className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" value={memberId} onChange={(e) => setMemberId(e.target.value)}>
            <option value="">Select member</option>
            {members.map((member) => <option key={member.id} value={member.id}>{member.firstName} {member.lastName}</option>)}
          </select>
          <input className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
          <input className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
          <button className="rounded-md bg-blue-600 px-3 py-2 text-sm font-semibold text-white hover:bg-blue-700" type="submit">Run Member Summary</button>
        </form>

        <form className="space-y-2 rounded-xl border border-blue-100 p-4" onSubmit={runFamilySummary}>
          <h3 className="font-semibold text-slate-900">Family</h3>
          <select className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" value={familyId} onChange={(e) => setFamilyId(e.target.value)}>
            <option value="">Select family</option>
            {families.map((family) => <option key={family.id} value={family.id}>{family.name}</option>)}
          </select>
          <input className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
          <input className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
          <button className="rounded-md bg-blue-600 px-3 py-2 text-sm font-semibold text-white hover:bg-blue-700" type="submit">Run Family Summary</button>
        </form>
      </div>

      <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
        {summaries.map((summary) => (
          <div className="rounded-xl border border-slate-200 p-4" key={summary.id}>
            <p className="text-xs font-semibold uppercase tracking-wide text-blue-700">{summary.serviceName ?? 'Summary'}</p>
            <p className="mt-1 text-lg font-bold text-slate-900">${summary.totalAmount.toFixed(2)}</p>
            <p className="text-sm text-slate-600">{summary.donationCount} donation(s)</p>
            <p className="text-xs text-slate-500">{summary.startDate} → {summary.endDate}</p>
          </div>
        ))}
      </div>

      {status && <p className="rounded-md border border-blue-200 bg-blue-50 px-3 py-2 text-sm text-blue-900">{status}</p>}
    </div>
  )
}

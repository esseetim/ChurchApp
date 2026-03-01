import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { api } from '../lib/api'
import { DonationMethod, DonationType, type Family, type Member } from '../lib/contracts'

const donationTypeOptions = [
  { value: DonationType.GeneralOffering, label: 'General Offering' },
  { value: DonationType.Tithe, label: 'Tithe' },
  { value: DonationType.BuildingFund, label: 'Building Fund' },
]

const donationMethodOptions = [
  { value: DonationMethod.Cash, label: 'Cash' },
  { value: DonationMethod.CashApp, label: 'CashApp' },
  { value: DonationMethod.Zelle, label: 'Zelle' },
  { value: DonationMethod.Check, label: 'Check' },
  { value: DonationMethod.Card, label: 'Card' },
  { value: DonationMethod.Other, label: 'Other' },
]

const today = () => new Date().toISOString().slice(0, 10)

export function DonationEntryPage() {
  const [members, setMembers] = useState<Member[]>([])
  const [families, setFamilies] = useState<Family[]>([])
  const [memberId, setMemberId] = useState('')
  const [familyId, setFamilyId] = useState('')
  const [serviceName, setServiceName] = useState('Sunday Service')
  const [enteredBy, setEnteredBy] = useState('volunteer')
  const [donationDate, setDonationDate] = useState(today())
  const [amount, setAmount] = useState('0')
  const [type, setType] = useState<DonationType>(DonationType.GeneralOffering)
  const [method, setMethod] = useState<DonationMethod>(DonationMethod.Cash)
  const [notes, setNotes] = useState('')
  const [status, setStatus] = useState<string | null>(null)

  const [newMemberFirstName, setNewMemberFirstName] = useState('')
  const [newMemberLastName, setNewMemberLastName] = useState('')
  const [newFamilyName, setNewFamilyName] = useState('')

  const loadLookups = async () => {
    const [memberResponse, familyResponse] = await Promise.all([
      api.getMembers({ page: 1, pageSize: 200 }),
      api.getFamilies({ page: 1, pageSize: 200 }),
    ])
    setMembers(memberResponse.members)
    setFamilies(familyResponse.families)
  }

  useEffect(() => {
    void loadLookups()
  }, [])

  const onCreateMember = async (event: FormEvent) => {
    event.preventDefault()
    try {
      const response = await api.createMember({
        firstName: newMemberFirstName,
        lastName: newMemberLastName,
      })
      setStatus(`Member created: ${response.memberId}`)
      setNewMemberFirstName('')
      setNewMemberLastName('')
      await loadLookups()
      setMemberId(response.memberId)
    } catch (error) {
      setStatus((error as Error).message)
    }
  }

  const onCreateFamily = async (event: FormEvent) => {
    event.preventDefault()
    try {
      const response = await api.createFamily({ name: newFamilyName })
      setStatus(`Family created: ${response.familyId}`)
      setNewFamilyName('')
      await loadLookups()
      setFamilyId(response.familyId)
    } catch (error) {
      setStatus((error as Error).message)
    }
  }

  const onLinkMemberToFamily = async () => {
    if (!memberId || !familyId) {
      setStatus('Select both member and family to link.')
      return
    }
    try {
      await api.addFamilyMember(familyId, { memberId })
      setStatus('Member linked to family.')
      await loadLookups()
    } catch (error) {
      setStatus((error as Error).message)
    }
  }

  const onSubmitDonation = async (event: FormEvent) => {
    event.preventDefault()
    if (!memberId) {
      setStatus('Select a member before recording a donation.')
      return
    }

    try {
      const response = await api.createDonation({
        memberId,
        donationAccountId: null,
        type,
        method,
        donationDate,
        amount: Number(amount),
        idempotencyKey: crypto.randomUUID(),
        enteredBy,
        serviceName,
        notes: notes.length > 0 ? notes : null,
      })
      setStatus(`Donation recorded: ${response.donationId}`)
      setAmount('0')
      setNotes('')
    } catch (error) {
      setStatus((error as Error).message)
    }
  }

  return (
    <div className="space-y-5">
      <h2 className="text-xl font-bold text-slate-900">Donation Desk</h2>

      <div className="grid gap-4 lg:grid-cols-2">
        <form className="space-y-3 rounded-xl border border-blue-100 p-4" onSubmit={onCreateMember}>
          <h3 className="font-semibold text-slate-900">Quick Create Member</h3>
          <div className="grid gap-2 sm:grid-cols-2">
            <input className="rounded-md border border-slate-300 px-3 py-2 text-sm" placeholder="First name" value={newMemberFirstName} onChange={(e) => setNewMemberFirstName(e.target.value)} />
            <input className="rounded-md border border-slate-300 px-3 py-2 text-sm" placeholder="Last name" value={newMemberLastName} onChange={(e) => setNewMemberLastName(e.target.value)} />
          </div>
          <button className="rounded-md bg-blue-600 px-3 py-2 text-sm font-semibold text-white hover:bg-blue-700" type="submit">Create Member</button>
        </form>

        <form className="space-y-3 rounded-xl border border-blue-100 p-4" onSubmit={onCreateFamily}>
          <h3 className="font-semibold text-slate-900">Quick Create Family</h3>
          <input className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" placeholder="Family name" value={newFamilyName} onChange={(e) => setNewFamilyName(e.target.value)} />
          <button className="rounded-md bg-blue-600 px-3 py-2 text-sm font-semibold text-white hover:bg-blue-700" type="submit">Create Family</button>
        </form>
      </div>

      <form className="space-y-3 rounded-xl border border-blue-100 p-4" onSubmit={onSubmitDonation}>
        <h3 className="font-semibold text-slate-900">Record Donation</h3>
        <div className="grid gap-3 md:grid-cols-2">
          <select className="rounded-md border border-slate-300 px-3 py-2 text-sm" value={memberId} onChange={(e) => setMemberId(e.target.value)}>
            <option value="">Select member</option>
            {members.map((member) => (
              <option key={member.id} value={member.id}>
                {member.firstName} {member.lastName}
              </option>
            ))}
          </select>
          <select className="rounded-md border border-slate-300 px-3 py-2 text-sm" value={familyId} onChange={(e) => setFamilyId(e.target.value)}>
            <option value="">Select family (optional)</option>
            {families.map((family) => (
              <option key={family.id} value={family.id}>{family.name}</option>
            ))}
          </select>
          <select className="rounded-md border border-slate-300 px-3 py-2 text-sm" value={type} onChange={(e) => setType(Number(e.target.value) as DonationType)}>
            {donationTypeOptions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
          </select>
          <select className="rounded-md border border-slate-300 px-3 py-2 text-sm" value={method} onChange={(e) => setMethod(Number(e.target.value) as DonationMethod)}>
            {donationMethodOptions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
          </select>
          <input className="rounded-md border border-slate-300 px-3 py-2 text-sm" type="date" value={donationDate} onChange={(e) => setDonationDate(e.target.value)} />
          <input className="rounded-md border border-slate-300 px-3 py-2 text-sm" type="number" min="0.01" step="0.01" value={amount} onChange={(e) => setAmount(e.target.value)} />
          <input className="rounded-md border border-slate-300 px-3 py-2 text-sm" placeholder="Service name" value={serviceName} onChange={(e) => setServiceName(e.target.value)} />
          <input className="rounded-md border border-slate-300 px-3 py-2 text-sm" placeholder="Entered by" value={enteredBy} onChange={(e) => setEnteredBy(e.target.value)} />
        </div>
        <textarea className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" placeholder="Notes (optional)" value={notes} onChange={(e) => setNotes(e.target.value)} />
        <div className="flex flex-wrap gap-2">
          <button className="rounded-md bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-700" type="submit">Record Donation</button>
          <button className="rounded-md border border-blue-300 px-4 py-2 text-sm font-semibold text-blue-800 hover:bg-blue-50" onClick={onLinkMemberToFamily} type="button">Link Member to Family</button>
          <button className="rounded-md border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50" onClick={() => void loadLookups()} type="button">Refresh Lookups</button>
        </div>
      </form>

      {status && (
        <p className="rounded-md border border-blue-200 bg-blue-50 px-3 py-2 text-sm text-blue-900">{status}</p>
      )}
    </div>
  )
}

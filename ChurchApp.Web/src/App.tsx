import { NavLink, Navigate, Route, Routes } from 'react-router-dom'
import { DonationEntryPage } from './pages/DonationEntryPage'
import { LedgerPage } from './pages/LedgerPage'
import { SummariesPage } from './pages/SummariesPage'
import { ReportsPage } from './pages/ReportsPage'

const navLinkClass = ({ isActive }: { isActive: boolean }) =>
  `rounded-lg px-3 py-2 text-sm font-semibold transition ${
    isActive ? 'bg-blue-600 text-white shadow-sm' : 'text-blue-900 hover:bg-blue-100'
  }`

function App() {
  return (
    <main className="mx-auto flex min-h-screen w-full max-w-6xl flex-col p-4 sm:p-6">
      <header className="mb-6 rounded-2xl border border-blue-200 bg-white/90 p-5 shadow-sm backdrop-blur">
        <p className="text-sm font-semibold tracking-wide text-blue-700">ChurchApp</p>
        <h1 className="mt-1 text-2xl font-bold text-slate-900 sm:text-3xl">Volunteer Donations Desk</h1>
        <p className="mt-2 text-sm text-slate-600">Simple workflow for recording donations and generating summaries.</p>
      </header>

      <nav className="mb-6 flex flex-wrap gap-2 rounded-xl border border-blue-100 bg-white p-2 shadow-sm">
        <NavLink to="/desk" className={navLinkClass}>Donation Desk</NavLink>
        <NavLink to="/ledger" className={navLinkClass}>Ledger</NavLink>
        <NavLink to="/summaries" className={navLinkClass}>Summaries</NavLink>
        <NavLink to="/reports" className={navLinkClass}>Reports</NavLink>
      </nav>

      <div className="rounded-2xl border border-blue-100 bg-white p-4 shadow-sm sm:p-6">
        <Routes>
          <Route path="/" element={<Navigate to="/desk" replace />} />
          <Route path="/desk" element={<DonationEntryPage />} />
          <Route path="/ledger" element={<LedgerPage />} />
          <Route path="/summaries" element={<SummariesPage />} />
          <Route path="/reports" element={<ReportsPage />} />
        </Routes>
      </div>
    </main>
  )
}

export default App

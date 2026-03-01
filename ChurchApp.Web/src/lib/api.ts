import type {
  CreateDonationRequest,
  CreateDonationResponse,
  DonationLedgerResponse,
  FamiliesResponse,
  MembersResponse,
  SummariesResponse,
  TimeRangeReportResponse,
  VoidDonationRequest,
} from './contracts'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5121'

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(init?.headers ?? {}),
    },
  })

  if (!response.ok) {
    const body = await response.text()
    throw new Error(body || `Request failed with status ${response.status}`)
  }

  return (await response.json()) as T
}

function withQuery(path: string, query: Record<string, string | number | boolean | undefined | null>): string {
  const searchParams = new URLSearchParams()
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== null && value !== '') {
      searchParams.set(key, String(value))
    }
  }

  const queryString = searchParams.toString()
  return queryString.length > 0 ? `${path}?${queryString}` : path
}

export const api = {
  createDonation: (requestBody: CreateDonationRequest) =>
    request<CreateDonationResponse>('/api/donations', {
      method: 'POST',
      body: JSON.stringify(requestBody),
    }),

  getDonations: (query: {
    page?: number
    pageSize?: number
    startDate?: string
    endDate?: string
    memberId?: string
    familyId?: string
    type?: number
    method?: number
    includeVoided?: boolean
  }) => request<DonationLedgerResponse>(withQuery('/api/donations', query)),

  voidDonation: (donationId: string, requestBody: VoidDonationRequest) =>
    request(`/api/donations/${donationId}/void`, {
      method: 'POST',
      body: JSON.stringify(requestBody),
    }),

  getMembers: (query: { search?: string; page?: number; pageSize?: number }) =>
    request<MembersResponse>(withQuery('/api/members', query)),

  createMember: (requestBody: { firstName: string; lastName: string; email?: string; phoneNumber?: string }) =>
    request<{ memberId: string }>('/api/members', {
      method: 'POST',
      body: JSON.stringify(requestBody),
    }),

  getFamilies: (query: { search?: string; page?: number; pageSize?: number }) =>
    request<FamiliesResponse>(withQuery('/api/families', query)),

  createFamily: (requestBody: { name: string }) =>
    request<{ familyId: string }>('/api/families', {
      method: 'POST',
      body: JSON.stringify(requestBody),
    }),

  addFamilyMember: (familyId: string, requestBody: { memberId: string }) =>
    request(`/api/families/${familyId}/members`, {
      method: 'POST',
      body: JSON.stringify(requestBody),
    }),

  getServiceSummaries: (query: { serviceName: string; startDate: string; endDate: string; persistReport?: boolean }) =>
    request<SummariesResponse>(withQuery('/api/summaries/service', query)),

  getMemberSummaries: (query: { memberId: string; periodType?: number; startDate?: string; endDate?: string; persistReport?: boolean }) =>
    request<SummariesResponse>(withQuery('/api/summaries/member', query)),

  getFamilySummaries: (query: { familyId: string; periodType?: number; startDate?: string; endDate?: string; persistReport?: boolean }) =>
    request<SummariesResponse>(withQuery('/api/summaries/family', query)),

  getTimeRangeReport: (query: { startDate: string; endDate: string; persistReport?: boolean }) =>
    request<TimeRangeReportResponse>(withQuery('/api/reports/time-range', query)),
}

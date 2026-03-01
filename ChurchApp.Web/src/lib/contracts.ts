export type Guid = string

export const DonationType = {
  GeneralOffering: 1,
  Tithe: 2,
  BuildingFund: 3,
} as const
export type DonationType = (typeof DonationType)[keyof typeof DonationType]

export const DonationMethod = {
  Cash: 1,
  CashApp: 2,
  Zelle: 3,
  Check: 4,
  Card: 5,
  Other: 6,
} as const
export type DonationMethod = (typeof DonationMethod)[keyof typeof DonationMethod]

export const DonationStatus = {
  Active: 1,
  Voided: 2,
} as const
export type DonationStatus = (typeof DonationStatus)[keyof typeof DonationStatus]

export const SummaryPeriodType = {
  Day: 1,
  Month: 2,
  Quarter: 3,
  Year: 4,
  CustomRange: 5,
} as const
export type SummaryPeriodType = (typeof SummaryPeriodType)[keyof typeof SummaryPeriodType]

export interface CreateDonationRequest {
  memberId: Guid
  donationAccountId: Guid | null
  type: DonationType
  method: DonationMethod
  donationDate: string
  amount: number
  idempotencyKey: string | null
  enteredBy: string | null
  serviceName: string | null
  notes: string | null
}

export interface CreateDonationResponse {
  donationId: Guid
  version: number
  isDuplicate: boolean
}

export interface DonationLedgerItem {
  id: Guid
  memberId: Guid
  donationAccountId: Guid | null
  type: DonationType
  method: DonationMethod
  donationDate: string
  amount: number
  status: DonationStatus
  serviceName: string | null
  notes: string | null
  createdAtUtc: string
  createdBy: string
  voidedAtUtc: string | null
  voidedBy: string | null
  voidReason: string | null
  version: number
}

export interface DonationLedgerResponse {
  page: number
  pageSize: number
  totalCount: number
  donations: DonationLedgerItem[]
}

export interface VoidDonationRequest {
  reason: string
  enteredBy: string | null
  expectedVersion: number
}

export interface SummaryItem {
  id: Guid
  type: number
  periodType: SummaryPeriodType
  startDate: string
  endDate: string
  serviceName: string | null
  memberId: Guid | null
  familyId: Guid | null
  totalAmount: number
  donationCount: number
  generatedAtUtc: string
}

export interface SummariesResponse {
  summaries: SummaryItem[]
}

export interface DonationTypeBreakdown {
  type: DonationType
  totalAmount: number
  donationCount: number
}

export interface TimeRangeReportResponse {
  startDate: string
  endDate: string
  totalAmount: number
  donationCount: number
  breakdown: DonationTypeBreakdown[]
}

export interface Member {
  id: Guid
  firstName: string
  lastName: string
  email: string | null
  phoneNumber: string | null
}

export interface MembersResponse {
  page: number
  pageSize: number
  totalCount: number
  members: Member[]
}

export interface Family {
  id: Guid
  name: string
  memberCount: number
}

export interface FamiliesResponse {
  page: number
  pageSize: number
  totalCount: number
  families: Family[]
}

export interface AuthUser {
  id: string
  email: string
  displayName: string
  isEnabled: boolean
  linkedEmployeeId?: string | null
  linkedEmployeeCode: string
  hasLinkedEmployee: boolean
  isManager: boolean
  managedEmployeeCount: number
  roles: string[]
}

export interface AuthResponse {
  accessToken: string
  tokenType: string
  accessTokenExpiresAtUtc: string
  user: AuthUser
}

export interface ApiErrorPayload {
  title: string
  status: number
  detail: string
  traceId: string
  errors?: Record<string, string[]>
}

export interface ProvidentFundOptions {
  employees: EmployeeAttendanceOption[]
  departments: LookupOption[]
  policies: ProvidentFundPolicyOption[]
  contributionTypes: string[]
  policyStatuses: string[]
  enrollmentStatuses: string[]
  batchStatuses: string[]
  ledgerTransactionTypes: string[]
  withdrawalStatuses: string[]
  withdrawalTypes: string[]
  adjustmentStatuses: string[]
  adjustmentTypes: string[]
  shareTypes: string[]
  permissions: string[]
}

export interface ProvidentFundDashboard {
  totalFundValue: number
  totalEmployeeContributions: number
  totalEmployerContributions: number
  pendingWithdrawalRequestCount: number
  currentMonthContributionStatus: string
  employeesEnrolled: number
  employeesNotEnrolled: number
  totalWithdrawalsThisMonth: number
  fundBalanceTrend: { period: string; balance: number }[]
}

export interface ProvidentFundPolicyOption {
  id: string
  policyName: string
  status: string
  allowVoluntaryContribution: boolean
  allowWithdrawal: boolean
}

export interface ProvidentFundPolicy {
  id: string
  policyName: string
  description: string
  eligibilityRules: string
  employeeContributionType: string
  employeeContributionValue: number
  employerContributionType: string
  employerContributionValue: number
  contributionFrequency: string
  effectiveDate: string
  status: string
  allowVoluntaryContribution: boolean
  allowWithdrawal: boolean
  allowLoan: boolean
  remarks: string
  vestingRuleCount: number
  activeEnrollmentCount: number
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface ProvidentFundPolicyInput {
  policyName: string
  description: string
  eligibilityRules: string
  employeeContributionType: string
  employeeContributionValue: number
  employerContributionType: string
  employerContributionValue: number
  contributionFrequency: string
  effectiveDate: string
  status: string
  allowVoluntaryContribution: boolean
  allowWithdrawal: boolean
  allowLoan: boolean
  remarks: string
}

export interface ProvidentFundVestingRule {
  id: string
  policyId: string
  policyName: string
  yearsOfService: number
  vestedPercentage: number
  remarks: string
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface ProvidentFundVestingRuleInput {
  policyId: string
  yearsOfService: number
  vestedPercentage: number
  remarks: string
}

export interface ProvidentFundEnrollment {
  id: string
  employeeId: string
  employeeCode: string
  employeeFullName: string
  departmentName: string
  policyId: string
  policyName: string
  enrollmentDate: string
  vestingStartDate: string
  employeeContributionOverrideType: string
  employeeContributionOverrideValue?: number | null
  employerContributionOverrideType: string
  employerContributionOverrideValue?: number | null
  status: string
  remarks: string
  grossBalance: number
  withdrawableBalance: number
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface ProvidentFundEnrollmentInput {
  employeeId: string
  policyId: string
  enrollmentDate: string
  vestingStartDate: string
  employeeContributionOverrideType: string
  employeeContributionOverrideValue?: number | null
  employerContributionOverrideType: string
  employerContributionOverrideValue?: number | null
  status: string
  remarks: string
}

export interface ProvidentFundContributionBatch {
  id: string
  batchNumber: string
  month: number
  year: number
  policyId?: string | null
  policyName: string
  isSupplemental: boolean
  status: string
  lineCount: number
  totalEmployeeContribution: number
  totalEmployerContribution: number
  totalVoluntaryContribution: number
  totalContribution: number
  createdByDisplayName: string
  reviewedByDisplayName: string
  postedByDisplayName: string
  postingDate?: string | null
  remarks: string
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface ProvidentFundContributionBatchLine {
  id: string
  batchId: string
  employeeId: string
  employeeCode: string
  employeeFullName: string
  departmentName: string
  enrollmentId: string
  basicSalary: number
  employeeContribution: number
  employerContribution: number
  voluntaryContribution: number
  totalContribution: number
  status: string
  remarks: string
}

export interface ProvidentFundContributionBatchDetail {
  batch: ProvidentFundContributionBatch
  lines: ProvidentFundContributionBatchLine[]
}

export interface GenerateProvidentFundContributionBatchInput {
  month: number
  year: number
  policyId?: string | null
  isSupplemental: boolean
  batchNumber: string
  remarks: string
  manualLines: {
    employeeId: string
    basicSalary?: number | null
    employeeContribution?: number | null
    employerContribution?: number | null
    voluntaryContribution?: number | null
  }[]
}

export interface ProvidentFundLedgerTransaction {
  id: string
  transactionNumber: string
  employeeId: string
  employeeCode: string
  employeeFullName: string
  departmentName: string
  enrollmentId: string
  policyId: string
  policyName: string
  transactionDate: string
  transactionType: string
  sourceType: string
  sourceReferenceId: string
  employeeShareAmount: number
  employerShareAmount: number
  voluntaryShareAmount: number
  interestAmount: number
  debitAmount: number
  creditAmount: number
  runningBalance?: number | null
  remarks: string
  createdByDisplayName: string
  createdAtUtc: string
  isReversed: boolean
  reversalReferenceId?: string | null
}

export interface ProvidentFundBalance {
  employeeId: string
  employeeCode: string
  employeeFullName: string
  departmentName: string
  enrollmentId?: string | null
  enrollmentStatus: string
  policyId?: string | null
  policyName: string
  enrollmentDate?: string | null
  vestingStartDate?: string | null
  vestingPercentage: number
  totalEmployeeContribution: number
  totalEmployerContribution: number
  totalVoluntaryContribution: number
  totalInterest: number
  totalWithdrawals: number
  totalAdjustments: number
  grossFundBalance: number
  vestedEmployerBalance: number
  nonVestedEmployerBalance: number
  withdrawableBalance: number
  latestTransactionDate?: string | null
}

export interface ProvidentFundWithdrawalRequest {
  id: string
  requestNumber: string
  employeeId: string
  employeeCode: string
  employeeFullName: string
  departmentName: string
  enrollmentId: string
  requestDate: string
  withdrawalType: string
  requestedAmount: number
  eligibleWithdrawableAmount: number
  approvedAmount: number
  reason: string
  attachmentPath: string
  status: string
  paymentDate?: string | null
  remarks: string
  approvals: ProvidentFundApprovalHistory[]
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface ProvidentFundApprovalHistory {
  id: string
  stepName: string
  action: string
  actorName: string
  remarks: string
  createdAtUtc: string
}

export interface ProvidentFundWithdrawalInput {
  employeeId?: string | null
  enrollmentId?: string | null
  requestDate: string
  withdrawalType: string
  requestedAmount: number
  reason: string
  attachmentPath: string
  remarks: string
}

export interface ProvidentFundAdjustment {
  id: string
  employeeId: string
  employeeCode: string
  employeeFullName: string
  departmentName: string
  enrollmentId: string
  adjustmentType: string
  adjustmentDate: string
  amount: number
  shareAffected: string
  reason: string
  attachmentPath: string
  status: string
  requestedByDisplayName: string
  approvedByDisplayName: string
  approvedAtUtc?: string | null
  postedAtUtc?: string | null
  decisionRemarks: string
  approvals: ProvidentFundApprovalHistory[]
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface ProvidentFundAdjustmentInput {
  employeeId: string
  enrollmentId?: string | null
  adjustmentType: string
  adjustmentDate: string
  amount: number
  shareAffected: string
  reason: string
  attachmentPath: string
}

export interface ProvidentFundContributionReportRow {
  employeeNumber: string
  employeeName: string
  department: string
  basicSalary: number
  employeeContribution: number
  employerContribution: number
  voluntaryContribution: number
  totalContribution: number
  batchStatus: string
}

export interface ProvidentFundBalanceReportRow {
  employeeNumber: string
  employeeName: string
  totalEmployeeShare: number
  totalEmployerShare: number
  vestedEmployerShare: number
  nonVestedEmployerShare: number
  interest: number
  withdrawals: number
  currentBalance: number
  withdrawableBalance: number
}

export interface ProvidentFundWithdrawalReportRow {
  requestNumber: string
  employee: string
  requestDate: string
  withdrawalType: string
  requestedAmount: number
  approvedAmount: number
  status: string
  paymentDate?: string | null
}

export interface ProvidentFundListQuery {
  search?: string
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  descending?: boolean
  status?: string
  policyId?: string
  employeeId?: string
  departmentId?: string
  transactionType?: string
  withdrawalType?: string
  adjustmentType?: string
  shareAffected?: string
  month?: number | string
  year?: number | string
  dateFrom?: string
  dateTo?: string
  asOfDate?: string
  employmentStatus?: string
}

export interface PagedResult<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface LookupOption {
  id: string
  code: string
  name: string
  parentId?: string | null
  isActive: boolean
}

export interface UserOption {
  id: string
  email: string
  displayName: string
  isEnabled: boolean
}

export interface UserSummary {
  id: string
  email: string
  displayName: string
  isEnabled: boolean
  createdAtUtc: string
  roles: string[]
}

export interface CreateUserInput {
  email: string
  displayName: string
  password: string
  isEnabled: boolean
  roleNames: string[]
}

export interface UpdateUserInput {
  email: string
  displayName: string
}

export interface SetUserStatusInput {
  isEnabled: boolean
}

export interface SetUserRolesInput {
  roleNames: string[]
}

export interface SetPasswordInput {
  newPassword: string
}

export interface Role {
  id: string
  name: string
  description: string
  userCount: number
}

export interface CreateRoleInput {
  name: string
  description: string
}

export interface UpdateRoleInput {
  name: string
  description: string
}

export interface RbacUser {
  id: string
  email: string
  displayName: string
  isEnabled: boolean
  roles: string[]
}

export interface RbacAssignment {
  userId: string
  userEmail: string
  roleId: string
  roleName: string
}

export interface RbacSummary {
  users: RbacUser[]
  roles: Role[]
  assignments: RbacAssignment[]
}

export interface OrganizationSummary {
  departmentCount: number
  activeDepartmentCount: number
  positionCount: number
  activePositionCount: number
  branchCount: number
  activeBranchCount: number
  employmentTypeCount: number
  activeEmploymentTypeCount: number
  employmentStatusCount: number
  activeEmploymentStatusCount: number
  employeeCount: number
  activeEmployeeCount: number
}

export interface OrganizationRecord {
  id: string
  code: string
  name: string
  description: string
  address: string
  departmentId?: string | null
  departmentName: string
  isActive: boolean
  employeeCount: number
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface OrganizationOptions {
  departments: LookupOption[]
  positions: LookupOption[]
  branches: LookupOption[]
  employmentTypes: LookupOption[]
  employmentStatuses: LookupOption[]
}

export interface OrganizationListQuery {
  search?: string
  isActive?: boolean | null
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  descending?: boolean
}

export interface OrganizationRecordInput {
  code: string
  name: string
  description: string
  isActive: boolean
  address?: string
  departmentId?: string | null
}

export type OrganizationResource =
  | 'departments'
  | 'positions'
  | 'branches'
  | 'employment-types'
  | 'employment-statuses'

export interface EmployeeSummary {
  id: string
  employeeCode: string
  fullName: string
  email: string
  mobileNumber: string
  departmentName: string
  positionName: string
  branchName: string
  employmentTypeName: string
  employmentStatusName: string
  managerName: string
  dateHired?: string | null
  isActive: boolean
}

export interface EmployeeDetail {
  id: string
  employeeCode: string
  firstName: string
  middleName: string
  lastName: string
  suffix: string
  fullName: string
  gender: string
  birthDate?: string | null
  civilStatus: string
  nationality: string
  mobileNumber: string
  email: string
  address: string
  cityProvince: string
  postalCode: string
  emergencyContactName: string
  emergencyContactRelationship: string
  emergencyContactPhone: string
  departmentId?: string | null
  departmentName: string
  departmentIsActive: boolean
  positionId?: string | null
  positionName: string
  positionIsActive: boolean
  branchId?: string | null
  branchName: string
  branchIsActive: boolean
  employmentTypeId?: string | null
  employmentTypeName: string
  employmentTypeIsActive: boolean
  employmentStatusId?: string | null
  employmentStatusName: string
  employmentStatusIsActive: boolean
  managerId?: string | null
  managerName: string
  workSchedule: string
  dateHired?: string | null
  dateRegularized?: string | null
  dateSeparated?: string | null
  sssNumber: string
  philHealthNumber: string
  pagIbigNumber: string
  tinNumber: string
  otherGovernmentId: string
  userId: string
  linkedUserEmail: string
  linkedUserDisplayName: string
  isActive: boolean
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface EmployeeManagerOption {
  id: string
  employeeCode: string
  fullName: string
  isActive: boolean
}

export interface EmployeeEditorOptions {
  departments: LookupOption[]
  positions: LookupOption[]
  branches: LookupOption[]
  employmentTypes: LookupOption[]
  employmentStatuses: LookupOption[]
  managers: EmployeeManagerOption[]
  userAccounts: UserOption[]
}

export interface EmployeeListQuery {
  search?: string
  departmentId?: string
  positionId?: string
  branchId?: string
  employmentTypeId?: string
  employmentStatusId?: string
  isActive?: boolean | null
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  descending?: boolean
}

export interface SaveEmployeeInput {
  employeeCode: string
  firstName: string
  middleName: string
  lastName: string
  suffix: string
  gender: string
  birthDate?: string | null
  civilStatus: string
  nationality: string
  mobileNumber: string
  email: string
  address: string
  cityProvince: string
  postalCode: string
  emergencyContactName: string
  emergencyContactRelationship: string
  emergencyContactPhone: string
  departmentId: string | null
  positionId: string | null
  branchId: string | null
  employmentTypeId: string | null
  employmentStatusId: string | null
  managerId?: string | null
  workSchedule: string
  dateHired?: string | null
  dateRegularized?: string | null
  dateSeparated?: string | null
  sssNumber: string
  philHealthNumber: string
  pagIbigNumber: string
  tinNumber: string
  otherGovernmentId: string
  userId?: string | null
  isActive: boolean
}

export interface DocumentType {
  id: string
  code: string
  name: string
  description: string
  requiresExpiryDate: boolean
  isRequired: boolean
  isActive: boolean
  documentCount: number
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface DocumentTypeOption {
  id: string
  code: string
  name: string
  requiresExpiryDate: boolean
  isRequired: boolean
  isActive: boolean
}

export interface DocumentTypeListQuery {
  search?: string
  isActive?: boolean | null
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  descending?: boolean
}

export interface DocumentTypeInput {
  code: string
  name: string
  description: string
  requiresExpiryDate: boolean
  isRequired: boolean
  isActive: boolean
}

export interface DocumentComplianceSummary {
  totalDocuments: number
  archivedDocuments: number
  expiredDocuments: number
  expiringSoonDocuments: number
  missingRequiredDocuments: number
  employeesWithIncompleteDocuments: number
  employeesWithExpiringDocuments: number
  requiredDocumentTypes: number
}

export interface EmployeeDocumentComplianceSummary {
  totalDocuments: number
  activeDocuments: number
  archivedDocuments: number
  missingRequiredDocuments: number
  expiredDocuments: number
  expiringSoonDocuments: number
  requiredDocumentTypes: number
  submittedRequiredDocumentTypes: number
  hasIssues: boolean
}

export interface MissingRequiredDocument {
  documentTypeId: string
  code: string
  name: string
  requiresExpiryDate: boolean
}

export type EmployeeDocumentStatusCode =
  | 'valid'
  | 'expiring-soon'
  | 'expired'
  | 'no-expiry'
  | 'archived'

export interface EmployeeDocument {
  id: string
  employeeId: string
  employeeCode: string
  employeeFullName: string
  departmentName: string
  branchName: string
  documentTypeId: string
  documentTypeCode: string
  documentTypeName: string
  documentTypeIsActive: boolean
  documentTypeRequiresExpiryDate: boolean
  documentTypeIsRequired: boolean
  title: string
  originalFileName: string
  fileSize: number
  mimeType: string
  issueDate?: string | null
  expiryDate?: string | null
  remarks: string
  uploadedByDisplayName: string
  uploadedByEmail: string
  isArchived: boolean
  statusCode: EmployeeDocumentStatusCode
  statusLabel: string
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface EmployeeDocumentProfile {
  employeeId: string
  employeeCode: string
  employeeFullName: string
  summary: EmployeeDocumentComplianceSummary
  availableDocumentTypes: DocumentTypeOption[]
  missingRequiredDocuments: MissingRequiredDocument[]
  documents: EmployeeDocument[]
}

export interface EmployeeDocumentListOptions {
  documentTypes: DocumentTypeOption[]
  departments: LookupOption[]
  branches: LookupOption[]
}

export interface EmployeeDocumentListQuery {
  search?: string
  employeeId?: string
  departmentId?: string
  branchId?: string
  documentTypeId?: string
  status?: string
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  descending?: boolean
}

export interface SaveEmployeeDocumentMetadataInput {
  documentTypeId: string | null
  title: string
  issueDate?: string | null
  expiryDate?: string | null
  remarks: string
}

export interface SetEmployeeDocumentArchiveStateInput {
  isArchived: boolean
}

export interface AttendanceSetupSummary {
  workScheduleCount: number
  activeWorkScheduleCount: number
  shiftCount: number
  activeShiftCount: number
  scheduleAssignmentCount: number
  activeScheduleAssignmentCount: number
}

export interface AttendanceTrendPoint {
  date: string
  presentCount: number
  lateCount: number
  absentCount: number
  incompleteCount: number
}

export interface AttendanceDashboardSummary {
  attendanceDate: string
  presentCount: number
  lateCount: number
  absentCount: number
  incompleteCount: number
  restDayCount: number
  noScheduleCount: number
  undertimeCount: number
  pendingAdjustmentRequestCount: number
  employeesWithoutScheduleAssignmentCount: number
  trend: AttendanceTrendPoint[]
}

export interface EmployeeAttendanceOption {
  id: string
  employeeCode: string
  fullName: string
  departmentName: string
  branchName: string
  isActive: boolean
}

export interface WorkScheduleOption {
  id: string
  code: string
  name: string
  scheduleType: string
  isActive: boolean
}

export interface ShiftOption {
  id: string
  code: string
  name: string
  startTime: string
  endTime: string
  isOvernight: boolean
  isActive: boolean
}

export interface AttendanceListOptions {
  employees: EmployeeAttendanceOption[]
  departments: LookupOption[]
  branches: LookupOption[]
  workSchedules: WorkScheduleOption[]
  shifts: ShiftOption[]
  statuses: string[]
  sources: string[]
}

export interface WorkScheduleRecord {
  id: string
  code: string
  name: string
  description: string
  scheduleType: string
  requiredWorkingMinutes: number
  gracePeriodMinutes: number
  breakDurationMinutes: number
  isActive: boolean
  assignmentCount: number
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface ShiftRecord {
  id: string
  code: string
  name: string
  startTime: string
  endTime: string
  breakStartTime?: string | null
  breakEndTime?: string | null
  requiredWorkingMinutes: number
  gracePeriodMinutes: number
  isOvernight: boolean
  isActive: boolean
  assignmentCount: number
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface EmployeeScheduleAssignmentRecord {
  id: string
  employeeId: string
  employeeCode: string
  employeeFullName: string
  departmentName: string
  branchName: string
  workScheduleId: string
  workScheduleCode: string
  workScheduleName: string
  workScheduleType: string
  workScheduleIsActive: boolean
  shiftId?: string | null
  shiftCode: string
  shiftName: string
  shiftIsActive: boolean
  effectiveStartDate: string
  effectiveEndDate?: string | null
  restDayValues: number[]
  restDayLabels: string[]
  isActive: boolean
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface AttendanceRecordListItem {
  attendanceRecordId?: string | null
  employeeId: string
  employeeCode: string
  employeeFullName: string
  departmentName: string
  branchName: string
  attendanceDate: string
  workScheduleName: string
  shiftName: string
  scheduledStartTime?: string | null
  scheduledEndTime?: string | null
  actualTimeIn?: string | null
  actualTimeOut?: string | null
  breakStartTime?: string | null
  breakEndTime?: string | null
  totalWorkedMinutes: number
  lateMinutes: number
  undertimeMinutes: number
  overtimeMinutes: number
  status: string
  source: string
  remarks: string
  hasScheduleAssignment: boolean
  hasBackingRecord: boolean
  createdByDisplayName: string
  updatedByDisplayName: string
  createdAtUtc?: string | null
  updatedAtUtc?: string | null
}

export interface WorkScheduleListQuery {
  search?: string
  isActive?: boolean | null
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  descending?: boolean
}

export interface ShiftListQuery {
  search?: string
  isActive?: boolean | null
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  descending?: boolean
}

export interface EmployeeScheduleAssignmentListQuery {
  search?: string
  employeeId?: string
  departmentId?: string
  branchId?: string
  isActive?: boolean | null
  date?: string
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  descending?: boolean
}

export interface AttendanceRecordListQuery {
  search?: string
  employeeId?: string
  departmentId?: string
  branchId?: string
  dateFrom?: string
  dateTo?: string
  status?: string
  source?: string
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  descending?: boolean
}

export interface SaveWorkScheduleInput {
  code: string
  name: string
  description: string
  scheduleType: string
  requiredWorkingMinutes: number
  gracePeriodMinutes: number
  breakDurationMinutes: number
  isActive: boolean
}

export interface SaveShiftInput {
  code: string
  name: string
  startTime: string
  endTime: string
  breakStartTime?: string | null
  breakEndTime?: string | null
  requiredWorkingMinutes: number
  gracePeriodMinutes: number
  isOvernight: boolean
  isActive: boolean
}

export interface SaveEmployeeScheduleAssignmentInput {
  employeeId: string | null
  workScheduleId: string | null
  shiftId?: string | null
  effectiveStartDate: string | null
  effectiveEndDate?: string | null
  restDayValues: number[]
  isActive: boolean
}

export interface SaveAttendanceRecordInput {
  employeeId: string | null
  attendanceDate: string | null
  actualTimeIn?: string | null
  actualTimeOut?: string | null
  breakStartTime?: string | null
  breakEndTime?: string | null
  source: string
  remarks: string
}

export interface LeaveDashboardSummary {
  businessDate: string
  pendingLeaveRequestCount: number
  approvedLeavesTodayCount: number
  employeesOnLeaveTodayCount: number
  lowBalanceCount: number
  negativeBalanceCount: number
  upcomingApprovedLeaveCount: number
  attendanceConflictCount: number
}

export interface LeaveTypeOption {
  id: string
  code: string
  name: string
  allowHalfDay: boolean
  requiresAttachment: boolean
  requiresReason: boolean
  allowNegativeBalance: boolean
  defaultAnnualCredits?: number | null
  isActive: boolean
}

export interface LeaveType {
  id: string
  code: string
  name: string
  description: string
  isPaid: boolean
  requiresAttachment: boolean
  requiresReason: boolean
  allowHalfDay: boolean
  allowNegativeBalance: boolean
  defaultAnnualCredits?: number | null
  maxDaysPerRequest?: number | null
  minDaysBeforeFiling?: number | null
  genderRestriction: string
  employmentTypeRestrictionIds: string[]
  countsRestDays: boolean
  countsHolidays: boolean
  allowDuringProbationaryPeriod: boolean
  isActive: boolean
  employeeCount: number
  pendingRequestCount: number
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface LeaveTypeListQuery {
  search?: string
  isActive?: boolean | null
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  descending?: boolean
}

export interface LeaveTypeInput {
  code: string
  name: string
  description: string
  isPaid: boolean
  requiresAttachment: boolean
  requiresReason: boolean
  allowHalfDay: boolean
  allowNegativeBalance: boolean
  defaultAnnualCredits?: number | null
  maxDaysPerRequest?: number | null
  minDaysBeforeFiling?: number | null
  genderRestriction: string
  employmentTypeRestrictionIds: string[]
  countsRestDays: boolean
  countsHolidays: boolean
  allowDuringProbationaryPeriod: boolean
  isActive: boolean
}

export interface LeaveManagementOptions {
  employees: EmployeeAttendanceOption[]
  departments: LookupOption[]
  branches: LookupOption[]
  employmentTypes: LookupOption[]
  leaveTypes: LeaveTypeOption[]
  statuses: string[]
  periodYears: number[]
}

export interface LeaveBalance {
  id: string
  employeeId: string
  employeeCode: string
  employeeFullName: string
  departmentName: string
  branchName: string
  leaveTypeId: string
  leaveTypeCode: string
  leaveTypeName: string
  leaveTypeIsPaid: boolean
  periodYear: number
  openingBalance: number
  accrued: number
  used: number
  pending: number
  adjusted: number
  carriedForward: number
  availableBalance: number
  isLowBalance: boolean
  isNegativeBalance: boolean
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface LeaveBalanceTransaction {
  id: string
  employeeId: string
  leaveTypeId: string
  periodYear: number
  leaveRequestId?: string | null
  transactionType: string
  amount: number
  balanceBefore: number
  balanceAfter: number
  remarks: string
  createdByDisplayName: string
  createdAtUtc: string
}

export interface LeaveBalanceListQuery {
  search?: string
  employeeId?: string
  departmentId?: string
  branchId?: string
  leaveTypeId?: string
  periodYear?: number
  lowBalanceOnly?: boolean | null
  negativeBalanceOnly?: boolean | null
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  descending?: boolean
}

export interface LeaveRequestListQuery {
  search?: string
  employeeId?: string
  departmentId?: string
  branchId?: string
  leaveTypeId?: string
  status?: string
  approverId?: string
  dateFrom?: string
  dateTo?: string
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  descending?: boolean
}

export interface LeaveRequest {
  id: string
  employeeId: string
  employeeCode: string
  employeeFullName: string
  departmentName: string
  branchName: string
  leaveTypeId: string
  leaveTypeCode: string
  leaveTypeName: string
  leaveTypeIsPaid: boolean
  startDate: string
  endDate: string
  startDayType: string
  endDayType: string
  totalLeaveDays: number
  reason: string
  status: string
  submittedAtUtc?: string | null
  approvedAtUtc?: string | null
  rejectedAtUtc?: string | null
  cancelledAtUtc?: string | null
  currentApproverDisplayName: string
  decisionRemarks: string
  hasAttachment: boolean
  attachmentOriginalFileName: string
  attachmentFileSize?: number | null
  hasAttendanceConflict: boolean
  attendanceConflictCount: number
  availableBalanceAfterApproval: number
  createdByDisplayName: string
  updatedByDisplayName: string
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface LeaveCalendarQuery {
  year: number
  month: number
  departmentId?: string
  branchId?: string
  employeeId?: string
  leaveTypeId?: string
  status?: string
}

export interface LeaveCalendarEntry {
  id: string
  employeeId: string
  employeeCode: string
  employeeFullName: string
  departmentName: string
  branchName: string
  leaveTypeName: string
  leaveTypeIsPaid: boolean
  startDate: string
  endDate: string
  totalLeaveDays: number
  status: string
}

export interface LeaveCalendarResponse {
  year: number
  month: number
  entries: LeaveCalendarEntry[]
}

export interface LeaveActionInput {
  remarks: string
}

export interface LeaveBalanceAdjustmentInput {
  employeeId: string | null
  leaveTypeId: string | null
  periodYear: number | null
  amount: number
  remarks: string
  effectiveDate?: string | null
}

export interface EmployeeLeaveProfileSummary {
  pendingRequestCount: number
  approvedRequestCount: number
  rejectedOrCancelledRequestCount: number
  lowBalanceCount: number
  negativeBalanceCount: number
}

export interface EmployeeLeaveProfile {
  employeeId: string
  employeeCode: string
  employeeFullName: string
  summary: EmployeeLeaveProfileSummary
  balances: LeaveBalance[]
  pendingRequests: LeaveRequest[]
  history: LeaveRequest[]
  ledger: LeaveBalanceTransaction[]
}

export interface PayrollDashboardSummary {
  businessDate: string
  currentOpenPayPeriod?: PayPeriodOption | null
  draftRunCount: number
  forReviewRunCount: number
  approvedRunCount: number
  employeesMissingCompensationProfileCount: number
  employeesWithAttendanceIssuesCount: number
  pendingPayrollAdjustmentCount: number
  payrollItemsOnHoldCount: number
  totalGrossPay: number
  totalDeductions: number
  totalNetPay: number
  recentRuns: PayrollRunSummary[]
}

export interface PayrollOptions {
  employees: EmployeeAttendanceOption[]
  departments: LookupOption[]
  branches: LookupOption[]
  employmentTypes: LookupOption[]
  employmentStatuses: LookupOption[]
  payPeriods: PayPeriodOption[]
  payPeriodTemplates: PayPeriodTemplateOption[]
  earningTypes: EarningTypeOption[]
  deductionTypes: DeductionTypeOption[]
  contributionTypes: ContributionTypeOption[]
  taxTables: TaxTableOption[]
  payTypes: string[]
  payFrequencies: string[]
  runStatuses: string[]
  adjustmentStatuses: string[]
  adjustmentTypes: string[]
}

export interface PayrollSetupSummary {
  payPeriodTemplateCount: number
  activePayPeriodTemplateCount: number
  earningTypeCount: number
  activeEarningTypeCount: number
  deductionTypeCount: number
  activeDeductionTypeCount: number
  contributionTypeCount: number
  activeContributionTypeCount: number
  governmentContributionTableCount: number
  activeGovernmentContributionTableCount: number
  taxTableCount: number
  activeTaxTableCount: number
}

export interface PayrollSettings {
  defaultPayFrequency: string
  defaultWorkingDaysPerMonth: number
  defaultWorkingHoursPerDay: number
  lateUndertimeDeductionPolicy: string
  absenceDeductionPolicy: string
  overtimeCalculationPolicy: string
  roundingRule: string
  payrollTimeZoneId: string
  payslipVisibilityRule: string
  allowNegativeNetPay: boolean
  defaultCurrency: string
}

export interface PayrollSetupListQuery {
  search?: string
  isActive?: boolean | null
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  descending?: boolean
}

export interface PayPeriodTemplateOption {
  id: string
  code: string
  name: string
  payFrequency: string
  periodLengthDays: number
  isActive: boolean
}

export interface PayPeriodTemplateRecord {
  id: string
  code: string
  name: string
  description: string
  payFrequency: string
  periodLengthDays: number
  payrollOffsetDays: number
  isActive: boolean
  payPeriodCount: number
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface SavePayPeriodTemplateInput {
  code: string
  name: string
  description: string
  payFrequency: string
  periodLengthDays: number
  payrollOffsetDays: number
  isActive: boolean
}

export interface EarningTypeOption {
  id: string
  code: string
  name: string
  category: string
  taxable: boolean
  isActive: boolean
}

export interface EarningTypeRecord {
  id: string
  code: string
  name: string
  description: string
  category: string
  taxable: boolean
  recurring: boolean
  affectsThirteenthMonth: boolean
  isActive: boolean
  employeeRecurringCount: number
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface SaveEarningTypeInput {
  code: string
  name: string
  description: string
  category: string
  taxable: boolean
  recurring: boolean
  affectsThirteenthMonth: boolean
  isActive: boolean
}

export interface DeductionTypeOption {
  id: string
  code: string
  name: string
  category: string
  preTax: boolean
  isActive: boolean
}

export interface DeductionTypeRecord {
  id: string
  code: string
  name: string
  description: string
  category: string
  preTax: boolean
  recurring: boolean
  isActive: boolean
  employeeRecurringCount: number
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface SaveDeductionTypeInput {
  code: string
  name: string
  description: string
  category: string
  preTax: boolean
  recurring: boolean
  isActive: boolean
}

export interface ContributionTypeOption {
  id: string
  code: string
  name: string
  isActive: boolean
}

export interface ContributionTypeRecord {
  id: string
  code: string
  name: string
  description: string
  employeeShareApplicable: boolean
  employerShareApplicable: boolean
  isActive: boolean
  tableCount: number
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface SaveContributionTypeInput {
  code: string
  name: string
  description: string
  employeeShareApplicable: boolean
  employerShareApplicable: boolean
  isActive: boolean
}

export interface GovernmentContributionBracket {
  id?: string
  minCompensation: number
  maxCompensation?: number | null
  employeeShareAmount?: number | null
  employeeShareRate?: number | null
  employerShareAmount?: number | null
  employerShareRate?: number | null
  remarks: string
}

export interface GovernmentContributionTableRecord {
  id: string
  contributionTypeId: string
  contributionTypeCode: string
  contributionTypeName: string
  name: string
  effectiveStartDate: string
  effectiveEndDate?: string | null
  isActive: boolean
  bracketCount: number
  brackets: GovernmentContributionBracket[]
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface SaveGovernmentContributionTableInput {
  contributionTypeId: string | null
  name: string
  effectiveStartDate: string | null
  effectiveEndDate?: string | null
  isActive: boolean
  brackets: GovernmentContributionBracket[]
}

export interface TaxTableOption {
  id: string
  code: string
  name: string
  payFrequency: string
  isActive: boolean
}

export interface TaxBracket {
  id?: string
  minTaxableIncome: number
  maxTaxableIncome?: number | null
  baseTax: number
  taxRate: number
  excessOver: number
}

export interface TaxTableRecord {
  id: string
  code: string
  name: string
  payFrequency: string
  effectiveStartDate: string
  effectiveEndDate?: string | null
  isActive: boolean
  brackets: TaxBracket[]
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface SaveTaxTableInput {
  code: string
  name: string
  payFrequency: string
  effectiveStartDate: string | null
  effectiveEndDate?: string | null
  isActive: boolean
  brackets: TaxBracket[]
}

export interface PayPeriodOption {
  id: string
  code: string
  name: string
  payFrequency: string
  periodStartDate: string
  periodEndDate: string
  payrollDate: string
  status: string
}

export interface PayPeriodRecord {
  id: string
  code: string
  name: string
  payFrequency: string
  periodStartDate: string
  periodEndDate: string
  payrollDate: string
  cutoffStartDate: string
  cutoffEndDate: string
  status: string
  remarks: string
  payPeriodTemplateId?: string | null
  payPeriodTemplateName: string
  payrollRunCount: number
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface PayPeriodListQuery {
  search?: string
  status?: string
  payFrequency?: string
  dateFrom?: string
  dateTo?: string
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  descending?: boolean
}

export interface SavePayPeriodInput {
  code: string
  name: string
  payFrequency: string
  periodStartDate: string | null
  periodEndDate?: string | null
  payrollDate?: string | null
  cutoffStartDate?: string | null
  cutoffEndDate?: string | null
  status: string
  remarks: string
  payPeriodTemplateId?: string | null
}

export interface CompensationProfileRecord {
  id: string
  employeeId: string
  employeeCode: string
  employeeFullName: string
  departmentName: string
  branchName: string
  payType: string
  payFrequency: string
  basicSalary: number
  dailyRate?: number | null
  hourlyRate?: number | null
  currency: string
  effectiveStartDate: string
  effectiveEndDate?: string | null
  isActive: boolean
  remarks: string
  createdByDisplayName: string
  updatedByDisplayName: string
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface CompensationProfileListQuery {
  search?: string
  employeeId?: string
  departmentId?: string
  branchId?: string
  isActive?: boolean | null
  effectiveDate?: string
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  descending?: boolean
}

export interface SaveCompensationProfileInput {
  employeeId: string | null
  payType: string
  payFrequency: string
  basicSalary: number
  dailyRate?: number | null
  hourlyRate?: number | null
  currency: string
  effectiveStartDate: string | null
  effectiveEndDate?: string | null
  isActive: boolean
  remarks: string
}

export interface EmployeeRecurringEarningRecord {
  id: string
  employeeId: string
  employeeCode: string
  employeeFullName: string
  earningTypeId: string
  earningTypeCode: string
  earningTypeName: string
  amount: number
  frequency: string
  effectiveStartDate: string
  effectiveEndDate?: string | null
  isActive: boolean
  remarks: string
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface SaveEmployeeRecurringEarningInput {
  employeeId: string | null
  earningTypeId: string | null
  amount: number
  frequency: string
  effectiveStartDate: string | null
  effectiveEndDate?: string | null
  isActive: boolean
  remarks: string
}

export interface EmployeeRecurringDeductionRecord {
  id: string
  employeeId: string
  employeeCode: string
  employeeFullName: string
  deductionTypeId: string
  deductionTypeCode: string
  deductionTypeName: string
  amount: number
  frequency: string
  balance?: number | null
  effectiveStartDate: string
  effectiveEndDate?: string | null
  isActive: boolean
  remarks: string
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface SaveEmployeeRecurringDeductionInput {
  employeeId: string | null
  deductionTypeId: string | null
  amount: number
  frequency: string
  balance?: number | null
  effectiveStartDate: string | null
  effectiveEndDate?: string | null
  isActive: boolean
  remarks: string
}

export interface RecurringPayrollComponentListQuery {
  search?: string
  employeeId?: string
  departmentId?: string
  branchId?: string
  isActive?: boolean | null
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  descending?: boolean
}

export interface PayrollRunSummary {
  id: string
  payPeriodId: string
  payPeriodCode: string
  payPeriodName: string
  referenceNumber: string
  name: string
  status: string
  employeeCount: number
  holdCount: number
  criticalIssueCount: number
  totalGrossPay: number
  totalDeductions: number
  totalNetPay: number
  generatedByDisplayName: string
  generatedAtUtc: string
  approvedByDisplayName: string
  approvedAtUtc?: string | null
  paidAtUtc?: string | null
  remarks: string
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface PayrollRunListQuery {
  search?: string
  payPeriodId?: string
  status?: string
  departmentId?: string
  branchId?: string
  dateFrom?: string
  dateTo?: string
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  descending?: boolean
}

export interface GeneratePayrollRunInput {
  payPeriodId: string | null
  referenceNumber: string
  name: string
  departmentId?: string | null
  branchId?: string | null
  employmentTypeId?: string | null
  employmentStatusId?: string | null
  selectedEmployeeIds: string[]
  remarks: string
}

export interface PayrollRunActionInput {
  remarks: string
}

export interface PayrollEarningLine {
  id: string
  earningTypeId?: string | null
  earningTypeCode: string
  earningTypeName: string
  description: string
  amount: number
  quantity?: number | null
  rate?: number | null
  source: string
  taxable: boolean
  isManual: boolean
  remarks: string
}

export interface PayrollDeductionLine {
  id: string
  deductionTypeId?: string | null
  deductionTypeCode: string
  deductionTypeName: string
  deductionCategory: string
  description: string
  amount: number
  source: string
  preTax: boolean
  isManual: boolean
  remarks: string
}

export interface PayrollRunItem {
  id: string
  employeeId: string
  employeeCode: string
  employeeName: string
  departmentName: string
  positionName: string
  branchName: string
  payType: string
  currency: string
  basicSalary: number
  dailyRate?: number | null
  hourlyRate?: number | null
  regularWorkedDays: number
  regularWorkedHours: number
  paidLeaveDays: number
  unpaidLeaveDays: number
  absentDays: number
  lateMinutes: number
  undertimeMinutes: number
  overtimeMinutes: number
  basicPay: number
  allowanceTotal: number
  overtimePay: number
  holidayPay: number
  leavePay: number
  bonusTotal: number
  otherEarningsTotal: number
  grossPay: number
  governmentDeductionsTotal: number
  taxDeduction: number
  absenceDeduction: number
  lateDeduction: number
  undertimeDeduction: number
  loanDeduction: number
  otherDeductionsTotal: number
  totalDeductions: number
  netPay: number
  employerContributionTotal: number
  status: string
  remarks: string
  hasCriticalIssues: boolean
  issues: string[]
  earnings: PayrollEarningLine[]
  deductions: PayrollDeductionLine[]
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface PayrollAuditLog {
  id: string
  entityType: string
  entityId: string
  action: string
  summary: string
  actorDisplayName: string
  createdAtUtc: string
}

export interface PayrollRunDetail {
  run: PayrollRunSummary
  payPeriod: PayPeriodRecord
  items: PayrollRunItem[]
  auditLogs: PayrollAuditLog[]
}

export interface PayrollAdjustmentRecord {
  id: string
  employeeId: string
  employeeCode: string
  employeeFullName: string
  departmentName: string
  branchName: string
  payPeriodId?: string | null
  payPeriodName: string
  payrollRunId?: string | null
  payrollRunReferenceNumber: string
  adjustmentType: string
  earningTypeId?: string | null
  earningTypeName: string
  deductionTypeId?: string | null
  deductionTypeName: string
  amount: number
  reason: string
  status: string
  requestedByDisplayName: string
  approvedByDisplayName: string
  approvedAtUtc?: string | null
  appliedAtUtc?: string | null
  decisionRemarks: string
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface PayrollAdjustmentListQuery {
  search?: string
  employeeId?: string
  payPeriodId?: string
  payrollRunId?: string
  departmentId?: string
  branchId?: string
  status?: string
  adjustmentType?: string
  dateFrom?: string
  dateTo?: string
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  descending?: boolean
}

export interface SavePayrollAdjustmentInput {
  employeeId: string | null
  payPeriodId?: string | null
  payrollRunId?: string | null
  adjustmentType: string
  earningTypeId?: string | null
  deductionTypeId?: string | null
  amount: number
  reason: string
}

export interface EmployeePayrollProfile {
  employeeId: string
  employeeCode: string
  employeeFullName: string
  compensationProfiles: CompensationProfileRecord[]
  recurringEarnings: EmployeeRecurringEarningRecord[]
  recurringDeductions: EmployeeRecurringDeductionRecord[]
  payrollHistory: PayrollRunItem[]
}

export interface PayrollReportGroup {
  label: string
  count: number
  grossPay: number
  deductions: number
  netPay: number
}

export interface PayrollReportLine {
  label: string
  amount: number
}

export interface PayrollReportQuery {
  payPeriodId?: string
  payrollRunId?: string
  employeeId?: string
  departmentId?: string
  branchId?: string
  employmentTypeId?: string
  status?: string
  dateFrom?: string
  dateTo?: string
}

export interface PayrollReports {
  totalGrossPay: number
  totalDeductions: number
  totalNetPay: number
  register: PayrollRunItem[]
  byDepartment: PayrollReportGroup[]
  byBranch: PayrollReportGroup[]
  earnings: PayrollReportLine[]
  deductions: PayrollReportLine[]
  governmentContributions: PayrollReportLine[]
  adjustments: PayrollAdjustmentRecord[]
}

export interface Payslip {
  payrollRunItemId: string
  companyName: string
  payrollRunReferenceNumber: string
  payrollRunName: string
  payPeriodName: string
  periodStartDate: string
  periodEndDate: string
  payrollDate: string
  employeeCode: string
  employeeName: string
  departmentName: string
  positionName: string
  branchName: string
  currency: string
  regularWorkedDays: number
  regularWorkedHours: number
  paidLeaveDays: number
  unpaidLeaveDays: number
  absentDays: number
  lateMinutes: number
  undertimeMinutes: number
  overtimeMinutes: number
  grossPay: number
  totalDeductions: number
  netPay: number
  employerContributionTotal: number
  remarks: string
  issues: string[]
  earnings: PayrollEarningLine[]
  deductions: PayrollDeductionLine[]
  generatedAtUtc: string
}

export interface UserNotification {
  id: string
  title: string
  message: string
  type: string
  referenceType: string
  referenceId: string
  actionUrl: string
  isRead: boolean
  readAtUtc?: string | null
  createdAtUtc: string
}

export interface NotificationSummary {
  unreadCount: number
  recent: UserNotification[]
}

export interface NotificationListQuery {
  isRead?: boolean | null
  sortBy?: string
  descending?: boolean
  pageNumber?: number
  pageSize?: number
}

export interface EmployeeSelfProfile {
  id: string
  employeeCode: string
  fullName: string
  firstName: string
  middleName: string
  lastName: string
  suffix: string
  gender: string
  birthDate?: string | null
  civilStatus: string
  nationality: string
  mobileNumber: string
  email: string
  address: string
  cityProvince: string
  postalCode: string
  emergencyContactName: string
  emergencyContactRelationship: string
  emergencyContactPhone: string
  departmentName: string
  positionName: string
  branchName: string
  employmentTypeName: string
  employmentStatusName: string
  managerName: string
  workSchedule: string
  dateHired?: string | null
  dateRegularized?: string | null
  dateSeparated?: string | null
  sssNumberMasked: string
  philHealthNumberMasked: string
  pagIbigNumberMasked: string
  tinNumberMasked: string
  otherGovernmentIdMasked: string
  isActive: boolean
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface ProfileFieldChange {
  fieldKey: string
  label: string
  oldValue: string
  newValue: string
}

export interface ProfileChangeRequest {
  id: string
  employeeId: string
  employeeCode: string
  employeeFullName: string
  requestType: string
  fieldChanges: ProfileFieldChange[]
  reason: string
  status: string
  requestedByDisplayName: string
  reviewedByDisplayName: string
  reviewerRemarks: string
  reviewedAtUtc?: string | null
  appliedAtUtc?: string | null
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface SaveProfileChangeRequestInput {
  mobileNumber: string
  email: string
  address: string
  cityProvince: string
  postalCode: string
  civilStatus: string
  nationality: string
  emergencyContactName: string
  emergencyContactRelationship: string
  emergencyContactPhone: string
  reason: string
}

export interface ProfileChangeRequestListQuery {
  status?: string
  dateFrom?: string
  dateTo?: string
  sortBy?: string
  descending?: boolean
  pageNumber?: number
  pageSize?: number
}

export interface AttendanceAdjustmentRequest {
  id: string
  employeeId: string
  employeeCode: string
  employeeFullName: string
  attendanceRecordId?: string | null
  attendanceDate: string
  requestType: string
  currentTimeIn?: string | null
  currentTimeOut?: string | null
  currentRemarks: string
  requestedTimeIn?: string | null
  requestedTimeOut?: string | null
  requestedRemarks: string
  reason: string
  status: string
  currentApproverDisplayName: string
  requestedByDisplayName: string
  reviewedByDisplayName: string
  reviewerRemarks: string
  reviewedAtUtc?: string | null
  appliedAtUtc?: string | null
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface AttendanceAdjustmentRequestListQuery {
  employeeId?: string
  status?: string
  dateFrom?: string
  dateTo?: string
  sortBy?: string
  descending?: boolean
  pageNumber?: number
  pageSize?: number
}

export interface SaveAttendanceAdjustmentRequestInput {
  attendanceRecordId?: string | null
  attendanceDate?: string | null
  requestType: string
  requestedTimeIn?: string | null
  requestedTimeOut?: string | null
  requestedRemarks: string
  reason: string
}

export interface ApprovalActionInput {
  remarks: string
}

export interface PayslipSummary {
  payrollRunItemId: string
  payrollRunReferenceNumber: string
  payPeriodName: string
  periodStartDate: string
  periodEndDate: string
  payrollDate: string
  currency: string
  grossPay: number
  netPay: number
  status: string
}

export interface MyPayslipListQuery {
  year?: number
  sortBy?: string
  descending?: boolean
  pageNumber?: number
  pageSize?: number
}

export interface EmployeePortalDashboard {
  employeeId: string
  employeeCode: string
  employeeFullName: string
  profileCompletionPercent: number
  todayAttendance?: AttendanceRecordListItem | null
  lastAttendance?: AttendanceRecordListItem | null
  leaveBalances: LeaveBalance[]
  pendingLeaveRequestCount: number
  pendingAttendanceAdjustmentRequestCount: number
  pendingProfileChangeRequestCount: number
  upcomingApprovedLeaves: LeaveRequest[]
  latestPayslip?: PayslipSummary | null
  documentSummary: {
    totalDocuments: number
    activeDocuments: number
    archivedDocuments: number
    missingRequiredDocuments: number
    expiredDocuments: number
    expiringSoonDocuments: number
    requiredDocumentTypes: number
    submittedRequiredDocumentTypes: number
    hasIssues: boolean
  }
  notifications: UserNotification[]
}

export interface EmployeeRequestHistoryItem {
  requestType: string
  requestLabel: string
  requestId: string
  title: string
  subtitle: string
  status: string
  currentApproverDisplayName: string
  submittedAtUtc: string
  lastUpdatedAtUtc: string
  canCancel: boolean
}

export interface ManagerDashboard {
  managerEmployeeId: string
  directReportCount: number
  presentTodayCount: number
  lateTodayCount: number
  absentTodayCount: number
  onLeaveTodayCount: number
  incompleteLogCount: number
  employeesWithoutScheduleCount: number
  pendingApprovalCount: number
  upcomingTeamLeaveCount: number
  notifications: UserNotification[]
}

export interface ManagerPortalOptions {
  employees: EmployeeAttendanceOption[]
  departments: LookupOption[]
  branches: LookupOption[]
}

export interface ManagerTeamMember {
  employeeId: string
  employeeCode: string
  fullName: string
  departmentName: string
  positionName: string
  branchName: string
  employmentStatusName: string
  mobileNumber: string
  email: string
  todayAttendanceStatus: string
  todayAttendanceTimeInLabel: string
  leaveStatus: string
  isActive: boolean
}

export interface ManagerTeamMemberListQuery {
  search?: string
  departmentId?: string
  branchId?: string
  sortBy?: string
  descending?: boolean
  pageNumber?: number
  pageSize?: number
}

export interface ApprovalCenterSummary {
  pendingLeaveRequestCount: number
  pendingAttendanceAdjustmentRequestCount: number
  pendingProfileChangeRequestCount: number
  pendingPayrollAdjustmentCount: number
  totalPendingCount: number
}

export interface ApprovalCenterOptions {
  departments: LookupOption[]
  branches: LookupOption[]
}

export interface ApprovalCenterInboxItem {
  approvalType: string
  approvalTypeLabel: string
  requestId: string
  employeeId: string
  employeeCode: string
  employeeFullName: string
  departmentName: string
  branchName: string
  title: string
  subtitle: string
  status: string
  currentApproverDisplayName: string
  submittedAtUtc: string
  lastUpdatedAtUtc: string
}

export interface ApprovalCenterQuery {
  type?: string
  status?: string
  search?: string
  departmentId?: string
  branchId?: string
  dateFrom?: string
  dateTo?: string
  sortBy?: string
  descending?: boolean
  pageNumber?: number
  pageSize?: number
}

export interface ReportDefinition {
  key: string
  name: string
  category: string
  description: string
  routePath: string
  allowedRoles: string[]
  filters: string[]
  supportsExport: boolean
  supportsSavedViews: boolean
}

export interface ReportColumn {
  key: string
  label: string
  alignment: string
}

export interface ReportRow {
  id: string
  linkPath: string
  values: Record<string, string>
}

export interface ReportMetric {
  key: string
  label: string
  value: string
  tone: string
}

export interface ReportResult {
  reportKey: string
  title: string
  description: string
  generatedAtUtc: string
  columns: ReportColumn[]
  rows: ReportRow[]
  metrics: ReportMetric[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface ReportQuery {
  search?: string
  employeeId?: string
  departmentId?: string
  branchId?: string
  employmentTypeId?: string
  employmentStatusId?: string
  leaveTypeId?: string
  documentTypeId?: string
  payPeriodId?: string
  payrollRunId?: string
  status?: string
  source?: string
  issueType?: string
  severity?: string
  entityType?: string
  action?: string
  dateFrom?: string
  dateTo?: string
  year?: number
  month?: number
  includeInactive?: boolean | null
  sortBy?: string
  descending?: boolean
  pageNumber?: number
  pageSize?: number
}

export interface ReportsCenter {
  reports: ReportDefinition[]
}

export interface ReportOptions {
  employees: EmployeeAttendanceOption[]
  departments: LookupOption[]
  branches: LookupOption[]
  employmentTypes: LookupOption[]
  employmentStatuses: LookupOption[]
  leaveTypes: LookupOption[]
  documentTypes: LookupOption[]
  payPeriods: LookupOption[]
  payrollRuns: LookupOption[]
}

export interface SavedReport {
  id: string
  reportKey: string
  name: string
  filtersJson: string
  isDefault: boolean
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface SaveSavedReportInput {
  reportKey: string
  name: string
  filtersJson: string
  isDefault: boolean
}

export interface ComplianceSummary {
  openIssueCount: number
  criticalIssueCount: number
  highIssueCount: number
  missingRequiredDocumentCount: number
  expiredDocumentCount: number
  expiringSoonDocumentCount: number
  missingGovernmentIdCount: number
  missingScheduleAssignmentCount: number
  missingCompensationProfileCount: number
  incompleteAttendanceCount: number
}

export interface ComplianceIssue {
  id: string
  issueType: string
  severity: string
  employeeId?: string | null
  departmentId?: string | null
  branchId?: string | null
  employeeCode: string
  employeeFullName: string
  departmentName: string
  branchName: string
  title: string
  description: string
  referenceType: string
  referenceId: string
  linkPath: string
  detectedAtUtc: string
}

export interface ComplianceIssueQuery {
  search?: string
  employeeId?: string
  departmentId?: string
  branchId?: string
  issueType?: string
  severity?: string
  sortBy?: string
  descending?: boolean
  pageNumber?: number
  pageSize?: number
}

export interface AnalyticsSeriesPoint {
  label: string
  value: number
}

export interface AnalyticsDashboard {
  metrics: ReportMetric[]
  headcountByDepartment: AnalyticsSeriesPoint[]
  headcountByBranch: AnalyticsSeriesPoint[]
  attendanceTrend: AnalyticsSeriesPoint[]
  leaveUsageTrend: AnalyticsSeriesPoint[]
  approvalVolume: AnalyticsSeriesPoint[]
  payrollCostTrend: AnalyticsSeriesPoint[]
}

export interface AuditLogQuery {
  search?: string
  entityType?: string
  action?: string
  employeeId?: string
  dateFrom?: string
  dateTo?: string
  sortBy?: string
  descending?: boolean
  pageNumber?: number
  pageSize?: number
}

export interface AuditLog {
  id: string
  actorUserId: string
  actorName: string
  action: string
  entityType: string
  entityId: string
  employeeId?: string | null
  employeeCode: string
  employeeFullName: string
  oldValuesJson: string
  newValuesJson: string
  ipAddress: string
  userAgent: string
  remarks: string
  createdAtUtc: string
}

export interface ProductionReadinessItem {
  key: string
  label: string
  status: string
  detail: string
  actionUrl: string
}

export interface ProductionReadinessSection {
  key: string
  title: string
  description: string
  items: ProductionReadinessItem[]
}

export interface OperationalGuidanceItem {
  key: string
  title: string
  description: string
}

export interface DataImportDefinition {
  key: string
  name: string
  description: string
  sampleFileName: string
  requiredColumns: string[]
  optionalColumns: string[]
}

export interface DataImportPreviewRow {
  rowNumber: number
  identifier: string
  operation: string
  status: string
  messages: string[]
  values: Record<string, string>
}

export interface DataImportPreview {
  importType: string
  importName: string
  fileName: string
  totalRows: number
  validRowCount: number
  invalidRowCount: number
  canApply: boolean
  columns: string[]
  rows: DataImportPreviewRow[]
}

export interface DataImportApplyResult {
  importType: string
  importName: string
  fileName: string
  processedCount: number
  createdCount: number
  updatedCount: number
  skippedCount: number
  errorCount: number
  appliedAtUtc: string
  rows: DataImportPreviewRow[]
}

export interface ProductionReadinessOverview {
  generatedAtUtc: string
  readinessPercent: number
  readyItemCount: number
  attentionItemCount: number
  blockedItemCount: number
  sections: ProductionReadinessSection[]
  availableImports: DataImportDefinition[]
  operationalGuidance: OperationalGuidanceItem[]
}

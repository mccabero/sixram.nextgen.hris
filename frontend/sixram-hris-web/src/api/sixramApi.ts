import { apiDownload, apiRequest } from './client'
import type {
  ApprovalActionInput,
  ApprovalCenterInboxItem,
  ApprovalCenterOptions,
  ApprovalCenterQuery,
  ApprovalCenterSummary,
  AnalyticsDashboard,
  AuditLog,
  AuditLogQuery,
  AttendanceAdjustmentRequest,
  AttendanceAdjustmentRequestListQuery,
  AttendanceDashboardSummary,
  AttendanceListOptions,
  AttendanceRecordListItem,
  AttendanceRecordListQuery,
  AttendanceSetupSummary,
  AuthResponse,
  CompensationProfileListQuery,
  CompensationProfileRecord,
  ComplianceIssue,
  ComplianceIssueQuery,
  ComplianceSummary,
  ContributionTypeRecord,
  CreateRoleInput,
  CreateUserInput,
  DeductionTypeRecord,
  DocumentComplianceSummary,
  DocumentType,
  DocumentTypeInput,
  DocumentTypeListQuery,
  EarningTypeRecord,
  EmployeeDetail,
  EmployeeDocument,
  EmployeeDocumentListOptions,
  EmployeeDocumentListQuery,
  EmployeeDocumentProfile,
  EmployeeEditorOptions,
  EmployeeLeaveProfile,
  EmployeePayrollProfile,
  EmployeePortalDashboard,
  EmployeeRecurringDeductionRecord,
  EmployeeRecurringEarningRecord,
  EmployeeRequestHistoryItem,
  EmployeeScheduleAssignmentListQuery,
  EmployeeScheduleAssignmentRecord,
  EmployeeSelfProfile,
  EmployeeSummary,
  EmployeeListQuery,
  GeneratePayrollRunInput,
  GovernmentContributionTableRecord,
  LeaveActionInput,
  LeaveBalance,
  LeaveBalanceAdjustmentInput,
  LeaveBalanceListQuery,
  LeaveBalanceTransaction,
  LeaveCalendarQuery,
  LeaveCalendarResponse,
  LeaveDashboardSummary,
  LeaveManagementOptions,
  LeaveRequest,
  LeaveRequestListQuery,
  LeaveType,
  LeaveTypeInput,
  LeaveTypeListQuery,
  ManagerDashboard,
  ManagerTeamMember,
  ManagerTeamMemberListQuery,
  ManagerPortalOptions,
  MyPayslipListQuery,
  NotificationListQuery,
  NotificationSummary,
  OrganizationListQuery,
  OrganizationOptions,
  OrganizationRecord,
  OrganizationRecordInput,
  OrganizationResource,
  OrganizationSummary,
  PayPeriodListQuery,
  PayPeriodRecord,
  PayPeriodTemplateRecord,
  PagedResult,
  Payslip,
  PayslipSummary,
  PayrollAdjustmentListQuery,
  PayrollAdjustmentRecord,
  PayrollDashboardSummary,
  PayrollOptions,
  PayrollReportQuery,
  PayrollReports,
  PayrollRunActionInput,
  PayrollRunDetail,
  PayrollRunListQuery,
  PayrollRunSummary,
  PayrollSettings,
  PayrollSetupListQuery,
  PayrollSetupSummary,
  GenerateProvidentFundContributionBatchInput,
  ProvidentFundAdjustment,
  ProvidentFundAdjustmentInput,
  ProvidentFundBalance,
  ProvidentFundBalanceReportRow,
  ProvidentFundContributionBatch,
  ProvidentFundContributionBatchDetail,
  ProvidentFundContributionReportRow,
  ProvidentFundDashboard,
  ProvidentFundEnrollment,
  ProvidentFundEnrollmentInput,
  ProvidentFundLedgerTransaction,
  ProvidentFundListQuery,
  ProvidentFundOptions,
  ProvidentFundPolicy,
  ProvidentFundPolicyInput,
  ProvidentFundVestingRule,
  ProvidentFundVestingRuleInput,
  ProvidentFundWithdrawalInput,
  ProvidentFundWithdrawalReportRow,
  ProvidentFundWithdrawalRequest,
  ProductionReadinessOverview,
  DataImportPreview,
  DataImportApplyResult,
  ProfileChangeRequest,
  ProfileChangeRequestListQuery,
  RbacSummary,
  ReportOptions,
  ReportQuery,
  ReportResult,
  ReportsCenter,
  RecurringPayrollComponentListQuery,
  Role,
  SaveSavedReportInput,
  SaveAttendanceAdjustmentRequestInput,
  SaveAttendanceRecordInput,
  SaveCompensationProfileInput,
  SaveContributionTypeInput,
  SaveDeductionTypeInput,
  SaveEarningTypeInput,
  SaveEmployeeDocumentMetadataInput,
  SaveEmployeeInput,
  SaveEmployeeRecurringDeductionInput,
  SaveEmployeeRecurringEarningInput,
  SaveEmployeeScheduleAssignmentInput,
  SaveGovernmentContributionTableInput,
  SavePayPeriodInput,
  SavePayPeriodTemplateInput,
  SavePayrollAdjustmentInput,
  SaveProfileChangeRequestInput,
  SaveShiftInput,
  SaveTaxTableInput,
  SaveWorkScheduleInput,
  SetEmployeeDocumentArchiveStateInput,
  SetPasswordInput,
  SetUserRolesInput,
  SetUserStatusInput,
  ShiftListQuery,
  ShiftRecord,
  SavedReport,
  TaxTableRecord,
  UpdateRoleInput,
  UpdateUserInput,
  UserNotification,
  UserSummary,
  WorkScheduleListQuery,
  WorkScheduleRecord,
} from '../types/models'

export const sixramApi = {
  login(email: string, password: string) {
    return apiRequest<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
      skipAuth: true,
      retryOnUnauthorized: false,
    })
  },

  refreshSession() {
    return apiRequest<AuthResponse | null>('/api/auth/refresh', {
      method: 'POST',
      skipAuth: true,
      retryOnUnauthorized: false,
    }).then((response) => response ?? null)
  },

  logout() {
    return apiRequest<void>('/api/auth/logout', {
      method: 'POST',
      skipAuth: true,
      retryOnUnauthorized: false,
    })
  },

  getNotificationSummary() {
    return apiRequest<NotificationSummary>('/api/notifications/summary')
  },

  getAnalyticsDashboard() {
    return apiRequest<AnalyticsDashboard>('/api/analytics/dashboard')
  },

  getReportsCenter() {
    return apiRequest<ReportsCenter>('/api/reports/registry')
  },

  getReportOptions() {
    return apiRequest<ReportOptions>('/api/reports/options')
  },

  runReport(reportKey: string, query: ReportQuery = {}) {
    return apiRequest<ReportResult>(`/api/reports/${encodeURIComponent(reportKey)}${toQueryString(query)}`)
  },

  exportReportCsv(reportKey: string, query: ReportQuery = {}) {
    return apiDownload(`/api/reports/${encodeURIComponent(reportKey)}/export/csv${toQueryString(query)}`)
  },

  getSavedReports() {
    return apiRequest<SavedReport[]>('/api/reports/saved')
  },

  createSavedReport(input: SaveSavedReportInput) {
    return apiRequest<SavedReport>('/api/reports/saved', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateSavedReport(savedReportId: string, input: SaveSavedReportInput) {
    return apiRequest<SavedReport>(`/api/reports/saved/${savedReportId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deleteSavedReport(savedReportId: string) {
    return apiRequest<void>(`/api/reports/saved/${savedReportId}`, {
      method: 'DELETE',
    })
  },

  getComplianceSummary() {
    return apiRequest<ComplianceSummary>('/api/compliance/summary')
  },

  getComplianceIssues(query: ComplianceIssueQuery = {}) {
    return apiRequest<PagedResult<ComplianceIssue>>(`/api/compliance/issues${toQueryString(query)}`)
  },

  getAuditLogs(query: AuditLogQuery = {}) {
    return apiRequest<PagedResult<AuditLog>>(`/api/audit-logs${toQueryString(query)}`)
  },

  getAuditLogById(auditLogId: string) {
    return apiRequest<AuditLog>(`/api/audit-logs/${auditLogId}`)
  },

  getProductionReadinessOverview() {
    return apiRequest<ProductionReadinessOverview>('/api/admin/production-readiness')
  },

  previewProductionImport(importType: string, file: File) {
    const formData = new FormData()
    formData.append('importType', importType)
    formData.append('file', file)

    return apiRequest<DataImportPreview>('/api/admin/production-readiness/imports/preview', {
      method: 'POST',
      body: formData,
    })
  },

  applyProductionImport(importType: string, file: File) {
    const formData = new FormData()
    formData.append('importType', importType)
    formData.append('file', file)

    return apiRequest<DataImportApplyResult>('/api/admin/production-readiness/imports/apply', {
      method: 'POST',
      body: formData,
    })
  },

  getNotifications(query: NotificationListQuery = {}) {
    return apiRequest<PagedResult<UserNotification>>(`/api/notifications${toQueryString(query)}`)
  },

  markNotificationRead(notificationId: string) {
    return apiRequest<void>(`/api/notifications/${notificationId}/read`, {
      method: 'POST',
    })
  },

  markAllNotificationsRead() {
    return apiRequest<void>('/api/notifications/read-all', {
      method: 'POST',
    })
  },

  getMyDashboard() {
    return apiRequest<EmployeePortalDashboard>('/api/me/dashboard')
  },

  getMyProfile() {
    return apiRequest<EmployeeSelfProfile>('/api/me/profile')
  },

  getMyProfileChangeRequests(query: ProfileChangeRequestListQuery = {}) {
    return apiRequest<PagedResult<ProfileChangeRequest>>(`/api/me/profile-change-requests${toQueryString(query)}`)
  },

  createMyProfileChangeRequest(input: SaveProfileChangeRequestInput) {
    return apiRequest<ProfileChangeRequest>('/api/me/profile-change-requests', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  cancelMyProfileChangeRequest(requestId: string, input: ApprovalActionInput) {
    return apiRequest<ProfileChangeRequest>(`/api/me/profile-change-requests/${requestId}/cancel`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  getMyDocuments() {
    return apiRequest<EmployeeDocumentProfile>('/api/me/documents')
  },

  downloadMyDocument(documentId: string) {
    return apiDownload(`/api/me/documents/${documentId}/download`)
  },

  getMyAttendance(query: AttendanceRecordListQuery = {}) {
    return apiRequest<PagedResult<AttendanceRecordListItem>>(`/api/me/attendance${toQueryString(query)}`)
  },

  getMyAttendanceAdjustments(query: AttendanceAdjustmentRequestListQuery = {}) {
    return apiRequest<PagedResult<AttendanceAdjustmentRequest>>(`/api/me/attendance/adjustments${toQueryString(query)}`)
  },

  createMyAttendanceAdjustment(input: SaveAttendanceAdjustmentRequestInput) {
    return apiRequest<AttendanceAdjustmentRequest>('/api/me/attendance/adjustments', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  cancelMyAttendanceAdjustment(requestId: string, input: ApprovalActionInput) {
    return apiRequest<AttendanceAdjustmentRequest>(`/api/me/attendance/adjustments/${requestId}/cancel`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  getMyLeaveProfile(periodYear?: number) {
    const query = typeof periodYear === 'number' ? `?periodYear=${periodYear}` : ''
    return apiRequest<EmployeeLeaveProfile>(`/api/me/leave/profile${query}`)
  },

  getMyLeaveOptions() {
    return apiRequest<LeaveManagementOptions>('/api/me/leave/options')
  },

  getMyLeaveRequests(query: LeaveRequestListQuery = {}) {
    return apiRequest<PagedResult<LeaveRequest>>(`/api/me/leave/requests${toQueryString(query)}`)
  },

  createMyLeaveRequest(formData: FormData) {
    return apiRequest<LeaveRequest>('/api/me/leave/requests', {
      method: 'POST',
      body: formData,
    })
  },

  cancelMyLeaveRequest(leaveRequestId: string, input: LeaveActionInput) {
    return apiRequest<LeaveRequest>(`/api/me/leave/requests/${leaveRequestId}/cancel`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  downloadMyLeaveAttachment(leaveRequestId: string) {
    return apiDownload(`/api/me/leave/requests/${leaveRequestId}/attachment`)
  },

  getMyPayslips(query: MyPayslipListQuery = {}) {
    return apiRequest<PagedResult<PayslipSummary>>(`/api/me/payslips${toQueryString(query)}`)
  },

  getMyPayslip(payrollRunItemId: string) {
    return apiRequest<Payslip>(`/api/me/payslips/${payrollRunItemId}`)
  },

  getMyRequests() {
    return apiRequest<EmployeeRequestHistoryItem[]>('/api/me/requests')
  },

  getManagerDashboard() {
    return apiRequest<ManagerDashboard>('/api/manager/dashboard')
  },

  getManagerOptions() {
    return apiRequest<ManagerPortalOptions>('/api/manager/options')
  },

  getMyTeam(query: ManagerTeamMemberListQuery = {}) {
    return apiRequest<PagedResult<ManagerTeamMember>>(`/api/manager/team${toQueryString(query)}`)
  },

  getTeamAttendance(query: AttendanceRecordListQuery = {}) {
    return apiRequest<PagedResult<AttendanceRecordListItem>>(`/api/manager/attendance${toQueryString(query)}`)
  },

  getTeamLeaveRequests(query: LeaveRequestListQuery = {}) {
    return apiRequest<PagedResult<LeaveRequest>>(`/api/manager/leave/requests${toQueryString(query)}`)
  },

  getTeamLeaveCalendar(query: LeaveCalendarQuery) {
    return apiRequest<LeaveCalendarResponse>(`/api/manager/leave/calendar${toQueryString(query)}`)
  },

  getApprovalCenterSummary() {
    return apiRequest<ApprovalCenterSummary>('/api/approvals/summary')
  },

  getApprovalCenterOptions() {
    return apiRequest<ApprovalCenterOptions>('/api/approvals/options')
  },

  getApprovalCenterInbox(query: ApprovalCenterQuery = {}) {
    return apiRequest<PagedResult<ApprovalCenterInboxItem>>(`/api/approvals/inbox${toQueryString(query)}`)
  },

  getApprovalProfileChangeRequest(requestId: string) {
    return apiRequest<ProfileChangeRequest>(`/api/approvals/profile-change-requests/${requestId}`)
  },

  approveProfileChangeRequest(requestId: string, input: ApprovalActionInput) {
    return apiRequest<ProfileChangeRequest>(`/api/approvals/profile-change-requests/${requestId}/approve`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  rejectProfileChangeRequest(requestId: string, input: ApprovalActionInput) {
    return apiRequest<ProfileChangeRequest>(`/api/approvals/profile-change-requests/${requestId}/reject`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  getApprovalAttendanceAdjustment(requestId: string) {
    return apiRequest<AttendanceAdjustmentRequest>(`/api/approvals/attendance-adjustments/${requestId}`)
  },

  approveAttendanceAdjustment(requestId: string, input: ApprovalActionInput) {
    return apiRequest<AttendanceAdjustmentRequest>(`/api/approvals/attendance-adjustments/${requestId}/approve`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  rejectAttendanceAdjustment(requestId: string, input: ApprovalActionInput) {
    return apiRequest<AttendanceAdjustmentRequest>(`/api/approvals/attendance-adjustments/${requestId}/reject`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  getApprovalLeaveRequest(leaveRequestId: string) {
    return apiRequest<LeaveRequest>(`/api/approvals/leave-requests/${leaveRequestId}`)
  },

  approveApprovalLeaveRequest(leaveRequestId: string, input: LeaveActionInput) {
    return apiRequest<LeaveRequest>(`/api/approvals/leave-requests/${leaveRequestId}/approve`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  rejectApprovalLeaveRequest(leaveRequestId: string, input: LeaveActionInput) {
    return apiRequest<LeaveRequest>(`/api/approvals/leave-requests/${leaveRequestId}/reject`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  approveApprovalPayrollAdjustment(payrollAdjustmentId: string, input: PayrollRunActionInput) {
    return apiRequest<PayrollAdjustmentRecord>(`/api/approvals/payroll-adjustments/${payrollAdjustmentId}/approve`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  rejectApprovalPayrollAdjustment(payrollAdjustmentId: string, input: PayrollRunActionInput) {
    return apiRequest<PayrollAdjustmentRecord>(`/api/approvals/payroll-adjustments/${payrollAdjustmentId}/reject`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  getUsers() {
    return apiRequest<UserSummary[]>('/api/admin/users')
  },

  createUser(input: CreateUserInput) {
    return apiRequest<UserSummary>('/api/admin/users', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateUser(userId: string, input: UpdateUserInput) {
    return apiRequest<UserSummary>(`/api/admin/users/${userId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deleteUser(userId: string) {
    return apiRequest<void>(`/api/admin/users/${userId}`, {
      method: 'DELETE',
    })
  },

  setUserStatus(userId: string, input: SetUserStatusInput) {
    return apiRequest<UserSummary>(`/api/admin/users/${userId}/status`, {
      method: 'PATCH',
      body: JSON.stringify(input),
    })
  },

  setUserRoles(userId: string, input: SetUserRolesInput) {
    return apiRequest<UserSummary>(`/api/admin/users/${userId}/roles`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  resetUserPassword(userId: string, input: SetPasswordInput) {
    return apiRequest<void>(`/api/admin/users/${userId}/reset-password`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  getRoles() {
    return apiRequest<Role[]>('/api/admin/roles')
  },

  createRole(input: CreateRoleInput) {
    return apiRequest<Role>('/api/admin/roles', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateRole(roleId: string, input: UpdateRoleInput) {
    return apiRequest<Role>(`/api/admin/roles/${roleId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deleteRole(roleId: string) {
    return apiRequest<void>(`/api/admin/roles/${roleId}`, {
      method: 'DELETE',
    })
  },

  getRbacSummary() {
    return apiRequest<RbacSummary>('/api/admin/rbac')
  },

  setRbacUserRoles(userId: string, input: SetUserRolesInput) {
    return apiRequest<UserSummary>(`/api/admin/rbac/users/${userId}/roles`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  getOrganizationSummary() {
    return apiRequest<OrganizationSummary>('/api/admin/organization/summary')
  },

  getOrganizationOptions() {
    return apiRequest<OrganizationOptions>('/api/admin/organization/options')
  },

  getOrganizationRecords(resource: OrganizationResource, query: OrganizationListQuery = {}) {
    return apiRequest<PagedResult<OrganizationRecord>>(`/api/admin/organization/${resource}${toQueryString(query)}`)
  },

  getOrganizationRecord(resource: OrganizationResource, id: string) {
    return apiRequest<OrganizationRecord>(`/api/admin/organization/${resource}/${id}`)
  },

  createOrganizationRecord(resource: OrganizationResource, input: OrganizationRecordInput) {
    return apiRequest<OrganizationRecord>(`/api/admin/organization/${resource}`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateOrganizationRecord(resource: OrganizationResource, id: string, input: OrganizationRecordInput) {
    return apiRequest<OrganizationRecord>(`/api/admin/organization/${resource}/${id}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deleteOrganizationRecord(resource: OrganizationResource, id: string) {
    return apiRequest<void>(`/api/admin/organization/${resource}/${id}`, {
      method: 'DELETE',
    })
  },

  getEmployees(query: EmployeeListQuery = {}) {
    return apiRequest<PagedResult<EmployeeSummary>>(`/api/admin/employees${toQueryString(query)}`)
  },

  getEmployeeOptions(employeeId?: string) {
    const query = employeeId ? `?employeeId=${encodeURIComponent(employeeId)}` : ''
    return apiRequest<EmployeeEditorOptions>(`/api/admin/employees/options${query}`)
  },

  getEmployeeById(employeeId: string) {
    return apiRequest<EmployeeDetail>(`/api/admin/employees/${employeeId}`)
  },

  createEmployee(input: SaveEmployeeInput) {
    return apiRequest<EmployeeDetail>('/api/admin/employees', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateEmployee(employeeId: string, input: SaveEmployeeInput) {
    return apiRequest<EmployeeDetail>(`/api/admin/employees/${employeeId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deleteEmployee(employeeId: string) {
    return apiRequest<void>(`/api/admin/employees/${employeeId}`, {
      method: 'DELETE',
    })
  },

  getEmployeeDocumentProfile(employeeId: string) {
    return apiRequest<EmployeeDocumentProfile>(`/api/admin/employees/${employeeId}/documents`)
  },

  createEmployeeDocument(employeeId: string, formData: FormData) {
    return apiRequest<EmployeeDocument>(`/api/admin/employees/${employeeId}/documents`, {
      method: 'POST',
      body: formData,
    })
  },

  updateEmployeeDocument(employeeId: string, documentId: string, input: SaveEmployeeDocumentMetadataInput) {
    return apiRequest<EmployeeDocument>(`/api/admin/employees/${employeeId}/documents/${documentId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  replaceEmployeeDocumentFile(employeeId: string, documentId: string, formData: FormData) {
    return apiRequest<EmployeeDocument>(`/api/admin/employees/${employeeId}/documents/${documentId}/replace`, {
      method: 'POST',
      body: formData,
    })
  },

  setEmployeeDocumentArchiveState(
    employeeId: string,
    documentId: string,
    input: SetEmployeeDocumentArchiveStateInput,
  ) {
    return apiRequest<EmployeeDocument>(`/api/admin/employees/${employeeId}/documents/${documentId}/archive`, {
      method: 'PATCH',
      body: JSON.stringify(input),
    })
  },

  deleteEmployeeDocument(employeeId: string, documentId: string) {
    return apiRequest<void>(`/api/admin/employees/${employeeId}/documents/${documentId}`, {
      method: 'DELETE',
    })
  },

  getDocumentComplianceSummary() {
    return apiRequest<DocumentComplianceSummary>('/api/admin/documents/summary')
  },

  getDocumentListOptions() {
    return apiRequest<EmployeeDocumentListOptions>('/api/admin/documents/options')
  },

  getEmployeeDocuments(query: EmployeeDocumentListQuery = {}) {
    return apiRequest<PagedResult<EmployeeDocument>>(`/api/admin/documents${toQueryString(query)}`)
  },

  downloadDocument(documentId: string) {
    return apiDownload(`/api/admin/documents/${documentId}/download`)
  },

  getDocumentTypes(query: DocumentTypeListQuery = {}) {
    return apiRequest<PagedResult<DocumentType>>(`/api/admin/document-types${toQueryString(query)}`)
  },

  createDocumentType(input: DocumentTypeInput) {
    return apiRequest<DocumentType>('/api/admin/document-types', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateDocumentType(documentTypeId: string, input: DocumentTypeInput) {
    return apiRequest<DocumentType>(`/api/admin/document-types/${documentTypeId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deleteDocumentType(documentTypeId: string) {
    return apiRequest<void>(`/api/admin/document-types/${documentTypeId}`, {
      method: 'DELETE',
    })
  },

  getAttendanceSummary() {
    return apiRequest<AttendanceDashboardSummary>('/api/admin/attendance/summary')
  },

  getAttendanceOptions() {
    return apiRequest<AttendanceListOptions>('/api/admin/attendance/options')
  },

  getAttendanceRecords(query: AttendanceRecordListQuery = {}) {
    return apiRequest<PagedResult<AttendanceRecordListItem>>(`/api/admin/attendance/records${toQueryString(query)}`)
  },

  getAttendanceRecord(attendanceRecordId: string) {
    return apiRequest<AttendanceRecordListItem>(`/api/admin/attendance/records/${attendanceRecordId}`)
  },

  createAttendanceRecord(input: SaveAttendanceRecordInput) {
    return apiRequest<AttendanceRecordListItem>('/api/admin/attendance/records', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateAttendanceRecord(attendanceRecordId: string, input: SaveAttendanceRecordInput) {
    return apiRequest<AttendanceRecordListItem>(`/api/admin/attendance/records/${attendanceRecordId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deleteAttendanceRecord(attendanceRecordId: string) {
    return apiRequest<void>(`/api/admin/attendance/records/${attendanceRecordId}`, {
      method: 'DELETE',
    })
  },

  getAttendanceSetupSummary() {
    return apiRequest<AttendanceSetupSummary>('/api/admin/attendance/setup/summary')
  },

  getAttendanceSetupOptions(assignmentId?: string) {
    const query = assignmentId ? `?assignmentId=${encodeURIComponent(assignmentId)}` : ''
    return apiRequest<AttendanceListOptions>(`/api/admin/attendance/setup/options${query}`)
  },

  getWorkSchedules(query: WorkScheduleListQuery = {}) {
    return apiRequest<PagedResult<WorkScheduleRecord>>(`/api/admin/attendance/setup/work-schedules${toQueryString(query)}`)
  },

  createWorkSchedule(input: SaveWorkScheduleInput) {
    return apiRequest<WorkScheduleRecord>('/api/admin/attendance/setup/work-schedules', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateWorkSchedule(workScheduleId: string, input: SaveWorkScheduleInput) {
    return apiRequest<WorkScheduleRecord>(`/api/admin/attendance/setup/work-schedules/${workScheduleId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deleteWorkSchedule(workScheduleId: string) {
    return apiRequest<void>(`/api/admin/attendance/setup/work-schedules/${workScheduleId}`, {
      method: 'DELETE',
    })
  },

  getShifts(query: ShiftListQuery = {}) {
    return apiRequest<PagedResult<ShiftRecord>>(`/api/admin/attendance/setup/shifts${toQueryString(query)}`)
  },

  createShift(input: SaveShiftInput) {
    return apiRequest<ShiftRecord>('/api/admin/attendance/setup/shifts', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateShift(shiftId: string, input: SaveShiftInput) {
    return apiRequest<ShiftRecord>(`/api/admin/attendance/setup/shifts/${shiftId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deleteShift(shiftId: string) {
    return apiRequest<void>(`/api/admin/attendance/setup/shifts/${shiftId}`, {
      method: 'DELETE',
    })
  },

  getScheduleAssignments(query: EmployeeScheduleAssignmentListQuery = {}) {
    return apiRequest<PagedResult<EmployeeScheduleAssignmentRecord>>(`/api/admin/attendance/setup/assignments${toQueryString(query)}`)
  },

  createScheduleAssignment(input: SaveEmployeeScheduleAssignmentInput) {
    return apiRequest<EmployeeScheduleAssignmentRecord>('/api/admin/attendance/setup/assignments', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateScheduleAssignment(assignmentId: string, input: SaveEmployeeScheduleAssignmentInput) {
    return apiRequest<EmployeeScheduleAssignmentRecord>(`/api/admin/attendance/setup/assignments/${assignmentId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  getLeaveSummary() {
    return apiRequest<LeaveDashboardSummary>('/api/admin/leave/summary')
  },

  getLeaveOptions() {
    return apiRequest<LeaveManagementOptions>('/api/admin/leave/options')
  },

  getLeaveBalances(query: LeaveBalanceListQuery = {}) {
    return apiRequest<PagedResult<LeaveBalance>>(`/api/admin/leave/balances${toQueryString(query)}`)
  },

  adjustLeaveBalance(input: LeaveBalanceAdjustmentInput) {
    return apiRequest<LeaveBalanceTransaction>('/api/admin/leave/balances/adjustments', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  getLeaveRequests(query: LeaveRequestListQuery = {}) {
    return apiRequest<PagedResult<LeaveRequest>>(`/api/admin/leave/requests${toQueryString(query)}`)
  },

  getLeaveRequestById(leaveRequestId: string) {
    return apiRequest<LeaveRequest>(`/api/admin/leave/requests/${leaveRequestId}`)
  },

  createLeaveRequest(formData: FormData) {
    return apiRequest<LeaveRequest>('/api/admin/leave/requests', {
      method: 'POST',
      body: formData,
    })
  },

  updateLeaveRequest(leaveRequestId: string, formData: FormData) {
    return apiRequest<LeaveRequest>(`/api/admin/leave/requests/${leaveRequestId}`, {
      method: 'PUT',
      body: formData,
    })
  },

  approveLeaveRequest(leaveRequestId: string, input: LeaveActionInput) {
    return apiRequest<LeaveRequest>(`/api/admin/leave/requests/${leaveRequestId}/approve`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  rejectLeaveRequest(leaveRequestId: string, input: LeaveActionInput) {
    return apiRequest<LeaveRequest>(`/api/admin/leave/requests/${leaveRequestId}/reject`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  cancelLeaveRequest(leaveRequestId: string, input: LeaveActionInput) {
    return apiRequest<LeaveRequest>(`/api/admin/leave/requests/${leaveRequestId}/cancel`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  deleteLeaveRequest(leaveRequestId: string) {
    return apiRequest<void>(`/api/admin/leave/requests/${leaveRequestId}`, {
      method: 'DELETE',
    })
  },

  downloadLeaveAttachment(leaveRequestId: string) {
    return apiDownload(`/api/admin/leave/requests/${leaveRequestId}/attachment`)
  },

  getLeaveCalendar(query: LeaveCalendarQuery) {
    return apiRequest<LeaveCalendarResponse>(`/api/admin/leave/calendar${toQueryString(query)}`)
  },

  getLeaveTypes(query: LeaveTypeListQuery = {}) {
    return apiRequest<PagedResult<LeaveType>>(`/api/admin/leave-types${toQueryString(query)}`)
  },

  createLeaveType(input: LeaveTypeInput) {
    return apiRequest<LeaveType>('/api/admin/leave-types', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateLeaveType(leaveTypeId: string, input: LeaveTypeInput) {
    return apiRequest<LeaveType>(`/api/admin/leave-types/${leaveTypeId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deleteLeaveType(leaveTypeId: string) {
    return apiRequest<void>(`/api/admin/leave-types/${leaveTypeId}`, {
      method: 'DELETE',
    })
  },

  getEmployeeLeaveProfile(employeeId: string, periodYear?: number) {
    const query = periodYear ? `?periodYear=${encodeURIComponent(periodYear)}` : ''
    return apiRequest<EmployeeLeaveProfile>(`/api/admin/employees/${employeeId}/leave${query}`)
  },

  getPayrollSummary() {
    return apiRequest<PayrollDashboardSummary>('/api/admin/payroll/summary')
  },

  getPayrollOptions() {
    return apiRequest<PayrollOptions>('/api/admin/payroll/options')
  },

  getPayrollSetupSummary() {
    return apiRequest<PayrollSetupSummary>('/api/admin/payroll/setup/summary')
  },

  getPayrollSettings() {
    return apiRequest<PayrollSettings>('/api/admin/payroll/setup/settings')
  },

  updatePayrollSettings(input: PayrollSettings) {
    return apiRequest<PayrollSettings>('/api/admin/payroll/setup/settings', {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  getPayPeriodTemplates(query: PayrollSetupListQuery = {}) {
    return apiRequest<PagedResult<PayPeriodTemplateRecord>>(`/api/admin/payroll/setup/pay-period-templates${toQueryString(query)}`)
  },

  createPayPeriodTemplate(input: SavePayPeriodTemplateInput) {
    return apiRequest<PayPeriodTemplateRecord>('/api/admin/payroll/setup/pay-period-templates', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updatePayPeriodTemplate(payPeriodTemplateId: string, input: SavePayPeriodTemplateInput) {
    return apiRequest<PayPeriodTemplateRecord>(`/api/admin/payroll/setup/pay-period-templates/${payPeriodTemplateId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deletePayPeriodTemplate(payPeriodTemplateId: string) {
    return apiRequest<void>(`/api/admin/payroll/setup/pay-period-templates/${payPeriodTemplateId}`, {
      method: 'DELETE',
    })
  },

  getEarningTypes(query: PayrollSetupListQuery = {}) {
    return apiRequest<PagedResult<EarningTypeRecord>>(`/api/admin/payroll/setup/earning-types${toQueryString(query)}`)
  },

  createEarningType(input: SaveEarningTypeInput) {
    return apiRequest<EarningTypeRecord>('/api/admin/payroll/setup/earning-types', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateEarningType(earningTypeId: string, input: SaveEarningTypeInput) {
    return apiRequest<EarningTypeRecord>(`/api/admin/payroll/setup/earning-types/${earningTypeId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deleteEarningType(earningTypeId: string) {
    return apiRequest<void>(`/api/admin/payroll/setup/earning-types/${earningTypeId}`, {
      method: 'DELETE',
    })
  },

  getDeductionTypes(query: PayrollSetupListQuery = {}) {
    return apiRequest<PagedResult<DeductionTypeRecord>>(`/api/admin/payroll/setup/deduction-types${toQueryString(query)}`)
  },

  createDeductionType(input: SaveDeductionTypeInput) {
    return apiRequest<DeductionTypeRecord>('/api/admin/payroll/setup/deduction-types', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateDeductionType(deductionTypeId: string, input: SaveDeductionTypeInput) {
    return apiRequest<DeductionTypeRecord>(`/api/admin/payroll/setup/deduction-types/${deductionTypeId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deleteDeductionType(deductionTypeId: string) {
    return apiRequest<void>(`/api/admin/payroll/setup/deduction-types/${deductionTypeId}`, {
      method: 'DELETE',
    })
  },

  getContributionTypes(query: PayrollSetupListQuery = {}) {
    return apiRequest<PagedResult<ContributionTypeRecord>>(`/api/admin/payroll/setup/contribution-types${toQueryString(query)}`)
  },

  createContributionType(input: SaveContributionTypeInput) {
    return apiRequest<ContributionTypeRecord>('/api/admin/payroll/setup/contribution-types', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateContributionType(contributionTypeId: string, input: SaveContributionTypeInput) {
    return apiRequest<ContributionTypeRecord>(`/api/admin/payroll/setup/contribution-types/${contributionTypeId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deleteContributionType(contributionTypeId: string) {
    return apiRequest<void>(`/api/admin/payroll/setup/contribution-types/${contributionTypeId}`, {
      method: 'DELETE',
    })
  },

  getGovernmentContributionTables(query: PayrollSetupListQuery = {}) {
    return apiRequest<PagedResult<GovernmentContributionTableRecord>>(`/api/admin/payroll/setup/contribution-tables${toQueryString(query)}`)
  },

  createGovernmentContributionTable(input: SaveGovernmentContributionTableInput) {
    return apiRequest<GovernmentContributionTableRecord>('/api/admin/payroll/setup/contribution-tables', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateGovernmentContributionTable(contributionTableId: string, input: SaveGovernmentContributionTableInput) {
    return apiRequest<GovernmentContributionTableRecord>(`/api/admin/payroll/setup/contribution-tables/${contributionTableId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deleteGovernmentContributionTable(contributionTableId: string) {
    return apiRequest<void>(`/api/admin/payroll/setup/contribution-tables/${contributionTableId}`, {
      method: 'DELETE',
    })
  },

  getTaxTables(query: PayrollSetupListQuery = {}) {
    return apiRequest<PagedResult<TaxTableRecord>>(`/api/admin/payroll/setup/tax-tables${toQueryString(query)}`)
  },

  createTaxTable(input: SaveTaxTableInput) {
    return apiRequest<TaxTableRecord>('/api/admin/payroll/setup/tax-tables', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateTaxTable(taxTableId: string, input: SaveTaxTableInput) {
    return apiRequest<TaxTableRecord>(`/api/admin/payroll/setup/tax-tables/${taxTableId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deleteTaxTable(taxTableId: string) {
    return apiRequest<void>(`/api/admin/payroll/setup/tax-tables/${taxTableId}`, {
      method: 'DELETE',
    })
  },

  getCompensationProfiles(query: CompensationProfileListQuery = {}) {
    return apiRequest<PagedResult<CompensationProfileRecord>>(`/api/admin/payroll/compensation/profiles${toQueryString(query)}`)
  },

  createCompensationProfile(input: SaveCompensationProfileInput) {
    return apiRequest<CompensationProfileRecord>('/api/admin/payroll/compensation/profiles', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateCompensationProfile(compensationProfileId: string, input: SaveCompensationProfileInput) {
    return apiRequest<CompensationProfileRecord>(`/api/admin/payroll/compensation/profiles/${compensationProfileId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deleteCompensationProfile(compensationProfileId: string) {
    return apiRequest<void>(`/api/admin/payroll/compensation/profiles/${compensationProfileId}`, {
      method: 'DELETE',
    })
  },

  getRecurringEarnings(query: RecurringPayrollComponentListQuery = {}) {
    return apiRequest<PagedResult<EmployeeRecurringEarningRecord>>(`/api/admin/payroll/compensation/recurring-earnings${toQueryString(query)}`)
  },

  createRecurringEarning(input: SaveEmployeeRecurringEarningInput) {
    return apiRequest<EmployeeRecurringEarningRecord>('/api/admin/payroll/compensation/recurring-earnings', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateRecurringEarning(recurringEarningId: string, input: SaveEmployeeRecurringEarningInput) {
    return apiRequest<EmployeeRecurringEarningRecord>(`/api/admin/payroll/compensation/recurring-earnings/${recurringEarningId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deleteRecurringEarning(recurringEarningId: string) {
    return apiRequest<void>(`/api/admin/payroll/compensation/recurring-earnings/${recurringEarningId}`, {
      method: 'DELETE',
    })
  },

  getRecurringDeductions(query: RecurringPayrollComponentListQuery = {}) {
    return apiRequest<PagedResult<EmployeeRecurringDeductionRecord>>(`/api/admin/payroll/compensation/recurring-deductions${toQueryString(query)}`)
  },

  createRecurringDeduction(input: SaveEmployeeRecurringDeductionInput) {
    return apiRequest<EmployeeRecurringDeductionRecord>('/api/admin/payroll/compensation/recurring-deductions', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateRecurringDeduction(recurringDeductionId: string, input: SaveEmployeeRecurringDeductionInput) {
    return apiRequest<EmployeeRecurringDeductionRecord>(`/api/admin/payroll/compensation/recurring-deductions/${recurringDeductionId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deleteRecurringDeduction(recurringDeductionId: string) {
    return apiRequest<void>(`/api/admin/payroll/compensation/recurring-deductions/${recurringDeductionId}`, {
      method: 'DELETE',
    })
  },

  getEmployeePayrollProfile(employeeId: string) {
    return apiRequest<EmployeePayrollProfile>(`/api/admin/employees/${employeeId}/payroll`)
  },

  getPayPeriods(query: PayPeriodListQuery = {}) {
    return apiRequest<PagedResult<PayPeriodRecord>>(`/api/admin/payroll/pay-periods${toQueryString(query)}`)
  },

  createPayPeriod(input: SavePayPeriodInput) {
    return apiRequest<PayPeriodRecord>('/api/admin/payroll/pay-periods', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updatePayPeriod(payPeriodId: string, input: SavePayPeriodInput) {
    return apiRequest<PayPeriodRecord>(`/api/admin/payroll/pay-periods/${payPeriodId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deletePayPeriod(payPeriodId: string) {
    return apiRequest<void>(`/api/admin/payroll/pay-periods/${payPeriodId}`, {
      method: 'DELETE',
    })
  },

  getPayrollRuns(query: PayrollRunListQuery = {}) {
    return apiRequest<PagedResult<PayrollRunSummary>>(`/api/admin/payroll/runs${toQueryString(query)}`)
  },

  getPayrollRunById(payrollRunId: string) {
    return apiRequest<PayrollRunDetail>(`/api/admin/payroll/runs/${payrollRunId}`)
  },

  generatePayrollRun(input: GeneratePayrollRunInput) {
    return apiRequest<PayrollRunDetail>('/api/admin/payroll/runs/generate', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  recalculatePayrollRun(payrollRunId: string) {
    return apiRequest<PayrollRunDetail>(`/api/admin/payroll/runs/${payrollRunId}/recalculate`, {
      method: 'POST',
      body: JSON.stringify({ remarks: '' }),
    })
  },

  submitPayrollRunForReview(payrollRunId: string, input: PayrollRunActionInput) {
    return apiRequest<PayrollRunDetail>(`/api/admin/payroll/runs/${payrollRunId}/submit-review`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  approvePayrollRun(payrollRunId: string, input: PayrollRunActionInput) {
    return apiRequest<PayrollRunDetail>(`/api/admin/payroll/runs/${payrollRunId}/approve`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  markPayrollRunPaid(payrollRunId: string, input: PayrollRunActionInput) {
    return apiRequest<PayrollRunDetail>(`/api/admin/payroll/runs/${payrollRunId}/mark-paid`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  cancelPayrollRun(payrollRunId: string, input: PayrollRunActionInput) {
    return apiRequest<PayrollRunDetail>(`/api/admin/payroll/runs/${payrollRunId}/cancel`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  getPayrollAdjustments(query: PayrollAdjustmentListQuery = {}) {
    return apiRequest<PagedResult<PayrollAdjustmentRecord>>(`/api/admin/payroll/adjustments${toQueryString(query)}`)
  },

  createPayrollAdjustment(input: SavePayrollAdjustmentInput) {
    return apiRequest<PayrollAdjustmentRecord>('/api/admin/payroll/adjustments', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updatePayrollAdjustment(payrollAdjustmentId: string, input: SavePayrollAdjustmentInput) {
    return apiRequest<PayrollAdjustmentRecord>(`/api/admin/payroll/adjustments/${payrollAdjustmentId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deletePayrollAdjustment(payrollAdjustmentId: string) {
    return apiRequest<void>(`/api/admin/payroll/adjustments/${payrollAdjustmentId}`, {
      method: 'DELETE',
    })
  },

  approvePayrollAdjustment(payrollAdjustmentId: string, input: PayrollRunActionInput) {
    return apiRequest<PayrollAdjustmentRecord>(`/api/admin/payroll/adjustments/${payrollAdjustmentId}/approve`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  rejectPayrollAdjustment(payrollAdjustmentId: string, input: PayrollRunActionInput) {
    return apiRequest<PayrollAdjustmentRecord>(`/api/admin/payroll/adjustments/${payrollAdjustmentId}/reject`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  cancelPayrollAdjustment(payrollAdjustmentId: string, input: PayrollRunActionInput) {
    return apiRequest<PayrollAdjustmentRecord>(`/api/admin/payroll/adjustments/${payrollAdjustmentId}/cancel`, {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  getPayrollReports(query: PayrollReportQuery = {}) {
    return apiRequest<PayrollReports>(`/api/admin/payroll/reports${toQueryString(query)}`)
  },

  getProvidentFundOptions() {
    return apiRequest<ProvidentFundOptions>('/api/provident-fund/options')
  },

  getProvidentFundDashboard() {
    return apiRequest<ProvidentFundDashboard>('/api/provident-fund/dashboard')
  },

  getProvidentFundPolicies(query: ProvidentFundListQuery = {}) {
    return apiRequest<PagedResult<ProvidentFundPolicy>>(`/api/provident-fund/policies${toQueryString(query)}`)
  },

  createProvidentFundPolicy(input: ProvidentFundPolicyInput) {
    return apiRequest<ProvidentFundPolicy>('/api/provident-fund/policies', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateProvidentFundPolicy(policyId: string, input: ProvidentFundPolicyInput) {
    return apiRequest<ProvidentFundPolicy>(`/api/provident-fund/policies/${policyId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  getProvidentFundVestingRules(query: ProvidentFundListQuery = {}) {
    return apiRequest<PagedResult<ProvidentFundVestingRule>>(`/api/provident-fund/vesting-rules${toQueryString(query)}`)
  },

  createProvidentFundVestingRule(input: ProvidentFundVestingRuleInput) {
    return apiRequest<ProvidentFundVestingRule>('/api/provident-fund/vesting-rules', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateProvidentFundVestingRule(vestingRuleId: string, input: ProvidentFundVestingRuleInput) {
    return apiRequest<ProvidentFundVestingRule>(`/api/provident-fund/vesting-rules/${vestingRuleId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  deleteProvidentFundVestingRule(vestingRuleId: string) {
    return apiRequest<void>(`/api/provident-fund/vesting-rules/${vestingRuleId}`, {
      method: 'DELETE',
    })
  },

  getProvidentFundEnrollments(query: ProvidentFundListQuery = {}) {
    return apiRequest<PagedResult<ProvidentFundEnrollment>>(`/api/provident-fund/enrollments${toQueryString(query)}`)
  },

  createProvidentFundEnrollment(input: ProvidentFundEnrollmentInput) {
    return apiRequest<ProvidentFundEnrollment>('/api/provident-fund/enrollments', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  updateProvidentFundEnrollment(enrollmentId: string, input: ProvidentFundEnrollmentInput) {
    return apiRequest<ProvidentFundEnrollment>(`/api/provident-fund/enrollments/${enrollmentId}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    })
  },

  getProvidentFundContributionBatches(query: ProvidentFundListQuery = {}) {
    return apiRequest<PagedResult<ProvidentFundContributionBatch>>(`/api/provident-fund/contribution-batches${toQueryString(query)}`)
  },

  getProvidentFundContributionBatch(batchId: string) {
    return apiRequest<ProvidentFundContributionBatchDetail>(`/api/provident-fund/contribution-batches/${batchId}`)
  },

  generateProvidentFundContributionBatch(input: GenerateProvidentFundContributionBatchInput) {
    return apiRequest<ProvidentFundContributionBatchDetail>('/api/provident-fund/contribution-batches/generate', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  reviewProvidentFundContributionBatch(batchId: string, remarks = '') {
    return apiRequest<ProvidentFundContributionBatchDetail>(`/api/provident-fund/contribution-batches/${batchId}/review`, {
      method: 'POST',
      body: JSON.stringify({ remarks }),
    })
  },

  postProvidentFundContributionBatch(batchId: string, remarks = '') {
    return apiRequest<ProvidentFundContributionBatchDetail>(`/api/provident-fund/contribution-batches/${batchId}/post`, {
      method: 'POST',
      body: JSON.stringify({ remarks }),
    })
  },

  cancelProvidentFundContributionBatch(batchId: string, remarks = '') {
    return apiRequest<ProvidentFundContributionBatchDetail>(`/api/provident-fund/contribution-batches/${batchId}/cancel`, {
      method: 'POST',
      body: JSON.stringify({ remarks }),
    })
  },

  getProvidentFundLedger(query: ProvidentFundListQuery = {}) {
    return apiRequest<PagedResult<ProvidentFundLedgerTransaction>>(`/api/provident-fund/ledger${toQueryString(query)}`)
  },

  reverseProvidentFundLedger(ledgerTransactionId: string, remarks = '') {
    return apiRequest<ProvidentFundLedgerTransaction>(`/api/provident-fund/ledger/${ledgerTransactionId}/reverse`, {
      method: 'POST',
      body: JSON.stringify({ remarks }),
    })
  },

  getProvidentFundBalance(employeeId: string, asOfDate?: string) {
    return apiRequest<ProvidentFundBalance>(`/api/provident-fund/balances/${employeeId}${toQueryString({ asOfDate })}`)
  },

  getProvidentFundWithdrawals(query: ProvidentFundListQuery = {}) {
    return apiRequest<PagedResult<ProvidentFundWithdrawalRequest>>(`/api/provident-fund/withdrawals${toQueryString(query)}`)
  },

  createProvidentFundWithdrawal(input: ProvidentFundWithdrawalInput) {
    return apiRequest<ProvidentFundWithdrawalRequest>('/api/provident-fund/withdrawals', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  submitProvidentFundWithdrawal(withdrawalId: string, remarks = '') {
    return apiRequest<ProvidentFundWithdrawalRequest>(`/api/provident-fund/withdrawals/${withdrawalId}/submit`, {
      method: 'PUT',
      body: JSON.stringify({ remarks }),
    })
  },

  approveProvidentFundWithdrawal(withdrawalId: string, approvedAmount?: number, remarks = '', closeEnrollment = false) {
    return apiRequest<ProvidentFundWithdrawalRequest>(`/api/provident-fund/withdrawals/${withdrawalId}/approve`, {
      method: 'PUT',
      body: JSON.stringify({ approvedAmount, remarks, closeEnrollment }),
    })
  },

  rejectProvidentFundWithdrawal(withdrawalId: string, remarks = '') {
    return apiRequest<ProvidentFundWithdrawalRequest>(`/api/provident-fund/withdrawals/${withdrawalId}/reject`, {
      method: 'PUT',
      body: JSON.stringify({ remarks }),
    })
  },

  markProvidentFundWithdrawalPaid(withdrawalId: string, approvedAmount?: number, remarks = '', closeEnrollment = false) {
    return apiRequest<ProvidentFundWithdrawalRequest>(`/api/provident-fund/withdrawals/${withdrawalId}/mark-paid`, {
      method: 'PUT',
      body: JSON.stringify({ approvedAmount, remarks, closeEnrollment }),
    })
  },

  getProvidentFundAdjustments(query: ProvidentFundListQuery = {}) {
    return apiRequest<PagedResult<ProvidentFundAdjustment>>(`/api/provident-fund/adjustments${toQueryString(query)}`)
  },

  createProvidentFundAdjustment(input: ProvidentFundAdjustmentInput) {
    return apiRequest<ProvidentFundAdjustment>('/api/provident-fund/adjustments', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  approveProvidentFundAdjustment(adjustmentId: string, remarks = '') {
    return apiRequest<ProvidentFundAdjustment>(`/api/provident-fund/adjustments/${adjustmentId}/approve`, {
      method: 'PUT',
      body: JSON.stringify({ remarks }),
    })
  },

  rejectProvidentFundAdjustment(adjustmentId: string, remarks = '') {
    return apiRequest<ProvidentFundAdjustment>(`/api/provident-fund/adjustments/${adjustmentId}/reject`, {
      method: 'PUT',
      body: JSON.stringify({ remarks }),
    })
  },

  postProvidentFundAdjustment(adjustmentId: string, remarks = '') {
    return apiRequest<ProvidentFundAdjustment>(`/api/provident-fund/adjustments/${adjustmentId}/post`, {
      method: 'PUT',
      body: JSON.stringify({ remarks }),
    })
  },

  getProvidentFundContributionReport(query: ProvidentFundListQuery = {}) {
    return apiRequest<ProvidentFundContributionReportRow[]>(`/api/provident-fund/reports/contributions${toQueryString(query)}`)
  },

  getProvidentFundBalanceReport(query: ProvidentFundListQuery = {}) {
    return apiRequest<ProvidentFundBalanceReportRow[]>(`/api/provident-fund/reports/balances${toQueryString(query)}`)
  },

  getProvidentFundWithdrawalReport(query: ProvidentFundListQuery = {}) {
    return apiRequest<ProvidentFundWithdrawalReportRow[]>(`/api/provident-fund/reports/withdrawals${toQueryString(query)}`)
  },

  getProvidentFundLedgerReport(query: ProvidentFundListQuery = {}) {
    return apiRequest<ProvidentFundLedgerTransaction[]>(`/api/provident-fund/reports/ledger${toQueryString(query)}`)
  },

  getMyProvidentFund() {
    return apiRequest<ProvidentFundBalance>('/api/me/provident-fund')
  },

  getMyProvidentFundLedger(query: ProvidentFundListQuery = {}) {
    return apiRequest<PagedResult<ProvidentFundLedgerTransaction>>(`/api/me/provident-fund/ledger${toQueryString(query)}`)
  },

  getMyProvidentFundWithdrawals(query: ProvidentFundListQuery = {}) {
    return apiRequest<PagedResult<ProvidentFundWithdrawalRequest>>(`/api/me/provident-fund/withdrawals${toQueryString(query)}`)
  },

  createMyProvidentFundWithdrawal(input: ProvidentFundWithdrawalInput) {
    return apiRequest<ProvidentFundWithdrawalRequest>('/api/me/provident-fund/withdrawals', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },

  submitMyProvidentFundWithdrawal(withdrawalId: string, remarks = '') {
    return apiRequest<ProvidentFundWithdrawalRequest>(`/api/me/provident-fund/withdrawals/${withdrawalId}/submit`, {
      method: 'PUT',
      body: JSON.stringify({ remarks }),
    })
  },

  getPayslip(payrollRunItemId: string) {
    return apiRequest<Payslip>(`/api/admin/payroll/payslips/${payrollRunItemId}`)
  },
}

function toQueryString(query: object) {
  const searchParams = new URLSearchParams()

  Object.entries(query as Record<string, unknown>).forEach(([key, value]) => {
    if (value === undefined || value === null || value === '') {
      return
    }

    searchParams.set(key, String(value))
  })

  const queryString = searchParams.toString()
  return queryString ? `?${queryString}` : ''
}

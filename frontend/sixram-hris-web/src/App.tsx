import { lazy, Suspense } from 'react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import {
  RequireAdmin,
  RequireApprovalAccess,
  RequireAuditLogAccess,
  RequireAuth,
  RequireComplianceAccess,
  RequireEmployeeLink,
  RequireManager,
  RequireProvidentFundAccess,
  RequireReportsAccess,
} from './auth/guards'
import { AppLayout } from './layout/AppLayout'

const AnalyticsDashboardPage = lazy(() => import('./pages/AnalyticsDashboardPage').then((module) => ({ default: module.AnalyticsDashboardPage })))
const ApprovalCenterPage = lazy(() => import('./pages/ApprovalCenterPage').then((module) => ({ default: module.ApprovalCenterPage })))
const AuditLogPage = lazy(() => import('./pages/AuditLogPage').then((module) => ({ default: module.AuditLogPage })))
const AttendancePage = lazy(() => import('./pages/AttendancePage').then((module) => ({ default: module.AttendancePage })))
const AdminProvidentFundPage = lazy(() => import('./pages/AdminProvidentFundPage').then((module) => ({ default: module.AdminProvidentFundPage })))
const ComplianceCenterPage = lazy(() => import('./pages/ComplianceCenterPage').then((module) => ({ default: module.ComplianceCenterPage })))
const DocumentTypesPage = lazy(() => import('./pages/DocumentTypesPage').then((module) => ({ default: module.DocumentTypesPage })))
const EmployeeFormPage = lazy(() => import('./pages/EmployeeFormPage').then((module) => ({ default: module.EmployeeFormPage })))
const EmployeeDocumentsPage = lazy(() => import('./pages/EmployeeDocumentsPage').then((module) => ({ default: module.EmployeeDocumentsPage })))
const EmployeeProfilePage = lazy(() => import('./pages/EmployeeProfilePage').then((module) => ({ default: module.EmployeeProfilePage })))
const EmployeesPage = lazy(() => import('./pages/EmployeesPage').then((module) => ({ default: module.EmployeesPage })))
const HomePage = lazy(() => import('./pages/HomePage').then((module) => ({ default: module.HomePage })))
const LeaveCalendarPage = lazy(() => import('./pages/LeaveCalendarPage').then((module) => ({ default: module.LeaveCalendarPage })))
const LeaveManagementPage = lazy(() => import('./pages/LeaveManagementPage').then((module) => ({ default: module.LeaveManagementPage })))
const LeaveTypesPage = lazy(() => import('./pages/LeaveTypesPage').then((module) => ({ default: module.LeaveTypesPage })))
const LoginPage = lazy(() => import('./pages/LoginPage').then((module) => ({ default: module.LoginPage })))
const ManageRolesPage = lazy(() => import('./pages/ManageRolesPage').then((module) => ({ default: module.ManageRolesPage })))
const ManageUsersPage = lazy(() => import('./pages/ManageUsersPage').then((module) => ({ default: module.ManageUsersPage })))
const ManagerDashboardPage = lazy(() => import('./pages/ManagerDashboardPage').then((module) => ({ default: module.ManagerDashboardPage })))
const MyAttendancePage = lazy(() => import('./pages/MyAttendancePage').then((module) => ({ default: module.MyAttendancePage })))
const MyDocumentsPage = lazy(() => import('./pages/MyDocumentsPage').then((module) => ({ default: module.MyDocumentsPage })))
const MyLeavePage = lazy(() => import('./pages/MyLeavePage').then((module) => ({ default: module.MyLeavePage })))
const MyPayslipDetailPage = lazy(() => import('./pages/MyPayslipDetailPage').then((module) => ({ default: module.MyPayslipDetailPage })))
const MyPayslipsPage = lazy(() => import('./pages/MyPayslipsPage').then((module) => ({ default: module.MyPayslipsPage })))
const MyProvidentFundPage = lazy(() => import('./pages/MyProvidentFundPage').then((module) => ({ default: module.MyProvidentFundPage })))
const MyProfilePage = lazy(() => import('./pages/MyProfilePage').then((module) => ({ default: module.MyProfilePage })))
const MyRequestsPage = lazy(() => import('./pages/MyRequestsPage').then((module) => ({ default: module.MyRequestsPage })))
const MyTeamPage = lazy(() => import('./pages/MyTeamPage').then((module) => ({ default: module.MyTeamPage })))
const NotificationsPage = lazy(() => import('./pages/NotificationsPage').then((module) => ({ default: module.NotificationsPage })))
const OrganizationLookupPage = lazy(() => import('./pages/OrganizationLookupPage').then((module) => ({ default: module.OrganizationLookupPage })))
const OrganizationSetupPage = lazy(() => import('./pages/OrganizationSetupPage').then((module) => ({ default: module.OrganizationSetupPage })))
const PayrollCompensationPage = lazy(() => import('./pages/PayrollCompensationPage').then((module) => ({ default: module.PayrollCompensationPage })))
const PayrollDashboardPage = lazy(() => import('./pages/PayrollDashboardPage').then((module) => ({ default: module.PayrollDashboardPage })))
const PayrollPayslipPage = lazy(() => import('./pages/PayrollPayslipPage').then((module) => ({ default: module.PayrollPayslipPage })))
const PayrollReportsPage = lazy(() => import('./pages/PayrollReportsPage').then((module) => ({ default: module.PayrollReportsPage })))
const PayrollRunDetailPage = lazy(() => import('./pages/PayrollRunDetailPage').then((module) => ({ default: module.PayrollRunDetailPage })))
const PayrollSetupPage = lazy(() => import('./pages/PayrollSetupPage').then((module) => ({ default: module.PayrollSetupPage })))
const ProductionReadinessPage = lazy(() => import('./pages/ProductionReadinessPage').then((module) => ({ default: module.ProductionReadinessPage })))
const ReportDetailPage = lazy(() => import('./pages/ReportDetailPage').then((module) => ({ default: module.ReportDetailPage })))
const ReportsCenterPage = lazy(() => import('./pages/ReportsCenterPage').then((module) => ({ default: module.ReportsCenterPage })))
const RbacManagementPage = lazy(() => import('./pages/RbacManagementPage').then((module) => ({ default: module.RbacManagementPage })))
const ScheduleAssignmentsPage = lazy(() => import('./pages/ScheduleAssignmentsPage').then((module) => ({ default: module.ScheduleAssignmentsPage })))
const ShiftsPage = lazy(() => import('./pages/ShiftsPage').then((module) => ({ default: module.ShiftsPage })))
const TeamAttendancePage = lazy(() => import('./pages/TeamAttendancePage').then((module) => ({ default: module.TeamAttendancePage })))
const TeamLeavePage = lazy(() => import('./pages/TeamLeavePage').then((module) => ({ default: module.TeamLeavePage })))
const WorkSchedulesPage = lazy(() => import('./pages/WorkSchedulesPage').then((module) => ({ default: module.WorkSchedulesPage })))

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Suspense fallback={<RouteFallback />}>
          <Routes>
            <Route path="/login" element={<LoginPage />} />

            <Route element={<RequireAuth />}>
              <Route element={<AppLayout />}>
                <Route index element={<HomePage />} />

                <Route path="/notifications" element={<NotificationsPage />} />

                <Route element={<RequireReportsAccess />}>
                  <Route path="/analytics" element={<AnalyticsDashboardPage />} />
                  <Route path="/reports" element={<ReportsCenterPage />} />
                  <Route path="/reports/:reportKey" element={<ReportDetailPage />} />
                </Route>

                <Route element={<RequireComplianceAccess />}>
                  <Route path="/compliance" element={<ComplianceCenterPage />} />
                </Route>

                <Route element={<RequireAuditLogAccess />}>
                  <Route path="/audit-logs" element={<AuditLogPage />} />
                </Route>

                <Route element={<RequireEmployeeLink />}>
                  <Route path="/me/dashboard" element={<HomePage />} />
                  <Route path="/me/profile" element={<MyProfilePage />} />
                  <Route path="/me/attendance" element={<MyAttendancePage />} />
                  <Route path="/me/leave" element={<MyLeavePage />} />
                  <Route path="/me/documents" element={<MyDocumentsPage />} />
                  <Route path="/me/payslips" element={<MyPayslipsPage />} />
                  <Route path="/me/payslips/:payrollRunItemId" element={<MyPayslipDetailPage />} />
                  <Route path="/me/provident-fund" element={<MyProvidentFundPage />} />
                  <Route path="/me/requests" element={<MyRequestsPage />} />
                </Route>

                <Route element={<RequireManager />}>
                  <Route path="/manager" element={<ManagerDashboardPage />} />
                  <Route path="/manager/team" element={<MyTeamPage />} />
                  <Route path="/manager/attendance" element={<TeamAttendancePage />} />
                  <Route path="/manager/leave" element={<TeamLeavePage />} />
                </Route>

                <Route element={<RequireApprovalAccess />}>
                  <Route path="/approvals" element={<ApprovalCenterPage />} />
                </Route>

                <Route element={<RequireProvidentFundAccess />}>
                  <Route path="/admin/provident-fund" element={<AdminProvidentFundPage />} />
                  <Route path="/admin/provident-fund/:section" element={<AdminProvidentFundPage />} />
                </Route>

                <Route element={<RequireAdmin />}>
                  <Route path="/admin/employees" element={<EmployeesPage />} />
                  <Route path="/admin/employees/new" element={<EmployeeFormPage />} />
                  <Route path="/admin/employees/:employeeId" element={<EmployeeProfilePage />} />
                  <Route path="/admin/employees/:employeeId/edit" element={<EmployeeFormPage />} />
                  <Route path="/admin/documents" element={<EmployeeDocumentsPage />} />
                  <Route path="/admin/document-types" element={<DocumentTypesPage />} />
                  <Route path="/admin/leave" element={<LeaveManagementPage />} />
                  <Route path="/admin/leave/calendar" element={<LeaveCalendarPage />} />
                  <Route path="/admin/leave/types" element={<LeaveTypesPage />} />
                  <Route path="/admin/attendance" element={<AttendancePage />} />
                  <Route path="/admin/attendance/work-schedules" element={<WorkSchedulesPage />} />
                  <Route path="/admin/attendance/shifts" element={<ShiftsPage />} />
                  <Route path="/admin/attendance/assignments" element={<ScheduleAssignmentsPage />} />
                  <Route path="/admin/organization" element={<OrganizationSetupPage />} />
                  <Route path="/admin/payroll" element={<PayrollDashboardPage />} />
                  <Route path="/admin/payroll/setup" element={<PayrollSetupPage />} />
                  <Route path="/admin/payroll/compensation" element={<PayrollCompensationPage />} />
                  <Route path="/admin/payroll/runs/:payrollRunId" element={<PayrollRunDetailPage />} />
                  <Route path="/admin/payroll/payslips/:payrollRunItemId" element={<PayrollPayslipPage />} />
                  <Route path="/admin/payroll/reports" element={<PayrollReportsPage />} />
                  <Route path="/admin/production-readiness" element={<ProductionReadinessPage />} />
                  <Route
                    path="/admin/organization/departments"
                    element={
                      <OrganizationLookupPage
                        description="Maintain department setup records used across employee master profiles and future HR modules."
                        resource="departments"
                        title="Departments"
                      />
                    }
                  />
                  <Route
                    path="/admin/organization/positions"
                    element={
                      <OrganizationLookupPage
                        description="Maintain position and job title setup records with optional department assignment."
                        resource="positions"
                        title="Positions"
                      />
                    }
                  />
                  <Route
                    path="/admin/organization/branches"
                    element={
                      <OrganizationLookupPage
                        description="Maintain work site and branch records referenced by employee profiles."
                        resource="branches"
                        title="Branches / Locations"
                      />
                    }
                  />
                  <Route
                    path="/admin/organization/employment-types"
                    element={
                      <OrganizationLookupPage
                        description="Maintain employment type records such as regular, probationary, or contractual."
                        resource="employment-types"
                        title="Employment Types"
                      />
                    }
                  />
                  <Route
                    path="/admin/organization/employment-statuses"
                    element={
                      <OrganizationLookupPage
                        description="Maintain employment status records such as active, regularized, resigned, or terminated."
                        resource="employment-statuses"
                        title="Employment Statuses"
                      />
                    }
                  />
                  <Route path="/admin/users" element={<ManageUsersPage />} />
                  <Route path="/admin/roles" element={<ManageRolesPage />} />
                  <Route path="/admin/rbac" element={<RbacManagementPage />} />
                </Route>
              </Route>
            </Route>

            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </Suspense>
      </AuthProvider>
    </BrowserRouter>
  )
}

function RouteFallback() {
  return (
    <div className="min-h-screen bg-slate-50 px-6 py-10">
      <div className="mx-auto max-w-2xl rounded-2xl border border-slate-200 bg-white px-6 py-8 text-sm text-slate-500 shadow-sm">
        Loading workspace...
      </div>
    </div>
  )
}

export default App

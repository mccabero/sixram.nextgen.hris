import { lazy, type ReactElement } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
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
} from '../auth/guards'
import { AppLayout } from '../layout/AppLayout'

const AnalyticsDashboardPage = lazy(() => import('../pages/AnalyticsDashboardPage').then((module) => ({ default: module.AnalyticsDashboardPage })))
const ApprovalCenterPage = lazy(() => import('../pages/ApprovalCenterPage').then((module) => ({ default: module.ApprovalCenterPage })))
const AuditLogPage = lazy(() => import('../pages/AuditLogPage').then((module) => ({ default: module.AuditLogPage })))
const AttendancePage = lazy(() => import('../pages/AttendancePage').then((module) => ({ default: module.AttendancePage })))
const AdminProvidentFundPage = lazy(() => import('../pages/AdminProvidentFundPage').then((module) => ({ default: module.AdminProvidentFundPage })))
const ComplianceCenterPage = lazy(() => import('../pages/ComplianceCenterPage').then((module) => ({ default: module.ComplianceCenterPage })))
const DocumentTypesPage = lazy(() => import('../pages/DocumentTypesPage').then((module) => ({ default: module.DocumentTypesPage })))
const EmployeeFormPage = lazy(() => import('../pages/EmployeeFormPage').then((module) => ({ default: module.EmployeeFormPage })))
const EmployeeDocumentsPage = lazy(() => import('../pages/EmployeeDocumentsPage').then((module) => ({ default: module.EmployeeDocumentsPage })))
const EmployeeProfilePage = lazy(() => import('../pages/EmployeeProfilePage').then((module) => ({ default: module.EmployeeProfilePage })))
const EmployeesPage = lazy(() => import('../pages/EmployeesPage').then((module) => ({ default: module.EmployeesPage })))
const HomePage = lazy(() => import('../pages/HomePage').then((module) => ({ default: module.HomePage })))
const LeaveCalendarPage = lazy(() => import('../pages/LeaveCalendarPage').then((module) => ({ default: module.LeaveCalendarPage })))
const LeaveManagementPage = lazy(() => import('../pages/LeaveManagementPage').then((module) => ({ default: module.LeaveManagementPage })))
const LeaveTypesPage = lazy(() => import('../pages/LeaveTypesPage').then((module) => ({ default: module.LeaveTypesPage })))
const LoginPage = lazy(() => import('../pages/LoginPage').then((module) => ({ default: module.LoginPage })))
const ManageRolesPage = lazy(() => import('../pages/ManageRolesPage').then((module) => ({ default: module.ManageRolesPage })))
const ManageUsersPage = lazy(() => import('../pages/ManageUsersPage').then((module) => ({ default: module.ManageUsersPage })))
const ManagerDashboardPage = lazy(() => import('../pages/ManagerDashboardPage').then((module) => ({ default: module.ManagerDashboardPage })))
const MyAttendancePage = lazy(() => import('../pages/MyAttendancePage').then((module) => ({ default: module.MyAttendancePage })))
const MyDocumentsPage = lazy(() => import('../pages/MyDocumentsPage').then((module) => ({ default: module.MyDocumentsPage })))
const MyLeavePage = lazy(() => import('../pages/MyLeavePage').then((module) => ({ default: module.MyLeavePage })))
const MyPayslipDetailPage = lazy(() => import('../pages/MyPayslipDetailPage').then((module) => ({ default: module.MyPayslipDetailPage })))
const MyPayslipsPage = lazy(() => import('../pages/MyPayslipsPage').then((module) => ({ default: module.MyPayslipsPage })))
const MyProvidentFundPage = lazy(() => import('../pages/MyProvidentFundPage').then((module) => ({ default: module.MyProvidentFundPage })))
const MyProfilePage = lazy(() => import('../pages/MyProfilePage').then((module) => ({ default: module.MyProfilePage })))
const MyRequestsPage = lazy(() => import('../pages/MyRequestsPage').then((module) => ({ default: module.MyRequestsPage })))
const MyTeamPage = lazy(() => import('../pages/MyTeamPage').then((module) => ({ default: module.MyTeamPage })))
const NotificationsPage = lazy(() => import('../pages/NotificationsPage').then((module) => ({ default: module.NotificationsPage })))
const OrganizationLookupPage = lazy(() => import('../pages/OrganizationLookupPage').then((module) => ({ default: module.OrganizationLookupPage })))
const OrganizationSetupPage = lazy(() => import('../pages/OrganizationSetupPage').then((module) => ({ default: module.OrganizationSetupPage })))
const PayrollCompensationPage = lazy(() => import('../pages/PayrollCompensationPage').then((module) => ({ default: module.PayrollCompensationPage })))
const PayrollDashboardPage = lazy(() => import('../pages/PayrollDashboardPage').then((module) => ({ default: module.PayrollDashboardPage })))
const PayrollPayslipPage = lazy(() => import('../pages/PayrollPayslipPage').then((module) => ({ default: module.PayrollPayslipPage })))
const PayrollReportsPage = lazy(() => import('../pages/PayrollReportsPage').then((module) => ({ default: module.PayrollReportsPage })))
const PayrollRunDetailPage = lazy(() => import('../pages/PayrollRunDetailPage').then((module) => ({ default: module.PayrollRunDetailPage })))
const PayrollSetupPage = lazy(() => import('../pages/PayrollSetupPage').then((module) => ({ default: module.PayrollSetupPage })))
const ProductionReadinessPage = lazy(() => import('../pages/ProductionReadinessPage').then((module) => ({ default: module.ProductionReadinessPage })))
const ReportDetailPage = lazy(() => import('../pages/ReportDetailPage').then((module) => ({ default: module.ReportDetailPage })))
const ReportsCenterPage = lazy(() => import('../pages/ReportsCenterPage').then((module) => ({ default: module.ReportsCenterPage })))
const RbacManagementPage = lazy(() => import('../pages/RbacManagementPage').then((module) => ({ default: module.RbacManagementPage })))
const ScheduleAssignmentsPage = lazy(() => import('../pages/ScheduleAssignmentsPage').then((module) => ({ default: module.ScheduleAssignmentsPage })))
const ShiftsPage = lazy(() => import('../pages/ShiftsPage').then((module) => ({ default: module.ShiftsPage })))
const TeamAttendancePage = lazy(() => import('../pages/TeamAttendancePage').then((module) => ({ default: module.TeamAttendancePage })))
const TeamLeavePage = lazy(() => import('../pages/TeamLeavePage').then((module) => ({ default: module.TeamLeavePage })))
const WorkSchedulesPage = lazy(() => import('../pages/WorkSchedulesPage').then((module) => ({ default: module.WorkSchedulesPage })))

type RouteDefinition = {
  path?: string
  index?: boolean
  element: ReactElement
}

type GuardedRouteGroup = {
  guard: ReactElement
  routes: RouteDefinition[]
}

const publicRoutes: RouteDefinition[] = [
  { path: '/login', element: <LoginPage /> },
]

const unguardedWorkspaceRoutes: RouteDefinition[] = [
  { index: true, element: <HomePage /> },
  { path: '/notifications', element: <NotificationsPage /> },
]

const guardedWorkspaceRoutes: GuardedRouteGroup[] = [
  {
    guard: <RequireReportsAccess />,
    routes: [
      { path: '/analytics', element: <AnalyticsDashboardPage /> },
      { path: '/reports', element: <ReportsCenterPage /> },
      { path: '/reports/:reportKey', element: <ReportDetailPage /> },
    ],
  },
  {
    guard: <RequireComplianceAccess />,
    routes: [{ path: '/compliance', element: <ComplianceCenterPage /> }],
  },
  {
    guard: <RequireAuditLogAccess />,
    routes: [{ path: '/audit-logs', element: <AuditLogPage /> }],
  },
  {
    guard: <RequireEmployeeLink />,
    routes: [
      { path: '/me/dashboard', element: <HomePage /> },
      { path: '/me/profile', element: <MyProfilePage /> },
      { path: '/me/attendance', element: <MyAttendancePage /> },
      { path: '/me/leave', element: <MyLeavePage /> },
      { path: '/me/documents', element: <MyDocumentsPage /> },
      { path: '/me/payslips', element: <MyPayslipsPage /> },
      { path: '/me/payslips/:payrollRunItemId', element: <MyPayslipDetailPage /> },
      { path: '/me/provident-fund', element: <MyProvidentFundPage /> },
      { path: '/me/requests', element: <MyRequestsPage /> },
    ],
  },
  {
    guard: <RequireManager />,
    routes: [
      { path: '/manager', element: <ManagerDashboardPage /> },
      { path: '/manager/team', element: <MyTeamPage /> },
      { path: '/manager/attendance', element: <TeamAttendancePage /> },
      { path: '/manager/leave', element: <TeamLeavePage /> },
    ],
  },
  {
    guard: <RequireApprovalAccess />,
    routes: [{ path: '/approvals', element: <ApprovalCenterPage /> }],
  },
  {
    guard: <RequireProvidentFundAccess />,
    routes: [
      { path: '/admin/provident-fund', element: <AdminProvidentFundPage /> },
      { path: '/admin/provident-fund/:section', element: <AdminProvidentFundPage /> },
    ],
  },
  {
    guard: <RequireAdmin />,
    routes: [
      { path: '/admin/employees', element: <EmployeesPage /> },
      { path: '/admin/employees/new', element: <EmployeeFormPage /> },
      { path: '/admin/employees/:employeeId', element: <EmployeeProfilePage /> },
      { path: '/admin/employees/:employeeId/edit', element: <EmployeeFormPage /> },
      { path: '/admin/documents', element: <EmployeeDocumentsPage /> },
      { path: '/admin/document-types', element: <DocumentTypesPage /> },
      { path: '/admin/leave', element: <LeaveManagementPage /> },
      { path: '/admin/leave/calendar', element: <LeaveCalendarPage /> },
      { path: '/admin/leave/types', element: <LeaveTypesPage /> },
      { path: '/admin/attendance', element: <AttendancePage /> },
      { path: '/admin/attendance/work-schedules', element: <WorkSchedulesPage /> },
      { path: '/admin/attendance/shifts', element: <ShiftsPage /> },
      { path: '/admin/attendance/assignments', element: <ScheduleAssignmentsPage /> },
      { path: '/admin/organization', element: <OrganizationSetupPage /> },
      { path: '/admin/payroll', element: <PayrollDashboardPage /> },
      { path: '/admin/payroll/setup', element: <PayrollSetupPage /> },
      { path: '/admin/payroll/compensation', element: <PayrollCompensationPage /> },
      { path: '/admin/payroll/runs/:payrollRunId', element: <PayrollRunDetailPage /> },
      { path: '/admin/payroll/payslips/:payrollRunItemId', element: <PayrollPayslipPage /> },
      { path: '/admin/payroll/reports', element: <PayrollReportsPage /> },
      { path: '/admin/production-readiness', element: <ProductionReadinessPage /> },
      {
        path: '/admin/organization/departments',
        element: (
          <OrganizationLookupPage
            description="Maintain department setup records used across employee master profiles and future HR modules."
            resource="departments"
            title="Departments"
          />
        ),
      },
      {
        path: '/admin/organization/positions',
        element: (
          <OrganizationLookupPage
            description="Maintain position and job title setup records with optional department assignment."
            resource="positions"
            title="Positions"
          />
        ),
      },
      {
        path: '/admin/organization/branches',
        element: (
          <OrganizationLookupPage
            description="Maintain work site and branch records referenced by employee profiles."
            resource="branches"
            title="Branches / Locations"
          />
        ),
      },
      {
        path: '/admin/organization/employment-types',
        element: (
          <OrganizationLookupPage
            description="Maintain employment type records such as regular, probationary, or contractual."
            resource="employment-types"
            title="Employment Types"
          />
        ),
      },
      {
        path: '/admin/organization/employment-statuses',
        element: (
          <OrganizationLookupPage
            description="Maintain employment status records such as active, regularized, resigned, or terminated."
            resource="employment-statuses"
            title="Employment Statuses"
          />
        ),
      },
      { path: '/admin/users', element: <ManageUsersPage /> },
      { path: '/admin/roles', element: <ManageRolesPage /> },
      { path: '/admin/rbac', element: <RbacManagementPage /> },
    ],
  },
]

export function AppRoutes() {
  return (
    <Routes>
      {renderRoutes(publicRoutes)}

      <Route element={<RequireAuth />}>
        <Route element={<AppLayout />}>
          {renderRoutes(unguardedWorkspaceRoutes)}
          {guardedWorkspaceRoutes.map((group, index) => (
            <Route element={group.guard} key={`guard-${index}`}>
              {renderRoutes(group.routes)}
            </Route>
          ))}
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

function renderRoutes(routes: RouteDefinition[]) {
  return routes.map((route) =>
    route.index ? (
      <Route element={route.element} index key="index" />
    ) : (
      <Route element={route.element} key={route.path} path={route.path} />
    ),
  )
}

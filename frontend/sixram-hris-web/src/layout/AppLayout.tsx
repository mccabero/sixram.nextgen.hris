/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useMemo, useState, type ReactElement, type SVGProps } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import { useAuth } from '../auth/AuthContext'
import type { NotificationSummary, UserNotification } from '../types/models'
import { formatDateTime } from '../utils/date'

type IconProps = SVGProps<SVGSVGElement>

type NavigationItem = {
  to: string
  label: string
}

type NavigationGroup = {
  key: string
  label: string
  icon: (props: IconProps) => ReactElement
  items: NavigationItem[]
}

type PageMeta = {
  section: string
  title: string
  subtitle: string
}

type Breadcrumb = {
  label: string
  to?: string
}

function DashboardIcon(props: IconProps) {
  return (
    <svg aria-hidden="true" fill="none" viewBox="0 0 24 24" {...props}>
      <path
        d="M4.75 5.75C4.75 5.198 5.198 4.75 5.75 4.75H10.25C10.802 4.75 11.25 5.198 11.25 5.75V10.25C11.25 10.802 10.802 11.25 10.25 11.25H5.75C5.198 11.25 4.75 10.802 4.75 10.25V5.75ZM12.75 5.75C12.75 5.198 13.198 4.75 13.75 4.75H18.25C18.802 4.75 19.25 5.198 19.25 5.75V8.25C19.25 8.802 18.802 9.25 18.25 9.25H13.75C13.198 9.25 12.75 8.802 12.75 8.25V5.75ZM4.75 13.75C4.75 13.198 5.198 12.75 5.75 12.75H8.25C8.802 12.75 9.25 13.198 9.25 13.75V18.25C9.25 18.802 8.802 19.25 8.25 19.25H5.75C5.198 19.25 4.75 18.802 4.75 18.25V13.75ZM12.75 11.75C12.75 11.198 13.198 10.75 13.75 10.75H18.25C18.802 10.75 19.25 11.198 19.25 11.75V18.25C19.25 18.802 18.802 19.25 18.25 19.25H13.75C13.198 19.25 12.75 18.802 12.75 18.25V11.75Z"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="1.8"
      />
    </svg>
  )
}

function PeopleIcon(props: IconProps) {
  return (
    <svg aria-hidden="true" fill="none" viewBox="0 0 24 24" {...props}>
      <path
        d="M8.75 10.25C10.1307 10.25 11.25 9.13071 11.25 7.75C11.25 6.36929 10.1307 5.25 8.75 5.25C7.36929 5.25 6.25 6.36929 6.25 7.75C6.25 9.13071 7.36929 10.25 8.75 10.25ZM15.75 11.75C17.1307 11.75 18.25 10.6307 18.25 9.25C18.25 7.86929 17.1307 6.75 15.75 6.75C14.3693 6.75 13.25 7.86929 13.25 9.25C13.25 10.6307 14.3693 11.75 15.75 11.75ZM4.75 18.25C4.75 15.9028 6.65279 14 9 14H10.5C12.8472 14 14.75 15.9028 14.75 18.25M13.75 18.25C13.75 16.5931 15.0931 15.25 16.75 15.25H17.25C18.9069 15.25 20.25 16.5931 20.25 18.25"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="1.8"
      />
    </svg>
  )
}

function ShieldIcon(props: IconProps) {
  return (
    <svg aria-hidden="true" fill="none" viewBox="0 0 24 24" {...props}>
      <path
        d="M12 4.75L18.25 7.25V11.75C18.25 15.7561 15.4144 19.203 12 20.25C8.58563 19.203 5.75 15.7561 5.75 11.75V7.25L12 4.75ZM9.75 12.25L11.25 13.75L14.75 10.25"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="1.8"
      />
    </svg>
  )
}

function WalletIcon(props: IconProps) {
  return (
    <svg aria-hidden="true" fill="none" viewBox="0 0 24 24" {...props}>
      <path
        d="M5.75 7.75H18.25C18.8023 7.75 19.25 8.19772 19.25 8.75V16.25C19.25 16.8023 18.8023 17.25 18.25 17.25H5.75C5.19772 17.25 4.75 16.8023 4.75 16.25V8.75C4.75 8.19772 5.19772 7.75 5.75 7.75ZM4.75 9.75H14.25C14.8023 9.75 15.25 10.1977 15.25 10.75V13.25C15.25 13.8023 14.8023 14.25 14.25 14.25H4.75M16.75 12H16.76"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="1.8"
      />
      <path d="M6.75 7.75V6.75C6.75 5.92157 7.42157 5.25 8.25 5.25H17.25" stroke="currentColor" strokeLinecap="round" strokeWidth="1.8" />
    </svg>
  )
}

function BellIcon(props: IconProps) {
  return (
    <svg aria-hidden="true" fill="none" viewBox="0 0 24 24" {...props}>
      <path
        d="M9.75 18.25H14.25M6.75 16.25V11C6.75 8.10051 9.10051 5.75 12 5.75C14.8995 5.75 17.25 8.10051 17.25 11V16.25L18.25 17.25V18.25H5.75V17.25L6.75 16.25Z"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="1.8"
      />
    </svg>
  )
}

function MenuIcon(props: IconProps) {
  return (
    <svg aria-hidden="true" fill="none" viewBox="0 0 24 24" {...props}>
      <path d="M4.75 7.75H19.25M4.75 12H19.25M4.75 16.25H13.25" stroke="currentColor" strokeLinecap="round" strokeWidth="1.8" />
    </svg>
  )
}

function CloseIcon(props: IconProps) {
  return (
    <svg aria-hidden="true" fill="none" viewBox="0 0 24 24" {...props}>
      <path d="M7.75 7.75L16.25 16.25M16.25 7.75L7.75 16.25" stroke="currentColor" strokeLinecap="round" strokeWidth="1.8" />
    </svg>
  )
}

function ChevronDownIcon(props: IconProps) {
  return (
    <svg aria-hidden="true" fill="none" viewBox="0 0 24 24" {...props}>
      <path d="M7.75 9.75L12 14L16.25 9.75" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.8" />
    </svg>
  )
}

function LogoutIcon(props: IconProps) {
  return (
    <svg aria-hidden="true" fill="none" viewBox="0 0 24 24" {...props}>
      <path
        d="M14.75 7.75L19 12L14.75 16.25M19 12H10.25M10.25 5.75H8C6.75736 5.75 5.75 6.75736 5.75 8V16C5.75 17.2426 6.75736 18.25 8 18.25H10.25"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="1.8"
      />
    </svg>
  )
}

function getDefaultExpanded(pathname: string) {
  return {
    home: pathname === '/' || pathname.startsWith('/notifications'),
    insights:
      pathname.startsWith('/analytics') ||
      pathname.startsWith('/reports') ||
      pathname.startsWith('/compliance') ||
      pathname.startsWith('/audit-logs'),
    employee: pathname.startsWith('/me/'),
    manager: pathname.startsWith('/manager'),
    approvals: pathname.startsWith('/approvals'),
    workforce:
      pathname.startsWith('/admin/employees') ||
      pathname.startsWith('/admin/attendance') ||
      pathname.startsWith('/admin/leave') ||
      pathname.startsWith('/admin/documents') ||
      pathname.startsWith('/admin/document-types') ||
      pathname.startsWith('/admin/organization') ||
      pathname.startsWith('/admin/production-readiness'),
    payroll: pathname.startsWith('/admin/payroll'),
    providentFund: pathname.startsWith('/admin/provident-fund'),
    security:
      pathname.startsWith('/admin/users') ||
      pathname.startsWith('/admin/roles') ||
      pathname.startsWith('/admin/rbac'),
  }
}

function isGroupActive(group: NavigationGroup, pathname: string) {
  return group.items.some((item) => pathname === item.to || pathname.startsWith(`${item.to}/`))
}

function resolvePageMeta(pathname: string, isAdmin: boolean, hasLinkedEmployee: boolean, isManager: boolean): PageMeta {
  if (pathname === '/') {
    if (isAdmin) {
      return {
        section: 'Dashboard',
        title: 'Dashboard',
        subtitle: 'Monitor HR operations, review activity across modules, and jump into administrator workflows.',
      }
    }

    if (hasLinkedEmployee) {
      return {
        section: 'Employee Portal',
        title: 'My Dashboard',
        subtitle: 'See your attendance, leave, documents, payslips, and self-service updates from one place.',
      }
    }

    if (isManager) {
      return {
        section: 'Manager Portal',
        title: 'Workspace',
        subtitle: 'Use the approval center and manager tools available to your account.',
      }
    }
  }

  if (pathname === '/notifications') {
    return {
      section: 'Updates',
      title: 'Notifications',
      subtitle: 'Review request updates, approval activity, and employee portal alerts.',
    }
  }

  if (pathname === '/analytics') {
    return {
      section: 'Insights',
      title: 'Analytics Dashboard',
      subtitle: 'Monitor headcount, attendance, leave, compliance, approvals, and payroll movement from one summary workspace.',
    }
  }

  if (pathname === '/reports') {
    return {
      section: 'Insights',
      title: 'Reports Center',
      subtitle: 'Browse permission-aware employee, attendance, leave, payroll, approval, and audit reports.',
    }
  }

  if (pathname.startsWith('/reports/')) {
    return {
      section: 'Insights',
      title: 'Report Detail',
      subtitle: 'Apply filters, review metrics, export data, and save reusable report views.',
    }
  }

  if (pathname === '/compliance') {
    return {
      section: 'Insights',
      title: 'Compliance Center',
      subtitle: 'Track missing requirements, expiring documents, incomplete data, and operational readiness issues.',
    }
  }

  if (pathname === '/audit-logs') {
    return {
      section: 'Insights',
      title: 'Audit Trail',
      subtitle: 'Review sensitive system activity with server-side redaction and permission-aware visibility.',
    }
  }

  if (pathname === '/approvals') {
    return {
      section: 'Approvals',
      title: 'Approval Center',
      subtitle: 'Review pending requests, compare details, and record approval decisions with remarks.',
    }
  }

  if (pathname === '/manager') {
    return {
      section: 'Manager Portal',
      title: 'Team Dashboard',
      subtitle: 'Track team attendance, upcoming leave, and pending approvals for your direct reports.',
    }
  }

  if (pathname.startsWith('/manager/team')) {
    return {
      section: 'Manager Portal',
      title: 'My Team',
      subtitle: 'Review direct reports, current attendance status, and limited employee profile details within your scope.',
    }
  }

  if (pathname.startsWith('/manager/attendance')) {
    return {
      section: 'Manager Portal',
      title: 'Team Attendance',
      subtitle: 'Filter attendance records for your direct reports and review attendance issues quickly.',
    }
  }

  if (pathname.startsWith('/manager/leave')) {
    return {
      section: 'Manager Portal',
      title: 'Team Leave',
      subtitle: 'Review leave requests, see the team leave calendar, and act on manager approvals.',
    }
  }

  if (pathname === '/me/dashboard') {
    return {
      section: 'Employee Portal',
      title: 'My Dashboard',
      subtitle: 'See your attendance, leave balances, documents, and latest payroll visibility in one view.',
    }
  }

  if (pathname.startsWith('/me/profile')) {
    return {
      section: 'Employee Portal',
      title: 'My Profile',
      subtitle: 'Review personal and employment details and submit profile change requests for HR review.',
    }
  }

  if (pathname.startsWith('/me/attendance')) {
    return {
      section: 'Employee Portal',
      title: 'My Attendance',
      subtitle: 'Review attendance history and submit correction requests when a log needs follow-up.',
    }
  }

  if (pathname.startsWith('/me/leave')) {
    return {
      section: 'Employee Portal',
      title: 'My Leave',
      subtitle: 'Track leave balances, submit leave requests, and monitor approvals and history.',
    }
  }

  if (pathname.startsWith('/me/documents')) {
    return {
      section: 'Employee Portal',
      title: 'My Documents',
      subtitle: 'Review your official document library and any compliance gaps flagged by HR.',
    }
  }

  if (pathname.startsWith('/me/payslips/')) {
    return {
      section: 'Employee Portal',
      title: 'My Payslip',
      subtitle: 'View the payroll snapshot that is visible to your employee account.',
    }
  }

  if (pathname.startsWith('/me/payslips')) {
    return {
      section: 'Employee Portal',
      title: 'My Payslips',
      subtitle: 'Review approved payroll history and open visible payslips for printing.',
    }
  }

  if (pathname.startsWith('/me/requests')) {
    return {
      section: 'Employee Portal',
      title: 'My Requests',
      subtitle: 'See the status of leave, attendance, and profile requests in one timeline.',
    }
  }

  if (pathname.startsWith('/me/provident-fund')) {
    return {
      section: 'Employee Portal',
      title: 'My Provident Fund',
      subtitle: 'Review your provident fund balance, contribution history, and withdrawal requests.',
    }
  }

  if (pathname.startsWith('/admin/employees/') && pathname.endsWith('/edit')) {
    return {
      section: 'Workforce',
      title: 'Edit Employee Profile',
      subtitle: 'Update the employee master record, assignments, compliance identifiers, linked account, and related document readiness.',
    }
  }

  if (pathname.startsWith('/admin/employees/')) {
    return {
      section: 'Workforce',
      title: 'Employee Profile',
      subtitle: 'Review the full employee master profile, organization placement, linked system details, and employee documents.',
    }
  }

  if (pathname.startsWith('/admin/payroll/runs/')) {
    return {
      section: 'Payroll',
      title: 'Payroll Run Detail',
      subtitle: 'Inspect employee payroll items, issue flags, audit activity, and payslip access for the selected run.',
    }
  }

  if (pathname.startsWith('/admin/payroll/payslips/')) {
    return {
      section: 'Payroll',
      title: 'Payslip',
      subtitle: 'Review and print the generated payroll snapshot for one employee and pay period.',
    }
  }

  if (pathname.startsWith('/admin/provident-fund')) {
    return {
      section: 'Provident Fund',
      title: 'Provident Fund Management',
      subtitle: 'Manage policies, enrollments, ledger-backed contribution posting, withdrawals, adjustments, and fund reports.',
    }
  }

  const pageTitles: Record<string, PageMeta> = {
    '/admin/employees': {
      section: 'Workforce',
      title: 'Employee Master Profiles',
      subtitle: 'Search, review, and maintain employee master records with organization and compliance details.',
    },
    '/admin/employees/new': {
      section: 'Workforce',
      title: 'Create Employee Profile',
      subtitle: 'Add a new employee master record and link it to the organization setup tables.',
    },
    '/admin/attendance': {
      section: 'Workforce',
      title: 'Attendance Records',
      subtitle: 'Track daily attendance, manual corrections, and operational attendance reporting for the workforce.',
    },
    '/admin/attendance/work-schedules': {
      section: 'Workforce',
      title: 'Work Schedules',
      subtitle: 'Maintain reusable attendance policy templates with grace periods, work minutes, and break rules.',
    },
    '/admin/attendance/shifts': {
      section: 'Workforce',
      title: 'Shifts',
      subtitle: 'Maintain shift windows, overnight handling, and break-time definitions for assigned employees.',
    },
    '/admin/attendance/assignments': {
      section: 'Workforce',
      title: 'Schedule Assignments',
      subtitle: 'Assign work schedules and shifts to employees with effective dates and rest-day configuration.',
    },
    '/admin/leave': {
      section: 'Workforce',
      title: 'Leave Management',
      subtitle: 'Review leave requests, adjust balances, and keep attendance and leave data aligned for HR operations.',
    },
    '/admin/leave/calendar': {
      section: 'Workforce',
      title: 'Leave Calendar',
      subtitle: 'Review approved and pending leaves in a monthly planning view for staffing visibility.',
    },
    '/admin/leave/types': {
      section: 'Workforce',
      title: 'Leave Types',
      subtitle: 'Maintain leave categories, filing rules, and annual credit defaults for the leave module.',
    },
    '/admin/documents': {
      section: 'Workforce',
      title: 'Employee Documents',
      subtitle: 'Track employee files, expiry status, and required-document compliance across the organization.',
    },
    '/admin/document-types': {
      section: 'Workforce',
      title: 'Document Types',
      subtitle: 'Maintain document categories, expiry rules, and required-document flags for employee records.',
    },
    '/admin/organization': {
      section: 'Workforce',
      title: 'Organization Setup',
      subtitle: 'Maintain departments, positions, branches, and employment reference tables for the HR foundation.',
    },
    '/admin/production-readiness': {
      section: 'Workforce',
      title: 'Production Readiness',
      subtitle: 'Review go-live readiness, import validated master data, and confirm the safeguards that protect operations in production.',
    },
    '/admin/organization/departments': {
      section: 'Workforce',
      title: 'Departments',
      subtitle: 'Maintain department records used by employee master profiles and downstream HR modules.',
    },
    '/admin/organization/positions': {
      section: 'Workforce',
      title: 'Positions',
      subtitle: 'Maintain position and job title records, with optional department ownership.',
    },
    '/admin/organization/branches': {
      section: 'Workforce',
      title: 'Branches / Locations',
      subtitle: 'Maintain branch and location records assigned to employees.',
    },
    '/admin/organization/employment-types': {
      section: 'Workforce',
      title: 'Employment Types',
      subtitle: 'Maintain the catalog of employee engagement types such as regular or contractual.',
    },
    '/admin/organization/employment-statuses': {
      section: 'Workforce',
      title: 'Employment Statuses',
      subtitle: 'Maintain operational employment statuses such as active, resigned, or terminated.',
    },
    '/admin/payroll': {
      section: 'Payroll',
      title: 'Payroll Dashboard',
      subtitle: 'Prepare pay periods, review payroll runs, manage adjustments, and move payroll through the approval flow.',
    },
    '/admin/payroll/setup': {
      section: 'Payroll',
      title: 'Payroll Setup',
      subtitle: 'Maintain payroll defaults, setup types, contribution tables, and tax tables without code changes.',
    },
    '/admin/payroll/compensation': {
      section: 'Payroll',
      title: 'Compensation Management',
      subtitle: 'Maintain compensation history plus recurring payroll earnings and deductions per employee.',
    },
    '/admin/payroll/reports': {
      section: 'Payroll',
      title: 'Payroll Reports',
      subtitle: 'Review payroll register data, grouped totals, payroll adjustments, and component summaries.',
    },
    '/admin/users': {
      section: 'Security',
      title: 'Manage User Accounts',
      subtitle: 'Create accounts, maintain status, reset passwords, and manage user role assignments.',
    },
    '/admin/roles': {
      section: 'Security',
      title: 'Manage Roles',
      subtitle: 'Maintain reusable authorization roles consumed by the API and UI route guards.',
    },
    '/admin/rbac': {
      section: 'Security',
      title: 'RBAC Management',
      subtitle: 'Review the live role-to-user matrix and update assignments from one admin view.',
    },
  }

  return pageTitles[pathname] ?? {
    section: 'Sixram HRIS',
    title: 'Workspace',
    subtitle: 'Use the navigation to move between the employee, manager, and HR workspaces available to your account.',
  }
}

function buildBreadcrumbs(pathname: string, page: PageMeta, isAdmin: boolean, hasLinkedEmployee: boolean, isManager: boolean): Breadcrumb[] {
  if (pathname === '/') {
    return [{ label: page.title }]
  }

  if (pathname === '/notifications') {
    return [{ label: 'Workspace', to: '/' }, { label: 'Notifications' }]
  }

  if (pathname.startsWith('/me/')) {
    return [{ label: 'Employee Portal', to: hasLinkedEmployee ? '/me/dashboard' : '/' }, { label: page.title }]
  }

  if (pathname.startsWith('/manager')) {
    return [{ label: 'Manager Portal', to: isManager ? '/manager' : '/' }, { label: page.title }]
  }

  if (pathname.startsWith('/approvals')) {
    return [{ label: 'Approvals', to: '/approvals' }, { label: page.title }]
  }

  if (
    pathname.startsWith('/analytics') ||
    pathname.startsWith('/reports') ||
    pathname.startsWith('/compliance') ||
    pathname.startsWith('/audit-logs')
  ) {
    return [{ label: 'Insights', to: '/analytics' }, { label: page.title }]
  }

  if (pathname.startsWith('/admin/payroll')) {
    return [{ label: 'Payroll', to: '/admin/payroll' }, { label: page.title }]
  }

  if (pathname.startsWith('/admin/provident-fund')) {
    return [{ label: 'Provident Fund', to: '/admin/provident-fund' }, { label: page.title }]
  }

  if (pathname.startsWith('/admin/users') || pathname.startsWith('/admin/roles') || pathname.startsWith('/admin/rbac')) {
    return [{ label: 'Security', to: '/admin/users' }, { label: page.title }]
  }

  if (pathname.startsWith('/admin/')) {
    return [{ label: 'HR Operations', to: isAdmin ? '/admin/employees' : '/' }, { label: page.title }]
  }

  return [{ label: page.section, to: '/' }, { label: page.title }]
}

export function AppLayout() {
  const { hasLinkedEmployee, isAdmin, isManager, logout, user } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [notificationOpen, setNotificationOpen] = useState(false)
  const [notificationSummary, setNotificationSummary] = useState<NotificationSummary | null>(null)
  const [expandedGroups, setExpandedGroups] = useState<Record<string, boolean>>(() => getDefaultExpanded(location.pathname))

  const canAccessApprovalCenter =
    isAdmin ||
    isManager ||
    (user?.roles.includes('HR') ?? false) ||
    (user?.roles.includes('PayrollOfficer') ?? false)

  const isHumanResources = user?.roles.includes('HR') ?? false
  const isPayrollOfficer = user?.roles.includes('PayrollOfficer') ?? false
  const canAccessReports = isAdmin || isManager || isHumanResources || isPayrollOfficer
  const canAccessCompliance = isAdmin || isManager || isHumanResources
  const canAccessAuditLogs = isAdmin || isHumanResources || isPayrollOfficer
  const canAccessProvidentFund = isAdmin || isHumanResources || isPayrollOfficer

  const navigationTree = useMemo<NavigationGroup[]>(() => {
    const groups: NavigationGroup[] = [
      {
        key: 'home',
        label: 'Workspace',
        icon: DashboardIcon,
        items: [
          { to: '/', label: 'Home' },
          { to: '/notifications', label: 'Notifications' },
        ],
      },
    ]

    if (canAccessReports || canAccessCompliance || canAccessAuditLogs) {
      groups.push({
        key: 'insights',
        label: 'Insights',
        icon: DashboardIcon,
        items: [
          ...(canAccessReports ? [{ to: '/analytics', label: 'Analytics Dashboard' }, { to: '/reports', label: 'Reports Center' }] : []),
          ...(canAccessCompliance ? [{ to: '/compliance', label: 'Compliance Center' }] : []),
          ...(canAccessAuditLogs ? [{ to: '/audit-logs', label: 'Audit Trail' }] : []),
        ],
      })
    }

    if (hasLinkedEmployee) {
      groups.push({
        key: 'employee',
        label: 'Employee Portal',
        icon: PeopleIcon,
        items: [
          { to: '/me/dashboard', label: 'My Dashboard' },
          { to: '/me/profile', label: 'My Profile' },
          { to: '/me/attendance', label: 'My Attendance' },
          { to: '/me/leave', label: 'My Leave' },
          { to: '/me/documents', label: 'My Documents' },
          { to: '/me/payslips', label: 'My Payslips' },
          { to: '/me/provident-fund', label: 'My Provident Fund' },
          { to: '/me/requests', label: 'My Requests' },
        ],
      })
    }

    if (isManager) {
      groups.push({
        key: 'manager',
        label: 'Manager Portal',
        icon: PeopleIcon,
        items: [
          { to: '/manager', label: 'Team Dashboard' },
          { to: '/manager/team', label: 'My Team' },
          { to: '/manager/attendance', label: 'Team Attendance' },
          { to: '/manager/leave', label: 'Team Leave' },
        ],
      })
    }

    if (canAccessApprovalCenter) {
      groups.push({
        key: 'approvals',
        label: 'Approvals',
        icon: ShieldIcon,
        items: [{ to: '/approvals', label: 'Approval Center' }],
      })
    }

    if (canAccessProvidentFund) {
      groups.push({
        key: 'providentFund',
        label: 'Provident Fund',
        icon: WalletIcon,
        items: [
          { to: '/admin/provident-fund', label: 'Dashboard' },
          { to: '/admin/provident-fund/policies', label: 'Fund Policies' },
          { to: '/admin/provident-fund/vesting', label: 'Vesting Rules' },
          { to: '/admin/provident-fund/enrollments', label: 'Employee Enrollment' },
          { to: '/admin/provident-fund/contributions', label: 'Monthly Contributions' },
          { to: '/admin/provident-fund/ledger', label: 'Fund Ledger' },
          { to: '/admin/provident-fund/withdrawals', label: 'Withdrawals' },
          { to: '/admin/provident-fund/adjustments', label: 'Adjustments' },
          { to: '/admin/provident-fund/reports', label: 'Reports' },
        ],
      })
    }

    if (isAdmin) {
      groups.push(
        {
          key: 'workforce',
          label: 'HR Operations',
          icon: ShieldIcon,
          items: [
            { to: '/admin/employees', label: 'Employees' },
            { to: '/admin/attendance', label: 'Attendance' },
            { to: '/admin/attendance/work-schedules', label: 'Work Schedules' },
            { to: '/admin/attendance/shifts', label: 'Shifts' },
            { to: '/admin/attendance/assignments', label: 'Schedule Assignments' },
            { to: '/admin/leave', label: 'Leave Management' },
            { to: '/admin/leave/calendar', label: 'Leave Calendar' },
            { to: '/admin/leave/types', label: 'Leave Types' },
            { to: '/admin/documents', label: 'Employee Documents' },
            { to: '/admin/document-types', label: 'Document Types' },
            { to: '/admin/organization', label: 'Organization Setup' },
            { to: '/admin/production-readiness', label: 'Production Readiness' },
          ],
        },
        {
          key: 'payroll',
          label: 'Payroll',
          icon: WalletIcon,
          items: [
            { to: '/admin/payroll', label: 'Payroll Dashboard' },
            { to: '/admin/payroll/compensation', label: 'Compensation' },
            { to: '/admin/payroll/setup', label: 'Payroll Setup' },
            { to: '/admin/payroll/reports', label: 'Payroll Reports' },
          ],
        },
        {
          key: 'security',
          label: 'Security',
          icon: ShieldIcon,
          items: [
            { to: '/admin/users', label: 'User Accounts' },
            { to: '/admin/roles', label: 'Roles' },
            { to: '/admin/rbac', label: 'RBAC Management' },
          ],
        },
      )
    }

    return groups
  }, [canAccessApprovalCenter, canAccessAuditLogs, canAccessCompliance, canAccessProvidentFund, canAccessReports, hasLinkedEmployee, isAdmin, isManager])

  const page = resolvePageMeta(location.pathname, isAdmin, hasLinkedEmployee, isManager)
  const breadcrumbs = buildBreadcrumbs(location.pathname, page, isAdmin, hasLinkedEmployee, isManager)
  const initials = user?.displayName?.slice(0, 1).toUpperCase() ?? 'S'

  async function loadNotificationSummary() {
    try {
      const response = await sixramApi.getNotificationSummary()
      setNotificationSummary(response)
    } catch {
      setNotificationSummary(null)
    }
  }

  useEffect(() => {
    setSidebarOpen(false)
    setNotificationOpen(false)
    setExpandedGroups((current) => {
      const next = { ...current }

      navigationTree.forEach((group) => {
        if (isGroupActive(group, location.pathname)) {
          next[group.key] = true
        }
      })

      return next
    })
  }, [location.pathname, navigationTree])

  useEffect(() => {
    if (user) {
      void loadNotificationSummary()
    }
  }, [location.pathname, user])

  async function handleOpenNotification(notification: UserNotification) {
    try {
      if (!notification.isRead) {
        await sixramApi.markNotificationRead(notification.id)
      }
    } finally {
      setNotificationOpen(false)
      await loadNotificationSummary()
      navigate(notification.actionUrl || '/notifications')
    }
  }

  async function handleMarkAllRead() {
    await sixramApi.markAllNotificationsRead()
    await loadNotificationSummary()
  }

  async function handleLogout() {
    await logout()
    navigate('/login', { replace: true })
  }

  const toggleGroup = (groupKey: string) => {
    setExpandedGroups((current) => ({
      ...current,
      [groupKey]: !current[groupKey],
    }))
  }

  const portalLabel = isAdmin ? 'Admin Console' : isManager ? 'Employee & Manager Portal' : 'Employee Portal'

  return (
    <div className="flex h-screen overflow-hidden bg-slate-50 text-slate-900">
      {sidebarOpen ? (
        <button
          aria-label="Close navigation"
          className="fixed inset-0 z-30 bg-slate-950/45 lg:hidden"
          onClick={() => setSidebarOpen(false)}
          type="button"
        />
      ) : null}

      <aside
        className={[
          'fixed inset-y-0 left-0 z-40 flex w-[290px] -translate-x-full flex-col border-r border-slate-200 bg-white transition-transform duration-200 lg:static lg:translate-x-0',
          sidebarOpen ? 'translate-x-0 shadow-xl' : '',
        ].join(' ')}
      >
        <div className="flex h-full flex-col">
          <div className="flex items-center justify-between border-b border-slate-100 px-5 py-6">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-[#465fff] text-sm font-semibold text-white shadow-sm">
                HR
              </div>
              <div>
                <p className="text-[15px] font-semibold text-slate-900">Sixram HRIS</p>
                <p className="text-xs text-slate-400">{portalLabel}</p>
              </div>
            </div>

            <button
              aria-label="Close sidebar"
              className="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-slate-200 text-slate-500 transition hover:bg-slate-50 lg:hidden"
              onClick={() => setSidebarOpen(false)}
              type="button"
            >
              <CloseIcon className="h-5 w-5" />
            </button>
          </div>

          <div className="flex-1 overflow-y-auto px-4 py-6">
            <p className="px-3 text-xs font-semibold uppercase tracking-[0.14em] text-slate-400">Menu</p>

            <div className="mt-4 space-y-1.5">
              {navigationTree.map((group) => {
                const Icon = group.icon
                const active = isGroupActive(group, location.pathname)
                const expanded = expandedGroups[group.key] ?? false

                return (
                  <div key={group.key}>
                    <button
                      aria-expanded={expanded}
                      className={[
                        'group flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-left text-sm font-medium transition',
                        active || expanded ? 'bg-[#ecf3ff] text-[#465fff]' : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900',
                      ].join(' ')}
                      onClick={() => toggleGroup(group.key)}
                      type="button"
                    >
                      <Icon
                        className={[
                          'h-5 w-5 shrink-0 transition',
                          active || expanded ? 'text-[#465fff]' : 'text-slate-500 group-hover:text-slate-700',
                        ].join(' ')}
                      />
                      <span className="flex-1 truncate">{group.label}</span>
                      <ChevronDownIcon
                        className={[
                          'h-4 w-4 transition-transform duration-200',
                          active || expanded ? 'text-[#465fff]' : 'text-slate-400',
                          expanded ? 'rotate-180' : '',
                        ].join(' ')}
                      />
                    </button>

                    {expanded ? (
                      <div className="mt-1.5 flex flex-col gap-1 pl-11">
                          {group.items.map((item) => (
                            <NavLink
                              className={({ isActive }) =>
                                [
                                  'rounded-lg px-3 py-2.5 text-sm font-medium transition',
                                  isActive
                                    ? 'bg-[#ecf3ff] text-[#465fff]'
                                    : 'text-slate-500 hover:bg-slate-50 hover:text-slate-700',
                                ].join(' ')
                              }
                              key={item.to}
                              to={item.to}
                            >
                              <span className="truncate">{item.label}</span>
                            </NavLink>
                          ))}
                      </div>
                    ) : null}
                  </div>
                )
              })}
            </div>
          </div>

          <div className="border-t border-slate-100 px-5 py-4">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-[#ecf3ff] text-sm font-semibold text-[#465fff]">
                {initials}
              </div>
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-semibold text-slate-900">{user?.displayName}</p>
                <p className="truncate text-xs text-slate-500">{user?.email}</p>
              </div>
              <div className="hidden rounded-md bg-slate-100 px-2 py-1 text-[11px] font-semibold text-slate-500 sm:block">
                {user?.roles[0] ?? 'User'}
              </div>
            </div>
          </div>
        </div>
      </aside>

      <div className="relative flex min-w-0 flex-1 flex-col overflow-y-auto overflow-x-hidden">
        <header className="sticky top-0 z-20 border-b border-slate-200 bg-white">
          <div className="flex flex-col gap-4 px-4 py-4 sm:px-6 lg:px-8">
            <div className="flex items-start justify-between gap-4">
              <div className="flex min-w-0 items-start gap-3">
              <button
                aria-label="Open navigation"
                className="mt-0.5 inline-flex h-11 w-11 items-center justify-center rounded-lg border border-slate-200 bg-white text-slate-600 shadow-sm transition hover:bg-slate-50 lg:hidden"
                onClick={() => setSidebarOpen(true)}
                type="button"
              >
                <MenuIcon className="h-5 w-5" />
              </button>

                <div className="min-w-0">
                  <nav aria-label="Breadcrumb" className="flex flex-wrap items-center gap-2 text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">
                    {breadcrumbs.map((crumb, index) => (
                      <div className="flex items-center gap-2" key={`${crumb.label}-${index}`}>
                        {crumb.to && index < breadcrumbs.length - 1 ? (
                          <NavLink className="transition hover:text-slate-600" to={crumb.to}>
                            {crumb.label}
                          </NavLink>
                        ) : (
                          <span className={index === breadcrumbs.length - 1 ? 'text-slate-500' : ''}>{crumb.label}</span>
                        )}
                        {index < breadcrumbs.length - 1 ? <span className="h-1 w-1 rounded-full bg-slate-300" /> : null}
                      </div>
                    ))}
                  </nav>
                  <h1 className="mt-2 text-2xl font-semibold text-slate-950 sm:text-[30px]">{page.title}</h1>
                  <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-500">{page.subtitle}</p>
                </div>
              </div>

              <div className="flex shrink-0 items-center gap-2 sm:gap-3">
              <div className="relative">
                <button
                  aria-label="Open notifications"
                  className="relative inline-flex h-11 w-11 items-center justify-center rounded-lg border border-slate-200 bg-white text-slate-600 shadow-sm transition hover:bg-slate-50"
                  onClick={() => setNotificationOpen((current) => !current)}
                  type="button"
                >
                  <BellIcon className="h-5 w-5" />
                  {notificationSummary && notificationSummary.unreadCount > 0 ? (
                    <span className="absolute right-2 top-2 min-w-[18px] rounded-full bg-[#465fff] px-1.5 py-0.5 text-[10px] font-semibold text-white">
                      {notificationSummary.unreadCount > 99 ? '99+' : notificationSummary.unreadCount}
                    </span>
                  ) : null}
                </button>

                {notificationOpen ? (
                  <div className="fixed inset-x-4 top-[88px] z-30 rounded-xl border border-slate-200 bg-white p-4 shadow-lg sm:absolute sm:inset-auto sm:right-0 sm:top-14 sm:w-[360px]">
                    <div className="flex items-center justify-between gap-3">
                      <div>
                        <p className="text-sm font-semibold text-slate-900">Notifications</p>
                        <p className="mt-1 text-xs text-slate-500">
                          {notificationSummary?.unreadCount ?? 0} unread update{notificationSummary?.unreadCount === 1 ? '' : 's'}
                        </p>
                      </div>
                      <div className="flex gap-2">
                        <button className="shell-button-secondary px-3 py-2 text-xs" onClick={() => void handleMarkAllRead()} type="button">
                          Mark all read
                        </button>
                        <button
                          className="shell-button-secondary px-3 py-2 text-xs"
                          onClick={() => {
                            setNotificationOpen(false)
                            navigate('/notifications')
                          }}
                          type="button"
                        >
                          View all
                        </button>
                      </div>
                    </div>

                    <div className="mt-4 space-y-2">
                      {notificationSummary?.recent.length ? (
                        notificationSummary.recent.slice(0, 5).map((notification) => (
                          <button
                            className={[
                              'w-full rounded-xl border px-4 py-3 text-left transition',
                              notification.isRead
                                ? 'border-slate-200 bg-white hover:bg-slate-50'
                                : 'border-[#465fff]/20 bg-[#465fff]/5 hover:bg-[#465fff]/10',
                            ].join(' ')}
                            key={notification.id}
                            onClick={() => void handleOpenNotification(notification)}
                            type="button"
                          >
                            <div className="text-sm font-semibold text-slate-900">{notification.title}</div>
                            <div className="mt-1 text-sm text-slate-500">{notification.message}</div>
                            <div className="mt-2 text-[11px] uppercase tracking-[0.16em] text-slate-400">
                              {formatDateTime(notification.createdAtUtc)}
                            </div>
                          </button>
                        ))
                      ) : (
                        <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-4 text-sm text-slate-500">
                          No notifications available right now.
                        </div>
                      )}
                    </div>
                  </div>
                ) : null}
              </div>

              {isAdmin ? <span className="shell-badge-brand hidden sm:inline-flex">Administrator</span> : null}
              {!isAdmin && isManager ? <span className="shell-badge-success hidden sm:inline-flex">Manager</span> : null}

              <div className="hidden items-center gap-3 rounded-xl border border-slate-200 bg-white px-3 py-2 shadow-sm md:flex">
                <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-slate-100 text-sm font-semibold text-slate-700">
                  {initials}
                </div>
                <div>
                  <p className="text-sm font-semibold text-slate-900">{user?.displayName}</p>
                  <p className="text-xs text-slate-500">Session secured</p>
                </div>
              </div>

              <button className="shell-button-secondary" onClick={() => void handleLogout()} type="button">
                <LogoutIcon className="h-4 w-4" />
                <span className="hidden sm:inline">Sign Out</span>
              </button>
            </div>
          </div>
            <div className="flex flex-wrap items-center gap-2 text-xs text-slate-500 sm:hidden">
              {isAdmin ? <span className="shell-badge-brand">Administrator</span> : null}
              {!isAdmin && isManager ? <span className="shell-badge-success">Manager</span> : null}
              {user?.roles.slice(0, 2).map((role) => (
                <span className="shell-badge-muted" key={`mobile-${role}`}>
                  {role}
                </span>
              ))}
            </div>
          </div>
        </header>

        <main className="flex-1">
          <div className="w-full px-4 py-5 sm:px-6 sm:py-6 lg:px-8 2xl:px-10 2xl:py-8">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  )
}

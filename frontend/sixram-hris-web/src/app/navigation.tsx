import type { ComponentType, SVGProps } from 'react'
import {
  canAccessApprovalCenter,
  canAccessAuditLogs,
  canAccessCompliance,
  canAccessProvidentFund,
  canAccessReports,
  type WorkspaceAccess,
} from './access'
import { DashboardIcon, PeopleIcon, ShieldIcon, WalletIcon } from './navigationIcons'

type IconProps = SVGProps<SVGSVGElement>

export type NavigationItem = {
  to: string
  label: string
}

export type NavigationGroup = {
  key: string
  label: string
  icon: ComponentType<IconProps>
  items: NavigationItem[]
}

export type PageMeta = {
  section: string
  title: string
  subtitle: string
}

export type Breadcrumb = {
  label: string
  to?: string
}

export function getDefaultExpanded(pathname: string) {
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

export function isGroupActive(group: NavigationGroup, pathname: string) {
  return group.items.some((item) => pathname === item.to || pathname.startsWith(`${item.to}/`))
}

export function buildNavigationTree(access: WorkspaceAccess): NavigationGroup[] {
  const allowReports = canAccessReports(access)
  const allowCompliance = canAccessCompliance(access)
  const allowAuditLogs = canAccessAuditLogs(access)
  const allowApprovalCenter = canAccessApprovalCenter(access)
  const allowProvidentFund = canAccessProvidentFund(access)

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

  if (allowReports || allowCompliance || allowAuditLogs) {
    groups.push({
      key: 'insights',
      label: 'Insights',
      icon: DashboardIcon,
      items: [
        ...(allowReports ? [{ to: '/analytics', label: 'Analytics Dashboard' }, { to: '/reports', label: 'Reports Center' }] : []),
        ...(allowCompliance ? [{ to: '/compliance', label: 'Compliance Center' }] : []),
        ...(allowAuditLogs ? [{ to: '/audit-logs', label: 'Audit Trail' }] : []),
      ],
    })
  }

  if (access.hasLinkedEmployee) {
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

  if (access.isManager) {
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

  if (allowApprovalCenter) {
    groups.push({
      key: 'approvals',
      label: 'Approvals',
      icon: ShieldIcon,
      items: [{ to: '/approvals', label: 'Approval Center' }],
    })
  }

  if (allowProvidentFund) {
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

  if (access.isAdmin) {
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
}

export function resolvePageMeta(pathname: string, access: WorkspaceAccess): PageMeta {
  if (pathname === '/') {
    if (access.isAdmin) {
      return {
        section: 'Dashboard',
        title: 'Dashboard',
        subtitle: 'Monitor HR operations, review activity across modules, and jump into administrator workflows.',
      }
    }

    if (access.hasLinkedEmployee) {
      return {
        section: 'Employee Portal',
        title: 'My Dashboard',
        subtitle: 'See your attendance, leave, documents, payslips, and self-service updates from one place.',
      }
    }

    if (access.isManager) {
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

export function buildBreadcrumbs(pathname: string, page: PageMeta, access: WorkspaceAccess): Breadcrumb[] {
  if (pathname === '/') {
    return [{ label: page.title }]
  }

  if (pathname === '/notifications') {
    return [{ label: 'Workspace', to: '/' }, { label: 'Notifications' }]
  }

  if (pathname.startsWith('/me/')) {
    return [{ label: 'Employee Portal', to: access.hasLinkedEmployee ? '/me/dashboard' : '/' }, { label: page.title }]
  }

  if (pathname.startsWith('/manager')) {
    return [{ label: 'Manager Portal', to: access.isManager ? '/manager' : '/' }, { label: page.title }]
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
    return [{ label: 'HR Operations', to: access.isAdmin ? '/admin/employees' : '/' }, { label: page.title }]
  }

  return [{ label: page.section, to: '/' }, { label: page.title }]
}

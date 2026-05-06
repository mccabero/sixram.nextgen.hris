# Sixram HRIS

Sixram HRIS is a baseline full-stack Human Resource Information System built from scratch with:

- Backend: ASP.NET Core Web API (`net9.0`), EF Core, SQL Server, ASP.NET Core Identity, JWT access tokens, rotating refresh tokens
- Frontend: React, Vite, Tailwind CSS
- Database: Microsoft SQL Server local instance using `SixramDB`

## Project Structure

```text
/backend/Sixram.Hris.Api
/frontend/sixram-hris-web
/Sixram.Hris.sln
/README.md
```

## Prerequisites

- .NET SDK 9.x
- Node.js 24+ and npm
- Microsoft SQL Server running locally and reachable at `localhost\SQLEXPRESS`
- A trusted local development certificate if you plan to use the HTTPS backend profile

## Backend Setup

From the repository root:

```powershell
dotnet tool restore
dotnet restore
dotnet build
```

The backend project is located at [backend/Sixram.Hris.Api](backend/Sixram.Hris.Api).

### Development Connection String

The SQL Server connection string is stored in [backend/Sixram.Hris.Api/appsettings.Development.json](backend/Sixram.Hris.Api/appsettings.Development.json):

```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=SixramDB;Trusted_Connection=True;TrustServerCertificate=True;"
```

This repository is currently configured for the local `SQLEXPRESS` instance detected on this machine. If your SQL Server is installed as the default unnamed instance instead, change the server portion back to `localhost`.

### Employee Document Storage

Employee document uploads are stored outside the public web root under:

```text
backend/Sixram.Hris.Api/App_Data/employee-documents
```

The default upload settings are configured in [backend/Sixram.Hris.Api/appsettings.json](backend/Sixram.Hris.Api/appsettings.json) and [backend/Sixram.Hris.Api/appsettings.Development.json](backend/Sixram.Hris.Api/appsettings.Development.json):

- `StorageRootPath`: `App_Data/employee-documents`
- `MaxFileSizeMb`: `10`
- `ExpiringSoonDays`: `30`
- Allowed file types: `.pdf`, `.doc`, `.docx`, `.jpg`, `.jpeg`, `.png`

### Leave Attachment Storage

Leave request attachments are stored outside the public web root under:

```text
backend/Sixram.Hris.Api/App_Data/leave-attachments
```

The default leave upload and summary settings are configured in [backend/Sixram.Hris.Api/appsettings.json](backend/Sixram.Hris.Api/appsettings.json) and [backend/Sixram.Hris.Api/appsettings.Development.json](backend/Sixram.Hris.Api/appsettings.Development.json):

- `StorageRootPath`: `App_Data/leave-attachments`
- `MaxAttachmentSizeMb`: `10`
- `UpcomingWindowDays`: `14`
- `LowBalanceThreshold`: `1`
- Allowed file types: `.pdf`, `.doc`, `.docx`, `.jpg`, `.jpeg`, `.png`

### EF Core Migration Update Command

Apply the included migrations and create/update `SixramDB`:

```powershell
dotnet ef database update --project backend/Sixram.Hris.Api --startup-project backend/Sixram.Hris.Api
```

The API also runs `Database.Migrate()` and seed logic automatically on startup.

### Run the Backend

```powershell
dotnet run --project backend/Sixram.Hris.Api
```

Default development URLs:

- API HTTP: `http://localhost:5180`
- API HTTPS: `https://localhost:7239`
- Swagger UI: `http://localhost:5180/swagger`

## Frontend Setup

The frontend project is located at [frontend/sixram-hris-web](frontend/sixram-hris-web).

```powershell
cd frontend/sixram-hris-web
npm install
npm run dev
```

Default frontend URL:

- App: `http://localhost:5173`

The frontend API base URL is configured in [frontend/sixram-hris-web/.env.development](frontend/sixram-hris-web/.env.development):

```env
VITE_API_BASE_URL=http://localhost:5180
```

## Default Admin Login

- Email: `admin@sixram.local`
- Password: `ChangeMe123!`

The login page does not prefill or display these seeded credentials. Treat them as first-run bootstrap credentials only, and change or reset the default admin password before shared UAT or production use.

On a fresh database, the startup seed ensures:

- Role: `Administrator`
- Role: `User`
- Role: `HR`
- Role: `Manager`
- Role: `PayrollOfficer`
- Admin user: `admin@sixram.local`
- Admin user assigned to `Administrator`

## API Surface

Authentication endpoints:

- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/auth/me`

Administrator-only endpoints:

- `GET/POST/PUT/DELETE /api/admin/users...`
- `PATCH /api/admin/users/{userId}/status`
- `POST /api/admin/users/{userId}/reset-password`
- `POST /api/admin/users/{userId}/change-password`
- `PUT /api/admin/users/{userId}/roles`
- `POST /api/admin/users/{userId}/roles/{roleName}`
- `DELETE /api/admin/users/{userId}/roles/{roleName}`
- `GET/POST/PUT/DELETE /api/admin/roles...`
- `GET /api/admin/rbac`
- `PUT /api/admin/rbac/users/{userId}/roles`
- `GET/POST/PUT/DELETE /api/admin/employees...`
- `GET /api/admin/employees/options`
- `GET/POST/PUT/PATCH/DELETE /api/admin/employees/{employeeId}/documents...`
- `POST /api/admin/employees/{employeeId}/documents/{documentId}/replace`
- `GET /api/admin/documents/summary`
- `GET /api/admin/documents/options`
- `GET /api/admin/documents`
- `GET /api/admin/documents/{documentId}/download`
- `GET/POST/PUT/DELETE /api/admin/document-types...`
- `GET /api/admin/attendance/summary`
- `GET /api/admin/attendance/options`
- `GET/POST/PUT/DELETE /api/admin/attendance/records...`
- `GET /api/admin/attendance/setup/summary`
- `GET /api/admin/attendance/setup/options`
- `GET/POST/PUT/DELETE /api/admin/attendance/setup/work-schedules...`
- `GET/POST/PUT/DELETE /api/admin/attendance/setup/shifts...`
- `GET/POST/PUT /api/admin/attendance/setup/assignments...`
- `GET /api/admin/leave/summary`
- `GET /api/admin/leave/options`
- `GET /api/admin/leave/balances`
- `POST /api/admin/leave/balances/adjustments`
- `GET /api/admin/leave/requests`
- `GET /api/admin/leave/requests/{leaveRequestId}`
- `POST /api/admin/leave/requests`
- `PUT /api/admin/leave/requests/{leaveRequestId}`
- `POST /api/admin/leave/requests/{leaveRequestId}/approve`
- `POST /api/admin/leave/requests/{leaveRequestId}/reject`
- `POST /api/admin/leave/requests/{leaveRequestId}/cancel`
- `DELETE /api/admin/leave/requests/{leaveRequestId}`
- `GET /api/admin/leave/requests/{leaveRequestId}/attachment`
- `GET /api/admin/leave/calendar`
- `GET/POST/PUT/DELETE /api/admin/leave-types...`
- `GET /api/admin/employees/{employeeId}/leave`
- `GET /api/admin/payroll/summary`
- `GET /api/admin/payroll/options`
- `GET/POST/PUT/DELETE /api/admin/payroll/pay-periods...`
- `GET /api/admin/payroll/runs`
- `GET /api/admin/payroll/runs/{payrollRunId}`
- `POST /api/admin/payroll/runs/generate`
- `POST /api/admin/payroll/runs/{payrollRunId}/recalculate`
- `POST /api/admin/payroll/runs/{payrollRunId}/submit-review`
- `POST /api/admin/payroll/runs/{payrollRunId}/approve`
- `POST /api/admin/payroll/runs/{payrollRunId}/mark-paid`
- `POST /api/admin/payroll/runs/{payrollRunId}/cancel`
- `GET/POST/PUT/DELETE /api/admin/payroll/adjustments...`
- `POST /api/admin/payroll/adjustments/{payrollAdjustmentId}/approve`
- `POST /api/admin/payroll/adjustments/{payrollAdjustmentId}/reject`
- `POST /api/admin/payroll/adjustments/{payrollAdjustmentId}/cancel`
- `GET /api/admin/payroll/reports`
- `GET /api/admin/payroll/payslips/{payrollRunItemId}`
- `GET/POST/PUT/DELETE /api/admin/payroll/setup/pay-period-templates...`
- `GET/POST/PUT/DELETE /api/admin/payroll/setup/earning-types...`
- `GET/POST/PUT/DELETE /api/admin/payroll/setup/deduction-types...`
- `GET/POST/PUT/DELETE /api/admin/payroll/setup/contribution-types...`
- `GET/POST/PUT/DELETE /api/admin/payroll/setup/contribution-tables...`
- `GET/POST/PUT/DELETE /api/admin/payroll/setup/tax-tables...`
- `GET /api/admin/payroll/setup/summary`
- `GET /api/admin/payroll/setup/settings`
- `PUT /api/admin/payroll/setup/settings`
- `GET/POST/PUT/DELETE /api/admin/payroll/compensation/profiles...`
- `GET/POST/PUT/DELETE /api/admin/payroll/compensation/recurring-earnings...`
- `GET/POST/PUT/DELETE /api/admin/payroll/compensation/recurring-deductions...`
- `GET /api/admin/employees/{employeeId}/payroll`
- `GET /api/admin/organization/summary`
- `GET /api/admin/organization/options`
- `GET/POST/PUT/DELETE /api/admin/organization/departments...`
- `GET/POST/PUT/DELETE /api/admin/organization/positions...`
- `GET/POST/PUT/DELETE /api/admin/organization/branches...`
- `GET/POST/PUT/DELETE /api/admin/organization/employment-types...`
- `GET/POST/PUT/DELETE /api/admin/organization/employment-statuses...`
- `GET /api/admin/production-readiness`
- `POST /api/admin/production-readiness/imports/preview`
- `POST /api/admin/production-readiness/imports/apply`

Admin endpoints are protected with `[Authorize(Roles = "Administrator")]`.

## HR Module Notes

The baseline now includes:

- Employee Master Profile
- Organization Setup
- Employee Documents and Compliance Management
- Attendance & Timekeeping
- Leave Management + Leave Credits
- Payroll Preparation + Compensation Management
- Employee Self-Service
- Manager Self-Service
- Approval Center
- Notifications
- Reports and Analytics
- Compliance Center
- Audit Trail

Seeded organization setup values include starter:

- Departments: `Human Resources`, `Information Technology`, `Finance`, `Operations`
- Positions: `HR Manager`, `Systems Administrator`, `Accountant`, `Operations Analyst`
- Branches: `Head Office`, `North Branch`, `South Branch`
- Employment Types: `Regular`, `Probationary`, `Contractual`
- Employment Statuses: `Active`, `Probationary`, `Regularized`, `Resigned`, `Terminated`

Seeded document types include starter:

- `Resume`
- `Employment Contract`
- `Government ID`
- `Certificate`
- `Medical Record`
- `Clearance`
- `Training Document`
- `Other`

Seeded attendance setup values include starter:

- Work Schedules: `Fixed 8-Hour Schedule`, `Flexible 8-Hour Schedule`, `Shifting 8-Hour Schedule`
- Shifts: `Day Shift 9:00 AM - 6:00 PM`, `Day Shift 8:00 AM - 5:00 PM`, `Night Shift 10:00 PM - 7:00 AM`

Seeded leave types include starter:

- `Vacation Leave`
- `Sick Leave`
- `Emergency Leave`
- `Maternity Leave`
- `Paternity Leave`
- `Bereavement Leave`
- `Solo Parent Leave`
- `Service Incentive Leave`
- `Unpaid Leave`
- `Other`

Seeded payroll setup values include starter:

- Pay Period Templates: `Weekly Payroll`, `Semi-Monthly Payroll`, `Monthly Payroll`
- Earning Types: `Basic Pay`, `Rice Allowance`, `Transport Allowance`, `Overtime Pay`, `Holiday Pay`, `Bonus`, `Commission`, `Reimbursement`, `Other Earning`
- Deduction Types: `SSS`, `PhilHealth`, `Pag-IBIG`, `Withholding Tax`, `Absence Deduction`, `Late Deduction`, `Undertime Deduction`, `Loan Deduction`, `Cash Advance`, `Other Deduction`
- Contribution Types: `SSS`, `PhilHealth`, `Pag-IBIG`
- Tax Tables: `Weekly`, `Semi-monthly`, `Monthly`, and `Custom` baseline withholding tables

Frontend admin routes now include:

- `/admin/employees`
- `/admin/employees/new`
- `/admin/employees/{employeeId}`
- `/admin/employees/{employeeId}/edit`
- `/admin/attendance`
- `/admin/attendance/work-schedules`
- `/admin/attendance/shifts`
- `/admin/attendance/assignments`
- `/admin/leave`
- `/admin/leave/calendar`
- `/admin/leave/types`
- `/admin/payroll`
- `/admin/payroll/compensation`
- `/admin/payroll/setup`
- `/admin/payroll/runs/{payrollRunId}`
- `/admin/payroll/payslips/{payrollRunItemId}`
- `/admin/payroll/reports`
- `/admin/documents`
- `/admin/document-types`
- `/admin/organization`
- `/admin/production-readiness`
- `/admin/organization/departments`
- `/admin/organization/positions`
- `/admin/organization/branches`
- `/admin/organization/employment-types`
- `/admin/organization/employment-statuses`

## Production Readiness Tools

Phase 9 adds an administrator-only operational workspace at `/admin/production-readiness` for:

- go-live checklist and operational readiness review
- import preview and apply workflow
- row-level CSV validation with downloadable error review
- links to the affected modules when a checklist item needs attention

Supported import types:

- `employees`
- `departments`
- `positions`
- `branches`
- `employment_types`
- `employment_statuses`
- `leave_balances`
- `compensation_profiles`

Import behavior:

- preview is required before apply
- imports are CSV-only in this baseline
- imports are transactional; invalid rows block apply
- duplicate rows inside the same file are detected
- master-data references are validated before save
- leave balance imports avoid duplicate zero-value ledger noise on repeat apply

## Go-Live Checklist

Before production rollout, confirm:

- organization setup is complete
- employee records are imported and validated
- user accounts are linked to employees where needed
- roles and permission assignments are reviewed
- leave balances are imported
- compensation profiles are complete
- schedules are assigned
- payroll settings, contribution tables, and tax tables are configured
- document types and required documents are configured
- key reports and exports are tested
- database and uploaded-file backup process is confirmed
- UAT is completed and signed off

## Auth And Security Notes

- Passwords are never encrypted/decrypted. They are hashed and verified with ASP.NET Core Identity.
- Access tokens are stored in memory on the frontend.
- Refresh tokens are stored in an HTTP-only cookie and rotated on each refresh.
- Refresh tokens are persisted in the database as hashes, not plaintext.
- Employee document files are stored on the server outside the public web root and are only served through authorized API endpoints.
- Raw server storage paths are not exposed to the frontend.
- Server-side validation enforces the allowed document extensions and upload size limit.
- Attendance calculations use server-side schedule rules and server-side validation for late, undertime, overtime, incomplete, absent, rest-day, and no-schedule states.
- Attendance timekeeping remains admin-driven for clock entry, while employee self-service users can review their own attendance and submit attendance adjustment requests through the ESS portal.
- Leave requests can be created in both the admin HR workspace and employee self-service, with manager or HR review enforced through the server-side approval rules.
- Leave balances are tracked per employee, per leave type, and per year, with a ledger for grant, usage, adjustment, and cancellation events.
- Approved leave requests create `On Leave` attendance markers through the backend when no conflicting actual attendance record exists.
- Payroll calculations are performed server-side from compensation profiles, attendance, approved leave, recurring payroll components, contribution/tax tables, and approved payroll adjustments.
- Payroll runs snapshot employee, compensation, and computed line-item values so historical payroll does not change when master data changes later.
- Payroll setup keeps contribution and withholding tables configurable; no hardcoded statutory rates are used in the codebase.
- Payslips are available in the payroll admin workspace and in employee self-service for the linked employee account, subject to the existing server-side access rules.
- The frontend does not use `localStorage` for tokens in this baseline, which reduces exposure to token theft via XSS.
- CORS is configured from `Cors:AllowedOrigins`, and Development also allows loopback browser origins such as `localhost`, `127.0.0.1`, and `::1` for local Vite ports.
- Production startup now fails fast if the JWT secret is missing or still set to the development fallback value.
- Security headers and HSTS are enabled for non-development environments.
- Report exports, document downloads, leave attachment downloads, payslip views, and data-import apply actions are audited.
- Report and audit date-range queries are capped server-side to reduce accidental heavy requests.

## Backup And Maintenance Notes

- Back up the SQL Server database on a recurring schedule appropriate for your environment.
- Back up `backend/Sixram.Hris.Api/App_Data/employee-documents` and `backend/Sixram.Hris.Api/App_Data/leave-attachments` alongside the database so metadata and files stay aligned.
- Review audit-log retention and exported CSV retention as part of your operational policy.
- Use the production-readiness page, compliance center, analytics dashboard, and audit trail together during UAT and early go-live monitoring.

## Verification Notes

- `dotnet build Sixram.Hris.sln`
- `npm run build`
- `dotnet list backend/Sixram.Hris.Api/Sixram.Hris.Api.csproj package --vulnerable --include-transitive`
- `npm audit --audit-level=high`

## Notes

- The backend includes DTOs, service classes, repositories where useful, validation, logging, global error handling, and Swagger.
- Usernames are aligned to email addresses.
- The React app includes route guards, role-aware navigation, route-level lazy loading, and real API integration for Users, Roles, RBAC, Employee Master, Organization Setup, Employee Documents, Document Types, Attendance, Leave, Payroll, reports, compliance, and production-readiness pages.
- If you change dev ports, update both the backend CORS settings and the frontend `VITE_API_BASE_URL`.

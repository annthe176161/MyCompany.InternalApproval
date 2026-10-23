# Project: Employee Profile & Tenure Module (Hồ sơ Nhân viên)

## Architecture
- **Backend Architecture**: .NET 8.0, ABP Framework 8.3.4, DDD layering:
  - `Domain.Shared`: Enums, localization, permissions constants (`InternalApprovalPermissions.Employees.ViewAll`).
  - `Domain`: `Department.ManagerId`, `DepartmentMember.JoinDate`, `Request.StartDate/EndDate`, domain helpers (`TenureCalculator`, `LeaveTimelineExtractor`), data seed.
  - `Application.Contracts`: Employee DTOs, `EmployeeLeaveReportItemDto`, `GetLeaveReportInput`, `IEmployeeAppService`, permissions definition.
  - `Application`: `EmployeeAppService` business implementation with 3-tier hierarchical scoping (`EvaluateCurrentUserAccessScopeAsync`, `CheckCanAccessEmployeeAsync`), AutoMapper profile.
  - `EntityFrameworkCore`: EF Core migrations, SQLite in-memory test provider.
  - `HttpApi`: `EmployeeController` exposing REST APIs under `/api/app/employee` including `/api/app/employee/leave-report`.
- **Frontend Architecture**: Angular 18, NG-ZORRO 18, Lepton-X theme:
  - `proxy/employees`: TypeScript models (`EmployeeLeaveReportItemDto`, `GetLeaveReportInput`) and `EmployeeService`.
  - `shared/services/excel-export.service.ts`: `exportLeaveStatistics` using `xlsx` with title, metadata rows, formatted columns, and summary total row.
  - `shared/components/employee-detail-drawer`: 4-tab reusable drawer.
  - `employee`: `/employees` management page with `<nz-tabset>` hosting Tab 1 ("Danh sách nhân viên") and Tab 2 ("Báo cáo ngày nghỉ phép"), with scoped filters and Excel export button.
  - Navigation & Styling: Zero-scroll layout, role-based department selector.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | R1.1 JoinDate Model & Migration | Safe nullable `JoinDate` in `DepartmentMember`, EF Core migration, fallback to `CreationTime` | M1 (DONE) | ORIGINAL_REQUEST §R1 |
| 2 | R1.2 Tenure Calculation & Format | Auto-calculate tenure with exact Vietnamese format ("2 năm 5 tháng", "8 tháng", "< 1 tháng") | M1 (DONE) | ORIGINAL_REQUEST §R1 |
| 3 | R1.3 Employee List & Detail API | Endpoints for listing employees (search, department filter) and profile detail | M1 (DONE) | ORIGINAL_REQUEST §R1 |
| 4 | R1.4 Update JoinDate API | Endpoint allowing admin/manager to update `JoinDate` with validation | M1 (DONE) | ORIGINAL_REQUEST §R1 |
| 5 | R2.1 Employee Request History API | List requests submitted by employee with status and type filters | M1 (DONE) | ORIGINAL_REQUEST §R2 |
| 6 | R2.2 Yearly Statistics API | Total request counts by status (Draft, Submitted, Approved, Rejected) and amounts requested/disbursed | M1 (DONE) | ORIGINAL_REQUEST §R2 |
| 7 | R3.1 Leave Timeline API & Parser | Extract leave requests with date range, duration in days, reason, and status | M1 (DONE) | ORIGINAL_REQUEST §R3 |
| 8 | R4.1 HR Analytics API | Headcount by department, average tenure by department, and request frequency | M1 (DONE) | ORIGINAL_REQUEST §R4 |
| 9 | R1.5 Navigation & Menu | Add "Hồ sơ nhân viên" (`/employees`) in sidebar menu (`route.provider.ts`) | M3 (DONE) | ORIGINAL_REQUEST §R1 |
| 10 | R1.6 Employee List UI Page | `/employees` table with full details: avatar, name, department, position, join date, tenure badge, request count | M3 (DONE) | ORIGINAL_REQUEST §R1 |
| 11 | R4.2 HR Analytics Dashboard Cards | Overview KPI cards (headcount distribution, company avg tenure, request frequency) on `/employees` | M3 (DONE) | ORIGINAL_REQUEST §R4 |
| 12 | R1.7 Reusable Drawer - Tab 1 (General & Tenure) | Drawer Tab 1: Profile info, tenure milestone badge, date picker to update `JoinDate` | M2 (DONE) | ORIGINAL_REQUEST §R1 |
| 13 | R3.2 Reusable Drawer - Tab 2 (Leave Timeline) | Drawer Tab 2: Visual timeline of leave requests (date range, days count, reason, status tag) | M2 (DONE) | ORIGINAL_REQUEST §R2 |
| 14 | R2.3 Reusable Drawer - Tab 3 (Request History) | Drawer Tab 3: All employee requests with filter dropdowns (Status, Type) | M2 (DONE) | ORIGINAL_REQUEST §R2 |
| 15 | R2.4 Reusable Drawer - Tab 4 (Yearly Stats) | Drawer Tab 4: Yearly request status counts and financial totals | M2 (DONE) | ORIGINAL_REQUEST §R2 |
| 16 | R1.8 Quick-View Integration (Department) | Click employee/manager name in Department views to open drawer | M2 (DONE) | ORIGINAL_REQUEST §R1 |
| 17 | R1.9 Quick-View Integration (Requests) | Click creator name in Request table or detail drawer to open drawer | M2 (DONE) | ORIGINAL_REQUEST §R1 |
| 18 | R1.10 Styling & Zero-Scroll | Fit 100% viewport, no unwanted window scrollbars, consistent fonts/icons/alignment | M3 (DONE) | ORIGINAL_REQUEST §Frontend |
| 19 | E2E Testing & Full Verification | Automated test suites (backend integration + E2E verification) 100% pass | M4 (DONE) | ORIGINAL_REQUEST §Verification |
| 20 | R1.11 Permissions & Role Seeding | Add `Employees.ViewAll` constant, definition, and seed to `admin` role | M5 | ORIGINAL_REQUEST §R1 (2026-10-22) |
| 21 | R1.12 Hierarchical Authorization Engine | 3-tier access control (Admin/ViewAll -> Department Manager -> Employee self-only) across all `EmployeeAppService` methods with 403 Forbidden enforcement | M5 | ORIGINAL_REQUEST §R1 (2026-10-22) |
| 22 | R2.1 Backend Leave Days Statistics API | `GetLeaveReportAsync` calculating total approved leave days, sent/approved counts, latest leave period | M5 | ORIGINAL_REQUEST §R2 (2026-10-22) |
| 23 | R1.13 Automated Backend Security Tests | Integration tests verifying positive & negative access for Admin, Managers, and Employees | M5 | ORIGINAL_REQUEST §Acceptance (2026-10-22) |
| 24 | R2.2 Frontend Leave Report Tab | Tab "Báo cáo ngày nghỉ phép" on `/employees` with 9 columns, Year/Dept/Search filters | M6 | ORIGINAL_REQUEST §R2 (2026-10-22) |
| 25 | R3.1 Enterprise Excel Export (.xlsx) | "Xuất Excel" button with header, metadata, data rows, and summary total row | M6 | ORIGINAL_REQUEST §R3 (2026-10-22) |
| 26 | Full Verification & Forensic Audit | Zero errors in `dotnet build`, `npm run build`, automated tests pass, forensic audit CLEAN | M7 | ORIGINAL_REQUEST §Quality (2026-10-22) |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Backend Domain, DB Migration, Application Services & APIs | Features 1-8 | none | DONE |
| M2 | Frontend Proxies, Reusable 4-Tab Drawer & Page Quick-View Integration | Features 12-17 | M1 | DONE |
| M3 | Frontend `/employees` Management Page, Navigation & HR Analytics | Features 9-11, 18 | M2 | DONE |
| M4 | Final Verification & Quality Hardening | Feature 19 | M1, M2, M3 | DONE |
| M5 | Backend Hierarchical Authorization & Leave Statistics Report API | Features 20-23 | M1-M4 | DONE |
| M6 | Frontend Leave Statistics Tab & Enterprise Excel Export | Features 24-25 | M5 | IN_PROGRESS |
| M7 | Quality Hardening, Stress Testing & Forensic Audit | Feature 26 | M5, M6 | PLANNED |

## Interface Contracts
### Backend ↔ Frontend Leave Report Contract
- Endpoint: `GET /api/app/employee/leave-report`
- Input: `GetLeaveReportInput` (`Year: int?`, `DepartmentId: Guid?`, `Filter: string?`, `SkipCount: int`, `MaxResultCount: int`, `Sorting: string?`)
- Output: `PagedResultDto<EmployeeLeaveReportItemDto>`:
  - `Id`: Guid
  - `UserName`: string
  - `FullName`: string
  - `Email`: string
  - `DepartmentId`: Guid?
  - `DepartmentCode`: string?
  - `DepartmentName`: string?
  - `Position`: string
  - `TotalApprovedLeaveDays`: double
  - `TotalSentLeaveRequests`: int
  - `TotalApprovedLeaveRequests`: int
  - `LatestLeaveStartDate`: DateTime?
  - `LatestLeaveEndDate`: DateTime?
  - `LatestLeaveDays`: double?
  - `LatestLeavePeriodFormatted`: string? (e.g. "25/10/2026 - 26/10/2026 (2 ngày)" or "Chưa có")

### Hierarchical Authorization Contract
- `Employees.ViewAll` or Role `admin` / username `admin`: Access all employees and departments.
- Department Manager (`Department.ManagerId == CurrentUser.Id`): Access only employees belonging to managed departments, plus their own record. Attempt to access other departments or employees outside managed scope returns 403 Forbidden (`AbpAuthorizationException`).
- Regular Employee: Access only self (`targetUserId == CurrentUser.Id`). All other queries return 403 Forbidden.

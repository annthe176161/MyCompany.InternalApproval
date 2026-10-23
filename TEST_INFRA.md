# TEST_INFRA.md - Test Architecture & 4-Tier Verification Infrastructure
# Modules: Employee Profile & Tenure | Hierarchical Authorization & Leave Statistics

## 1. Overview & Test Architecture

This document defines the comprehensive testing architecture, feature inventory, 4-tier coverage methodology, and automated verification harness for the **Employee Profile & Tenure** and **Hierarchical Department Authorization & Leave Days Statistics** modules in the `MyCompany.InternalApproval` system.

### 1.1 Dual-Layer Verification Strategy
The test infrastructure employs two complementary test tiers:
1. **In-Process Integration Testing (ABP TestBase + EF Core + SQLite In-Memory)**:
   - Located in: `aspnet-core/test/MyCompany.InternalApproval.EntityFrameworkCore.Tests/` and `aspnet-core/test/MyCompany.InternalApproval.Application.Tests/`
   - Executes with `dotnet test`.
   - Validates entity mappings, database transactions, domain logic, validation constraints, and service contracts in an isolated in-memory relational environment.
   - Includes `EfCoreHierarchicalAuthorizationAndLeaveStatsTests.cs` for automated security scoping and leave accounting verification.
2. **Automated End-to-End (E2E) Test Harness (Standalone Verification Engine)**:
   - Located in: `test/e2e/` (runner: `test/e2e/run-e2e-tests.ps1` and node assertion engine `test/e2e/e2e-runner.js`)
   - Capable of executing either against live HTTP endpoints (`/api/app/employee/*`) or standalone verification harnesses.
   - Verifies end-to-end data contracts, HTTP response status codes (200 OK, 403 Forbidden, 404 Not Found), JSON payload integrity, Vietnamese formatting compliance, cross-feature state mutations, and real-world user journeys.

### 1.2 Test Directory Layout
```
d:\Project\MyCompany.InternalApproval\
├── test\
│   └── e2e\
│       ├── run-e2e-tests.ps1               # Master PowerShell test runner (-Module, -Tier, -ApiUrl, -DotnetTest)
│       ├── e2e-runner.js                   # Node.js automated test runner & assertion engine
│       ├── helpers\
│       │   ├── tenure-oracle.js            # Reference tenure calculation & Vietnamese formatter
│       │   ├── leave-extractor-oracle.js   # 3-tier leave regex & structured date extractor
│       │   ├── stats-oracle.js             # Yearly stats & HR analytics reference engine
│       │   ├── auth-oracle.js              # Hierarchical authorization & scoping reference rules
│       │   └── leave-report-oracle.js      # Leave report, cross-year intersection & Excel validator
│       ├── specs\
│       │   ├── tier1-feature-coverage.test.js              # Module 1: Isolated feature tests (40 tests)
│       │   ├── tier2-boundary-corner.test.js               # Module 1: Boundary & corner cases (11 tests)
│       │   ├── tier3-cross-interactions.test.js            # Module 1: Cross-feature mutations (5 tests)
│       │   ├── tier4-real-world-scenarios.test.js          # Module 1: Lifecycles & HR analytics (4 tests)
│       │   ├── tier1-hierarchical-auth-leave-stats.test.js # Module 2: Hierarchical access & stats (15 tests)
│       │   ├── tier2-hierarchical-auth-boundary-corner.test.js # Module 2: Cross-dept 403 & boundaries (11 tests)
│       │   ├── tier3-hierarchical-auth-cross-interactions.test.js # Module 2: Multi-dept & hybrid roles (13 tests)
│       │   └── tier4-hierarchical-auth-real-world.test.js  # Module 2: Real-world audits & Excel export (8 tests)
│       └── fixtures\
│           └── test-data.json                      # Seed inputs & expected reference values
├── aspnet-core\test\
│   ├── MyCompany.InternalApproval.Application.Tests\
│   │   └── Employees\
│   │       └── EmployeeAppService_Tests.cs         # Abstract test base for Employee services
│   └── MyCompany.InternalApproval.EntityFrameworkCore.Tests\
│       └── EntityFrameworkCore\Applications\
│           ├── EfCoreEmployeeAppServiceTests.cs     # EF Core SQLite in-memory runner
│           ├── Challenger2EmpiricalTests.cs         # Empirical stress test suite (56 tests)
│           └── EfCoreHierarchicalAuthorizationAndLeaveStatsTests.cs # Hierarchical auth & leave stats suite (8 tests)
├── TEST_INFRA.md                                   # This specification
└── TEST_READY.md                                   # Ready-to-run test execution instructions & summary
```

---

## 2. Feature Inventory & Interface Contracts

The testing infrastructure maps directly to requirements from `ORIGINAL_REQUEST.md`:

### Module 1: Employee Profile & Tenure (Milestone 4 - R1 to R4)

| Feature ID | Feature Name | Requirement | Target Endpoint / Class | Expected Interface Contract |
|---|---|---|---|---|
| **F-01** | `JoinDate` Model & Migration | R1 | `DepartmentMember.JoinDate`, `InternalApprovalDbContext` | Nullable `DateTime?`, migration applied cleanly |
| **F-02** | Fallback Rule to `CreationTime` | R1 | `effectiveJoinDate = member.JoinDate ?? user.CreationTime` | If `JoinDate` is null, uses `user.CreationTime` |
| **F-03** | Tenure Calculation & Formatting | R1 | `TenureCalculator.cs` | Vietnamese format: `"2 năm 5 tháng"`, `"8 tháng"`, `"< 1 tháng"`, `"1 năm"` |
| **F-04** | Employee List Endpoint | R1 | `GET /api/app/employee` | Paged `EmployeeListItemDto` with search, dept filter, tenure |
| **F-05** | Employee Profile Detail | R1 | `GET /api/app/employee/{id}` | `EmployeeProfileDto` with current year stats and department details |
| **F-06** | Update `JoinDate` API | R1 | `PUT /api/app/employee/{id}/join-date` | Input: `UpdateEmployeeJoinDateDto`; rejects future dates |
| **F-07** | Employee Request History | R2 | `GET /api/app/employee/{id}/requests` | Paged `RequestDto` with Status, Type, Year filters |
| **F-08** | Yearly Statistics Calculation | R2 | `GET /api/app/employee/{id}/yearly-stats` | Total requests, Draft/Submitted/Approved/Rejected counts, requested vs disbursed amounts, leave days |
| **F-09** | Leave Timeline Extraction | R3 | `GET /api/app/employee/{id}/leave-timeline` | `LeaveTimelineItemDto` with `StartDate`, `EndDate`, `Days`, reason, status |
| **F-10** | HR Analytics & Company Stats | R4 | `GET /api/app/employee/analytics` | `HrAnalyticsDto` (headcount by dept, average tenure by dept, request frequencies) |

### Module 2: Hierarchical Authorization & Leave Statistics (Milestone 5 - R1 to R3)

| Feature ID | Feature Name | Requirement | Target Endpoint / Class | Expected Interface Contract |
|---|---|---|---|---|
| **F-11** | `Employees.ViewAll` Permission | R1 | `InternalApprovalPermissions.Employees.ViewAll` | Registered in permission provider, granted to `admin` |
| **F-12** | Hierarchical Department Access Scoping | R1 | `EmployeeAppService.cs` (`GetListAsync`, `GetAsync`, `GetRequestsAsync`, `GetLeaveTimelineAsync`, `GetYearlyStatsAsync`) | Admin: 100% full company; Manager: managed departments + self; Employee: self only |
| **F-13** | Leave Days Statistics Report API | R2 | `GET /api/app/employee/leave-report` | Input: `GetLeaveReportInput`; Output: `PagedResultDto<EmployeeLeaveReportItemDto>` |
| **F-14** | Cross-Year Boundary Allocation | R2 | `EmployeeAppService.cs` (leave calculation algorithm) | Leave spanning New Year is partitioned into target year calendar window |
| **F-15** | Excel Export Data Structure | R3 | `ExcelExportService` (Angular) & Export Payload | Standard title, metadata block, 10 data columns, summary row |
| **F-16** | Cross-Department JoinDate Constraints | R1 | `PUT /api/app/employee/{id}/join-date` | Managers can only update members in managed departments; rejects cross-department edits with `AbpAuthorizationException` |

---

## 3. 4-Tier Coverage Methodology

The test suite enforces a rigorous 4-tier testing hierarchy to guarantee defect discovery across isolated components, boundary boundaries, integrated features, and real user workflows.

```
+-------------------------------------------------------------------------+
|                  TIER 4: REAL-WORLD SCENARIOS                           |
|   Complete lifecycles, operational audits, Excel export generation      |
+-------------------------------------------------------------------------+
|                TIER 3: CROSS-FEATURE INTERACTIONS                       |
|   Multi-dept manager, hybrid roles, JoinDate scope, approval transitions|
+-------------------------------------------------------------------------+
|                 TIER 2: BOUNDARY & CORNER CASES                         |
|   Cross-dept 403, unmanaged dept 403, 0-leave defaults, cross-year span|
+-------------------------------------------------------------------------+
|                   TIER 1: FEATURE COVERAGE                              |
|   Admin global access, manager scoped access, employee self, stats calc |
+-------------------------------------------------------------------------+
```

### 3.1 Module 1: Employee Profile & Tenure (60 Tests)

#### Tier 1: Feature Coverage (40 Tests)
- **TC-F01 to TC-F05**: `JoinDate` model and fallback to `CreationTime`.
- **TC-F06 to TC-F10**: Vietnamese tenure formatting (`"2 năm 5 tháng"`, `"8 tháng"`, `"< 1 tháng"`).
- **TC-F11 to TC-F15**: Employee list filtering and comprehensive profile detail.
- **TC-F16 to TC-F20**: Update `JoinDate` API, past date validation, future date rejection.
- **TC-F21 to TC-F25**: Employee request history with status, type, and year filters.
- **TC-F26 to TC-F30**: Yearly statistics accounting invariants.
- **TC-F31 to TC-F35**: Leave timeline extraction (structured dates, Vietnamese regex ranges, duration counts).
- **TC-F36 to TC-F40**: HR analytics (headcount distribution, department average tenure, top requesters).

#### Tier 2: Boundary & Corner Cases (11 Tests)
- **TC-B01**: Null `JoinDate` fallback to `CreationTime`.
- **TC-B02**: Future date validation rejection (`AbpValidationException`).
- **TC-B03**: Zero-request inactive employee returns clean zero stats without NRE.
- **TC-B04**: Leap year handling (`2024-02-29`).
- **TC-B05**: Overlapping / multiple leaves in same calendar month.
- **TC-B06**: Same-day tenure boundary (`< 1 tháng`).
- **TC-B07**: Senior long-tenure formatting (`15 năm 6 tháng`).
- **TC-B08**: Non-existent GUID returns HTTP 404 (`EntityNotFoundException`).
- **TC-B09**: Malformed free-text leave fallback to `CreationTime`.
- **TC-B10**: Fractional leave days ("nghỉ 0.5 ngày").
- **TC-B11**: Extreme past dates validation (`JoinDate < 1970-01-01`).

#### Tier 3: Cross-Feature Interactions (5 Tests)
- **TC-C01**: `JoinDate` update cascades to profile, list badge, and department average tenure.
- **TC-C02**: Leave request submission updates leave timeline and increments submitted count.
- **TC-C03**: Request approval transitions financial amounts from requested to disbursed.
- **TC-C04**: Department reassignment recalculates department headcounts and average tenures.
- **TC-C05**: Leave rejection marks timeline status as `Rejected` and excludes days from approved leave days.

#### Tier 4: Real-World Scenarios (4 Tests)
- **TC-S01**: Complete employee onboarding & tenure progression lifecycle.
- **TC-S02**: Employee vacation request, manager review, and approval flow.
- **TC-S03**: Multi-category annual budget and request audit.
- **TC-S04**: Executive HR analytics dashboard and company headcount audit.

---

### 3.2 Module 2: Hierarchical Authorization & Leave Statistics (47 Tests)

#### Tier 1: Feature Coverage (15 Tests)
- **TC-H01**: Admin user evaluates to Global access scope (`isGlobal = true`).
- **TC-H02**: Admin can access all employees across IT, Sales, HR, Finance departments.
- **TC-H03**: Admin can filter employee list by any department freely.
- **TC-H04**: User with `Employees.ViewAll` permission evaluates to Global scope without requiring `admin` role.
- **TC-H05**: Department Manager evaluates to Manager scope with exact set of managed department IDs.
- **TC-H06**: Department Manager can access employees belonging to their managed department.
- **TC-H07**: Department Manager employee list query returns only members of managed departments and self.
- **TC-H08**: Sales Manager accesses Sales department members only.
- **TC-H09**: Regular employee evaluates to Employee scope (`isEmployee = true`).
- **TC-H10**: Regular employee can access their own profile and records.
- **TC-H11**: Regular employee calling list query receives strictly 1 record (self).
- **TC-H12**: `TotalApprovedLeaveDays` counts strictly from requests where `Type == Leave` and `Status == Approved`.
- **TC-H13**: Submitted leave requests increment `TotalSentLeaveRequests` but do NOT increase `TotalApprovedLeaveDays`.
- **TC-H14**: Non-leave requests (Equipment, AdvanceMoney) are completely excluded from leave statistics.
- **TC-H15**: Formats latest leave period correctly for single-day (`"15/05/2026 (1 ngày)"`) and multi-day (`"25/10/2026 - 26/10/2026 (2 ngày)"`).

#### Tier 2: Boundary & Corner Cases (11 Tests)
- **TC-HB01**: IT Manager calling `GetAsync` for a Sales employee throws `AbpAuthorizationException` (HTTP 403).
- **TC-HB02**: IT Manager calling `GetListAsync` with `DepartmentId = SalesDeptId` throws `AbpAuthorizationException`.
- **TC-HB03**: Regular Employee calling `GetAsync` for a colleague throws `AbpAuthorizationException`.
- **TC-HB04**: Regular Employee calling `GetAsync` for their department manager throws `AbpAuthorizationException`.
- **TC-HB05**: Querying non-existent GUID throws `EntityNotFoundException` (HTTP 404), distinguishing from 403.
- **TC-HB06**: Department Manager attempting to access an unassigned employee (no department) is blocked with 403.
- **TC-HB07**: Admin can access unassigned employees without department membership.
- **TC-HB08**: Unassigned employee can access their own profile.
- **TC-HB09**: Querying an empty department (0 members) returns an empty array safely without exceptions.
- **TC-HB10**: Employee with 0 leave requests returns safe defaults: `totalApprovedLeaveDays = 0`, `totalSentLeaveRequests = 0`, `totalApprovedLeaveRequests = 0`, `latestLeavePeriodFormatted = 'Chưa có'`, `latestLeaveDays = null`, `latestLeaveStartDate = null`.
- **TC-HB11**: Cross-Year Boundary: Leave request from `28/12/2025` to `04/01/2026` (8 days) allocates exactly 4 days in Year 2025 and 4 days in Year 2026.

#### Tier 3: Cross-Feature Interactions (13 Tests)
- **TC-HC01**: Multi-department manager managing Dept IT and Dept R&D evaluates to scope containing both department IDs.
- **TC-HC02**: Multi-department manager lists employees from both managed departments simultaneously.
- **TC-HC03**: Multi-department manager can filter by either managed department (IT only, R&D only).
- **TC-HC04**: Multi-department manager querying unmanaged Dept HR is rejected with `AbpAuthorizationException`.
- **TC-HC05**: Cross-department hybrid role (Manager of Dept DX, regular employee in Dept HR) evaluates to Manager scope for Dept DX.
- **TC-HC06**: Hybrid user CAN access staff in Dept DX where they are Manager.
- **TC-HC07**: Hybrid user CANNOT access peers in Dept HR where they are an employee (throws 403).
- **TC-HC08**: Hybrid user CAN access their own profile.
- **TC-HC09**: HR Manager with `Employees.Edit` permission CAN update `JoinDate` of HR department staff.
- **TC-HC10**: HR Manager CANNOT update `JoinDate` of IT staff (throws `AbpAuthorizationException`).
- **TC-HC11**: Regular employee without `Employees.Edit` CANNOT update `JoinDate` of self or anyone (throws 403).
- **TC-HC12**: Admin with `Employees.Edit` CAN update `JoinDate` of any employee across all departments.
- **TC-HC13**: Leave Approval Transitions: Submitting leave request increases submitted count; approving increases both approved count and approved days; rejecting keeps approved days unchanged.

#### Tier 4: Real-World Acceptance Scenarios (8 Tests)
- **TC-HS01**: Scenario 1 - IT Department Manager generates leave report seeing only IT team members with verified 7 approved days.
- **TC-HS02**: Scenario 1 - IT Manager directly attempting to access Sales staff is blocked with `AbpAuthorizationException`.
- **TC-HS03**: Scenario 2 - Sales Department Manager attempting to filter IT department is rejected with `AbpAuthorizationException`.
- **TC-HS04**: Scenario 2 - Sales Manager attempting to access IT developer profile is blocked with 403.
- **TC-HS05**: Scenario 2 - Sales Manager generates report for Sales department showing verified 4 approved leave days.
- **TC-HS06**: Scenario 3 - Admin generates company-wide leave report across all departments showing verified 13 total leave days.
- **TC-HS07**: Scenario 3 - Builds and validates complete Excel export payload: Title, Metadata, 10 standard columns, and Summary row matching company totals.
- **TC-HS08**: Scenario 4 - End-to-end user journey: Onboard new developer -> Dev submits 3-day leave request -> Manager reviews and approves -> Leave statistics report and IT department totals update immediately.

---

## 4. Authoritative Sources of Expected Output

Every test case asserts against verified authoritative sources:
1. **Security & Authorization Scoping Rules**:
   - `ORIGINAL_REQUEST.md § 2026-10-22T08:33:47Z R1`:
     - Admin / `Employees.ViewAll`: 100% full company access.
     - Department Manager: `Department.ManagerId == CurrentUser.Id` — scoped strictly to managed departments.
     - Regular Employee: `CurrentUser.Id == Employee.UserId` — strictly self-only access.
     - Unauthorized cross-department access MUST throw `AbpAuthorizationException` (HTTP 403).
2. **Leave Days Accounting Invariants**:
   - `TotalApprovedLeaveDays = Sum(Approved Leave Request Days in Target Year Window)`.
   - `TotalSentLeaveRequests = Count(Leave Requests with Status != Draft)`.
   - `TotalApprovedLeaveRequests = Count(Leave Requests with Status == Approved)`.
   - Cross-year allocation: `EffectiveDays = (min(EndDate, Dec 31) - max(StartDate, Jan 1)) + 1`.
   - Zero-leave fallback: `"Chưa có"`.
3. **Excel Export Structure Contract**:
   - Title: `"BÁO CÁO THỐNG KÊ NGÀY NGHỈ PHÉP NHÂN VIÊN"`.
   - Metadata: `Năm {year}`, `Phạm vi: {scope}`, `Người xuất: {name}`, `Thời gian: {timestamp}`.
   - Columns: `STT`, `Mã NV / Username`, `Họ và tên`, `Email`, `Phòng ban`, `Chức danh / Vị trí`, `Tổng ngày nghỉ đã duyệt (ngày)`, `Số đợt nghỉ (Duyệt / Đã gửi)`, `Đợt nghỉ gần nhất`, `Lý do đợt nghỉ gần nhất`.
   - Summary Row: `TỔNG CỘNG`, `Tổng ngày nghỉ toàn bộ`, `Tổng số đợt nghỉ (Duyệt / Đã gửi)`.

---

## 5. Automated Test Execution Commands

### 5.1 Standalone E2E Test Suite (Node.js Runner)
```powershell
# Run ALL modules and ALL tiers (107 tests total)
node test/e2e/e2e-runner.js

# Run only Hierarchical Authorization & Leave Statistics module (47 tests)
node test/e2e/e2e-runner.js --module=hierarchical

# Run specific tier within Hierarchical module
node test/e2e/e2e-runner.js --module=hierarchical --tier=1
node test/e2e/e2e-runner.js --module=hierarchical --tier=2
node test/e2e/e2e-runner.js --module=hierarchical --tier=3
node test/e2e/e2e-runner.js --module=hierarchical --tier=4

# Run only Profile & Tenure module (60 tests)
node test/e2e/e2e-runner.js --module=profile
```

### 5.2 Master PowerShell Verification Runner
```powershell
# Run all tests across both modules
.\test\e2e\run-e2e-tests.ps1

# Run hierarchical module specifically
.\test\e2e\run-e2e-tests.ps1 -Module hierarchical

# Run hierarchical module and execute backend EF Core integration tests
.\test\e2e\run-e2e-tests.ps1 -Module hierarchical -DotnetTest
```

### 5.3 Backend In-Process .NET Integration Tests (xUnit)
```powershell
# Run all EF Core test suites
dotnet test aspnet-core/test/MyCompany.InternalApproval.EntityFrameworkCore.Tests/MyCompany.InternalApproval.EntityFrameworkCore.Tests.csproj

# Run specifically the Hierarchical Authorization & Leave Stats test suite
dotnet test aspnet-core/test/MyCompany.InternalApproval.EntityFrameworkCore.Tests/MyCompany.InternalApproval.EntityFrameworkCore.Tests.csproj --filter "FullyQualifiedName~EfCoreHierarchicalAuthorizationAndLeaveStatsTests"
```

const { describe, it } = require('node:test');
const assert = require('node:assert/strict');
const {
  filterEmployeeList,
  canAccessEmployee,
  assertCanAccessEmployee,
  AbpAuthorizationException
} = require('../helpers/auth-oracle');
const {
  calculateEmployeeLeaveStats,
  generateLeaveStatsReport,
  validateExcelExportStructure
} = require('../helpers/leave-report-oracle');
const { RequestType, RequestStatus } = require('../helpers/stats-oracle');

// Fixtures for realistic company structure
const departments = [
  { id: 'dept-it', code: 'IT', name: 'Công nghệ Thông tin', managerId: 'user-manager-it' },
  { id: 'dept-sales', code: 'SALES', name: 'Kinh doanh', managerId: 'user-manager-sales' },
  { id: 'dept-hr', code: 'HR', name: 'Nhân sự', managerId: 'user-manager-hr' }
];

const members = [
  { userId: 'user-manager-it', departmentId: 'dept-it', position: 'Trưởng Phòng IT' },
  { userId: 'user-dev-it', departmentId: 'dept-it', position: 'Kỹ Sư Phần Mềm' },
  { userId: 'user-manager-sales', departmentId: 'dept-sales', position: 'Trưởng Phòng Kinh Doanh' },
  { userId: 'user-staff-sales', departmentId: 'dept-sales', position: 'Chuyên Viên Kinh Doanh' },
  { userId: 'user-manager-hr', departmentId: 'dept-hr', position: 'Trưởng Phòng HR' },
  { userId: 'user-staff-hr', departmentId: 'dept-hr', position: 'Chuyên Viên HR' }
];

const employees = [
  { id: 'user-admin', userId: 'user-admin', userName: 'admin', fullName: 'Quản Trị Viên', role: 'admin', email: 'admin@company.com' },
  { id: 'user-manager-it', userId: 'user-manager-it', userName: 'manager.it', fullName: 'Nguyễn Văn Hùng', role: 'Manager', departmentId: 'dept-it', departmentCode: 'IT', departmentName: 'Công nghệ Thông tin', position: 'Trưởng Phòng IT', isManager: true, email: 'manager.it@company.com' },
  { id: 'user-dev-it', userId: 'user-dev-it', userName: 'dev.it', fullName: 'Lê Minh Tuấn', role: 'Employee', departmentId: 'dept-it', departmentCode: 'IT', departmentName: 'Công nghệ Thông tin', position: 'Kỹ Sư Phần Mềm', isManager: false, email: 'dev.it@company.com' },
  { id: 'user-manager-sales', userId: 'user-manager-sales', userName: 'manager.sales', fullName: 'Vũ Hải Nam', role: 'Manager', departmentId: 'dept-sales', departmentCode: 'SALES', departmentName: 'Kinh doanh', position: 'Trưởng Phòng Kinh Doanh', isManager: true, email: 'manager.sales@company.com' },
  { id: 'user-staff-sales', userId: 'user-staff-sales', userName: 'staff.sales', fullName: 'Ngô Bảo Long', role: 'Employee', departmentId: 'dept-sales', departmentCode: 'SALES', departmentName: 'Kinh doanh', position: 'Chuyên Viên Kinh Doanh', isManager: false, email: 'staff.sales@company.com' },
  { id: 'user-manager-hr', userId: 'user-manager-hr', userName: 'manager.hr', fullName: 'Trần Thị Mai', role: 'Manager', departmentId: 'dept-hr', departmentCode: 'HR', departmentName: 'Nhân sự', position: 'Trưởng Phòng HR', isManager: true, email: 'manager.hr@company.com' },
  { id: 'user-staff-hr', userId: 'user-staff-hr', userName: 'staff.hr', fullName: 'Phạm Thu Hà', role: 'Employee', departmentId: 'dept-hr', departmentCode: 'HR', departmentName: 'Nhân sự', position: 'Chuyên Viên HR', isManager: false, email: 'staff.hr@company.com' }
];

const requests = [
  // IT team requests
  {
    id: 'req-01',
    creatorId: 'user-dev-it',
    type: RequestType.Leave,
    status: RequestStatus.Approved,
    startDate: '2026-10-25T00:00:00Z',
    endDate: '2026-10-26T00:00:00Z',
    title: 'Nghỉ 2 ngày',
    description: 'Nghỉ việc riêng'
  },
  {
    id: 'req-02',
    creatorId: 'user-manager-it',
    type: RequestType.Leave,
    status: RequestStatus.Approved,
    startDate: '2026-07-10T00:00:00Z',
    endDate: '2026-07-14T00:00:00Z',
    title: 'Nghỉ hè gia đình',
    description: 'Nghỉ hè 5 ngày'
  },
  // Sales team requests
  {
    id: 'req-03',
    creatorId: 'user-staff-sales',
    type: RequestType.Leave,
    status: RequestStatus.Approved,
    startDate: '2026-04-12T00:00:00Z',
    endDate: '2026-04-12T00:00:00Z',
    title: 'Nghỉ phép 1 ngày',
    description: 'Khám sức khỏe'
  },
  {
    id: 'req-04',
    creatorId: 'user-manager-sales',
    type: RequestType.Leave,
    status: RequestStatus.Approved,
    startDate: '2026-09-01T00:00:00Z',
    endDate: '2026-09-03T00:00:00Z',
    title: 'Nghỉ phép 3 ngày',
    description: 'Việc gia đình'
  },
  // HR team requests
  {
    id: 'req-05',
    creatorId: 'user-staff-hr',
    type: RequestType.Leave,
    status: RequestStatus.Approved,
    startDate: '2026-03-20T00:00:00Z',
    endDate: '2026-03-21T00:00:00Z',
    title: 'Nghỉ phép 2 ngày',
    description: 'Du lịch'
  }
];

describe('Tier 4: Real-World Acceptance Scenarios (Hierarchical Auth & Leave Stats)', () => {

  describe('Scenario 4.1: IT Department Manager Inspecting IT Team Leave Report', () => {
    const itManager = employees.find(e => e.userName === 'manager.it');
    const staffSales = employees.find(e => e.userName === 'staff.sales');

    it('TC-HS01: IT Manager generates report seeing only IT team members and correct totals', () => {
      const report = generateLeaveStatsReport(
        employees,
        requests,
        2026,
        null,
        itManager,
        departments,
        members
      );

      // Should contain 2 IT members (manager.it and dev.it)
      assert.strictEqual(report.totalEmployees, 2);
      assert.ok(report.items.every(item => item.departmentCode === 'IT'));

      // Total leave days for IT team: manager.it (5 days) + dev.it (2 days) = 7 days
      assert.strictEqual(report.totalCompanyLeaveDays, 7);
      assert.strictEqual(report.totalCompanyLeaveRequestsApproved, 2);
    });

    it('TC-HS02: IT Manager directly querying Sales staff is blocked with AbpAuthorizationException', () => {
      assert.throws(() => {
        assertCanAccessEmployee(itManager, staffSales.id, departments, members, employees);
      }, AbpAuthorizationException);
    });
  });

  describe('Scenario 4.2: Sales Department Manager Blocked from IT Team', () => {
    const salesManager = employees.find(e => e.userName === 'manager.sales');
    const devIt = employees.find(e => e.userName === 'dev.it');

    it('TC-HS03: Sales Manager attempting to filter by IT department is rejected', () => {
      assert.throws(() => {
        generateLeaveStatsReport(
          employees,
          requests,
          2026,
          'dept-it',
          salesManager,
          departments,
          members
        );
      }, AbpAuthorizationException);
    });

    it('TC-HS04: Sales Manager directly accessing IT developer profile is blocked', () => {
      assert.throws(() => {
        assertCanAccessEmployee(salesManager, devIt.id, departments, members, employees);
      }, AbpAuthorizationException);
    });

    it('TC-HS05: Sales Manager generates report for Sales team showing 4 days total', () => {
      const report = generateLeaveStatsReport(
        employees,
        requests,
        2026,
        'dept-sales',
        salesManager,
        departments,
        members
      );

      assert.strictEqual(report.totalEmployees, 2);
      // staff.sales (1 day) + manager.sales (3 days) = 4 days
      assert.strictEqual(report.totalCompanyLeaveDays, 4);
      assert.strictEqual(report.totalCompanyLeaveRequestsApproved, 2);
    });
  });

  describe('Scenario 4.3: Admin Company-wide Audit & Excel Export Verification', () => {
    const adminUser = employees.find(e => e.userName === 'admin');

    it('TC-HS06: Admin generates company-wide leave statistics report across all departments', () => {
      const report = generateLeaveStatsReport(
        employees,
        requests,
        2026,
        null,
        adminUser,
        departments,
        members
      );

      // Total 6 department members (excluding unassigned admin or including all)
      const deptMembersCount = report.items.filter(i => i.departmentId !== null).length;
      assert.strictEqual(deptMembersCount, 6);

      // Total company approved leave days: IT(7) + Sales(4) + HR(2) = 13 days
      assert.strictEqual(report.totalCompanyLeaveDays, 13);
      assert.strictEqual(report.totalCompanyLeaveRequestsApproved, 5);
    });

    it('TC-HS07: Builds and validates Excel export data structure with all required columns and summary', () => {
      const report = generateLeaveStatsReport(
        employees,
        requests,
        2026,
        null,
        adminUser,
        departments,
        members
      );

      const excelPayload = {
        title: 'BÁO CÁO THỐNG KÊ NGÀY NGHỈ PHÉP NHÂN VIÊN',
        metadata: {
          year: `Năm ${report.year}`,
          departmentScope: 'Toàn công ty',
          exportedBy: adminUser.fullName,
          exportedAt: new Date().toISOString()
        },
        columns: [
          'STT',
          'Mã NV / Username',
          'Họ và tên',
          'Email',
          'Phòng ban',
          'Chức danh / Vị trí',
          'Tổng ngày nghỉ đã duyệt (ngày)',
          'Số đợt nghỉ (Duyệt / Đã gửi)',
          'Đợt nghỉ gần nhất',
          'Lý do đợt nghỉ gần nhất'
        ],
        rows: report.items.map((item, index) => [
          index + 1,
          item.userName,
          item.fullName,
          item.email,
          item.departmentName,
          item.position,
          item.totalApprovedLeaveDays,
          `${item.totalLeaveRequestsApproved} / ${item.totalLeaveRequestsSubmitted} đợt`,
          item.latestLeavePeriodFormatted,
          item.latestLeaveReason || ''
        ]),
        summaryRow: {
          totalEmployees: report.totalEmployees,
          totalLeaveDays: report.totalCompanyLeaveDays,
          totalLeaveRequestsApproved: report.totalCompanyLeaveRequestsApproved,
          totalLeaveRequestsSubmitted: report.totalCompanyLeaveRequestsSubmitted
        }
      };

      const validation = validateExcelExportStructure(excelPayload);
      assert.strictEqual(validation.isValid, true, `Excel structure validation failed: ${validation.errors.join(', ')}`);
      assert.strictEqual(excelPayload.summaryRow.totalLeaveDays, 13);
    });
  });

  describe('Scenario 4.4: Full Leave Request & Statistics Lifecycle', () => {
    it('TC-HS08: New employee onboarded -> submits leave -> manager approves -> leave stats updated', () => {
      const newDev = {
        id: 'user-new-dev',
        userId: 'user-new-dev',
        userName: 'new.dev',
        fullName: 'Trần Văn Mới',
        departmentId: 'dept-it',
        departmentCode: 'IT',
        departmentName: 'Công nghệ Thông tin',
        position: 'Lập Trình Viên Mới'
      };

      const dynamicEmployees = [...employees, newDev];
      const dynamicMembers = [...members, { userId: 'user-new-dev', departmentId: 'dept-it', position: 'Lập Trình Viên Mới' }];
      const dynamicRequests = [...requests];

      const itManager = employees.find(e => e.userName === 'manager.it');

      // 1. Initial state: new dev has 0 leave days
      let devStats = calculateEmployeeLeaveStats(newDev, dynamicRequests, 2026);
      assert.strictEqual(devStats.totalApprovedLeaveDays, 0);
      assert.strictEqual(devStats.latestLeavePeriodFormatted, 'Chưa có');

      // 2. New dev submits 3-day leave request
      const newLeaveReq = {
        id: 'req-new-01',
        creatorId: newDev.id,
        type: RequestType.Leave,
        status: RequestStatus.Submitted,
        startDate: '2026-11-20T00:00:00Z',
        endDate: '2026-11-22T00:00:00Z',
        title: 'Nghỉ phép 3 ngày cưới hỏi',
        description: 'Về quê dự đám cưới'
      };
      dynamicRequests.push(newLeaveReq);

      // Still 0 approved days
      devStats = calculateEmployeeLeaveStats(newDev, dynamicRequests, 2026);
      assert.strictEqual(devStats.totalApprovedLeaveDays, 0);
      assert.strictEqual(devStats.totalLeaveRequestsSubmitted, 1);

      // 3. IT Manager approves the leave request
      newLeaveReq.status = RequestStatus.Approved;

      // 4. Leave stats immediately reflects 3 approved days and formatted period
      devStats = calculateEmployeeLeaveStats(newDev, dynamicRequests, 2026);
      assert.strictEqual(devStats.totalApprovedLeaveDays, 3);
      assert.strictEqual(devStats.totalLeaveRequestsApproved, 1);
      assert.strictEqual(devStats.latestLeavePeriodFormatted, '20/11/2026 - 22/11/2026 (3 ngày)');

      // 5. IT Department report total reflects the new 3 days (7 + 3 = 10 days)
      const itReport = generateLeaveStatsReport(
        dynamicEmployees,
        dynamicRequests,
        2026,
        'dept-it',
        itManager,
        departments,
        dynamicMembers
      );
      assert.strictEqual(itReport.totalCompanyLeaveDays, 10);
    });
  });
});

const { describe, it } = require('node:test');
const assert = require('node:assert/strict');
const {
  evaluateAccessScope,
  canAccessEmployee,
  assertCanAccessEmployee,
  filterEmployeeList,
  Permissions
} = require('../helpers/auth-oracle');
const {
  calculateEmployeeLeaveStats,
  generateLeaveStatsReport,
  calculateCrossYearIntersection,
  formatLatestLeavePeriod
} = require('../helpers/leave-report-oracle');
const { RequestType, RequestStatus } = require('../helpers/stats-oracle');

// Fixtures
const testDepartments = [
  { id: 'dept-it', code: 'IT', name: 'Công nghệ Thông tin', managerId: 'user-manager-it' },
  { id: 'dept-sales', code: 'SALES', name: 'Kinh doanh', managerId: 'user-manager-sales' },
  { id: 'dept-hr', code: 'HR', name: 'Nhân sự', managerId: 'user-manager-hr' }
];

const testMembers = [
  { userId: 'user-manager-it', departmentId: 'dept-it', position: 'Trưởng Phòng IT' },
  { userId: 'user-dev-it', departmentId: 'dept-it', position: 'Kỹ Sư Phần Mềm' },
  { userId: 'user-manager-sales', departmentId: 'dept-sales', position: 'Trưởng Phòng Sales' },
  { userId: 'user-staff-sales', departmentId: 'dept-sales', position: 'Chuyên Viên Sales' },
  { userId: 'user-manager-hr', departmentId: 'dept-hr', position: 'Trưởng Phòng HR' },
  { userId: 'user-staff-hr', departmentId: 'dept-hr', position: 'Chuyên Viên HR' }
];

const testEmployees = [
  { id: 'user-admin', userId: 'user-admin', userName: 'admin', fullName: 'Quản Trị Viên', role: 'admin', departmentId: null, position: 'Admin' },
  { id: 'user-manager-it', userId: 'user-manager-it', userName: 'manager.it', fullName: 'Nguyễn Văn Hùng', role: 'Manager', departmentId: 'dept-it', position: 'Trưởng Phòng IT', isManager: true },
  { id: 'user-dev-it', userId: 'user-dev-it', userName: 'dev.it', fullName: 'Lê Minh Tuấn', role: 'Employee', departmentId: 'dept-it', position: 'Kỹ Sư Phần Mềm', isManager: false },
  { id: 'user-manager-sales', userId: 'user-manager-sales', userName: 'manager.sales', fullName: 'Vũ Hải Nam', role: 'Manager', departmentId: 'dept-sales', position: 'Trưởng Phòng Sales', isManager: true },
  { id: 'user-staff-sales', userId: 'user-staff-sales', userName: 'staff.sales', fullName: 'Ngô Bảo Long', role: 'Employee', departmentId: 'dept-sales', position: 'Chuyên Viên Sales', isManager: false },
  { id: 'user-manager-hr', userId: 'user-manager-hr', userName: 'manager.hr', fullName: 'Trần Thị Mai', role: 'Manager', departmentId: 'dept-hr', position: 'Trưởng Phòng HR', isManager: true },
  { id: 'user-staff-hr', userId: 'user-staff-hr', userName: 'staff.hr', fullName: 'Phạm Thu Hà', role: 'Employee', departmentId: 'dept-hr', position: 'Chuyên Viên HR', isManager: false }
];

const testRequests = [
  {
    id: 'req-01',
    creatorId: 'user-dev-it',
    type: RequestType.Leave,
    status: RequestStatus.Approved,
    startDate: '2026-10-25T00:00:00Z',
    endDate: '2026-10-26T00:00:00Z',
    title: 'Nghỉ 2 ngày',
    description: 'Nghỉ giải quyết việc riêng'
  },
  {
    id: 'req-02',
    creatorId: 'user-dev-it',
    type: RequestType.Leave,
    status: RequestStatus.Submitted,
    startDate: '2026-11-10T00:00:00Z',
    endDate: '2026-11-12T00:00:00Z',
    title: 'Nghỉ 3 ngày',
    description: 'Đang chờ duyệt'
  },
  {
    id: 'req-03',
    creatorId: 'user-dev-it',
    type: RequestType.Equipment,
    status: RequestStatus.Approved,
    amount: 5000000,
    title: 'Mua màn hình',
    description: 'Thiết bị làm việc'
  },
  {
    id: 'req-04',
    creatorId: 'user-staff-sales',
    type: RequestType.Leave,
    status: RequestStatus.Approved,
    startDate: '2026-05-15T00:00:00Z',
    endDate: '2026-05-15T00:00:00Z',
    title: 'Nghỉ 1 ngày',
    description: 'Việc cá nhân'
  }
];

describe('Tier 1: Feature Coverage (Hierarchical Auth & Leave Statistics)', () => {

  describe('Feature 1.1: Admin / ViewAll Authority', () => {
    const adminUser = testEmployees.find(e => e.userName === 'admin');

    it('TC-H01: Admin evaluates to Global scope', () => {
      const scope = evaluateAccessScope(adminUser, testDepartments, testMembers);
      assert.strictEqual(scope.isGlobal, true);
      assert.strictEqual(scope.type, 'Admin');
    });

    it('TC-H02: Admin can access any employee across all departments', () => {
      for (const emp of testEmployees) {
        const canAccess = canAccessEmployee(adminUser, emp.id, testDepartments, testMembers);
        assert.strictEqual(canAccess, true, `Admin should have access to ${emp.userName}`);
      }
    });

    it('TC-H03: Admin can filter list by any department', () => {
      const itEmployees = filterEmployeeList(adminUser, testEmployees, testDepartments, testMembers, { departmentId: 'dept-it' });
      assert.strictEqual(itEmployees.length, 2);
      assert.ok(itEmployees.every(e => e.departmentId === 'dept-it'));

      const salesEmployees = filterEmployeeList(adminUser, testEmployees, testDepartments, testMembers, { departmentId: 'dept-sales' });
      assert.strictEqual(salesEmployees.length, 2);
      assert.ok(salesEmployees.every(e => e.departmentId === 'dept-sales'));
    });

    it('TC-H04: User with ViewAll permission evaluates to Global scope without admin role', () => {
      const regularWithPermission = { id: 'auditor-01', role: 'Employee' };
      const scope = evaluateAccessScope(regularWithPermission, testDepartments, testMembers, [Permissions.Employees.ViewAll]);
      assert.strictEqual(scope.isGlobal, true);
    });
  });

  describe('Feature 1.2: Department Manager Scoped Access', () => {
    const itManager = testEmployees.find(e => e.userName === 'manager.it');
    const salesManager = testEmployees.find(e => e.userName === 'manager.sales');

    it('TC-H05: Department Manager evaluates to Manager scope with managed department IDs', () => {
      const scope = evaluateAccessScope(itManager, testDepartments, testMembers);
      assert.strictEqual(scope.isManager, true);
      assert.strictEqual(scope.isGlobal, false);
      assert.ok(scope.managedDepartmentIds.has('dept-it'));
      assert.ok(!scope.managedDepartmentIds.has('dept-sales'));
    });

    it('TC-H06: Manager can access employees within their managed department', () => {
      const devIt = testEmployees.find(e => e.userName === 'dev.it');
      assert.strictEqual(canAccessEmployee(itManager, devIt.id, testDepartments, testMembers), true);
      assert.strictEqual(canAccessEmployee(itManager, itManager.id, testDepartments, testMembers), true);
    });

    it('TC-H07: Manager list query only returns employees in managed departments', () => {
      const accessibleList = filterEmployeeList(itManager, testEmployees, testDepartments, testMembers);
      assert.strictEqual(accessibleList.length, 2);
      assert.ok(accessibleList.some(e => e.userName === 'manager.it'));
      assert.ok(accessibleList.some(e => e.userName === 'dev.it'));
      assert.ok(!accessibleList.some(e => e.userName === 'staff.sales'));
    });

    it('TC-H08: Sales Manager can access Sales department members only', () => {
      const staffSales = testEmployees.find(e => e.userName === 'staff.sales');
      assert.strictEqual(canAccessEmployee(salesManager, staffSales.id, testDepartments, testMembers), true);
      assert.strictEqual(canAccessEmployee(salesManager, salesManager.id, testDepartments, testMembers), true);
    });
  });

  describe('Feature 1.3: Regular Employee Strict Self-Access', () => {
    const devIt = testEmployees.find(e => e.userName === 'dev.it');

    it('TC-H09: Regular employee evaluates to Employee scope', () => {
      const scope = evaluateAccessScope(devIt, testDepartments, testMembers);
      assert.strictEqual(scope.isEmployee, true);
      assert.strictEqual(scope.isGlobal, false);
      assert.strictEqual(scope.isManager, false);
    });

    it('TC-H10: Regular employee can access self', () => {
      assert.strictEqual(canAccessEmployee(devIt, devIt.id, testDepartments, testMembers), true);
    });

    it('TC-H11: Regular employee calling list receives only self', () => {
      const result = filterEmployeeList(devIt, testEmployees, testDepartments, testMembers);
      assert.strictEqual(result.length, 1);
      assert.strictEqual(result[0].id, devIt.id);
    });
  });

  describe('Feature 1.4: Leave Statistics Calculation Engine', () => {
    const devIt = testEmployees.find(e => e.userName === 'dev.it');

    it('TC-H12: Calculates TotalApprovedLeaveDays from approved leave requests only', () => {
      const stats = calculateEmployeeLeaveStats(devIt, testRequests, 2026);
      assert.strictEqual(stats.totalApprovedLeaveDays, 2);
    });

    it('TC-H13: Submitted requests increment totalLeaveRequestsSubmitted but not approved days', () => {
      const stats = calculateEmployeeLeaveStats(devIt, testRequests, 2026);
      assert.strictEqual(stats.totalLeaveRequestsSubmitted, 2); // req-01 (Approved) + req-02 (Submitted)
      assert.strictEqual(stats.totalLeaveRequestsApproved, 1);  // req-01 only
      assert.strictEqual(stats.totalApprovedLeaveDays, 2);       // req-01 (2 days)
    });

    it('TC-H14: Equipment / AdvanceMoney requests are excluded from leave calculations', () => {
      const stats = calculateEmployeeLeaveStats(devIt, testRequests, 2026);
      // req-03 is Equipment, must NOT be counted in leave statistics
      assert.strictEqual(stats.totalLeaveRequestsSubmitted, 2);
    });

    it('TC-H15: Formats latest leave period correctly for multi-day and single-day leave', () => {
      const singleDay = {
        title: 'Nghỉ 1 ngày',
        startDate: '2026-05-15T00:00:00Z',
        endDate: '2026-05-15T00:00:00Z',
        creationTime: '2026-05-10T00:00:00Z'
      };
      const formattedSingle = formatLatestLeavePeriod(singleDay);
      assert.strictEqual(formattedSingle, '15/05/2026 (1 ngày)');

      const multiDay = {
        title: 'Nghỉ 2 ngày',
        startDate: '2026-10-25T00:00:00Z',
        endDate: '2026-10-26T00:00:00Z',
        creationTime: '2026-10-20T00:00:00Z'
      };
      const formattedMulti = formatLatestLeavePeriod(multiDay);
      assert.strictEqual(formattedMulti, '25/10/2026 - 26/10/2026 (2 ngày)');
    });
  });
});

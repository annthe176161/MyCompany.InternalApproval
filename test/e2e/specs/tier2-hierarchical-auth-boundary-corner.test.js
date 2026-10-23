const { describe, it } = require('node:test');
const assert = require('node:assert/strict');
const {
  canAccessEmployee,
  assertCanAccessEmployee,
  filterEmployeeList,
  AbpAuthorizationException,
  EntityNotFoundException
} = require('../helpers/auth-oracle');
const {
  calculateEmployeeLeaveStats,
  calculateCrossYearIntersection
} = require('../helpers/leave-report-oracle');
const { RequestType, RequestStatus } = require('../helpers/stats-oracle');

// Fixtures
const testDepartments = [
  { id: 'dept-it', code: 'IT', name: 'Công nghệ Thông tin', managerId: 'user-manager-it' },
  { id: 'dept-sales', code: 'SALES', name: 'Kinh doanh', managerId: 'user-manager-sales' },
  { id: 'dept-empty', code: 'EMPTY', name: 'Phòng ban mới thành lập', managerId: null }
];

const testMembers = [
  { userId: 'user-manager-it', departmentId: 'dept-it' },
  { userId: 'user-dev-it', departmentId: 'dept-it' },
  { userId: 'user-manager-sales', departmentId: 'dept-sales' },
  { userId: 'user-staff-sales', departmentId: 'dept-sales' }
];

const testEmployees = [
  { id: 'user-admin', userId: 'user-admin', userName: 'admin', role: 'admin', departmentId: null },
  { id: 'user-manager-it', userId: 'user-manager-it', userName: 'manager.it', role: 'Manager', departmentId: 'dept-it' },
  { id: 'user-dev-it', userId: 'user-dev-it', userName: 'dev.it', role: 'Employee', departmentId: 'dept-it' },
  { id: 'user-manager-sales', userId: 'user-manager-sales', userName: 'manager.sales', role: 'Manager', departmentId: 'dept-sales' },
  { id: 'user-staff-sales', userId: 'user-staff-sales', userName: 'staff.sales', role: 'Employee', departmentId: 'dept-sales' },
  { id: 'user-unassigned', userId: 'user-unassigned', userName: 'new.joiner', role: 'Employee', departmentId: null }
];

describe('Tier 2: Boundary & Corner Cases (Hierarchical Auth & Leave Stats)', () => {

  describe('Boundary 2.1: Cross-Department Unauthorized Access Blocking', () => {
    const itManager = testEmployees.find(e => e.userName === 'manager.it');
    const staffSales = testEmployees.find(e => e.userName === 'staff.sales');
    const devIt = testEmployees.find(e => e.userName === 'dev.it');

    it('TC-HB01: IT Manager attempting to access Sales staff throws AbpAuthorizationException', () => {
      assert.throws(() => {
        assertCanAccessEmployee(itManager, staffSales.id, testDepartments, testMembers, testEmployees);
      }, (err) => {
        assert.ok(err instanceof AbpAuthorizationException);
        assert.strictEqual(err.statusCode, 403);
        return true;
      });
    });

    it('TC-HB02: IT Manager querying list with unmanaged DepartmentId throws AbpAuthorizationException', () => {
      assert.throws(() => {
        filterEmployeeList(itManager, testEmployees, testDepartments, testMembers, { departmentId: 'dept-sales' });
      }, (err) => {
        assert.ok(err instanceof AbpAuthorizationException);
        assert.strictEqual(err.statusCode, 403);
        return true;
      });
    });

    it('TC-HB03: Regular Employee attempting to access colleague profile throws AbpAuthorizationException', () => {
      assert.throws(() => {
        assertCanAccessEmployee(devIt, staffSales.id, testDepartments, testMembers, testEmployees);
      }, (err) => {
        assert.ok(err instanceof AbpAuthorizationException);
        assert.strictEqual(err.statusCode, 403);
        return true;
      });
    });

    it('TC-HB04: Regular Employee attempting to access manager profile throws AbpAuthorizationException', () => {
      assert.throws(() => {
        assertCanAccessEmployee(devIt, itManager.id, testDepartments, testMembers, testEmployees);
      }, (err) => {
        assert.ok(err instanceof AbpAuthorizationException);
        assert.strictEqual(err.statusCode, 403);
        return true;
      });
    });
  });

  describe('Boundary 2.2: Non-Existent User ID vs Authorization Precedence', () => {
    const itManager = testEmployees.find(e => e.userName === 'manager.it');
    const nonExistentId = '00000000-0000-0000-0000-000000000000';

    it('TC-HB05: Non-existent user ID throws EntityNotFoundException (404) not AuthorizationException', () => {
      assert.throws(() => {
        assertCanAccessEmployee(itManager, nonExistentId, testDepartments, testMembers, testEmployees);
      }, (err) => {
        assert.ok(err instanceof EntityNotFoundException);
        assert.strictEqual(err.statusCode, 404);
        return true;
      });
    });
  });

  describe('Boundary 2.3: Unassigned Employee Access Scoping', () => {
    const itManager = testEmployees.find(e => e.userName === 'manager.it');
    const adminUser = testEmployees.find(e => e.userName === 'admin');
    const unassigned = testEmployees.find(e => e.userName === 'new.joiner');

    it('TC-HB06: Department Manager cannot access unassigned employee (throws 403)', () => {
      assert.throws(() => {
        assertCanAccessEmployee(itManager, unassigned.id, testDepartments, testMembers, testEmployees);
      }, AbpAuthorizationException);
    });

    it('TC-HB07: Admin can access unassigned employee', () => {
      assert.doesNotThrow(() => {
        assertCanAccessEmployee(adminUser, unassigned.id, testDepartments, testMembers, testEmployees);
      });
    });

    it('TC-HB08: Unassigned employee can access self', () => {
      assert.doesNotThrow(() => {
        assertCanAccessEmployee(unassigned, unassigned.id, testDepartments, testMembers, testEmployees);
      });
    });
  });

  describe('Boundary 2.4: Empty Department Handling', () => {
    const adminUser = testEmployees.find(e => e.userName === 'admin');

    it('TC-HB09: Empty department returns empty array without throwing exceptions', () => {
      const results = filterEmployeeList(adminUser, testEmployees, testDepartments, testMembers, { departmentId: 'dept-empty' });
      assert.strictEqual(results.length, 0);
    });
  });

  describe('Boundary 2.5: Zero-Leave Employee Inactivity Defaults', () => {
    const unassigned = testEmployees.find(e => e.userName === 'new.joiner');

    it('TC-HB10: Employee with zero leave requests returns clean zero stats and "Chưa có" label', () => {
      const stats = calculateEmployeeLeaveStats(unassigned, [], 2026);
      assert.strictEqual(stats.totalApprovedLeaveDays, 0);
      assert.strictEqual(stats.totalLeaveRequestsSubmitted, 0);
      assert.strictEqual(stats.totalLeaveRequestsApproved, 0);
      assert.strictEqual(stats.latestLeavePeriodFormatted, 'Chưa có');
      assert.strictEqual(stats.latestLeaveStartDate, null);
      assert.strictEqual(stats.latestLeaveEndDate, null);
      assert.strictEqual(stats.latestLeaveDays, 0);
    });
  });

  describe('Boundary 2.6: Cross-Year Boundary Leave Intersection', () => {
    it('TC-HB11: Correctly partitions leave spanning New Year boundary', () => {
      // 28/12/2025 to 04/01/2026 = 8 days total
      const start = '2025-12-28T00:00:00Z';
      const end = '2026-01-04T00:00:00Z';

      // In 2025: Dec 28, 29, 30, 31 = 4 days
      const days2025 = calculateCrossYearIntersection(start, end, 2025);
      assert.strictEqual(days2025, 4);

      // In 2026: Jan 1, 2, 3, 4 = 4 days
      const days2026 = calculateCrossYearIntersection(start, end, 2026);
      assert.strictEqual(days2026, 4);

      // In 2024: 0 days
      const days2024 = calculateCrossYearIntersection(start, end, 2024);
      assert.strictEqual(days2024, 0);

      // Sum of partitioned days equals total leave duration
      assert.strictEqual(days2025 + days2026, 8);
    });
  });
});

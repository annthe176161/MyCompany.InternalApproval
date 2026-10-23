const { describe, it } = require('node:test');
const assert = require('node:assert/strict');
const {
  evaluateAccessScope,
  canAccessEmployee,
  assertCanAccessEmployee,
  filterEmployeeList,
  assertCanUpdateJoinDate,
  AbpAuthorizationException,
  Permissions
} = require('../helpers/auth-oracle');
const {
  calculateEmployeeLeaveStats,
  generateLeaveStatsReport
} = require('../helpers/leave-report-oracle');
const { RequestType, RequestStatus } = require('../helpers/stats-oracle');

// Fixtures for Multi-Dept and Hybrid Roles
const departments = [
  { id: 'dept-it', code: 'IT', name: 'Công nghệ Thông tin', managerId: 'user-manager-multi' },
  { id: 'dept-rd', code: 'RD', name: 'Nghiên cứu & Phát triển', managerId: 'user-manager-multi' },
  { id: 'dept-hr', code: 'HR', name: 'Nhân sự', managerId: 'user-manager-hr' },
  { id: 'dept-dx', code: 'DX', name: 'Chuyển đổi số', managerId: 'user-hybrid' }
];

const members = [
  { userId: 'user-manager-multi', departmentId: 'dept-it', position: 'Trưởng Khối IT' },
  { userId: 'user-dev-it', departmentId: 'dept-it', position: 'Kỹ Sư Phần Mềm' },
  { userId: 'user-researcher-rd', departmentId: 'dept-rd', position: 'Chuyên Viên R&D' },
  { userId: 'user-manager-hr', departmentId: 'dept-hr', position: 'Trưởng Phòng HR' },
  { userId: 'user-hybrid', departmentId: 'dept-hr', position: 'Chuyên Viên HR' }, // Member in HR
  { userId: 'user-hybrid', departmentId: 'dept-dx', position: 'Trưởng Ban DX' },   // Manager in DX
  { userId: 'user-staff-dx', departmentId: 'dept-dx', position: 'Chuyên Viên DX' },
  { userId: 'user-staff-hr', departmentId: 'dept-hr', position: 'Chuyên Viên Tuyển Dụng' }
];

const employees = [
  { id: 'user-admin', userId: 'user-admin', userName: 'admin', role: 'admin' },
  { id: 'user-manager-multi', userId: 'user-manager-multi', userName: 'manager.multi', role: 'Manager', departmentId: 'dept-it' },
  { id: 'user-dev-it', userId: 'user-dev-it', userName: 'dev.it', role: 'Employee', departmentId: 'dept-it' },
  { id: 'user-researcher-rd', userId: 'user-researcher-rd', userName: 'researcher.rd', role: 'Employee', departmentId: 'dept-rd' },
  { id: 'user-manager-hr', userId: 'user-manager-hr', userName: 'manager.hr', role: 'Manager', departmentId: 'dept-hr' },
  { id: 'user-hybrid', userId: 'user-hybrid', userName: 'staff.hybrid', role: 'Manager', departmentId: 'dept-dx' },
  { id: 'user-staff-dx', userId: 'user-staff-dx', userName: 'staff.dx', role: 'Employee', departmentId: 'dept-dx' },
  { id: 'user-staff-hr', userId: 'user-staff-hr', userName: 'staff.hr', role: 'Employee', departmentId: 'dept-hr' }
];

describe('Tier 3: Cross-Feature Interactions (Hierarchical Auth & Leave Stats)', () => {

  describe('Interaction 3.1: Manager of Multiple Departments (Multi-Department Manager)', () => {
    const multiManager = employees.find(e => e.userName === 'manager.multi');

    it('TC-HC01: Multi-department manager evaluates to scope containing all managed departments', () => {
      const scope = evaluateAccessScope(multiManager, departments, members);
      assert.strictEqual(scope.isManager, true);
      assert.strictEqual(scope.managedDepartmentIds.size, 2);
      assert.ok(scope.managedDepartmentIds.has('dept-it'));
      assert.ok(scope.managedDepartmentIds.has('dept-rd'));
      assert.ok(!scope.managedDepartmentIds.has('dept-hr'));
    });

    it('TC-HC02: Multi-department manager lists employees from both managed departments', () => {
      const list = filterEmployeeList(multiManager, employees, departments, members);
      const userNames = list.map(e => e.userName);
      assert.ok(userNames.includes('manager.multi'));
      assert.ok(userNames.includes('dev.it'));
      assert.ok(userNames.includes('researcher.rd'));
      assert.ok(!userNames.includes('staff.hr'));
    });

    it('TC-HC03: Multi-department manager can filter by either managed department', () => {
      const itOnly = filterEmployeeList(multiManager, employees, departments, members, { departmentId: 'dept-it' });
      assert.ok(itOnly.every(e => e.departmentId === 'dept-it'));

      const rdOnly = filterEmployeeList(multiManager, employees, departments, members, { departmentId: 'dept-rd' });
      assert.ok(rdOnly.every(e => e.departmentId === 'dept-rd'));
    });

    it('TC-HC04: Multi-department manager querying unmanaged department is rejected', () => {
      assert.throws(() => {
        filterEmployeeList(multiManager, employees, departments, members, { departmentId: 'dept-hr' });
      }, AbpAuthorizationException);
    });
  });

  describe('Interaction 3.2: Cross-Department Hybrid Role (Manager in Dept A, Employee in Dept B)', () => {
    const hybridUser = employees.find(e => e.userName === 'staff.hybrid');
    const staffDx = employees.find(e => e.userName === 'staff.dx');
    const staffHr = employees.find(e => e.userName === 'staff.hr');

    it('TC-HC05: Hybrid user has Manager scope for Dept DX', () => {
      const scope = evaluateAccessScope(hybridUser, departments, members);
      assert.strictEqual(scope.isManager, true);
      assert.ok(scope.managedDepartmentIds.has('dept-dx'));
      assert.ok(!scope.managedDepartmentIds.has('dept-hr'));
    });

    it('TC-HC06: Hybrid user CAN access staff in Dept DX where they are Manager', () => {
      assert.strictEqual(canAccessEmployee(hybridUser, staffDx.id, departments, members), true);
    });

    it('TC-HC07: Hybrid user CANNOT access colleagues in Dept HR where they are just an employee', () => {
      assert.strictEqual(canAccessEmployee(hybridUser, staffHr.id, departments, members), false);
      assert.throws(() => {
        assertCanAccessEmployee(hybridUser, staffHr.id, departments, members, employees);
      }, AbpAuthorizationException);
    });

    it('TC-HC08: Hybrid user CAN access their own profile', () => {
      assert.strictEqual(canAccessEmployee(hybridUser, hybridUser.id, departments, members), true);
    });
  });

  describe('Interaction 3.3: Update JoinDate Permission & Department Scope Enforcement', () => {
    const managerHr = employees.find(e => e.userName === 'manager.hr');
    const staffHr = employees.find(e => e.userName === 'staff.hr');
    const devIt = employees.find(e => e.userName === 'dev.it');
    const adminUser = employees.find(e => e.userName === 'admin');

    it('TC-HC09: HR Manager with Employees.Edit CAN update JoinDate of HR staff', () => {
      assert.doesNotThrow(() => {
        assertCanUpdateJoinDate(managerHr, staffHr.id, departments, members, employees, [Permissions.Employees.Edit]);
      });
    });

    it('TC-HC10: HR Manager CANNOT update JoinDate of IT staff (throws AbpAuthorizationException)', () => {
      assert.throws(() => {
        assertCanUpdateJoinDate(managerHr, devIt.id, departments, members, employees, [Permissions.Employees.Edit]);
      }, (err) => {
        assert.ok(err instanceof AbpAuthorizationException);
        assert.strictEqual(err.statusCode, 403);
        return true;
      });
    });

    it('TC-HC11: Regular employee without Edit permission CANNOT update JoinDate (throws AbpAuthorizationException)', () => {
      assert.throws(() => {
        assertCanUpdateJoinDate(devIt, devIt.id, departments, members, employees, []);
      }, AbpAuthorizationException);
    });

    it('TC-HC12: Admin CAN update JoinDate of any employee across all departments', () => {
      assert.doesNotThrow(() => {
        assertCanUpdateJoinDate(adminUser, devIt.id, departments, members, employees, [Permissions.Employees.Edit]);
        assertCanUpdateJoinDate(adminUser, staffHr.id, departments, members, employees, [Permissions.Employees.Edit]);
      });
    });
  });

  describe('Interaction 3.4: Leave Approval State Transitions Impact on Statistics', () => {
    const devIt = employees.find(e => e.userName === 'dev.it');

    it('TC-HC13: Submitting leave increases submitted count but not approved days; approving increases both', () => {
      const dynamicRequests = [
        {
          id: 'req-dyn-01',
          creatorId: devIt.id,
          type: RequestType.Leave,
          status: RequestStatus.Draft,
          startDate: '2026-08-01T00:00:00Z',
          endDate: '2026-08-03T00:00:00Z',
          title: 'Draft leave'
        }
      ];

      // State 1: Draft
      let stats = calculateEmployeeLeaveStats(devIt, dynamicRequests, 2026);
      assert.strictEqual(stats.totalLeaveRequestsSubmitted, 0);
      assert.strictEqual(stats.totalLeaveRequestsApproved, 0);
      assert.strictEqual(stats.totalApprovedLeaveDays, 0);

      // State 2: Submitted
      dynamicRequests[0].status = RequestStatus.Submitted;
      stats = calculateEmployeeLeaveStats(devIt, dynamicRequests, 2026);
      assert.strictEqual(stats.totalLeaveRequestsSubmitted, 1);
      assert.strictEqual(stats.totalLeaveRequestsApproved, 0);
      assert.strictEqual(stats.totalApprovedLeaveDays, 0);

      // State 3: Approved
      dynamicRequests[0].status = RequestStatus.Approved;
      stats = calculateEmployeeLeaveStats(devIt, dynamicRequests, 2026);
      assert.strictEqual(stats.totalLeaveRequestsSubmitted, 1);
      assert.strictEqual(stats.totalLeaveRequestsApproved, 1);
      assert.strictEqual(stats.totalApprovedLeaveDays, 3);

      // State 4: Rejected (e.g. if rejected instead of approved)
      dynamicRequests[0].status = RequestStatus.Rejected;
      stats = calculateEmployeeLeaveStats(devIt, dynamicRequests, 2026);
      assert.strictEqual(stats.totalLeaveRequestsSubmitted, 1);
      assert.strictEqual(stats.totalLeaveRequestsApproved, 0);
      assert.strictEqual(stats.totalApprovedLeaveDays, 0);
    });
  });
});

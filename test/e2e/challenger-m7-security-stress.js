/**
 * Security Boundary Stress & Penetration Test Harness (Milestone 7)
 * Author: Security Boundary Challenger
 * 
 * Adversarial stress testing for Hierarchical Department Authorization:
 * - Cross-department query isolation
 * - Department manager scope limits
 * - Employee self-only scope
 * - Admin / ViewAll full view
 * - Multi-department manager & edge conditions
 * - Unmanaged department injection attacks
 */

const assert = require('assert');
const {
  evaluateAccessScope,
  canAccessEmployee,
  assertCanAccessEmployee,
  filterEmployeeList,
  assertCanUpdateJoinDate,
  AbpAuthorizationException,
  EntityNotFoundException,
  Permissions
} = require('./helpers/auth-oracle');
const {
  calculateEmployeeLeaveStats,
  generateLeaveStatsReport,
  calculateCrossYearIntersection
} = require('./helpers/leave-report-oracle');
const { RequestType, RequestStatus } = require('./helpers/stats-oracle');

console.log('================================================================');
console.log('       SECURITY BOUNDARY ADVERSARIAL STRESS HARNESS (M7)        ');
console.log('================================================================\n');

let totalChecks = 0;
let passedChecks = 0;
let failedChecks = 0;

function runCheck(name, fn) {
  totalChecks++;
  try {
    fn();
    passedChecks++;
    console.log(`  [PASS] ${name}`);
  } catch (err) {
    failedChecks++;
    console.error(`  [FAIL] ${name}: ${err.message}`);
  }
}

// ---------------------------------------------------------------------
// Setup Complex Multi-Department Synthetic Enterprise Environment
// ---------------------------------------------------------------------
const departments = [
  { id: 'dept-hq', code: 'HQ', name: 'Ban Giám Đốc', managerId: 'user-ceo' },
  { id: 'dept-it', code: 'IT', name: 'Công nghệ Thông tin', managerId: 'user-manager-it' },
  { id: 'dept-rd', code: 'RD', name: 'Nghiên cứu & Phát triển', managerId: 'user-manager-it' }, // Multi-dept manager!
  { id: 'dept-sales', code: 'SALES', name: 'Kinh Doanh', managerId: 'user-manager-sales' },
  { id: 'dept-mkt', code: 'MKT', name: 'Marketing', managerId: 'user-manager-sales' }, // Multi-dept manager!
  { id: 'dept-hr', code: 'HR', name: 'Nhân Sự', managerId: 'user-manager-hr' },
  { id: 'dept-fin', code: 'FIN', name: 'Tài Chính', managerId: 'user-manager-fin' },
  { id: 'dept-empty', code: 'EMPTY', name: 'Phòng ban mới', managerId: null }
];

const employees = [];
const members = [];

// 1. Admin & CEO
employees.push({ id: 'user-admin', userId: 'user-admin', userName: 'admin', role: 'admin', departmentId: null, fullName: 'Admin' });
employees.push({ id: 'user-ceo', userId: 'user-ceo', userName: 'ceo', role: 'Manager', departmentId: 'dept-hq', fullName: 'General Director' });
members.push({ userId: 'user-ceo', departmentId: 'dept-hq', position: 'CEO' });

// 2. Managers
employees.push({ id: 'user-manager-it', userId: 'user-manager-it', userName: 'manager.it', role: 'Manager', departmentId: 'dept-it', fullName: 'IT Director' });
members.push({ userId: 'user-manager-it', departmentId: 'dept-it', position: 'Director' });

employees.push({ id: 'user-manager-sales', userId: 'user-manager-sales', userName: 'manager.sales', role: 'Manager', departmentId: 'dept-sales', fullName: 'Sales Director' });
members.push({ userId: 'user-manager-sales', departmentId: 'dept-sales', position: 'Director' });

employees.push({ id: 'user-manager-hr', userId: 'user-manager-hr', userName: 'manager.hr', role: 'Manager', departmentId: 'dept-hr', fullName: 'HR Director' });
members.push({ userId: 'user-manager-hr', departmentId: 'dept-hr', position: 'Director' });

employees.push({ id: 'user-manager-fin', userId: 'user-manager-fin', userName: 'manager.fin', role: 'Manager', departmentId: 'dept-fin', fullName: 'Finance Director' });
members.push({ userId: 'user-manager-fin', departmentId: 'dept-fin', position: 'Director' });

// 3. Auditors / ViewAll granted staff
employees.push({ id: 'user-auditor', userId: 'user-auditor', userName: 'internal.auditor', role: 'Employee', departmentId: 'dept-fin', fullName: 'Internal Auditor' });
members.push({ userId: 'user-auditor', departmentId: 'dept-fin', position: 'Auditor' });

// 4. Regular Employees in various departments (5 in each dept)
const deptKeys = ['dept-it', 'dept-rd', 'dept-sales', 'dept-mkt', 'dept-hr', 'dept-fin'];
deptKeys.forEach((deptId, dIdx) => {
  for (let e = 1; e <= 5; e++) {
    const id = `user-${deptId}-emp-${e}`;
    employees.push({
      id: id,
      userId: id,
      userName: `emp.${deptId}.${e}`,
      role: 'Employee',
      departmentId: deptId,
      fullName: `Staff ${deptId} ${e}`
    });
    members.push({
      userId: id,
      departmentId: deptId,
      position: `Staff Level ${e}`
    });
  }
});

// 5. Unassigned employees (no department)
employees.push({ id: 'user-unassigned-1', userId: 'user-unassigned-1', userName: 'unassigned.1', role: 'Employee', departmentId: null, fullName: 'Unassigned 1' });
employees.push({ id: 'user-unassigned-2', userId: 'user-unassigned-2', userName: 'unassigned.2', role: 'Employee', departmentId: null, fullName: 'Unassigned 2' });

console.log(`Environment initialized: ${departments.length} departments, ${employees.length} employees, ${members.length} memberships.\n`);

// ---------------------------------------------------------------------
// TEST SUITE 1: Matrix Exhaustive Cross-Access Verification (NxN pairs)
// ---------------------------------------------------------------------
console.log('--- 1. Matrix Cross-Access Verification (NxN pairs) ---');

runCheck('Admin has 100% access across ALL employees (Nx1 pairs)', () => {
  const admin = employees.find(e => e.userName === 'admin');
  for (const target of employees) {
    const canAccess = canAccessEmployee(admin, target.id, departments, members);
    assert.strictEqual(canAccess, true, `Admin should access ${target.userName}`);
  }
});

runCheck('User with ViewAll permission has 100% access across ALL employees', () => {
  const auditor = employees.find(e => e.userName === 'internal.auditor');
  for (const target of employees) {
    const canAccess = canAccessEmployee(auditor, target.id, departments, members, [Permissions.Employees.ViewAll]);
    assert.strictEqual(canAccess, true, `Auditor with ViewAll should access ${target.userName}`);
  }
});

runCheck('Multi-dept Manager (IT & RD) can access IT and RD staff, but 0% of other departments', () => {
  const itManager = employees.find(e => e.userName === 'manager.it');
  let allowedCount = 0;
  let blockedCount = 0;

  for (const target of employees) {
    const canAccess = canAccessEmployee(itManager, target.id, departments, members);
    const targetMember = members.find(m => m.userId === target.id);
    const isSelf = target.id === itManager.id;
    const isManaged = targetMember && (targetMember.departmentId === 'dept-it' || targetMember.departmentId === 'dept-rd');

    if (isSelf || isManaged) {
      assert.strictEqual(canAccess, true, `IT Manager should access ${target.userName}`);
      allowedCount++;
    } else {
      assert.strictEqual(canAccess, false, `IT Manager MUST NOT access ${target.userName}`);
      blockedCount++;
    }
  }

  // 1 self + 5 IT staff + 5 RD staff = 11 allowed
  assert.strictEqual(allowedCount, 11);
  assert.strictEqual(blockedCount, employees.length - 11);
});

runCheck('Single-dept Manager (HR) can access only HR staff and self, but 0% of other departments', () => {
  const hrManager = employees.find(e => e.userName === 'manager.hr');
  let allowedCount = 0;
  let blockedCount = 0;

  for (const target of employees) {
    const canAccess = canAccessEmployee(hrManager, target.id, departments, members);
    const targetMember = members.find(m => m.userId === target.id);
    const isSelf = target.id === hrManager.id;
    const isManaged = targetMember && targetMember.departmentId === 'dept-hr';

    if (isSelf || isManaged) {
      assert.strictEqual(canAccess, true, `HR Manager should access ${target.userName}`);
      allowedCount++;
    } else {
      assert.strictEqual(canAccess, false, `HR Manager MUST NOT access ${target.userName}`);
      blockedCount++;
    }
  }

  // 1 self + 5 HR staff = 6 allowed
  assert.strictEqual(allowedCount, 6);
  assert.strictEqual(blockedCount, employees.length - 6);
});

runCheck('Regular Employee can ONLY access self (exactly 1 target)', () => {
  const regularEmp = employees.find(e => e.userName === 'emp.dept-it.1');
  let allowedCount = 0;
  let blockedCount = 0;

  for (const target of employees) {
    const canAccess = canAccessEmployee(regularEmp, target.id, departments, members);
    if (target.id === regularEmp.id) {
      assert.strictEqual(canAccess, true, 'Regular employee must access self');
      allowedCount++;
    } else {
      assert.strictEqual(canAccess, false, `Regular employee MUST NOT access ${target.userName}`);
      blockedCount++;
    }
  }

  assert.strictEqual(allowedCount, 1);
  assert.strictEqual(blockedCount, employees.length - 1);
});

// ---------------------------------------------------------------------
// TEST SUITE 2: Department Filter Injection & Penetration Attacks
// ---------------------------------------------------------------------
console.log('\n--- 2. Department Filter Injection & Scope Boundary Attacks ---');

runCheck('Manager injecting unmanaged DepartmentId throws AbpAuthorizationException', () => {
  const hrManager = employees.find(e => e.userName === 'manager.hr');
  const unmanagedDeptIds = ['dept-it', 'dept-sales', 'dept-fin', 'dept-hq', 'dept-empty'];

  for (const unmanagedDeptId of unmanagedDeptIds) {
    assert.throws(() => {
      filterEmployeeList(hrManager, employees, departments, members, { departmentId: unmanagedDeptId });
    }, (err) => {
      assert.ok(err instanceof AbpAuthorizationException);
      assert.strictEqual(err.statusCode, 403);
      return true;
    }, `HR Manager should be blocked from filtering ${unmanagedDeptId}`);
  }
});

runCheck('Manager querying legitimate managed DepartmentId succeeds with 0 cross-department leak', () => {
  const itManager = employees.find(e => e.userName === 'manager.it');
  
  const itResults = filterEmployeeList(itManager, employees, departments, members, { departmentId: 'dept-it' });
  assert.ok(itResults.length > 0);
  assert.ok(itResults.every(e => e.departmentId === 'dept-it'));

  const rdResults = filterEmployeeList(itManager, employees, departments, members, { departmentId: 'dept-rd' });
  assert.ok(rdResults.length > 0);
  assert.ok(rdResults.every(e => e.departmentId === 'dept-rd'));
});

runCheck('Regular Employee filtering by any department receives ONLY self if matching, else empty (never leak colleagues)', () => {
  const itEmp = employees.find(e => e.userName === 'emp.dept-it.1');

  // Filter by own department: returns only self!
  const ownDeptResults = filterEmployeeList(itEmp, employees, departments, members, { departmentId: 'dept-it' });
  assert.strictEqual(ownDeptResults.length, 1);
  assert.strictEqual(ownDeptResults[0].id, itEmp.id);

  // Filter by foreign department: returns 0 items!
  const foreignDeptResults = filterEmployeeList(itEmp, employees, departments, members, { departmentId: 'dept-sales' });
  assert.strictEqual(foreignDeptResults.length, 0);
});

// ---------------------------------------------------------------------
// TEST SUITE 3: Unauthenticated & Identity Edge Cases
// ---------------------------------------------------------------------
console.log('\n--- 3. Unauthenticated & Edge Case Security Tests ---');

runCheck('Null or undefined caller throws AbpAuthorizationException', () => {
  assert.throws(() => {
    evaluateAccessScope(null, departments, members);
  }, AbpAuthorizationException);

  assert.throws(() => {
    evaluateAccessScope({}, departments, members);
  }, AbpAuthorizationException);

  assert.strictEqual(canAccessEmployee(null, 'user-admin', departments, members), false);
  assert.strictEqual(canAccessEmployee({}, 'user-admin', departments, members), false);
});

runCheck('Non-existent target user ID throws EntityNotFoundException (404), not silent failure', () => {
  const admin = employees.find(e => e.userName === 'admin');
  const itManager = employees.find(e => e.userName === 'manager.it');
  const fakeId = '00000000-dead-beef-0000-000000000000';

  assert.throws(() => {
    assertCanAccessEmployee(admin, fakeId, departments, members, employees);
  }, EntityNotFoundException);

  assert.throws(() => {
    assertCanAccessEmployee(itManager, fakeId, departments, members, employees);
  }, EntityNotFoundException);
});

runCheck('Unassigned employees (no department) cannot be accessed by department managers', () => {
  const itManager = employees.find(e => e.userName === 'manager.it');
  const unassigned = employees.find(e => e.userName === 'unassigned.1');

  assert.strictEqual(canAccessEmployee(itManager, unassigned.id, departments, members), false);
  assert.throws(() => {
    assertCanAccessEmployee(itManager, unassigned.id, departments, members, employees);
  }, AbpAuthorizationException);
});

runCheck('Unassigned employees can be accessed by Admin', () => {
  const admin = employees.find(e => e.userName === 'admin');
  const unassigned = employees.find(e => e.userName === 'unassigned.1');

  assert.strictEqual(canAccessEmployee(admin, unassigned.id, departments, members), true);
  assert.doesNotThrow(() => {
    assertCanAccessEmployee(admin, unassigned.id, departments, members, employees);
  });
});

// ---------------------------------------------------------------------
// TEST SUITE 4: Leave Stats Report Hierarchical Aggregation Security
// ---------------------------------------------------------------------
console.log('\n--- 4. Leave Stats Report Hierarchical Isolation ---');

const syntheticRequests = [
  // IT team
  { id: 'req-it-1', creatorId: 'user-manager-it', type: RequestType.Leave, status: RequestStatus.Approved, startDate: '2026-06-01T00:00:00Z', endDate: '2026-06-03T00:00:00Z', title: '3 days' },
  { id: 'req-it-2', creatorId: 'user-dept-it-emp-1', type: RequestType.Leave, status: RequestStatus.Approved, startDate: '2026-07-01T00:00:00Z', endDate: '2026-07-02T00:00:00Z', title: '2 days' },
  // Sales team
  { id: 'req-sales-1', creatorId: 'user-manager-sales', type: RequestType.Leave, status: RequestStatus.Approved, startDate: '2026-08-01T00:00:00Z', endDate: '2026-08-05T00:00:00Z', title: '5 days' },
  { id: 'req-sales-2', creatorId: 'user-dept-sales-emp-1', type: RequestType.Leave, status: RequestStatus.Approved, startDate: '2026-09-01T00:00:00Z', endDate: '2026-09-01T00:00:00Z', title: '1 day' },
  // HR team
  { id: 'req-hr-1', creatorId: 'user-dept-hr-emp-1', type: RequestType.Leave, status: RequestStatus.Approved, startDate: '2026-10-01T00:00:00Z', endDate: '2026-10-04T00:00:00Z', title: '4 days' }
];

runCheck('Admin Leave Report aggregates across 100% company with no omissions', () => {
  const admin = employees.find(e => e.userName === 'admin');
  const report = generateLeaveStatsReport(employees, syntheticRequests, 2026, null, admin, departments, members);

  // Total leave days: 3 + 2 + 5 + 1 + 4 = 15 days
  assert.strictEqual(report.totalCompanyLeaveDays, 15);
  assert.strictEqual(report.totalCompanyLeaveRequestsApproved, 5);
  assert.strictEqual(report.totalEmployees, employees.length);
});

runCheck('IT Manager Leave Report aggregates ONLY IT + RD team, strictly 0 Sales or HR days', () => {
  const itManager = employees.find(e => e.userName === 'manager.it');
  const report = generateLeaveStatsReport(employees, syntheticRequests, 2026, null, itManager, departments, members);

  // IT Manager sees 11 staff (IT + RD)
  assert.strictEqual(report.totalEmployees, 11);
  // Total leave days for IT: 3 (manager) + 2 (emp 1) = 5 days. Never includes Sales or HR!
  assert.strictEqual(report.totalCompanyLeaveDays, 5);
  assert.strictEqual(report.totalCompanyLeaveRequestsApproved, 2);
  assert.ok(report.items.every(item => item.departmentId === 'dept-it' || item.departmentId === 'dept-rd' || item.id === itManager.id));
});

runCheck('Regular Employee Leave Report aggregates ONLY self, strictly 0 colleagues', () => {
  const itEmp = employees.find(e => e.userName === 'emp.dept-it.1');
  const report = generateLeaveStatsReport(employees, syntheticRequests, 2026, null, itEmp, departments, members);

  assert.strictEqual(report.totalEmployees, 1);
  assert.strictEqual(report.items[0].id, itEmp.id);
  assert.strictEqual(report.totalCompanyLeaveDays, 2); // itEmp has 2 days
  assert.strictEqual(report.totalCompanyLeaveRequestsApproved, 1);
});

runCheck('IT Manager attempting to generate Leave Report for Sales throws 403 Forbidden', () => {
  const itManager = employees.find(e => e.userName === 'manager.it');
  assert.throws(() => {
    generateLeaveStatsReport(employees, syntheticRequests, 2026, 'dept-sales', itManager, departments, members);
  }, AbpAuthorizationException);
});

// ---------------------------------------------------------------------
// SUMMARY
// ---------------------------------------------------------------------
console.log('\n================================================================');
console.log(`TOTAL CHECKS: ${totalChecks} | PASSED: ${passedChecks} | FAILED: ${failedChecks}`);
console.log('================================================================\n');

if (failedChecks > 0) {
  process.exit(1);
} else {
  console.log('VERDICT: ALL SECURITY BOUNDARIES AND ISOLATION CONTROLS PASSED 100%!');
  process.exit(0);
}

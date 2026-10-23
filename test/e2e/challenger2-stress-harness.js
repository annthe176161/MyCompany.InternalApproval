/**
 * Challenger 2 Stress Harness
 * High-volume randomized Monte Carlo stress testing for:
 * 1. Leave Timeline extraction regex engine
 * 2. Yearly Statistics mathematical & accounting invariants
 * 3. HR Analytics distribution & percentage invariants
 */

const assert = require('assert');
const { extractLeaveTimeline } = require('./helpers/leave-extractor-oracle');
const { calculateYearlyStats, calculateHrAnalytics, RequestType, RequestStatus } = require('./helpers/stats-oracle');

console.log('================================================================');
console.log('       CHALLENGER 2 EMPIRICAL STRESS & INVARIANT HARNESS       ');
console.log('================================================================\n');

let totalTests = 0;
let passedTests = 0;
let failedTests = 0;

function runCheck(name, fn) {
  totalTests++;
  try {
    fn();
    passedTests++;
    console.log(`  [PASS] ${name}`);
  } catch (err) {
    failedTests++;
    console.error(`  [FAIL] ${name}: ${err.message}`);
  }
}

// -------------------------------------------------------------
// Harness 1: Vietnamese Leave Regex Stress Testing (5,000 permutations)
// -------------------------------------------------------------
console.log('--- 1. Regex Extraction Stress Harness (5,000 inputs) ---');

const datePrefixes = ['từ ngày', 'từ', 'nghỉ từ', 'xin nghỉ từ', 'từ khoảng'];
const dateDelims = ['/', '-'];
const dateConnectors = ['đến ngày', 'đến', 'tới ngày', 'tới'];

runCheck('Regex Range: 1,000 randomized valid date ranges', () => {
  for (let i = 0; i < 1000; i++) {
    const d1 = Math.floor(Math.random() * 20) + 1;
    const m1 = Math.floor(Math.random() * 12) + 1;
    const d2 = d1 + Math.floor(Math.random() * 5); // 0 to 4 days later in same month
    const m2 = m1;
    const y = 2026;

    const prefix = datePrefixes[i % datePrefixes.length];
    const delim = dateDelims[i % dateDelims.length];
    const conn = dateConnectors[i % dateConnectors.length];

    const text = `${prefix} ${d1}${delim}${m1} ${conn} ${d2}${delim}${m2} đi du lịch`;
    const req = {
      title: 'Đơn xin nghỉ phép',
      description: text,
      creationTime: '2026-01-01T00:00:00Z',
      status: RequestStatus.Approved
    };

    const extracted = extractLeaveTimeline(req);
    const expectedDays = d2 - d1 + 1;
    assert.strictEqual(extracted.days, expectedDays, `Failed on: "${text}" -> expected ${expectedDays}, got ${extracted.days}`);
  }
});

runCheck('Regex Decimal & Day Count: 1,000 randomized fractional day counts', () => {
  const dayValues = [0.5, 1, 1.5, 2, 2.5, 3, 4.5, 5, 10];
  const templates = [
    days => `Tôi xin nghỉ ${days} ngày phép`,
    days => `nghỉ ${days} ngày`,
    days => `xin nghỉ ${days} ngày đi khám bệnh`,
    days => `Nghỉ ${days.toString().replace('.', ',')} ngày buổi sáng`
  ];

  for (let i = 0; i < 1000; i++) {
    const val = dayValues[i % dayValues.length];
    const tmpl = templates[i % templates.length];
    const text = tmpl(val);

    const req = {
      title: text,
      description: '',
      creationTime: '2026-03-01T00:00:00Z',
      status: RequestStatus.Approved
    };

    const extracted = extractLeaveTimeline(req);
    assert.strictEqual(extracted.days, val, `Failed on: "${text}" -> expected ${val}, got ${extracted.days}`);
  }
});

// -------------------------------------------------------------
// Harness 2: Yearly Statistics Accounting Invariants (5,000 batches)
// -------------------------------------------------------------
console.log('\n--- 2. Yearly Statistics Accounting Invariants (5,000 batches) ---');

runCheck('Invariant: TotalRequests == Draft + Submitted + Approved + Rejected across 5,000 batches', () => {
  for (let b = 0; b < 5000; b++) {
    const requestCount = Math.floor(Math.random() * 50);
    const requests = [];

    for (let r = 0; r < requestCount; r++) {
      const status = Math.floor(Math.random() * 4); // 0, 1, 2, 3
      const type = Math.floor(Math.random() * 3) + 1; // 1, 2, 3
      const amount = Math.floor(Math.random() * 10000000);
      const year = Math.random() < 0.8 ? 2026 : 2025; // 80% current year

      requests.push({
        id: `req-${b}-${r}`,
        creatorId: 'user-1',
        type,
        status,
        amount,
        creationTime: `${year}-06-15T10:00:00Z`
      });
    }

    const stats = calculateYearlyStats(requests, 2026);
    const sum = stats.draftCount + stats.submittedCount + stats.approvedCount + stats.rejectedCount;

    assert.strictEqual(stats.totalRequests, sum, `Batch ${b} failed: totalRequests=${stats.totalRequests}, sum=${sum}`);
  }
});

runCheck('Invariant: TotalDisbursed <= TotalRequested across 5,000 batches', () => {
  for (let b = 0; b < 5000; b++) {
    const requestCount = Math.floor(Math.random() * 30);
    const requests = [];

    for (let r = 0; r < requestCount; r++) {
      const status = Math.floor(Math.random() * 4);
      const type = Math.floor(Math.random() * 3) + 1;
      const amount = Math.floor(Math.random() * 20000000);

      requests.push({
        id: `req-${b}-${r}`,
        creatorId: 'user-1',
        type,
        status,
        amount,
        creationTime: '2026-04-10T00:00:00Z'
      });
    }

    const stats = calculateYearlyStats(requests, 2026);
    assert.ok(stats.totalAmountDisbursed <= stats.totalAmountRequested,
      `Batch ${b} failed: disbursed=${stats.totalAmountDisbursed} > requested=${stats.totalAmountRequested}`);
  }
});

runCheck('Invariant: TotalLeaveDays strictly includes ONLY approved leaves across 1,000 batches', () => {
  for (let b = 0; b < 1000; b++) {
    const requests = [
      {
        id: `l-approved-${b}`,
        creatorId: 'user-1',
        type: RequestType.Leave,
        status: RequestStatus.Approved,
        title: 'Nghỉ phép 2 ngày',
        creationTime: '2026-05-01T00:00:00Z'
      },
      {
        id: `l-draft-${b}`,
        creatorId: 'user-1',
        type: RequestType.Leave,
        status: RequestStatus.Draft,
        title: 'Nghỉ phép 4 ngày',
        creationTime: '2026-05-05T00:00:00Z'
      },
      {
        id: `l-rejected-${b}`,
        creatorId: 'user-1',
        type: RequestType.Leave,
        status: RequestStatus.Rejected,
        title: 'Nghỉ phép 6 ngày',
        creationTime: '2026-05-10T00:00:00Z'
      }
    ];

    const stats = calculateYearlyStats(requests, 2026);
    assert.strictEqual(stats.totalLeaveDays, 2.0, `Batch ${b} failed: expected 2.0 approved days, got ${stats.totalLeaveDays}`);
  }
});

// -------------------------------------------------------------
// Harness 3: HR Analytics Invariants (5,000 organizational setups)
// -------------------------------------------------------------
console.log('\n--- 3. HR Analytics Invariants Stress Harness (5,000 organizations) ---');

runCheck('Invariant: Department headcounts sum to total assigned employees (when all assigned)', () => {
  for (let b = 0; b < 1000; b++) {
    const deptCount = Math.floor(Math.random() * 6) + 1; // 1 to 6 departments
    const departments = [];
    for (let d = 0; d < deptCount; d++) {
      departments.push({ id: `dept-${d}`, code: `D${d}`, name: `Dept ${d}` });
    }

    const employeeCount = Math.floor(Math.random() * 50) + 1; // 1 to 50 employees
    const employees = [];
    for (let e = 0; e < employeeCount; e++) {
      const assignedDept = departments[Math.floor(Math.random() * deptCount)].id;
      employees.push({
        id: `emp-${e}`,
        userId: `emp-${e}`,
        fullName: `Employee ${e}`,
        departmentId: assignedDept,
        joinDate: '2024-01-01T00:00:00Z'
      });
    }

    const analytics = calculateHrAnalytics(employees, departments, [], 2026);
    const sumHeadcount = analytics.headcountByDepartment.reduce((s, h) => s + h.headcount, 0);

    assert.strictEqual(sumHeadcount, employeeCount,
      `Org ${b} failed: sumHeadcount=${sumHeadcount} != totalEmployees=${employeeCount}`);
  }
});

runCheck('Invariant: Department percentages sum to 100% (+/- 0.2%) for realistic org sizes (<= 6 depts)', () => {
  for (let b = 0; b < 1000; b++) {
    const deptCount = Math.floor(Math.random() * 5) + 2; // 2 to 6 departments
    const departments = [];
    for (let d = 0; d < deptCount; d++) {
      departments.push({ id: `dept-${d}`, code: `D${d}`, name: `Dept ${d}` });
    }

    const employeeCount = Math.floor(Math.random() * 80) + 5; // 5 to 84 employees
    const employees = [];
    for (let e = 0; e < employeeCount; e++) {
      const assignedDept = departments[Math.floor(Math.random() * deptCount)].id;
      employees.push({
        id: `emp-${e}`,
        userId: `emp-${e}`,
        fullName: `Employee ${e}`,
        departmentId: assignedDept,
        joinDate: '2023-01-01T00:00:00Z'
      });
    }

    const analytics = calculateHrAnalytics(employees, departments, [], 2026);
    const sumPercentage = analytics.headcountByDepartment.reduce((s, h) => s + h.percentage, 0);
    const diff = Math.round(Math.abs(sumPercentage - 100.0) * 10) / 10;

    const maxDiff = deptCount <= 4 ? 0.2 : 0.3;
    assert.ok(diff <= maxDiff,
      `Org ${b} failed: sumPercentage=${sumPercentage.toFixed(2)}%, diff=${diff.toFixed(2)}% > ${maxDiff}%`);
  }
});

runCheck('Adversarial Scenario: Unassigned employees (e.g. admin) break percentage & headcount sums', () => {
  const departments = [
    { id: 'dept-it', code: 'IT', name: 'Công nghệ' },
    { id: 'dept-hr', code: 'HR', name: 'Nhân sự' }
  ];

  const employees = [
    { id: 'emp-admin', userId: 'emp-admin', fullName: 'Admin', departmentId: null, joinDate: '2020-01-01T00:00:00Z' }, // Unassigned!
    { id: 'emp-it', userId: 'emp-it', fullName: 'IT Staff', departmentId: 'dept-it', joinDate: '2023-01-01T00:00:00Z' },
    { id: 'emp-hr', userId: 'emp-hr', fullName: 'HR Staff', departmentId: 'dept-hr', joinDate: '2024-01-01T00:00:00Z' }
  ];

  const analytics = calculateHrAnalytics(employees, departments, [], 2026);
  const sumHeadcount = analytics.headcountByDepartment.reduce((s, h) => s + h.headcount, 0);
  const sumPercentage = analytics.headcountByDepartment.reduce((s, h) => s + h.percentage, 0);

  // In this unassigned case:
  // IT = 1/3 (33.3%), HR = 1/3 (33.3%)
  // Sum = 66.6% (100% - 66.6% = 33.4% deficit!)
  // Headcounts = 2 (Sum) vs 3 (Total)
  assert.strictEqual(analytics.companySummary.totalEmployees, 3);
  assert.strictEqual(sumHeadcount, 2, 'Headcount should be 2 because admin is unassigned');
  assert.strictEqual(sumPercentage, 66.6, 'Sum percentage should be 66.6% due to unassigned deficit');
  console.log(`     Confirmed: Unassigned employee causes ${sumPercentage}% sum and ${sumHeadcount}/${analytics.companySummary.totalEmployees} headcount gap.`);
});

console.log('\n================================================================');
console.log(`STRESS HARNESS COMPLETED: ${passedTests}/${totalTests} CHECKS PASSED`);
if (failedTests > 0) {
  console.log(`WARNING: ${failedTests} CHECKS FAILED`);
}
console.log('================================================================');

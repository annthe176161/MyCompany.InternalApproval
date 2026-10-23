const { describe, it } = require('node:test');
const assert = require('node:assert/strict');
const { calculateTenure, resolveEffectiveJoinDate } = require('../helpers/tenure-oracle');
const { extractLeaveTimeline } = require('../helpers/leave-extractor-oracle');
const { calculateYearlyStats, calculateHrAnalytics, RequestType, RequestStatus } = require('../helpers/stats-oracle');

describe('Tier 4: Real-World Scenarios (End-to-End Lifecycles)', () => {

  const fixedNow = new Date('2026-10-22T00:00:00Z');

  it('TC-S01: Complete Employee Onboarding & Seniority Milestone Progression Lifecycle', () => {
    // 1. Initial Onboarding: Account created today with no initial JoinDate
    const newEmployee = {
      id: 'emp-newbie',
      userId: 'emp-newbie',
      userName: 'quang.pham',
      fullName: 'Phạm Nhật Quang',
      email: 'quang.pham@company.com',
      departmentId: 'dept-it',
      position: 'Kỹ Sư Phần Mềm Tập Sự',
      isManager: false,
      joinDate: null,
      creationTime: '2026-10-22T08:00:00Z'
    };

    // Verify initial fallback tenure
    const effectiveInitial = resolveEffectiveJoinDate(newEmployee.joinDate, newEmployee.creationTime);
    const tenureInitial = calculateTenure(effectiveInitial, fixedNow);
    assert.strictEqual(tenureInitial.formatted, '< 1 tháng');
    assert.strictEqual(tenureInitial.totalMonths, 0);

    // 2. HR Admin reviews employment contract: actual start was 1 year 6 months ago (2025-04-22)
    const officialJoinDate = '2025-04-22T00:00:00Z';
    newEmployee.joinDate = officialJoinDate;

    // 3. System recalculates tenure
    const effectiveUpdated = resolveEffectiveJoinDate(newEmployee.joinDate, newEmployee.creationTime);
    const tenureUpdated = calculateTenure(effectiveUpdated, fixedNow);

    // 4. Assert Seniority Milestone
    assert.strictEqual(tenureUpdated.years, 1);
    assert.strictEqual(tenureUpdated.months, 6);
    assert.strictEqual(tenureUpdated.totalMonths, 18);
    assert.strictEqual(tenureUpdated.formatted, '1 năm 6 tháng');
  });

  it('TC-S02: Employee Annual Vacation Request & Manager Approval Lifecycle', () => {
    const employeeId = 'emp-vacationer';
    const managerId = 'mgr-approver';

    // 1. Employee creates a leave request for 4 days
    const leaveRequest = {
      id: 'leave-vacation-2026',
      creatorId: employeeId,
      title: 'Nghỉ phép thường niên 4 ngày cùng gia đình',
      description: 'Tôi xin phép nghỉ 4 ngày từ 17/11/2026 đến 20/11/2026.',
      type: RequestType.Leave,
      status: RequestStatus.Draft,
      amount: null,
      creationTime: '2026-10-22T09:00:00Z'
    };

    // 2. Employee submits request
    leaveRequest.status = RequestStatus.Submitted;

    // Verify interim state in timeline
    let timelineView = extractLeaveTimeline(leaveRequest);
    assert.strictEqual(timelineView.status, RequestStatus.Submitted);
    assert.strictEqual(timelineView.days, 4);

    // Verify interim yearly stats (should not count unapproved leave days)
    let statsInterim = calculateYearlyStats([leaveRequest], 2026);
    assert.strictEqual(statsInterim.submittedCount, 1);
    assert.strictEqual(statsInterim.approvedCount, 0);
    assert.strictEqual(statsInterim.totalLeaveDays, 0);

    // 3. Manager reviews Drawer and approves request
    leaveRequest.status = RequestStatus.Approved;
    leaveRequest.approverId = managerId;
    leaveRequest.approverName = 'Trưởng Phòng Nguyễn Văn Quản';
    leaveRequest.approvalTime = '2026-10-22T14:30:00Z';

    // 4. Final verification: Leave timeline and yearly stats
    let timelineFinal = extractLeaveTimeline(leaveRequest);
    assert.strictEqual(timelineFinal.status, RequestStatus.Approved);
    assert.strictEqual(timelineFinal.approverName, 'Trưởng Phòng Nguyễn Văn Quản');

    let statsFinal = calculateYearlyStats([leaveRequest], 2026);
    assert.strictEqual(statsFinal.submittedCount, 0);
    assert.strictEqual(statsFinal.approvedCount, 1);
    assert.strictEqual(statsFinal.totalLeaveDays, 4.0);
  });

  it('TC-S03: Multi-Category Annual Request Cycle (Leave + Equipment + AdvanceMoney)', () => {
    const employeeId = 'emp-active';
    const requests = [
      // 1. Leave: 2 days, approved
      {
        id: 'req-cycle-1',
        creatorId: employeeId,
        title: 'Nghỉ phép 2 ngày việc gia đình',
        description: 'Nghỉ 2 ngày từ 05/03/2026 đến 06/03/2026',
        type: RequestType.Leave,
        status: RequestStatus.Approved,
        amount: null,
        creationTime: '2026-03-01T08:00:00Z'
      },
      // 2. Equipment: 12,000,000 VNĐ, approved
      {
        id: 'req-cycle-2',
        creatorId: employeeId,
        title: 'Mua Laptop Workstation mới',
        description: 'Laptop phục vụ đồ họa kỹ thuật',
        type: RequestType.Equipment,
        status: RequestStatus.Approved,
        amount: 12000000,
        creationTime: '2026-05-10T10:00:00Z'
      },
      // 3. AdvanceMoney: 20,000,000 VNĐ, rejected
      {
        id: 'req-cycle-3',
        creatorId: employeeId,
        title: 'Tạm ứng kinh phí hội chợ quốc tế',
        description: 'Tạm ứng chi phí gian hàng',
        type: RequestType.AdvanceMoney,
        status: RequestStatus.Rejected,
        amount: 20000000,
        rejectionReason: 'Vượt ngân sách cho phép quý 2',
        creationTime: '2026-06-01T11:00:00Z'
      }
    ];

    const stats = calculateYearlyStats(requests, 2026);

    // Count checks
    assert.strictEqual(stats.totalRequests, 3);
    assert.strictEqual(stats.approvedCount, 2);
    assert.strictEqual(stats.rejectedCount, 1);
    assert.strictEqual(stats.submittedCount, 0);
    assert.strictEqual(stats.draftCount, 0);

    // Financial calculations
    assert.strictEqual(stats.totalAmountRequested, 32000000); // 12M + 20M
    assert.strictEqual(stats.totalAmountDisbursed, 12000000); // Only approved 12M
    assert.strictEqual(stats.totalLeaveDays, 2.0); // Only approved leave days

    // Category breakdown
    const eqStat = stats.breakdownByType.find(b => b.type === RequestType.Equipment);
    assert.strictEqual(eqStat.count, 1);
    assert.strictEqual(eqStat.totalAmount, 12000000);

    const advStat = stats.breakdownByType.find(b => b.type === RequestType.AdvanceMoney);
    assert.strictEqual(advStat.count, 1);
    assert.strictEqual(advStat.totalAmount, 20000000);

    const leaveStat = stats.breakdownByType.find(b => b.type === RequestType.Leave);
    assert.strictEqual(leaveStat.count, 1);
    assert.strictEqual(leaveStat.totalAmount, 0);
  });

  it('TC-S04: Executive HR Analytics & Headcount Audit Across Enterprise', () => {
    const departments = [
      { id: 'dept-eng', code: 'ENG', name: 'Khối Kỹ Thuật' },
      { id: 'dept-sales', code: 'SALES', name: 'Khối Kinh Doanh' },
      { id: 'dept-ops', code: 'OPS', name: 'Khối Vận Hành' }
    ];

    const employees = [
      { id: 'e1', departmentId: 'dept-eng', joinDate: '2023-10-22T00:00:00Z', creationTime: '2023-10-22T00:00:00Z' }, // 3y
      { id: 'e2', departmentId: 'dept-eng', joinDate: '2024-10-22T00:00:00Z', creationTime: '2024-10-22T00:00:00Z' }, // 2y
      { id: 'e3', departmentId: 'dept-sales', joinDate: '2025-10-22T00:00:00Z', creationTime: '2025-10-22T00:00:00Z' }, // 1y
      { id: 'e4', departmentId: 'dept-sales', joinDate: '2026-04-22T00:00:00Z', creationTime: '2026-04-22T00:00:00Z' }, // 6m
      { id: 'e5', departmentId: 'dept-ops', joinDate: '2026-10-01T00:00:00Z', creationTime: '2026-10-01T00:00:00Z' }   // < 1m
    ];

    const requests = [
      { creatorId: 'e1', departmentId: 'dept-eng', creationTime: '2026-01-10T00:00:00Z', type: RequestType.Equipment, status: RequestStatus.Approved, amount: 10000000 },
      { creatorId: 'e1', departmentId: 'dept-eng', creationTime: '2026-03-15T00:00:00Z', type: RequestType.Leave, status: RequestStatus.Approved },
      { creatorId: 'e3', departmentId: 'dept-sales', creationTime: '2026-05-20T00:00:00Z', type: RequestType.AdvanceMoney, status: RequestStatus.Approved, amount: 5000000 }
    ];

    const analytics = calculateHrAnalytics(employees, departments, requests, 2026, fixedNow);

    // 1. Company Summary
    assert.strictEqual(analytics.companySummary.totalEmployees, 5);
    assert.strictEqual(analytics.companySummary.totalDepartments, 3);
    assert.strictEqual(analytics.companySummary.totalRequestsThisYear, 3);
    assert.strictEqual(analytics.companySummary.totalDisbursedAmountThisYear, 15000000);

    // 2. Department Headcounts sum to 5 and 100%
    const totalHeadcount = analytics.headcountByDepartment.reduce((sum, d) => sum + d.headcount, 0);
    assert.strictEqual(totalHeadcount, 5);
    const totalPercentage = analytics.headcountByDepartment.reduce((sum, d) => sum + d.percentage, 0);
    assert.ok(Math.abs(totalPercentage - 100.0) < 0.2);

    // 3. Department Tenures: ENG average = (36 + 24) / 2 = 30 months = 2 years 6 months
    const engTenure = analytics.tenureByDepartment.find(d => d.departmentCode === 'ENG');
    assert.strictEqual(engTenure.averageTenureMonths, 30);
    assert.strictEqual(engTenure.averageTenureFormatted, '2 năm 6 tháng');

    // 4. Request frequency: ENG has 2 requests / 2 employees = 1.0; SALES has 1 / 2 = 0.5
    const engFreq = analytics.requestFrequencyByDepartment.find(d => d.departmentCode === 'ENG');
    assert.strictEqual(engFreq.totalRequests, 2);
    assert.strictEqual(engFreq.requestsPerEmployee, 1.0);

    // 5. Top Requesters: e1 is #1 with 2 requests
    assert.strictEqual(analytics.topRequesters[0].userId, 'e1');
    assert.strictEqual(analytics.topRequesters[0].requestCount, 2);
  });

});

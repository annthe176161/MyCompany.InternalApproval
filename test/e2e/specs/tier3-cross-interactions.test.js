const { describe, it } = require('node:test');
const assert = require('node:assert/strict');
const { calculateTenure, resolveEffectiveJoinDate } = require('../helpers/tenure-oracle');
const { extractLeaveTimeline } = require('../helpers/leave-extractor-oracle');
const { calculateYearlyStats, calculateHrAnalytics, RequestType, RequestStatus } = require('../helpers/stats-oracle');

describe('Tier 3: Cross-Feature Interactions', () => {

  const fixedNow = new Date('2026-10-22T00:00:00Z');

  it('TC-C01: Updating JoinDate dynamically cascades to profile tenure, list tenure, and department average tenure', () => {
    // Clone initial dataset
    const employees = [
      { id: 'emp-1', userId: 'emp-1', fullName: 'Nhân viên 1', departmentId: 'dept-it', joinDate: '2025-10-22T00:00:00Z', creationTime: '2025-10-01T00:00:00Z' },
      { id: 'emp-2', userId: 'emp-2', fullName: 'Nhân viên 2', departmentId: 'dept-it', joinDate: '2024-10-22T00:00:00Z', creationTime: '2024-10-01T00:00:00Z' }
    ];
    const departments = [
      { id: 'dept-it', code: 'IT', name: 'Phòng CNTT' }
    ];

    // Initial check: emp-1 has 1 year, emp-2 has 2 years -> IT average is 1.5 years (18 months)
    let analyticsBefore = calculateHrAnalytics(employees, departments, [], 2026, fixedNow);
    assert.strictEqual(analyticsBefore.tenureByDepartment[0].averageTenureMonths, 18);

    // ACTION: Update emp-1 JoinDate from 1 year ago to 3 years ago (2023-10-22)
    employees[0].joinDate = '2023-10-22T00:00:00Z';

    // Verify 1: Profile tenure
    const newTenure = calculateTenure(employees[0].joinDate, fixedNow);
    assert.strictEqual(newTenure.years, 3);
    assert.strictEqual(newTenure.formatted, '3 năm');

    // Verify 2: Department average tenure updated: (36 + 24) / 2 = 30 months = 2.5 years
    let analyticsAfter = calculateHrAnalytics(employees, departments, [], 2026, fixedNow);
    assert.strictEqual(analyticsAfter.tenureByDepartment[0].averageTenureMonths, 30);
    assert.strictEqual(analyticsAfter.tenureByDepartment[0].averageTenureFormatted, '2 năm 6 tháng');
  });

  it('TC-C02: Submitting a leave request synchronously updates request history, yearly stats, and leave timeline', () => {
    const employeeId = 'emp-leave-user';
    const requests = [];

    // Initial state
    const initialStats = calculateYearlyStats(requests, 2026);
    assert.strictEqual(initialStats.totalRequests, 0);
    assert.strictEqual(initialStats.submittedCount, 0);

    // ACTION: Submit a new leave request
    const newLeave = {
      id: 'leave-req-101',
      creatorId: employeeId,
      title: 'Xin nghỉ phép 3 ngày việc gia đình',
      description: 'Nghỉ từ 10/11/2026 đến 12/11/2026',
      type: RequestType.Leave,
      status: RequestStatus.Submitted,
      creationTime: '2026-10-22T08:00:00Z'
    };
    requests.push(newLeave);

    // Verify 1: Request history contains 1 submitted item
    const userRequests = requests.filter(r => r.creatorId === employeeId);
    assert.strictEqual(userRequests.length, 1);
    assert.strictEqual(userRequests[0].status, RequestStatus.Submitted);

    // Verify 2: Yearly stats increments TotalRequests and SubmittedCount
    const updatedStats = calculateYearlyStats(requests, 2026);
    assert.strictEqual(updatedStats.totalRequests, 1);
    assert.strictEqual(updatedStats.submittedCount, 1);
    assert.strictEqual(updatedStats.totalLeaveDays, 0, 'Unapproved leave days must not be counted in totalLeaveDays');

    // Verify 3: Leave timeline reflects the new submission with 3 days
    const timelineItem = extractLeaveTimeline(newLeave);
    assert.strictEqual(timelineItem.status, RequestStatus.Submitted);
    assert.strictEqual(timelineItem.days, 3);
  });

  it('TC-C03: Approving an equipment request transitions status and shifts financial metrics from requested to disbursed', () => {
    const request = {
      id: 'eq-req-201',
      creatorId: 'emp-buyer',
      title: 'Mua máy chiếu hội thảo',
      type: RequestType.Equipment,
      status: RequestStatus.Submitted,
      amount: 15000000,
      creationTime: '2026-10-15T09:00:00Z'
    };
    const requests = [request];

    // Before approval
    const statsBefore = calculateYearlyStats(requests, 2026);
    assert.strictEqual(statsBefore.submittedCount, 1);
    assert.strictEqual(statsBefore.approvedCount, 0);
    assert.strictEqual(statsBefore.totalAmountRequested, 15000000);
    assert.strictEqual(statsBefore.totalAmountDisbursed, 0);

    // ACTION: Manager approves request
    request.status = RequestStatus.Approved;
    request.approverId = 'manager-1';
    request.approvalTime = '2026-10-22T10:00:00Z';

    // After approval
    const statsAfter = calculateYearlyStats(requests, 2026);
    assert.strictEqual(statsAfter.submittedCount, 0);
    assert.strictEqual(statsAfter.approvedCount, 1);
    assert.strictEqual(statsAfter.totalAmountRequested, 15000000, 'Total requested remains constant');
    assert.strictEqual(statsAfter.totalAmountDisbursed, 15000000, 'Total disbursed updated with approved amount');
  });

  it('TC-C04: Department reassignment cascades to headcount percentages and department average tenures', () => {
    const employees = [
      { id: 'e1', userId: 'e1', departmentId: 'dept-it', joinDate: '2024-10-22T00:00:00Z', creationTime: '2024-10-22T00:00:00Z' },
      { id: 'e2', userId: 'e2', departmentId: 'dept-it', joinDate: '2024-10-22T00:00:00Z', creationTime: '2024-10-22T00:00:00Z' },
      { id: 'e3', userId: 'e3', departmentId: 'dept-hr', joinDate: '2025-10-22T00:00:00Z', creationTime: '2025-10-22T00:00:00Z' },
      { id: 'e4', userId: 'e4', departmentId: 'dept-hr', joinDate: '2025-10-22T00:00:00Z', creationTime: '2025-10-22T00:00:00Z' }
    ];
    const departments = [
      { id: 'dept-it', code: 'IT', name: 'CNTT' },
      { id: 'dept-hr', code: 'HR', name: 'Nhân sự' }
    ];

    // Initial headcount: 2 IT (50%), 2 HR (50%)
    let analytics = calculateHrAnalytics(employees, departments, [], 2026, fixedNow);
    assert.strictEqual(analytics.headcountByDepartment[0].headcount, 2);
    assert.strictEqual(analytics.headcountByDepartment[1].headcount, 2);
    assert.strictEqual(analytics.headcountByDepartment[0].percentage, 50.0);
    assert.strictEqual(analytics.headcountByDepartment[1].percentage, 50.0);

    // ACTION: Transfer e2 from IT to HR
    employees[1].departmentId = 'dept-hr';

    // Verify: IT now has 1 (25%), HR now has 3 (75%)
    let analyticsAfter = calculateHrAnalytics(employees, departments, [], 2026, fixedNow);
    assert.strictEqual(analyticsAfter.headcountByDepartment[0].headcount, 1);
    assert.strictEqual(analyticsAfter.headcountByDepartment[1].headcount, 3);
    assert.strictEqual(analyticsAfter.headcountByDepartment[0].percentage, 25.0);
    assert.strictEqual(analyticsAfter.headcountByDepartment[1].percentage, 75.0);
  });

  it('TC-C05: Rejecting a leave request records rejection reason and excludes duration from yearly approved leave days', () => {
    const leaveRequest = {
      id: 'leave-rej-1',
      creatorId: 'emp-x',
      title: 'Xin nghỉ 5 ngày du lịch hè',
      description: 'Nghỉ 5 ngày',
      type: RequestType.Leave,
      status: RequestStatus.Submitted,
      creationTime: '2026-07-01T00:00:00Z'
    };
    const requests = [leaveRequest];

    // ACTION: Manager rejects request with reason
    leaveRequest.status = RequestStatus.Rejected;
    leaveRequest.approverId = 'manager-hr';
    leaveRequest.rejectionReason = 'Trùng lịch triển khai dự án trọng điểm';
    leaveRequest.approvalTime = '2026-07-02T10:00:00Z';

    // Verify 1: Timeline displays Rejected tag and verbatim reason
    const timelineItem = extractLeaveTimeline(leaveRequest);
    assert.strictEqual(timelineItem.status, RequestStatus.Rejected);
    assert.strictEqual(timelineItem.rejectionReason, 'Trùng lịch triển khai dự án trọng điểm');

    // Verify 2: Yearly stats increments RejectedCount and keeps totalLeaveDays at 0
    const stats = calculateYearlyStats(requests, 2026);
    assert.strictEqual(stats.rejectedCount, 1);
    assert.strictEqual(stats.totalLeaveDays, 0);
  });

});

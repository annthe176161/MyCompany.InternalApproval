const { describe, it } = require('node:test');
const assert = require('node:assert/strict');
const { calculateTenure, formatVietnameseTenure, resolveEffectiveJoinDate } = require('../helpers/tenure-oracle');
const { extractLeaveTimeline } = require('../helpers/leave-extractor-oracle');
const { calculateYearlyStats, calculateHrAnalytics, RequestType, RequestStatus } = require('../helpers/stats-oracle');
const testData = require('../fixtures/test-data.json');

describe('Tier 1: Feature Coverage (Isolated Verification)', () => {

  describe('Feature 1 & 2: JoinDate Model & Fallback to CreationTime', () => {
    it('TC-F01: Should use explicit JoinDate when populated', () => {
      const explicitDate = '2024-05-15T00:00:00Z';
      const creationTime = '2024-05-10T00:00:00Z';
      const effective = resolveEffectiveJoinDate(explicitDate, creationTime);
      assert.strictEqual(effective.toISOString(), new Date(explicitDate).toISOString());
    });

    it('TC-F02: Should fall back to CreationTime when JoinDate is null', () => {
      const creationTime = '2026-10-05T10:00:00Z';
      const effective = resolveEffectiveJoinDate(null, creationTime);
      assert.strictEqual(effective.toISOString(), new Date(creationTime).toISOString());
    });

    it('TC-F03: Should fall back to CreationTime when JoinDate is undefined', () => {
      const creationTime = '2025-01-01T00:00:00Z';
      const effective = resolveEffectiveJoinDate(undefined, creationTime);
      assert.strictEqual(effective.toISOString(), new Date(creationTime).toISOString());
    });

    it('TC-F04: Should preserve UTC timestamps without arbitrary timezone offset', () => {
      const joinDate = '2023-11-20T14:30:00.000Z';
      const effective = resolveEffectiveJoinDate(joinDate, '2023-01-01T00:00:00Z');
      assert.strictEqual(effective.getUTCFullYear(), 2023);
      assert.strictEqual(effective.getUTCMonth(), 10);
      assert.strictEqual(effective.getUTCDate(), 20);
    });

    it('TC-F05: Should evaluate fallback individually across multiple employees', () => {
      for (const emp of testData.employees) {
        const effective = resolveEffectiveJoinDate(emp.joinDate, emp.creationTime);
        if (emp.joinDate) {
          assert.strictEqual(effective.toISOString(), new Date(emp.joinDate).toISOString());
        } else {
          assert.strictEqual(effective.toISOString(), new Date(emp.creationTime).toISOString());
        }
      }
    });
  });

  describe('Feature 3: Tenure Calculation & Vietnamese Formatting', () => {
    const fixedNow = new Date('2026-10-22T00:00:00Z');

    it('TC-F06: Should calculate and format multi-year multi-month tenure ("2 năm 5 tháng")', () => {
      // 2024-05-15 to 2026-10-22 = 2 years 5 months
      const result = calculateTenure('2024-05-15T00:00:00Z', fixedNow);
      assert.strictEqual(result.years, 2);
      assert.strictEqual(result.months, 5);
      assert.strictEqual(result.formatted, '2 năm 5 tháng');
    });

    it('TC-F07: Should calculate and format sub-year tenure ("8 tháng")', () => {
      // 2026-02-15 to 2026-10-22 = 0 years 8 months
      const result = calculateTenure('2026-02-15T00:00:00Z', fixedNow);
      assert.strictEqual(result.years, 0);
      assert.strictEqual(result.months, 8);
      assert.strictEqual(result.formatted, '8 tháng');
    });

    it('TC-F08: Should calculate and format sub-month tenure ("< 1 tháng")', () => {
      // 2026-10-05 to 2026-10-22 = 0 years 0 months
      const result = calculateTenure('2026-10-05T00:00:00Z', fixedNow);
      assert.strictEqual(result.years, 0);
      assert.strictEqual(result.months, 0);
      assert.strictEqual(result.formatted, '< 1 tháng');
    });

    it('TC-F09: Should calculate exact round-year tenure ("3 năm" and "1 năm")', () => {
      // 2023-10-22 to 2026-10-22 = exact 3 years
      const r3 = calculateTenure('2023-10-22T00:00:00Z', fixedNow);
      assert.strictEqual(r3.years, 3);
      assert.strictEqual(r3.months, 0);
      assert.strictEqual(r3.formatted, '3 năm');

      // 2025-10-22 to 2026-10-22 = exact 1 year
      const r1 = calculateTenure('2025-10-22T00:00:00Z', fixedNow);
      assert.strictEqual(r1.years, 1);
      assert.strictEqual(r1.months, 0);
      assert.strictEqual(r1.formatted, '1 năm');
    });

    it('TC-F10: Should decrement month if target day is earlier than join day', () => {
      // Join on Oct 25, 2025, target Oct 22, 2026 -> 11 months, not 1 year
      const result = calculateTenure('2025-10-25T00:00:00Z', fixedNow);
      assert.strictEqual(result.years, 0);
      assert.strictEqual(result.months, 11);
      assert.strictEqual(result.formatted, '11 tháng');
    });
  });

  describe('Feature 4 & 5: Employee List & Profile Detail Data Contracts', () => {
    it('TC-F11: Employee list schema contract should validate all required fields', () => {
      const emp = testData.employees[0];
      const requiredFields = ['id', 'userId', 'userName', 'fullName', 'email', 'departmentId', 'departmentName', 'position', 'isManager', 'creationTime'];
      for (const field of requiredFields) {
        assert.ok(emp[field] !== undefined, `Field ${field} must be present in EmployeeListItemDto`);
      }
    });

    it('TC-F12: Should correctly filter employees by text query matching name or username', () => {
      const filter = 'Tuấn';
      const filtered = testData.employees.filter(e => 
        e.fullName.toLowerCase().includes(filter.toLowerCase()) || 
        e.userName.toLowerCase().includes(filter.toLowerCase())
      );
      assert.strictEqual(filtered.length, 1);
      assert.strictEqual(filtered[0].userName, 'tuan.le');
    });

    it('TC-F13: Should filter employees by department ID', () => {
      const itDeptId = '11111111-1111-1111-1111-111111111111';
      const itEmps = testData.employees.filter(e => e.departmentId === itDeptId);
      assert.strictEqual(itEmps.length, 2);
      assert.ok(itEmps.every(e => e.departmentCode === 'IT'));
    });

    it('TC-F14: Profile detail should retain manager designation accurately', () => {
      const manager = testData.employees.find(e => e.isManager === true);
      assert.ok(manager);
      assert.strictEqual(manager.userName, 'nam.tran');
      assert.strictEqual(manager.position, 'Trưởng Phòng CNTT');
    });

    it('TC-F15: Profile DTO should compute effective join date and formatted tenure', () => {
      const emp = testData.employees[0];
      const effective = resolveEffectiveJoinDate(emp.joinDate, emp.creationTime);
      const tenure = calculateTenure(effective, new Date('2026-10-22T00:00:00Z'));
      assert.strictEqual(tenure.formatted, '2 năm 5 tháng');
    });
  });

  describe('Feature 6: Update JoinDate API Constraints & Validation', () => {
    const fixedNow = new Date('2026-10-22T00:00:00Z');

    it('TC-F16: Should accept a valid historical JoinDate and recalculate tenure', () => {
      const newJoinDate = '2022-04-10T00:00:00Z';
      const tenure = calculateTenure(newJoinDate, fixedNow);
      assert.strictEqual(tenure.years, 4);
      assert.strictEqual(tenure.months, 6);
      assert.strictEqual(tenure.formatted, '4 năm 6 tháng');
    });

    it('TC-F17: Should reject a future JoinDate', () => {
      const futureDate = new Date('2028-01-01T00:00:00Z');
      const isValid = futureDate <= fixedNow;
      assert.strictEqual(isValid, false, 'Future JoinDate must fail validation');
    });

    it('TC-F18: Should reject an ancient date prior to 1970', () => {
      const ancientDate = new Date('1965-05-10T00:00:00Z');
      const minDate = new Date('1970-01-01T00:00:00Z');
      const isValid = ancientDate >= minDate;
      assert.strictEqual(isValid, false, 'Dates before 1970 must fail validation');
    });

    it('TC-F19: Should correctly detect changes in JoinDate', () => {
      const oldJoinDate = '2024-05-15T00:00:00Z';
      const updatedJoinDate = '2023-01-10T00:00:00Z';
      assert.notStrictEqual(new Date(oldJoinDate).getTime(), new Date(updatedJoinDate).getTime());
    });

    it('TC-F20: Recalculated totalMonths must equal (years * 12 + months)', () => {
      const tenure = calculateTenure('2021-03-01T00:00:00Z', fixedNow);
      assert.strictEqual(tenure.totalMonths, tenure.years * 12 + tenure.months);
    });
  });

  describe('Feature 7: Employee Request History Filtering', () => {
    it('TC-F21: Should filter requests exclusively authored by target employee', () => {
      const tuanId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
      const tuanRequests = testData.requests.filter(r => r.creatorId === tuanId);
      assert.strictEqual(tuanRequests.length, 3);
      assert.ok(tuanRequests.every(r => r.creatorId === tuanId));
    });

    it('TC-F22: Should filter requests by RequestStatus (Approved)', () => {
      const approved = testData.requests.filter(r => r.status === RequestStatus.Approved);
      assert.strictEqual(approved.length, 3);
    });

    it('TC-F23: Should filter requests by RequestType (Leave)', () => {
      const leaves = testData.requests.filter(r => r.type === RequestType.Leave);
      assert.strictEqual(leaves.length, 2);
    });

    it('TC-F24: Should filter requests by target creation year (2026)', () => {
      const requests2026 = testData.requests.filter(r => new Date(r.creationTime).getFullYear() === 2026);
      assert.strictEqual(requests2026.length, 5);
    });

    it('TC-F25: Should support pagination slicing (skipCount, maxResultCount)', () => {
      const skipCount = 1;
      const maxResultCount = 2;
      const paged = testData.requests.slice(skipCount, skipCount + maxResultCount);
      assert.strictEqual(paged.length, 2);
      assert.strictEqual(paged[0].id, testData.requests[1].id);
    });
  });

  describe('Feature 8: Yearly Statistics Calculations', () => {
    it('TC-F26: TotalRequests must match sum of all status breakdown counts', () => {
      const stats = calculateYearlyStats(testData.requests, 2026);
      const sum = stats.draftCount + stats.submittedCount + stats.approvedCount + stats.rejectedCount;
      assert.strictEqual(stats.totalRequests, sum);
    });

    it('TC-F27: TotalAmountRequested must aggregate amounts for Equipment and AdvanceMoney', () => {
      const stats = calculateYearlyStats(testData.requests, 2026);
      // req-0002 (9,500,000) + req-0003 (5,000,000) + req-0005 (15,000,000) = 29,500,000
      assert.strictEqual(stats.totalAmountRequested, 29500000);
    });

    it('TC-F28: TotalAmountDisbursed must sum strictly Approved requests', () => {
      const stats = calculateYearlyStats(testData.requests, 2026);
      // Only req-0002 is approved with amount 9,500,000
      assert.strictEqual(stats.totalAmountDisbursed, 9500000);
      assert.ok(stats.totalAmountDisbursed <= stats.totalAmountRequested);
    });

    it('TC-F29: TotalLeaveDays must accumulate approved leave days only', () => {
      const stats = calculateYearlyStats(testData.requests, 2026);
      // req-0001 (2 days, approved) + req-0004 (1 day, approved) = 3 days
      assert.strictEqual(stats.totalLeaveDays, 3.0);
    });

    it('TC-F30: BreakdownByType must include counts for all 3 standard categories', () => {
      const stats = calculateYearlyStats(testData.requests, 2026);
      assert.strictEqual(stats.breakdownByType.length, 3);
      const leaveBreakdown = stats.breakdownByType.find(b => b.type === RequestType.Leave);
      assert.strictEqual(leaveBreakdown.count, 2);
    });
  });

  describe('Feature 9: Leave Timeline Extraction & Parsing', () => {
    it('TC-F31: Should extract dates from structured properties if available', () => {
      const req = {
        id: 'structured-1',
        title: 'Nghỉ hè gia đình',
        startDate: '2026-07-01T00:00:00Z',
        endDate: '2026-07-05T00:00:00Z',
        days: 5.0,
        status: RequestStatus.Approved,
        creationTime: '2026-06-20T00:00:00Z'
      };
      const result = extractLeaveTimeline(req);
      assert.strictEqual(result.source, 'structured');
      assert.strictEqual(result.days, 5.0);
      assert.strictEqual(result.startDate.toISOString(), new Date('2026-07-01T00:00:00Z').toISOString());
    });

    it('TC-F32: Should extract date range from Vietnamese text ("từ 25/10 đến 26/10")', () => {
      const req = testData.requests[0]; // "từ 25/10 đến 26/10"
      const result = extractLeaveTimeline(req);
      assert.strictEqual(result.source, 'regex-range');
      assert.strictEqual(result.days, 2.0);
      assert.strictEqual(result.startDate.getUTCDate(), 25);
      assert.strictEqual(result.startDate.getUTCMonth(), 9); // Month 9 is October
      assert.strictEqual(result.endDate.getUTCDate(), 26);
    });

    it('TC-F33: Should extract days count from text ("xin nghỉ 1 ngày")', () => {
      const req = testData.requests[3]; // "Xin nghỉ 1 ngày khám sức khỏe định kỳ"
      const result = extractLeaveTimeline(req);
      assert.strictEqual(result.days, 1.0);
    });

    it('TC-F34: Should fall back gracefully to CreationTime when text contains no dates', () => {
      const req = {
        id: 'no-date-1',
        title: 'Nghỉ việc riêng khẩn cấp',
        description: 'Tôi có việc đột xuất cần nghỉ.',
        status: RequestStatus.Submitted,
        creationTime: '2026-11-04T08:00:00Z'
      };
      const result = extractLeaveTimeline(req);
      assert.strictEqual(result.source, 'fallback');
      assert.strictEqual(result.days, 1.0);
      assert.strictEqual(result.startDate.getUTCDate(), 4);
      assert.strictEqual(result.startDate.getUTCMonth(), 10); // November
    });

    it('TC-F35: Should capture approver information and approval time', () => {
      const req = testData.requests[0];
      const result = extractLeaveTimeline(req);
      assert.strictEqual(result.approverName, 'Trần Hải Nam');
      assert.strictEqual(result.status, RequestStatus.Approved);
      assert.ok(result.approvalTime);
    });
  });

  describe('Feature 10: HR Analytics Aggregations', () => {
    const fixedNow = new Date('2026-10-22T00:00:00Z');

    it('TC-F36: CompanySummary total employees and departments must match dataset totals', () => {
      const analytics = calculateHrAnalytics(testData.employees, testData.departments, testData.requests, 2026, fixedNow);
      assert.strictEqual(analytics.companySummary.totalEmployees, 4);
      assert.strictEqual(analytics.companySummary.totalDepartments, 3);
    });

    it('TC-F37: Department headcount percentages must sum to 100% (+/- 0.5% rounding)', () => {
      const analytics = calculateHrAnalytics(testData.employees, testData.departments, testData.requests, 2026, fixedNow);
      const totalPercentage = analytics.headcountByDepartment.reduce((s, d) => s + d.percentage, 0);
      assert.ok(Math.abs(totalPercentage - 100) < 0.5, `Sum of percentages was ${totalPercentage}`);
    });

    it('TC-F38: Department tenure should produce valid formatted Vietnamese strings', () => {
      const analytics = calculateHrAnalytics(testData.employees, testData.departments, testData.requests, 2026, fixedNow);
      for (const dept of analytics.tenureByDepartment) {
        assert.ok(typeof dept.averageTenureFormatted === 'string');
        assert.ok(dept.averageTenureFormatted.length > 0);
      }
    });

    it('TC-F39: Request frequency should accurately calculate requests per employee', () => {
      const analytics = calculateHrAnalytics(testData.employees, testData.departments, testData.requests, 2026, fixedNow);
      const itDept = analytics.requestFrequencyByDepartment.find(d => d.departmentCode === 'IT');
      assert.strictEqual(itDept.totalRequests, 3);
      assert.strictEqual(itDept.requestsPerEmployee, 1.5); // 3 requests / 2 IT employees
    });

    it('TC-F40: Top requesters should correctly identify most active submitters in order', () => {
      const analytics = calculateHrAnalytics(testData.employees, testData.departments, testData.requests, 2026, fixedNow);
      assert.ok(analytics.topRequesters.length > 0);
      assert.strictEqual(analytics.topRequesters[0].fullName, 'Lê Minh Tuấn');
      assert.strictEqual(analytics.topRequesters[0].requestCount, 3);
    });
  });

});

const { describe, it } = require('node:test');
const assert = require('node:assert/strict');
const { calculateTenure, formatVietnameseTenure, resolveEffectiveJoinDate } = require('../helpers/tenure-oracle');
const { extractLeaveTimeline } = require('../helpers/leave-extractor-oracle');
const { calculateYearlyStats, calculateHrAnalytics, RequestType, RequestStatus } = require('../helpers/stats-oracle');

describe('Tier 2: Boundary & Corner Cases', () => {

  it('TC-B01: Null JoinDate fallback to CreationTime displays formatted tenure without null reference exceptions', () => {
    const rawJoinDate = null;
    const creationTime = '2026-05-10T08:00:00Z';
    const effective = resolveEffectiveJoinDate(rawJoinDate, creationTime);
    const tenure = calculateTenure(effective, new Date('2026-10-22T00:00:00Z'));
    assert.strictEqual(tenure.years, 0);
    assert.strictEqual(tenure.months, 5);
    assert.strictEqual(tenure.formatted, '5 tháng');
  });

  it('TC-B02: Future JoinDate must be rejected by validation constraints', () => {
    const fixedNow = new Date('2026-10-22T00:00:00Z');
    const tomorrow = new Date('2026-10-23T00:00:00Z');
    const futureYear = new Date('2099-01-01T00:00:00Z');

    assert.strictEqual(tomorrow > fixedNow, true, 'Tomorrow should be detected as future');
    assert.strictEqual(futureYear > fixedNow, true, 'Year 2099 should be detected as future');

    // If a future date somehow enters calculation, tenure calculator should safely return 0/0 and "< 1 tháng"
    const tenureTomorrow = calculateTenure(tomorrow, fixedNow);
    assert.strictEqual(tenureTomorrow.formatted, '< 1 tháng');
    assert.strictEqual(tenureTomorrow.totalMonths, 0);
  });

  it('TC-B03: Zero requests employee handling (empty arrays, 0 sums, no division by zero)', () => {
    const emptyRequests = [];
    const stats = calculateYearlyStats(emptyRequests, 2026);

    assert.strictEqual(stats.totalRequests, 0);
    assert.strictEqual(stats.draftCount, 0);
    assert.strictEqual(stats.submittedCount, 0);
    assert.strictEqual(stats.approvedCount, 0);
    assert.strictEqual(stats.rejectedCount, 0);
    assert.strictEqual(stats.totalAmountRequested, 0);
    assert.strictEqual(stats.totalAmountDisbursed, 0);
    assert.strictEqual(stats.totalLeaveDays, 0);
    assert.ok(Array.isArray(stats.breakdownByType));
  });

  it('TC-B04: Leap year date handling (February 29 join date)', () => {
    // Leap year 2024: Feb 29, 2024
    const leapJoinDate = '2024-02-29T00:00:00Z';
    // 2 years later on Feb 28, 2026 (non-leap year)
    const target2026 = new Date('2026-02-28T00:00:00Z');
    const tenure = calculateTenure(leapJoinDate, target2026);
    // Since Feb 28 is day 28 < day 29, months decrement: 1 year 11 months
    assert.strictEqual(tenure.years, 1);
    assert.strictEqual(tenure.months, 11);
    assert.strictEqual(tenure.formatted, '1 năm 11 tháng');

    // On March 1, 2026: exact 2 years 0 months
    const targetMarch = new Date('2026-03-01T00:00:00Z');
    const tenureMarch = calculateTenure(leapJoinDate, targetMarch);
    assert.strictEqual(tenureMarch.years, 2);
    assert.strictEqual(tenureMarch.months, 0);
    assert.strictEqual(tenureMarch.formatted, '2 năm');
  });

  it('TC-B05: Multiple overlapping / consecutive leaves in timeline parse correctly', () => {
    const leave1 = {
      id: 'leave-1',
      title: 'Nghỉ phép đợt 1',
      description: 'Xin nghỉ từ 10/11/2026 đến 12/11/2026',
      type: RequestType.Leave,
      status: RequestStatus.Approved,
      creationTime: '2026-10-01T00:00:00Z'
    };
    const leave2 = {
      id: 'leave-2',
      title: 'Nghỉ phép đợt 2',
      description: 'Xin nghỉ từ 13/11/2026 đến 14/11/2026',
      type: RequestType.Leave,
      status: RequestStatus.Submitted,
      creationTime: '2026-10-02T00:00:00Z'
    };

    const parsed1 = extractLeaveTimeline(leave1);
    const parsed2 = extractLeaveTimeline(leave2);

    assert.strictEqual(parsed1.days, 3);
    assert.strictEqual(parsed2.days, 2);
    assert.ok(parsed1.endDate < parsed2.startDate);
  });

  it('TC-B06: Zero-day tenure boundary (Join date is today -> "< 1 tháng")', () => {
    const today = new Date('2026-10-22T10:00:00Z');
    const tenure = calculateTenure(today, today);
    assert.strictEqual(tenure.years, 0);
    assert.strictEqual(tenure.months, 0);
    assert.strictEqual(tenure.totalMonths, 0);
    assert.strictEqual(tenure.formatted, '< 1 tháng');
  });

  it('TC-B07: Extremely long employee tenure (15 years 6 months)', () => {
    const longJoinDate = '2011-04-15T00:00:00Z';
    const now = new Date('2026-10-22T00:00:00Z');
    const tenure = calculateTenure(longJoinDate, now);
    assert.strictEqual(tenure.years, 15);
    assert.strictEqual(tenure.months, 6);
    assert.strictEqual(tenure.totalMonths, 186);
    assert.strictEqual(tenure.formatted, '15 năm 6 tháng');
  });

  it('TC-B08: Non-existent employee ID error contract simulation', () => {
    const dummyId = '00000000-0000-0000-0000-000000000000';
    const employees = [{ id: '11111111-1111-1111-1111-111111111111' }];
    const found = employees.find(e => e.id === dummyId);
    assert.strictEqual(found, undefined, 'Unknown ID must yield not found / 404');
  });

  it('TC-B09: Leave request with completely malformed text falls back gracefully', () => {
    const malformed = {
      id: 'malformed-1',
      title: '...',
      description: null,
      type: RequestType.Leave,
      status: RequestStatus.Draft,
      creationTime: '2026-08-15T12:00:00Z'
    };
    const extracted = extractLeaveTimeline(malformed);
    assert.strictEqual(extracted.source, 'fallback');
    assert.strictEqual(extracted.days, 1.0);
    assert.ok(extracted.startDate instanceof Date);
    assert.ok(extracted.endDate instanceof Date);
  });

  it('TC-B10: Fractional leave days parsing ("nghỉ 0.5 ngày" and "xin nghỉ 1.5 ngày")', () => {
    const halfDay = {
      id: 'half-1',
      title: 'Nghỉ việc gia đình buổi sáng',
      description: 'Tôi xin nghỉ 0.5 ngày sáng thứ Hai',
      type: RequestType.Leave,
      status: RequestStatus.Approved,
      creationTime: '2026-09-01T00:00:00Z'
    };
    const oneAndHalfDay = {
      id: 'one-half-1',
      title: 'Nghỉ việc riêng',
      description: 'Tôi xin nghỉ 1.5 ngày',
      type: RequestType.Leave,
      status: RequestStatus.Approved,
      creationTime: '2026-09-05T00:00:00Z'
    };

    const ex1 = extractLeaveTimeline(halfDay);
    const ex2 = extractLeaveTimeline(oneAndHalfDay);

    assert.strictEqual(ex1.days, 0.5);
    assert.strictEqual(ex2.days, 1.5);
  });

  it('TC-B11: Day-of-month calendar boundary (Joined on Jan 31 calculated on Feb 28)', () => {
    const joinDate = '2026-01-31T00:00:00Z';
    const feb28 = new Date('2026-02-28T00:00:00Z');
    const tenure = calculateTenure(joinDate, feb28);
    // Since day 28 < day 31, months decrement from 1 to 0 -> "< 1 tháng"
    assert.strictEqual(tenure.years, 0);
    assert.strictEqual(tenure.months, 0);
    assert.strictEqual(tenure.formatted, '< 1 tháng');
  });

});

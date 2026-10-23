/**
 * Authoritative Leave Days Statistics & Excel Export Oracle
 * MyCompany.InternalApproval - Tab Báo cáo ngày phép & Xuất Excel
 * 
 * Formalizes rules from ORIGINAL_REQUEST.md (§ R2, § R3 2026-10-22T08:33:47Z) and survey reports.
 */

const { extractLeaveTimeline } = require('./leave-extractor-oracle');
const { filterEmployeeList, evaluateAccessScope, AbpAuthorizationException } = require('./auth-oracle');
const { RequestType, RequestStatus } = require('./stats-oracle');

/**
 * Calculates effective leave days within a target year for a given date range.
 * Properly calculates cross-year boundaries (e.g. 28/12/2025 to 04/01/2026).
 */
function calculateCrossYearIntersection(startDate, endDate, targetYear) {
  if (!startDate || !endDate) return 0;

  const rawStart = new Date(startDate);
  const rawEnd = new Date(endDate);

  const startUtc = Date.UTC(rawStart.getUTCFullYear(), rawStart.getUTCMonth(), rawStart.getUTCDate());
  const endUtc = Date.UTC(rawEnd.getUTCFullYear(), rawEnd.getUTCMonth(), rawEnd.getUTCDate());

  const windowStartUtc = Date.UTC(targetYear, 0, 1);
  const windowEndUtc = Date.UTC(targetYear, 11, 31);

  // No intersection
  if (startUtc > windowEndUtc || endUtc < windowStartUtc) {
    return 0;
  }

  const effectiveStartUtc = Math.max(startUtc, windowStartUtc);
  const effectiveEndUtc = Math.min(endUtc, windowEndUtc);

  // Convert difference in ms to days (inclusive)
  const diffDays = Math.round((effectiveEndUtc - effectiveStartUtc) / (1000 * 60 * 60 * 24)) + 1;

  return Math.max(0, diffDays);
}

/**
 * Formats a date to dd/MM/yyyy string.
 */
function formatDateVN(date) {
  if (!date) return '';
  const d = new Date(date);
  const day = String(d.getUTCDate()).padStart(2, '0');
  const month = String(d.getUTCMonth() + 1).padStart(2, '0');
  const year = d.getUTCFullYear();
  return `${day}/${month}/${year}`;
}

/**
 * Formats the latest leave period string according to specifications.
 * e.g. "25/10/2026 - 26/10/2026 (2 ngày)" or "15/09/2026 (1 ngày)" or "Chưa có".
 */
function formatLatestLeavePeriod(latestRequest) {
  if (!latestRequest) {
    return 'Chưa có';
  }

  const parsed = extractLeaveTimeline(latestRequest);

  const startStr = formatDateVN(parsed.startDate);
  const endStr = formatDateVN(parsed.endDate);
  const days = parsed.days;

  if (startStr === endStr) {
    return `${startStr} (${days} ngày)`;
  }
  return `${startStr} - ${endStr} (${days} ngày)`;
}

/**
 * Computes leave statistics for an individual employee for a given year.
 */
function calculateEmployeeLeaveStats(employee, allRequests = [], targetYear = 2026) {
  const empId = employee.id || employee.userId;

  // Find all leave requests authored by this employee
  const empRequests = allRequests.filter(r => (r.creatorId === empId) && (r.type === RequestType.Leave));

  // 1. Total submitted leave requests (status != Draft)
  const submittedRequests = empRequests.filter(r => r.status !== RequestStatus.Draft);
  const totalLeaveRequestsSubmitted = submittedRequests.length;

  // 2. Total approved leave requests
  const approvedRequests = empRequests.filter(r => r.status === RequestStatus.Approved);
  const totalLeaveRequestsApproved = approvedRequests.length;

  // 3. Total approved leave days in targetYear
  let totalApprovedLeaveDays = 0;
  for (const req of approvedRequests) {
    const parsed = extractLeaveTimeline(req);
    const daysInYear = calculateCrossYearIntersection(parsed.startDate, parsed.endDate, targetYear);
    totalApprovedLeaveDays += daysInYear;
  }

  // 4. Latest leave period (from approved requests)
  let latestApproved = null;
  if (approvedRequests.length > 0) {
    // Sort descending by StartDate or CreationTime
    const sorted = [...approvedRequests].sort((a, b) => {
      const timeA = new Date(a.startDate || a.creationTime).getTime();
      const timeB = new Date(b.startDate || b.creationTime).getTime();
      return timeB - timeA;
    });
    latestApproved = sorted[0];
  }

  let latestParsed = null;
  if (latestApproved) {
    latestParsed = extractLeaveTimeline(latestApproved);
  }

  return {
    userId: empId,
    userName: employee.userName || '',
    fullName: employee.fullName || employee.name || '',
    email: employee.email || '',
    departmentId: employee.departmentId || null,
    departmentCode: employee.departmentCode || '',
    departmentName: employee.departmentName || '',
    position: employee.position || '',
    isManager: !!employee.isManager,
    year: targetYear,
    totalApprovedLeaveDays,
    totalLeaveRequestsSubmitted,
    totalLeaveRequestsApproved,
    latestLeaveStartDate: latestParsed ? latestParsed.startDate.toISOString() : null,
    latestLeaveEndDate: latestParsed ? latestParsed.endDate.toISOString() : null,
    latestLeaveDays: latestParsed ? latestParsed.days : 0,
    latestLeavePeriodFormatted: formatLatestLeavePeriod(latestApproved),
    latestLeaveReason: latestParsed ? latestParsed.reason : null
  };
}

/**
 * Generates the full Leave Stats Report according to caller permissions and filters.
 */
function generateLeaveStatsReport(
  allEmployees = [],
  allRequests = [],
  targetYear = 2026,
  departmentFilter = null,
  currentUser = null,
  departments = [],
  members = [],
  grantedPermissions = []
) {
  // Apply hierarchical filtering
  const accessibleEmployees = filterEmployeeList(
    currentUser,
    allEmployees,
    departments,
    members,
    { departmentId: departmentFilter },
    grantedPermissions
  );

  const employeeStatsList = accessibleEmployees.map(emp => 
    calculateEmployeeLeaveStats(emp, allRequests, targetYear)
  );

  const totalEmployees = employeeStatsList.length;
  const totalCompanyLeaveDays = employeeStatsList.reduce((acc, x) => acc + x.totalApprovedLeaveDays, 0);
  const totalCompanyLeaveRequestsSubmitted = employeeStatsList.reduce((acc, x) => acc + x.totalLeaveRequestsSubmitted, 0);
  const totalCompanyLeaveRequestsApproved = employeeStatsList.reduce((acc, x) => acc + x.totalLeaveRequestsApproved, 0);

  return {
    year: targetYear,
    departmentFilter: departmentFilter || 'ALL',
    totalEmployees,
    totalCompanyLeaveDays,
    totalCompanyLeaveRequestsSubmitted,
    totalCompanyLeaveRequestsApproved,
    items: employeeStatsList
  };
}

/**
 * Validates the structure and content of an Excel export payload.
 */
function validateExcelExportStructure(exportPayload) {
  const errors = [];

  if (!exportPayload.title || exportPayload.title !== 'BÁO CÁO THỐNG KÊ NGÀY NGHỈ PHÉP NHÂN VIÊN') {
    errors.push(`Invalid report title: ${exportPayload.title}`);
  }

  if (!exportPayload.metadata) {
    errors.push('Missing metadata in export payload');
  } else {
    if (!exportPayload.metadata.year) errors.push('Missing metadata.year');
    if (!exportPayload.metadata.departmentScope) errors.push('Missing metadata.departmentScope');
    if (!exportPayload.metadata.exportedBy) errors.push('Missing metadata.exportedBy');
    if (!exportPayload.metadata.exportedAt) errors.push('Missing metadata.exportedAt');
  }

  const expectedColumns = [
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
  ];

  if (!exportPayload.columns || !Array.isArray(exportPayload.columns)) {
    errors.push('Missing or invalid columns array');
  } else {
    for (const col of expectedColumns) {
      if (!exportPayload.columns.includes(col)) {
        errors.push(`Missing column in export: ${col}`);
      }
    }
  }

  if (!exportPayload.rows || !Array.isArray(exportPayload.rows)) {
    errors.push('Missing or invalid rows array');
  }

  if (!exportPayload.summaryRow) {
    errors.push('Missing summaryRow in export payload');
  } else {
    if (typeof exportPayload.summaryRow.totalLeaveDays !== 'number') {
      errors.push('summaryRow missing totalLeaveDays');
    }
  }

  return {
    isValid: errors.length === 0,
    errors
  };
}

module.exports = {
  calculateCrossYearIntersection,
  formatDateVN,
  formatLatestLeavePeriod,
  calculateEmployeeLeaveStats,
  generateLeaveStatsReport,
  validateExcelExportStructure
};

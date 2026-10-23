/**
 * Stats Oracle - Reference mathematical and domain aggregation models
 */

const { calculateTenure, formatVietnameseTenure } = require('./tenure-oracle');
const { extractLeaveTimeline } = require('./leave-extractor-oracle');

const RequestType = {
  Leave: 1,
  Equipment: 2,
  AdvanceMoney: 3
};

const RequestStatus = {
  Draft: 0,
  Submitted: 1,
  Approved: 2,
  Rejected: 3
};

function calculateYearlyStats(requests, year = new Date().getFullYear()) {
  const yearRequests = requests.filter(r => {
    const d = new Date(r.creationTime);
    return d.getFullYear() === year;
  });

  let draftCount = 0;
  let submittedCount = 0;
  let approvedCount = 0;
  let rejectedCount = 0;
  let totalAmountRequested = 0;
  let totalAmountDisbursed = 0;
  let totalLeaveDays = 0;

  const breakdownMap = {
    [RequestType.Leave]: { type: RequestType.Leave, typeName: 'Nghỉ phép', count: 0, totalAmount: 0 },
    [RequestType.Equipment]: { type: RequestType.Equipment, typeName: 'Mua sắm thiết bị', count: 0, totalAmount: 0 },
    [RequestType.AdvanceMoney]: { type: RequestType.AdvanceMoney, typeName: 'Tạm ứng tiền', count: 0, totalAmount: 0 }
  };

  for (const r of yearRequests) {
    if (r.status === RequestStatus.Draft) draftCount++;
    else if (r.status === RequestStatus.Submitted) submittedCount++;
    else if (r.status === RequestStatus.Approved) approvedCount++;
    else if (r.status === RequestStatus.Rejected) rejectedCount++;

    if (breakdownMap[r.type]) {
      breakdownMap[r.type].count++;
    }

    const amount = Number(r.amount) || 0;
    if (r.type === RequestType.Equipment || r.type === RequestType.AdvanceMoney) {
      totalAmountRequested += amount;
      if (breakdownMap[r.type]) {
        breakdownMap[r.type].totalAmount += amount;
      }
      if (r.status === RequestStatus.Approved) {
        totalAmountDisbursed += amount;
      }
    }

    if (r.type === RequestType.Leave && r.status === RequestStatus.Approved) {
      const extracted = extractLeaveTimeline(r);
      totalLeaveDays += extracted.days;
    }
  }

  const totalRequests = draftCount + submittedCount + approvedCount + rejectedCount;

  return {
    year,
    totalRequests,
    draftCount,
    submittedCount,
    approvedCount,
    rejectedCount,
    totalAmountRequested,
    totalAmountDisbursed,
    totalLeaveDays,
    breakdownByType: Object.values(breakdownMap)
  };
}

function calculateHrAnalytics(employees, departments, requests, year = new Date().getFullYear(), targetDate = new Date()) {
  const totalEmployees = employees.length;
  const totalDepartments = departments.length;

  let totalMonthsSum = 0;
  for (const emp of employees) {
    const effectiveJoinDate = emp.joinDate || emp.creationTime;
    const { totalMonths } = calculateTenure(effectiveJoinDate, targetDate);
    totalMonthsSum += totalMonths;
  }

  const companyAvgMonths = totalEmployees > 0 ? totalMonthsSum / totalEmployees : 0;
  const companyAvgTenureFormatted = formatVietnameseTenure(
    Math.floor(companyAvgMonths / 12),
    Math.round(companyAvgMonths % 12),
    Math.round(companyAvgMonths)
  );

  const yearRequests = requests.filter(r => new Date(r.creationTime).getFullYear() === year);
  const totalRequestsThisYear = yearRequests.length;
  const totalDisbursedAmountThisYear = yearRequests
    .filter(r => r.status === RequestStatus.Approved && (r.type === RequestType.Equipment || r.type === RequestType.AdvanceMoney))
    .reduce((sum, r) => sum + (Number(r.amount) || 0), 0);

  // Group by department
  const deptMap = {};
  for (const d of departments) {
    deptMap[d.id] = {
      departmentId: d.id,
      departmentCode: d.code,
      departmentName: d.name,
      employees: [],
      requests: []
    };
  }

  for (const emp of employees) {
    if (emp.departmentId && deptMap[emp.departmentId]) {
      deptMap[emp.departmentId].employees.push(emp);
    }
  }

  for (const r of yearRequests) {
    if (r.departmentId && deptMap[r.departmentId]) {
      deptMap[r.departmentId].requests.push(r);
    }
  }

  const headcountByDepartment = [];
  const tenureByDepartment = [];
  const requestFrequencyByDepartment = [];

  for (const d of departments) {
    const group = deptMap[d.id];
    const headcount = group.employees.length;
    const percentage = totalEmployees > 0 ? Math.round((headcount / totalEmployees) * 1000) / 10 : 0;

    headcountByDepartment.push({
      departmentId: d.id,
      departmentCode: d.code,
      departmentName: d.name,
      headcount,
      percentage
    });

    let deptMonths = 0;
    let minMonths = headcount > 0 ? Infinity : 0;
    let maxMonths = 0;

    for (const emp of group.employees) {
      const { totalMonths } = calculateTenure(emp.joinDate || emp.creationTime, targetDate);
      deptMonths += totalMonths;
      if (totalMonths < minMonths) minMonths = totalMonths;
      if (totalMonths > maxMonths) maxMonths = totalMonths;
    }

    if (headcount === 0) minMonths = 0;

    const avgMonths = headcount > 0 ? deptMonths / headcount : 0;
    tenureByDepartment.push({
      departmentId: d.id,
      departmentCode: d.code,
      departmentName: d.name,
      averageTenureMonths: avgMonths,
      averageTenureFormatted: formatVietnameseTenure(
        Math.floor(avgMonths / 12),
        Math.round(avgMonths % 12),
        Math.round(avgMonths)
      ),
      minTenureMonths: minMonths,
      maxTenureMonths: maxMonths
    });

    const deptRequests = group.requests.length;
    requestFrequencyByDepartment.push({
      departmentId: d.id,
      departmentCode: d.code,
      departmentName: d.name,
      totalRequests: deptRequests,
      requestsPerEmployee: headcount > 0 ? Math.round((deptRequests / headcount) * 100) / 100 : 0
    });
  }

  // Top Requesters
  const employeeRequestCounts = {};
  for (const r of yearRequests) {
    const uid = r.creatorId;
    if (uid) {
      employeeRequestCounts[uid] = (employeeRequestCounts[uid] || 0) + 1;
    }
  }

  const topRequesters = Object.entries(employeeRequestCounts)
    .map(([userId, count]) => {
      const emp = employees.find(e => e.userId === userId || e.id === userId);
      return {
        userId,
        fullName: emp ? emp.fullName : 'Nhân viên',
        departmentName: emp ? emp.departmentName : '',
        requestCount: count
      };
    })
    .sort((a, b) => b.requestCount - a.requestCount)
    .slice(0, 10);

  return {
    companySummary: {
      totalEmployees,
      totalDepartments,
      companyAverageTenureMonths: companyAvgMonths,
      companyAverageTenureFormatted: companyAvgTenureFormatted,
      totalRequestsThisYear,
      totalDisbursedAmountThisYear
    },
    headcountByDepartment,
    tenureByDepartment,
    requestFrequencyByDepartment,
    topRequesters
  };
}

module.exports = {
  RequestType,
  RequestStatus,
  calculateYearlyStats,
  calculateHrAnalytics
};

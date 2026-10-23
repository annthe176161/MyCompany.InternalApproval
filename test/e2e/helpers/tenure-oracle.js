/**
 * Tenure Calculator Oracle - Reference implementation based on ORIGINAL_REQUEST.md & PROJECT.md
 * 
 * Rules:
 * - If target date is before join date: (0, 0, 0) -> "< 1 tháng"
 * - Years = target.Year - joinDate.Year
 * - Months = target.Month - joinDate.Month
 * - If target.Day < joinDate.Day -> months--
 * - If months < 0 -> years--, months += 12
 * - If years < 0 -> years = 0, months = 0
 * 
 * Vietnamese formatting:
 * - totalMonths <= 0 -> "< 1 tháng"
 * - years == 0 -> "${months} tháng"
 * - months == 0 -> "${years} năm"
 * - otherwise -> "${years} năm ${months} tháng"
 */

function calculateTenure(joinDateInput, targetDateInput = new Date()) {
  const joinDate = new Date(joinDateInput);
  const targetDate = new Date(targetDateInput);

  if (targetDate < joinDate) {
    return { years: 0, months: 0, totalMonths: 0, formatted: '< 1 tháng' };
  }

  let years = targetDate.getFullYear() - joinDate.getFullYear();
  let months = targetDate.getMonth() - joinDate.getMonth();

  if (targetDate.getDate() < joinDate.getDate()) {
    months--;
  }

  if (months < 0) {
    years--;
    months += 12;
  }

  if (years < 0) {
    years = 0;
    months = 0;
  }

  const totalMonths = years * 12 + months;
  const formatted = formatVietnameseTenure(years, months, totalMonths);

  return { years, months, totalMonths, formatted };
}

function formatVietnameseTenure(years, months, totalMonths) {
  if (totalMonths <= 0 || (years <= 0 && months <= 0)) {
    return '< 1 tháng';
  }
  if (years <= 0) {
    return `${months} tháng`;
  }
  if (months <= 0) {
    return `${years} năm`;
  }
  return `${years} năm ${months} tháng`;
}

function resolveEffectiveJoinDate(joinDate, creationTime) {
  if (joinDate && joinDate !== null && joinDate !== undefined) {
    return new Date(joinDate);
  }
  return new Date(creationTime);
}

module.exports = {
  calculateTenure,
  formatVietnameseTenure,
  resolveEffectiveJoinDate
};

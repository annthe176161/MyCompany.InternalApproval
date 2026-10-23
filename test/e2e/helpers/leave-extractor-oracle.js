/**
 * Leave Timeline Extractor Oracle - Reference implementation based on ORIGINAL_REQUEST.md & PROJECT.md
 */

const DATE_RANGE_REGEX = /từ\s+(?:(?:ngày|khoảng)\s+)?(\d{1,2})[/-](\d{1,2})(?:[/-](\d{4}))?\s+(?:đến|tới)(?:\s+hết)?(?:\s+ngày)?\s+(\d{1,2})[/-](\d{1,2})(?:[/-](\d{4}))?/i;
const DAYS_COUNT_REGEX = /(?:nghỉ|xin nghỉ)?\s*(\d+(?:[.,]\d+)?)\s*ngày/i;

function extractLeaveTimeline(request) {
  const title = request.title || '';
  const description = request.description || '';
  const creationTime = new Date(request.creationTime || Date.now());
  const text = `${title} ${description}`.trim();

  // Tier 1: Structured properties
  if (request.startDate && request.endDate) {
    const start = new Date(request.startDate);
    const end = new Date(request.endDate);
    const diffTime = Math.abs(end.getTime() - start.getTime());
    const days = request.days || (Math.round(diffTime / (1000 * 60 * 60 * 24)) + 1);
    return {
      requestId: request.id,
      title: request.title,
      startDate: start,
      endDate: end,
      days: days,
      reason: request.reason || request.description || request.title,
      status: request.status,
      approverName: request.approverName,
      approvalTime: request.approvalTime,
      rejectionReason: request.rejectionReason,
      source: 'structured'
    };
  }

  // Tier 2: Regex range parsing
  const rangeMatch = text.match(DATE_RANGE_REGEX);
  if (rangeMatch) {
    const d1 = parseInt(rangeMatch[1], 10);
    const m1 = parseInt(rangeMatch[2], 10) - 1; // 0-indexed in JS
    const y1 = rangeMatch[3] ? parseInt(rangeMatch[3], 10) : creationTime.getFullYear();

    const d2 = parseInt(rangeMatch[4], 10);
    const m2 = parseInt(rangeMatch[5], 10) - 1;
    const y2 = rangeMatch[6] ? parseInt(rangeMatch[6], 10) : y1;

    const startDate = new Date(Date.UTC(y1, m1, d1));
    let endDate = new Date(Date.UTC(y2, m2, d2));
    if (endDate < startDate) {
      endDate = new Date(startDate);
    }

    const diffDays = Math.round((endDate.getTime() - startDate.getTime()) / (1000 * 60 * 60 * 24)) + 1;
    return {
      requestId: request.id,
      title: request.title,
      startDate,
      endDate,
      days: diffDays,
      reason: description || title,
      status: request.status,
      approverName: request.approverName,
      approvalTime: request.approvalTime,
      rejectionReason: request.rejectionReason,
      source: 'regex-range'
    };
  }

  // Tier 2b: Regex days count
  let parsedDays = 1.0;
  const daysMatch = text.match(DAYS_COUNT_REGEX);
  if (daysMatch) {
    parsedDays = parseFloat(daysMatch[1].replace(',', '.'));
  }

  // Tier 3: Fallback using CreationTime
  const startDate = new Date(Date.UTC(creationTime.getFullYear(), creationTime.getMonth(), creationTime.getDate()));
  const endDate = new Date(startDate.getTime() + Math.max(0, Math.ceil(parsedDays) - 1) * 24 * 60 * 60 * 1000);

  return {
    requestId: request.id,
    title: request.title,
    startDate,
    endDate,
    days: parsedDays,
    reason: description || title,
    status: request.status,
    approverName: request.approverName,
    approvalTime: request.approvalTime,
    rejectionReason: request.rejectionReason,
    source: 'fallback'
  };
}

module.exports = {
  extractLeaveTimeline
};

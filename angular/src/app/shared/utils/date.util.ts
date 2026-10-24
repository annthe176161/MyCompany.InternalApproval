// Tính số ngày nghỉ phép (bao gồm cả ngày bắt đầu và ngày kết thúc)
export function calculateLeaveDays(
  startDate?: string | Date | null,
  endDate?: string | Date | null,
): number | null {
  if (!startDate) return null;
  const start = new Date(startDate);
  const end = endDate ? new Date(endDate) : new Date(startDate);
  start.setHours(0, 0, 0, 0);
  end.setHours(0, 0, 0, 0);
  const diffTime = end.getTime() - start.getTime();
  const days = Math.round(diffTime / (1000 * 3600 * 24)) + 1;
  return days > 0 ? days : 1;
}

// Định dạng ngày theo chuẩn DD/MM/YYYY
export function formatDateVn(dateVal?: string | Date | null): string {
  if (!dateVal) return '';
  const d = dateVal instanceof Date ? dateVal : new Date(dateVal);
  if (isNaN(d.getTime())) return '';
  const pad = (n: number) => (n < 10 ? '0' + n : `${n}`);
  return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()}`;
}

// Định dạng ngày giờ theo chuẩn DD/MM/YYYY HH:mm
export function formatDateTimeShortVn(dateVal?: string | Date | null): string {
  if (!dateVal) return '';
  const d = dateVal instanceof Date ? dateVal : new Date(dateVal);
  if (isNaN(d.getTime())) return '';
  const pad = (n: number) => (n < 10 ? '0' + n : `${n}`);
  return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

// Định dạng ngày giờ đầy đủ theo chuẩn DD/MM/YYYY HH:mm:ss
export function formatDateTimeFullVn(dateVal?: string | Date | null): string {
  if (!dateVal) return '';
  const d = dateVal instanceof Date ? dateVal : new Date(dateVal);
  if (isNaN(d.getTime())) return '';
  const pad = (n: number) => (n < 10 ? '0' + n : `${n}`);
  return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
}

// Chuyển đổi ngày sang chuỗi ISO cục bộ YYYY-MM-DDT00:00:00 (tránh lệch múi giờ UTC)
export function formatToLocalIso(dateVal: Date | string): string {
  const d = new Date(dateVal);
  const pad = (n: number) => (n < 10 ? '0' + n : `${n}`);
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T00:00:00`;
}

// Tạo chuỗi dấu thời gian phục vụ đặt tên file xuất báo cáo: YYYYMMDD_HHmm
export function getFileTimestamp(): string {
  const d = new Date();
  const pad = (n: number) => (n < 10 ? '0' + n : `${n}`);
  return `${d.getFullYear()}${pad(d.getMonth() + 1)}${pad(d.getDate())}_${pad(d.getHours())}${pad(d.getMinutes())}`;
}


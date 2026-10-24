// Định dạng số tiền sang chuẩn tiền tệ Việt Nam (VNĐ)
export function formatMoneyVnd(amount?: number | null): string {
  if (amount === undefined || amount === null) return '0 ₫';
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
}

// Định dạng hiển thị dấu phân cách hàng nghìn cho ô nhập số tiền
export function formatCurrencyInput(value: number): string {
  return value ? `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',') : '';
}

// Phân tích chuỗi từ ô nhập số tiền về giá trị số nguyên gốc
export function parseCurrencyInput(value: string): string {
  return value.replace(/\$\s?|(,*)/g, '');
}


// Trích xuất thông báo lỗi chi tiết từ phản hồi API ABP hoặc trả về thông báo mặc định
export function extractErrorMessage(err: any, fallback: string): string {
  const validationErrors = err?.error?.error?.validationErrors;
  if (Array.isArray(validationErrors) && validationErrors.length > 0) {
    return validationErrors
      .map((v: any) => v.message)
      .filter(Boolean)
      .join('; ');
  }
  return err?.error?.error?.message || err?.error?.message || fallback;
}


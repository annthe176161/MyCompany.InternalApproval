// Cấu trúc mẫu biểu thức chính quy phục vụ dịch chuỗi động
export interface UiPatternTranslation {
  pattern: RegExp;
  replace: (...matches: string[]) => string;
}


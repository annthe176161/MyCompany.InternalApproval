// Cấu hình hiển thị của một cột trong bảng dữ liệu
export interface ColumnConfig {
  key: string;
  label: string;
  visible: boolean;
  width: string;
  required?: boolean;
}


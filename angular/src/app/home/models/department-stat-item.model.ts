// Mô hình thống kê đề xuất theo phòng ban trên Dashboard
export interface DepartmentStatItem {
  code: string;
  name: string;
  count: number;
  totalAmount: number;
  percent: number;
  color: string;
}


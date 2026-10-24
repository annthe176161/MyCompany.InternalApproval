import { RequestType } from '../../proxy/requests';

// Mô hình thống kê đề xuất theo loại hình trên Dashboard
export interface TypeStatItem {
  type: RequestType;
  label: string;
  count: number;
  amount: number;
  percent: number;
  color: string;
}


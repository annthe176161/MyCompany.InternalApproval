import { RequestStatusMeta } from '../../shared/models';

// Mô hình thống kê đề xuất theo trạng thái trên Dashboard
export interface StatusStatItem {
  meta: RequestStatusMeta;
  count: number;
  percent?: number;
  subtext: string;
}


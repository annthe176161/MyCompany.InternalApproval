import { RequestDto } from '../../proxy/requests';

// Dữ liệu đầu vào khi xuất báo cáo tổng hợp Dashboard ra Excel
export interface DashboardReportData {
  totalCount: number;
  totalAmount: number;
  approvedAmount: number;
  approvalRate: number;
  statusStats: { label: string; count: number }[];
  typeStats: { label: string; count: number; amount: number; percent: number }[];
  requests: RequestDto[];
  scopeName: string;
  exporterName: string;
}


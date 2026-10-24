import { RequestStatus } from '../../proxy/requests/request-status.enum';

// Siêu dữ liệu hiển thị cho từng trạng thái đề xuất
export interface RequestStatusMeta {
  value: RequestStatus;
  label: string;
  badgeStatus: 'default' | 'processing' | 'success' | 'error' | 'warning';
  color: string;
  icon: string;
  cardClass: string;
  textClass: string;
  subtext: string;
  mySubtext: string;
  approverSubtext: string;
}


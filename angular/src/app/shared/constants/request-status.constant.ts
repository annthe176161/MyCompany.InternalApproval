import { RequestStatus } from '../../proxy/requests/request-status.enum';

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

export const REQUEST_STATUS_CONFIG: Record<RequestStatus, RequestStatusMeta> = {
  [RequestStatus.Draft]: {
    value: RequestStatus.Draft,
    label: 'Nháp',
    badgeStatus: 'default',
    color: 'default',
    icon: 'snippets',
    cardClass: 'bg-slate',
    textClass: 'text-muted',
    subtext: 'Bản nháp lưu tạm',
    mySubtext: 'Bản nháp bạn chưa gửi',
    approverSubtext: 'Hồ sơ nháp',
  },
  [RequestStatus.Submitted]: {
    value: RequestStatus.Submitted,
    label: 'Đã gửi duyệt',
    badgeStatus: 'processing',
    color: 'blue',
    icon: 'clock-circle',
    cardClass: 'bg-amber',
    textClass: 'text-warning',
    subtext: 'Hồ sơ đang đợi xét duyệt',
    mySubtext: 'Đang đợi quản lý xem xét',
    approverSubtext: 'Hồ sơ đang đợi bạn duyệt',
  },
  [RequestStatus.Approved]: {
    value: RequestStatus.Approved,
    label: 'Đã duyệt',
    badgeStatus: 'success',
    color: 'green',
    icon: 'check-circle',
    cardClass: 'bg-emerald',
    textClass: 'text-success',
    subtext: 'Hồ sơ đã được thông qua',
    mySubtext: 'Hồ sơ đã được thông qua',
    approverSubtext: 'Hồ sơ bạn đã phê duyệt',
  },
  [RequestStatus.Rejected]: {
    value: RequestStatus.Rejected,
    label: 'Bị từ chối',
    badgeStatus: 'error',
    color: 'red',
    icon: 'close-circle',
    cardClass: 'bg-red',
    textClass: 'text-danger',
    subtext: 'Hồ sơ không được thông qua',
    mySubtext: 'Cần xem lý do và cập nhật lại',
    approverSubtext: 'Hồ sơ bạn đã từ chối',
  },
};

export const REQUEST_STATUS_LIST: RequestStatusMeta[] = [
  REQUEST_STATUS_CONFIG[RequestStatus.Draft],
  REQUEST_STATUS_CONFIG[RequestStatus.Submitted],
  REQUEST_STATUS_CONFIG[RequestStatus.Approved],
  REQUEST_STATUS_CONFIG[RequestStatus.Rejected],
];

export function getRequestStatusMeta(status: RequestStatus): RequestStatusMeta {
  return (
    REQUEST_STATUS_CONFIG[status] || {
      value: status,
      label: 'Không xác định',
      badgeStatus: 'default',
      color: 'default',
      icon: 'question-circle',
      cardClass: 'bg-slate',
      textClass: 'text-muted',
      subtext: 'Trạng thái không xác định',
      mySubtext: 'Trạng thái không xác định',
      approverSubtext: 'Trạng thái không xác định',
    }
  );
}


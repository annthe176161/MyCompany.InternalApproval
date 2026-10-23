import type { EntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import { RequestStatus } from '../requests/request-status.enum';

export interface ApprovalNotificationDto extends EntityDto<string> {
  requestId: string;
  title: string;
  status: RequestStatus;
  receiverId: string;
  approverId?: string;
  approverName?: string;
  message: string;
  isRead: boolean;
  creationTime: string;
}

export interface GetNotificationListInput extends PagedAndSortedResultRequestDto {}


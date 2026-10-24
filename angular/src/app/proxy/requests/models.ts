import type { RequestType } from './request-type.enum';
import type { CreationAuditedEntityDto, FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { RequestStatus } from './request-status.enum';

export interface ApproveRequestDto {
  note?: string;
}

export interface CreateRequestCommentDto {
  content: string;
}

export interface CreateRequestDto {
  title: string;
  description?: string;
  type: RequestType;
  amount?: number;
  startDate?: string;
  endDate?: string;
}

export interface GetRequestListInput extends PagedAndSortedResultRequestDto {
  filter?: string;
  status?: RequestStatus;
  type?: RequestType;
  creatorId?: string;
}

export interface RejectRequestDto {
  reason: string;
}

export interface RequestCommentDto extends CreationAuditedEntityDto<string> {
  requestId?: string;
  content?: string;
  creatorName?: string;
}

export interface RequestDto extends FullAuditedEntityDto<string> {
  id: string;
  title?: string;
  description?: string;
  type: RequestType;
  status: RequestStatus;
  amount?: number;
  startDate?: string;
  endDate?: string;
  approverId?: string;
  approverName?: string;
  approvalTime?: string;
  rejectionReason?: string;
  creatorName?: string;
  departmentName?: string;
  departmentCode?: string;
  comments: RequestCommentDto[];
}

export interface UpdateRequestDto {
  title: string;
  description?: string;
  type: RequestType;
  amount?: number;
  startDate?: string;
  endDate?: string;
}

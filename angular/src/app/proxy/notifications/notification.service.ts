import { Injectable } from '@angular/core';
import { RestService, Rest, PagedResultDto } from '@abp/ng.core';
import type { ApprovalNotificationDto, GetNotificationListInput } from './models';

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  apiName = 'Default';

  getList = (input: GetNotificationListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<ApprovalNotificationDto>>(
      {
        method: 'GET',
        url: '/api/app/notification',
        params: {
          sorting: input.sorting,
          skipCount: input.skipCount,
          maxResultCount: input.maxResultCount,
        },
      },
      { apiName: this.apiName, ...config }
    );

  getUnreadCount = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, number>(
      {
        method: 'GET',
        url: '/api/app/notification/unread-count',
      },
      { apiName: this.apiName, ...config }
    );

  markAsRead = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>(
      {
        method: 'POST',
        url: `/api/app/notification/${id}/mark-as-read`,
      },
      { apiName: this.apiName, ...config }
    );

  markAllAsRead = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>(
      {
        method: 'POST',
        url: '/api/app/notification/mark-all-as-read',
      },
      { apiName: this.apiName, ...config }
    );

  constructor(private restService: RestService) {}
}


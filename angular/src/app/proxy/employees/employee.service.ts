import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { RequestDto } from '../requests/models';
import type {
  EmployeeLeaveReportItemDto,
  EmployeeListItemDto,
  EmployeeProfileDto,
  EmployeeYearlyStatsDto,
  GetEmployeeListInput,
  GetEmployeeRequestsInput,
  GetLeaveReportInput,
  HrAnalyticsDto,
  LeaveTimelineItemDto,
  UpdateEmployeeJoinDateDto,
} from './models';

@Injectable({
  providedIn: 'root',
})
export class EmployeeService {
  apiName = 'Default';

  constructor(private restService: RestService) {}

  getList = (input: GetEmployeeListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<EmployeeListItemDto>>(
      {
        method: 'GET',
        url: '/api/app/employee',
        params: {
          filter: input.filter,
          departmentId: input.departmentId,
          minTenureMonths: input.minTenureMonths,
          maxTenureMonths: input.maxTenureMonths,
          sorting: input.sorting,
          skipCount: input.skipCount,
          maxResultCount: input.maxResultCount,
        },
      },
      { apiName: this.apiName, ...config }
    );

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, EmployeeProfileDto>(
      {
        method: 'GET',
        url: `/api/app/employee/${id}`,
      },
      { apiName: this.apiName, ...config }
    );

  updateJoinDate = (
    id: string,
    input: UpdateEmployeeJoinDateDto,
    config?: Partial<Rest.Config>
  ) =>
    this.restService.request<any, EmployeeProfileDto>(
      {
        method: 'PUT',
        url: `/api/app/employee/${id}/join-date`,
        body: input,
      },
      { apiName: this.apiName, ...config }
    );

  getRequests = (
    id: string,
    input: GetEmployeeRequestsInput,
    config?: Partial<Rest.Config>
  ) =>
    this.restService.request<any, PagedResultDto<RequestDto>>(
      {
        method: 'GET',
        url: `/api/app/employee/${id}/requests`,
        params: {
          status: input.status,
          type: input.type,
          year: input.year,
          sorting: input.sorting,
          skipCount: input.skipCount,
          maxResultCount: input.maxResultCount,
        },
      },
      { apiName: this.apiName, ...config }
    );

  getYearlyStats = (id: string, year?: number, config?: Partial<Rest.Config>) =>
    this.restService.request<any, EmployeeYearlyStatsDto>(
      {
        method: 'GET',
        url: `/api/app/employee/${id}/yearly-stats`,
        params: { year },
      },
      { apiName: this.apiName, ...config }
    );

  getLeaveTimeline = (id: string, year?: number, config?: Partial<Rest.Config>) =>
    this.restService.request<any, LeaveTimelineItemDto[]>(
      {
        method: 'GET',
        url: `/api/app/employee/${id}/leave-timeline`,
        params: { year },
      },
      { apiName: this.apiName, ...config }
    );

  getAnalytics = (year?: number, config?: Partial<Rest.Config>) =>
    this.restService.request<any, HrAnalyticsDto>(
      {
        method: 'GET',
        url: '/api/app/employee/analytics',
        params: { year },
      },
      { apiName: this.apiName, ...config }
    );

  getLeaveReport = (input: GetLeaveReportInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<EmployeeLeaveReportItemDto>>(
      {
        method: 'GET',
        url: '/api/app/employee/leave-report',
        params: {
          year: input.year,
          departmentId: input.departmentId,
          filter: input.filter,
          sorting: input.sorting,
          skipCount: input.skipCount,
          maxResultCount: input.maxResultCount,
        },
      },
      { apiName: this.apiName, ...config }
    );
}

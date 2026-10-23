import type { ApproveRequestDto, CreateRequestCommentDto, CreateRequestDto, GetRequestListInput, RejectRequestDto, RequestCommentDto, RequestDto, UpdateRequestDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class RequestService {
  apiName = 'Default';
  

  addComment = (id: string, input: CreateRequestCommentDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RequestCommentDto>({
      method: 'POST',
      url: `/api/app/request/${id}/comment`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  approve = (id: string, input: ApproveRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RequestDto>({
      method: 'POST',
      url: `/api/app/request/${id}/approve`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RequestDto>({
      method: 'POST',
      url: '/api/app/request',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/request/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RequestDto>({
      method: 'GET',
      url: `/api/app/request/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetRequestListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<RequestDto>>({
      method: 'GET',
      url: '/api/app/request',
      params: { filter: input.filter, status: input.status, type: input.type, creatorId: input.creatorId, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  reject = (id: string, input: RejectRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RequestDto>({
      method: 'POST',
      url: `/api/app/request/${id}/reject`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  submit = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RequestDto>({
      method: 'POST',
      url: `/api/app/request/${id}/submit`,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdateRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RequestDto>({
      method: 'PUT',
      url: `/api/app/request/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}

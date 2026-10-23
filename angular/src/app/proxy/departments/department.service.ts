import { Injectable } from '@angular/core';
import { RestService, Rest, PagedResultDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type {
  DepartmentDto,
  CreateUpdateDepartmentDto,
  DepartmentMemberDto,
  AddDepartmentMemberDto,
  UpdateDepartmentMemberDto,
  DepartmentUserLookupDto
} from './models';

@Injectable({
  providedIn: 'root',
})
export class DepartmentService {
  apiName = 'Default';

  constructor(private restService: RestService) {}

  getList = (input: PagedAndSortedResultRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<DepartmentDto>>({
      method: 'GET',
      url: '/api/app/department',
      params: { skipCount: input.skipCount, maxResultCount: input.maxResultCount, sorting: input.sorting },
    },
    { apiName: this.apiName, ...config });

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, DepartmentDto>({
      method: 'GET',
      url: `/api/app/department/${id}`,
    },
    { apiName: this.apiName, ...config });

  create = (input: CreateUpdateDepartmentDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, DepartmentDto>({
      method: 'POST',
      url: '/api/app/department',
      body: input,
    },
    { apiName: this.apiName, ...config });

  update = (id: string, input: CreateUpdateDepartmentDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, DepartmentDto>({
      method: 'PUT',
      url: `/api/app/department/${id}`,
      body: input,
    },
    { apiName: this.apiName, ...config });

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/department/${id}`,
    },
    { apiName: this.apiName, ...config });

  getMembers = (departmentId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, DepartmentMemberDto[]>({
      method: 'GET',
      url: `/api/app/department/members/${departmentId}`,
    },
    { apiName: this.apiName, ...config });

  addMember = (departmentId: string, input: AddDepartmentMemberDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/department/member/${departmentId}`,
      body: input,
    },
    { apiName: this.apiName, ...config });

  updateMember = (departmentId: string, userId: string, input: UpdateDepartmentMemberDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'PUT',
      url: '/api/app/department/member',
      params: { departmentId, userId },
      body: input,
    },
    { apiName: this.apiName, ...config });

  removeMember = (departmentId: string, userId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: '/api/app/department/member',
      params: { departmentId, userId },
    },
    { apiName: this.apiName, ...config });

  setManager = (departmentId: string, userId?: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: '/api/app/department/set-manager',
      params: { departmentId, userId },
    },
    { apiName: this.apiName, ...config });

  getAvailableUsers = (departmentId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, DepartmentUserLookupDto[]>({
      method: 'GET',
      url: `/api/app/department/available-users/${departmentId}`,
    },
    { apiName: this.apiName, ...config });
}


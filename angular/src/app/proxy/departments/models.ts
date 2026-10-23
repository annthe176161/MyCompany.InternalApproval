import type { FullAuditedEntityDto } from '@abp/ng.core';

export interface DepartmentDto extends FullAuditedEntityDto<string> {
  code: string;
  name: string;
  description?: string;
  managerId?: string;
  managerName?: string;
  memberCount: number;
}

export interface CreateUpdateDepartmentDto {
  code: string;
  name: string;
  description?: string;
  managerId?: string;
}

export interface DepartmentMemberDto {
  id: string;
  departmentId: string;
  userId: string;
  userName: string;
  fullName: string;
  email: string;
  position: string;
  isManager: boolean;
  creationTime: string;
}

export interface AddDepartmentMemberDto {
  userId: string;
  position: string;
}

export interface UpdateDepartmentMemberDto {
  position: string;
}

export interface DepartmentUserLookupDto {
  id: string;
  userName: string;
  fullName: string;
  email: string;
  currentDepartmentName?: string;
  currentPosition?: string;
}


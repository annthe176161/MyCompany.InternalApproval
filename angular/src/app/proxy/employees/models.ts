import type { EntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { RequestDto, RequestStatus, RequestType } from '../requests';

export interface EmployeeListItemDto extends EntityDto<string> {
  userName: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
  departmentId?: string;
  departmentCode?: string;
  departmentName?: string;
  position: string;
  isManager: boolean;
  joinDate: string;
  rawJoinDate?: string;
  creationTime: string;
  tenureMonths: number;
  tenureFormatted: string;
  currentYearRequestCount: number;
}

export interface EmployeeProfileDto extends EmployeeListItemDto {
  currentYearStats: EmployeeYearlyStatsDto;
  recentRequests: RequestDto[];
}

export interface RequestTypeStatDto {
  type: RequestType;
  typeName: string;
  count: number;
  totalAmount: number;
}

export interface EmployeeYearlyStatsDto {
  year: number;
  totalRequests: number;
  approvedCount: number;
  submittedCount: number;
  draftCount: number;
  rejectedCount: number;
  totalAmountRequested: number;
  totalAmountDisbursed: number;
  leaveDaysTotal: number;
  breakdownByType: RequestTypeStatDto[];
}

export interface LeaveTimelineItemDto {
  requestId: string;
  title: string;
  startDate: string;
  endDate: string;
  days: number;
  reason: string;
  status: RequestStatus;
  statusName: string;
  creationTime: string;
  approverName?: string;
  approvalTime?: string;
  rejectionReason?: string;
}

export interface GetEmployeeListInput extends PagedAndSortedResultRequestDto {
  filter?: string;
  departmentId?: string;
  minTenureMonths?: number;
  maxTenureMonths?: number;
}

export interface GetEmployeeRequestsInput extends PagedAndSortedResultRequestDto {
  status?: RequestStatus;
  type?: RequestType;
  year?: number;
}

export interface UpdateEmployeeJoinDateDto {
  joinDate: string;
}

export interface CompanyHrSummaryDto {
  totalEmployees: number;
  totalDepartments: number;
  companyAverageTenureMonths: number;
  companyAverageTenureFormatted: string;
  totalRequestsThisYear: number;
  totalDisbursedAmountThisYear: number;
}

export interface DepartmentHeadcountDto {
  departmentId: string;
  departmentCode: string;
  departmentName: string;
  headcount: number;
  percentage: number;
}

export interface DepartmentTenureDto {
  departmentId: string;
  departmentCode: string;
  departmentName: string;
  averageTenureMonths: number;
  averageTenureFormatted: string;
  minTenureMonths: number;
  maxTenureMonths: number;
}

export interface DepartmentRequestFrequencyDto {
  departmentId: string;
  departmentCode: string;
  departmentName: string;
  totalRequests: number;
  requestsPerEmployee: number;
}

export interface TopRequesterDto {
  userId: string;
  fullName: string;
  departmentName: string;
  requestCount: number;
}

export interface HrAnalyticsDto {
  year: number;
  companySummary: CompanyHrSummaryDto;
  headcountByDepartment: DepartmentHeadcountDto[];
  tenureByDepartment: DepartmentTenureDto[];
  requestFrequencyByDepartment: DepartmentRequestFrequencyDto[];
  topRequesters: TopRequesterDto[];
}

export interface GetLeaveReportInput extends PagedAndSortedResultRequestDto {
  year?: number;
  departmentId?: string;
  filter?: string;
}

export interface EmployeeLeaveReportItemDto extends EntityDto<string> {
  userName?: string;
  fullName?: string;
  email?: string;
  departmentId?: string;
  departmentCode?: string;
  departmentName?: string;
  position?: string;
  totalApprovedLeaveDays?: number;
  totalSentLeaveRequests?: number;
  totalApprovedLeaveRequests?: number;
  latestLeaveStartDate?: string;
  latestLeaveEndDate?: string;
  latestLeaveDays?: number;
  latestLeavePeriodFormatted?: string;
}


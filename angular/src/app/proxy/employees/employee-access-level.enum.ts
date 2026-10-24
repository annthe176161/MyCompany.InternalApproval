import { mapEnumToOptions } from '@abp/ng.core';

// Cấp độ quyền truy cập dữ liệu nhân sự của người dùng hiện tại
export enum EmployeeAccessLevel {
  Self = 0,
  DepartmentManager = 1,
  Admin = 2,
}

export const employeeAccessLevelOptions = mapEnumToOptions(EmployeeAccessLevel);


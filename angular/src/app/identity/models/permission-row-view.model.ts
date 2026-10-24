// Mô hình dòng hiển thị quyền hạn trong modal phân quyền
export interface PermissionRowView {
  name: string;
  displayName: string;
  parentName?: string;
  isGranted: boolean;
  isDisabled: boolean;
  grantedByRoles: string[];
  level: number;
}


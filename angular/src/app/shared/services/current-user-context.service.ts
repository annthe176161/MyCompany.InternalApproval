import { Injectable } from '@angular/core';
import { ConfigStateService, PermissionService } from '@abp/ng.core';

// Dịch vụ trung tâm quản lý thông tin phiên đăng nhập và kiểm tra phân quyền người dùng hiện tại
@Injectable({
  providedIn: 'root',
})
export class CurrentUserContextService {
  constructor(
    private configState: ConfigStateService,
    private permissionService: PermissionService,
  ) {}

  // Lấy đối tượng thông tin người dùng hiện tại từ trạng thái cấu hình ABP
  get currentUser(): any {
    return this.configState.getOne('currentUser');
  }

  // Lấy định danh (ID) của người dùng đang đăng nhập
  get userId(): string | null {
    return this.currentUser?.id || null;
  }

  // Lấy tên tài khoản đăng nhập (UserName)
  get userName(): string {
    return this.currentUser?.userName || '';
  }

  // Lấy địa chỉ email của người dùng hiện tại
  get email(): string {
    return this.currentUser?.email || '';
  }

  // Lấy danh sách vai trò của người dùng hiện tại
  get roles(): string[] {
    return this.currentUser?.roles || [];
  }

  // Lấy họ và tên đầy đủ của người dùng đang đăng nhập
  get fullName(): string {
    const user = this.currentUser;
    if (!user) return 'Người dùng';
    return user.name ? `${user.name} ${user.surName || ''}`.trim() : user.userName || 'Người dùng';
  }

  // Kiểm tra người dùng hiện tại có phải là Quản trị viên hệ thống (Admin) hay không
  isAdmin(additionalPolicies: string[] = []): boolean {
    const user = this.currentUser;
    const roleLower = this.roles.map(r => r.toLowerCase());
    const hasExtraPolicy = additionalPolicies.some(p => this.permissionService.getGrantedPolicy(p));

    return (
      roleLower.includes('admin') ||
      user?.userName?.toLowerCase() === 'admin' ||
      this.permissionService.getGrantedPolicy('AbpIdentity.Roles') ||
      hasExtraPolicy
    );
  }

  // Kiểm tra người dùng hiện tại có vai trò Người phê duyệt / Quản lý (Approver / Manager) hay không
  isApprover(): boolean {
    if (this.isAdmin()) return false;
    const roleLower = this.roles.map(r => r.toLowerCase());
    return (
      this.permissionService.getGrantedPolicy('Requests.Approve') ||
      roleLower.some(r => r.includes('manager') || r.includes('leader') || r.includes('approver'))
    );
  }
}


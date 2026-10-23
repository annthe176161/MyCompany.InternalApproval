import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import {
  IdentityRoleCreateDto,
  IdentityRoleDto,
  IdentityRoleService,
  IdentityRoleUpdateDto,
} from '@abp/ng.identity/proxy';
import { PermissionService } from '@abp/ng.core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { finalize } from 'rxjs/operators';

@Component({
  selector: 'app-roles',
  standalone: false,
  templateUrl: './roles.component.html',
  styleUrls: ['./roles.component.scss'],
})
export class RolesComponent implements OnInit {
  roles: IdentityRoleDto[] = [];
  totalCount = 0;
  isLoading = false;

  // Tham số phân trang & tìm kiếm
  filterText = '';
  pageIndex = 1;
  pageSize = 10;

  // Quyền hạn của người dùng hiện tại trên trang Vai trò
  canCreate = false;
  canEdit = false;
  canDelete = false;
  canManagePermissions = false;

  // Trạng thái Modal Thêm / Sửa Vai trò
  isRoleModalVisible = false;
  isEditMode = false;
  isSubmitting = false;
  selectedRole: IdentityRoleDto | null = null;
  roleForm!: FormGroup;

  // Trạng thái Modal Phân quyền
  isPermModalVisible = false;
  permTargetKey = '';
  permTargetTitle = '';

  constructor(
    private roleService: IdentityRoleService,
    private permissionService: PermissionService,
    private fb: FormBuilder,
    private message: NzMessageService
  ) {
    this.initForm();
  }

  ngOnInit(): void {
    this.checkPermissions();
    this.loadRoles();
  }

  // Kiểm tra quyền thao tác của tài khoản đang đăng nhập
  private checkPermissions(): void {
    this.canCreate = this.permissionService.getGrantedPolicy('AbpIdentity.Roles.Create');
    this.canEdit = this.permissionService.getGrantedPolicy('AbpIdentity.Roles.Update');
    this.canDelete = this.permissionService.getGrantedPolicy('AbpIdentity.Roles.Delete');
    this.canManagePermissions = this.permissionService.getGrantedPolicy(
      'AbpIdentity.Roles.ManagePermissions'
    );
  }

  // Khởi tạo Form tạo/sửa vai trò
  private initForm(): void {
    this.roleForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(256)]],
      isDefault: [false],
      isPublic: [true],
    });
  }

  // Tải danh sách vai trò từ API
  loadRoles(): void {
    this.isLoading = true;
    this.roleService
      .getList({
        filter: this.filterText.trim() || undefined,
        skipCount: (this.pageIndex - 1) * this.pageSize,
        maxResultCount: this.pageSize,
        sorting: 'name asc',
      })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: res => {
          this.roles = res.items || [];
          this.totalCount = res.totalCount || 0;
        },
        error: () => {
          this.message.error('Không thể tải danh sách vai trò. Vui lòng thử lại!');
        },
      });
  }

  // Tìm kiếm vai trò
  onSearch(): void {
    this.pageIndex = 1;
    this.loadRoles();
  }

  onPageIndexChange(page: number): void {
    this.pageIndex = page;
    this.loadRoles();
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.pageIndex = 1;
    this.loadRoles();
  }

  // Giải thích ý nghĩa từng vai trò để bảng danh sách trực quan, dễ hiểu
  getRoleSummaryDescription(role: IdentityRoleDto): string {
    const name = (role.name || '').toLowerCase();
    if (role.isStatic || name === 'admin') {
      return 'Vai trò Quản trị viên tối cao mặc định của hệ thống. Không thể xóa hoặc đổi tên để đảm bảo an toàn.';
    }
    if (name.includes('manager') || name.includes('trưởng phòng') || name.includes('quản lý')) {
      return 'Dành cho cấp quản lý/trưởng bộ phận có quyền xem và phê duyệt các đề xuất nội bộ.';
    }
    if (name.includes('employee') || name.includes('nhân viên') || name.includes('staff')) {
      return 'Dành cho nhân viên tạo phiếu đề xuất và theo dõi tiến độ phê duyệt cá nhân.';
    }
    if (role.isDefault) {
      return 'Vai trò tự động gán cho nhân viên mới khi tạo tài khoản trên hệ thống.';
    }
    return 'Vai trò tùy chỉnh dùng để phân nhóm quyền hạn theo nghiệp vụ công ty.';
  }

  // Mở popup Thêm vai trò mới
  openCreateModal(): void {
    this.isEditMode = false;
    this.selectedRole = null;
    this.roleForm.reset({
      name: '',
      isDefault: false,
      isPublic: true,
    });
    this.roleForm.get('name')?.enable();
    this.isRoleModalVisible = true;
  }

  // Mở popup Chỉnh sửa vai trò
  openEditModal(role: IdentityRoleDto): void {
    this.isEditMode = true;
    this.selectedRole = role;
    this.roleForm.reset({
      name: role.name || '',
      isDefault: !!role.isDefault,
      isPublic: !!role.isPublic,
    });

    // Nếu là vai trò tĩnh cốt lõi (như admin) -> Khóa ô nhập tên và hiển thị giải thích rõ ràng trên giao diện
    if (role.isStatic) {
      this.roleForm.get('name')?.disable();
    } else {
      this.roleForm.get('name')?.enable();
    }

    this.isRoleModalVisible = true;
  }

  closeRoleModal(): void {
    this.isRoleModalVisible = false;
    this.selectedRole = null;
  }

  // Đảo trạng thái khi người dùng bấm vào thẻ tùy chọn Mặc định / Công khai
  toggleOptionCard(controlName: 'isDefault' | 'isPublic'): void {
    const ctrl = this.roleForm.get(controlName);
    if (ctrl) {
      ctrl.setValue(!ctrl.value);
    }
  }

  // Lưu thông tin Vai trò (Tạo mới hoặc Cập nhật)
  saveRole(): void {
    if (this.roleForm.invalid) {
      Object.values(this.roleForm.controls).forEach(control => {
        control.markAsDirty();
        control.updateValueAndValidity({ onlySelf: true });
      });
      this.message.warning('Vui lòng nhập tên vai trò hợp lệ!');
      return;
    }

    const rawValues = this.roleForm.getRawValue();
    const roleName = (rawValues.name || '').trim();
    if (!roleName) {
      this.message.warning('Tên vai trò không được để trống!');
      return;
    }

    this.isSubmitting = true;

    if (this.isEditMode && this.selectedRole?.id) {
      const updateInput: IdentityRoleUpdateDto = {
        name: roleName,
        isDefault: !!rawValues.isDefault,
        isPublic: !!rawValues.isPublic,
        concurrencyStamp: this.selectedRole.concurrencyStamp,
      };

      this.roleService
        .update(this.selectedRole.id, updateInput)
        .pipe(finalize(() => (this.isSubmitting = false)))
        .subscribe({
          next: () => {
            this.message.success(`Đã cập nhật vai trò "${roleName}" thành công!`);
            this.closeRoleModal();
            this.loadRoles();
          },
          error: err => {
            const msg =
              err?.error?.error?.message ||
              err?.error?.message ||
              'Không thể cập nhật vai trò. Có thể tên vai trò đã tồn tại!';
            this.message.error(msg);
          },
        });
    } else {
      const createInput: IdentityRoleCreateDto = {
        name: roleName,
        isDefault: !!rawValues.isDefault,
        isPublic: !!rawValues.isPublic,
      };

      this.roleService
        .create(createInput)
        .pipe(finalize(() => (this.isSubmitting = false)))
        .subscribe({
          next: created => {
            this.message.success(
              `Đã tạo vai trò "${roleName}" thành công! Bạn có thể bấm "Phân quyền" để cấp quyền cho vai trò này.`
            );
            this.closeRoleModal();
            this.loadRoles();
            // Mở luôn cửa sổ phân quyền nếu người dùng có quyền cấu hình
            if (this.canManagePermissions && created?.name) {
              this.openPermissionsModal(created);
            }
          },
          error: err => {
            const msg =
              err?.error?.error?.message ||
              err?.error?.message ||
              'Không thể tạo vai trò mới. Có thể tên vai trò này đã trùng!';
            this.message.error(msg);
          },
        });
    }
  }

  // Xóa vai trò
  deleteRole(role: IdentityRoleDto): void {
    if (!role.id) return;
    if (role.isStatic) {
      this.message.warning('Đây là vai trò cốt lõi của hệ thống, không thể xóa!');
      return;
    }

    this.isLoading = true;
    this.roleService
      .delete(role.id)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: () => {
          this.message.success(`Đã xóa vai trò "${role.name}" khỏi hệ thống!`);
          this.loadRoles();
        },
        error: () => {
          this.message.error('Không thể xóa vai trò này. Vui lòng thử lại!');
        },
      });
  }

  // Mở popup Phân quyền chi tiết cho Vai trò
  openPermissionsModal(role: IdentityRoleDto): void {
    if (!role.name) return;
    this.permTargetKey = role.name;
    this.permTargetTitle = role.name;
    this.isPermModalVisible = true;
  }
}


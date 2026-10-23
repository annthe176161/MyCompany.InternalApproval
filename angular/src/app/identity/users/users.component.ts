import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import {
  IdentityRoleDto,
  IdentityUserCreateDto,
  IdentityUserDto,
  IdentityUserService,
  IdentityUserUpdateDto,
} from '@abp/ng.identity/proxy';
import { PermissionService } from '@abp/ng.core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { finalize } from 'rxjs/operators';

@Component({
  selector: 'app-users',
  standalone: false,
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.scss'],
})
export class UsersComponent implements OnInit {
  users: IdentityUserDto[] = [];
  totalCount = 0;
  isLoading = false;

  // Tìm kiếm & phân trang
  filterText = '';
  pageIndex = 1;
  pageSize = 10;

  // Quyền thao tác của người dùng đang đăng nhập
  canCreate = false;
  canEdit = false;
  canDelete = false;
  canManagePermissions = false;
  canManageRoles = false;

  // Danh sách các Vai trò có thể gán trong hệ thống
  assignableRoles: IdentityRoleDto[] = [];
  selectedRoleNames: Set<string> = new Set<string>();

  // Trạng thái Modal Thêm / Sửa Tài khoản
  isUserModalVisible = false;
  isEditMode = false;
  isSubmitting = false;
  activeTabIndex = 0;
  passwordVisible = false;
  selectedUser: IdentityUserDto | null = null;
  userForm!: FormGroup;

  // Trạng thái Modal Phân quyền cá nhân
  isPermModalVisible = false;
  permTargetKey = '';
  permTargetTitle = '';

  constructor(
    private userService: IdentityUserService,
    private permissionService: PermissionService,
    private fb: FormBuilder,
    private message: NzMessageService
  ) {
    this.initForm();
  }

  ngOnInit(): void {
    this.checkPermissions();
    this.loadUsers();
    this.loadAssignableRoles();
  }

  private checkPermissions(): void {
    this.canCreate = this.permissionService.getGrantedPolicy('AbpIdentity.Users.Create');
    this.canEdit = this.permissionService.getGrantedPolicy('AbpIdentity.Users.Update');
    this.canDelete = this.permissionService.getGrantedPolicy('AbpIdentity.Users.Delete');
    this.canManagePermissions = this.permissionService.getGrantedPolicy(
      'AbpIdentity.Users.ManagePermissions'
    );
    this.canManageRoles = this.permissionService.getGrantedPolicy(
      'AbpIdentity.Users.Update.ManageRoles'
    );
  }

  private initForm(): void {
    this.userForm = this.fb.group({
      userName: ['', [Validators.required, Validators.maxLength(256)]],
      password: [''],
      surname: ['', [Validators.maxLength(64)]],
      name: ['', [Validators.maxLength(64)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
      phoneNumber: ['', [Validators.maxLength(16)]],
      isActive: [true],
      lockoutEnabled: [true],
    });
  }

  // Tải danh sách tài khoản người dùng
  loadUsers(): void {
    this.isLoading = true;
    this.userService
      .getList({
        filter: this.filterText.trim() || undefined,
        skipCount: (this.pageIndex - 1) * this.pageSize,
        maxResultCount: this.pageSize,
        sorting: 'userName asc',
      })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: res => {
          this.users = res.items || [];
          this.totalCount = res.totalCount || 0;
        },
        error: () => {
          this.message.error('Không thể tải danh sách tài khoản người dùng!');
        },
      });
  }

  // Tải danh sách tất cả vai trò để gán cho tài khoản
  loadAssignableRoles(): void {
    this.userService.getAssignableRoles().subscribe({
      next: res => {
        this.assignableRoles = res.items || [];
      },
    });
  }

  onSearch(): void {
    this.pageIndex = 1;
    this.loadUsers();
  }

  onPageIndexChange(page: number): void {
    this.pageIndex = page;
    this.loadUsers();
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.pageIndex = 1;
    this.loadUsers();
  }

  // Ghép Họ và Tên đầy đủ của người dùng
  getFullName(user: IdentityUserDto): string {
    const parts = [user.surname, user.name].filter(Boolean);
    return parts.length > 0 ? parts.join(' ') : user.userName || 'Chưa cập nhật tên';
  }

  // Lấy chữ cái đầu làm Avatar
  getAvatarInitial(user: IdentityUserDto): string {
    const source = user.name || user.userName || 'U';
    return source.charAt(0).toUpperCase();
  }

  // Mở Modal Tạo tài khoản mới
  openCreateModal(): void {
    this.isEditMode = false;
    this.selectedUser = null;
    this.activeTabIndex = 0;
    this.passwordVisible = false;

    // Khi tạo mới thì bắt buộc nhập mật khẩu
    this.userForm.get('password')?.setValidators([Validators.required, Validators.minLength(6)]);
    this.userForm.get('password')?.updateValueAndValidity();

    this.userForm.reset({
      userName: '',
      password: '',
      surname: '',
      name: '',
      email: '',
      phoneNumber: '',
      isActive: true,
      lockoutEnabled: true,
    });

    // Tự động tích chọn các vai trò có cờ "Mặc định" (isDefault = true)
    this.selectedRoleNames.clear();
    this.assignableRoles
      .filter(r => r.isDefault && r.name)
      .forEach(r => this.selectedRoleNames.add(r.name as string));

    this.isUserModalVisible = true;
  }

  // Mở Modal Chỉnh sửa tài khoản (có thể mở thẳng vào tab Vai trò nếu truyền tabIndex = 1)
  openEditModal(user: IdentityUserDto, initialTab = 0): void {
    if (!user.id) return;
    this.isEditMode = true;
    this.selectedUser = user;
    this.activeTabIndex = initialTab;
    this.passwordVisible = false;

    // Khi chỉnh sửa thì mật khẩu là tùy chọn (để trống nếu không muốn đổi mật khẩu)
    this.userForm.get('password')?.clearValidators();
    this.userForm.get('password')?.updateValueAndValidity();

    this.userForm.reset({
      userName: user.userName || '',
      password: '',
      surname: user.surname || '',
      name: user.name || '',
      email: user.email || '',
      phoneNumber: user.phoneNumber || '',
      isActive: user.isActive !== false,
      lockoutEnabled: user.lockoutEnabled !== false,
    });

    // Tải các vai trò hiện tại của người dùng này
    this.selectedRoleNames.clear();
    this.userService.getRoles(user.id).subscribe({
      next: res => {
        (res.items || []).forEach(r => {
          if (r.name) this.selectedRoleNames.add(r.name);
        });
      },
    });

    this.isUserModalVisible = true;
  }

  closeUserModal(): void {
    this.isUserModalVisible = false;
    this.selectedUser = null;
  }

  // Kiểm tra 1 vai trò có đang được chọn cho user này không
  isRoleSelected(roleName?: string): boolean {
    return !!roleName && this.selectedRoleNames.has(roleName);
  }

  // Bật / Tắt chọn vai trò cho tài khoản
  toggleRoleSelection(roleName?: string): void {
    if (!roleName) return;
    if (this.selectedRoleNames.has(roleName)) {
      this.selectedRoleNames.delete(roleName);
    } else {
      this.selectedRoleNames.add(roleName);
    }
  }

  // Đảo trạng thái thẻ công tắc trong form User
  toggleUserOption(controlName: 'isActive' | 'lockoutEnabled'): void {
    const ctrl = this.userForm.get(controlName);
    if (ctrl) {
      ctrl.setValue(!ctrl.value);
    }
  }

  // Lưu thông tin Tài khoản (Tạo mới hoặc Cập nhật)
  saveUser(): void {
    if (this.userForm.invalid) {
      Object.values(this.userForm.controls).forEach(control => {
        control.markAsDirty();
        control.updateValueAndValidity({ onlySelf: true });
      });
      this.activeTabIndex = 0;
      this.message.warning('Vui lòng kiểm tra lại các thông tin bắt buộc!');
      return;
    }

    const raw = this.userForm.getRawValue();
    const roleNames = Array.from(this.selectedRoleNames);

    this.isSubmitting = true;

    if (this.isEditMode && this.selectedUser?.id) {
      const updateInput: IdentityUserUpdateDto = {
        userName: (raw.userName || '').trim(),
        name: (raw.name || '').trim(),
        surname: (raw.surname || '').trim(),
        email: (raw.email || '').trim(),
        phoneNumber: (raw.phoneNumber || '').trim(),
        isActive: !!raw.isActive,
        lockoutEnabled: !!raw.lockoutEnabled,
        roleNames,
        concurrencyStamp: this.selectedUser.concurrencyStamp,
      };

      // Chỉ gửi mật khẩu mới nếu người dùng có nhập vào ô Đổi mật khẩu
      if (raw.password && raw.password.trim().length > 0) {
        updateInput.password = raw.password.trim();
      }

      this.userService
        .update(this.selectedUser.id, updateInput)
        .pipe(finalize(() => (this.isSubmitting = false)))
        .subscribe({
          next: () => {
            this.message.success(
              `Đã cập nhật thông tin tài khoản "${updateInput.userName}" thành công!`
            );
            this.closeUserModal();
            this.loadUsers();
          },
          error: err => {
            const msg =
              err?.error?.error?.message ||
              err?.error?.message ||
              'Không thể cập nhật tài khoản. Vui lòng kiểm tra lại định dạng mật khẩu hoặc email!';
            this.message.error(msg);
          },
        });
    } else {
      const createInput: IdentityUserCreateDto = {
        userName: (raw.userName || '').trim(),
        password: (raw.password || '').trim(),
        name: (raw.name || '').trim(),
        surname: (raw.surname || '').trim(),
        email: (raw.email || '').trim(),
        phoneNumber: (raw.phoneNumber || '').trim(),
        isActive: !!raw.isActive,
        lockoutEnabled: !!raw.lockoutEnabled,
        roleNames,
      };

      this.userService
        .create(createInput)
        .pipe(finalize(() => (this.isSubmitting = false)))
        .subscribe({
          next: () => {
            this.message.success(
              `Đã tạo tài khoản "${createInput.userName}" thành công!`
            );
            this.closeUserModal();
            this.loadUsers();
          },
          error: err => {
            const msg =
              err?.error?.error?.message ||
              err?.error?.message ||
              'Không thể tạo tài khoản. Mật khẩu cần có chữ hoa, chữ thường, chữ số và ký tự đặc biệt!';
            this.message.error(msg);
          },
        });
    }
  }

  // Xóa tài khoản người dùng
  deleteUser(user: IdentityUserDto): void {
    if (!user.id) return;
    if (user.userName?.toLowerCase() === 'admin') {
      this.message.warning('Không thể xóa tài khoản Quản trị viên gốc (admin)!');
      return;
    }

    this.isLoading = true;
    this.userService
      .delete(user.id)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: () => {
          this.message.success(`Đã xóa tài khoản "${user.userName}" thành công!`);
          this.loadUsers();
        },
        error: () => {
          this.message.error('Không thể xóa tài khoản này. Vui lòng thử lại!');
        },
      });
  }

  // Mở popup Phân quyền riêng cho Tài khoản
  openPermissionsModal(user: IdentityUserDto): void {
    if (!user.id) return;
    this.permTargetKey = user.id;
    this.permTargetTitle = `${this.getFullName(user)} (@${user.userName})`;
    this.isPermModalVisible = true;
  }
}


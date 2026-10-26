import { Component, OnInit, ViewChild } from '@angular/core';
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
import { PermissionModalComponent } from '../components/permission-modal/permission-modal.component';
import { extractErrorMessage } from '../../shared/utils';

// Màn hình quản lý danh sách tài khoản người dùng và gán vai trò hệ thống
@Component({
  selector: 'app-users',
  standalone: false,
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.scss'],
})
export class UsersComponent implements OnInit {
  @ViewChild('permModal') permModal!: PermissionModalComponent;

  users: IdentityUserDto[] = [];
  totalCount = 0;
  isLoading = false;

  filterText = '';
  pageIndex = 1;
  pageSize = 10;

  canCreate = false;
  canEdit = false;
  canDelete = false;
  canManagePermissions = false;
  canManageRoles = false;

  assignableRoles: IdentityRoleDto[] = [];
  selectedRoleNames: Set<string> = new Set<string>();

  isUserModalVisible = false;
  isEditMode = false;
  isSubmitting = false;
  activeTabIndex = 0;
  selectedUser: IdentityUserDto | null = null;
  userForm!: FormGroup;

  readonly defaultPassword = 'Company@2026';
  passwordVisible = false;

  constructor(
    private userService: IdentityUserService,
    private permissionService: PermissionService,
    private fb: FormBuilder,
    private message: NzMessageService,
  ) {
    this.userForm = this.fb.group({
      userName: ['', [Validators.required, Validators.maxLength(256)]],
      password: [this.defaultPassword],
      surname: ['', [Validators.maxLength(64)]],
      name: ['', [Validators.maxLength(64)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
      phoneNumber: ['', [Validators.maxLength(16)]],
      isActive: [true],
      lockoutEnabled: [false],
    });
  }

  ngOnInit(): void {
    this.canCreate = this.permissionService.getGrantedPolicy('AbpIdentity.Users.Create');
    this.canEdit = this.permissionService.getGrantedPolicy('AbpIdentity.Users.Update');
    this.canDelete = this.permissionService.getGrantedPolicy('AbpIdentity.Users.Delete');
    this.canManagePermissions = this.permissionService.getGrantedPolicy(
      'AbpIdentity.Users.ManagePermissions',
    );
    this.canManageRoles = this.permissionService.getGrantedPolicy(
      'AbpIdentity.Users.Update.ManageRoles',
    );

    this.loadUsers();
    this.loadAssignableRoles();
  }

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
          this.message.error('Không thể tải danh sách tài khoản!');
        },
      });
  }

  loadAssignableRoles(): void {
    this.userService.getAssignableRoles().subscribe({
      next: res => {
        this.assignableRoles = res.items || [];
        // Nếu modal tạo mới đang mở và chưa chọn vai trò nào, tự động chọn Employee
        if (this.isUserModalVisible && !this.isEditMode && this.selectedRoleNames.size === 0) {
          this.selectedRoleNames.add('Employee');
          this.assignableRoles
            .filter(r => r.isDefault && r.name)
            .forEach(r => this.selectedRoleNames.add(r.name as string));
        }
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

  getFullName(user: IdentityUserDto): string {
    const parts = [user.surname, user.name].filter(Boolean);
    return parts.length > 0 ? parts.join(' ') : '—';
  }

  openCreateModal(): void {
    this.isEditMode = false;
    this.selectedUser = null;
    this.activeTabIndex = 0;
    this.passwordVisible = false;

    this.userForm.get('password')?.setValidators([Validators.required, Validators.minLength(6)]);
    this.userForm.get('password')?.updateValueAndValidity();

    this.userForm.reset({
      userName: '',
      password: this.defaultPassword,
      surname: '',
      name: '',
      email: '',
      phoneNumber: '',
      isActive: true,
      lockoutEnabled: false,
    });

    this.selectedRoleNames.clear();
    // Luôn chọn sẵn vai trò Employee làm mặc định cho tài khoản mới
    this.selectedRoleNames.add('Employee');
    this.assignableRoles
      .filter(r => r.isDefault && r.name)
      .forEach(r => this.selectedRoleNames.add(r.name as string));

    this.isUserModalVisible = true;
  }

  openEditModal(user: IdentityUserDto): void {
    if (!user.id) return;
    this.isEditMode = true;
    this.selectedUser = user;
    this.activeTabIndex = 0;
    this.passwordVisible = false;

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
      lockoutEnabled: false,
    });

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

  isRoleSelected(roleName?: string): boolean {
    return !!roleName && this.selectedRoleNames.has(roleName);
  }

  toggleRoleSelection(roleName?: string): void {
    if (!roleName) return;
    if (this.selectedRoleNames.has(roleName)) {
      this.selectedRoleNames.delete(roleName);
    } else {
      this.selectedRoleNames.add(roleName);
    }
  }

  saveUser(): void {
    if (this.userForm.invalid) {
      Object.values(this.userForm.controls).forEach(control => {
        control.markAsDirty();
        control.updateValueAndValidity({ onlySelf: true });
      });
      this.activeTabIndex = 0;
      return;
    }

    const raw = this.userForm.getRawValue();
    let roleNames = Array.from(this.selectedRoleNames);
    if (!this.isEditMode && roleNames.length === 0) {
      // Đảm bảo tài khoản mới luôn được cấp tối thiểu vai trò Employee
      roleNames = ['Employee'];
    }
    this.isSubmitting = true;

    if (this.isEditMode && this.selectedUser?.id) {
      const updateInput: IdentityUserUpdateDto = {
        userName: (raw.userName || '').trim(),
        name: (raw.name || '').trim(),
        surname: (raw.surname || '').trim(),
        email: (raw.email || '').trim(),
        phoneNumber: (raw.phoneNumber || '').trim(),
        isActive: !!raw.isActive,
        lockoutEnabled: false,
        roleNames,
        concurrencyStamp: this.selectedUser.concurrencyStamp,
      };

      if (raw.password && raw.password.trim().length > 0) {
        updateInput.password = raw.password.trim();
      }

      this.userService
        .update(this.selectedUser.id, updateInput)
        .pipe(finalize(() => (this.isSubmitting = false)))
        .subscribe({
          next: () => {
            this.message.success('Đã cập nhật tài khoản thành công!');
            this.closeUserModal();
            this.loadUsers();
          },
          error: err => {
            this.message.error(extractErrorMessage(err, 'Không thể cập nhật tài khoản!'));
          },
        });
    } else {
      const createInput: IdentityUserCreateDto = {
        userName: (raw.userName || '').trim(),
        password: (raw.password || '').trim() || this.defaultPassword,
        name: (raw.name || '').trim(),
        surname: (raw.surname || '').trim(),
        email: (raw.email || '').trim(),
        phoneNumber: (raw.phoneNumber || '').trim(),
        isActive: !!raw.isActive,
        lockoutEnabled: false,
        roleNames,
      };

      this.userService
        .create(createInput)
        .pipe(finalize(() => (this.isSubmitting = false)))
        .subscribe({
          next: () => {
            this.message.success('Đã thêm tài khoản mới thành công!');
            this.closeUserModal();
            this.loadUsers();
          },
          error: err => {
            this.message.error(extractErrorMessage(err, 'Không thể tạo tài khoản!'));
          },
        });
    }
  }

  deleteUser(user: IdentityUserDto): void {
    if (!user.id || user.userName?.toLowerCase() === 'admin') return;

    this.isLoading = true;
    this.userService
      .delete(user.id)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: () => {
          this.message.success(`Đã xóa tài khoản "${user.userName}"!`);
          this.loadUsers();
        },
        error: () => {
          this.message.error('Không thể xóa tài khoản này!');
        },
      });
  }
}

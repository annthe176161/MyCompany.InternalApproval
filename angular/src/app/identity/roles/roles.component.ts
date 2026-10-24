import { Component, OnInit, ViewChild } from '@angular/core';
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
import { PermissionModalComponent } from '../components/permission-modal/permission-modal.component';
import { extractErrorMessage } from '../../shared/utils';

// Màn hình quản lý danh sách vai trò và cấu hình quyền hạn hệ thống
@Component({
  selector: 'app-roles',
  standalone: false,
  templateUrl: './roles.component.html',
  styleUrls: ['./roles.component.scss'],
})
export class RolesComponent implements OnInit {
  @ViewChild('permModal') permModal!: PermissionModalComponent;

  roles: IdentityRoleDto[] = [];
  totalCount = 0;
  isLoading = false;

  filterText = '';
  pageIndex = 1;
  pageSize = 10;

  canCreate = false;
  canEdit = false;
  canDelete = false;
  canManagePermissions = false;

  isRoleModalVisible = false;
  isEditMode = false;
  isSubmitting = false;
  selectedRole: IdentityRoleDto | null = null;
  roleForm!: FormGroup;

  constructor(
    private roleService: IdentityRoleService,
    private permissionService: PermissionService,
    private fb: FormBuilder,
    private message: NzMessageService,
  ) {
    this.roleForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(256)]],
      isDefault: [false],
      isPublic: [true],
    });
  }

  ngOnInit(): void {
    this.canCreate = this.permissionService.getGrantedPolicy('AbpIdentity.Roles.Create');
    this.canEdit = this.permissionService.getGrantedPolicy('AbpIdentity.Roles.Update');
    this.canDelete = this.permissionService.getGrantedPolicy('AbpIdentity.Roles.Delete');
    this.canManagePermissions = this.permissionService.getGrantedPolicy(
      'AbpIdentity.Roles.ManagePermissions',
    );
    this.loadRoles();
  }

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
          this.message.error('Không thể tải danh sách vai trò!');
        },
      });
  }

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

  openEditModal(role: IdentityRoleDto): void {
    this.isEditMode = true;
    this.selectedRole = role;
    this.roleForm.reset({
      name: role.name || '',
      isDefault: !!role.isDefault,
      isPublic: !!role.isPublic,
    });

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

  saveRole(): void {
    if (this.roleForm.invalid) {
      Object.values(this.roleForm.controls).forEach(control => {
        control.markAsDirty();
        control.updateValueAndValidity({ onlySelf: true });
      });
      return;
    }

    const rawValues = this.roleForm.getRawValue();
    const roleName = (rawValues.name || '').trim();
    if (!roleName) {
      this.message.warning('Vui lòng nhập tên vai trò!');
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
            this.message.success('Đã cập nhật vai trò thành công!');
            this.closeRoleModal();
            this.loadRoles();
          },
          error: err => {
            this.message.error(extractErrorMessage(err, 'Không thể cập nhật vai trò!'));
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
          next: () => {
            this.message.success('Đã thêm vai trò mới thành công!');
            this.closeRoleModal();
            this.loadRoles();
          },
          error: err => {
            this.message.error(extractErrorMessage(err, 'Không thể thêm vai trò mới!'));
          },
        });
    }
  }

  deleteRole(role: IdentityRoleDto): void {
    if (!role.id || role.isStatic) return;

    this.isLoading = true;
    this.roleService
      .delete(role.id)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: () => {
          this.message.success(`Đã xóa vai trò "${role.name}"!`);
          this.loadRoles();
        },
        error: () => {
          this.message.error('Không thể xóa vai trò này!');
        },
      });
  }
}

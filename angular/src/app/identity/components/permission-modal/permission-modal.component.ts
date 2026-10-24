import { ChangeDetectorRef, Component, EventEmitter, Output } from '@angular/core';
import { ConfigStateService } from '@abp/ng.core';
import {
  PermissionGrantInfoDto,
  PermissionGroupDto,
  PermissionsService,
  UpdatePermissionDto,
} from '@abp/ng.permission-management/proxy';
import { NzMessageService } from 'ng-zorro-antd/message';
import { finalize, switchMap } from 'rxjs/operators';
import { of } from 'rxjs';
import {
  PERMISSION_GROUP_NAME_OVERRIDES,
  PERMISSION_NAME_OVERRIDES,
} from '../../../shared/constants';
import { PermissionRowView } from '../../models';

export { PermissionRowView } from '../../models';

// Modal quản lý phân quyền chi tiết cho Vai trò (R) hoặc Người dùng (U)
@Component({
  selector: 'app-permission-modal',
  standalone: false,
  templateUrl: './permission-modal.component.html',
  styleUrls: ['./permission-modal.component.scss'],
})
export class PermissionModalComponent {
  @Output() saved = new EventEmitter<void>();

  visible = false;
  isLoading = false;
  isSaving = false;

  providerName: 'R' | 'U' = 'R';
  providerKey = '';
  entityTitle = '';

  searchKeyword = '';
  groups: PermissionGroupDto[] = [];
  selectedGroupIndex = 0;

  // Mảng phẳng lưu toàn bộ quyền để tính toán nhanh
  allPermissions: PermissionGrantInfoDto[] = [];
  // Bản chụp ban đầu để chỉ gửi những quyền thực sự thay đổi khi bấm Lưu
  private initialGrantMap = new Map<string, boolean>();
  // Mảng hiển thị của nhóm hiện tại
  displayedPermissions: PermissionRowView[] = [];

  private readonly groupNameOverrides = PERMISSION_GROUP_NAME_OVERRIDES;
  private readonly permissionNameOverrides = PERMISSION_NAME_OVERRIDES;

  constructor(
    private permissionsService: PermissionsService,
    private configState: ConfigStateService,
    private message: NzMessageService,
    private cdr: ChangeDetectorRef,
  ) {}

  // Mở modal phân quyền theo đối tượng Vai trò hoặc Người dùng
  open(providerName: 'R' | 'U', providerKey: string, title?: string): void {
    if (!providerKey) return;
    this.providerName = providerName;
    this.providerKey = providerKey;
    this.entityTitle = title || providerKey;
    this.searchKeyword = '';
    this.selectedGroupIndex = 0;
    this.groups = [];
    this.allPermissions = [];
    this.displayedPermissions = [];
    this.visible = true;
    this.loadPermissions();
  }

  close(): void {
    if (this.isSaving) return;
    this.visible = false;
  }

  loadPermissions(): void {
    this.isLoading = true;
    this.permissionsService
      .get(this.providerName, this.providerKey)
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.markForCheck();
        }),
      )
      .subscribe({
        next: res => {
          this.groups = (res.groups || []).filter(g => g.permissions && g.permissions.length > 0);
          this.allPermissions = [];
          this.initialGrantMap.clear();

          this.groups.forEach(g => {
            (g.permissions || []).forEach(p => {
              this.allPermissions.push(p);
              if (p.name) {
                this.initialGrantMap.set(p.name, !!p.isGranted);
              }
            });
          });

          if (!this.entityTitle && res.entityDisplayName) {
            this.entityTitle = res.entityDisplayName;
          }

          this.selectedGroupIndex = 0;
          this.rebuildDisplayedList();
        },
        error: () => {
          this.message.error('Không thể tải danh sách quyền hạn. Vui lòng thử lại!');
          this.visible = false;
        },
      });
  }

  selectGroup(index: number): void {
    this.selectedGroupIndex = index;
    this.rebuildDisplayedList();
  }

  onSearchChange(): void {
    this.rebuildDisplayedList();
  }

  getGroupDisplayName(group: PermissionGroupDto): string {
    if (group.name && this.groupNameOverrides[group.name]) {
      return this.groupNameOverrides[group.name];
    }
    return group.displayName || group.name || '';
  }

  getPermissionDisplayName(perm: PermissionGrantInfoDto): string {
    if (perm.name && this.permissionNameOverrides[perm.name]) {
      return this.permissionNameOverrides[perm.name];
    }
    return perm.displayName || perm.name || '';
  }

  getGrantedCountInGroup(group: PermissionGroupDto): number {
    return (group.permissions || []).filter(p => p.isGranted).length;
  }

  isGrantedByOtherProvider(perm: PermissionGrantInfoDto): boolean {
    if (this.providerName !== 'U' || !perm.grantedProviders?.length) {
      return false;
    }
    return perm.grantedProviders.some(p => p.providerName === 'R');
  }

  private getGrantedRoles(perm: PermissionGrantInfoDto): string[] {
    if (this.providerName !== 'U' || !perm.grantedProviders?.length) return [];
    return perm.grantedProviders
      .filter(p => p.providerName === 'R' && p.providerKey)
      .map(p => p.providerKey as string);
  }

  // Tính mức thụt lề theo phân cấp cha - con của quyền hạn
  private calculateLevel(
    perm: PermissionGrantInfoDto,
    groupPerms: PermissionGrantInfoDto[],
  ): number {
    let level = 0;
    let currentParentName = perm.parentName;
    while (currentParentName && level < 3) {
      level++;
      const parent = groupPerms.find(p => p.name === currentParentName);
      currentParentName = parent?.parentName;
    }
    return level;
  }

  // Cập nhật mảng hiển thị quyền hạn khi đổi nhóm hoặc tìm kiếm
  rebuildDisplayedList(): void {
    const keyword = this.searchKeyword.trim().toLowerCase();

    if (keyword) {
      this.displayedPermissions = this.allPermissions
        .filter(p => {
          const label = this.getPermissionDisplayName(p).toLowerCase();
          const code = (p.name || '').toLowerCase();
          return label.includes(keyword) || code.includes(keyword);
        })
        .map(p => ({
          name: p.name || '',
          displayName: this.getPermissionDisplayName(p),
          parentName: p.parentName,
          isGranted: !!p.isGranted,
          isDisabled: this.isGrantedByOtherProvider(p),
          grantedByRoles: this.getGrantedRoles(p),
          level: 0,
        }));
      return;
    }

    const currentGroup = this.groups[this.selectedGroupIndex];
    const perms = currentGroup?.permissions || [];

    this.displayedPermissions = perms.map(p => ({
      name: p.name || '',
      displayName: this.getPermissionDisplayName(p),
      parentName: p.parentName,
      isGranted: !!p.isGranted,
      isDisabled: this.isGrantedByOtherProvider(p),
      grantedByRoles: this.getGrantedRoles(p),
      level: this.calculateLevel(p, perms),
    }));
  }

  // Kiểm tra trạng thái cấp toàn bộ quyền hệ thống
  get isAllGranted(): boolean {
    return this.allPermissions.length > 0 && this.allPermissions.every(p => p.isGranted);
  }

  get isAllIndeterminate(): boolean {
    const granted = this.allPermissions.filter(p => p.isGranted).length;
    return granted > 0 && granted < this.allPermissions.length;
  }

  toggleAllPermissions(checked: boolean): void {
    this.allPermissions.forEach(p => {
      if (!this.isGrantedByOtherProvider(p)) {
        p.isGranted = checked;
      } else {
        p.isGranted = true;
      }
    });
    this.rebuildDisplayedList();
  }

  // Kiểm tra trạng thái cấp toàn bộ quyền trong nhóm đang chọn
  get isCurrentGroupAllGranted(): boolean {
    const perms = this.groups[this.selectedGroupIndex]?.permissions || [];
    return perms.length > 0 && perms.every(p => p.isGranted);
  }

  get isCurrentGroupIndeterminate(): boolean {
    const perms = this.groups[this.selectedGroupIndex]?.permissions || [];
    const granted = perms.filter(p => p.isGranted).length;
    return granted > 0 && granted < perms.length;
  }

  toggleCurrentGroupPermissions(checked: boolean): void {
    const perms = this.groups[this.selectedGroupIndex]?.permissions || [];
    perms.forEach(p => {
      if (!this.isGrantedByOtherProvider(p)) {
        p.isGranted = checked;
      } else {
        p.isGranted = true;
      }
    });
    this.rebuildDisplayedList();
  }

  // Bật hoặc tắt từng quyền đơn lẻ kèm đồng bộ quyền cha - con
  onTogglePermission(row: PermissionRowView, checked: boolean): void {
    if (row.isDisabled) return;

    const target = this.allPermissions.find(p => p.name === row.name);
    if (!target) return;

    target.isGranted = checked;

    if (checked) {
      this.grantParentChain(target.parentName);
    } else {
      this.revokeChildrenChain(target.name);
    }

    this.rebuildDisplayedList();
  }

  private grantParentChain(parentName?: string): void {
    if (!parentName) return;
    const parent = this.allPermissions.find(p => p.name === parentName);
    if (parent) {
      parent.isGranted = true;
      this.grantParentChain(parent.parentName);
    }
  }

  private revokeChildrenChain(parentName?: string): void {
    if (!parentName) return;
    this.allPermissions
      .filter(p => p.parentName === parentName)
      .forEach(child => {
        if (!this.isGrantedByOtherProvider(child)) {
          child.isGranted = false;
        }
        this.revokeChildrenChain(child.name);
      });
  }

  private shouldRefreshAppConfig(): boolean {
    const currentUser = this.configState.getOne('currentUser');
    if (!currentUser) return false;
    if (this.providerName === 'R') {
      return (currentUser.roles || []).includes(this.providerKey);
    }
    if (this.providerName === 'U') {
      return currentUser.id === this.providerKey;
    }
    return false;
  }

  save(): void {
    if (this.isSaving) return;

    // Chỉ lọc ra những quyền thực sự thay đổi so với trạng thái ban đầu
    const changedPermissions: UpdatePermissionDto[] = this.allPermissions
      .filter(p => p.name && this.initialGrantMap.get(p.name) !== !!p.isGranted)
      .map(p => ({
        name: p.name,
        isGranted: !!p.isGranted,
      }));

    if (changedPermissions.length === 0) {
      this.message.info('Không có thay đổi nào về quyền hạn.');
      this.visible = false;
      return;
    }

    this.isSaving = true;
    this.permissionsService
      .update(this.providerName, this.providerKey, { permissions: changedPermissions })
      .pipe(
        switchMap(() =>
          this.shouldRefreshAppConfig() ? this.configState.refreshAppState() : of(null),
        ),
        finalize(() => {
          this.isSaving = false;
          this.cdr.markForCheck();
        }),
      )
      .subscribe({
        next: () => {
          this.message.success(`Đã lưu phân quyền cho "${this.entityTitle}" thành công!`);
          this.visible = false;
          this.saved.emit();
        },
        error: () => {
          this.message.error('Không thể lưu phân quyền. Vui lòng thử lại!');
        },
      });
  }
}

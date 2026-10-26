import { Component, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { finalize } from 'rxjs/operators';
import { PermissionService } from '@abp/ng.core';
import { DepartmentService } from '../proxy/departments/department.service';
import {
  DepartmentDto,
  DepartmentMemberDto,
  DepartmentUserLookupDto,
} from '../proxy/departments/models';
import { EmployeeDetailDrawerComponent } from '../shared/components/employee-detail-drawer/employee-detail-drawer.component';
import { COMMON_DEPARTMENT_POSITIONS } from '../shared/constants';
import { extractErrorMessage } from '../shared/utils';

// Màn hình quản lý danh mục phòng ban và phân bổ nhân sự, chức vụ
@Component({
  selector: 'app-department',
  templateUrl: './department.component.html',
  styleUrls: ['./department.component.scss'],
})
export class DepartmentComponent implements OnInit {
  @ViewChild('employeeDrawer') employeeDrawer?: EmployeeDetailDrawerComponent;

  departments: DepartmentDto[] = [];
  filteredDepartments: DepartmentDto[] = [];
  loading = false;
  searchKeyword = '';

  // Quyền thao tác trên phòng ban
  canCreate = false;
  canEdit = false;
  canDelete = false;
  canManageMembers = false;

  // Trạng thái modal thêm / sửa phòng ban
  isModalVisible = false;
  isModalSubmitting = false;
  selectedDepartment: DepartmentDto | null = null;
  deptForm!: FormGroup;

  // Trạng thái ngăn kéo quản lý nhân sự và chức vụ phòng ban
  isDrawerVisible = false;
  isMembersLoading = false;
  isAddingMember = false;
  activeDepartment: DepartmentDto | null = null;
  members: DepartmentMemberDto[] = [];
  availableUsers: DepartmentUserLookupDto[] = [];
  addMemberForm!: FormGroup;
  editingMemberId: string | null = null;
  editingPosition = '';

  // Danh sách chức vụ gợi ý dùng chung
  readonly commonPositions = COMMON_DEPARTMENT_POSITIONS;

  constructor(
    private departmentService: DepartmentService,
    private fb: FormBuilder,
    private message: NzMessageService,
    private permissionService: PermissionService,
  ) {}

  ngOnInit(): void {
    this.checkPermissions();
    this.initForms();
    this.loadDepartments();
  }

  checkPermissions(): void {
    this.canCreate = this.permissionService.getGrantedPolicy('Departments.Create');
    this.canEdit = this.permissionService.getGrantedPolicy('Departments.Edit');
    this.canDelete = this.permissionService.getGrantedPolicy('Departments.Delete');
    this.canManageMembers = this.permissionService.getGrantedPolicy('Departments.ManageMembers');
  }

  initForms(): void {
    this.deptForm = this.fb.group({
      code: ['', [Validators.required, Validators.maxLength(50)]],
      name: ['', [Validators.required, Validators.maxLength(128)]],
      description: ['', [Validators.maxLength(500)]],
      managerId: [null],
    });

    this.addMemberForm = this.fb.group({
      userId: [null, [Validators.required]],
      position: ['', [Validators.required, Validators.maxLength(128)]],
    });
  }

  loadDepartments(): void {
    this.loading = true;
    this.departmentService.getList({ skipCount: 0, maxResultCount: 100 }).subscribe({
      next: res => {
        this.departments = res.items || [];
        this.filterDepartments();
        this.loading = false;
      },
      error: err => {
        this.message.error(extractErrorMessage(err, 'Không thể tải danh sách phòng ban.'));
        this.loading = false;
      },
    });
  }

  openEmployeeDrawer(userId?: string): void {
    if (userId) {
      this.employeeDrawer?.open(userId);
    }
  }

  filterDepartments(): void {
    if (!this.searchKeyword.trim()) {
      this.filteredDepartments = [...this.departments];
      return;
    }
    const kw = this.searchKeyword.toLowerCase().trim();
    this.filteredDepartments = this.departments.filter(
      d =>
        d.name.toLowerCase().includes(kw) ||
        d.code.toLowerCase().includes(kw) ||
        (d.managerName && d.managerName.toLowerCase().includes(kw)),
    );
  }

  openCreateModal(): void {
    this.selectedDepartment = null;
    this.deptForm.reset({
      code: '',
      name: '',
      description: '',
      managerId: null,
    });
    this.isModalVisible = true;
  }

  openEditModal(dept: DepartmentDto): void {
    this.selectedDepartment = dept;
    this.deptForm.patchValue({
      code: dept.code,
      name: dept.name,
      description: dept.description,
      managerId: dept.managerId || null,
    });
    this.isModalVisible = true;
  }

  handleModalCancel(): void {
    this.isModalVisible = false;
  }

  handleModalSubmit(): void {
    if (this.deptForm.invalid) {
      Object.values(this.deptForm.controls).forEach(control => {
        if (control.invalid) {
          control.markAsDirty();
          control.updateValueAndValidity({ onlySelf: true });
        }
      });
      return;
    }

    this.isModalSubmitting = true;
    const formVal = this.deptForm.value;

    if (this.selectedDepartment) {
      this.departmentService.update(this.selectedDepartment.id, formVal).subscribe({
        next: () => {
          this.message.success('Cập nhật phòng ban thành công!');
          this.isModalVisible = false;
          this.isModalSubmitting = false;
          this.loadDepartments();
        },
        error: err => {
          this.message.error(extractErrorMessage(err, 'Có lỗi xảy ra khi cập nhật phòng ban.'));
          this.isModalSubmitting = false;
        },
      });
    } else {
      this.departmentService.create(formVal).subscribe({
        next: () => {
          this.message.success('Thêm mới phòng ban thành công!');
          this.isModalVisible = false;
          this.isModalSubmitting = false;
          this.loadDepartments();
        },
        error: err => {
          this.message.error(extractErrorMessage(err, 'Có lỗi xảy ra khi tạo phòng ban.'));
          this.isModalSubmitting = false;
        },
      });
    }
  }

  deleteDepartment(dept: DepartmentDto): void {
    this.departmentService.delete(dept.id).subscribe({
      next: () => {
        this.message.success(`Đã xóa phòng ban "${dept.name}"`);
        this.loadDepartments();
      },
      error: err => {
        this.message.error(extractErrorMessage(err, 'Không thể xóa phòng ban.'));
      },
    });
  }

  // Quản lý nhân sự và chức vụ trong ngăn kéo phòng ban
  openMembersDrawer(dept: DepartmentDto): void {
    this.activeDepartment = dept;
    this.isDrawerVisible = true;
    this.editingMemberId = null;
    this.addMemberForm.reset({
      userId: null,
      position: '',
    });
    this.loadMembersAndUsers(dept.id);
  }

  closeDrawer(): void {
    this.isDrawerVisible = false;
    this.activeDepartment = null;
    this.loadDepartments();
  }

  loadMembersAndUsers(departmentId: string): void {
    this.isMembersLoading = true;
    this.departmentService.getMembers(departmentId).subscribe({
      next: members => {
        this.members = members || [];
        this.departmentService.getAvailableUsers(departmentId).subscribe({
          next: users => {
            this.availableUsers = users || [];
            this.isMembersLoading = false;
          },
          error: () => {
            this.isMembersLoading = false;
          },
        });
      },
      error: err => {
        this.message.error(
          extractErrorMessage(err, 'Không thể tải danh sách nhân sự phòng ban.'),
        );
        this.isMembersLoading = false;
      },
    });
  }

  submitAddMember(): void {
    if (this.isAddingMember) return;
    if (this.addMemberForm.invalid || !this.activeDepartment) {
      Object.values(this.addMemberForm.controls).forEach(c => {
        c.markAsDirty();
        c.updateValueAndValidity();
      });
      return;
    }

    const { userId, position } = this.addMemberForm.value;

    const selectedUser = this.availableUsers.find(u => u.id === userId);
    if (selectedUser?.currentDepartmentName && selectedUser.currentDepartmentName !== this.activeDepartment.name) {
      this.message.error(
        `Nhân sự "${selectedUser.fullName}" hiện đang thuộc phòng ban "${selectedUser.currentDepartmentName}". Mỗi nhân sự chỉ được thuộc duy nhất 1 phòng ban! Vui lòng gỡ khỏi phòng ban cũ trước khi thêm vào phòng ban mới.`,
      );
      return;
    }

    this.isAddingMember = true;
    this.departmentService
      .addMember(this.activeDepartment.id, { userId, position })
      .pipe(finalize(() => (this.isAddingMember = false)))
      .subscribe({
        next: () => {
          this.message.success('Đã thêm nhân sự vào phòng ban!');
          this.addMemberForm.reset();
          this.loadMembersAndUsers(this.activeDepartment!.id);
        },
        error: err => {
          this.message.error(extractErrorMessage(err, 'Không thể thêm nhân sự.'));
        },
      });
  }

  startEditPosition(member: DepartmentMemberDto): void {
    this.editingMemberId = member.userId;
    this.editingPosition = member.position;
  }

  cancelEditPosition(): void {
    this.editingMemberId = null;
    this.editingPosition = '';
  }

  savePosition(member: DepartmentMemberDto): void {
    if (!this.editingPosition.trim() || !this.activeDepartment) return;

    this.departmentService
      .updateMember(this.activeDepartment.id, member.userId, {
        position: this.editingPosition.trim(),
      })
      .subscribe({
        next: () => {
          this.message.success('Cập nhật chức vụ thành công!');
          member.position = this.editingPosition.trim();
          this.editingMemberId = null;
        },
        error: err => {
          this.message.error(extractErrorMessage(err, 'Không thể cập nhật chức vụ.'));
        },
      });
  }

  toggleManager(member: DepartmentMemberDto): void {
    if (!this.activeDepartment) return;

    const newManagerId = member.isManager ? undefined : member.userId;
    const actionText = member.isManager ? 'hủy quyền Trưởng phòng' : 'bổ nhiệm làm Trưởng phòng';

    this.departmentService
      .setManager(this.activeDepartment.id, newManagerId)
      .subscribe({
        next: () => {
          this.message.success(`Đã ${actionText} cho ${member.fullName}!`);
          this.loadMembersAndUsers(this.activeDepartment!.id);
        },
        error: err => {
          this.message.error(extractErrorMessage(err, 'Thao tác không thành công.'));
        },
      });
  }

  removeMember(member: DepartmentMemberDto): void {
    if (!this.activeDepartment) return;

    this.departmentService
      .removeMember(this.activeDepartment.id, member.userId)
      .subscribe({
        next: () => {
          this.message.success(`Đã xóa ${member.fullName} khỏi phòng ban.`);
          this.loadMembersAndUsers(this.activeDepartment!.id);
        },
        error: err => {
          this.message.error(extractErrorMessage(err, 'Không thể xóa nhân sự khỏi phòng ban.'));
        },
      });
  }

  quickSelectPosition(pos: string): void {
    this.addMemberForm.patchValue({ position: pos });
  }
}

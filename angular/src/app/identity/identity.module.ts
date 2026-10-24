import { NgModule } from '@angular/core';
import { SharedModule } from '../shared/shared.module';
import { IdentityRoutingModule } from './identity-routing.module';
import { RolesComponent } from './roles/roles.component';
import { UsersComponent } from './users/users.component';
import { PermissionModalComponent } from './components/permission-modal/permission-modal.component';

// Module quản lý tài khoản người dùng, vai trò và phân quyền hệ thống
@NgModule({
  declarations: [RolesComponent, UsersComponent, PermissionModalComponent],
  imports: [SharedModule, IdentityRoutingModule],
})
export class IdentityManagementModule {}

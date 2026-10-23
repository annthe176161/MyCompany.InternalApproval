import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { authGuard, permissionGuard } from '@abp/ng.core';
import { RolesComponent } from './roles/roles.component';
import { UsersComponent } from './users/users.component';

const routes: Routes = [
  {
    path: '',
    redirectTo: 'roles',
    pathMatch: 'full',
  },
  {
    path: 'roles',
    component: RolesComponent,
    canActivate: [authGuard, permissionGuard],
    data: {
      requiredPolicy: 'AbpIdentity.Roles',
    },
  },
  {
    path: 'users',
    component: UsersComponent,
    canActivate: [authGuard, permissionGuard],
    data: {
      requiredPolicy: 'AbpIdentity.Users',
    },
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class IdentityRoutingModule {}


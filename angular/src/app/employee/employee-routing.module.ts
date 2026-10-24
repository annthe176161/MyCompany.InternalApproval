import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { authGuard, permissionGuard } from '@abp/ng.core';
import { EmployeeComponent } from './employee.component';

const routes: Routes = [
  {
    path: '',
    component: EmployeeComponent,
    canActivate: [authGuard, permissionGuard],
    data: {
      requiredPolicy: 'Employees.Default',
    },
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class EmployeeRoutingModule {}


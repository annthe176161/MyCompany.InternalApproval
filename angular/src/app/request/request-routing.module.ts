import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { permissionGuard } from '@abp/ng.core';
import { RequestComponent } from './request.component';

const routes: Routes = [
  {
    path: '',
    component: RequestComponent,
    canActivate: [permissionGuard],
    data: { requiredPolicy: 'Requests.Default' },
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class RequestRoutingModule {}


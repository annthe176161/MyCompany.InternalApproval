import { NgModule } from '@angular/core';
import { SharedModule } from '../shared/shared.module';
import { RequestRoutingModule } from './request-routing.module';
import { RequestComponent } from './request.component';
import { RequestFormModalComponent } from './components/request-form-modal/request-form-modal.component';
import { RequestActionModalComponent } from './components/request-action-modal/request-action-modal.component';
import { RequestDetailDrawerComponent } from './components/request-detail-drawer/request-detail-drawer.component';

// Module quản lý đề xuất và quy trình phê duyệt nội bộ
@NgModule({
  declarations: [
    RequestComponent,
    RequestFormModalComponent,
    RequestActionModalComponent,
    RequestDetailDrawerComponent,
  ],
  imports: [SharedModule, RequestRoutingModule],
})
export class RequestModule {}

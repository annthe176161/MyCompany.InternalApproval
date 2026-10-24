import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CoreModule } from '@abp/ng.core';
import { ThemeSharedModule } from '@abp/ng.theme.shared';
import { SharedModule } from '../shared/shared.module';

// NG-ZORRO Modules
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzTimelineModule } from 'ng-zorro-antd/timeline';
import { NzMessageModule } from 'ng-zorro-antd/message';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzBadgeModule } from 'ng-zorro-antd/badge';
import { NzDropDownModule } from 'ng-zorro-antd/dropdown';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzPopoverModule } from 'ng-zorro-antd/popover';
import { NzRadioModule } from 'ng-zorro-antd/radio';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';

import { RequestRoutingModule } from './request-routing.module';
import { RequestComponent } from './request.component';
import { RequestFormModalComponent } from './components/request-form-modal/request-form-modal.component';
import { RequestActionModalComponent } from './components/request-action-modal/request-action-modal.component';
import { RequestDetailDrawerComponent } from './components/request-detail-drawer/request-detail-drawer.component';

@NgModule({
  declarations: [
    RequestComponent,
    RequestFormModalComponent,
    RequestActionModalComponent,
    RequestDetailDrawerComponent,
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    CoreModule,
    ThemeSharedModule,
    SharedModule,
    RequestRoutingModule,

    // NG-ZORRO Modules
    NzTableModule,
    NzModalModule,
    NzFormModule,
    NzInputModule,
    NzInputNumberModule,
    NzSelectModule,
    NzTagModule,
    NzButtonModule,
    NzTimelineModule,
    NzMessageModule,
    NzPopconfirmModule,
    NzBadgeModule,
    NzDropDownModule,
    NzDrawerModule,
    NzIconModule,
    NzCardModule,
    NzToolTipModule,
    NzDividerModule,
    NzCheckboxModule,
    NzPopoverModule,
    NzRadioModule,
    NzDatePickerModule,
  ],
})
export class RequestModule {}


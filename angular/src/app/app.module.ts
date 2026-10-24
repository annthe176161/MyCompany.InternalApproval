import { CoreModule, provideAbpCore, withOptions } from '@abp/ng.core';
import { registerLocale } from '@abp/ng.core/locale';
import {
  InternetConnectionStatusComponent,
  ThemeSharedModule,
  provideAbpThemeShared,
} from '@abp/ng.theme.shared';
import { provideFeatureManagementConfig } from '@abp/ng.feature-management';
import { provideAbpOAuth } from '@abp/ng.oauth';
import { provideIdentityConfig } from '@abp/ng.identity/config';
import { provideTenantManagementConfig } from '@abp/ng.tenant-management/config';
import { provideAccountConfig } from '@abp/ng.account/config';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { environment } from '../environments/environment';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { APP_ROUTE_PROVIDER } from './route.provider';
import { NOTIFICATION_NAV_ITEM_PROVIDER } from './nav-item.provider';
import { ThemeLeptonXModule } from '@abp/ng.theme.lepton-x';
import { SideMenuLayoutModule } from '@abp/ng.theme.lepton-x/layouts';
import { AccountLayoutModule } from '@abp/ng.theme.lepton-x/account';
import { NZ_I18N, vi_VN } from 'ng-zorro-antd/i18n';
import { NZ_CONFIG, NzConfig } from 'ng-zorro-antd/core/config';
import { registerLocaleData } from '@angular/common';
import en from '@angular/common/locales/en';
import vi from '@angular/common/locales/vi';
import { FormsModule } from '@angular/forms';

registerLocaleData(en);
registerLocaleData(vi);

const ngZorroConfig: NzConfig = {
  message: {
    nzTop: 24,
    nzDuration: 4000,
    nzMaxStack: 5,
  },
  notification: {
    nzTop: 24,
    nzPlacement: 'topRight',
    nzDuration: 4500,
  },
};

@NgModule({
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    AppRoutingModule,
    CoreModule,
    ThemeSharedModule,

    InternetConnectionStatusComponent,
    ThemeLeptonXModule.forRoot(),
    SideMenuLayoutModule.forRoot(),
    AccountLayoutModule.forRoot(),
    FormsModule,
  ],
  declarations: [AppComponent],
  providers: [
    APP_ROUTE_PROVIDER,
    NOTIFICATION_NAV_ITEM_PROVIDER,
    provideAbpCore(
      withOptions({
        environment,
        registerLocaleFn: registerLocale(),
      }),
    ),
    provideAbpOAuth(),
    provideAbpThemeShared(),
    provideAccountConfig(),
    provideIdentityConfig(),
    provideTenantManagementConfig(),
    provideFeatureManagementConfig(),
    { provide: NZ_I18N, useValue: vi_VN },
    { provide: NZ_CONFIG, useValue: ngZorroConfig },
  ],
  bootstrap: [AppComponent],
})
export class AppModule {}

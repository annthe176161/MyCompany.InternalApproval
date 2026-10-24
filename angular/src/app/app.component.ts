import { Component, OnInit } from '@angular/core';
import { ReplaceableComponentsService } from '@abp/ng.core';
import { eThemeLeptonXComponents, eUserMenuItems } from '@abp/ng.theme.lepton-x';
import { UserMenuService } from '@abp/ng.theme.shared';
import { BrandLogoComponent } from './shared/components/brand-logo/brand-logo.component';
import { AppLanguageService } from './shared/services';

@Component({
  selector: 'app-root',
  template: `
    <abp-loader-bar></abp-loader-bar>
    <abp-dynamic-layout></abp-dynamic-layout>
    <abp-internet-status></abp-internet-status>
  `,
})
export class AppComponent implements OnInit {
  constructor(
    private replaceableComponents: ReplaceableComponentsService,
    private userMenuService: UserMenuService,
    private appLanguageService: AppLanguageService,
  ) {}

  ngOnInit(): void {
    // Khởi tạo đồng bộ đa ngôn ngữ giao diện toàn hệ thống
    this.appLanguageService.init();

    this.replaceableComponents.add({
      component: BrandLogoComponent,
      key: eThemeLeptonXComponents.Logo,
    });

    // Loại bỏ mục Tài khoản của tôi khỏi menu người dùng
    this.userMenuService.removeItem(eUserMenuItems.MyAccount);
    this.userMenuService.removeItem('MyAccount');

    this.userMenuService.items$.subscribe(items => {
      if (items.some(x => x.id === eUserMenuItems.MyAccount || x.id === 'MyAccount')) {
        this.userMenuService.removeItem(eUserMenuItems.MyAccount);
        this.userMenuService.removeItem('MyAccount');
      }
    });
  }
}

import { RoutesService, eLayoutType } from '@abp/ng.core';
import { APP_INITIALIZER } from '@angular/core';

export const APP_ROUTE_PROVIDER = [
  { provide: APP_INITIALIZER, useFactory: configureRoutes, deps: [RoutesService], multi: true },
];

function configureRoutes(routesService: RoutesService) {
  return () => {
    routesService.add([
      {
        path: '/',
        name: '::Menu:Home',
        iconClass: 'fas fa-home',
        order: 1,
        layout: eLayoutType.application,
      },
      {
        path: '/requests',
        name: '::Menu:Requests',
        iconClass: 'fas fa-file-invoice',
        order: 2,
        layout: eLayoutType.application,
        requiredPolicy: 'Requests.Default',
      },
      {
        path: '/departments',
        name: '::Menu:Departments',
        parentName: 'AbpUiNavigation::Menu:Administration',
        iconClass: 'fas fa-sitemap',
        order: 1,
        layout: eLayoutType.application,
        requiredPolicy: 'Departments.Default',
      },
      {
        path: '/employees',
        name: '::Menu:Employees',
        iconClass: 'fas fa-id-card',
        order: 3,
        layout: eLayoutType.application,
      },
    ]);

    // Tinh gọn & Chuẩn hóa tên gọi menu hệ thống theo khóa đa ngôn ngữ
    routesService.patch('AbpIdentity::Menu:IdentityManagement', {
      name: '::Menu:IdentityManagement',
    });
    routesService.patch('AbpIdentity::Roles', {
      name: '::Menu:Roles',
    });
    routesService.patch('AbpIdentity::Users', {
      name: '::Menu:Users',
    });

    // Loại bỏ hoàn toàn mục Người thuê, Cài đặt và Tài khoản của tôi khỏi menu
    routesService.remove([
      'AbpTenantManagement::Menu:TenantManagement',
      'AbpSettingManagement::Settings',
      'AbpAccount::MyAccount',
    ]);
  };
}

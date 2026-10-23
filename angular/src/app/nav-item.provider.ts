import { APP_INITIALIZER } from '@angular/core';
import { NavItemsService } from '@abp/ng.theme.shared';
import { NotificationBellComponent } from './shared/components/notification-bell/notification-bell.component';

export const NOTIFICATION_NAV_ITEM_PROVIDER = [
  {
    provide: APP_INITIALIZER,
    useFactory: configureNotificationNavItem,
    deps: [NavItemsService],
    multi: true,
  },
];

function configureNotificationNavItem(navItems: NavItemsService) {
  return () => {
    navItems.addItems([
      {
        id: 'NotificationBell',
        order: 1,
        component: NotificationBellComponent,
      },
    ]);
  };
}


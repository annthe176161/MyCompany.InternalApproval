import { Component, OnInit } from '@angular/core';
import { ReplaceableComponentsService } from '@abp/ng.core';
import { eThemeLeptonXComponents } from '@abp/ng.theme.lepton-x';
import { BrandLogoComponent } from './shared/components/brand-logo/brand-logo.component';

@Component({
  selector: 'app-root',
  template: `
    <abp-loader-bar></abp-loader-bar>
    <abp-dynamic-layout></abp-dynamic-layout>
    <abp-internet-status></abp-internet-status>
  `,
})
export class AppComponent implements OnInit {
  constructor(private replaceableComponents: ReplaceableComponentsService) {}

  ngOnInit(): void {
    this.replaceableComponents.add({
      component: BrandLogoComponent,
      key: eThemeLeptonXComponents.Logo,
    });
  }
}

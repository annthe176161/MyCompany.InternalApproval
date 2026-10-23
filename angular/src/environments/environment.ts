import { Environment } from '@abp/ng.core';

const baseUrl = 'http://localhost:4200';

export const environment = {
  production: false,
  application: {
    baseUrl,
    name: 'InternalApproval',
    logoUrl: '',
  },
  oAuthConfig: {
    issuer: 'https://localhost:44341/',
    redirectUri: baseUrl,
    clientId: 'InternalApproval_App',
    responseType: 'code',
    scope: 'offline_access InternalApproval',
    requireHttps: true,
  },
  apis: {
    default: {
      url: 'https://localhost:44341',
      rootNamespace: 'MyCompany.InternalApproval',
    },
  },
} as Environment;

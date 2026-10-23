import { mapEnumToOptions } from '@abp/ng.core';

export enum RequestType {
  Leave = 1,
  Equipment = 2,
  AdvanceMoney = 3,
}

export const requestTypeOptions = mapEnumToOptions(RequestType);

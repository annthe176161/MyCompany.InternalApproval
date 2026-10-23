import { RequestType } from '../../proxy/requests/request-type.enum';

export interface RequestTypeMeta {
  value: RequestType;
  label: string;
  color: string;
  chartColor: string;
}

export const REQUEST_TYPE_CONFIG: Record<RequestType, RequestTypeMeta> = {
  [RequestType.Leave]: {
    value: RequestType.Leave,
    label: 'Nghỉ phép',
    color: 'purple',
    chartColor: '#722ed1',
  },
  [RequestType.Equipment]: {
    value: RequestType.Equipment,
    label: 'Mua thiết bị',
    color: 'orange',
    chartColor: '#fa8c16',
  },
  [RequestType.AdvanceMoney]: {
    value: RequestType.AdvanceMoney,
    label: 'Tạm ứng tiền',
    color: 'cyan',
    chartColor: '#13c2c2',
  },
};

export const REQUEST_TYPE_LIST: RequestTypeMeta[] = [
  REQUEST_TYPE_CONFIG[RequestType.Leave],
  REQUEST_TYPE_CONFIG[RequestType.Equipment],
  REQUEST_TYPE_CONFIG[RequestType.AdvanceMoney],
];

export function getRequestTypeMeta(type: RequestType): RequestTypeMeta {
  return (
    REQUEST_TYPE_CONFIG[type] || {
      value: type,
      label: 'Khác',
      color: 'default',
      chartColor: '#8c8c8c',
    }
  );
}


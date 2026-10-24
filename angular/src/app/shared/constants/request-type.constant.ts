import { RequestType } from '../../proxy/requests/request-type.enum';
import { RequestTypeMeta } from '../models/request-type-meta.model';

export { RequestTypeMeta } from '../models/request-type-meta.model';

// Cấu hình siêu dữ liệu hiển thị cho từng loại đề xuất
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

// Danh sách loại đề xuất theo thứ tự hiển thị
export const REQUEST_TYPE_LIST: RequestTypeMeta[] = [
  REQUEST_TYPE_CONFIG[RequestType.Leave],
  REQUEST_TYPE_CONFIG[RequestType.Equipment],
  REQUEST_TYPE_CONFIG[RequestType.AdvanceMoney],
];

// Lấy thông tin cấu hình hiển thị theo mã loại đề xuất
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

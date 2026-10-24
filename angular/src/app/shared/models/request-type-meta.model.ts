import { RequestType } from '../../proxy/requests/request-type.enum';

// Siêu dữ liệu hiển thị cho từng loại đề xuất
export interface RequestTypeMeta {
  value: RequestType;
  label: string;
  color: string;
  chartColor: string;
}


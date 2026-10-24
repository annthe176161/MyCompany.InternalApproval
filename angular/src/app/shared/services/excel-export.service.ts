import { Injectable } from '@angular/core';
import * as XLSX from 'xlsx';
import { RequestDto } from '../../proxy/requests';
import type { EmployeeLeaveReportItemDto } from '../../proxy/employees/models';
import { getRequestStatusMeta } from '../constants/request-status.constant';
import { getRequestTypeMeta } from '../constants/request-type.constant';
import { DashboardReportData } from '../models/dashboard-report-data.model';
import { formatDateTimeFullVn, formatDateTimeShortVn, getFileTimestamp } from '../utils/date.util';

export { DashboardReportData } from '../models/dashboard-report-data.model';

// Dịch vụ xuất báo cáo dữ liệu ra tập tin Excel (.xlsx)
@Injectable({
  providedIn: 'root',
})
export class ExcelExportService {
  // Xuất danh sách đề xuất chi tiết ra file Excel (.xlsx)
  exportRequests(
    requests: RequestDto[],
    exporterName: string = 'Người dùng',
    scopeName: string = 'Đề xuất của tôi',
    filterText?: string,
  ): void {
    const wb = XLSX.utils.book_new();
    const printTimeStr = formatDateTimeShortVn(new Date());

    // Dữ liệu tiêu đề và cấu trúc bảng báo cáo
    const aoaData: any[][] = [
      ['HỆ THỐNG QUẢN LÝ PHÊ DUYỆT NỘI BỘ (INTERNAL APPROVAL)'],
      ['BÁO CÁO DANH SÁCH ĐỀ XUẤT CHI TIẾT'],
      [
        `Thời gian xuất: ${printTimeStr}`,
        `Người xuất: ${exporterName}`,
        `Phạm vi: ${scopeName}`,
        filterText ? `Bộ lọc: ${filterText}` : '',
      ],
      [],
      [
        'STT',
        'Mã đề xuất',
        'Tiêu đề đề xuất',
        'Loại hình',
        'Số tiền (VNĐ)',
        'Người tạo đề xuất',
        'Ngày tạo',
        'Trạng thái',
        'Người phê duyệt',
        'Ngày phê duyệt',
        'Lý do từ chối / Ghi chú',
      ],
    ];

    let totalAmount = 0;

    requests.forEach((req, idx) => {
      const typeMeta = getRequestTypeMeta(req.type);
      const statusMeta = getRequestStatusMeta(req.status);
      const amount = req.amount || 0;
      totalAmount += amount;

      aoaData.push([
        idx + 1,
        req.id ? '#' + req.id.substring(0, 8).toUpperCase() : '',
        req.title || '',
        typeMeta.label,
        amount > 0 ? amount : '',
        req.creatorName || 'N/A',
        formatDateTimeShortVn(req.creationTime),
        statusMeta.label,
        req.approverName || '',
        formatDateTimeShortVn(req.approvalTime),
        req.rejectionReason || '',
      ]);
    });

    // Dòng tổng kết cuối bảng
    aoaData.push([]);
    aoaData.push([
      'TỔNG CỘNG',
      `${requests.length} đề xuất`,
      '',
      '',
      totalAmount > 0 ? totalAmount : 0,
      '',
      '',
      '',
      '',
      '',
      '',
    ]);

    const ws = XLSX.utils.aoa_to_sheet(aoaData);

    // Cấu hình độ rộng các cột hiển thị
    ws['!cols'] = [
      { wch: 6 },
      { wch: 14 },
      { wch: 34 },
      { wch: 18 },
      { wch: 18 },
      { wch: 22 },
      { wch: 18 },
      { wch: 16 },
      { wch: 22 },
      { wch: 18 },
      { wch: 30 },
    ];

    XLSX.utils.book_append_sheet(wb, ws, 'Danh sách đề xuất');

    const fileName = `Bao_Cao_De_Xuat_${scopeName.replace(/\s+/g, '_')}_${getFileTimestamp()}.xlsx`;
    XLSX.writeFile(wb, fileName);
  }

  // Xuất báo cáo tổng hợp Dashboard gồm 2 Sheet: Tổng quan chỉ số và Danh sách chi tiết
  exportDashboardReport(data: DashboardReportData): void {
    const wb = XLSX.utils.book_new();
    const printTimeStr = formatDateTimeShortVn(new Date());

    // Sheet 1: Tổng quan chỉ số vận hành
    const summaryAoa: any[][] = [
      ['HỆ THỐNG QUẢN LÝ PHÊ DUYỆT NỘI BỘ (INTERNAL APPROVAL)'],
      ['BÁO CÁO THỐNG KÊ TỔNG QUAN VẬN HÀNH (DASHBOARD)'],
      [
        `Thời gian xuất: ${printTimeStr}`,
        `Người xuất: ${data.exporterName}`,
        `Góc nhìn: ${data.scopeName}`,
      ],
      [],
      ['1. CÁC CHỈ SỐ HOẠT ĐỘNG CHÍNH (KEY METRICS)'],
      ['Chỉ số', 'Giá trị', 'Đơn vị tính'],
      ['Tổng số lượng đề xuất', data.totalCount, 'phiếu'],
      ['Tổng kinh phí đề xuất', data.totalAmount, 'VNĐ'],
      ['Tổng kinh phí đã phê duyệt', data.approvedAmount, 'VNĐ'],
      ['Tỷ lệ duyệt thành công', data.approvalRate, '%'],
      [],
      ['2. THỐNG KÊ THEO TRẠNG THÁI'],
      ['Trạng thái', 'Số lượng', 'Tỷ lệ (%)'],
    ];

    const total = data.totalCount || 1;
    data.statusStats.forEach(st => {
      const pct = Math.round((st.count / total) * 100);
      summaryAoa.push([st.label, st.count, `${pct}%`]);
    });

    summaryAoa.push([]);
    summaryAoa.push(['3. THỐNG KÊ THEO LOẠI HÌNH ĐỀ XUẤT']);
    summaryAoa.push(['Loại hình', 'Số lượng', 'Tổng kinh phí (VNĐ)', 'Tỷ trọng (%)']);

    data.typeStats.forEach(tp => {
      summaryAoa.push([tp.label, tp.count, tp.amount, `${tp.percent}%`]);
    });

    const summaryWs = XLSX.utils.aoa_to_sheet(summaryAoa);
    summaryWs['!cols'] = [{ wch: 32 }, { wch: 22 }, { wch: 22 }, { wch: 18 }];
    XLSX.utils.book_append_sheet(wb, summaryWs, 'Tổng quan chỉ số');

    // Sheet 2: Danh sách hồ sơ đề xuất chi tiết
    const detailsAoa: any[][] = [
      ['DANH SÁCH HỒ SƠ ĐỀ XUẤT CHI TIẾT'],
      [],
      [
        'STT',
        'Mã đề xuất',
        'Tiêu đề',
        'Loại hình',
        'Số tiền (VNĐ)',
        'Người tạo',
        'Ngày tạo',
        'Trạng thái',
        'Người duyệt',
        'Ngày duyệt',
        'Lý do từ chối / Ghi chú',
      ],
    ];

    data.requests.forEach((req, idx) => {
      const typeMeta = getRequestTypeMeta(req.type);
      const statusMeta = getRequestStatusMeta(req.status);
      detailsAoa.push([
        idx + 1,
        req.id ? '#' + req.id.substring(0, 8).toUpperCase() : '',
        req.title || '',
        typeMeta.label,
        req.amount || '',
        req.creatorName || 'N/A',
        formatDateTimeShortVn(req.creationTime),
        statusMeta.label,
        req.approverName || '',
        formatDateTimeShortVn(req.approvalTime),
        req.rejectionReason || '',
      ]);
    });

    const detailsWs = XLSX.utils.aoa_to_sheet(detailsAoa);
    detailsWs['!cols'] = [
      { wch: 6 },
      { wch: 14 },
      { wch: 34 },
      { wch: 18 },
      { wch: 18 },
      { wch: 22 },
      { wch: 18 },
      { wch: 16 },
      { wch: 22 },
      { wch: 18 },
      { wch: 30 },
    ];
    XLSX.utils.book_append_sheet(wb, detailsWs, 'Danh sách chi tiết');

    const fileName = `Bao_Cao_Tong_Quan_Dashboard_${getFileTimestamp()}.xlsx`;
    XLSX.writeFile(wb, fileName);
  }

  // Xuất báo cáo thống kê ngày nghỉ phép nhân sự ra file Excel (.xlsx)
  exportLeaveStatistics(
    data: EmployeeLeaveReportItemDto[],
    exporterName: string = 'Người dùng',
    scopeName: string = 'Toàn công ty',
    year: number = new Date().getFullYear(),
    filterText?: string,
  ): void {
    const wb = XLSX.utils.book_new();
    const exportTimeStr = formatDateTimeFullVn(new Date());

    const aoaData: any[][] = [
      ['BÁO CÁO THỐNG KÊ NGÀY NGHỈ PHÉP NHÂN VIÊN'],
      [],
      ['Năm báo cáo:', year],
      ['Phạm vi phòng ban:', scopeName],
      ['Người xuất báo cáo:', exporterName],
      ['Thời gian xuất file:', exportTimeStr],
      ['Bộ lọc tìm kiếm:', filterText || '(Không có)'],
      [],
      [
        'STT',
        'Mã NV / Tên đăng nhập',
        'Họ và tên',
        'Email',
        'Phòng ban',
        'Chức vụ',
        'Tổng ngày nghỉ phép (đã duyệt)',
        'Số đợt gửi',
        'Số đợt duyệt',
        'Đợt nghỉ gần nhất',
      ],
    ];

    let totalApprovedDays = 0;
    let totalSentRequests = 0;
    let totalApprovedRequests = 0;

    data.forEach((item, index) => {
      const approvedDays = item.totalApprovedLeaveDays || 0;
      const sentRequests = item.totalSentLeaveRequests || 0;
      const approvedRequests = item.totalApprovedLeaveRequests || 0;

      totalApprovedDays += approvedDays;
      totalSentRequests += sentRequests;
      totalApprovedRequests += approvedRequests;

      aoaData.push([
        index + 1,
        item.userName || '',
        item.fullName || '',
        item.email || '',
        item.departmentName || '',
        item.position || '',
        approvedDays,
        sentRequests,
        approvedRequests,
        item.latestLeavePeriodFormatted || 'Chưa có',
      ]);
    });

    // Dòng tổng cộng cuối bảng báo cáo nghỉ phép
    aoaData.push([
      'TỔNG CỘNG',
      '',
      '',
      '',
      '',
      '',
      totalApprovedDays,
      totalSentRequests,
      totalApprovedRequests,
      '',
    ]);

    const ws = XLSX.utils.aoa_to_sheet(aoaData);

    ws['!cols'] = [
      { wch: 6 },
      { wch: 22 },
      { wch: 26 },
      { wch: 28 },
      { wch: 24 },
      { wch: 20 },
      { wch: 30 },
      { wch: 14 },
      { wch: 14 },
      { wch: 34 },
    ];

    XLSX.utils.book_append_sheet(wb, ws, 'Báo cáo ngày nghỉ phép');

    const fileName = `BaoCao_NgayNghiPhep_${year}_${getFileTimestamp()}.xlsx`;
    XLSX.writeFile(wb, fileName);
  }
}

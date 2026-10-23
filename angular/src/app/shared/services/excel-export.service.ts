import { Injectable } from '@angular/core';
import * as XLSX from 'xlsx';
import { RequestDto, RequestStatus, RequestType } from '../../proxy/requests';
import type { EmployeeLeaveReportItemDto } from '../../proxy/employees/models';
import { getRequestStatusMeta } from '../constants/request-status.constant';
import { getRequestTypeMeta } from '../constants/request-type.constant';

export interface DashboardReportData {
  totalCount: number;
  totalAmount: number;
  approvedAmount: number;
  approvalRate: number;
  statusStats: { label: string; count: number }[];
  typeStats: { label: string; count: number; amount: number; percent: number }[];
  requests: RequestDto[];
  scopeName: string;
  exporterName: string;
}

@Injectable({
  providedIn: 'root',
})
export class ExcelExportService {
  constructor() {}

  /**
   * Định dạng ngày giờ dạng DD/MM/YYYY HH:mm
   */
  private formatDate(dateVal?: string | Date | null): string {
    if (!dateVal) return '';
    try {
      const d = dateVal instanceof Date ? dateVal : new Date(dateVal);
      if (isNaN(d.getTime())) return '';
      const pad = (n: number) => (n < 10 ? '0' + n : '' + n);
      return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
    } catch {
      return '';
    }
  }

  /**
   * Tạo timestamp phục vụ đặt tên file: YYYYMMDD_HHmm
   */
  private getFileTimestamp(): string {
    const d = new Date();
    const pad = (n: number) => (n < 10 ? '0' + n : '' + n);
    return `${d.getFullYear()}${pad(d.getMonth() + 1)}${pad(d.getDate())}_${pad(d.getHours())}${pad(d.getMinutes())}`;
  }

  /**
   * Xuất danh sách đề xuất ra file Excel (.xlsx)
   */
  exportRequests(
    requests: RequestDto[],
    exporterName: string = 'Người dùng',
    scopeName: string = 'Đề xuất của tôi',
    filterText?: string
  ): void {
    const wb = XLSX.utils.book_new();

    const now = new Date();
    const printTimeStr = this.formatDate(now.toISOString());

    // Header thông tin báo cáo
    const aoaData: any[][] = [
      ['HỆ THỐNG QUẢN LÝ PHÊ DUYỆT NỘI BỘ (INTERNAL APPROVAL)'],
      ['BÁO CÁO DANH SÁCH ĐỀ XUẤT CHI TIẾT'],
      [`Thời gian xuất: ${printTimeStr}`, `Người xuất: ${exporterName}`, `Phạm vi: ${scopeName}`, filterText ? `Bộ lọc: ${filterText}` : ''],
      [], // Dòng trống
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
        this.formatDate(req.creationTime),
        statusMeta.label,
        req.approverName || '',
        this.formatDate(req.approvalTime),
        req.rejectionReason || '',
      ]);
    });

    // Dòng tổng kết
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

    // Cấu hình độ rộng các cột (Characters width)
    ws['!cols'] = [
      { wch: 6 },  // STT
      { wch: 14 }, // Mã
      { wch: 34 }, // Tiêu đề
      { wch: 18 }, // Loại
      { wch: 18 }, // Số tiền
      { wch: 22 }, // Người tạo
      { wch: 18 }, // Ngày tạo
      { wch: 16 }, // Trạng thái
      { wch: 22 }, // Người duyệt
      { wch: 18 }, // Ngày duyệt
      { wch: 30 }, // Ghi chú
    ];

    XLSX.utils.book_append_sheet(wb, ws, 'Danh sách đề xuất');

    const fileName = `Bao_Cao_De_Xuat_${scopeName.replace(/\s+/g, '_')}_${this.getFileTimestamp()}.xlsx`;
    XLSX.writeFile(wb, fileName);
  }

  /**
   * Xuất báo cáo tổng hợp Dashboard (2 Sheets: Tổng quan chỉ số + Danh sách chi tiết)
   */
  exportDashboardReport(data: DashboardReportData): void {
    const wb = XLSX.utils.book_new();
    const now = new Date();
    const printTimeStr = this.formatDate(now.toISOString());

    // ================= SHEET 1: TỔNG QUAN CHỈ SỐ =================
    const summaryAoa: any[][] = [
      ['HỆ THỐNG QUẢN LÝ PHÊ DUYỆT NỘI BỘ (INTERNAL APPROVAL)'],
      ['BÁO CÁO THỐNG KÊ TỔNG QUAN VẬN HÀNH (DASHBOARD)'],
      [`Thời gian xuất: ${printTimeStr}`, `Người xuất: ${data.exporterName}`, `Góc nhìn: ${data.scopeName}`],
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

    // ================= SHEET 2: DANH SÁCH CHI TIẾT =================
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
        this.formatDate(req.creationTime),
        statusMeta.label,
        req.approverName || '',
        this.formatDate(req.approvalTime),
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

    const fileName = `Bao_Cao_Tong_Quan_Dashboard_${this.getFileTimestamp()}.xlsx`;
    XLSX.writeFile(wb, fileName);
  }

  /**
   * Định dạng ngày giờ dạng DD/MM/YYYY HH:mm:ss
   */
  private formatDateTime(dateVal?: string | Date | null): string {
    if (!dateVal) return '';
    try {
      const d = dateVal instanceof Date ? dateVal : new Date(dateVal);
      if (isNaN(d.getTime())) return '';
      const pad = (n: number) => (n < 10 ? '0' + n : '' + n);
      return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
    } catch {
      return '';
    }
  }

  /**
   * Xuất báo cáo thống kê ngày nghỉ phép ra file Excel (.xlsx)
   */
  exportLeaveStatistics(
    data: EmployeeLeaveReportItemDto[],
    exporterName: string = 'Người dùng',
    scopeName: string = 'Toàn công ty',
    year: number = new Date().getFullYear(),
    filterText?: string
  ): void {
    const wb = XLSX.utils.book_new();
    const exportTimeStr = this.formatDateTime(new Date());

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

    // Summary total row
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
      { wch: 6 },  // STT
      { wch: 22 }, // Mã NV / Tên đăng nhập
      { wch: 26 }, // Họ và tên
      { wch: 28 }, // Email
      { wch: 24 }, // Phòng ban
      { wch: 20 }, // Chức vụ
      { wch: 30 }, // Tổng ngày nghỉ phép (đã duyệt)
      { wch: 14 }, // Số đợt gửi
      { wch: 14 }, // Số đợt duyệt
      { wch: 34 }, // Đợt nghỉ gần nhất
    ];

    XLSX.utils.book_append_sheet(wb, ws, 'Báo cáo ngày nghỉ phép');

    const fileName = `BaoCao_NgayNghiPhep_${year}_${this.getFileTimestamp()}.xlsx`;
    XLSX.writeFile(wb, fileName);
  }
}



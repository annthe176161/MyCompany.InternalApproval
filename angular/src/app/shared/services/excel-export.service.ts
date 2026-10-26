import { Injectable } from '@angular/core';
import * as XLSX from 'xlsx';
import { RequestDto } from '../../proxy/requests';
import type {
  EmployeeLeaveReportItemDto,
  EmployeeProfileDto,
  EmployeeYearlyStatsDto,
  LeaveTimelineItemDto,
} from '../../proxy/employees/models';
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

    const sumStatus = data.statusStats.reduce((s, x) => s + (x.count || 0), 0);
    const total = Math.max(data.totalCount || 0, sumStatus) || 1;
    data.statusStats.forEach(st => {
      const pct = Math.round(((st.count || 0) / total) * 100);
      summaryAoa.push([st.label, st.count || 0, `${pct}%`]);
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

  // Xuất báo cáo thống kê chi tiết và lịch sử đề xuất của riêng 1 nhân sự (.xlsx)
  exportEmployeeDetailReport(
    profile: EmployeeProfileDto,
    yearlyStats: EmployeeYearlyStatsDto | null,
    requests: RequestDto[],
    leaveTimeline: LeaveTimelineItemDto[],
    exporterName: string = 'Người dùng',
  ): void {
    const wb = XLSX.utils.book_new();
    const exportTimeStr = formatDateTimeFullVn(new Date());
    const year = yearlyStats?.year || new Date().getFullYear();

    // 1. SHEET 1: THỐNG KÊ TỔNG QUAN
    const summaryAoa: any[][] = [
      ['HỆ THỐNG QUẢN LÝ PHÊ DUYỆT NỘI BỘ (INTERNAL APPROVAL)'],
      ['BÁO CÁO THỐNG KÊ CHI TIẾT NHÂN SỰ & ĐỀ XUẤT'],
      [],
      ['Người xuất báo cáo:', exporterName],
      ['Thời gian xuất file:', exportTimeStr],
      ['Năm thống kê:', year],
      [],
      ['1. THÔNG TIN NHÂN SỰ:'],
      ['Họ và tên:', profile.fullName || '—'],
      ['Tên tài khoản (Username):', profile.userName || '—'],
      ['Địa chỉ Email:', profile.email || '—'],
      ['Phòng ban:', profile.departmentName ? `${profile.departmentName} (${profile.departmentCode})` : 'Chưa phân bổ'],
      ['Chức danh / Vị trí:', profile.position || 'Nhân viên'],
      ['Vai trò quản lý:', profile.isManager ? 'Trưởng phòng' : 'Thành viên'],
      ['Ngày vào làm việc:', profile.joinDate ? formatDateTimeShortVn(profile.joinDate) : 'Chưa cập nhật'],
      ['Thâm niên công tác:', profile.tenureFormatted || '0 tháng'],
      [],
      ['2. TỔNG HỢP SỐ LƯỢNG ĐỀ XUẤT & NGHỈ PHÉP (NĂM ' + year + '):'],
      ['Chỉ số thống kê', 'Giá trị', 'Đơn vị tính'],
      ['Tổng số đơn đề xuất đã gửi lên', yearlyStats?.totalRequests ?? requests.length, 'Đơn'],
      ['Số đơn đề xuất đã được phê duyệt', yearlyStats?.approvedCount ?? 0, 'Đơn'],
      ['Số đơn đề xuất đang chờ phê duyệt', yearlyStats?.submittedCount ?? 0, 'Đơn'],
      ['Số đơn đề xuất bị từ chối', yearlyStats?.rejectedCount ?? 0, 'Đơn'],
      ['Tổng số đợt gửi đơn nghỉ phép', leaveTimeline.length, 'Đợt'],
      ['Tổng số ngày nghỉ phép đã duyệt', yearlyStats?.leaveDaysTotal ?? 0, 'Ngày'],
      ['Tổng số tiền đã đề xuất', yearlyStats?.totalAmountRequested ?? 0, 'VNĐ'],
      ['Tổng số tiền đã được giải ngân', yearlyStats?.totalAmountDisbursed ?? 0, 'VNĐ'],
      [],
      ['3. PHÂN BỔ ĐỀ XUẤT THEO TỪNG LOẠI HÌNH:'],
      ['Loại hình đề xuất', 'Số lượng đơn', 'Tổng số tiền (VNĐ)'],
    ];

    if (yearlyStats?.breakdownByType?.length) {
      yearlyStats.breakdownByType.forEach(item => {
        summaryAoa.push([item.typeName, item.count, item.totalAmount]);
      });
    } else {
      summaryAoa.push(['Không có dữ liệu phân loại', 0, 0]);
    }

    const summaryWs = XLSX.utils.aoa_to_sheet(summaryAoa);
    summaryWs['!cols'] = [{ wch: 36 }, { wch: 28 }, { wch: 20 }];
    XLSX.utils.book_append_sheet(wb, summaryWs, 'Thống kê tổng quan');

    // 2. SHEET 2: DANH SÁCH TẤT CẢ ĐỀ XUẤT
    const requestsAoa: any[][] = [
      ['DANH SÁCH TẤT CẢ ĐỀ XUẤT ĐÃ GỬI LÊN'],
      [`Nhân sự: ${profile.fullName} (@${profile.userName}) - Phòng ban: ${profile.departmentName || 'N/A'}`],
      [],
      [
        'STT',
        'Mã đề xuất',
        'Tiêu đề đề xuất',
        'Loại hình',
        'Số tiền (VNĐ)',
        'Ngày tạo đơn',
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
      const amt = req.amount || 0;
      totalAmount += amt;

      requestsAoa.push([
        idx + 1,
        req.id ? '#' + req.id.substring(0, 8).toUpperCase() : '',
        req.title || '',
        typeMeta.label,
        amt > 0 ? amt : 0,
        formatDateTimeShortVn(req.creationTime),
        statusMeta.label,
        req.approverName || '',
        formatDateTimeShortVn(req.approvalTime),
        req.rejectionReason || '',
      ]);
    });

    requestsAoa.push([]);
    requestsAoa.push([
      'TỔNG CỘNG',
      `${requests.length} đề xuất`,
      '',
      '',
      totalAmount,
      '',
      '',
      '',
      '',
      '',
    ]);

    const requestsWs = XLSX.utils.aoa_to_sheet(requestsAoa);
    requestsWs['!cols'] = [
      { wch: 6 },
      { wch: 14 },
      { wch: 34 },
      { wch: 18 },
      { wch: 18 },
      { wch: 18 },
      { wch: 16 },
      { wch: 22 },
      { wch: 18 },
      { wch: 32 },
    ];
    XLSX.utils.book_append_sheet(wb, requestsWs, 'Lịch sử đề xuất');

    // 3. SHEET 3: CHI TIẾT CÁC ĐỢT NGHỈ PHÉP
    const leaveAoa: any[][] = [
      ['CHI TIẾT LỊCH SỬ NGHỈ PHÉP'],
      [`Nhân sự: ${profile.fullName} (@${profile.userName})`],
      [],
      [
        'STT',
        'Mã đơn',
        'Tiêu đề',
        'Từ ngày',
        'Đến ngày',
        'Số ngày nghỉ',
        'Lý do nghỉ phép',
        'Trạng thái',
        'Người phê duyệt',
        'Ngày phê duyệt',
      ],
    ];

    let totalLeaveDays = 0;
    leaveTimeline.forEach((leave, idx) => {
      totalLeaveDays += leave.days || 0;
      leaveAoa.push([
        idx + 1,
        leave.requestId ? '#' + leave.requestId.substring(0, 8).toUpperCase() : '',
        leave.title || '',
        leave.startDate ? formatDateTimeShortVn(leave.startDate) : '',
        leave.endDate ? formatDateTimeShortVn(leave.endDate) : '',
        leave.days || 0,
        leave.reason || '',
        leave.statusName || '',
        leave.approverName || '',
        leave.approvalTime ? formatDateTimeShortVn(leave.approvalTime) : '',
      ]);
    });

    leaveAoa.push([]);
    leaveAoa.push([
      'TỔNG CỘNG',
      `${leaveTimeline.length} đợt nghỉ`,
      '',
      '',
      '',
      totalLeaveDays,
      '',
      '',
      '',
      '',
    ]);

    const leaveWs = XLSX.utils.aoa_to_sheet(leaveAoa);
    leaveWs['!cols'] = [
      { wch: 6 },
      { wch: 14 },
      { wch: 30 },
      { wch: 16 },
      { wch: 16 },
      { wch: 14 },
      { wch: 32 },
      { wch: 16 },
      { wch: 22 },
      { wch: 18 },
    ];
    XLSX.utils.book_append_sheet(wb, leaveWs, 'Chi tiết nghỉ phép');

    const cleanName = (profile.userName || 'NhanVien').replace(/[^a-zA-Z0-9_-]/g, '_');
    const fileName = `BaoCao_NhanSu_${cleanName}_${getFileTimestamp()}.xlsx`;
    XLSX.writeFile(wb, fileName);
  }
}

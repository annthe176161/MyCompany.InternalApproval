namespace MyCompany.InternalApproval.Employees;

// Tổng quan chỉ số nhân sự và đề xuất của toàn công ty
public class CompanyHrSummaryDto
{
    // Tổng số nhân viên
    public int TotalEmployees { get; set; }

    // Tổng số phòng ban đang hoạt động
    public int TotalDepartments { get; set; }

    // Thâm niên trung bình toàn công ty (tháng)
    public double CompanyAverageTenureMonths { get; set; }

    // Thâm niên trung bình toàn công ty định dạng tiếng Việt
    public string CompanyAverageTenureFormatted { get; set; } = string.Empty;

    // Tổng số đề xuất phát sinh trong năm
    public int TotalRequestsThisYear { get; set; }

    // Tổng số tiền đã giải ngân trong năm
    public decimal TotalDisbursedAmountThisYear { get; set; }
}


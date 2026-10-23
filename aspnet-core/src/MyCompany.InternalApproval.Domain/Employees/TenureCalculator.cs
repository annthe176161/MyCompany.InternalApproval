using System;

namespace MyCompany.InternalApproval.Employees;

/// <summary>
/// Tiện ích tính toán và định dạng thâm niên làm việc (Tenure / Seniority)
/// theo chuẩn tiếng Việt thân thiện ("2 năm 5 tháng", "8 tháng", "< 1 tháng", "1 năm").
/// </summary>
public static class TenureCalculator
{
    /// <summary>
    /// Xác định ngày vào làm hiệu lực (Effective Join Date).
    /// Ưu tiên JoinDate do người dùng cấu hình, nếu null tự động fallback về CreationTime của tài khoản.
    /// </summary>
    public static DateTime GetEffectiveJoinDate(DateTime? configuredJoinDate, DateTime userCreationTime)
    {
        return configuredJoinDate ?? userCreationTime;
    }

    /// <summary>
    /// Tính toán số năm và số tháng làm việc từ ngày bắt đầu đến thời điểm target (mặc định UtcNow).
    /// </summary>
    public static (int Years, int Months, int TotalMonths) Calculate(DateTime joinDate, DateTime? now = null)
    {
        var target = now ?? DateTime.UtcNow;
        if (target < joinDate)
        {
            return (0, 0, 0);
        }

        int years = target.Year - joinDate.Year;
        int months = target.Month - joinDate.Month;

        if (target.Day < joinDate.Day)
        {
            months--;
        }

        if (months < 0)
        {
            years--;
            months += 12;
        }

        if (years < 0)
        {
            years = 0;
            months = 0;
        }

        int totalMonths = years * 12 + months;
        return (years, months, totalMonths);
    }

    /// <summary>
    /// Định dạng thâm niên theo chuẩn tiếng Việt thân thiện:
    /// - "2 năm 5 tháng"
    /// - "8 tháng"
    /// - "< 1 tháng"
    /// - "1 năm"
    /// </summary>
    public static string FormatVietnamese(int years, int months)
    {
        if (years <= 0 && months <= 0)
        {
            return "< 1 tháng";
        }

        if (years <= 0)
        {
            return $"{months} tháng";
        }

        if (months <= 0)
        {
            return $"{years} năm";
        }

        return $"{years} năm {months} tháng";
    }

    /// <summary>
    /// Tính toán và định dạng trực tiếp từ JoinDate.
    /// </summary>
    public static string CalculateAndFormat(DateTime joinDate, DateTime? now = null)
    {
        var (years, months, _) = Calculate(joinDate, now);
        return FormatVietnamese(years, months);
    }

    /// <summary>
    /// Định dạng số tháng trung bình thành chuỗi tiếng Việt.
    /// </summary>
    public static string FormatAverageMonths(double averageMonths)
    {
        if (averageMonths <= 0)
        {
            return "< 1 tháng";
        }

        int totalM = (int)Math.Round(averageMonths);
        int years = totalM / 12;
        int months = totalM % 12;

        return FormatVietnamese(years, months);
    }
}

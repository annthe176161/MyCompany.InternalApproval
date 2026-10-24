using System;

namespace MyCompany.InternalApproval.Employees;

// Tiện ích tính toán và định dạng thâm niên làm việc theo chuẩn tiếng Việt
public static class TenureCalculator
{
    // Xác định ngày vào làm hiệu lực (ưu tiên JoinDate, nếu chưa có thì lấy CreationTime)
    public static DateTime GetEffectiveJoinDate(DateTime? configuredJoinDate, DateTime userCreationTime)
    {
        return configuredJoinDate ?? userCreationTime;
    }

    // Tính toán số năm, số tháng lẻ và tổng số tháng làm việc tính đến thời điểm hiện tại
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

    // Định dạng chuỗi thâm niên tiếng Việt ("2 năm 5 tháng", "8 tháng", "< 1 tháng", "1 năm")
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

    // Tính toán và định dạng trực tiếp từ ngày vào làm
    public static string CalculateAndFormat(DateTime joinDate, DateTime? now = null)
    {
        var (years, months, _) = Calculate(joinDate, now);
        return FormatVietnamese(years, months);
    }

    // Định dạng số tháng trung bình sang chuỗi tiếng Việt
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

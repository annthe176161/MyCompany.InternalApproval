using System;
using System.Globalization;
using System.Text.RegularExpressions;
using MyCompany.InternalApproval.Requests;

namespace MyCompany.InternalApproval.Employees;

/// <summary>
/// Trích xuất dữ liệu timeline nghỉ phép đa tầng:
/// 1. Tầng 1: Dữ liệu ngày tháng có cấu trúc (StartDate, EndDate trong Request).
/// 2. Tầng 2: Bóc tách regex từ tiêu đề và nội dung tiếng Việt ("từ ngày dd/MM đến ngày dd/MM", "X ngày").
/// 3. Tầng 3: Cơ chế dự phòng an toàn theo CreationTime.
/// </summary>
public static class LeaveTimelineExtractor
{
    private static readonly Regex DateRangeRegex = new Regex(
        @"từ\s+(?:(?:ngày|khoảng)\s+)?(\d{1,2})[/-](\d{1,2})(?:[/-](\d{2,4}))?\s+(?:đến|tới)(?:\s+hết)?(?:\s+ngày)?\s+(\d{1,2})[/-](\d{1,2})(?:[/-](\d{2,4}))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SingleDateRegex = new Regex(
        @"(?:vào\s+ngày|ngày)\s+(\d{1,2})[/-](\d{1,2})(?:[/-](\d{2,4}))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DaysCountRegex = new Regex(
        @"(?:nghỉ|xin nghỉ)?\s*(\d+(?:[.,]\d+)?)\s*ngày",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static LeaveData Extract(Request request)
    {
        return Extract(request.Title, request.Description, request.CreationTime, request.StartDate, request.EndDate);
    }

    public static LeaveData Extract(
        string title,
        string? description,
        DateTime creationTime,
        DateTime? structuredStartDate = null,
        DateTime? structuredEndDate = null)
    {
        // 1. Tầng 1: Dữ liệu có cấu trúc từ Request
        if (structuredStartDate.HasValue && structuredEndDate.HasValue)
        {
            var start = structuredStartDate.Value.Date;
            var end = structuredEndDate.Value.Date;
            if (end < start)
            {
                end = start;
            }

            double days = (end - start).TotalDays + 1;
            var reason = !string.IsNullOrWhiteSpace(description) ? description.Trim() : title.Trim();
            return new LeaveData(start, end, days, reason);
        }

        var text = $"{title} {description ?? string.Empty}".Trim();

        // 2. Tầng 2: Phân tích cú pháp văn bản qua Regex
        // 2.1. Thử bóc tách khoảng thời gian "từ dd/MM đến dd/MM"
        var rangeMatch = DateRangeRegex.Match(text);
        if (rangeMatch.Success)
        {
            try
            {
                int d1 = int.Parse(rangeMatch.Groups[1].Value);
                int m1 = int.Parse(rangeMatch.Groups[2].Value);
                int y1 = rangeMatch.Groups[3].Success ? ParseYear(rangeMatch.Groups[3].Value, creationTime.Year) : creationTime.Year;

                int d2 = int.Parse(rangeMatch.Groups[4].Value);
                int m2 = int.Parse(rangeMatch.Groups[5].Value);
                int y2 = rangeMatch.Groups[6].Success ? ParseYear(rangeMatch.Groups[6].Value, y1) : y1;

                var start = new DateTime(y1, m1, d1);
                var end = new DateTime(y2, m2, d2);
                if (end < start)
                {
                    end = start;
                }

                double days = (end - start).TotalDays + 1;
                var reason = !string.IsNullOrWhiteSpace(description) ? description.Trim() : title.Trim();
                return new LeaveData(start, end, days, reason);
            }
            catch
            {
                // Nếu phân tích ngày lỗi định dạng, tiếp tục thử tầng kế tiếp
            }
        }

        // 2.2. Thử bóc tách số ngày "X ngày"
        double parsedDays = 1.0;
        var daysMatch = DaysCountRegex.Match(text);
        if (daysMatch.Success)
        {
            var rawDays = daysMatch.Groups[1].Value.Replace(',', '.');
            if (double.TryParse(rawDays, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) && d > 0)
            {
                parsedDays = d;
            }
        }

        // 2.3. Thử bóc tách ngày đơn lẻ
        var singleMatch = SingleDateRegex.Match(text);
        if (singleMatch.Success)
        {
            try
            {
                int d = int.Parse(singleMatch.Groups[1].Value);
                int m = int.Parse(singleMatch.Groups[2].Value);
                int y = singleMatch.Groups[3].Success ? ParseYear(singleMatch.Groups[3].Value, creationTime.Year) : creationTime.Year;

                var start = new DateTime(y, m, d);
                int daysSpan = Math.Max(1, (int)Math.Ceiling(parsedDays));
                var end = start.AddDays(daysSpan - 1);
                var reason = !string.IsNullOrWhiteSpace(description) ? description.Trim() : title.Trim();
                return new LeaveData(start, end, parsedDays, reason);
            }
            catch
            {
                // Bỏ qua lỗi parse ngày
            }
        }

        // 3. Tầng 3: Dự phòng an toàn theo CreationTime
        var fallbackStart = (structuredStartDate ?? creationTime).Date;
        var fallbackEnd = structuredEndDate?.Date ?? fallbackStart.AddDays(Math.Max(1, (int)Math.Ceiling(parsedDays)) - 1);
        if (fallbackEnd < fallbackStart)
        {
            fallbackEnd = fallbackStart;
        }

        var fallbackReason = !string.IsNullOrWhiteSpace(description) ? description.Trim() : title.Trim();
        return new LeaveData(fallbackStart, fallbackEnd, parsedDays, fallbackReason);
    }

    private static int ParseYear(string yearStr, int fallbackYear)
    {
        if (int.TryParse(yearStr, out int y))
        {
            if (y < 100)
            {
                return 2000 + y;
            }
            return y;
        }
        return fallbackYear;
    }
}

public record LeaveData(DateTime StartDate, DateTime EndDate, double Days, string Reason);

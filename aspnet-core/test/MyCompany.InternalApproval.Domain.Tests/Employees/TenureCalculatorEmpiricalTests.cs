using System;
using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MyCompany.InternalApproval.Employees;

public class TenureCalculatorEmpiricalTests
{
    [Fact]
    public void Verify_Vietnamese_Formatting_Standard_Cases()
    {
        var baseDate = new DateTime(2026, 10, 22);

        // 2 năm 5 tháng
        var t1 = TenureCalculator.Calculate(new DateTime(2024, 5, 15), baseDate);
        t1.Years.ShouldBe(2);
        t1.Months.ShouldBe(5);
        TenureCalculator.FormatVietnamese(t1.Years, t1.Months).ShouldBe("2 năm 5 tháng");

        // 8 tháng
        var t2 = TenureCalculator.Calculate(new DateTime(2026, 2, 15), baseDate);
        t2.Years.ShouldBe(0);
        t2.Months.ShouldBe(8);
        TenureCalculator.FormatVietnamese(t2.Years, t2.Months).ShouldBe("8 tháng");

        // < 1 tháng
        var t3 = TenureCalculator.Calculate(new DateTime(2026, 10, 5), baseDate);
        t3.Years.ShouldBe(0);
        t3.Months.ShouldBe(0);
        TenureCalculator.FormatVietnamese(t3.Years, t3.Months).ShouldBe("< 1 tháng");

        // 1 năm
        var t4 = TenureCalculator.Calculate(new DateTime(2025, 10, 22), baseDate);
        t4.Years.ShouldBe(1);
        t4.Months.ShouldBe(0);
        TenureCalculator.FormatVietnamese(t4.Years, t4.Months).ShouldBe("1 năm");

        // 3 năm
        var t5 = TenureCalculator.Calculate(new DateTime(2023, 10, 22), baseDate);
        t5.Years.ShouldBe(3);
        t5.Months.ShouldBe(0);
        TenureCalculator.FormatVietnamese(t5.Years, t5.Months).ShouldBe("3 năm");
    }

    [Fact]
    public void Verify_Leap_Year_And_Day29_Calculations()
    {
        var leapDayJoin = new DateTime(2024, 2, 29);

        // Same day -> < 1 tháng
        var c1 = TenureCalculator.Calculate(leapDayJoin, new DateTime(2024, 2, 29));
        c1.TotalMonths.ShouldBe(0);
        TenureCalculator.FormatVietnamese(c1.Years, c1.Months).ShouldBe("< 1 tháng");

        // Exactly 1 non-leap year later on Feb 28 (day 28 < day 29) -> 11 tháng
        var c2 = TenureCalculator.Calculate(leapDayJoin, new DateTime(2025, 2, 28));
        c2.Years.ShouldBe(0);
        c2.Months.ShouldBe(11);
        c2.TotalMonths.ShouldBe(11);
        TenureCalculator.FormatVietnamese(c2.Years, c2.Months).ShouldBe("11 tháng");

        // Non-leap year March 01 -> 1 năm tròn
        var c3 = TenureCalculator.Calculate(leapDayJoin, new DateTime(2025, 3, 1));
        c3.Years.ShouldBe(1);
        c3.Months.ShouldBe(0);
        c3.TotalMonths.ShouldBe(12);
        TenureCalculator.FormatVietnamese(c3.Years, c3.Months).ShouldBe("1 năm");

        // 4 years later, leap year Feb 28 -> 3 năm 11 tháng
        var c4 = TenureCalculator.Calculate(leapDayJoin, new DateTime(2028, 2, 28));
        c4.Years.ShouldBe(3);
        c4.Months.ShouldBe(11);
        TenureCalculator.FormatVietnamese(c4.Years, c4.Months).ShouldBe("3 năm 11 tháng");

        // 4 years later, leap year Feb 29 -> 4 năm tròn
        var c5 = TenureCalculator.Calculate(leapDayJoin, new DateTime(2028, 2, 29));
        c5.Years.ShouldBe(4);
        c5.Months.ShouldBe(0);
        TenureCalculator.FormatVietnamese(c5.Years, c5.Months).ShouldBe("4 năm");

        // 4 years later, leap year March 01 -> 4 năm tròn (0 months)
        var c6 = TenureCalculator.Calculate(leapDayJoin, new DateTime(2028, 3, 1));
        c6.Years.ShouldBe(4);
        c6.Months.ShouldBe(0);
        TenureCalculator.FormatVietnamese(c6.Years, c6.Months).ShouldBe("4 năm");
    }

    [Fact]
    public void Verify_Month_End_Calendar_Boundaries()
    {
        // Jan 31 -> Feb 28 (common year 2026, 28 days in Feb)
        var jan31 = new DateTime(2026, 1, 31);
        var feb28 = TenureCalculator.Calculate(jan31, new DateTime(2026, 2, 28));
        feb28.TotalMonths.ShouldBe(0);
        TenureCalculator.FormatVietnamese(feb28.Years, feb28.Months).ShouldBe("< 1 tháng");

        // Jan 31 -> Mar 01
        var mar01 = TenureCalculator.Calculate(jan31, new DateTime(2026, 3, 1));
        mar01.Years.ShouldBe(0);
        mar01.Months.ShouldBe(1);
        TenureCalculator.FormatVietnamese(mar01.Years, mar01.Months).ShouldBe("1 tháng");

        // Jan 31 -> Mar 31
        var mar31 = TenureCalculator.Calculate(jan31, new DateTime(2026, 3, 31));
        mar31.Years.ShouldBe(0);
        mar31.Months.ShouldBe(2);
        TenureCalculator.FormatVietnamese(mar31.Years, mar31.Months).ShouldBe("2 tháng");

        // Mar 31 -> Apr 30 (30-day month)
        var mar31_2026 = new DateTime(2026, 3, 31);
        var apr30 = TenureCalculator.Calculate(mar31_2026, new DateTime(2026, 4, 30));
        apr30.TotalMonths.ShouldBe(0);
        TenureCalculator.FormatVietnamese(apr30.Years, apr30.Months).ShouldBe("< 1 tháng");

        // Mar 31 -> May 01
        var may01 = TenureCalculator.Calculate(mar31_2026, new DateTime(2026, 5, 1));
        may01.Years.ShouldBe(0);
        may01.Months.ShouldBe(1);
        TenureCalculator.FormatVietnamese(may01.Years, may01.Months).ShouldBe("1 tháng");

        // Consecutive 31-day months (Jul 31 -> Aug 31)
        var jul31 = new DateTime(2026, 7, 31);
        var aug31 = TenureCalculator.Calculate(jul31, new DateTime(2026, 8, 31));
        aug31.Years.ShouldBe(0);
        aug31.Months.ShouldBe(1);
        TenureCalculator.FormatVietnamese(aug31.Years, aug31.Months).ShouldBe("1 tháng");

        // Dec 31 -> Jan 01 (Year boundary)
        var dec31 = new DateTime(2025, 12, 31);
        var jan01 = TenureCalculator.Calculate(dec31, new DateTime(2026, 1, 1));
        jan01.TotalMonths.ShouldBe(0);
        TenureCalculator.FormatVietnamese(jan01.Years, jan01.Months).ShouldBe("< 1 tháng");

        // Dec 31 -> Jan 31 (Exact 1 month)
        var jan31_next = TenureCalculator.Calculate(dec31, new DateTime(2026, 1, 31));
        jan31_next.Years.ShouldBe(0);
        jan31_next.Months.ShouldBe(1);
        TenureCalculator.FormatVietnamese(jan31_next.Years, jan31_next.Months).ShouldBe("1 tháng");
    }

    [Fact]
    public void Verify_Future_Dates_And_Same_Day()
    {
        var now = new DateTime(2026, 10, 22, 12, 0, 0);

        // Same day, earlier time
        var sameDay = TenureCalculator.Calculate(now.Date, now);
        sameDay.TotalMonths.ShouldBe(0);
        TenureCalculator.FormatVietnamese(sameDay.Years, sameDay.Months).ShouldBe("< 1 tháng");

        // Future date (tomorrow)
        var tomorrow = now.AddDays(1);
        var future1 = TenureCalculator.Calculate(tomorrow, now);
        future1.TotalMonths.ShouldBe(0);
        TenureCalculator.FormatVietnamese(future1.Years, future1.Months).ShouldBe("< 1 tháng");

        // Future date (1 year later)
        var futureYear = now.AddYears(1);
        var future2 = TenureCalculator.Calculate(futureYear, now);
        future2.TotalMonths.ShouldBe(0);
        TenureCalculator.FormatVietnamese(future2.Years, future2.Months).ShouldBe("< 1 tháng");
    }

    [Fact]
    public void Verify_Null_Fallback_Logic()
    {
        var creationTime = new DateTime(2025, 6, 1, 8, 0, 0);
        var explicitJoin = new DateTime(2024, 1, 15);

        // Null configured join date -> falls back to creationTime
        var eff1 = TenureCalculator.GetEffectiveJoinDate(null, creationTime);
        eff1.ShouldBe(creationTime);

        // Explicit configured join date -> takes explicit
        var eff2 = TenureCalculator.GetEffectiveJoinDate(explicitJoin, creationTime);
        eff2.ShouldBe(explicitJoin);
    }

    [Fact]
    public void Verify_Average_Tenure_Formatting()
    {
        TenureCalculator.FormatAverageMonths(0.0).ShouldBe("< 1 tháng");
        TenureCalculator.FormatAverageMonths(-1.0).ShouldBe("< 1 tháng");
        TenureCalculator.FormatAverageMonths(0.4).ShouldBe("< 1 tháng");
        // In .NET Math.Round default is ToEven (banker's rounding): 0.5 -> 0 ("< 1 tháng"), 1.5 -> 2 ("2 tháng")
        TenureCalculator.FormatAverageMonths(0.5).ShouldBe("< 1 tháng");
        TenureCalculator.FormatAverageMonths(0.51).ShouldBe("1 tháng");
        TenureCalculator.FormatAverageMonths(0.6).ShouldBe("1 tháng");
        TenureCalculator.FormatAverageMonths(1.5).ShouldBe("2 tháng");
        TenureCalculator.FormatAverageMonths(7.8).ShouldBe("8 tháng");
        TenureCalculator.FormatAverageMonths(11.4).ShouldBe("11 tháng");
        TenureCalculator.FormatAverageMonths(11.5).ShouldBe("1 năm"); // 11.5 rounds to 12 -> 1 year
        TenureCalculator.FormatAverageMonths(24.0).ShouldBe("2 năm");
        TenureCalculator.FormatAverageMonths(29.0).ShouldBe("2 năm 5 tháng");
        TenureCalculator.FormatAverageMonths(186.0).ShouldBe("15 năm 6 tháng");
    }

    [Fact]
    public void Stress_Test_Tenure_Invariant_Harness_10000_Iterations()
    {
        var regex = new Regex(@"^(< 1 tháng|\d+ năm|\d+ tháng|\d+ năm \d+ tháng)$");
        var rnd = new Random(1337);
        var baseStart = new DateTime(1980, 1, 1);
        var totalDays = (new DateTime(2040, 1, 1) - baseStart).Days;

        for (int i = 0; i < 10000; i++)
        {
            var d1 = baseStart.AddDays(rnd.Next(totalDays));
            var d2 = baseStart.AddDays(rnd.Next(totalDays));

            var res = TenureCalculator.Calculate(d1, d2);
            var fmt = TenureCalculator.FormatVietnamese(res.Years, res.Months);

            // Invariants
            res.Years.ShouldBeGreaterThanOrEqualTo(0);
            res.Months.ShouldBeInRange(0, 11);
            res.TotalMonths.ShouldBe(res.Years * 12 + res.Months);
            fmt.ShouldNotBeNullOrWhiteSpace();
            regex.IsMatch(fmt).ShouldBeTrue();

            if (d2 <= d1)
            {
                res.TotalMonths.ShouldBe(0);
                fmt.ShouldBe("< 1 tháng");
            }
        }
    }

    [Fact]
    public void Verify_Extreme_And_Multi_Decade_Tenure()
    {
        var target = new DateTime(2026, 10, 22);

        // 50 years
        var t50 = TenureCalculator.Calculate(new DateTime(1976, 10, 22), target);
        t50.Years.ShouldBe(50);
        t50.Months.ShouldBe(0);
        t50.TotalMonths.ShouldBe(600);
        TenureCalculator.FormatVietnamese(t50.Years, t50.Months).ShouldBe("50 năm");

        // 75 years and 8 months
        var t75 = TenureCalculator.Calculate(new DateTime(1951, 2, 22), target);
        t75.Years.ShouldBe(75);
        t75.Months.ShouldBe(8);
        t75.TotalMonths.ShouldBe(75 * 12 + 8);
        TenureCalculator.FormatVietnamese(t75.Years, t75.Months).ShouldBe("75 năm 8 tháng");
    }

    [Fact]
    public void Verify_All_12_Month_End_Boundaries_Matrix()
    {
        // For year 2026 (common year)
        var daysInMonth = new[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        for (int m = 1; m <= 12; m++)
        {
            var endOfMonth = new DateTime(2026, m, daysInMonth[m - 1]);
            var nextDay = endOfMonth.AddDays(1);

            // Starting on end of month, evaluating next day -> TotalMonths must be 0 (< 1 tháng)
            var tenureNextDay = TenureCalculator.Calculate(endOfMonth, nextDay);
            tenureNextDay.TotalMonths.ShouldBe(0);
            TenureCalculator.FormatVietnamese(tenureNextDay.Years, tenureNextDay.Months).ShouldBe("< 1 tháng");

            // Evaluating 1 year later on same date
            if (m != 2 || daysInMonth[m - 1] != 29) // not leap day
            {
                var oneYearLater = new DateTime(2027, m, daysInMonth[m - 1]);
                var tenure1Yr = TenureCalculator.Calculate(endOfMonth, oneYearLater);
                tenure1Yr.Years.ShouldBe(1);
                tenure1Yr.Months.ShouldBe(0);
                TenureCalculator.FormatVietnamese(tenure1Yr.Years, tenure1Yr.Months).ShouldBe("1 năm");
            }
        }
    }

    [Fact]
    public void Stress_Test_Tenure_100_000_Iterations_Performance()
    {
        var regex = new Regex(@"^(< 1 tháng|\d+ năm|\d+ tháng|\d+ năm \d+ tháng)$");
        var rnd = new Random(9999);
        var baseDate = new DateTime(1990, 1, 1);
        var maxDays = (new DateTime(2030, 1, 1) - baseDate).Days;

        for (int i = 0; i < 100000; i++)
        {
            var d1 = baseDate.AddDays(rnd.Next(maxDays));
            var d2 = baseDate.AddDays(rnd.Next(maxDays));

            var result = TenureCalculator.Calculate(d1, d2);
            var fmt = TenureCalculator.FormatVietnamese(result.Years, result.Months);

            result.Years.ShouldBeGreaterThanOrEqualTo(0);
            result.Months.ShouldBeInRange(0, 11);
            result.TotalMonths.ShouldBe(result.Years * 12 + result.Months);
            regex.IsMatch(fmt).ShouldBeTrue();
        }
    }
}

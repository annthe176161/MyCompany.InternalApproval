using System;

namespace MyCompany.InternalApproval.Employees;

// Dữ liệu bóc tách đợt nghỉ phép (ngày bắt đầu, ngày kết thúc, số ngày nghỉ và lý do)
public record LeaveData(
    DateTime StartDate,
    DateTime EndDate,
    double Days,
    string Reason
);


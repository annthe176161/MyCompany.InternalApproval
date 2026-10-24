namespace MyCompany.InternalApproval.Common;

// Cấu trúc phản hồi chuẩn toàn hệ thống (chuẩn HTTP 200 kèm envelope)
public class ApiResponse<T>
{
    // Cờ trạng thái thực thi (true: thành công, false: thất bại)
    public bool Success { get; set; }

    // Mã lỗi hệ thống hoặc nghiệp vụ (ví dụ: InternalApproval:00003)
    public string? Code { get; set; }

    // Thông điệp phản hồi cho người dùng
    public string? Message { get; set; }

    // Dữ liệu trả về khi thành công
    public T? Data { get; set; }

    // Phương thức tĩnh tạo phản hồi thành công
    public static ApiResponse<T> Ok(T? data, string message = "Thành công")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Code = null,
            Message = message,
            Data = data
        };
    }

    // Phương thức tĩnh tạo phản hồi lỗi
    public static ApiResponse<T> Fail(string code, string message)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Code = code,
            Message = message,
            Data = default
        };
    }
}


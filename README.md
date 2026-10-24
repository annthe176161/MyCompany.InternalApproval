# TÀI LIỆU KIẾN TRÚC VÀ QUY CHUẨN PHÁT TRIỂN DỰ ÁN INTERNAL APPROVAL

Tài liệu này là sổ tay hướng dẫn chính thức (Onboarding & Coding Convention) cho dự án Hệ thống Quản lý và Phê duyệt Đề xuất Nội bộ (MyCompany.InternalApproval). Khi bảo trì hoặc phát triển thêm bất kỳ module mới nào, lập trình viên bắt buộc tuân thủ đúng kiến trúc, cấu trúc thư mục, quy tắc đặt tên và mẫu xử lý lỗi được quy định trong tài liệu này.


## PHẦN 1: TỔNG QUAN KIẾN TRÚC HỆ THỐNG

Hệ thống được xây dựng theo kiến trúc Domain-Driven Design (DDD) trên nền tảng ABP Framework (.NET 8) kết hợp với Angular và thư viện giao diện NG-ZORRO (Ant Design).

### 1.1. Cấu trúc các tầng phía Backend (aspnet-core/src/)

| Tầng (Project) | Thư mục mã nguồn | Trách nhiệm chính | Quy tắc bắt buộc |
| --- | --- | --- | --- |
| Domain.Shared | MyCompany.InternalApproval.Domain.Shared | Chứa Enum, mã lỗi InternalApprovalDomainErrorCodes, hằng số Consts và từ điển đa ngôn ngữ (Localization vi.json, en.json). | Không phụ thuộc vào bất kỳ tầng nào khác. Mọi module có phân loại hay trạng thái đều phải khai báo Enum tại đây. |
| Domain | MyCompany.InternalApproval.Domain | Chứa AggregateRoot, Entity (kế thừa FullAuditedAggregateRoot<Guid>), DomainService (Manager), Interface Repository, Event Data (Eto) và DataSeed. | Mọi thuộc tính trong Entity phải dùng private set. Mọi quy tắc nghiệp vụ cốt lõi (Invariants) phải nằm tại Entity hoặc DomainService. |
| Application.Contracts | MyCompany.InternalApproval.Application.Contracts | Chứa DTO, Input DTO, Interface AppService, ApiResponse envelope và cây định nghĩa phân quyền (Permissions). | Đặt toàn bộ file DTO và Interface trực tiếp trong thư mục module (Requests/, Departments/, Employees/, Notifications/), không tạo thêm thư mục con Dtos/. |
| Application | MyCompany.InternalApproval.Application | Cài đặt AppService, cấu hình AutoMapperProfile, xử lý EventHandler và BackgroundJob. | AppService chỉ điều phối luồng nghiệp vụ, kiểm tra quyền và gọi DomainService/Repository, không viết logic tính toán phức tạp trực tiếp tại đây. |
| EntityFrameworkCore | MyCompany.InternalApproval.EntityFrameworkCore | Chứa DbContext, cấu hình bảng Fluent API, Migrations và cài đặt EfCoreRepository cho từng module. | Mỗi module có truy vấn riêng hoặc cần Include bảng tránh lỗi N+1 query đều phải có thư mục riêng và cài đặt EfCoreRepository tương ứng. |
| HttpApi & HttpApi.Host | MyCompany.InternalApproval.HttpApi / HttpApi.Host | Chứa các Controller tường minh kế thừa InternalApprovalController, cấu hình Swagger, xác thực OpenIddict và bộ lọc ngoại lệ UnifiedExceptionFilter. | Đảm bảo đường dẫn API thống nhất chuẩn /api/app/<module>. Thay thế bộ lọc lỗi mặc định của ABP để luôn trả về HTTP 200 OK kèm ApiResponse. |

### 1.2. Cấu trúc các thư mục phía Frontend (angular/src/app/)

| Thư mục | Trách nhiệm và vai trò | Quy chuẩn áp dụng |
| --- | --- | --- |
| proxy/ | Chứa các Service, Model và Enum TypeScript gọi API xuống Backend. | Được chia theo từng thư mục module (requests/, departments/, employees/, notifications/). |
| shared/models/ | Chứa các Interface giao diện dùng chung trên nhiều Component (ColumnConfig, RequestStatusMeta, DashboardReportData...). | Mỗi Interface được tách riêng thành 1 file .model.ts độc lập và xuất ra qua index.ts. |
| shared/constants/ | Chứa các danh mục hằng số cấu hình toàn hệ thống (request-status, request-type, department-position, permission-name). | Không viết cứng (hardcode) danh sách trạng thái, màu sắc hay tên quyền ở từng Component. |
| shared/utils/ | Chứa các hàm tiện ích tái sử dụng: error.util.ts, date.util.ts, currency.util.ts, avatar.util.ts. | Tất cả Component phải gọi hàm tiện ích từ đây, không viết trùng lặp logic xử lý. |
| shared/services/ | Chứa các dịch vụ chia sẻ toàn ứng dụng: CurrentUserContextService (quản lý người dùng, phân quyền), ExcelExportService (xuất file Excel). | Dùng chung một nguồn dữ liệu trạng thái đăng nhập và xuất báo cáo cho toàn dự án. |
| shared/shared.module.ts | Khai báo và export toàn bộ các module UI của Angular, ABP và NG-ZORRO. | Các module chức năng chỉ cần import SharedModule, không khai báo lặp lại từng module nhỏ của NG-ZORRO. |
| Các module chức năng (home/, request/, department/, employee/, identity/) | Quản lý từng màn hình nghiệp vụ độc lập, áp dụng cơ chế Lazy Loading. | Giao diện rõ ràng, đồng bộ bảng dữ liệu nz-table, phân trang backend và modal thao tác. |


## PHẦN 2: QUY CHUẨN CLEAN CODE CỦA CÔNG TY (BẮT BUỘC TUÂN THỦ)

1. Nguyên tắc 1 Class / 1 Interface / 1 Enum trên 1 File
   - Tuyệt đối không gộp nhiều class, record, enum hoặc interface vào chung một file .cs hay .ts.
   - Tên file phải đặt trùng khớp với tên class hoặc interface bên trong (ví dụ: LeaveTimelineItemDto đặt trong file LeaveTimelineItemDto.cs).

2. Quy chuẩn viết Comment trong code
   - Chỉ sử dụng comment một dòng bằng dấu // với nội dung tiếng Việt ngắn gọn, rõ nghĩa để giải thích mục đích của class, phương thức hoặc đoạn xử lý nghiệp vụ quan trọng.
   - Không sử dụng comment dạng khối XML /// <summary> trong C# hoặc JSDoc /** ... */ trong TypeScript.

3. Đóng gói trạng thái trong Domain (Private Setters)
   - Trong các Entity như Request, RequestComment, Department, DepartmentMember, mọi thuộc tính đều phải để private set.
   - Khi cần thay đổi giá trị, phải gọi qua các hàm nghiệp vụ rõ tên (SetTitle, SetTypeAndAmount, SetLeaveDates, Submit, Approve, Reject) có kiểm tra dữ liệu đầu vào chặt chẽ.

4. Chia nhỏ các Class lớn và Tái sử dụng code (DRY)
   - Khi một AppService có nhiều mảng nghiệp vụ lớn (như EmployeeAppService), phải tách thành các file partial class theo từng nhóm chức năng:
     + EmployeeAppService.cs: Xử lý hồ sơ nhân sự, ngày vào làm, lịch sử đề xuất.
     + EmployeeAppService.AccessControl.cs: Xử lý phân quyền truy cập 3 cấp (Admin, Trưởng phòng, Nhân viên).
     + EmployeeAppService.Analytics.cs: Xử lý thống kê nhân sự toàn công ty và báo cáo nghỉ phép.
   - Ở phía Frontend, mọi xử lý định dạng tiền, ngày tháng, tính số ngày nghỉ, lấy màu Avatar, kiểm tra quyền hiện tại phải gọi từ shared/utils và CurrentUserContextService, không viết lặp lại ở từng Component.

5. Quy chuẩn màu sắc và Giao diện (UI Theme)
   - Sử dụng tông màu xanh dương doanh nghiệp chủ đạo (#2563eb / #1d4ed8) kết hợp màu xám trung tính.
   - Không sử dụng màu hồng (#f72585) của theme mặc định cho bất kỳ hiệu ứng di chuột (hover), đường viền hay icon menu nào.


## PHẦN 3: QUY CHUẨN XỬ LÝ LỖI VÀ FORM BÁO LỖI

### 3.1. Cấu trúc chuẩn phản hồi API của công ty (Luôn trả về mã HTTP 200 OK)

Quy định bắt buộc: Mọi API nghiệp vụ (/api/app/...) khi xảy ra lỗi nghiệp vụ, lỗi kiểm tra dữ liệu hoặc lỗi phân quyền đều phải trả về mã trạng thái HTTP 200 OK kèm đối tượng JSON chuẩn (ApiResponse envelope) gồm đúng 4 trường: success, code, message, data. Tuyệt đối không để API văng mã lỗi HTTP 500, HTTP 403 hay HTTP 400 ra ngoài Client.

| Tiêu chí so sánh | Chuẩn của Công ty đề ra | Chuẩn mặc định ABP Framework (Không dùng) |
| --- | --- | --- |
| Mã HTTP Status | Luôn là 200 OK | Trả về 403 Forbidden, 400 Bad Request, hoặc 500 Internal Server Error |
| Cấu trúc JSON | Gồm 4 trường phẳng: success, code, message, data | Lồng nhau trong đối tượng error: { code, message, details, data, validationErrors } |
| Ảnh hưởng phía Client | Luồng dữ liệu chạy mượt mà, không văng lỗi đỏ ngắt quãng console | Bị coi là sự cố mạng/hạ tầng, browser văng exception đỏ console |
| Hiển thị thông báo | Frontend đọc thẳng res.message hoặc dùng extractErrorMessage hiển thị thông báo | Frontend phải bóc tách sâu nhiều tầng (err.error.error.message...) |

#### Cấu trúc mẫu phản hồi khi gặp LỖI (Chuẩn của công ty):
```json
{
  "success": false,
  "code": "InternalApproval:00003",
  "message": "Bạn không được phép tự phê duyệt đề xuất của chính mình!",
  "data": null
}
```

#### Cấu trúc mặc định của ABP Framework (Công ty KHÔNG sử dụng):
```json
{
  "error": {
    "code": "InternalApproval:00003",
    "message": "Bạn không được phép tự phê duyệt đề xuất của chính mình!",
    "details": null,
    "data": {},
    "validationErrors": null
  }
}
```

#### Cấu trúc mẫu phản hồi khi THÀNH CÔNG:
```json
{
  "success": true,
  "code": null,
  "message": "Thành công",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Đề xuất mua sắm thiết bị",
    "status": 1
  }
}
```

#### Cách cài đặt phía Backend (.NET / ABP):
- Hệ thống cài đặt bộ lọc UnifiedExceptionFilter (kế thừa AbpExceptionFilter) tại thư mục aspnet-core/src/MyCompany.InternalApproval.HttpApi.Host/Middleware/UnifiedExceptionFilter.cs.
- Bộ lọc được kích hoạt tự động với thuộc tính [Dependency(ReplaceServices = true)] để thay thế bộ lọc lỗi mặc định của ABP.
- Khi có bất kỳ Exception nào phát sinh từ các đường dẫn /api/app/*:
  1. Ghi log đầy đủ lỗi kèm StackTrace chi tiết vào hệ thống log máy chủ phục vụ lập trình viên điều tra, sửa lỗi.
  2. Chuyển đổi mã lỗi sang câu thông báo tiếng Việt tương ứng đã được cấu hình trong file Localization (vi.json).
  3. Gán context.HttpContext.Response.StatusCode = StatusCodes.Status200OK.
  4. Trả về đối tượng JSON đồng nhất { success = false, code, message, data = null }.
- Lớp ApiResponse<T> được định nghĩa tại Application.Contracts/Common/ApiResponse.cs hỗ trợ đóng gói dữ liệu phản hồi dạng type-safe khi cần thiết.

### 3.2. Bảng danh mục mã lỗi nghiệp vụ Backend (Domain.Shared)

| Mã lỗi hệ thống | Ý nghĩa nghiệp vụ | Thông báo hiển thị cho người dùng |
| --- | --- | --- |
| InternalApproval:00001 | Không phải người tạo đề xuất | Chỉ người tạo mới có quyền gửi phê duyệt đề xuất này! |
| InternalApproval:00002 | Trạng thái đề xuất không hợp lệ | Chỉ đề xuất ở trạng thái Nháp mới có thể gửi phê duyệt! |
| InternalApproval:00003 | Vi phạm quy tắc tự phê duyệt | Bạn không được phép tự phê duyệt đề xuất của chính mình! |
| InternalApproval:00004 | Sai trạng thái khi phê duyệt/từ chối | Chỉ đề xuất đang chờ duyệt mới có thể phê duyệt hoặc từ chối! |
| InternalApproval:00005 | Thiếu lý do từ chối | Vui lòng nhập lý do từ chối đề xuất! |
| InternalApproval:00006 | Số tiền không hợp lệ | Số tiền đề xuất phải lớn hơn 0 đối với loại mua sắm hoặc tạm ứng! |
| InternalApproval:00007 | Chỉnh sửa đơn không phải nháp | Chỉ đề xuất ở trạng thái Nháp mới được phép chỉnh sửa hoặc xóa! |
| InternalApproval:00008 | Thiếu khoảng thời gian nghỉ phép | Đề xuất nghỉ phép bắt buộc phải chọn ngày bắt đầu và ngày kết thúc! |
| InternalApproval:00009 | Ngày nghỉ phép không hợp lệ | Ngày kết thúc nghỉ phép không được nhỏ hơn ngày bắt đầu! |

- Sử dụng đúng loại Exception theo ngữ cảnh (UnifiedExceptionFilter sẽ tự động bắt và đóng gói):
  + Dùng BusinessException kèm mã lỗi trong InternalApprovalDomainErrorCodes khi vi phạm quy tắc nghiệp vụ Domain:
    `throw new BusinessException(InternalApprovalDomainErrorCodes.CannotApproveOwnRequest);`
  + Dùng UserFriendlyException khi cần trả về thông báo lỗi kiểm tra dữ liệu trực tiếp cho người dùng:
    `throw new UserFriendlyException($"Mã phòng ban '{input.Code}' đã tồn tại trong hệ thống.");`
  + Dùng AbpAuthorizationException khi người dùng truy cập dữ liệu vượt quá phạm vi phân quyền:
    `throw new AbpAuthorizationException("Bạn không có quyền truy cập thông tin nhân sự ngoài phạm vi quản lý.");`

### 3.3. Quy chuẩn hiển thị lỗi và kiểm tra Form phía Frontend (Angular)

1. Bóc tách và hiển thị lỗi khi gọi API:
   - Luôn sử dụng hàm extractErrorMessage từ shared/utils kết hợp với NzMessageService để hiển thị thông báo lỗi thân thiện:
```typescript
this.requestService.approve(id, input).subscribe({
  next: (res: any) => {
    if (res && res.success === false) {
      this.message.error(res.message || 'Không thể phê duyệt đề xuất!');
      return;
    }
    this.message.success('Phê duyệt đề xuất thành công!');
  },
  error: err => {
    this.message.error(extractErrorMessage(err, 'Không thể phê duyệt đề xuất!'));
  },
});
```

2. Kiểm tra hợp lệ Form (ReactiveFormsModule) trước khi gửi API:
   - Khi người dùng bấm Lưu mà form chưa hợp lệ, phải đánh dấu dirty để hiển thị thông báo lỗi dưới từng ô nhập:
```typescript
if (this.form.invalid) {
  Object.values(this.form.controls).forEach(control => {
    if (control.invalid) {
      control.markAsDirty();
      control.updateValueAndValidity({ onlySelf: true });
    }
  });
  return;
}
```
   - Trên file HTML, mỗi ô nhập liệu đặt trong thẻ nz-form-control kèm thuộc tính nzErrorTip tiếng Việt rõ ràng.


## PHẦN 4: TỔNG HỢP CÁC MODULE VÀ HẠNG MỤC ĐÃ CHUẨN HÓA TRONG DỰ ÁN

| Module | Nghiệp vụ cốt lõi | Các tính năng và quy chuẩn nổi bật |
| --- | --- | --- |
| Quản lý Đề xuất (Requests) | Quy trình xét duyệt 4 trạng thái cho 3 loại đề xuất (Nghỉ phép, Mua thiết bị, Tạm ứng). | Chọn khoảng ngày nghỉ tự động tính số ngày; chặn tự duyệt đơn mình; EventBus & Background Job gửi thông báo; trao đổi bình luận; xuất Excel. |
| Cơ cấu Phòng ban (Departments) | Quản lý danh sách phòng ban, bổ nhiệm Trưởng phòng, phân bổ nhân sự. | Cập nhật chức vụ thành viên với bộ danh mục chuẩn; tách đầy đủ IDepartmentRepository, EfCoreDepartmentRepository, DepartmentController. |
| Hồ sơ Nhân sự (Employees) | Quản lý thông tin công tác, thâm niên và thống kê ngày nghỉ phép. | Cơ chế phân quyền 3 cấp (Admin, Trưởng phòng, Nhân viên xem bản thân không bị 403); ngăn kéo chi tiết nhân viên 4 tab; báo cáo nghỉ phép theo năm và xuất Excel. |
| Thông báo & Quản trị Hệ thống | Chuông thông báo thời gian thực, quản lý Vai trò, Tài khoản và Phân quyền. | Chuông thông báo click mở ngay chi tiết đề xuất; giao diện phân quyền tiếng Việt; xóa bỏ các bảng và menu dư thừa không sử dụng (Tenants, Settings). |


## PHẦN 5: QUY TRÌNH 6 BƯỚC CHUẨN KHI THÊM MỘT MODULE MỚI

Khi cần phát triển thêm một module mới vào dự án (ví dụ: Module Quản lý Tài sản - Assets), lập trình viên thực hiện lần lượt theo 6 bước sau:

| Bước | Tầng thực hiện | Các công việc bắt buộc thực hiện |
| --- | --- | --- |
| Bước 1 | Domain.Shared | Tạo thư mục module; tạo các file Enum riêng; bổ sung mã lỗi vào InternalApprovalDomainErrorCodes.cs và câu dịch vào vi.json, en.json. |
| Bước 2 | Domain & EntityFrameworkCore | Tạo AggregateRoot kế thừa FullAuditedAggregateRoot<Guid> với private set; tạo Interface Repository; cấu hình Fluent API, Index trong DbContext; tạo EfCoreRepository và Migration. |
| Bước 3 | Application.Contracts | Khai báo quyền hạn mới trong Permissions; tạo thư mục module chứa DTO, Input DTO và Interface AppService (mỗi class 1 file riêng, không tạo thư mục con Dtos/). |
| Bước 4 | Application & HttpApi | Viết AppService điều phối nghiệp vụ; cấu hình AutoMapperProfile; viết Controller trong HttpApi/Controllers/; viết bài kiểm thử tích hợp (Integration Test). |
| Bước 5 | Frontend Angular | Cập nhật proxy service/model; tạo lazy module chỉ import SharedModule; tách models/ riêng; dùng CurrentUserContextService và extractErrorMessage. |
| Bước 6 | Kiểm thử & Kiểm soát chất lượng | Rà soát comment chỉ dùng dấu // tiếng Việt; chạy dotnet test vượt qua 100%; chạy npm run build không có lỗi trước khi commit và push Git. |

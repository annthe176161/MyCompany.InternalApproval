TÀI LIỆU KIẾN TRÚC VÀ QUY CHUẨN PHÁT TRIỂN DỰ ÁN INTERNAL APPROVAL

Tài liệu này là sổ tay hướng dẫn chính thức cho dự án Hệ thống Quản lý và Phê duyệt Đề xuất Nội bộ (MyCompany.InternalApproval). Khi bảo trì hoặc phát triển thêm bất kỳ module mới nào, lập trình viên bắt buộc tuân thủ đúng kiến trúc, cấu trúc thư mục, quy tắc đặt tên và mẫu xử lý lỗi được quy định dưới đây.


PHẦN 1: TỔNG QUAN KIẾN TRÚC HỆ THỐNG

Hệ thống được xây dựng theo kiến trúc Domain-Driven Design (DDD) trên nền tảng ABP Framework (.NET 8) kết hợp với Angular và thư viện giao diện NG-ZORRO (Ant Design).

1.1. Cấu trúc các tầng phía Backend (thư mục aspnet-core/src/)

1. Tầng Domain.Shared (MyCompany.InternalApproval.Domain.Shared)
   - Nhiệm vụ: Chứa các Enum, mã lỗi hệ thống (InternalApprovalDomainErrorCodes), các hằng số giới hạn độ dài (Consts) và file ngôn ngữ dịch thuật (Localization/InternalApproval/vi.json, en.json).
   - Quy tắc: Tầng này không phụ thuộc vào bất kỳ tầng nào khác. Mọi module nếu có trạng thái hoặc phân loại thì bắt buộc phải khai báo Enum tại đây.

2. Tầng Domain (MyCompany.InternalApproval.Domain)
   - Nhiệm vụ: Chứa các thực thể nghiệp vụ (Entity, AggregateRoot kế thừa FullAuditedAggregateRoot<Guid>), Domain Service (Manager), Interface Repository riêng của từng module, dữ liệu sự kiện (Eto) và DataSeedContributor.
   - Quy tắc: Các thuộc tính trong Entity phải dùng private set để bảo vệ dữ liệu. Mọi quy tắc nghiệp vụ cốt lõi (như cấm tự duyệt đơn của mình, kiểm tra ngày nghỉ hợp lệ) phải viết tại Entity hoặc Domain Service.

3. Tầng Application.Contracts (MyCompany.InternalApproval.Application.Contracts)
   - Nhiệm vụ: Chứa các lớp DTO, Input DTO, Interface của AppService và khai báo cây phân quyền (InternalApprovalPermissions).
   - Quy tắc: Tất cả các file DTO và Interface của một module phải đặt trực tiếp trong thư mục của module đó (ví dụ: Requests/, Departments/, Employees/, Notifications/). Không tạo thêm thư mục con Dtos/ bên trong để đảm bảo đồng bộ toàn dự án.

4. Tầng Application (MyCompany.InternalApproval.Application)
   - Nhiệm vụ: Chứa các lớp cài đặt AppService, cấu hình AutoMapper (InternalApprovalApplicationAutoMapperProfile), các EventHandler và BackgroundJob.
   - Quy tắc: AppService chỉ làm nhiệm vụ điều phối luồng dữ liệu, kiểm tra phân quyền và gọi xuống Domain Service hoặc Repository, không viết logic nghiệp vụ phức tạp trực tiếp tại đây.

5. Tầng EntityFrameworkCore (MyCompany.InternalApproval.EntityFrameworkCore)
   - Nhiệm vụ: Chứa InternalApprovalDbContext, cấu hình bảng Fluent API, Migrations và cài đặt các Custom Repository (EfCoreRepository).
   - Quy tắc: Mỗi module có truy vấn đặc thù hoặc cần gộp bảng (Include) để tránh lỗi N+1 query đều phải tách thư mục riêng (như Requests/, Departments/, Notifications/) và cài đặt EfCoreRepository tương ứng.

6. Tầng HttpApi và HttpApi.Host
   - Nhiệm vụ: Chứa các Controller tường minh (RequestController, DepartmentController, EmployeeController) kế thừa InternalApprovalController, cấu hình Swagger và xác thực OpenIddict.
   - Quy tắc: Đảm bảo các đường dẫn API rõ ràng, thống nhất theo chuẩn /api/app/<tên-module>.


1.2. Cấu trúc các thư mục phía Frontend (thư mục angular/src/app/)

1. Thư mục proxy/
   - Chứa các Service, Model và Enum TypeScript gọi API xuống Backend, được chia theo từng thư mục module: requests/, departments/, employees/, notifications/.

2. Thư mục shared/models/
   - Chứa các Interface dùng chung trên nhiều màn hình (như ColumnConfig, RequestStatusMeta, RequestTypeMeta, DashboardReportData). Mỗi Interface được tách riêng thành 1 file .model.ts và xuất ra qua file index.ts.

3. Thư mục shared/constants/
   - Chứa các cấu hình danh mục cố định dùng chung toàn hệ thống (request-status.constant.ts, request-type.constant.ts, department-position.constant.ts, permission-name.constant.ts). Không viết cứng (hardcode) danh sách trạng thái hay màu sắc lặp lại ở từng Component.

4. Thư mục shared/utils/
   - Chứa các hàm tiện ích dùng chung:
     + error.util.ts: Hàm extractErrorMessage bóc tách thông báo lỗi từ phản hồi API ABP.
     + date.util.ts: Các hàm tính số ngày nghỉ phép (calculateLeaveDays) và định dạng ngày giờ chuẩn Việt Nam.
     + currency.util.ts: Các hàm định dạng tiền tệ VNĐ (formatMoneyVnd, formatCurrencyInput, parseCurrencyInput).
     + avatar.util.ts: Các hàm lấy chữ cái viết tắt tên nhân viên, tạo màu Avatar cố định và hiển thị nhãn thâm niên công tác.

5. Thư mục shared/services/
   - Chứa các dịch vụ dùng chung toàn ứng dụng:
     + CurrentUserContextService: Quản lý thông tin người dùng đang đăng nhập (userId, userName, fullName, email, roles) và kiểm tra nhanh vai trò isAdmin(), isApprover().
     + ExcelExportService: Xuất các báo cáo danh sách đề xuất, tổng quan Dashboard và báo cáo ngày nghỉ phép ra file Excel (.xlsx).

6. File shared/shared.module.ts
   - Tập trung và export toàn bộ các module giao diện dùng chung của Angular, ABP và bộ thư viện NG-ZORRO. Các module chức năng (RequestModule, DepartmentModule, EmployeeModule, HomeModule, IdentityManagementModule) chỉ cần import SharedModule và Routing Module, không cần khai báo lặp lại từng module con của NG-ZORRO.

7. Các thư mục module chức năng (home/, request/, department/, employee/, identity/)
   - Mỗi module quản lý một màn hình nghiệp vụ độc lập (Lazy-loaded module). Nếu module có các kiểu dữ liệu nội bộ riêng phục vụ giao diện thì đặt trong thư mục models/ của chính module đó.


PHẦN 2: QUY CHUẨN CLEAN CODE CỦA CÔNG TY (BẮT BUỘC TUÂN THỦ)

1. Nguyên tắc 1 Class / 1 Interface / 1 Enum trên 1 File
   - Tuyệt đối không gộp nhiều class, record, enum hoặc interface vào chung một file .cs hay .ts.
   - Tên file phải đặt trùng khớp với tên class hoặc interface bên trong (ví dụ: LeaveTimelineItemDto đặt trong file LeaveTimelineItemDto.cs).

2. Quy chuẩn viết Comment trong code
   - Chỉ sử dụng comment một dòng bằng dấu // với nội dung tiếng Việt ngắn gọn, rõ nghĩa để giải thích mục đích của class, phương thức hoặc đoạn xử lý nghiệp vụ quan trọng.
   - Không sử dụng comment dạng khối XML /// <summary> trong C# hoặc JSDoc /** ... */ trong TypeScript.

3. Đóng gói trạng thái trong Domain (Private Setters)
   - Trong các Entity như Request, RequestComment, Department, DepartmentMember, mọi thuộc tính đều phải để private set.
   - Khi cần thay đổi giá trị, phải gọi qua các hàm nghiệp vụ rõ tên (ví dụ: SetTitle, SetTypeAndAmount, SetLeaveDates, Submit, Approve, Reject) có kiểm tra dữ liệu đầu vào chặt chẽ.

4. Chia nhỏ các Class lớn và Tái sử dụng code (DRY)
   - Khi một AppService có nhiều mảng nghiệp vụ lớn (như EmployeeAppService), phải tách thành các file partial class theo từng nhóm chức năng:
     + EmployeeAppService.cs: Xử lý hồ sơ nhân sự, ngày vào làm, lịch sử đề xuất.
     + EmployeeAppService.AccessControl.cs: Xử lý phân quyền truy cập 3 cấp (Admin, Trưởng phòng, Nhân viên).
     + EmployeeAppService.Analytics.cs: Xử lý thống kê nhân sự toàn công ty và báo cáo nghỉ phép.
   - Ở phía Frontend, mọi xử lý định dạng tiền, ngày tháng, tính số ngày nghỉ, lấy màu Avatar, kiểm tra quyền hiện tại phải gọi từ shared/utils và CurrentUserContextService, không viết lặp lại ở từng Component.

5. Quy chuẩn màu sắc và Giao diện (UI Theme)
   - Sử dụng tông màu xanh dương doanh nghiệp chủ đạo (#2563eb / #1d4ed8) kết hợp màu xám trung tính.
   - Không sử dụng màu hồng (#f72585) của theme mặc định cho bất kỳ hiệu ứng di chuột (hover), đường viền hay icon menu nào.


PHẦN 3: QUY CHUẨN XỬ LÝ LỖI VÀ FORM BÁO LỖI

3.1. Quy chuẩn ném lỗi phía Backend (.NET / ABP)

1. Khai báo mã lỗi nghiệp vụ tại file Domain.Shared/InternalApprovalDomainErrorCodes.cs:
   - InternalApproval:00001: Chỉ người tạo mới có quyền gửi phê duyệt đề xuất này.
   - InternalApproval:00002: Chỉ đề xuất ở trạng thái Nháp mới có thể gửi phê duyệt.
   - InternalApproval:00003: Bạn không được phép tự phê duyệt đề xuất của chính mình.
   - InternalApproval:00004: Chỉ đề xuất đang chờ duyệt mới có thể phê duyệt hoặc từ chối.
   - InternalApproval:00005: Vui lòng nhập lý do từ chối đề xuất.
   - InternalApproval:00006: Số tiền đề xuất phải lớn hơn 0 đối với loại mua sắm hoặc tạm ứng.
   - InternalApproval:00007: Chỉ đề xuất ở trạng thái Nháp mới được phép chỉnh sửa hoặc xóa.
   - InternalApproval:00008: Đề xuất nghỉ phép bắt buộc phải chọn ngày bắt đầu và ngày kết thúc.
   - InternalApproval:00009: Ngày kết thúc nghỉ phép không được nhỏ hơn ngày bắt đầu.

2. Khai báo câu thông báo tương ứng trong file Localization/InternalApproval/vi.json và en.json.

3. Sử dụng đúng loại Exception theo ngữ cảnh:
   - Dùng BusinessException kèm mã lỗi trong InternalApprovalDomainErrorCodes khi vi phạm quy tắc nghiệp vụ Domain:
     throw new BusinessException(InternalApprovalDomainErrorCodes.CannotApproveOwnRequest);
   - Dùng UserFriendlyException khi cần trả về thông báo lỗi kiểm tra dữ liệu trực tiếp cho người dùng:
     throw new UserFriendlyException($"Mã phòng ban '{input.Code}' đã tồn tại trong hệ thống.");
   - Dùng AbpAuthorizationException khi người dùng truy cập dữ liệu vượt quá phạm vi phân quyền:
     throw new AbpAuthorizationException("Bạn không có quyền truy cập thông tin nhân sự ngoài phạm vi quản lý.");


3.2. Quy chuẩn hiển thị lỗi và kiểm tra Form phía Frontend (Angular)

1. Bóc tách và hiển thị lỗi khi gọi API:
   - Luôn sử dụng hàm extractErrorMessage từ shared/utils kết hợp với NzMessageService:
     this.requestService.approve(id, input).subscribe({
       next: () => {
         this.message.success('Phê duyệt đề xuất thành công!');
       },
       error: err => {
         this.message.error(extractErrorMessage(err, 'Không thể phê duyệt đề xuất!'));
       },
     });

2. Kiểm tra hợp lệ Form (ReactiveFormsModule) trước khi gửi API:
   - Khi người dùng bấm Lưu mà form chưa hợp lệ, phải đánh dấu dirty để hiển thị thông báo lỗi dưới từng ô nhập:
     if (this.form.invalid) {
       Object.values(this.form.controls).forEach(control => {
         if (control.invalid) {
           control.markAsDirty();
           control.updateValueAndValidity({ onlySelf: true });
         }
       });
       return;
     }
   - Trên file HTML, mỗi ô nhập liệu đặt trong thẻ nz-form-control kèm thuộc tính nzErrorTip tiếng Việt rõ ràng.


PHẦN 4: TỔNG HỢP CÁC MODULE VÀ HẠNG MỤC ĐÃ CHUẨN HÓA TRONG DỰ ÁN

4.1. Module Quản lý và Phê duyệt Đề xuất (Requests)
   - Hoàn thiện quy trình 4 trạng thái (Nháp -> Đã gửi duyệt -> Đã duyệt / Bị từ chối) cho 3 loại đề xuất (Nghỉ phép, Mua thiết bị, Tạm ứng tiền).
   - Bổ sung chọn khoảng thời gian nghỉ phép (Từ ngày - Đến ngày) lưu trực tiếp vào StartDate và EndDate dưới cơ sở dữ liệu, tự động tính tổng số ngày nghỉ và tự động gợi ý tiêu đề đơn xin nghỉ phép.
   - Chặn tự duyệt đơn của chính mình ở cả tầng Domain (RequestManager) và ẩn nút thao tác trên giao diện.
   - Tích hợp EventBus (RequestStatusChangedEto) và Background Job (SendApprovalNotificationJob) tự động gửi thông báo cho người tạo ngay khi đơn được duyệt hoặc bị từ chối.
   - Hỗ trợ trao đổi bình luận trực tiếp trên từng đề xuất, xem lịch sử phê duyệt dạng Timeline và xuất danh sách ra file Excel.

4.2. Module Quản lý Cơ cấu Phòng ban (Departments)
   - Quản lý danh sách phòng ban (Thêm, Sửa, Xóa, Tìm kiếm nhanh).
   - Bổ nhiệm hoặc hủy quyền Trưởng phòng trực tiếp.
   - Phân bổ nhân sự vào phòng ban, cập nhật chức vụ (Position) với bộ gợi ý chức vụ dùng chung (COMMON_DEPARTMENT_POSITIONS).
   - Chuẩn hóa đầy đủ từ IDepartmentRepository, EfCoreDepartmentRepository đến DepartmentAppService và DepartmentController.

4.3. Module Hồ sơ Nhân sự và Thống kê Nghỉ phép (Employees)
   - Áp dụng cơ chế phân quyền 3 cấp (EmployeeAccessLevel):
     + Quản trị viên (Admin / có quyền Employees.ViewAll): Xem và thống kê toàn bộ nhân sự trong công ty.
     + Trưởng phòng (DepartmentManager): Xem danh sách nhân sự và báo cáo nghỉ phép của các phòng ban do mình quản lý.
     + Nhân viên (Self): Xem hồ sơ cá nhân, thâm niên công tác, lịch sử đề xuất và thống kê ngày nghỉ phép của chính mình mà không bị chặn lỗi 403 Forbidden.
   - Cung cấp ngăn kéo xem nhanh hồ sơ nhân viên (EmployeeDetailDrawerComponent gồm 4 tab) tích hợp xuyên suốt tại trang Nhân sự, Phòng ban và Đề xuất.
   - Cung cấp Tab Báo cáo ngày nghỉ phép theo năm, phòng ban và xuất báo cáo Excel tổng hợp.

4.4. Module Thông báo, Tài khoản và Giao diện chung
   - Chuông thông báo thời gian thực (NotificationBellComponent) trên thanh điều hướng, bấm vào thông báo sẽ tự động chuyển đến trang Đề xuất và mở ngăn kéo chi tiết của đơn đó.
   - Quản lý Vai trò (RolesComponent), Tài khoản (UsersComponent) và Modal phân quyền tiếng Việt (PermissionModalComponent).
   - Xóa bỏ các trang và menu dư thừa không sử dụng (Cài đặt, Người thuê, Tài khoản của tôi) để giao diện gọn gàng, tập trung đúng nghiệp vụ.


PHẦN 5: QUY TRÌNH 6 BƯỚC CHUẨN KHI THÊM MỘT MODULE MỚI

Khi cần phát triển thêm một module mới vào dự án (ví dụ: Module Quản lý Tài sản - Assets), lập trình viên thực hiện lần lượt theo 6 bước sau:

Bước 1: Khai báo tại tầng Domain.Shared
   - Tạo thư mục mới cho module trong Domain.Shared.
   - Tạo các file Enum (mỗi Enum 1 file riêng) và file hằng số giới hạn độ dài.
   - Bổ sung mã lỗi vào InternalApprovalDomainErrorCodes.cs và thêm câu dịch tiếng Việt / tiếng Anh vào vi.json, en.json.

Bước 2: Xây dựng Entity, Domain Service và Repository tại tầng Domain và EntityFrameworkCore
   - Tạo AggregateRoot / Entity kế thừa FullAuditedAggregateRoot<Guid>, sử dụng private set cho các thuộc tính.
   - Tạo Interface Repository trong tầng Domain.
   - Khai báo DbSet và cấu hình bảng, Index trong InternalApprovalDbContext.cs.
   - Tạo lớp EfCoreRepository trong thư mục riêng của module tại tầng EntityFrameworkCore, sau đó tạo Migration cập nhật cơ sở dữ liệu.

Bước 3: Khai báo DTO và Phân quyền tại tầng Application.Contracts
   - Khai báo các quyền mới (Default, Create, Edit, Delete...) trong InternalApprovalPermissions.cs và InternalApprovalPermissionDefinitionProvider.cs.
   - Tạo thư mục module trong Application.Contracts, đặt các file DTO, Input DTO và Interface AppService trực tiếp trong thư mục đó (mỗi class/interface 1 file riêng, không tạo thư mục con Dtos/).

Bước 4: Cài đặt nghiệp vụ tại tầng Application và HttpApi
   - Viết lớp AppService trong tầng Application, cấu hình ánh xạ đối tượng trong InternalApprovalApplicationAutoMapperProfile.cs.
   - Tạo Controller tương ứng trong tầng HttpApi/Controllers/.
   - Viết bài kiểm thử tích hợp (Integration Test) để kiểm tra phân quyền và các quy tắc nghiệp vụ.

Bước 5: Xây dựng giao diện tại tầng Frontend Angular
   - Cập nhật hoặc sinh mới thư mục proxy/<tên-module>/ chứa Service và Model gọi API.
   - Tạo module mới trong angular/src/app/<tên-module>/, chỉ import SharedModule và Routing Module của nó.
   - Tách riêng các Interface giao diện vào thư mục models/, các hằng số vào shared/constants/.
   - Sử dụng CurrentUserContextService để kiểm tra quyền người dùng và extractErrorMessage từ shared/utils/ để hiển thị lỗi thống nhất.

Bước 6: Kiểm tra chất lượng trước khi đẩy code lên Git
   - Rà soát lại toàn bộ comment đảm bảo chỉ dùng // bằng tiếng Việt ngắn gọn.
   - Chạy lệnh dotnet test đảm bảo 100% bài test vượt qua.
   - Chạy lệnh npm --prefix angular run build đảm bảo biên dịch Frontend thành công không có lỗi.

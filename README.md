# Tài Liệu Kiến Trúc & Quy Chuẩn Phát Triển Dự Án Internal Approval

Tài liệu này là sổ tay hướng dẫn chính thức (Onboarding & Coding Convention) cho dự án **Hệ thống Quản lý và Phê duyệt Đề xuất Nội bộ (`MyCompany.InternalApproval`)**. Khi bảo trì hoặc phát triển thêm bất kỳ module mới nào, lập trình viên bắt buộc tuân thủ đúng kiến trúc, cấu trúc thư mục, quy tắc đặt tên và mẫu xử lý lỗi được quy định trong tài liệu này.

## 1. Tổng Quan Công Nghệ & Kiến Trúc Hệ Thống

Hệ thống được xây dựng theo kiến trúc **Domain-Driven Design (DDD)** trên nền tảng **ABP Framework (.NET 8)** kết hợp với **Angular + NG-ZORRO (Ant Design)**.

### 1.1. Các tầng Backend (`aspnet-core/src/`)

| Tầng (Project) | Trách nhiệm chính | Quy tắc bắt buộc |
| :--- | :--- | :--- |
| `Domain.Shared` | Chứa `Enums`, mã lỗi `InternalApprovalDomainErrorCodes`, hằng số độ dài (`Consts`) và từ điển đa ngôn ngữ (`Localization/InternalApproval/*.json`). | Không phụ thuộc vào bất kỳ tầng nào khác. Mọi module nếu có trạng thái hoặc phân loại phải định nghĩa `Enum` tại đây. |
| `Domain` | Chứa `AggregateRoot`, `Entity`, `DomainService` (`Manager`), `Repository Interface`, `Event Data (Eto)` và `DataSeedContributor`. | Các `Entity` phải đóng gói trạng thái bằng `private set`. Mọi quy tắc nghiệp vụ cốt lõi (Invariants) phải nằm trong Entity hoặc `DomainService`. |
| `Application.Contracts` | Chứa `DTO`, `Input DTO`, `Interface AppService` và định nghĩa cây quyền hạn (`Permissions`). | Đặt các file DTO trực tiếp trong thư mục của từng module (`Requests/`, `Departments/`, `Employees/`, `Notifications/`), không tạo thêm thư mục con `Dtos/` gây lệch cấu trúc. |
| `Application` | Chứa cài đặt `AppService`, cấu hình `AutoMapperProfile`, `EventHandler` và `BackgroundJob`. | `AppService` chỉ điều phối luồng nghiệp vụ, kiểm tra quyền và gọi `DomainService` / `Repository`, không viết logic nghiệp vụ phức tạp trực tiếp tại đây. |
| `EntityFrameworkCore` | Chứa `DbContext`, cấu hình bảng Fluent API, `Migrations` và cài đặt `EfCoreRepository` cho từng module. | Mỗi module có truy vấn đặc thù hoặc xử lý N+1 phải có thư mục riêng (`Requests/`, `Departments/`, `Notifications/`) cài đặt `EfCoreRepository`. |
| `HttpApi` & `HttpApi.Host` | Chứa các `Controller` tường minh (`RequestController`, `DepartmentController`, `EmployeeController`) kế thừa `InternalApprovalController`, cấu hình Swagger và OpenIddict. | Đảm bảo mỗi module có Controller rõ ràng và đồng bộ đường dẫn REST API `/api/app/<module>`. |

### 1.2. Cấu trúc tầng Frontend (`angular/src/app/`)

| Thư mục | Trách nhiệm chính | Quy tắc bắt buộc |
| :--- | :--- | :--- |
| `proxy/` | Chứa các `Service`, `Model` và `Enum` TypeScript tương ứng với `Application.Contracts` phía Backend. | Chia theo từng thư mục module (`requests/`, `departments/`, `employees/`, `notifications/`). |
| `shared/models/` | Chứa các `Interface` / `Model` dùng chung trên giao diện (`ColumnConfig`, `RequestStatusMeta`, `RequestTypeMeta`, `DashboardReportData`). | Mỗi `Interface` đặt trong một file `.model.ts` riêng biệt và xuất qua `index.ts`. |
| `shared/constants/` | Chứa các cấu hình danh mục cố định (`request-status.constant.ts`, `request-type.constant.ts`, `department-position.constant.ts`, `permission-name.constant.ts`). | Tuyệt đối không viết cứng (hardcode) danh sách trạng thái, màu sắc hay từ điển quyền hạn lặp lại trong từng Component. |
| `shared/utils/` | Chứa các hàm tiện ích thuần túy (`error.util.ts`, `date.util.ts`, `currency.util.ts`, `avatar.util.ts`). | Mọi xử lý bóc tách lỗi API, tính số ngày nghỉ phép, định dạng tiền tệ VNĐ, tạo màu Avatar phải gọi từ `shared/utils`. |
| `shared/services/` | Chứa các dịch vụ dùng chung toàn ứng dụng (`CurrentUserContextService`, `ExcelExportService`). | Kiểm tra thông tin đăng nhập, vai trò `isAdmin()`, `isApprover()` thông qua `CurrentUserContextService`. |
| `shared/shared.module.ts` | Tập trung và export toàn bộ module dùng chung của Angular, ABP và bộ thư viện `NG-ZORRO`. | Các Feature Module chỉ cần import `SharedModule` và Routing Module, không import lặp lại từng `Nz...Module`. |
| `<feature>/` | Các module chức năng (`home/`, `request/`, `department/`, `employee/`, `identity/`). | Nếu màn hình có các kiểu dữ liệu nội bộ riêng, bắt buộc tách vào thư mục `<feature>/models/` (1 interface / 1 file). |

---

## 2. Quy Chuẩn Clean Code Của Công Ty (Bắt Buộc Tuân Thủ)

1. **Nguyên tắc 1 Class / 1 Interface / 1 Enum trên 1 File**:
   - Không gộp nhiều `class`, `record`, `enum` hoặc `interface` vào chung một file `.cs` hay `.ts`.
   - Tên file phải khớp chính xác với tên kiểu dữ liệu bên trong (ví dụ: `LeaveTimelineItemDto` nằm trong `LeaveTimelineItemDto.cs`).
2. **Quy chuẩn viết Comment**:
   - Chỉ sử dụng comment một dòng `//` bằng tiếng Việt ngắn gọn, rõ nghĩa để giải thích mục đích của class, hàm hoặc đoạn logic nghiệp vụ quan trọng.
   - Không sử dụng khối comment tài liệu rườm rà như `/// <summary>` trong C# hay `/** ... */` JSDoc trong TypeScript.
3. **Đóng gói dữ liệu Domain (Encapsulation & Private Setters)**:
   - Trong `Entity` / `AggregateRoot`, các thuộc tính phải dùng `private set;`.
   - Việc thay đổi dữ liệu phải đi qua các phương thức có tên mang ý nghĩa nghiệp vụ rõ ràng (như `SetTitle`, `SetTypeAndAmount`, `SetLeaveDates`, `Submit`, `Approve`, `Reject`) kèm kiểm tra hợp lệ (`Check.NotNullOrWhiteSpace`, `BusinessException`).
4. **Nguyên tắc không lặp lại code (DRY - Don't Repeat Yourself)**:
   - Các logic lớn (như `EmployeeAppService` hơn 800 dòng) phải được tách bằng `partial class` theo từng mảng trách nhiệm (`EmployeeAppService.cs`, `EmployeeAppService.AccessControl.cs`, `EmployeeAppService.Analytics.cs`).
   - Phía Frontend, không tự viết lại hàm lấy chữ cái đầu tên nhân viên, tính số ngày nghỉ hay bóc lỗi API trong Component mà phải import từ `src/app/shared/utils`.
5. **Quy chuẩn màu sắc & Giao diện (Enterprise UI Theme)**:
   - Giao diện sử dụng tông màu xanh dương doanh nghiệp chủ đạo (`#2563eb` / `#1d4ed8`) và xám trung tính (`Slate`).
   - Không sử dụng màu hồng (`#f72585`) của theme mặc định cho các hiệu ứng di chuột (`hover`), đường viền hay biểu tượng menu.

---

## 3. Quy Chuẩn Xử Lý Lỗi & Form Báo Lỗi

### 3.1. Phía Backend (.NET / ABP)

Mọi lỗi nghiệp vụ được định nghĩa tập trung và ném ra thông qua `BusinessException` hoặc `UserFriendlyException`:

1. **Khai báo mã lỗi tại `Domain.Shared/InternalApprovalDomainErrorCodes.cs`**:
   ```csharp
   // Định nghĩa các mã lỗi nghiệp vụ dùng chung trong toàn hệ thống
   public static class InternalApprovalDomainErrorCodes
   {
       public const string OnlyCreatorCanSubmitRequest = "InternalApproval:00001";
       public const string OnlyDraftRequestCanBeSubmitted = "InternalApproval:00002";
       public const string CannotApproveOwnRequest = "InternalApproval:00003";
       public const string OnlySubmittedRequestCanBeApprovedOrRejected = "InternalApproval:00004";
       public const string RejectionReasonIsRequired = "InternalApproval:00005";
       public const string AmountMustBePositiveForFinancialRequest = "InternalApproval:00006";
       public const string OnlyDraftRequestCanBeModifiedOrDeleted = "InternalApproval:00007";
       public const string LeaveDatesAreRequired = "InternalApproval:00008";
       public const string LeaveEndDateCannotBeEarlierThanStartDate = "InternalApproval:00009";
   }
   ```

2. **Khai báo nội dung thông báo trong `Localization/InternalApproval/vi.json` và `en.json`**:
   ```json
   "InternalApproval:00003": "Bạn không được phép tự phê duyệt đề xuất của chính mình!",
   "InternalApproval:00008": "Đề xuất nghỉ phép bắt buộc phải chọn ngày bắt đầu và ngày kết thúc!"
   ```

3. **Cách ném lỗi trong Domain / AppService**:
   - Với lỗi vi phạm quy tắc nghiệp vụ Domain (đã có mã lỗi):
     ```csharp
     throw new BusinessException(InternalApprovalDomainErrorCodes.CannotApproveOwnRequest);
     ```
   - Với lỗi kiểm tra dữ liệu đầu vào cụ thể (như trùng mã phòng ban, ngày vào làm vượt quá hiện tại):
     ```csharp
     throw new UserFriendlyException($"Mã phòng ban '{input.Code}' đã tồn tại trong hệ thống.");
     ```
   - Với lỗi truy cập vượt thẩm quyền phân cấp:
     ```csharp
     throw new AbpAuthorizationException("Bạn không có quyền truy cập thông tin nhân sự ngoài phạm vi quản lý.");
     ```

### 3.2. Phía Frontend (Angular + NG-ZORRO)

1. **Hiển thị lỗi từ API bằng `extractErrorMessage` và `NzMessageService`**:
   - Tuyệt đối không viết thủ công `err?.error?.error?.message || ...` ở từng hàm. Sử dụng hàm chuẩn từ `shared/utils/error.util.ts`:
   ```typescript
   import { extractErrorMessage } from '../shared/utils';

   this.requestService.approve(id, input).subscribe({
     next: () => {
       this.message.success('Phê duyệt đề xuất thành công!');
     },
     error: err => {
       this.message.error(extractErrorMessage(err, 'Không thể phê duyệt đề xuất!'));
     },
   });
   ```

2. **Kiểm tra hợp lệ Form (`ReactiveFormsModule` + `nz-form-control`)**:
   - Trước khi gửi dữ liệu lên server, luôn đánh dấu `markAsDirty()` và `updateValueAndValidity()` để hiển thị dòng báo lỗi đỏ dưới từng ô nhập liệu:
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
   - Trong file HTML, sử dụng thuộc tính `nzErrorTip` trên `<nz-form-control>` để hiển thị câu báo lỗi rõ ràng bằng tiếng Việt:
   ```html
   <nz-form-item>
     <nz-form-label nzRequired>Tiêu đề đề xuất</nz-form-label>
     <nz-form-control nzErrorTip="Vui lòng nhập tiêu đề đề xuất (tối đa 200 ký tự)!">
       <input nz-input formControlName="title" placeholder="Nhập tiêu đề..." />
     </nz-form-control>
   </nz-form-item>
   ```

---

## 4. Tổng Hợp Các Module & Hạng Mục Đã Chuẩn Hóa Trong Dự Án

### 4.1. Phân hệ Quản lý & Phê duyệt Đề xuất (`Requests`)
- **Backend**:
  - `Request` (`AggregateRoot`) và `RequestComment` (`Entity`) với `private set` bảo vệ toàn vẹn dữ liệu.
  - Hỗ trợ đầy đủ 3 loại đề xuất (`RequestType`: `Leave`, `Equipment`, `AdvanceMoney`) và 4 trạng thái (`RequestStatus`: `Draft`, `Submitted`, `Approved`, `Rejected`).
  - Bổ sung trường `StartDate` và `EndDate` cho đề xuất nghỉ phép (`Leave`), tự động kiểm tra `EndDate >= StartDate` ngay từ tầng Domain.
  - `RequestManager` (`DomainService`) kiểm soát chặt chẽ: chỉ người tạo mới được gửi duyệt đơn nháp, cấm tự duyệt đơn của chính mình, từ chối bắt buộc có lý do.
  - Tích hợp `RequestStatusChangedEto`, `RequestStatusChangedEventHandler` và `SendApprovalNotificationJob` để tạo thông báo tự động khi trạng thái đơn thay đổi.
  - `EfCoreRequestRepository` xử lý `Include(x => x.Comments)` giải quyết triệt để lỗi N+1 query.
- **Frontend**:
  - Màn hình danh sách `RequestComponent` hỗ trợ lọc theo từ khóa, trạng thái, loại hình, phạm vi (`Đề xuất của tôi` / `Toàn hệ thống`), tùy chỉnh ẩn/hiện cột lưu vào `localStorage` và xuất báo cáo Excel.
  - Tách riêng 3 component con rõ ràng: `RequestFormModalComponent` (tự động chọn khoảng ngày nghỉ, tính số ngày nghỉ và gợi ý tiêu đề), `RequestActionModalComponent` (duyệt / từ chối) và `RequestDetailDrawerComponent` (xem chi tiết, timeline phê duyệt và gửi bình luận).

### 4.2. Phân hệ Quản lý Cơ cấu Phòng ban (`Departments`)
- **Backend**:
  - Xây dựng `Department` (`AggregateRoot`) và `DepartmentMember` (`Entity` lưu thông tin nhân sự thuộc phòng ban, chức vụ `Position` và ngày vào làm `JoinDate`).
  - Chuẩn hóa kiến trúc với `IDepartmentRepository`, `EfCoreDepartmentRepository`, `DepartmentAppService` và `DepartmentController`.
  - Hỗ trợ gán / hủy quyền Trưởng phòng (`SetManagerAsync`), luân chuyển nhân sự giữa các phòng ban và tra cứu danh sách người dùng khả dụng.
- **Frontend**:
  - Giao diện `DepartmentComponent` tối giản, trực quan, quản lý danh mục phòng ban và ngăn kéo (`Drawer`) phân bổ nhân sự, cập nhật chức vụ nhanh từ danh sách `COMMON_DEPARTMENT_POSITIONS`.

### 4.3. Phân hệ Hồ sơ Nhân sự & Thống kê Nghỉ phép (`Employees`)
- **Backend**:
  - Tách cấu trúc `EmployeeAppService` thành 3 file `partial class`:
    - `EmployeeAppService.cs`: Danh sách nhân viên, chi tiết hồ sơ, cập nhật ngày vào làm, lịch sử đề xuất và timeline nghỉ phép.
    - `EmployeeAppService.AccessControl.cs`: Cơ chế phân quyền 3 cấp (`EmployeeAccessLevel`: `Admin` xem toàn công ty -> `DepartmentManager` xem nhân sự thuộc phòng ban mình quản lý -> `Self` nhân viên xem hồ sơ cá nhân).
    - `EmployeeAppService.Analytics.cs`: Thống kê nhân sự toàn công ty (`HrAnalyticsDto`) và báo cáo tổng hợp ngày nghỉ phép (`GetLeaveReportAsync`).
  - Cho phép mọi nhân viên đăng nhập đều vào được trang `/employees` để tra cứu hồ sơ và ngày nghỉ phép của bản thân mà không bị lỗi `403 Forbidden`, đồng thời chặn truy cập trái phép sang hồ sơ người khác.
- **Frontend**:
  - Màn hình `EmployeeComponent` gồm 2 Tab: **Danh sách nhân viên** (kèm thẻ chỉ số HR Analytics) và **Báo cáo ngày nghỉ phép** (lọc theo năm, phòng ban, từ khóa và xuất Excel chuyên nghiệp).
  - Component dùng chung `EmployeeDetailDrawerComponent` (4 Tab: Thông tin chung & Thâm niên, Lịch sử nghỉ phép, Lịch sử đề xuất, Thống kê năm) có thể mở nhanh khi bấm vào tên nhân viên ở cả trang Phòng ban và trang Đề xuất.

### 4.4. Phân hệ Thông báo, Tài khoản & Giao diện Hệ thống (`Notifications`, `Identity`, `Home`)
- **Thông báo (`Notifications`)**: Chuông thông báo thời gian thực `NotificationBellComponent` trên thanh điều hướng, đồng bộ với `IApprovalNotificationRepository` và `EfCoreApprovalNotificationRepository` ở Backend.
- **Quản trị Tài khoản & Phân quyền (`Identity`)**: Quản lý Vai trò (`RolesComponent`), Tài khoản (`UsersComponent`) và Modal phân quyền tiếng Việt trực quan (`PermissionModalComponent`).
- **Tinh gọn Menu & Giao diện**:
  - Loại bỏ các mục không sử dụng khỏi thanh menu và menu người dùng (`Cài đặt`, `Người thuê`, `Tài khoản của tôi`) tại `route.provider.ts`, `app.component.ts` và `app-routing.module.ts`.
  - Đồng bộ bảng màu xanh doanh nghiệp (`#2563eb`), loại bỏ hoàn toàn hiệu ứng hover màu hồng mặc định của theme LeptonX trong `styles.scss`.

---

## 5. Quy Trình 6 Bước Khi Thêm Một Module Mới

Khi được giao phát triển một phân hệ mới (ví dụ: `Assets` - Quản lý Tài sản), hãy thực hiện tuần tự theo 6 bước chuẩn dưới đây:

1. **Bước 1 - Định nghĩa tại `Domain.Shared`**:
   - Tạo thư mục `Assets/`, thêm các file `Enum` (1 enum / 1 file) và hằng số độ dài `AssetConsts.cs`.
   - Khai báo mã lỗi vào `InternalApprovalDomainErrorCodes.cs` và thêm bản dịch vào `vi.json`, `en.json`.
2. **Bước 2 - Xây dựng thực thể và Repository tại `Domain` & `EntityFrameworkCore`**:
   - Tạo `Asset.cs` kế thừa `FullAuditedAggregateRoot<Guid>` với `private set`.
   - Tạo `IAssetRepository.cs` trong `Domain/Assets/`.
   - Cấu hình bảng `AppAssets` và Index trong `InternalApprovalDbContext.cs`, tạo `EfCoreAssetRepository.cs` trong `EntityFrameworkCore/Assets/` và chạy `dotnet ef migrations add Added_Asset_Module`.
3. **Bước 3 - Khai báo hợp đồng tại `Application.Contracts`**:
   - Thêm nhóm quyền `Assets` vào `InternalApprovalPermissions.cs` và `InternalApprovalPermissionDefinitionProvider.cs`.
   - Tạo thư mục `Application.Contracts/Assets/` chứa `AssetDto.cs`, `CreateUpdateAssetDto.cs`, `GetAssetListInput.cs` và `IAssetAppService.cs` (mỗi kiểu dữ liệu 1 file riêng, không tạo thư mục con `Dtos/`).
4. **Bước 4 - Cài đặt nghiệp vụ tại `Application` & `HttpApi`**:
   - Tạo `AssetAppService.cs` trong `Application/Assets/`, cấu hình ánh xạ trong `InternalApprovalApplicationAutoMapperProfile.cs`.
   - Tạo `AssetController.cs` trong `HttpApi/Controllers/Assets/`.
   - Viết kiểm thử tích hợp (Integration Test) để xác nhận phân quyền và logic nghiệp vụ.
5. **Bước 5 - Tích hợp Frontend Angular**:
   - Chạy `abp generate-proxy -t ng` hoặc cập nhật thư mục `angular/src/app/proxy/assets/`.
   - Tạo module `angular/src/app/asset/` import `SharedModule` và `AssetRoutingModule`.
   - Tách các interface giao diện riêng vào `asset/models/`, các hằng số dùng chung vào `shared/constants/`, sử dụng `CurrentUserContextService` và `extractErrorMessage` từ `shared/utils/`.
6. **Bước 6 - Kiểm tra chất lượng trước khi Commit**:
   - Đảm bảo toàn bộ comment đều dùng `//` tiếng Việt ngắn gọn.
   - Chạy `dotnet test` (tất cả bài test đều Pass) và `npm --prefix angular run build` (0 lỗi biên dịch).

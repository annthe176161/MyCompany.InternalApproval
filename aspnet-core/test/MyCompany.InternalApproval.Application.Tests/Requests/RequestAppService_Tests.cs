using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MyCompany.InternalApproval.Requests;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;
using Xunit;

namespace MyCompany.InternalApproval.Requests;

public abstract class RequestAppService_Tests<TStartupModule> : InternalApprovalApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected readonly IRequestAppService RequestAppService;
    protected readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;

    protected readonly Guid EmployeeId = Guid.NewGuid();
    protected readonly string EmployeeName = "employee";

    protected readonly Guid ManagerId = Guid.NewGuid();
    protected readonly string ManagerName = "manager";

    protected RequestAppService_Tests()
    {
        RequestAppService = GetRequiredService<IRequestAppService>();
        CurrentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    protected IDisposable LoginAs(Guid userId, string userName, string role = "Employee")
    {
        var claims = new[]
        {
            new Claim(AbpClaimTypes.UserId, userId.ToString()),
            new Claim(AbpClaimTypes.UserName, userName),
            new Claim(AbpClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        return CurrentPrincipalAccessor.Change(new ClaimsPrincipal(identity));
    }

    // Test: Tạo đề xuất ở trạng thái Nháp thành công
    [Fact]
    public async Task Should_Create_Draft_Request_Successfully()
    {
        using (LoginAs(EmployeeId, EmployeeName))
        {
            // Act
            var result = await RequestAppService.CreateAsync(new CreateRequestDto
            {
                Title = "Xin nghỉ phép du lịch",
                Type = RequestType.Leave,
                Description = "Nghỉ phép 2 ngày cuối tuần"
            });

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldNotBe(Guid.Empty);
            result.Title.ShouldBe("Xin nghỉ phép du lịch");
            result.Type.ShouldBe(RequestType.Leave);
            result.Status.ShouldBe(RequestStatus.Draft);
        }
    }

    // Test: Gửi duyệt đề xuất thành công
    [Fact]
    public async Task Should_Submit_Draft_Request()
    {
        using (LoginAs(EmployeeId, EmployeeName))
        {
            // Arrange
            var created = await RequestAppService.CreateAsync(new CreateRequestDto
            {
                Title = "Tạm ứng công tác phí",
                Type = RequestType.AdvanceMoney,
                Amount = 2000000,
                Description = "Tạm ứng đi công tác Hải Phòng"
            });

            // Act
            var submitted = await RequestAppService.SubmitAsync(created.Id);

            // Assert
            submitted.Status.ShouldBe(RequestStatus.Submitted);
        }
    }

    // Test: Chặn gửi duyệt lại khi đề xuất đã được gửi trước đó
    [Fact]
    public async Task Should_Not_Allow_Submit_If_Already_Submitted()
    {
        using (LoginAs(EmployeeId, EmployeeName))
        {
            // Arrange
            var created = await RequestAppService.CreateAsync(new CreateRequestDto
            {
                Title = "Mua bàn phím cơ",
                Type = RequestType.Equipment,
                Amount = 1500000
            });
            await RequestAppService.SubmitAsync(created.Id);

            // Act & Assert
            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await RequestAppService.SubmitAsync(created.Id);
            });

            ex.Code.ShouldBe(InternalApprovalDomainErrorCodes.OnlyDraftCanBeSubmitted);
        }
    }

    // Test: Chống tự duyệt - Chặn người tạo tự phê duyệt đề xuất của chính mình
    [Fact]
    public async Task Should_Not_Allow_Creator_To_Approve_Own_Request()
    {
        using (LoginAs(EmployeeId, EmployeeName))
        {
            // Arrange
            var created = await RequestAppService.CreateAsync(new CreateRequestDto
            {
                Title = "Đề xuất cá nhân",
                Type = RequestType.Leave
            });
            await RequestAppService.SubmitAsync(created.Id);

            // Act & Assert: Creator tries to approve own request
            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await RequestAppService.ApproveAsync(created.Id, new ApproveRequestDto
                {
                    Note = "Tự phê duyệt cho mình"
                });
            });

            ex.Code.ShouldBe(InternalApprovalDomainErrorCodes.CannotApproveOwnRequest);
        }
    }

    // Test: Quản lý phê duyệt đề xuất chờ duyệt thành công
    [Fact]
    public async Task Should_Allow_Manager_To_Approve_Submitted_Request()
    {
        Guid requestId;

        // Arrange: Employee creates and submits request
        using (LoginAs(EmployeeId, EmployeeName))
        {
            var created = await RequestAppService.CreateAsync(new CreateRequestDto
            {
                Title = "Mua chuột không dây",
                Type = RequestType.Equipment,
                Amount = 500000
            });
            await RequestAppService.SubmitAsync(created.Id);
            requestId = created.Id;
        }

        // Act: Manager approves request
        using (LoginAs(ManagerId, ManagerName, "admin"))
        {
            var approved = await RequestAppService.ApproveAsync(requestId, new ApproveRequestDto
            {
                Note = "Đồng ý duyệt cấp thiết bị"
            });

            // Assert
            approved.Status.ShouldBe(RequestStatus.Approved);
            approved.ApproverId.ShouldBe(ManagerId);
        }
    }

    // Test: Bắt buộc phải nhập lý do khi từ chối đề xuất
    [Fact]
    public async Task Should_Require_Reason_When_Rejecting_Request()
    {
        Guid requestId;

        using (LoginAs(EmployeeId, EmployeeName))
        {
            var created = await RequestAppService.CreateAsync(new CreateRequestDto
            {
                Title = "Mua thiết bị cao cấp",
                Type = RequestType.Equipment,
                Amount = 50000000
            });
            await RequestAppService.SubmitAsync(created.Id);
            requestId = created.Id;
        }

        using (LoginAs(ManagerId, ManagerName, "admin"))
        {
            // Act & Assert: Reject without reason triggers ABP validation
            await Should.ThrowAsync<Volo.Abp.Validation.AbpValidationException>(async () =>
            {
                await RequestAppService.RejectAsync(requestId, new RejectRequestDto
                {
                    Reason = "   "
                });
            });
        }
    }

    // Test: Quản lý từ chối đề xuất chờ duyệt thành công
    [Fact]
    public async Task Should_Allow_Manager_To_Reject_Submitted_Request()
    {
        Guid requestId;

        using (LoginAs(EmployeeId, EmployeeName))
        {
            var created = await RequestAppService.CreateAsync(new CreateRequestDto
            {
                Title = "Tạm ứng vượt hạn mức",
                Type = RequestType.AdvanceMoney,
                Amount = 30000000
            });
            await RequestAppService.SubmitAsync(created.Id);
            requestId = created.Id;
        }

        using (LoginAs(ManagerId, ManagerName, "admin"))
        {
            var rejected = await RequestAppService.RejectAsync(requestId, new RejectRequestDto
            {
                Reason = "Vượt hạn mức quy định cho nhân viên"
            });

            // Assert
            rejected.Status.ShouldBe(RequestStatus.Rejected);
            rejected.ApproverId.ShouldBe(ManagerId);
            rejected.RejectionReason.ShouldBe("Vượt hạn mức quy định cho nhân viên");
        }
    }

    // Test: Chặn xóa đề xuất khi không còn ở trạng thái Nháp
    [Fact]
    public async Task Should_Not_Allow_Delete_If_Not_Draft()
    {
        using (LoginAs(EmployeeId, EmployeeName))
        {
            // Arrange
            var created = await RequestAppService.CreateAsync(new CreateRequestDto
            {
                Title = "Đơn đã gửi duyệt",
                Type = RequestType.Leave
            });
            await RequestAppService.SubmitAsync(created.Id);

            // Act & Assert: Delete submitted request
            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await RequestAppService.DeleteAsync(created.Id);
            });

            ex.Code.ShouldBe(InternalApprovalDomainErrorCodes.OnlyDraftCanBeDeleted);
        }
    }

    // Test: Xóa đề xuất ở trạng thái Nháp thành công
    [Fact]
    public async Task Should_Allow_Delete_Draft_Request()
    {
        using (LoginAs(EmployeeId, EmployeeName))
        {
            // Arrange
            var created = await RequestAppService.CreateAsync(new CreateRequestDto
            {
                Title = "Đơn nháp cần xóa",
                Type = RequestType.Leave
            });

            // Act
            await RequestAppService.DeleteAsync(created.Id);

            // Assert
            await Should.ThrowAsync<EntityNotFoundException>(async () =>
            {
                await RequestAppService.GetAsync(created.Id);
            });
        }
    }

    // Test: Kiểm tra số tiền bắt buộc và lớn hơn 0 cho loại Mua thiết bị
    [Fact]
    public async Task Should_Validate_Amount_For_Equipment_Request()
    {
        using (LoginAs(EmployeeId, EmployeeName))
        {
            // Act & Assert: Equipment with null or zero amount
            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await RequestAppService.CreateAsync(new CreateRequestDto
                {
                    Title = "Mua màn hình",
                    Type = RequestType.Equipment,
                    Amount = 0
                });
            });

            ex.Code.ShouldBe(InternalApprovalDomainErrorCodes.AmountRequiredForType);
        }
    }

    // Test: Người tạo chỉnh sửa đề xuất Nháp thành công
    [Fact]
    public async Task Should_Allow_Creator_To_Update_Draft_Request()
    {
        using (LoginAs(EmployeeId, EmployeeName))
        {
            // Arrange
            var created = await RequestAppService.CreateAsync(new CreateRequestDto
            {
                Title = "Tiêu đề cũ",
                Type = RequestType.Leave,
                Description = "Mô tả cũ"
            });

            // Act
            var updated = await RequestAppService.UpdateAsync(created.Id, new UpdateRequestDto
            {
                Title = "Tiêu đề mới đã sửa",
                Type = RequestType.Leave,
                Description = "Mô tả mới đã sửa"
            });

            // Assert
            updated.Title.ShouldBe("Tiêu đề mới đã sửa");
            updated.Description.ShouldBe("Mô tả mới đã sửa");
        }
    }

    // Test: Chặn chỉnh sửa đề xuất khi không còn ở trạng thái Nháp
    [Fact]
    public async Task Should_Not_Allow_Update_If_Not_Draft()
    {
        using (LoginAs(EmployeeId, EmployeeName))
        {
            // Arrange
            var created = await RequestAppService.CreateAsync(new CreateRequestDto
            {
                Title = "Đơn xin nghỉ phép",
                Type = RequestType.Leave
            });
            await RequestAppService.SubmitAsync(created.Id);

            // Act & Assert
            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await RequestAppService.UpdateAsync(created.Id, new UpdateRequestDto
                {
                    Title = "Cố tình sửa sau khi gửi duyệt",
                    Type = RequestType.Leave
                });
            });

            ex.Code.ShouldBe(InternalApprovalDomainErrorCodes.OnlyDraftCanBeEdited);
        }
    }

    // Test: Chặn người khác chỉnh sửa đề xuất của mình
    [Fact]
    public async Task Should_Not_Allow_Non_Creator_To_Update()
    {
        Guid requestId;

        // Employee creates draft request
        using (LoginAs(EmployeeId, EmployeeName))
        {
            var created = await RequestAppService.CreateAsync(new CreateRequestDto
            {
                Title = "Đơn của Employee",
                Type = RequestType.Leave
            });
            requestId = created.Id;
        }

        // Another user (Manager) tries to edit Employee's draft
        using (LoginAs(ManagerId, ManagerName))
        {
            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await RequestAppService.UpdateAsync(requestId, new UpdateRequestDto
                {
                    Title = "Người khác cố tình sửa",
                    Type = RequestType.Leave
                });
            });

            ex.Code.ShouldBe(InternalApprovalDomainErrorCodes.OnlyCreatorCanEdit);
        }
    }

    // Test: Thêm bình luận vào đề xuất thành công
    [Fact]
    public async Task Should_Add_Comment_To_Request()
    {
        Guid requestId;

        using (LoginAs(EmployeeId, EmployeeName))
        {
            var created = await RequestAppService.CreateAsync(new CreateRequestDto
            {
                Title = "Đơn cần bình luận",
                Type = RequestType.Leave
            });
            requestId = created.Id;

            // Act
            var comment = await RequestAppService.AddCommentAsync(requestId, new CreateRequestCommentDto
            {
                Content = "Bổ sung thông tin người bàn giao công việc"
            });

            // Assert
            comment.ShouldNotBeNull();
            comment.Content.ShouldBe("Bổ sung thông tin người bàn giao công việc");

            var requestWithDetails = await RequestAppService.GetAsync(requestId);
            requestWithDetails.Comments.Count.ShouldBe(1);
            requestWithDetails.Comments.First().Content.ShouldBe("Bổ sung thông tin người bàn giao công việc");
        }
    }

    // Test: Lọc đề xuất theo loại và phân trang chính xác
    [Fact]
    public async Task Should_Filter_And_Paginate_Requests()
    {
        using (LoginAs(EmployeeId, EmployeeName))
        {
            // Arrange
            await RequestAppService.CreateAsync(new CreateRequestDto
            {
                Title = "Mua màn hình Dell 27 inch",
                Type = RequestType.Equipment,
                Amount = 5000000
            });

            await RequestAppService.CreateAsync(new CreateRequestDto
            {
                Title = "Xin nghỉ phép hè",
                Type = RequestType.Leave
            });

            // Act: Filter by Type Equipment
            var result = await RequestAppService.GetListAsync(new GetRequestListInput
            {
                Type = RequestType.Equipment,
                MaxResultCount = 10,
                SkipCount = 0
            });

            // Assert
            result.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
            result.Items.ShouldAllBe(x => x.Type == RequestType.Equipment);
        }
    }
}

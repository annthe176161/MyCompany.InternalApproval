using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCompany.InternalApproval.Migrations
{
    /// <inheritdoc />
    public partial class Added_ApprovalNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppApprovalNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReceiverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApproverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppApprovalNotifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppApprovalNotifications_CreationTime",
                table: "AppApprovalNotifications",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppApprovalNotifications_ReceiverId",
                table: "AppApprovalNotifications",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_AppApprovalNotifications_RequestId",
                table: "AppApprovalNotifications",
                column: "RequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppApprovalNotifications");
        }
    }
}

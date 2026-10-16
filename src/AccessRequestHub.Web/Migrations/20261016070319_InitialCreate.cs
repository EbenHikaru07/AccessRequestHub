using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccessRequestHub.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PolicyVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsAuditor = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Users_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SystemOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Applications_Users_SystemOwnerId",
                        column: x => x.SystemOwnerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AccessRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccessLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PolicyVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RejectReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessRequests", x => x.Id);
                    table.CheckConstraint("CK_AccessRequests_AccessLevel", "[AccessLevel] IN ('Read','Admin')");
                    table.CheckConstraint("CK_AccessRequests_Environment", "[Environment] IN ('NonProduction','Production')");
                    table.CheckConstraint("CK_AccessRequests_Justification", "LEN(LTRIM(RTRIM([Justification]))) > 0");
                    table.CheckConstraint("CK_AccessRequests_RejectReason", "[Status] <> 'Rejected' OR LEN(LTRIM(RTRIM(ISNULL([RejectReason],'')))) > 0");
                    table.CheckConstraint("CK_AccessRequests_Status", "[Status] IN ('PendingManagerApproval','PendingSystemOwnerApproval','Approved','Rejected')");
                    table.ForeignKey(
                        name: "FK_AccessRequests_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AccessRequests_PolicyVersions_PolicyVersionId",
                        column: x => x.PolicyVersionId,
                        principalTable: "PolicyVersions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AccessRequests_Users_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ToStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEvents_AccessRequests_AccessRequestId",
                        column: x => x.AccessRequestId,
                        principalTable: "AccessRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditEvents_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "PolicyVersions",
                columns: new[] { "Id", "Name" },
                values: new object[] { new Guid("bbbbbbbb-0000-0000-0000-000000000001"), "v1" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "DisplayName", "Email", "IsAuditor", "ManagerId" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Bob", "bob@example.local", false, null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Carol", "carol@example.local", false, null },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "Dana", "dana@example.local", false, null },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "Erin", "erin@example.local", true, null }
                });

            migrationBuilder.InsertData(
                table: "Applications",
                columns: new[] { "Id", "Name", "SystemOwnerId" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000001"), "CRM", new Guid("33333333-3333-3333-3333-333333333333") },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000002"), "Finance Portal", new Guid("44444444-4444-4444-4444-444444444444") }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "DisplayName", "Email", "IsAuditor", "ManagerId" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "Alice", "alice@example.local", false, new Guid("22222222-2222-2222-2222-222222222222") });

            migrationBuilder.CreateIndex(
                name: "IX_AccessRequests_ApplicationId",
                table: "AccessRequests",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRequests_PolicyVersionId",
                table: "AccessRequests",
                column: "PolicyVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRequests_RequesterId_ClientRequestId",
                table: "AccessRequests",
                columns: new[] { "RequesterId", "ClientRequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessRequests_Status_ApplicationId",
                table: "AccessRequests",
                columns: new[] { "Status", "ApplicationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_Name",
                table: "Applications",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_SystemOwnerId",
                table: "Applications",
                column: "SystemOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_AccessRequestId_OccurredAtUtc",
                table: "AuditEvents",
                columns: new[] { "AccessRequestId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_ActorId",
                table: "AuditEvents",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyVersions_Name",
                table: "PolicyVersions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ManagerId",
                table: "Users",
                column: "ManagerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "AccessRequests");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "PolicyVersions");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

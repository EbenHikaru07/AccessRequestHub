using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessRequestHub.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditAppendOnlyTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TRIGGER TR_AuditEvents_AppendOnly ON dbo.AuditEvents
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    THROW 50001, 'AuditEvents is append-only.', 1;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER dbo.TR_AuditEvents_AppendOnly");
        }
    }
}

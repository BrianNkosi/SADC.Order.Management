using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SADC.Order.Management.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds the OutboxMessages table for the transactional outbox pattern.
    /// Orders and outbox messages are inserted atomically in the same SaveChanges call,
    /// then a background publisher polls and sends to RabbitMQ.
    ///
    /// Zero-downtime note: This is a purely additive migration (new table).
    /// The old app version simply doesn't interact with it.
    /// Deploy: apply migration → deploy new app version.
    /// Rollback: dotnet ef database update AddOrderRowVersion
    /// </summary>
    public partial class AddOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            // Filtered index for efficient polling of unprocessed messages
            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc",
                table: "OutboxMessages",
                column: "ProcessedAtUtc",
                filter: "[ProcessedAtUtc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages");
        }
    }
}

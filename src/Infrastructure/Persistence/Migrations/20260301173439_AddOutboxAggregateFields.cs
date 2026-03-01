using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SADC.Order.Management.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxAggregateFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AggregateId",
                table: "OutboxMessages",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "AggregateType",
                table: "OutboxMessages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Order");

            migrationBuilder.AddColumn<DateTime>(
                name: "OccurredAtUtc",
                table: "OutboxMessages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "OutboxMessages",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_AggregateType_AggregateId",
                table: "OutboxMessages",
                columns: new[] { "AggregateType", "AggregateId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_AggregateType_AggregateId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "AggregateId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "AggregateType",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "OccurredAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "OutboxMessages");
        }
    }
}

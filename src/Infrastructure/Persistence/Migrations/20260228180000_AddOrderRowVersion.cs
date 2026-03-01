using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SADC.Order.Management.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds a SQL Server rowversion column to the Orders table
    /// for optimistic concurrency control.
    /// 
    /// Zero-downtime note: This is an additive change (new column with server
    /// default), so it can be applied while the old app version is still running.
    /// The old code simply ignores the new column.
    /// </summary>
    public partial class AddOrderRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Orders",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Orders");
        }
    }
}

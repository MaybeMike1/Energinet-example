using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GasFlowRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dataset = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GasDay = table.Column<DateOnly>(type: "date", nullable: false),
                    KWhFromBiogas = table.Column<long>(type: "bigint", nullable: false),
                    KWhToDenmark = table.Column<long>(type: "bigint", nullable: false),
                    KWhFromNorthSea = table.Column<long>(type: "bigint", nullable: false),
                    KWhToOrFromStorage = table.Column<long>(type: "bigint", nullable: false),
                    KWhToOrFromGermany = table.Column<long>(type: "bigint", nullable: false),
                    KWhToSweden = table.Column<long>(type: "bigint", nullable: false),
                    KWhFromTyra = table.Column<long>(type: "bigint", nullable: false),
                    KWhToPoland = table.Column<long>(type: "bigint", nullable: false),
                    RetrievedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GasFlowRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GasFlowRecords_Dataset_GasDay",
                table: "GasFlowRecords",
                columns: new[] { "Dataset", "GasDay" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GasFlowRecords");
        }
    }
}
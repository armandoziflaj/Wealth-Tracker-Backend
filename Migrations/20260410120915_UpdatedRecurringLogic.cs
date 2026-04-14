using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WealthTracker.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedRecurringLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NextOccurrence",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ParentTransactionId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecursionTime",
                table: "Transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ParentTransactionId",
                table: "Transactions",
                column: "ParentTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Transactions_ParentTransactionId",
                table: "Transactions",
                column: "ParentTransactionId",
                principalTable: "Transactions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Transactions_ParentTransactionId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ParentTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "NextOccurrence",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ParentTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RecursionTime",
                table: "Transactions");
        }
    }
}

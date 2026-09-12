using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class RemovePromotionChainId_FixCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PromotionBranches_Promotions_PromotionId",
                table: "PromotionBranches");

            migrationBuilder.DropForeignKey(
                name: "FK_PromotionProducts_Promotions_PromotionId",
                table: "PromotionProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_Promotions_Chains_ChainId",
                table: "Promotions");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_ChainId",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "ChainId",
                table: "Promotions");

            migrationBuilder.AddForeignKey(
                name: "FK_PromotionBranches_Promotions_PromotionId",
                table: "PromotionBranches",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PromotionProducts_Promotions_PromotionId",
                table: "PromotionProducts",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PromotionBranches_Promotions_PromotionId",
                table: "PromotionBranches");

            migrationBuilder.DropForeignKey(
                name: "FK_PromotionProducts_Promotions_PromotionId",
                table: "PromotionProducts");

            migrationBuilder.AddColumn<long>(
                name: "ChainId",
                table: "Promotions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_ChainId",
                table: "Promotions",
                column: "ChainId");

            migrationBuilder.AddForeignKey(
                name: "FK_PromotionBranches_Promotions_PromotionId",
                table: "PromotionBranches",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PromotionProducts_Promotions_PromotionId",
                table: "PromotionProducts",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Promotions_Chains_ChainId",
                table: "Promotions",
                column: "ChainId",
                principalTable: "Chains",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

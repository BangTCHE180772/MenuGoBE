using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRecipeTableAndDirectProductRecipes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Recipes_RecipeId",
                table: "OrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_RecipesDetaileds_Products_ProductId",
                table: "RecipesDetaileds");

            migrationBuilder.DropForeignKey(
                name: "FK_RecipesDetaileds_Recipes_RecipeId",
                table: "RecipesDetaileds");

            migrationBuilder.Sql(@"
                UPDATE ""RecipesDetaileds"" rd
                SET ""RecipeId"" = r.""ProductId""
                FROM ""Recipes"" r
                WHERE rd.""RecipeId"" = r.""Id"";

                DELETE FROM ""RecipesDetaileds""
                WHERE ""RecipeId"" NOT IN (SELECT ""Id"" FROM ""Products"");
            ");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_OrderDetails_RecipeId",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "RecipeId",
                table: "OrderDetails");

            migrationBuilder.RenameColumn(
                name: "RecipeId",
                table: "RecipesDetaileds",
                newName: "ParentProductId");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "RecipesDetaileds",
                newName: "IngredientProductId");

            migrationBuilder.RenameIndex(
                name: "IX_RecipesDetaileds_RecipeId",
                table: "RecipesDetaileds",
                newName: "IX_RecipesDetaileds_ParentProductId");

            migrationBuilder.RenameIndex(
                name: "IX_RecipesDetaileds_ProductId",
                table: "RecipesDetaileds",
                newName: "IX_RecipesDetaileds_IngredientProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecipesDetaileds_Products_IngredientProductId",
                table: "RecipesDetaileds",
                column: "IngredientProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RecipesDetaileds_Products_ParentProductId",
                table: "RecipesDetaileds",
                column: "ParentProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecipesDetaileds_Products_IngredientProductId",
                table: "RecipesDetaileds");

            migrationBuilder.DropForeignKey(
                name: "FK_RecipesDetaileds_Products_ParentProductId",
                table: "RecipesDetaileds");

            migrationBuilder.RenameColumn(
                name: "ParentProductId",
                table: "RecipesDetaileds",
                newName: "RecipeId");

            migrationBuilder.RenameColumn(
                name: "IngredientProductId",
                table: "RecipesDetaileds",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_RecipesDetaileds_ParentProductId",
                table: "RecipesDetaileds",
                newName: "IX_RecipesDetaileds_RecipeId");

            migrationBuilder.RenameIndex(
                name: "IX_RecipesDetaileds_IngredientProductId",
                table: "RecipesDetaileds",
                newName: "IX_RecipesDetaileds_ProductId");

            migrationBuilder.AddColumn<long>(
                name: "RecipeId",
                table: "OrderDetails",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    VersionName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recipes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_RecipeId",
                table: "OrderDetails",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_ProductId",
                table: "Recipes",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Recipes_RecipeId",
                table: "OrderDetails",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RecipesDetaileds_Products_ProductId",
                table: "RecipesDetaileds",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RecipesDetaileds_Recipes_RecipeId",
                table: "RecipesDetaileds",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

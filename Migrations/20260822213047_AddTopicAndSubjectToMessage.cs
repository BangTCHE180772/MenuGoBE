using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicAndSubjectToMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "Messages",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "Messages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Subject",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "Messages");
        }
    }
}

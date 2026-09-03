using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestChatAndProductConsultation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "GuestChatSessionId",
                table: "Messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MessageType",
                table: "Messages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "Messages",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CustomerId",
                table: "Conversations",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "GuestChatSessionId",
                table: "Conversations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AnonymousChatRetentionMinutes",
                table: "Branches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "GuestChatSessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuestId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuestName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActiveTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestChatSessions_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_GuestChatSessionId",
                table: "Messages",
                column: "GuestChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_GuestChatSessionId",
                table: "Conversations",
                column: "GuestChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestChatSessions_BranchId",
                table: "GuestChatSessions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestChatSessions_GuestId",
                table: "GuestChatSessions",
                column: "GuestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestChatSessions_LastActiveTime",
                table: "GuestChatSessions",
                column: "LastActiveTime");

            migrationBuilder.CreateIndex(
                name: "IX_GuestChatSessions_Token",
                table: "GuestChatSessions",
                column: "Token");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_GuestChatSessions_GuestChatSessionId",
                table: "Conversations",
                column: "GuestChatSessionId",
                principalTable: "GuestChatSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_GuestChatSessions_GuestChatSessionId",
                table: "Messages",
                column: "GuestChatSessionId",
                principalTable: "GuestChatSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_GuestChatSessions_GuestChatSessionId",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_GuestChatSessions_GuestChatSessionId",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "GuestChatSessions");

            migrationBuilder.DropIndex(
                name: "IX_Messages_GuestChatSessionId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_GuestChatSessionId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "GuestChatSessionId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MessageType",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "GuestChatSessionId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "AnonymousChatRetentionMinutes",
                table: "Branches");

            migrationBuilder.AlterColumn<long>(
                name: "CustomerId",
                table: "Conversations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}

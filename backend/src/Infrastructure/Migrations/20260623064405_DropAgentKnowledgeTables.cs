using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropAgentKnowledgeTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_knowledge_files");

            migrationBuilder.DropTable(
                name: "agent_knowledge_folders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agent_knowledge_folders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: false, collation: "ascii_general_ci"),
                    agent_id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: false, collation: "ascii_general_ci"),
                    created_by_user_id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: false, collation: "ascii_general_ci"),
                    parent_folder_id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: true, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    modified_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_knowledge_folders", x => x.id);
                    table.ForeignKey(
                        name: "FK_agent_knowledge_folders_agent_knowledge_folders_parent_folde~",
                        column: x => x.parent_folder_id,
                        principalTable: "agent_knowledge_folders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_agent_knowledge_folders_agents_agent_id",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_agent_knowledge_folders_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "agent_knowledge_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: false, collation: "ascii_general_ci"),
                    agent_id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: false, collation: "ascii_general_ci"),
                    created_by_user_id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: false, collation: "ascii_general_ci"),
                    folder_id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: true, collation: "ascii_general_ci"),
                    content_type = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    extension = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    original_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    storage_key = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_knowledge_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_agent_knowledge_files_agent_knowledge_folders_folder_id",
                        column: x => x.folder_id,
                        principalTable: "agent_knowledge_folders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_agent_knowledge_files_agents_agent_id",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_agent_knowledge_files_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_agent_knowledge_files_agent_id",
                table: "agent_knowledge_files",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "IX_agent_knowledge_files_created_by_user_id",
                table: "agent_knowledge_files",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_agent_knowledge_files_folder_id",
                table: "agent_knowledge_files",
                column: "folder_id");

            migrationBuilder.CreateIndex(
                name: "IX_agent_knowledge_files_storage_key",
                table: "agent_knowledge_files",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_agent_knowledge_folders_agent_id",
                table: "agent_knowledge_folders",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "IX_agent_knowledge_folders_agent_id_parent_folder_id_name",
                table: "agent_knowledge_folders",
                columns: new[] { "agent_id", "parent_folder_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_agent_knowledge_folders_created_by_user_id",
                table: "agent_knowledge_folders",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_agent_knowledge_folders_parent_folder_id",
                table: "agent_knowledge_folders",
                column: "parent_folder_id");
        }
    }
}

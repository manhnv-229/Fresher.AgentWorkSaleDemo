using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Infrastructure.Persistence.Migrations;

public partial class RefactorBusinessModules : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "code",
            table: "agents",
            type: "varchar(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<Guid>(
            name: "created_by_user_id",
            table: "agents",
            type: "char(36)",
            maxLength: 36,
            nullable: false,
            defaultValue: Guid.Empty);

        migrationBuilder.AddColumn<Guid>(
            name: "modified_by_user_id",
            table: "agents",
            type: "char(36)",
            maxLength: 36,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "published_at",
            table: "agents",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "deleted_at",
            table: "agents",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "modified_at",
            table: "agents",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "password_changed_at",
            table: "users",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "modified_at",
            table: "users",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "modified_at",
            table: "tenants",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "modified_at",
            table: "roles",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "modified_at",
            table: "user_tenants",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "assigned_at",
            table: "user_roles",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "agent_knowledge_folders",
            columns: table => new
            {
                id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: false),
                agent_id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: false),
                parent_folder_id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: true),
                created_by_user_id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: false),
                name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                modified_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_agent_knowledge_folders", x => x.id);
                table.ForeignKey(
                    name: "FK_agent_knowledge_folders_agent_knowledge_folders_parent_folde",
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
            });

        migrationBuilder.CreateTable(
            name: "agent_knowledge_files",
            columns: table => new
            {
                id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: false),
                agent_id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: false),
                folder_id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: true),
                created_by_user_id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: false),
                name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                original_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                content_type = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                extension = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                storage_key = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                size_bytes = table.Column<long>(type: "bigint", nullable: false),
                created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                modified_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
            });

        migrationBuilder.CreateTable(
            name: "audit_logs",
            columns: table => new
            {
                id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: false),
                user_id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: true),
                tenant_id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: true),
                action = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                user_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                target_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                target_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                ip_address = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                description = table.Column<string>(type: "longtext", nullable: false),
                created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_audit_logs", x => x.id);
                table.ForeignKey(
                    name: "FK_audit_logs_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_audit_logs_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(name: "IX_agents_code", table: "agents", column: "code");
        migrationBuilder.CreateIndex(name: "IX_agents_created_by_user_id", table: "agents", column: "created_by_user_id");
        migrationBuilder.CreateIndex(name: "IX_agents_modified_by_user_id", table: "agents", column: "modified_by_user_id");
        migrationBuilder.CreateIndex(name: "IX_agents_tenant_id_code", table: "agents", columns: new[] { "tenant_id", "code" }, unique: true);
        migrationBuilder.AddForeignKey(
            name: "FK_agents_users_created_by_user_id",
            table: "agents",
            column: "created_by_user_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(
            name: "FK_agents_users_modified_by_user_id",
            table: "agents",
            column: "modified_by_user_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.CreateIndex(name: "IX_agent_knowledge_folders_agent_id", table: "agent_knowledge_folders", column: "agent_id");
        migrationBuilder.CreateIndex(name: "IX_agent_knowledge_folders_created_by_user_id", table: "agent_knowledge_folders", column: "created_by_user_id");
        migrationBuilder.CreateIndex(name: "IX_agent_knowledge_folders_parent_folder_id", table: "agent_knowledge_folders", column: "parent_folder_id");
        migrationBuilder.CreateIndex(name: "IX_agent_knowledge_folders_agent_id_parent_folder_id_name", table: "agent_knowledge_folders", columns: new[] { "agent_id", "parent_folder_id", "name" }, unique: true);
        migrationBuilder.AddForeignKey(
            name: "FK_agent_knowledge_folders_users_created_by_user_id",
            table: "agent_knowledge_folders",
            column: "created_by_user_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.CreateIndex(name: "IX_agent_knowledge_files_agent_id", table: "agent_knowledge_files", column: "agent_id");
        migrationBuilder.CreateIndex(name: "IX_agent_knowledge_files_created_by_user_id", table: "agent_knowledge_files", column: "created_by_user_id");
        migrationBuilder.CreateIndex(name: "IX_agent_knowledge_files_folder_id", table: "agent_knowledge_files", column: "folder_id");
        migrationBuilder.CreateIndex(name: "IX_agent_knowledge_files_storage_key", table: "agent_knowledge_files", column: "storage_key", unique: true);
        migrationBuilder.AddForeignKey(
            name: "FK_agent_knowledge_files_users_created_by_user_id",
            table: "agent_knowledge_files",
            column: "created_by_user_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.CreateIndex(name: "IX_audit_logs_action", table: "audit_logs", column: "action");
        migrationBuilder.CreateIndex(name: "IX_audit_logs_created_at", table: "audit_logs", column: "created_at");
        migrationBuilder.CreateIndex(name: "IX_audit_logs_tenant_id", table: "audit_logs", column: "tenant_id");
        migrationBuilder.CreateIndex(name: "IX_audit_logs_user_id", table: "audit_logs", column: "user_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_agents_users_created_by_user_id", table: "agents");
        migrationBuilder.DropForeignKey(name: "FK_agents_users_modified_by_user_id", table: "agents");
        migrationBuilder.DropForeignKey(name: "FK_agent_knowledge_files_users_created_by_user_id", table: "agent_knowledge_files");
        migrationBuilder.DropForeignKey(name: "FK_agent_knowledge_folders_users_created_by_user_id", table: "agent_knowledge_folders");

        migrationBuilder.DropIndex(name: "IX_agents_code", table: "agents");
        migrationBuilder.DropIndex(name: "IX_agents_created_by_user_id", table: "agents");
        migrationBuilder.DropIndex(name: "IX_agents_modified_by_user_id", table: "agents");
        migrationBuilder.DropIndex(name: "IX_agents_tenant_id_code", table: "agents");

        migrationBuilder.DropTable(name: "audit_logs");
        migrationBuilder.DropTable(name: "agent_knowledge_files");
        migrationBuilder.DropTable(name: "agent_knowledge_folders");

        migrationBuilder.DropColumn(name: "code", table: "agents");
        migrationBuilder.DropColumn(name: "created_by_user_id", table: "agents");
        migrationBuilder.DropColumn(name: "modified_by_user_id", table: "agents");
        migrationBuilder.DropColumn(name: "published_at", table: "agents");
        migrationBuilder.DropColumn(name: "deleted_at", table: "agents");
        migrationBuilder.DropColumn(name: "modified_at", table: "agents");

        migrationBuilder.DropColumn(name: "password_changed_at", table: "users");
        migrationBuilder.DropColumn(name: "modified_at", table: "users");
        migrationBuilder.DropColumn(name: "modified_at", table: "tenants");
        migrationBuilder.DropColumn(name: "modified_at", table: "roles");
        migrationBuilder.DropColumn(name: "modified_at", table: "user_tenants");
        migrationBuilder.DropColumn(name: "assigned_at", table: "user_roles");
    }
}

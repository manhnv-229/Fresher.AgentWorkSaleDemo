using Demo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Demo.Infrastructure.Migrations;

[DbContext(typeof(DemoDbContext))]
[Migration("20260624172000_MakeKnowledgeChecksumIndexNonUnique")]
public partial class MakeKnowledgeChecksumIndexNonUnique : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_knowledge_storage_objects_checksum_sha256_size_bytes",
            table: "knowledge_storage_objects");

        migrationBuilder.CreateIndex(
            name: "IX_knowledge_storage_objects_checksum_sha256_size_bytes",
            table: "knowledge_storage_objects",
            columns: new[] { "checksum_sha256", "size_bytes" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_knowledge_storage_objects_checksum_sha256_size_bytes",
            table: "knowledge_storage_objects");

        migrationBuilder.CreateIndex(
            name: "IX_knowledge_storage_objects_checksum_sha256_size_bytes",
            table: "knowledge_storage_objects",
            columns: new[] { "checksum_sha256", "size_bytes" },
            unique: true);
    }
}

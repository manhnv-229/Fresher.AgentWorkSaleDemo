using Demo.Application.Services;
using Demo.Domain.Entities;
using Demo.Domain.Enums;

namespace Demo.Application.Tests.Services;

/// <summary>
/// Kiểm tra các helper xử lý cây thư mục và mapping knowledge cơ bản.
/// </summary>
public sealed class KnowledgeServiceHelperTests
{
    [Fact]
    public void BuildStorageObjectKey_ShouldUseInternalPrefixForEmptyTenant()
    {
        var agentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var fileId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var result = KnowledgeServiceHelper.BuildStorageObjectKey(Guid.Empty, agentId, fileId, ".pdf");

        result.Should().Be("internal/agents/11111111111111111111111111111111/knowledge/22222222222222222222222222222222.pdf");
    }

    [Fact]
    public void BuildStorageObjectKey_ShouldUseTenantPrefixForTenantScope()
    {
        var tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var agentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var fileId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var result = KnowledgeServiceHelper.BuildStorageObjectKey(tenantId, agentId, fileId, ".txt");

        result.Should().Be("tenants/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/agents/11111111111111111111111111111111/knowledge/22222222222222222222222222222222.txt");
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("  Folder A  ", "Folder A")]
    public void NormalizeDisplayName_ShouldTrimAndReturnNullForEmpty(string value, string? expected)
    {
        var result = KnowledgeServiceHelper.NormalizeDisplayName(value);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("  folder a  ", "FOLDER A")]
    public void NormalizeName_ShouldTrimAndUppercase(string? value, string expected)
    {
        var result = KnowledgeServiceHelper.NormalizeName(value);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("application/json", ".pdf", "application/json")]
    [InlineData("", ".pdf", "application/pdf")]
    [InlineData("", ".jpeg", "image/jpeg")]
    [InlineData("", ".bin", "application/octet-stream")]
    public void NormalizeContentType_ShouldPreferGivenContentTypeOrMapFromExtension(string contentType, string extension, string expected)
    {
        var result = KnowledgeServiceHelper.NormalizeContentType(contentType, extension);

        result.Should().Be(expected);
    }

    [Fact]
    public void BuildTree_ShouldCreateSortedHierarchy()
    {
        var rootAId = Guid.Parse("10000000-0000-0000-0000-000000000000");
        var rootBId = Guid.Parse("20000000-0000-0000-0000-000000000000");
        var childId = Guid.Parse("30000000-0000-0000-0000-000000000000");
        var folders = new[]
        {
            CreateFolder(rootBId, null, "Zeta"),
            CreateFolder(childId, rootAId, "Child"),
            CreateFolder(rootAId, null, "Alpha")
        };

        var result = KnowledgeServiceHelper.BuildTree(folders, null);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(rootAId);
        result[0].Children.Should().ContainSingle().Which.Id.Should().Be(childId);
        result[1].Id.Should().Be(rootBId);
    }

    [Fact]
    public void BuildBreadcrumb_ShouldReturnPathFromRootToSelectedFolder()
    {
        var rootId = Guid.Parse("10000000-0000-0000-0000-000000000000");
        var childId = Guid.Parse("20000000-0000-0000-0000-000000000000");
        var leafId = Guid.Parse("30000000-0000-0000-0000-000000000000");
        var folders = new[]
        {
            CreateFolder(rootId, null, "Root"),
            CreateFolder(childId, rootId, "Child"),
            CreateFolder(leafId, childId, "Leaf")
        };

        var result = KnowledgeServiceHelper.BuildBreadcrumb(folders, folders[2]);

        result.Select(item => item.Name).Should().Equal("Root", "Child", "Leaf");
    }

    [Fact]
    public void IsDescendant_ShouldReturnTrueOnlyForNestedFolder()
    {
        var rootId = Guid.Parse("10000000-0000-0000-0000-000000000000");
        var childId = Guid.Parse("20000000-0000-0000-0000-000000000000");
        var leafId = Guid.Parse("30000000-0000-0000-0000-000000000000");
        var otherId = Guid.Parse("40000000-0000-0000-0000-000000000000");
        var folders = new[]
        {
            CreateFolder(rootId, null, "Root"),
            CreateFolder(childId, rootId, "Child"),
            CreateFolder(leafId, childId, "Leaf"),
            CreateFolder(otherId, null, "Other")
        };

        KnowledgeServiceHelper.IsDescendant(folders, rootId, leafId).Should().BeTrue();
        KnowledgeServiceHelper.IsDescendant(folders, childId, rootId).Should().BeFalse();
        KnowledgeServiceHelper.IsDescendant(folders, otherId, leafId).Should().BeFalse();
    }

    [Fact]
    public void MapFile_ShouldFallbackToDefaultValuesWhenStorageOrUserMissing()
    {
        var fileId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();
        var file = new AgentKnowledgeFile
        {
            Id = fileId,
            Name = "handbook",
            OriginalName = "Handbook.pdf",
            Extension = ".pdf",
            Status = AgentKnowledgeFileStatus.Active,
            CreatedByUserId = createdByUserId,
            CreatedAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = KnowledgeServiceHelper.MapFile(file);

        result.Id.Should().Be(fileId);
        result.ContentType.Should().Be("application/octet-stream");
        result.SizeBytes.Should().Be(0);
        result.CreatedByUserName.Should().Be("Unknown");
    }

    [Fact]
    public void MapFileDetail_ShouldPreferResolvedUserAndStorageValues()
    {
        var file = new AgentKnowledgeFile
        {
            Id = Guid.NewGuid(),
            Name = "handbook",
            OriginalName = "Handbook.pdf",
            Extension = ".pdf",
            Status = AgentKnowledgeFileStatus.Active,
            CreatedByUserId = Guid.NewGuid(),
            CreatedAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            StorageObject = new KnowledgeStorageObject
            {
                ContentType = "application/pdf",
                SizeBytes = 1024,
                StorageBucket = "knowledge",
                StorageObjectKey = "tenant/file.pdf"
            }
        };

        var result = KnowledgeServiceHelper.MapFileDetail(file, "Nguyen Van A");

        result.ContentType.Should().Be("application/pdf");
        result.SizeBytes.Should().Be(1024);
        result.StorageBucket.Should().Be("knowledge");
        result.StorageObjectKey.Should().Be("tenant/file.pdf");
        result.CreatedByUserName.Should().Be("Nguyen Van A");
    }

    private static AgentKnowledgeFolder CreateFolder(Guid id, Guid? parentFolderId, string name)
    {
        return new AgentKnowledgeFolder
        {
            Id = id,
            ParentFolderId = parentFolderId,
            Name = name,
            NormalizedName = name.ToUpperInvariant()
        };
    }
}

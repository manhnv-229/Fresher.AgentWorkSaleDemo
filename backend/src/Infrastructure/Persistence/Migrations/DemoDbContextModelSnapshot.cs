using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

using Demo.Infrastructure.Persistence;

#nullable disable

namespace Demo.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DemoDbContext))]
public partial class DemoDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "9.0.0");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DemoDbContext).Assembly);
    }
}

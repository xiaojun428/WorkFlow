using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WorkflowSystem.Infrastructure.Persistence;

/// <summary>
/// 设计时 DbContext 工厂：供 `dotnet ef migrations add` / `database update` 使用。
/// 连接字符串从环境变量读取，避免硬编码到仓库。
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<WorkflowDbContext>
{
    public WorkflowDbContext CreateDbContext(string[] args)
    {
        // 设计时连接字符串：优先环境变量，缺省使用本地开发实例。
        var connectionString =
            Environment.GetEnvironmentVariable("WorkflowDb")
            ?? "Server=localhost\\MSSQLSERVER1;Database=WorkflowSystem;Trusted_Connection=True;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new WorkflowDbContext(options);
    }
}

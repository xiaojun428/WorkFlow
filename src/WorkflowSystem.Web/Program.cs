using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Globalization;
using WorkflowSystem.Application;
using WorkflowSystem.Application.Abstractions;
using WorkflowSystem.Infrastructure;
using WorkflowSystem.Workflows;
using WorkflowSystem.Web.Authorization;
using WorkflowSystem.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// 使用 Serilog 结构化日志：先读取配置，再作为默认日志工厂。
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)          // 从 appsettings 读取 Serilog 配置
    .Enrich.FromLogContext()
    .WriteTo.Console()                                      // 基础输出，生产环境可在配置中改为文件/Sink
    .CreateLogger();

// 用 Serilog 替换默认日志提供程序。
builder.Host.UseSerilog();

// 应用层 / 基础设施层 / 工作流层服务注册（组合根）。
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWorkflows(builder.Configuration);

// MVC + Razor Views + 本地化（中英切换，需求 10）。
builder.Services.AddControllersWithViews()
    .AddViewLocalization();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// 支持的中文与英文文化。
var supportedCultures = new[]
{
    new CultureInfo("zh-CN"),
    new CultureInfo("en-US"),
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("zh-CN");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    // 语言选择：优先 query string 的 culture 参数，其次 cookie，最后浏览器 Accept-Language。
    options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider { QueryStringKey = "culture" });
});

// 认证：Entra OIDC（或开发环境回退 Cookie）。
builder.Services.AddWorkflowAuthentication(builder.Configuration);

// 当前用户上下文（依赖 HttpContext）。
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// 授权：Policy-based + 资源级 AuthorizationHandler（阶段 5）。
builder.Services.AddAuthorization(options => options.AddPermissionPolicies());
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddScoped<IAuthorizationHandler, DocumentAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ApprovalTaskAuthorizationHandler>();

// 健康检查（阶段 2 提供 /health 探活端点，供部署与监控使用）。
builder.Services.AddHealthChecks();

// Swagger / OpenAPI：后续阶段仅对需要的 API 控制器启用。
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 启动时应用业务库迁移（确保部署后表结构就绪；生产环境也可通过部署脚本显式迁移）。
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WorkflowSystem.Infrastructure.Persistence.WorkflowDbContext>();
    db.Database.Migrate();
}

// 生产环境统一异常处理：不返回堆栈与内部信息。
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // 开发环境启用 Swagger。
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();

// 本地化中间件（需在路由后、认证前启用 RequestCulture）。
app.UseRequestLocalization();

app.UseAuthentication();
app.UseAuthorization();

// 静态资源与健康检查端点。
app.MapStaticAssets();
app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// 让集成测试能够通过 WebApplicationFactory 访问 Program。
public partial class Program { }

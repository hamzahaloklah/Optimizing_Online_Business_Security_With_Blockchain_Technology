using Microsoft.EntityFrameworkCore;
using SportsStore.Models;
using SportsStore.Services;

var builder = WebApplication.CreateBuilder(args);

// 🔧 تحميل إعدادات النود
string? nodeConfig = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT_SPECIFIC");

if (!string.IsNullOrEmpty(nodeConfig))
{
    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile($"appsettings.{nodeConfig}.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

    Console.WriteLine($"Loaded configuration file: appsettings.{nodeConfig}.json");
}
else
{
    builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    Console.WriteLine("Loaded default configuration file: appsettings.json");
}

// 🔧 Services
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

// 🔥 HttpClient عام
builder.Services.AddHttpClient();

// 🔧 Database
var connectionString = builder.Configuration["ConnectionStrings:SportsStoreConnection"];
builder.Services.AddDbContext<StoreDbContext>(opts =>
{
    opts.UseSqlServer(connectionString);
});
Console.WriteLine($"Active Database Connection: {connectionString}");

// 🔧 Core Services
builder.Services.AddSingleton<KeyGeneratorService>();
builder.Services.AddSingleton<NodeService>();

// 🔧 Communication (مع Cluster Head فقط)
builder.Services.AddHttpClient<NodeCommunicationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
});

// 🔧 Blockchain
builder.Services.AddSingleton<BlockchainService>();

// 🔧 Store
builder.Services.AddScoped<IStoreRepository, EFStoreRepository>();
builder.Services.AddScoped<Cart>(sp => SessionCart.GetCart(sp));

var app = builder.Build();

// 🔧 Debug Info
var nodeService = app.Services.GetRequiredService<NodeService>();

Console.WriteLine("\n--- Node Initialization ---");
Console.WriteLine($"Node ID (before registration): {nodeService.CurrentNode.NodeId}");
Console.WriteLine("---------------------------");

// 🔧 Middleware
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

// 🔧 Routes
app.MapControllerRoute(
    name: "catpage",
    pattern: "{category}/Page{productPage:int}",
    defaults: new { controller = "Node", action = "Index" });

app.MapControllerRoute(
    name: "page",
    pattern: "Page{productPage:int}",
    defaults: new { controller = "Node", action = "Index", productPage = 1 });

app.MapControllerRoute(
    name: "category",
    pattern: "{category}",
    defaults: new { controller = "Node", action = "Index", productPage = 1 });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Node}/{action=Index}/{id?}");

app.MapRazorPages();

// 🔧 Seed DB
SeedData.EnsurePopulated(app);

Console.WriteLine("Application is fully initialized and ready!");
Console.WriteLine($"Node is listening on: {builder.Configuration["NODE_ADDRESS"] ?? "http://localhost:5000"}\n");

app.Run();
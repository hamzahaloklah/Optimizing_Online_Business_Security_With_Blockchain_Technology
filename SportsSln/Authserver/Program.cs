using AuthServer.Services;

var builder = WebApplication.CreateBuilder(args);

// ================= SERVICES =================
builder.Services.AddControllers();

builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<RegistryService>();

// 🔐 CORS (للسماح للنود بالاتصال)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

// ================= BUILD =================
var app = builder.Build();

// ================= MIDDLEWARE =================
app.UseCors("AllowAll");

// (اختياري) إذا بدك HTTPS
// app.UseHttpsRedirection();

// ================= ROUTING =================
app.MapControllers();

// ================= RUN =================
app.Run("http://localhost:4000");
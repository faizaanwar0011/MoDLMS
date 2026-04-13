using MoDLibrary.Hubs;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

var wkhtmltoxPath = Path.Combine(AppContext.BaseDirectory, "libwkhtmltox.dll");
if (OperatingSystem.IsWindows() && File.Exists(wkhtmltoxPath))
{
    NativeLibrary.Load(wkhtmltoxPath);
}

builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

builder.Services.AddControllersWithViews();

builder.Services.AddSignalR();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Account/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Member}/{action=Index}/{id?}");

app.MapHub<NotificationHub>("/notificationHub");

app.Run();
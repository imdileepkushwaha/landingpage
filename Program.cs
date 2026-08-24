using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Data;
using SoftflipSolutions.Filters;
using SoftflipSolutions.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var mvcBuilder = builder.Services.AddControllersWithViews();
if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}
builder.Services.AddMemoryCache();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IDealPdfService, DealPdfService>();
builder.Services.AddScoped<IEmployeeDocumentPdfService, EmployeeDocumentPdfService>();
builder.Services.AddScoped<IEmployeeAccessService, EmployeeAccessService>();
builder.Services.AddScoped<ICompanyProfileService, CompanyProfileService>();
builder.Services.AddScoped<IPartnerVisitingCardService, PartnerVisitingCardService>();
builder.Services.AddScoped<IPartnerCertificateService, PartnerCertificateService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEmailLogService, EmailLogService>();
builder.Services.AddScoped<IAdminAccessService, AdminAccessService>();
builder.Services.AddScoped<AdminMenuAccessFilter>();
builder.Services.AddSingleton<ICaptchaService, CaptchaService>();
builder.Services.AddSingleton<IFormSpamGuard, FormSpamGuardService>();
builder.Services.AddSingleton<IPhoneValidationService, PhoneValidationService>();

builder.Services.AddAuthentication("AdminCookie")
    .AddCookie("AdminCookie", options =>
    {
        options.Cookie.Name = "AdminAuth";
        options.LoginPath = "/Admin/Login";
        options.LogoutPath = "/Admin/Logout";
        options.AccessDeniedPath = "/Admin/AccessDenied";
    })
    .AddCookie("PartnerCookie", options =>
    {
        options.Cookie.Name = "PartnerAuth";
        options.LoginPath = "/Partner/Login";
        options.LogoutPath = "/Partner/Logout";
        options.AccessDeniedPath = "/Partner/Login";
    })
    .AddCookie("EmployeeCookie", options =>
    {
        options.Cookie.Name = "EmployeeAuth";
        options.LoginPath = "/Employee/Login";
        options.LogoutPath = "/Employee/Logout";
        options.AccessDeniedPath = "/Employee/Login";
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Classic wwwroot serving — uploads + CSS/JS. Avoid MapStaticAssets compressed
// endpoints (site.css.br/.gz), which 500 on live when files are out of sync after partial deploy.
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        context.Response.OnStarting(() =>
        {
            var path = context.Request.Path.Value ?? "";
            if (path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";
            }
            return Task.CompletedTask;
        });
        await next();
    });
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Repositories;
using WebBanHang.Services;
using WedBanHang.Models;
using WedBanHang.Repositories;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddDefaultTokenProviders()
    .AddDefaultUI()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddRazorPages();

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IProductRepository, EFProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();

builder.Services.AddSingleton<IUserIdProvider, NameUserIdProvider>();

builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();

builder.Services.AddScoped<EmailService>();

builder.Services.AddAuthentication()
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = "613083317375-s56f2hokdpuf4hnj45f4t098iinsfg6h.apps.googleusercontent.com";
        googleOptions.ClientSecret = "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";
    });
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();


app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
    name: "Admin",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
    endpoints.MapControllerRoute(
    name: "default",
    pattern: "{controller=Product}/{action=Index}/{id?}");
    endpoints.MapHub<ChatHub>("/chathub");
});

async Task CreateAdminUserAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Đảm bảo Role Admin đã tồn tại
    if (!await roleManager.RoleExistsAsync(SD.Role_Admin))
    {
        await roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
    }

    // Tạo admin nếu chưa tồn tại
    var adminEmail = "admin@gmail.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        var user = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,

        };

        var result = await userManager.CreateAsync(user, "Aa@123");

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, SD.Role_Admin);
        }
    }
}
async Task CreateSuperAdminUserAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Tạo các vai trò nếu chưa có
    string[] roles = { SD.Role_Admin, SD.Role_SuperAdmin, SD.Role_User };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // ✅ Tạo SuperAdmin nếu chưa có
    var superAdminEmail = "superadmin@gmail.com";
    var superAdminUser = await userManager.FindByEmailAsync(superAdminEmail);

    if (superAdminUser == null)
    {
        var user = new ApplicationUser
        {
            UserName = superAdminEmail,
            Email = superAdminEmail
        };

        var result = await userManager.CreateAsync(user, "Aa@123");

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, SD.Role_SuperAdmin);
            await userManager.AddToRoleAsync(user, SD.Role_Admin); // ✅ Kế thừa quyền admin
        }
    }
}


await CreateAdminUserAsync(app);
await CreateSuperAdminUserAsync(app);

app.MapRazorPages();
app.Run();

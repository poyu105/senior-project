using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using senior_project_web.Data;

var builder = WebApplication.CreateBuilder(args);

//註冊資料庫
builder.Services.AddDbContext<OrderSystemDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("OrderSystem"));
});

//全域範圍驗證機制
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    //未登入時自動轉移至此網址
    //option.LoginPath = new PathString("/Home/Login");
    //未授權角色時會轉移至此網址
    //option.AccessDeniedPath = new PathString("/Home/NoRole");
    //登入後10分鐘後失效
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

//登入驗證機制(順序不能顛倒)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

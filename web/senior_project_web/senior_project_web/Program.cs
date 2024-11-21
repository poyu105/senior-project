using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using senior_project_web.Data;

var builder = WebApplication.CreateBuilder(args);

//���U��Ʈw
builder.Services.AddDbContext<OrderSystemDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("OrderSystem"));
});

//����d�����Ҿ���
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    //���n�J�ɦ۰��ಾ�ܦ����}
    //option.LoginPath = new PathString("/Home/Login");
    //�����v����ɷ|�ಾ�ܦ����}
    //option.AccessDeniedPath = new PathString("/Home/NoRole");
    //�n�J��10�����ᥢ��
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

//�n�J���Ҿ���(���Ǥ����A��)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

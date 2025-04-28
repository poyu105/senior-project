using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using orderSys_bk.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrderSysDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//註冊CORS(允許來自不同來源的請求)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        //policy.AllowAnyOrigin() //允許來自所有來源的請求
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174") //允許來自特定前端請求
              .AllowAnyMethod() //允許所有HTTP方法(GET, POST, PUT, DELETE)
              .AllowAnyHeader(); //允許所有標頭
    });
});

// 註冊身份驗證服務並設置 Bearer Token 驗證
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false, // 設置是否驗證 Issuer
            ValidateAudience = false, // 設置是否驗證 Audience
            ValidateLifetime = true, // 設置是否驗證 JWT 是否過期
            ClockSkew = TimeSpan.Zero, // 即時檢查過期設為零，預設是5分鐘
            ValidateIssuerSigningKey = true, // 驗證密鑰是否正確
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("1234567890_abcdefghijklmnopqrstuvwxyz_ABCDEFGHIJKLMNOPQRSTUVWXYZ_0987654321")), // 使用密鑰來驗證 JWT 
        };
    });

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
/*if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}*/

app.UseHttpsRedirection();

// 啟用 CORS 策略
app.UseCors("AllowAll");

// 啟用 wwwroot 資料夾裡的靜態檔案
app.UseStaticFiles();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

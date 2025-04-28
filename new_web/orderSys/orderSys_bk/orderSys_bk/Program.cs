using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using orderSys_bk.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrderSysDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//���UCORS(���\�Ӧۤ��P�ӷ����ШD)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        //policy.AllowAnyOrigin() //���\�Ӧ۩Ҧ��ӷ����ШD
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174") //���\�ӦۯS�w�e�ݽШD
              .AllowAnyMethod() //���\�Ҧ�HTTP��k(GET, POST, PUT, DELETE)
              .AllowAnyHeader(); //���\�Ҧ����Y
    });
});

// ���U�������ҪA�Ȩó]�m Bearer Token ����
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false, // �]�m�O�_���� Issuer
            ValidateAudience = false, // �]�m�O�_���� Audience
            ValidateLifetime = true, // �]�m�O�_���� JWT �O�_�L��
            ClockSkew = TimeSpan.Zero, // �Y���ˬd�L���]���s�A�w�]�O5����
            ValidateIssuerSigningKey = true, // ���ұK�_�O�_���T
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("1234567890_abcdefghijklmnopqrstuvwxyz_ABCDEFGHIJKLMNOPQRSTUVWXYZ_0987654321")), // �ϥαK�_������ JWT 
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

// �ҥ� CORS ����
app.UseCors("AllowAll");

// �ҥ� wwwroot ��Ƨ��̪��R�A�ɮ�
app.UseStaticFiles();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

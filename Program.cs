using MenengiomaBackend.Data;
using Microsoft.EntityFrameworkCore;
using MenengiomaBackend.Services;

var builder = WebApplication.CreateBuilder(args);
// CORS politikasını ekliyoruz
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// 1. PostgreSQL Veritabanı Bağlantımız
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. EKSİK OLAN SATIR: Projeye Controller kullanacağımızı söylüyoruz
builder.Services.AddControllers();
builder.Services.AddHttpClient<AiIntegrationService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
// Uygulamaya CORS politikasını kullan diyoruz
app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 3. EKSİK OLAN SATIR: Gelen istekleri Controller'lara yönlendiriyoruz
app.MapControllers();

app.Run();
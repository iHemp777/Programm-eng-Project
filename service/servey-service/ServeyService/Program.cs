using Microsoft.EntityFrameworkCore;
using SurveyService.Data;
using System.Text.Json.Serialization;

// Program.cs — точка входа ASP.NET Core приложения.
//
// Здесь настраиваются:
// - DI (внедрение зависимостей): контроллеры, DbContext и прочие сервисы.
// - Middleware pipeline: Swagger, HTTPS redirection, маршрутизация контроллеров.
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // Entity graph contains cycles (Survey -> Questions -> Survey).
        // IgnoreCycles is enough for this API and prevents 500 on serialization.
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Подключение EF Core + PostgreSQL.
// Строка подключения берётся из appsettings*.json: ConnectionStrings:Default
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// Применяем миграции при старте (удобно для Docker/CI и локального запуска).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Включаем маршрутизацию атрибутных контроллеров (например SurveysController).
app.MapControllers();

app.Run();

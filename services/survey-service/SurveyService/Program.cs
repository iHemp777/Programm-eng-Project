using Microsoft.EntityFrameworkCore;
using SurveyService.Data;

// Program.cs — точка входа ASP.NET Core приложения.
//
// Здесь настраиваются:
// - DI (внедрение зависимостей): контроллеры, DbContext и прочие сервисы.
// - Middleware pipeline: Swagger, HTTPS redirection, маршрутизация контроллеров.
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Подключение EF Core + PostgreSQL.
// Строка подключения берётся из appsettings*.json: ConnectionStrings:Default
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

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

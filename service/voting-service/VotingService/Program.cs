using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using VotingService.Data;
using VotingService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var redisConnection = builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException("ConnectionStrings:Redis is not configured.");
    return ConnectionMultiplexer.Connect(redisConnection);
});

builder.Services.AddHttpClient<ISurveyClient, SurveyClient>(client =>
{
    var baseUrl = builder.Configuration["Services:SurveyService:BaseUrl"]
        ?? throw new InvalidOperationException("Services:SurveyService:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddScoped<IStatisticsCacheService, StatisticsCacheService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "api-gateway" }));
app.MapGet("/swagger", () => Results.Content("""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>API Gateway Swagger Links</title>
  <style>
    body { font-family: sans-serif; margin: 2rem; line-height: 1.5; }
    h1 { margin-bottom: 0.5rem; }
    ul { padding-left: 1.25rem; }
    a { text-decoration: none; }
    a:hover { text-decoration: underline; }
  </style>
</head>
<body>
  <h1>Swagger Endpoints</h1>
  <p>Gateway works. Open service Swagger using links below:</p>
  <ul>
    <li><a href="http://localhost:5001/swagger">Survey Service Swagger</a></li>
    <li><a href="http://localhost:5002/swagger">Voting Service Swagger</a></li>
  </ul>
</body>
</html>
""", "text/html"));
app.MapReverseProxy();

app.Run();

public partial class Program;

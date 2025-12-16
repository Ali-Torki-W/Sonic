using Sonic.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSonicApi(builder.Configuration);

var app = builder.Build();

app.UseSonicApiPipeline();

app.Run();

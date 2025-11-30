using MongoDB.Bson;
using MongoDB.Driver;
using Sonic.Infrastructure;
using Sonic.Infrastructure.Config;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddSingleton<MongoDbContext>();

builder.Services.AddControllers();

var app = builder.Build();

// --- Mongo health check ---
app.MapGet("/db-health", async (MongoDbContext dbContext) =>
{
    try
    {
        // This sends a ping to MongoDB, no collections required
        var command = new BsonDocument("ping", 1);
        await dbContext
            .GetDatabase() // we'll add this helper
            .RunCommandAsync<BsonDocument>(command);

        return Results.Ok(new { status = "OK", message = "MongoDB reachable" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"MongoDB health check failed: {ex.Message}");
    }
});

app.MapControllers();

app.Run();

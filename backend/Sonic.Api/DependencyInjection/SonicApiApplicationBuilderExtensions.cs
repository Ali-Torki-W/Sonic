using MongoDB.Bson;
using Sonic.Api.MiddleWares;
using Sonic.Infrastructure.Config;

namespace Sonic.Api;

public static class SonicApiApplicationBuilderExtensions
{
    public static WebApplication UseSonicApiPipeline(this WebApplication app)
    {
        app.UseRouting();

        app.UseCors(SonicApiServiceCollectionExtensions.CorsPolicyName); // NEW: moved above error middleware

        app.UseMiddleware<ErrorHandlingMiddleware>(); // MOVED: now after CORS

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sonic API v1");
            c.RoutePrefix = "swagger";
        });

        app.MapGet("/health", () => Results.Ok(new { status = "OK" })).AllowAnonymous();

        app.MapGet("/db-health", async (MongoDbContext dbContext) =>
        {
            var command = new BsonDocument("ping", 1);
            await dbContext.GetDatabase().RunCommandAsync<BsonDocument>(command);
            return Results.Ok(new { status = "OK", message = "MongoDB reachable" });
        }).AllowAnonymous();

        app.MapControllers();

        return app;
    }
}

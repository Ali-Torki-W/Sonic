using System.Reflection;

namespace Sonic.Tests.TestHelpers;

public static class ExceptionAssert
{
    public static int? TryGetStatusCode(Exception ex)
    {
        // Most likely in your project: ApiException.StatusCode
        var candidates = new[] { "StatusCode", "HttpStatusCode", "Code" };

        foreach (var name in candidates)
        {
            var prop = ex.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (prop is null) continue;

            if (prop.PropertyType == typeof(int))
                return (int?)prop.GetValue(ex);

            // Some libs expose HttpStatusCode enum
            if (prop.PropertyType.IsEnum)
            {
                var value = prop.GetValue(ex);
                if (value is not null)
                    return Convert.ToInt32(value);
            }
        }

        return null;
    }

    public static async Task AssertStatusCodeAsync(Func<Task> act, int expectedStatusCode)
    {
        // IMPORTANT: ThrowsAnyAsync does NOT require exact type match
        var ex = await Assert.ThrowsAnyAsync<Exception>(act);

        var status = TryGetStatusCode(ex);
        Assert.True(status.HasValue, $"Exception {ex.GetType().Name} has no readable status code property.");
        Assert.Equal(expectedStatusCode, status.Value);
    }
}

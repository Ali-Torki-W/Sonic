using System.Text.Json.Serialization;

namespace Sonic.Domain.Posts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PostType
{
    Experience, // 0 in DB - Experience in DTOs and ...
    Idea,
    ModelGuide,
    Course,
    News,
    Campaign
}

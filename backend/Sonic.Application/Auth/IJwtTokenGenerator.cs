using Sonic.Domain.Users;

namespace Sonic.Application.Auth;

public interface IJwtTokenGenerator
{
    AuthToken GenerateToken(User user);
}

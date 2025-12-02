using Sonic.Application.Auth.DTOs;
using Sonic.Domain.Users;

namespace Sonic.Application.Auth.interfaces;

public interface IJwtTokenGenerator
{
    AuthToken GenerateToken(User user);
}

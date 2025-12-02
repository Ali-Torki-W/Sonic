using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sonic.Application.Auth;
using Sonic.Application.Auth.DTOs;
using Sonic.Application.Auth.interfaces;
using Sonic.Domain.Users;

namespace Sonic.Infrastructure.Auth;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly byte[] _secretKeyBytes;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(_options.Secret) || _options.Secret.Length < 32)
        {
            throw new InvalidOperationException("JWT Secret is not configured or too short (min 32 chars).");
        }

        _secretKeyBytes = Encoding.UTF8.GetBytes(_options.Secret);
    }

    public AuthToken GenerateToken(User user)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenMinutes <= 0 ? 60 : _options.AccessTokenMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(_secretKeyBytes),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.WriteToken(token);

        return new AuthToken(jwt, expires);
    }

    AuthToken IJwtTokenGenerator.GenerateToken(User user)
    {
        throw new NotImplementedException();
    }
}

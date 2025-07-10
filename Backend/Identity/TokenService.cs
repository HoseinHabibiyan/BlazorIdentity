using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Identity;

public class TokenService(IConfiguration config , UserManager<ApplicationUser> userManager)
{
    public async Task<string> GenerateAccessToken(ApplicationUser user)
    {
        var claims = await GetClaims(user);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:Secret"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["JWT:Issuer"],
            audience: config["JWT:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        TokenValidationParameters tokenValidationParameters = new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:Secret"]!)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false
        };

        JwtSecurityTokenHandler tokenHandler = new();
        ClaimsPrincipal principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);

        if (validatedToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }
        
        return principal;
    }

    private async Task<List<Claim>> GetClaims(ApplicationUser user)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameof(user));
        
        string name = user.UserName ?? string.Empty;
        
        if(user?.FirstName is not null)
            name = user.FirstName;
        
        if(user?.LastName is not null)
            name += $" {user.LastName}";
        
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name,  user.Email)
        ];
        
        IList<string> roles = await userManager.GetRolesAsync(user);
        claims.Add(new Claim(ClaimTypes.Role, string.Join(",",roles)));

        return claims;
    }
}
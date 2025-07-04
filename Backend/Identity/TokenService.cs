using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Identity;

public class TokenService(IConfiguration config)
{
    public string Generate(IdentityUser user)
    {
        var claims = new[] { new Claim(ClaimTypes.Name, user.Email!) };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:Issuer"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
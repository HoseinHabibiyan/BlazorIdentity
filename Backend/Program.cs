using System.Security.Claims;
using System.Text;
using Backend.Context;
using Backend.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("InMemoryDatabase"));

#region Identity Configurations

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(o =>
    {
        o.Password.RequireDigit = false;
        o.Password.RequireLowercase = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequiredLength = 3;
    })
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddAuthorization();
builder.Services.AddScoped<TokenService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidAudience = builder.Configuration["JWT:Audience"],
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]!))
    };
});

#endregion

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Identity API");
        options.WithTheme(ScalarTheme.BluePlanet);
    });
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    await DataSeedExtensions.IdentitySeed(userManager, roleManager);
}

#region APIs

app.MapPost("/api/account/register", async (AuthInput input, UserManager<ApplicationUser> userManager) =>
{
    ApplicationUser user = new()
    {
        FirstName = input.FirstName,
        LastName = input.LastName,
        UserName = input.Email,
        Email = input.Email,
        
    };
    IdentityResult result = await userManager.CreateAsync(user, input.Password);
    return result.Succeeded ? Results.Ok() : Results.BadRequest(result.Errors);
});

app.MapPost("/api/account/login",
    async (AuthInput auth, UserManager<ApplicationUser> userManager, TokenService tokenService) =>
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(auth.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, auth.Password))
            return Results.Unauthorized();
        
        string accessToken = await tokenService.GenerateAccessToken(user);
        string refreshToken = tokenService.GenerateRefreshToken();
        
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await userManager.UpdateAsync(user);
        
        return Results.Ok(new { accessToken , refreshToken });
    });

app.MapPost("/api/account/refreshToken",
    async (TokenModel input, UserManager<ApplicationUser> userManager, TokenService tokenService) =>
    {
        ClaimsPrincipal principal = tokenService.GetPrincipalFromExpiredToken(input.AccessToken);
        
        ApplicationUser? user = await userManager.FindByEmailAsync(principal.Identity?.Name!);

        if (user is null || user.RefreshToken != input.RefreshToken || user.RefreshTokenExpiryTime < DateTime.UtcNow)
        {
            return Results.Unauthorized();
        }
        
        string accessToken = await tokenService.GenerateAccessToken(user);
        string refreshToken = tokenService.GenerateRefreshToken();
        
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await userManager.UpdateAsync(user);
        
        return Results.Ok(new { accessToken , refreshToken });
    });

app.MapPost("/api/account/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok();
});

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/api/weatherforecast", [Authorize(Roles = "Admin")]() =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("WeatherForecast");

#endregion

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

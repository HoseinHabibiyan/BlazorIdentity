using System.Text;
using Backend.Context;
using Backend.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(o=> o.UseInMemoryDatabase("InMemoryDatabase"));

#region Identity Configurations

builder.Services.AddIdentity<IdentityUser, IdentityRole>(o =>
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Issuer"]!))
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
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    await DataSeedExtensions.IdentitySeed(userManager, roleManager);
}

#region APIs

app.MapPost("/api/account/register", async (AuthInput input, UserManager<IdentityUser> userManager) =>
{
    var user = new IdentityUser { UserName = input.Email, Email = input.Email };
    var result = await userManager.CreateAsync(user, input.Password);
    return result.Succeeded ? Results.Ok() : Results.BadRequest(result.Errors);
});

app.MapPost("/api/account/login", async (AuthInput dto, UserManager<IdentityUser> userManager, TokenService tokenService) =>
{
    var user = await userManager.FindByEmailAsync(dto.Email);
    if (user == null || !await userManager.CheckPasswordAsync(user, dto.Password))
        return Results.Unauthorized();

    var token = tokenService.Generate(user);
    return Results.Ok(new { token });
});

app.MapPost("/api/account/logout", async (SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok();
});

#endregion

app.Run();
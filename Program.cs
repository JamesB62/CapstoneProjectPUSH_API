using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using CapstoneProjectPUSH_API.Services;
using Microsoft.EntityFrameworkCore;
using CapstoneProjectPUSH_API.Data;
using System.Security.Claims;
using CapstoneProjectPUSH_API.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<PushDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("PushDb"));
});

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<EmailSender>();

string jwtKey = TokenService.GetKey(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // Reject tokens for accounts that have been deactivated or deleted
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                if (context.Principal == null)
                {
                    context.Fail("Invalid token.");
                    return;
                }

                string? idText = context.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
                int userId;

                if (!int.TryParse(idText, out userId))
                {
                    context.Fail("Invalid token.");
                    return;
                }

                PushDbContext db = context.HttpContext.RequestServices.GetRequiredService<PushDbContext>();
                User? user = await db.Users.FindAsync(userId);

                if (user == null || !user.IsActive)
                {
                    context.Fail("This account has been deactivated or no longer exists.");
                }
            }
        };
    });

var app = builder.Build();

// Seed the database
using (IServiceScope scope = app.Services.CreateScope())
{
    PushDbContext db = scope.ServiceProvider.GetRequiredService<PushDbContext>();
    await DbSeeder.SeedAsync(db, app.Configuration);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

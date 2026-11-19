using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using CapstoneProjectPUSH_API.Services;
using Microsoft.EntityFrameworkCore;
using CapstoneProjectPUSH_API.Data;

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

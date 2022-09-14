using System.Text;
using API.Data;
using API.Helpers;
using API.Interfaces;
using API.Middleware;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var tokenKey = builder.Configuration.GetValue<string>("TokenKey");
char[] tokenKeyArray = tokenKey.ToCharArray();

// Add services to the container.
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<Seed>();
builder.Services.AddAutoMapper(typeof(AutoMapperProfiles).Assembly);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DataContext>(options => 
{
    options.UseSqlite(connectionString);
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(option => {
    option.AddPolicy("cors", builder => {
        builder.AllowAnyMethod().AllowAnyHeader().WithOrigins("https://localhost:4200");
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters{
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKeyArray)),
            ValidateIssuer = false,
            ValidateAudience = false,
        };
    });

var app = builder.Build();

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
var seeder = services.GetRequiredService<Seed>();

try
{
    var context = services.GetRequiredService<DataContext>();
    await context.Database.MigrateAsync();
    await seeder.SeedUsers();
}
catch(Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred during migration");
}

app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("cors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

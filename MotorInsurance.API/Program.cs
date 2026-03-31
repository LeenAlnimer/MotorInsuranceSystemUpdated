using Microsoft.EntityFrameworkCore;
using MotorInsurance.API.Data;

// Repositories
using MotorInsurance.API.Repositories.Car;
using MotorInsurance.API.Repositories.Claim;
using MotorInsurance.API.Repositories.Client;
using MotorInsurance.API.Repositories.Policy;
using MotorInsurance.API.Repositories.Quote;
using MotorInsurance.API.Repositories.RefreshToken;
using MotorInsurance.API.Repositories.User;
// Services
using MotorInsurance.API.Services.Car;
using MotorInsurance.API.Services.Claim;
using MotorInsurance.API.Services.Client;
using MotorInsurance.API.Services.Policy;
using MotorInsurance.API.Services.Quote;
using MotorInsurance.API.Services.RefreshToken;
using MotorInsurance.API.Services.Users;
using MotorInsurance.API.Services.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Car

builder.Services.AddScoped<ICarRepository, CarRepository>();
builder.Services.AddScoped<ICarService, CarService>();

// Quote
builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();
builder.Services.AddScoped<IQuoteService, QuoteService>();

// Policy
builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
builder.Services.AddScoped<IPolicyService, PolicyService>();

// Client
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IClientService, ClientService>();

// Claim

builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
builder.Services.AddScoped<IClaimService, ClaimService>();

// User
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

// RefreshToken 
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
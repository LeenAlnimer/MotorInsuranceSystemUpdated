using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MotorInsurance.API.Data;
using MotorInsurance.API.Models;
using MotorInsurance.API.Services.Auth;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;

namespace MotorInsurance.Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = "TestDb_" + Guid.NewGuid();
        private readonly ConcurrentDictionary<string, string> _tokenCache = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Email:Host"]     = "localhost",
                    ["Email:Port"]     = "25",
                    ["Email:From"]     = "test@test.com",
                    ["Email:Password"] = "password"
                });
            });

            builder.ConfigureServices(services =>
            {
                var toRemove = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                                d.ServiceType == typeof(ApplicationDbContext) ||
                                (d.ServiceType.IsGenericType &&
                                 d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)) ||
                                (d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
                                 d.ImplementationType == typeof(MotorInsurance.API.Services.Background.PolicyExpirationService)))
                    .ToList();
                foreach (var d in toRemove)
                    services.Remove(d);

                services.AddScoped(_ =>
                {
                    var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
                        .UseInMemoryDatabase(_dbName)
                        .Options;
                    return new ApplicationDbContext(opts);
                });
            });
        }

        /// <summary>
        /// Seeds an Admin user directly into the in-memory DB and returns a valid JWT.
        /// The token is cached per suffix — login is only called once regardless of how
        /// many times InitializeAsync invokes this (avoids hitting the rate limiter).
        /// </summary>
        public async Task<string> SeedAdminTokenAsync(HttpClient client, string suffix, string phone)
        {
            if (_tokenCache.TryGetValue(suffix, out var cached))
                return cached;

            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var email = $"admin_{suffix}@test.com";
                if (!db.Users.Any(u => u.Email == email))
                {
                    db.Users.Add(new User
                    {
                        Username    = $"admin_{suffix}",
                        Email       = email,
                        PhoneNumber = phone,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                        Role        = "Admin",
                        DateCreated = DateTime.UtcNow
                    });
                    await db.SaveChangesAsync();
                }
            }

            var resp = await client.PostAsJsonAsync("/api/auth/login", new
            {
                EmailOrPhone = $"admin_{suffix}@test.com",
                Password   = "Admin123!"
            });

            var body  = await resp.Content.ReadFromJsonAsync<JsonElement>();
            var token = body.GetProperty("token").GetString()!;
            _tokenCache.TryAdd(suffix, token);
            return token;
        }

        /// <summary>
        /// Seeds a Client user directly into the in-memory DB and returns a valid JWT
        /// generated via JwtService — no HTTP login call, so the rate limiter is never hit.
        /// </summary>
        public async Task<(string Token, int UserId)> SeedClientAndGetTokenAsync(
            string username, string email, string phone)
        {
            int userId;
            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (!db.Users.Any(u => u.Email == email))
                {
                    var user = new User
                    {
                        Username    = username,
                        Email       = email,
                        PhoneNumber = phone,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
                        Role        = "Client",
                        DateCreated = DateTime.UtcNow
                    };
                    db.Users.Add(user);
                    await db.SaveChangesAsync();
                    userId = user.Id;
                }
                else
                {
                    userId = db.Users.First(u => u.Email == email).Id;
                }
            }

            using (var scope = Services.CreateScope())
            {
                var jwt = scope.ServiceProvider.GetRequiredService<JwtService>();
                return (jwt.GenerateToken(userId, username, "Client"), userId);
            }
        }
    }
}

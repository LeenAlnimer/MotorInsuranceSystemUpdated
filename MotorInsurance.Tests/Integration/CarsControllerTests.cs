using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace MotorInsurance.Tests.Integration
{
    public class CarsControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public CarsControllerTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<(string Token, int UserId)> RegisterAndLoginAsync(string userSuffix, string phone)
        {
            var registerResp = await _client.PostAsJsonAsync("/api/auth/register", new
            {
                FullName = "Test User",
                Username = $"car_user_{userSuffix}",
                Email = $"car_{userSuffix}@test.com",
                PhoneNumber = phone,
                Password = "Test123!",
                ConfirmPassword = "Test123!"
            });

            if (!registerResp.IsSuccessStatusCode)
            {
                var err = await registerResp.Content.ReadAsStringAsync();
                throw new Exception($"Register failed: {registerResp.StatusCode} — {err}");
            }

            var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                EmailOrPhone = $"car_{userSuffix}@test.com",
                Password = "Test123!"
            });

            if (!loginResp.IsSuccessStatusCode)
            {
                var err = await loginResp.Content.ReadAsStringAsync();
                throw new Exception($"Login failed: {loginResp.StatusCode} — {err}");
            }

            var body = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
            var token = body.GetProperty("token").GetString()!;

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var userIdStr = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var userId = userIdStr != null ? int.Parse(userIdStr) : 0;

            return (token, userId);
        }

        private static object CarPayload() => new
        {
            Brand = "Toyota",
            Model = "Camry",
            Year = 2022,
            Price = 20000,
            FuelType = 0
        };

        [Fact]
        public async Task GetCars_AccessingOtherUserCars_Returns403()
        {
            var (token, userId) = await RegisterAndLoginAsync("auth01", "0775555555");
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync($"/api/users/{userId + 9999}/cars");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetCars_WithValidToken_Returns200()
        {
            var (token, userId) = await RegisterAndLoginAsync("get01", "0771111111");
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync($"/api/users/{userId}/cars");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateCar_AsClient_Returns403()
        {
            var (token, userId) = await RegisterAndLoginAsync("create01", "0772222222");
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsJsonAsync($"/api/users/{userId}/cars", CarPayload());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetCar_NotFound_Returns404()
        {
            var (token, userId) = await RegisterAndLoginAsync("getbyid01", "0773333333");
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync($"/api/users/{userId}/cars/99999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteCar_AsClient_Returns403()
        {
            var (token, userId) = await RegisterAndLoginAsync("delete01", "0774444444");
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.DeleteAsync($"/api/users/{userId}/cars/1");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}

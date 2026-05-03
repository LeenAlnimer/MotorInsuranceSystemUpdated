using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MotorInsurance.Tests.Integration
{
    public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public AuthControllerTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        // كل test يستخدم بيانات فريدة (email + phone) لتجنّب التعارض في نفس الـ DB
        private static object MakeUser(string suffix, string phone) => new
        {
            FullName = $"Test User {suffix}",
            Username = $"user_{suffix}",
            Email = $"user_{suffix}@test.com",
            PhoneNumber = phone,
            Password = "Test123!",
            ConfirmPassword = "Test123!"
        };

        [Fact]
        public async Task Register_ValidData_Returns201()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/register", MakeUser("reg01", "0791111111"));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("user_reg01", body.GetProperty("username").GetString());
            Assert.Equal("user_reg01@test.com", body.GetProperty("email").GetString());
        }

        [Fact]
        public async Task Register_InvalidEmail_Returns400()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/register", new
            {
                FullName = "Test User",
                Username = "user_bademail",
                Email = "not-an-email",
                PhoneNumber = "0791234568",
                Password = "Test123!",
                ConfirmPassword = "Test123!"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_WeakPassword_Returns400()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/register", new
            {
                FullName = "Test User",
                Username = "user_weakpass",
                Email = "weakpass@test.com",
                PhoneNumber = "0791234569",
                Password = "weak",
                ConfirmPassword = "weak"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Login_ValidCredentials_Returns200WithToken()
        {
            await _client.PostAsJsonAsync("/api/auth/register", MakeUser("login01", "0792222222"));

            var response = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                EmailOrPhone = "user_login01@test.com",
                Password = "Test123!"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotEmpty(body.GetProperty("token").GetString()!);
            Assert.NotEmpty(body.GetProperty("refreshToken").GetString()!);
        }

        [Fact]
        public async Task Login_WrongPassword_Returns400()
        {
            await _client.PostAsJsonAsync("/api/auth/register", MakeUser("login02", "0793333333"));

            var response = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                EmailOrPhone = "user_login02@test.com",
                Password = "WrongPass999!"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Login_NonExistentUser_Returns400()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                EmailOrPhone = "nobody@nowhere.com",
                Password = "Test123!"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetMe_WithoutToken_Returns401()
        {
            var response = await _client.GetAsync("/api/users/me");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetMe_WithValidToken_Returns200()
        {
            await _client.PostAsJsonAsync("/api/auth/register", MakeUser("me01", "0794444444"));

            var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                EmailOrPhone = "user_me01@test.com",
                Password = "Test123!"
            });

            var loginBody = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
            var token = loginBody.GetProperty("token").GetString()!;

            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var meResp = await _client.GetAsync("/api/users/me");
            Assert.Equal(HttpStatusCode.OK, meResp.StatusCode);

            var me = await meResp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("user_me01", me.GetProperty("username").GetString());
        }
    }
}

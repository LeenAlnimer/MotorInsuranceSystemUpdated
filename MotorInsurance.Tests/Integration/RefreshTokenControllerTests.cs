using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MotorInsurance.Tests.Integration
{
    public class RefreshTokenControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public RefreshTokenControllerTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<(string Token, string RefreshToken)> RegisterAndLoginAsync(string suffix, string phone)
        {
            await _client.PostAsJsonAsync("/api/auth/register", new
            {
                FullName = "Token User",
                Username = $"rt_user_{suffix}",
                Email = $"rt_{suffix}@test.com",
                PhoneNumber = phone,
                Password = "Test123!",
                ConfirmPassword = "Test123!"
            });

            var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                EmailOrPhone = $"rt_{suffix}@test.com",
                Password = "Test123!"
            });

            var body = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
            return (
                body.GetProperty("token").GetString()!,
                body.GetProperty("refreshToken").GetString()!
            );
        }

        [Fact]
        public async Task Refresh_ValidToken_Returns200WithNewTokens()
        {
            var (_, refreshToken) = await RegisterAndLoginAsync("rf01", "0771000001");

            var resp = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotEmpty(body.GetProperty("token").GetString()!);
            Assert.NotEmpty(body.GetProperty("refreshToken").GetString()!);
        }

        [Fact]
        public async Task Refresh_InvalidToken_Returns400()
        {
            var resp = await _client.PostAsJsonAsync("/api/auth/refresh", new
            {
                refreshToken = "this-token-does-not-exist"
            });

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task Logout_ValidToken_Returns204()
        {
            var (_, refreshToken) = await RegisterAndLoginAsync("lo01", "0771000002");

            var resp = await _client.PostAsJsonAsync("/api/auth/logout", new { refreshToken });

            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        }

        [Fact]
        public async Task Refresh_AfterLogout_Returns400()
        {
            var (_, refreshToken) = await RegisterAndLoginAsync("ral01", "0771000003");

            await _client.PostAsJsonAsync("/api/auth/logout", new { refreshToken });

            var resp = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task Refresh_SameTokenTwice_SecondReturns400()
        {
            var (_, refreshToken) = await RegisterAndLoginAsync("st01", "0771000004");

            var first = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            var second = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });
            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        }
    }
}

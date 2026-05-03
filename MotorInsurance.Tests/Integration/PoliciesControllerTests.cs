using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MotorInsurance.Tests.Integration
{
    public class PoliciesControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;
        private string _adminToken = "";

        public PoliciesControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client  = factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            _adminToken = await _factory.SeedAdminTokenAsync(_client, "pol_adm", "0780300001");
        }

        public Task DisposeAsync() => Task.CompletedTask;

        // ── Helpers ───────────────────────────────────────────────────────────────

        private Task<(string Token, int UserId)> RegisterClientAsync(string suffix, string phone) =>
            _factory.SeedClientAndGetTokenAsync($"pol_{suffix}", $"pol_{suffix}@test.com", phone);

        private async Task<int> CreateCarAsync(int userId)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/users/{userId}/cars")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) },
                Content = JsonContent.Create(new
                {
                    Brand    = "Honda",
                    Model    = "Civic",
                    Year     = DateTime.UtcNow.Year - 1,
                    Price    = 18000,
                    FuelType = 0
                })
            };
            var resp = await _client.SendAsync(req);
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("id").GetInt32();
        }

        private async Task<int> GenerateQuoteAsync(string clientToken, int carId)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/quotes/generate")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) },
                Content = JsonContent.Create(new { CarId = carId })
            };
            var resp = await _client.SendAsync(req);
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("id").GetInt32();
        }

        private async Task ApproveQuoteAsync(int quoteId)
        {
            var req = new HttpRequestMessage(HttpMethod.Put, $"/api/quotes/{quoteId}/approve")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            await _client.SendAsync(req);
        }

        private async Task<int> GetPolicyIdAsync(string clientToken)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/policies")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) }
            };
            var resp = await _client.SendAsync(req);
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("data")[0].GetProperty("id").GetInt32();
        }

        private async Task<(string ClientToken, int UserId, int PolicyId)> SetupFlowAsync(string suffix, string phone)
        {
            var (clientToken, userId) = await RegisterClientAsync(suffix, phone);
            var carId    = await CreateCarAsync(userId);
            var quoteId  = await GenerateQuoteAsync(clientToken, carId);
            await ApproveQuoteAsync(quoteId);
            var policyId = await GetPolicyIdAsync(clientToken);
            return (clientToken, userId, policyId);
        }

        // ── Tests: Unauthorized ───────────────────────────────────────────────────

        [Fact]
        public async Task GetAll_WithoutToken_Returns401()
        {
            var resp = await _client.GetAsync("/api/policies");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact]
        public async Task GetById_WithoutToken_Returns401()
        {
            var resp = await _client.GetAsync("/api/policies/1");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        // ── Tests: GetAll ─────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAll_ClientSeesOnlyOwnPolicies_Returns200()
        {
            var (clientToken, _, _) = await SetupFlowAsync("p01", "0782000001");

            var req = new HttpRequestMessage(HttpMethod.Get, "/api/policies")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, body.GetProperty("totalCount").GetInt32());
        }

        [Fact]
        public async Task GetAll_AdminSeesAllPolicies_Returns200()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/policies")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        // ── Tests: GetById ────────────────────────────────────────────────────────

        [Fact]
        public async Task GetById_OwnPolicy_Returns200WithActiveStatus()
        {
            var (clientToken, _, policyId) = await SetupFlowAsync("p02", "0782000002");

            var req = new HttpRequestMessage(HttpMethod.Get, $"/api/policies/{policyId}")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Active", body.GetProperty("status").GetString());
        }

        [Fact]
        public async Task GetById_OtherUserPolicy_Returns403()
        {
            var (_, _, policyId)       = await SetupFlowAsync("p03a", "0782000003");
            var (otherToken, _, _) = await SetupFlowAsync("p03b", "0782000004");

            var req = new HttpRequestMessage(HttpMethod.Get, $"/api/policies/{policyId}")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", otherToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact]
        public async Task GetById_NotFound_Returns404()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/policies/99999")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        // ── Tests: Cancel ─────────────────────────────────────────────────────────

        [Fact]
        public async Task Cancel_AsClient_Returns403()
        {
            var (clientToken, _, policyId) = await SetupFlowAsync("p04", "0782000005");

            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/policies/{policyId}/cancel")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact]
        public async Task Cancel_ActivePolicy_Returns200()
        {
            var (_, _, policyId) = await SetupFlowAsync("p05", "0782000006");

            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/policies/{policyId}/cancel")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Cancelled", body.GetProperty("status").GetString());
        }

        [Fact]
        public async Task Cancel_AlreadyCancelled_Returns409()
        {
            var (_, _, policyId) = await SetupFlowAsync("p06", "0782000007");

            var cancel1 = new HttpRequestMessage(HttpMethod.Post, $"/api/policies/{policyId}/cancel")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            await _client.SendAsync(cancel1);

            var cancel2 = new HttpRequestMessage(HttpMethod.Post, $"/api/policies/{policyId}/cancel")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(cancel2);

            Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        }

        // ── Tests: Renew ──────────────────────────────────────────────────────────

        [Fact]
        public async Task Renew_AsClient_Returns403()
        {
            var (clientToken, _, policyId) = await SetupFlowAsync("p07", "0782000008");

            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/policies/{policyId}/renew")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact]
        public async Task Renew_ActivePolicy_Returns409()
        {
            var (_, _, policyId) = await SetupFlowAsync("p08", "0782000009");

            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/policies/{policyId}/renew")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        }

        [Fact]
        public async Task Renew_CancelledPolicy_Returns409()
        {
            var (_, _, policyId) = await SetupFlowAsync("p09", "0782000010");

            var cancel = new HttpRequestMessage(HttpMethod.Post, $"/api/policies/{policyId}/cancel")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            await _client.SendAsync(cancel);

            var renew = new HttpRequestMessage(HttpMethod.Post, $"/api/policies/{policyId}/renew")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(renew);

            Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        }

        [Fact]
        public async Task Renew_NotFound_Returns404()
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/policies/99999/renew")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }
    }
}

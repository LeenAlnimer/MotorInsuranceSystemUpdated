using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MotorInsurance.Tests.Integration
{
    public class ClaimsControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;
        private string _adminToken = "";

        public ClaimsControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client  = factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            _adminToken = await _factory.SeedAdminTokenAsync(_client, "clm_adm", "0780200001");
        }

        public Task DisposeAsync() => Task.CompletedTask;

        // ── Helpers ───────────────────────────────────────────────────────────────

        private Task<(string Token, int UserId)> RegisterClientAsync(string suffix, string phone) =>
            _factory.SeedClientAndGetTokenAsync($"clm_{suffix}", $"clm_{suffix}@test.com", phone);

        private async Task<int> CreateCarAsync(int userId)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/users/{userId}/cars")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) },
                Content = JsonContent.Create(new
                {
                    Brand    = "Toyota",
                    Model    = "Camry",
                    Year     = DateTime.UtcNow.Year - 2,
                    Price    = 20000,
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

        private async Task<int> CreateClaimAsync(string clientToken, int policyId)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/claims")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) },
                Content = JsonContent.Create(new
                {
                    PolicyId    = policyId,
                    Description = "Accident damage",
                    ClaimAmount = 500
                })
            };
            var resp = await _client.SendAsync(req);
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("id").GetInt32();
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
            var resp = await _client.GetAsync("/api/claims");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact]
        public async Task Create_WithoutToken_Returns401()
        {
            var resp = await _client.PostAsJsonAsync("/api/claims",
                new { PolicyId = 1, Description = "test" });
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        // ── Tests: Create ─────────────────────────────────────────────────────────

        [Fact]
        public async Task Create_ValidClaim_Returns201()
        {
            var (clientToken, _, policyId) = await SetupFlowAsync("c01", "0781000001");

            var req = new HttpRequestMessage(HttpMethod.Post, "/api/claims")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) },
                Content = JsonContent.Create(new { PolicyId = policyId, Description = "Windshield crack", ClaimAmount = 500 })
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(policyId, body.GetProperty("policyId").GetInt32());
        }

        [Fact]
        public async Task Create_InvalidPolicyId_Returns400()
        {
            var (clientToken, _, _) = await SetupFlowAsync("c02", "0781000002");

            var req = new HttpRequestMessage(HttpMethod.Post, "/api/claims")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) },
                Content = JsonContent.Create(new { PolicyId = 99999, Description = "test" })
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task Create_PolicyOfAnotherUser_Returns400()
        {
            var (_, _, policyId)         = await SetupFlowAsync("c03a", "0781000003");
            var (otherToken, _, _) = await SetupFlowAsync("c03b", "0781000004");

            var req = new HttpRequestMessage(HttpMethod.Post, "/api/claims")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", otherToken) },
                Content = JsonContent.Create(new { PolicyId = policyId, Description = "not mine" })
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        // ── Tests: GetAll ─────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAll_ClientSeesOnlyOwnClaims_Returns200()
        {
            var (clientToken, _, policyId) = await SetupFlowAsync("c04", "0781000005");
            await CreateClaimAsync(clientToken, policyId);

            var req = new HttpRequestMessage(HttpMethod.Get, "/api/claims")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("totalCount").GetInt32() >= 1);
        }

        [Fact]
        public async Task GetAll_AdminSeesAllClaims_Returns200()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/claims")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        // ── Tests: GetById ────────────────────────────────────────────────────────

        [Fact]
        public async Task GetById_OwnClaim_Returns200()
        {
            var (clientToken, _, policyId) = await SetupFlowAsync("c05", "0781000006");
            var claimId = await CreateClaimAsync(clientToken, policyId);

            var req = new HttpRequestMessage(HttpMethod.Get, $"/api/claims/{claimId}")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        [Fact]
        public async Task GetById_OtherUserClaim_Returns403()
        {
            var (clientToken, _, policyId) = await SetupFlowAsync("c06a", "0781000007");
            var claimId = await CreateClaimAsync(clientToken, policyId);

            var (otherToken, _, _) = await SetupFlowAsync("c06b", "0781000008");

            var req = new HttpRequestMessage(HttpMethod.Get, $"/api/claims/{claimId}")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", otherToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact]
        public async Task GetById_NotFound_Returns404()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/claims/99999")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        // ── Tests: Approve / Reject ───────────────────────────────────────────────

        [Fact]
        public async Task Approve_AsClient_Returns403()
        {
            var (clientToken, _, policyId) = await SetupFlowAsync("c07", "0781000009");
            var claimId = await CreateClaimAsync(clientToken, policyId);

            var req = new HttpRequestMessage(HttpMethod.Put, $"/api/claims/{claimId}/approve")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact]
        public async Task Approve_AsAdmin_Returns200()
        {
            var (clientToken, _, policyId) = await SetupFlowAsync("c08", "0781000010");
            var claimId = await CreateClaimAsync(clientToken, policyId);

            var req = new HttpRequestMessage(HttpMethod.Put, $"/api/claims/{claimId}/approve")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        [Fact]
        public async Task Approve_AlreadyApproved_Returns409()
        {
            var (clientToken, _, policyId) = await SetupFlowAsync("c09", "0781000011");
            var claimId = await CreateClaimAsync(clientToken, policyId);

            var approve = new HttpRequestMessage(HttpMethod.Put, $"/api/claims/{claimId}/approve")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            await _client.SendAsync(approve);

            var again = new HttpRequestMessage(HttpMethod.Put, $"/api/claims/{claimId}/approve")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(again);

            Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        }

        [Fact]
        public async Task Reject_AsAdmin_Returns200()
        {
            var (clientToken, _, policyId) = await SetupFlowAsync("c10", "0781000012");
            var claimId = await CreateClaimAsync(clientToken, policyId);

            var req = new HttpRequestMessage(HttpMethod.Put, $"/api/claims/{claimId}/reject")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        [Fact]
        public async Task Reject_AsClient_Returns403()
        {
            var (clientToken, _, policyId) = await SetupFlowAsync("c11", "0781000013");
            var claimId = await CreateClaimAsync(clientToken, policyId);

            var req = new HttpRequestMessage(HttpMethod.Put, $"/api/claims/{claimId}/reject")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        // ── Tests: Delete ─────────────────────────────────────────────────────────

        [Fact]
        public async Task Delete_AsClient_Returns403()
        {
            var (clientToken, _, policyId) = await SetupFlowAsync("c12", "0781000014");
            var claimId = await CreateClaimAsync(clientToken, policyId);

            var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/claims/{claimId}")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact]
        public async Task Delete_AsAdmin_Returns204()
        {
            var (clientToken, _, policyId) = await SetupFlowAsync("c13", "0781000015");
            var claimId = await CreateClaimAsync(clientToken, policyId);

            // Claims can only be deleted after rejection
            var rejectReq = new HttpRequestMessage(HttpMethod.Put, $"/api/claims/{claimId}/reject")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            await _client.SendAsync(rejectReq);

            var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/claims/{claimId}")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        }

        [Fact]
        public async Task Delete_NotFound_Returns404()
        {
            var req = new HttpRequestMessage(HttpMethod.Delete, "/api/claims/99999")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }
    }
}

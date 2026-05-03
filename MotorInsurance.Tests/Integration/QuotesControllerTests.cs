using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MotorInsurance.Tests.Integration
{
    public class QuotesControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;
        private string _adminToken = "";

        public QuotesControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client  = factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            _adminToken = await _factory.SeedAdminTokenAsync(_client, "qut_adm", "0780400001");
        }

        public Task DisposeAsync() => Task.CompletedTask;

        // ── Helpers ───────────────────────────────────────────────────────────────

        private Task<(string Token, int UserId)> RegisterClientAsync(string suffix, string phone) =>
            _factory.SeedClientAndGetTokenAsync($"qut_{suffix}", $"qut_{suffix}@test.com", phone);

        private async Task<int> CreateCarAsync(int userId)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/users/{userId}/cars")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) },
                Content = JsonContent.Create(new
                {
                    Brand    = "Toyota",
                    Model    = "Corolla",
                    Year     = DateTime.UtcNow.Year - 1,
                    Price    = 15000,
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

        private async Task<(string ClientToken, int UserId, int CarId, int QuoteId)> SetupWithQuoteAsync(
            string suffix, string phone)
        {
            var (clientToken, userId) = await RegisterClientAsync(suffix, phone);
            var carId   = await CreateCarAsync(userId);
            var quoteId = await GenerateQuoteAsync(clientToken, carId);
            return (clientToken, userId, carId, quoteId);
        }

        // ── Tests: Unauthorized ───────────────────────────────────────────────────

        [Fact]
        public async Task GetAll_WithoutToken_Returns401()
        {
            var resp = await _client.GetAsync("/api/quotes");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact]
        public async Task GetById_WithoutToken_Returns401()
        {
            var resp = await _client.GetAsync("/api/quotes/1");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact]
        public async Task Generate_WithoutToken_Returns401()
        {
            var resp = await _client.PostAsJsonAsync("/api/quotes/generate", new { CarId = 1 });
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        // ── Tests: Generate ───────────────────────────────────────────────────────

        [Fact]
        public async Task Generate_ValidCar_Returns201()
        {
            var (clientToken, userId) = await RegisterClientAsync("q01", "0789400001");
            var carId = await CreateCarAsync(userId);

            var req = new HttpRequestMessage(HttpMethod.Post, "/api/quotes/generate")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) },
                Content = JsonContent.Create(new { CarId = carId })
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(carId, body.GetProperty("carId").GetInt32());
        }

        [Fact]
        public async Task Generate_InvalidCarId_Returns400()
        {
            var (clientToken, _) = await RegisterClientAsync("q02", "0789400002");

            var req = new HttpRequestMessage(HttpMethod.Post, "/api/quotes/generate")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) },
                Content = JsonContent.Create(new { CarId = 99999 })
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task Generate_CarOfAnotherUser_Returns403()
        {
            var (_, userAId)      = await RegisterClientAsync("q03a", "0789400003");
            var (otherToken, _)   = await RegisterClientAsync("q03b", "0789400004");
            var carId = await CreateCarAsync(userAId);

            var req = new HttpRequestMessage(HttpMethod.Post, "/api/quotes/generate")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", otherToken) },
                Content = JsonContent.Create(new { CarId = carId })
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        // ── Tests: GetAll ─────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAll_ClientSeesOnlyOwnQuotes_Returns200()
        {
            var (clientToken, userId) = await RegisterClientAsync("q04", "0789400005");
            var carId = await CreateCarAsync(userId);
            await GenerateQuoteAsync(clientToken, carId);

            var req = new HttpRequestMessage(HttpMethod.Get, "/api/quotes")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("totalCount").GetInt32() >= 1);
        }

        [Fact]
        public async Task GetAll_AdminSeesAllQuotes_Returns200()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/quotes")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        // ── Tests: GetById ────────────────────────────────────────────────────────

        [Fact]
        public async Task GetById_OwnQuote_Returns200()
        {
            var (clientToken, _, _, quoteId) = await SetupWithQuoteAsync("q05", "0789400006");

            var req = new HttpRequestMessage(HttpMethod.Get, $"/api/quotes/{quoteId}")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        [Fact]
        public async Task GetById_OtherUserQuote_Returns403()
        {
            var (_, _, _, quoteId) = await SetupWithQuoteAsync("q06a", "0789400007");
            var (otherToken, _)    = await RegisterClientAsync("q06b", "0789400008");

            var req = new HttpRequestMessage(HttpMethod.Get, $"/api/quotes/{quoteId}")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", otherToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact]
        public async Task GetById_NotFound_Returns404()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/quotes/99999")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        // ── Tests: Approve ────────────────────────────────────────────────────────

        [Fact]
        public async Task Approve_AsClient_Returns403()
        {
            var (clientToken, _, _, quoteId) = await SetupWithQuoteAsync("q07", "0789400009");

            var req = new HttpRequestMessage(HttpMethod.Put, $"/api/quotes/{quoteId}/approve")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact]
        public async Task Approve_AsAdmin_Returns200()
        {
            var (_, _, _, quoteId) = await SetupWithQuoteAsync("q08", "0789400010");

            var req = new HttpRequestMessage(HttpMethod.Put, $"/api/quotes/{quoteId}/approve")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        [Fact]
        public async Task Approve_NotFound_Returns404()
        {
            var req = new HttpRequestMessage(HttpMethod.Put, "/api/quotes/99999/approve")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task Approve_AlreadyApproved_Returns400()
        {
            var (_, _, _, quoteId) = await SetupWithQuoteAsync("q09", "0789400011");

            var approve1 = new HttpRequestMessage(HttpMethod.Put, $"/api/quotes/{quoteId}/approve")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            await _client.SendAsync(approve1);

            var approve2 = new HttpRequestMessage(HttpMethod.Put, $"/api/quotes/{quoteId}/approve")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(approve2);

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task Approve_RejectedQuote_Returns400()
        {
            var (_, _, _, quoteId) = await SetupWithQuoteAsync("q10", "0789400012");

            var reject = new HttpRequestMessage(HttpMethod.Put, $"/api/quotes/{quoteId}/reject")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            await _client.SendAsync(reject);

            var approve = new HttpRequestMessage(HttpMethod.Put, $"/api/quotes/{quoteId}/approve")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(approve);

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        // ── Tests: Reject ─────────────────────────────────────────────────────────

        [Fact]
        public async Task Reject_AsClient_Returns403()
        {
            var (clientToken, _, _, quoteId) = await SetupWithQuoteAsync("q11", "0789400013");

            var req = new HttpRequestMessage(HttpMethod.Put, $"/api/quotes/{quoteId}/reject")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact]
        public async Task Reject_AsAdmin_Returns200()
        {
            var (_, _, _, quoteId) = await SetupWithQuoteAsync("q12", "0789400014");

            var req = new HttpRequestMessage(HttpMethod.Put, $"/api/quotes/{quoteId}/reject")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        [Fact]
        public async Task Reject_NotFound_Returns404()
        {
            var req = new HttpRequestMessage(HttpMethod.Put, "/api/quotes/99999/reject")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task Reject_AlreadyRejected_Returns400()
        {
            var (_, _, _, quoteId) = await SetupWithQuoteAsync("q13", "0789400015");

            var reject1 = new HttpRequestMessage(HttpMethod.Put, $"/api/quotes/{quoteId}/reject")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            await _client.SendAsync(reject1);

            var reject2 = new HttpRequestMessage(HttpMethod.Put, $"/api/quotes/{quoteId}/reject")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(reject2);

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        // ── Tests: Delete ─────────────────────────────────────────────────────────

        [Fact]
        public async Task Delete_AsClient_Returns403()
        {
            var (clientToken, _, _, quoteId) = await SetupWithQuoteAsync("q14", "0789400016");

            var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/quotes/{quoteId}")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", clientToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact]
        public async Task Delete_AsAdmin_Returns204()
        {
            var (_, _, _, quoteId) = await SetupWithQuoteAsync("q15", "0789400017");

            var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/quotes/{quoteId}")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        }

        [Fact]
        public async Task Delete_NotFound_Returns404()
        {
            var req = new HttpRequestMessage(HttpMethod.Delete, "/api/quotes/99999")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task Delete_QuoteWithPolicy_Returns409()
        {
            var (_, _, _, quoteId) = await SetupWithQuoteAsync("q16", "0789400018");

            var approve = new HttpRequestMessage(HttpMethod.Put, $"/api/quotes/{quoteId}/approve")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            await _client.SendAsync(approve);

            var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/quotes/{quoteId}")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(delete);

            Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        }
    }
}

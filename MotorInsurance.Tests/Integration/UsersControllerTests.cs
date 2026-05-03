using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MotorInsurance.Tests.Integration
{
    public class UsersControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;
        private string _adminToken  = "";
        private string _clientToken = "";

        public UsersControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client  = factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            _adminToken = await _factory.SeedAdminTokenAsync(_client, "usr_adm", "0780500001");
            (_clientToken, _) = await _factory.SeedClientAndGetTokenAsync(
                "usr_cli", "usr_cli@test.com", "0789500001");
        }

        public Task DisposeAsync() => Task.CompletedTask;

        // ── Helper ────────────────────────────────────────────────────────────────

        private static object NewUserPayload(string suffix, string phone, string role = "Employee") => new
        {
            Username    = $"usr_{suffix}",
            Email       = $"usr_{suffix}@test.com",
            PhoneNumber = phone,
            Password    = "Admin123!",
            Role        = role
        };

        // ── Tests: GetAll ─────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAll_WithoutToken_Returns401()
        {
            var resp = await _client.GetAsync("/api/users");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact]
        public async Task GetAll_AsClient_Returns403()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/users")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _clientToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact]
        public async Task GetAll_AsAdmin_Returns200()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/users")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        // ── Tests: CreateUser ─────────────────────────────────────────────────────

        [Fact]
        public async Task CreateUser_WithoutToken_Returns401()
        {
            var resp = await _client.PostAsJsonAsync("/api/users", NewUserPayload("u01", "0789500010"));
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact]
        public async Task CreateUser_AsClient_Returns403()
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/users")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _clientToken) },
                Content = JsonContent.Create(NewUserPayload("u02", "0789500011"))
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact]
        public async Task CreateUser_AsAdmin_Returns201WithCorrectData()
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/users")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) },
                Content = JsonContent.Create(NewUserPayload("u03", "0789500012"))
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("usr_u03", body.GetProperty("username").GetString());
            Assert.Equal("Employee",  body.GetProperty("role").GetString());
        }

        [Fact]
        public async Task CreateUser_DuplicateEmail_Returns400()
        {
            var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/users")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) },
                Content = JsonContent.Create(NewUserPayload("u04", "0789500013"))
            };
            await _client.SendAsync(req1);

            var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/users")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) },
                Content = JsonContent.Create(new
                {
                    Username    = "usr_u04dup",
                    Email       = "usr_u04@test.com",   // duplicate
                    PhoneNumber = "0789500014",
                    Password    = "Admin123!",
                    Role        = "Employee"
                })
            };
            var resp = await _client.SendAsync(req2);

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        // ── Tests: UpdateRole ─────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateRole_WithoutToken_Returns401()
        {
            var resp = await _client.PutAsJsonAsync("/api/users/1/role", new { Role = "Employee" });
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact]
        public async Task UpdateRole_AsClient_Returns403()
        {
            var req = new HttpRequestMessage(HttpMethod.Put, "/api/users/1/role")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _clientToken) },
                Content = JsonContent.Create(new { Role = "Employee" })
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact]
        public async Task UpdateRole_AsAdmin_Returns200()
        {
            var create = new HttpRequestMessage(HttpMethod.Post, "/api/users")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) },
                Content = JsonContent.Create(NewUserPayload("u05", "0789500015"))
            };
            var created = await (await _client.SendAsync(create))
                .Content.ReadFromJsonAsync<JsonElement>();
            var userId = created.GetProperty("id").GetInt32();

            var req = new HttpRequestMessage(HttpMethod.Put, $"/api/users/{userId}/role")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) },
                Content = JsonContent.Create(new { Role = "Admin" })
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        [Fact]
        public async Task UpdateRole_NotFound_Returns404()
        {
            var req = new HttpRequestMessage(HttpMethod.Put, "/api/users/99999/role")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) },
                Content = JsonContent.Create(new { Role = "Employee" })
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        // ── Tests: Delete ─────────────────────────────────────────────────────────

        [Fact]
        public async Task Delete_WithoutToken_Returns401()
        {
            var resp = await _client.DeleteAsync("/api/users/1");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact]
        public async Task Delete_AsClient_Returns403()
        {
            var req = new HttpRequestMessage(HttpMethod.Delete, "/api/users/1")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _clientToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact]
        public async Task Delete_AsAdmin_Returns204()
        {
            var create = new HttpRequestMessage(HttpMethod.Post, "/api/users")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) },
                Content = JsonContent.Create(NewUserPayload("u06", "0789500016"))
            };
            var created = await (await _client.SendAsync(create))
                .Content.ReadFromJsonAsync<JsonElement>();
            var userId = created.GetProperty("id").GetInt32();

            var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/users/{userId}")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        }

        [Fact]
        public async Task Delete_NotFound_Returns404()
        {
            var req = new HttpRequestMessage(HttpMethod.Delete, "/api/users/99999")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        // ── Tests: GetStatus ──────────────────────────────────────────────────────

        [Fact]
        public async Task GetStatus_WithoutToken_Returns401()
        {
            var resp = await _client.GetAsync("/api/users/status");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact]
        public async Task GetStatus_AsClient_Returns403()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/users/status")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _clientToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact]
        public async Task GetStatus_AsAdmin_Returns200WithExpectedShape()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/users/status")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _adminToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.TryGetProperty("policies", out _));
            Assert.True(body.TryGetProperty("claims",   out _));
            Assert.True(body.TryGetProperty("users",    out _));
            Assert.True(body.TryGetProperty("quotes",   out _));
        }

        // ── Tests: GetMe ──────────────────────────────────────────────────────────

        [Fact]
        public async Task GetMe_WithoutToken_Returns401()
        {
            var resp = await _client.GetAsync("/api/users/me");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact]
        public async Task GetMe_Returns200WithOwnData()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/users/me")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _clientToken) }
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("usr_cli", body.GetProperty("username").GetString());
        }

        // ── Tests: UpdateMe ───────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateMe_WithoutToken_Returns401()
        {
            var resp = await _client.PutAsJsonAsync("/api/users/me", new { PhoneNumber = "0789599999" });
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact]
        public async Task UpdateMe_Returns200()
        {
            var req = new HttpRequestMessage(HttpMethod.Put, "/api/users/me")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _clientToken) },
                Content = JsonContent.Create(new { PhoneNumber = "0789500099" })
            };
            var resp = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }
    }
}

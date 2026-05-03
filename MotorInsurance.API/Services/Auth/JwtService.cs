using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SecurityClaim = System.Security.Claims.Claim;
using SecurityClaimTypes = System.Security.Claims.ClaimTypes;

namespace MotorInsurance.API.Services.Auth
{
    public class JwtService
    {
        private readonly string _key;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiryHours;

        public JwtService(IConfiguration configuration)
        {
            _key = configuration["Jwt:Key"]!;
            _issuer = configuration["Jwt:Issuer"]!;
            _audience = configuration["Jwt:Audience"]!;
            _expiryHours = int.TryParse(configuration["Jwt:ExpiryHours"], out var h) ? h : 2;
        }

        public string GenerateToken(int userId, string username, string role)
        {
            var claims = new List<SecurityClaim>
            {
                new SecurityClaim(SecurityClaimTypes.NameIdentifier, userId.ToString()),
                new SecurityClaim(SecurityClaimTypes.Name, username),
                new SecurityClaim(SecurityClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_expiryHours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using JwtClaim = System.Security.Claims.Claim;

namespace MotorInsurance.API.Services.Auth
{
    public class JwtService
    {
        private readonly string _key;

        public JwtService(IConfiguration configuration)
        {
            _key = configuration["Jwt:Key"];
        }

        public string GenerateToken(string username, string role)
        {
            var claims = new List<JwtClaim>
            {
                new JwtClaim(ClaimTypes.Name, username),
                new JwtClaim(ClaimTypes.Role, role) 
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
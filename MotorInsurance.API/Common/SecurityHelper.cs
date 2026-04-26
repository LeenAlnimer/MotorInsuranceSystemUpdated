namespace MotorInsurance.API.Common
{
    public static class SecurityHelper
    {
        public static string GenerateSecureToken()
        {
            var bytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}

using System.IdentityModel.Tokens.Jwt;

namespace Hellang.Authentication.JwtBearer.Google
{
    public static class GoogleClaimTypes
    {
        public static string Name { get; } = "name";

        public static string Domain { get; } = "hd";

        public static string Locale { get; } = "locale";

        public static string Picture { get; } = "picture";

        public static string EmailVerified { get; } = "email_verified";

        public static string Email { get; } = JwtRegisteredClaimNames.Email;

        public static string GivenName { get; } = JwtRegisteredClaimNames.GivenName;
        
        public static string FamilyName { get; } = JwtRegisteredClaimNames.FamilyName;
    }
}

using System.IdentityModel.Tokens.Jwt;

namespace Hellang.Authentication.JwtBearer.Google
{
    public static class GoogleClaimTypes
    {
        public static string Domain { get; } = "hd";

        public static string Name { get; } = "name";

        public static string Locale { get; } = "locale";

        public static string Picture { get; } = "picture";

        public static string Profile { get; } = "profile";

        public static string EmailVerified { get; } = "email_verified";
        
        public static string Iss { get; } = JwtRegisteredClaimNames.Iss;

        public static string Sub { get; } = JwtRegisteredClaimNames.Sub;

        public static string Azp { get; } = JwtRegisteredClaimNames.Azp;

        public static string Aud { get; } = JwtRegisteredClaimNames.Aud;

        public static string Iat { get; } = JwtRegisteredClaimNames.Iat;

        public static string Exp { get; } = JwtRegisteredClaimNames.Exp;
        
        public static string Email { get; } = JwtRegisteredClaimNames.Email;

        public static string GivenName { get; } = JwtRegisteredClaimNames.GivenName;
        
        public static string FamilyName { get; } = JwtRegisteredClaimNames.FamilyName;
    }
}

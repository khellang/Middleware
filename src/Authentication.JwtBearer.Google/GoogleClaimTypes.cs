using System.IdentityModel.Tokens.Jwt;

namespace Hellang.Authentication.JwtBearer.Google
{
    public static class GoogleClaimTypes
    {
        /// <summary>
        /// The hosted G Suite domain of the user.
        /// </summary>
        public static string Domain { get; } = "hd";

        /// <summary>
        /// The user's full name, in a displayable form.
        /// </summary>
        public static string Name { get; } = "name";

        /// <summary>
        /// The user's preferred locale.
        /// </summary>
        public static string Locale { get; } = "locale";

        /// <summary>
        /// The URL of the user's profile picture.
        /// </summary>
        public static string Picture { get; } = "picture";

        /// <summary>
        /// The URL of the user's profile page.
        /// </summary>
        public static string Profile { get; } = "profile";

        /// <summary>
        /// True if the user's e-mail address has been verified; otherwise false.
        /// </summary>
        public static string EmailVerified { get; } = "email_verified";

        /// <summary>
        /// The Issuer Identifier for the Issuer of the response.
        /// </summary>
        public static string Iss { get; } = JwtRegisteredClaimNames.Iss;

        /// <summary>
        /// An identifier for the user, unique among all Google accounts and never reused.
        /// </summary>
        public static string Sub { get; } = JwtRegisteredClaimNames.Sub;

        /// <summary>
        /// The client_id of the authorized presenter.
        /// </summary>
        public static string Azp { get; } = JwtRegisteredClaimNames.Azp;

        /// <summary>
        /// Identifies the audience that this ID token is intended for.
        /// </summary>
        public static string Aud { get; } = JwtRegisteredClaimNames.Aud;

        /// <summary>
        /// The time the ID token was issued, represented in Unix time (integer seconds).
        /// </summary>
        public static string Iat { get; } = JwtRegisteredClaimNames.Iat;

        /// <summary>
        /// The time the ID token expires, represented in Unix time (integer seconds).
        /// </summary>
        public static string Exp { get; } = JwtRegisteredClaimNames.Exp;

        /// <summary>
        /// The user's email address.
        /// </summary>
        public static string Email { get; } = JwtRegisteredClaimNames.Email;

        /// <summary>
        /// The user's given name.
        /// </summary>
        public static string GivenName { get; } = JwtRegisteredClaimNames.GivenName;

        /// <summary>
        /// The user's family name.
        /// </summary>
        public static string FamilyName { get; } = JwtRegisteredClaimNames.FamilyName;
    }
}

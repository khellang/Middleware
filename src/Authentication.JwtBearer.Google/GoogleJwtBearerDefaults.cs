using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Hellang.Authentication.JwtBearer.Google
{
    /// <summary>
    /// Default values used by Google bearer authentication.
    /// </summary>
    public static class GoogleJwtBearerDefaults
    {
        /// <summary>
        /// The default authority for Google authentication.
        /// </summary>
        public static readonly string Authority = "https://accounts.google.com";

        /// <summary>
        /// The default authentication scheme for Google authentication.
        /// </summary>
        public static readonly string AuthenticationScheme = "Google." + JwtBearerDefaults.AuthenticationScheme;
    }
}

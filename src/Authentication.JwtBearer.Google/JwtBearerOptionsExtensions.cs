using System;
using Hellang.Authentication.JwtBearer.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class JwtBearerOptionsExtensions
    {        
        /// <summary>
        /// Configures the JWT Bearer authentication handler to validate Google OpenID Connect tokens
        /// for a specified <paramref name="clientId"/>.
        /// </summary>
        /// <param name="options">The options to configure.</param>
        /// <param name="clientId">The client ID string that you obtain from the API Console.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="clientId"/> argument is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">If the <paramref name="clientId"/> argument is empty.</exception>
        public static JwtBearerOptions UseGoogle(this JwtBearerOptions options, string clientId)
        {
            return options.UseGoogle(clientId, null);
        }

        /// <summary>
        /// Configures the JWT Bearer authentication handler to validate Google OpenID Connect tokens
        /// for a specified <paramref name="clientId"/> and <paramref name="hostedDomain"/>.
        /// </summary>
        /// <param name="options">The options to configure.</param>
        /// <param name="clientId">The client ID string that you obtain from the API Console.</param>
        /// <param name="hostedDomain">The hd (hosted domain) parameter streamlines the login process for G Suite hosted accounts.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="clientId"/> argument is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">If the <paramref name="clientId"/> argument is empty.</exception>
        public static JwtBearerOptions UseGoogle(this JwtBearerOptions options, string clientId, string hostedDomain)
        {
            if (clientId == null)
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            if (clientId.Length == 0)
            {
                throw new ArgumentException("ClientId cannot be empty.", nameof(clientId));
            }

            options.Audience = clientId;
            options.Authority = "https://accounts.google.com";
            
            options.SecurityTokenValidators.Clear();
            options.SecurityTokenValidators.Add(new GoogleJwtSecurityTokenHandler());
            
            options.TokenValidationParameters = new GoogleTokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                NameClaimType = GoogleClaimTypes.Name,
                AuthenticationType = "Google." + JwtBearerDefaults.AuthenticationScheme,

                HostedDomain = hostedDomain,
                ValidIssuers = new[] { options.Authority, "accounts.google.com" },
            };

            return options;
        }
    }
}

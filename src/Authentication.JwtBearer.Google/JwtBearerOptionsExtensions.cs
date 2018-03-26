using System;
using Hellang.Authentication.JwtBearer.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class JwtBearerOptionsExtensions
    {
        public static JwtBearerOptions UseGoogle(this JwtBearerOptions options, string clientId)
        {
            return options.UseGoogle(clientId, null);
        }

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
                AuthenticationType = JwtBearerDefaults.AuthenticationScheme,

                HostedDomain = hostedDomain,
                ValidIssuers = new[] { options.Authority, "accounts.google.com" },
            };

            return options;
        }
    }
}

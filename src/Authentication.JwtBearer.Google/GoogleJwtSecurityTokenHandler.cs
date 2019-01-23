using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Hellang.Authentication.JwtBearer.Google
{
    public class GoogleJwtSecurityTokenHandler : JwtSecurityTokenHandler
    {
        public GoogleJwtSecurityTokenHandler()
        {
            InboundClaimTypeMap.Clear();
        }
        
        /// <inheritdoc />
        /// <exception cref="SecurityTokenInvalidDomainException">token 'hd' claim did not match HostedDomain.</exception>
        public override ClaimsPrincipal ValidateToken(string token, TokenValidationParameters validationParameters, out SecurityToken validatedToken)
        {
            // The base class should already take care of validating signature, issuer,
            // audience and expiry. We just need to handle the hosted domain validation below.
            var principal = base.ValidateToken(token, validationParameters, out validatedToken);

            if (validationParameters is GoogleTokenValidationParameters googleParameters)
            {
                var expected = googleParameters.HostedDomain;

                // No domain specified. Skip validation.
                if (string.IsNullOrEmpty(expected))
                {
                    return principal;
                }

                var actual = principal.FindFirst(GoogleClaimTypes.Domain)?.Value;

                ValidateHostedDomain(actual, expected);
            }

            return principal;
        }

        private static void ValidateHostedDomain(string actual, string expected)
        {
            if (string.IsNullOrEmpty(actual))
            {
                throw LogHelper.LogExceptionMessage(new SecurityTokenInvalidDomainException(LogMessages.IDX10250) { InvalidDomain = null });
            }

            if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            {
                var message = string.Format(LogMessages.IDX10251, actual, expected);

                throw LogHelper.LogExceptionMessage(new SecurityTokenInvalidDomainException(message) { InvalidDomain = actual });
            }

            LogHelper.LogInformation(LogMessages.IDX10252, actual);
        }
    }
}

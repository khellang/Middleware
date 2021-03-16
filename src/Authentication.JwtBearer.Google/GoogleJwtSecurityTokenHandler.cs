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
                var domain = googleParameters.HostedDomain;

                // No domain specified. Skip validation.
                if (string.IsNullOrEmpty(domain))
                {
                    return principal;
                }

                if (googleParameters.ValidateHostedDomain)
                {
                    ValidateHostedDomain(domain!, principal);
                }
            }

            return principal;
        }

        private static void ValidateHostedDomain(string expectedDomain, ClaimsPrincipal principal)
        {
            var actualDomain = principal.FindFirst(GoogleClaimTypes.Domain)?.Value;

            if (string.IsNullOrEmpty(actualDomain))
            {
                throw LogHelper.LogExceptionMessage(new SecurityTokenInvalidDomainException(LogMessages.IDX10250) { InvalidDomain = null });
            }

            if (!actualDomain!.Equals(expectedDomain, StringComparison.OrdinalIgnoreCase))
            {
                var message = string.Format(LogMessages.IDX10251, actualDomain, expectedDomain);

                throw LogHelper.LogExceptionMessage(new SecurityTokenInvalidDomainException(message) { InvalidDomain = actualDomain });
            }

            LogHelper.LogInformation(LogMessages.IDX10252, actualDomain);
        }
    }
}

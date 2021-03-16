// ReSharper disable once CheckNamespace
namespace Microsoft.IdentityModel.Tokens
{
    public class SecurityTokenInvalidDomainException : SecurityTokenValidationException
    {
        public SecurityTokenInvalidDomainException(string message) : base(message)
        {
        }

        public string? InvalidDomain { get; set; }
    }
}

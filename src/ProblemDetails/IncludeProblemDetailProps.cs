namespace Hellang.Middleware.ProblemDetails
{
    /// <summary>
    /// Properties to include in ProblemDetails.
    /// By default, all properties are false and will not be included.
    /// </summary>
    public class IncludeProblemDetailProps
    {
        /// <summary>
        /// Include the Exception Details Stack
        /// </summary>
        public bool ExceptionDetails { get; set; }

        /// <summary>
        /// Include the Detail summary property
        /// </summary>
        public bool Detail { get; set; }
    }
}

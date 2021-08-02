using System;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Hellang.Middleware.ProblemDetails
{
    /// <summary>
    /// Extension methods for <see cref="MediaTypeCollection"/>.
    /// </summary>
    internal static class MediaTypeCollectionExtensions
    {
        /// <summary>
        /// Creates a new <see cref="MediaTypeCollection"/> with the same items as an existing <see cref="MediaTypeCollection"/>.
        /// </summary>
        /// <param name="mediaTypeCollection">The source <see cref="MediaTypeCollection"/> to copy items from.</param>
        /// <returns>A new <see cref="MediaTypeCollection"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="mediaTypeCollection"/> is <c>null</c>.</exception>
        public static MediaTypeCollection Clone(this MediaTypeCollection mediaTypeCollection)
        {
            if (mediaTypeCollection == null)
            {
                throw new ArgumentNullException(nameof(mediaTypeCollection));
            }

            var clone = new MediaTypeCollection();

            foreach (string mediaType in mediaTypeCollection)
            {
                clone.Add(mediaType);
            }

            return clone;
        }
    }
}

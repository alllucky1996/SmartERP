﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Smartstore.IO;
using Smartstore.Utilities;

namespace Smartstore
{
    public static class HttpRequestExtensions
    {
        private static readonly List<(string, string)> _sslHeaders = new List<(string, string)>
        {
            ("HTTP_CLUSTER_HTTPS", "on"),
            ("HTTP_X_FORWARDED_PROTO", "https"),
            ("X-Forwarded-Proto", "https"),
            ("x-arr-ssl", null),
            ("X-Forwarded-Protocol", "https"),
            ("X-Forwarded-Ssl", "on"),
            ("X-Url-Scheme", "https")
        };

        /// <summary>
        /// Tries to read a request value first from <see cref="HttpRequest.Form"/> (if method is POST), then from
        /// <see cref="HttpRequest.Query"/>.
        /// </summary>
        /// <param name="key">The key to lookup.</param>
        /// <param name="values">The found values if any</param>
        /// <returns><c>true</c> if a value with passed <paramref name="key"/> was present, <c>false</c> otherwise.</returns>
        public static bool TryGetValue(this HttpRequest request, string key, out StringValues values)
        {
            values = StringValues.Empty;

            if (request.HasFormContentType)
            {
                values = request.Form[key];
            }

            if (values == StringValues.Empty)
            {
                values = request.Query[key];
            }

            return values != StringValues.Empty;
        }

        /// <summary>
        /// Tries to read a request value first from <see cref="HttpRequest.Form"/> (if method is POST), then from
        /// <see cref="HttpRequest.Query"/>, and converts value to <typeparamref name="T"/>.
        /// </summary>
        /// <param name="key">The key to lookup.</param>
        /// <param name="value">The found and converted value</param>
        /// <returns><c>true</c> if a value with passed <paramref name="key"/> was present and could be converted, <c>false</c> otherwise.</returns>
        public static bool TryGetValueAs<T>(this HttpRequest request, string key, out T value)
        {
            value = default;

            if (request.TryGetValue(key, out var values))
            {
                return CommonHelper.TryConvert(values.ToString(), out value);
            }

            return false;
        }

        public static bool IsAdminArea(this HttpRequest request)
        {
            Guard.NotNull(request, nameof(request));

            // TODO: Not really reliable. Change this.

            // Try route
            if (request.HttpContext.TryGetRouteValueAs<string>("area", out var area) && area.EqualsNoCase("admin"))
            {
                // INFO: Module area views can also render in backend. So don't return false if area is not "admin".
                return true;
            }

            // Try URL prefix
            if (request.Path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public static string UserAgent(this HttpRequest httpRequest)
        {
            if (httpRequest.Headers.TryGetValue(HeaderNames.UserAgent, out var value))
            {
                return value.ToString();
            }

            return null;
        }

        /// <summary>
        /// Gets the raw request path (PathBase + Path + QueryString)
        /// </summary>
        /// <returns>The raw URL</returns>
        public static string RawUrl(this HttpRequest httpRequest)
        {
            // Try to resolve unencoded raw value from feature.
            var url = httpRequest.HttpContext.Features.Get<IHttpRequestFeature>()?.RawTarget;
            if (url.IsEmpty())
            {
                // Fallback
                url = httpRequest.PathBase + httpRequest.Path + httpRequest.QueryString;
            }

            return url;
        }

        public static string UrlReferrer(this HttpRequest httpRequest)
        {
            if (httpRequest.Headers.TryGetValue(HeaderNames.Referer, out var value))
            {
                return value.ToString();
            }

            return null;
        }

        /// <summary>
        /// Gets a value which indicates whether the HTTP connection uses secure sockets (HTTPS protocol). 
        /// Works with cloud's load balancers.
        /// </summary>
        public static bool IsSecureConnection(this HttpRequest httpRequest)
        {
            if (httpRequest.IsHttps)
            {
                return true;
            }

            foreach (var tuple in _sslHeaders)
            {
                var serverVar = httpRequest.Headers[tuple.Item1];
                if (serverVar != StringValues.Empty)
                {
                    return tuple.Item2 == null || tuple.Item2.Equals(serverVar, StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the current request is an AJAX request.
        /// </summary>
        /// <param name="httpRequest"></param>
        public static bool IsAjaxRequest(this HttpRequest httpRequest)
        {
            return
                string.Equals(httpRequest.Headers[HeaderNames.XRequestedWith], "XMLHttpRequest", StringComparison.Ordinal) ||
                string.Equals(httpRequest.Query[HeaderNames.XRequestedWith], "XMLHttpRequest", StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets a value which indicates whether the current request requests a static resource, like .txt, .pdf, .js, .css etc.
        /// </summary>
        public static bool IsStaticResourceRequested(this HttpRequest request)
        {
            if (request is null)
                return false;

            return MimeTypes.TryMapNameToMimeType(request.Path, out _);
        }
    }
}
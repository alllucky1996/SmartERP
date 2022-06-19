﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Smartstore;
using Smartstore.Core.Localization.Routing;

namespace Microsoft.AspNetCore.Builder
{
    public static class LocalizedEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Adds a localized endpoint that matches HTTP requests for the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The (language neutral) route pattern. A second endpoint with {culture:culture}/ prefix
        /// will be craeted and prepended to this endpoint.
        /// </param>
        /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
        public static IEndpointConventionBuilder MapLocalized(this IEndpointRouteBuilder endpoints, string pattern, RequestDelegate requestDelegate)
        {
            Guard.NotNull(endpoints, nameof(endpoints));

            return new CompositeEndpointConventionBuilder(new[] 
            {
                endpoints.Map("{culture:culture}/" + pattern, requestDelegate)
                    .WithMetadata(new LocalizedRouteMetadata(null, false)),
                endpoints.Map(pattern, requestDelegate)
                    .WithMetadata(new LocalizedRouteMetadata(null, true))
            });
        }

        /// <summary>
        /// Adds localized endpoints for controller actions and specifies
        /// a route with the given name, pattern, defaults, constraints, and dataTokens.
        /// </summary>
        /// <param name="name">The name of the route.</param>
        /// <param name="pattern">
        /// The (language neutral) route pattern. A second endpoint with {culture:culture}/ prefix
        /// will be craeted and prepended to this endpoint.
        /// </param>
        /// <param name="defaults">
        /// An object that contains default values for route parameters. The object's properties
        /// represent the names and values of the default values.
        /// </param>
        /// <param name="constraints">
        /// An object that contains constraints for the route. The object's properties represent
        /// the names and values of the constraints.
        /// </param>
        /// <param name="dataTokens">
        /// An object that contains data tokens for the route. The object's properties represent
        /// the names and values of the data tokens.
        /// </param>
        public static IEndpointConventionBuilder MapLocalizedControllerRoute(this IEndpointRouteBuilder endpoints, 
            string name, 
            string pattern, 
            object defaults = null, 
            object constraints = null, 
            object dataTokens = null)
        {
            Guard.NotNull(endpoints, nameof(endpoints));

            string name2 = name.IsEmpty() ? null : name + "__noculture";

            return new CompositeEndpointConventionBuilder(new[]
            {
                endpoints
                    .MapControllerRoute(name, "{culture:culture}/" + pattern, defaults, constraints, dataTokens)
                    .WithMetadata(new LocalizedRouteMetadata(null, false)),
                endpoints
                    .MapControllerRoute(name2, pattern, defaults, constraints, dataTokens)
                    .WithMetadata(new LocalizedRouteMetadata(null, true))
            });
        }
    }
}
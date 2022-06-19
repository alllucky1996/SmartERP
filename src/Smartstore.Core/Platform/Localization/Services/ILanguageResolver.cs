﻿using Microsoft.AspNetCore.Http;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Responsible for resolving the current customer's working language.
    /// </summary>
    public interface ILanguageResolver
    {
        Task<Language> ResolveLanguageAsync(Customer currentCustomer, HttpContext httpContext);
    }
}

﻿using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Smartstore.Core.DataExchange;
using Smartstore.Core.DataExchange.Import;
using Smartstore.Core.Identity;
using Smartstore.Core.Identity.Rules;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;
using Smartstore.Engine.Builders;
using Smartstore.Net;

namespace Smartstore.Core.Bootstrapping
{
    internal sealed class IdentityStarter : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddIdentity<Customer, CustomerRole>()
                .AddDefaultTokenProviders()
                .AddErrorDescriber<LocalizedIdentityErrorDescriber>()
                .AddRoleManager<RoleManager<CustomerRole>>()
                .AddUserValidator<UserValidator>()
                .AddPasswordValidator<UserValidator>()
                .AddSignInManager<SmartSignInManager>();

            services.TryAddEnumerable(
                    ServiceDescriptor.Singleton<IConfigureOptions<IdentityOptions>, IdentityOptionsConfigurer>());

            if (appContext.IsInstalled)
            {
                services.ConfigureApplicationCookie(options =>
                {
                    options.Cookie.Name = CookieNames.Identity;
                    options.LoginPath = "/login";
                    options.LogoutPath = "/logout";
                    options.AccessDeniedPath = "/access-denied";
                    options.ReturnUrlParameter = "returnUrl";
                });

                services.ConfigureExternalCookie(options =>
                {
                    options.Cookie.Name = CookieNames.ExternalAuthentication;
                    options.LoginPath = "/login";
                    options.LogoutPath = "/logout";
                    options.AccessDeniedPath = "/access-denied";
                    options.ReturnUrlParameter = "returnUrl";
                });

                services.AddAuthentication().AddCookie(CookieNames.ExternalAuthentication);
            }

            // TODO: (mh) (core) // Add Identity IEmailSender and ISmsSender to service collection.
            // RE: This won't be needed right now. Will be implemented when we offer real 2FA.
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<UserStore>().As<IUserStore>().As<IUserStore<Customer>>().InstancePerLifetimeScope();
            builder.RegisterType<RoleStore>().As<IRoleStore>().As<IRoleStore<CustomerRole>>().InstancePerLifetimeScope();
            builder.RegisterType<CustomerService>().As<ICustomerService>().InstancePerLifetimeScope();
            builder.RegisterType<VoidLookupNormalizer>().As<ILookupNormalizer>().InstancePerLifetimeScope();
            builder.RegisterType<PasswordHasher>().As<IPasswordHasher<Customer>>().InstancePerLifetimeScope();
            //builder.RegisterType<UserValidator>().As<IUserValidator<Customer>>().As<IPasswordValidator<Customer>>().InstancePerLifetimeScope();
            builder.RegisterType<GdprTool>().As<IGdprTool>().InstancePerLifetimeScope();
            builder.RegisterType<CookieConsentManager>().As<ICookieConsentManager>().InstancePerLifetimeScope();
            builder.RegisterType<CustomerImporter>().Keyed<IEntityImporter>(ImportEntityType.Customer).InstancePerLifetimeScope();

            // Rules.
            builder.RegisterType<TargetGroupService>()
                .As<ITargetGroupService>()
                .Keyed<IRuleProvider>(RuleScope.Customer)
                .InstancePerLifetimeScope();

            builder.RegisterType<CustomerRoleRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            if (builder.ApplicationContext.IsInstalled)
            {
                builder.Configure(StarterOrdering.AuthenticationMiddleware, app =>
                {
                    app.UseAuthentication();
                });

                builder.Configure(StarterOrdering.AfterRoutingMiddleware, app =>
                {
                    app.UseAuthorization();
                });
            }
        }
    }
}
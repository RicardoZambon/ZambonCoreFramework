﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Zambon.Core.Database;
using Zambon.Core.Database.ChangeTracker;
using Zambon.Core.Database.ChangeTracker.DI;
using Zambon.Core.Database.ChangeTracker.Services;
using Zambon.Core.Module;
using Zambon.Core.Module.Interfaces;
using Zambon.Core.Module.Services;
using Zambon.Core.Security.BusinessObjects;
using Zambon.Core.Security.Identity;
using Zambon.Core.WebModule.ActionFilters;
using Zambon.Core.WebModule.CustomProviders;
using Zambon.Core.WebModule.Services;
using Zambon.Core.Database.DI;

namespace Zambon.Core.WebModule
{
    public class ModuleStartup
    {
        public IConfigurationRoot Configuration { get; protected set; }

        protected IHostingEnvironment Env { get; set; }

        protected string AssemblyName { get; }


        protected bool UseDataProtectionInFileSystem { get; set; } = true;


        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public virtual void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseSession();

            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new EmbeddedFileProvider(typeof(Zambon.Core.WebModule.ModuleStartup).GetTypeInfo().Assembly, "Zambon.Core.WebModule.wwwroot")
            });

            app.UseAuthentication();

            //app.UseHttpsRedirection();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }


        #region Configuring Services

        public void CustomConfigureServices(IServiceCollection services, IConfigurationRoot Configuration)
        {
            CustomConfigureServices<Users, Roles>(services, Configuration);
        }

        public void CustomConfigureServices<U, R>(IServiceCollection services, IConfigurationRoot Configuration) where U : class, IUsers where R : class, IRoles
        {
            StartConfiguringServices(services, Configuration);

            ConfigureDatabase(services, Configuration);

            EndConfiguringServices<U, R>(services, Configuration);
        }


        private void StartConfiguringServices(IServiceCollection services, IConfigurationRoot Configuration)
        {
            var appConfigs = Configuration.GetSection(nameof(ApplicationConfigs)).AsEnumerable().Where(x => x.Key != nameof(ApplicationConfigs)).ToDictionary(x => x.Key.Replace($"{nameof(ApplicationConfigs)}:", ""), y => y.Value);
            services.Configure<ApplicationConfigs>(a => { a.Set(appConfigs); });

            if (UseDataProtectionInFileSystem)
                services.AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(appConfigs.ContainsKey("FileStorePath") ? appConfigs["FileStorePath"].ToString() : string.Empty, "DataProtection")))
                    .SetApplicationName(Env.ApplicationName.Replace(".", "_"));

            services.ConfigureApplicationCookie(options => { options.Cookie.Name = string.Format(".AspNetCore.{0}.Cookies", Env.ApplicationName.Replace(".", "_")); });
        }

        private void EndConfiguringServices<U, R>(IServiceCollection services, IConfigurationRoot Configuration) where U : class, IUsers where R : class, IRoles
        {
            ConfigureIdentityAuthentication<U, R>(services, Configuration);

            ConfigureApplicationModel(services, Configuration);

            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.FileProviders.Add(
                    new CompositeFileProvider(new EmbeddedFileProvider(typeof(Zambon.Core.WebModule.ModuleStartup).GetTypeInfo().Assembly, "Zambon.Core.WebModule"))
                );
            });

            services.AddMvc(config =>
            {
                config.ModelMetadataDetailsProviders.Add(new CoreMetaDataProvider());

                var policy = new AuthorizationPolicyBuilder()
                     .RequireAuthenticatedUser()
                     .Build();
                config.Filters.Add(new AuthorizeFilter(policy));
            }).SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_2_1).AddSessionStateTempDataProvider();

            var serviceDescriptor = services.FirstOrDefault((s => s.ServiceType == typeof(IObjectModelValidator)));
            services.Remove(serviceDescriptor);
            services.Add(new ServiceDescriptor(typeof(IObjectModelValidator), _ => new CustomModelValidator(), ServiceLifetime.Singleton));

            services.AddSession();

            //services.AddHsts(options =>
            //{
            //    options.Preload = true;
            //    options.IncludeSubDomains = true;
            //    options.MaxAge = TimeSpan.FromDays(60);
            //});

            //services.AddHttpsRedirection(options =>
            //{
            //    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
            //    //options.HttpsPort = 5001;
            //});
        }

        #endregion

        #region Database

        private void ConfigureDatabase(IServiceCollection services, IConfigurationRoot Configuration)
        {
            services.AddChangeTrackerServices<ChangeTrackerService>();
            services.AddChangeTrackerDbCache(options =>
            {
                options.ConnectionString = Configuration.GetConnectionString("DefaultConnection");
                options.SchemaName = "Cache";
                options.TableName = "CachedData";
            });
            //services.AddSingleton<IInstanceKeyService, ChangeTrackerService>();
            //services.AddSingleton<CoreChangeTracker>();
            //services.AddDistributedSqlServerCache(options =>
            //{
            //    options.ConnectionString = Configuration.GetConnectionString("DefaultConnection");
            //    options.SchemaName = "Cache";
            //    options.TableName = "CachedData";
            //});

            services.AddDatabase(Configuration.GetConnectionString("DefaultConnection"), this.GetType().Assembly.GetName().Name);
            //services.AddEntityFrameworkSqlServer();
            //services.AddEntityFrameworkProxies();
            //services.AddDbContextPool<CoreDbContext>((serviceProvider, optionsBuilder) =>
            //{
            //    optionsBuilder.UseLazyLoadingProxies().UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
            //        b => b.MigrationsAssembly(this.GetType().Assembly.GetName().Name).MigrationsHistoryTable("MigrationsHistory", "EF")
            //    );
            //    optionsBuilder.UseInternalServiceProvider(serviceProvider);
            //});
        }

        #endregion

        #region Identity / Authentication

        private void ConfigureIdentityAuthentication(IServiceCollection services, IConfigurationRoot Configuration)
        {
            ConfigureIdentityAuthentication<Users, Roles>(services, Configuration);
        }

        private void ConfigureIdentityAuthentication<U, R>(IServiceCollection services, IConfigurationRoot Configuration) where U : class, IUsers where R : class, IRoles
        {
            services.AddIdentity<U, R>(o => { o.Password = CoreUserManager<U>.passwordOptions; })
                .AddUserManager<CoreUserManager<U>>()
                .AddRoleManager<CoreRoleManager<R>>()
                .AddUserStore<CoreUserStore<U>>()
                .AddRoleStore<CoreRoleStore<R>>()
                .AddDefaultTokenProviders();

            services.AddScoped<IUserClaimsPrincipalFactory<U>, ClaimsPrincipalFactory<U, R>>();

            services.AddTransient<IUserProvider, UserProvider<U>>();

            services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

            })
            .AddCookie(o => {
                o.LoginPath = new PathString("/Account/Login");
                o.SlidingExpiration = true;
                o.ExpireTimeSpan = new TimeSpan(336, 0, 0);
                o.Cookie.Name = string.Format(".AspNetCore.{0}.Cookies", Env.ApplicationName.Replace(".", "_"));
            });

            services.AddScoped<IUserService, CurrentUserService<U,R>>();
        }

        #endregion

        #region Application / Model

        private void ConfigureApplicationModel(IServiceCollection services, IConfigurationRoot Configuration)
        {
            services.AddSingleton<ModelService>();
            services.AddScoped<ILanguageService, LanguageService>();
            services.AddScoped<ApplicationService>();
            services.AddScoped<ExpressionsService>();
        }

        #endregion

    }
}
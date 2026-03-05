using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Serialization;
using System;
using System.Text;
using ValueCards.Hubs;
using ValueCards.Models;
using ValueCards.Services;
using ValueCards.Services.Identity;

namespace ValueCards
{
  public class Startup
  {
    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
      Configuration = configuration;
      Env = env;
    }

    public IConfiguration Configuration { get; }
    public IWebHostEnvironment Env { get; set; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.Configure<EmailSettings>(Configuration.GetSection("EmailSettings"));

      services.AddControllersWithViews()
        .AddNewtonsoftJson(options => options.SerializerSettings.ContractResolver = new DefaultContractResolver());
      //services.AddRazorPages()
      //  .AddRazorRuntimeCompilation();

      // configure strongly typed settings objects
      var appSettingsSection = Configuration.GetSection("AppSettings");
      services.Configure<AppSettings>(appSettingsSection);

      // configure jwt authentication
      var appSettings = appSettingsSection.Get<AppSettings>();
      var key = Encoding.ASCII.GetBytes(appSettings.Secret);

      IMvcBuilder builder = services.AddRazorPages();
#if DEBUG
      if (Env.IsDevelopment())
      {
        builder.AddRazorRuntimeCompilation();
      }
#endif

      services.AddDefaultIdentity<SBUser>(options =>
      {
        options.SignIn.RequireConfirmedAccount = false;
      })
        .AddUserStore<SBUserStore>()
        .AddClaimsPrincipalFactory<SBUserClaimsPrincipalFactory>()
        //.AddUserManager<SBUserManager>()
        .AddSignInManager<SBSignInManager>()
        .AddDefaultTokenProviders();

      //services.AddScoped<SBUserManager>();
      //services.AddScoped<UserManager<SBUser>>(x => (UserManager<SBUser>)x.GetRequiredService<SBUserManager>());

      services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
          options.LoginPath = "/account/login";
        });
      services.Configure<CookiePolicyOptions>(options =>
      {
        options.CheckConsentNeeded = context => true;
        options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.None;
      });

      services.AddAuthorization(options =>
      {
        var defaultAuthorizationPolicyBuilder = new AuthorizationPolicyBuilder(CookieAuthenticationDefaults.AuthenticationScheme);
        defaultAuthorizationPolicyBuilder = defaultAuthorizationPolicyBuilder.RequireAuthenticatedUser();
        options.DefaultPolicy = defaultAuthorizationPolicyBuilder.Build();

        options.AddPolicy(CookieAuthenticationDefaults.AuthenticationScheme, policy =>
        {
          policy.AuthenticationSchemes.Add(CookieAuthenticationDefaults.AuthenticationScheme);
          policy.RequireAuthenticatedUser();
          policy.RequireClaim("shiftId");
        });
      });

      //services.AddScoped<IAuthenticationHandlerProvider, MyAuthenticationHandlerProvider>();
      //services.AddTransient<IClaimsTransformation, ClaimsTransformer>();

      services.AddSingleton<ITempDataProvider, CookieTempDataProvider>();
      services.Configure<CookieTempDataProviderOptions>(options =>
      {
        options.Cookie.IsEssential = true;
      });

      services.Configure<WebServiceOption>(Configuration.GetSection("WebService"));

      services.AddHealthChecks();

      services.AddSignalR();

      services.AddKendo();

      services.AddScoped<IApiClient, ApiClient>();
      services.AddScoped<IConsumerRepository, CachedConsumerRepository>();
      services.AddScoped<IConsumerService, ConsumerService>();

      services.AddSingleton<IEmailSender, EmailSender>();
      services.AddSingleton<IRichEmailSender, EmailSender>(x => (EmailSender)x.GetService<IEmailSender>());
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseDatabaseErrorPage();
      }
      else
      {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }

      
       //app.UseHttpsRedirection();
      app.UseStaticFiles();

      app.UseRouting();

      app.UseAuthentication();

      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
      // endpoints.MapControllers();
       endpoints.MapHub<ProgressHub>("/progressHub");
       endpoints.MapControllerRoute(
                  name: "default",
                  pattern: "{controller=consumers}/{action=Index}/{id?}");
        endpoints.MapRazorPages();
        endpoints.MapHealthChecks("/healthz", new HealthCheckOptions() { });
      });

    }
  }
}

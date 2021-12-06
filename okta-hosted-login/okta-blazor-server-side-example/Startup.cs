using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using okta_blazor_server_side_example.Data;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using System.IO;
using Microsoft.AspNetCore.Http;
using Okta.AspNetCore;

namespace okta_blazor_server_side_example
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddSingleton<WeatherForecastService>();
            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            }
            )
                .AddCookie()
               
            .AddOktaMvc(new OktaMvcOptions
            {
                // Replace the Okta placeholders in appsettings.json with your Okta configuration.
                OktaDomain = Configuration.GetValue<string>("Okta:OktaDomain"),
                ClientId = Configuration.GetValue<string>("Okta:ClientId"),
                ClientSecret = Configuration.GetValue<string>("Okta:ClientSecret"),
               // AuthorizationServerId = Configuration.GetValue<string>("Okta:AuthorizationServerId"),
                OnAuthenticationFailed = OnAuthenticationFailed,
                OnOktaApiFailure = OnOktaApiFailure,
                OnTokenValidated = OnTokenValidated,

            });
        }
        // TODO  Check with Plain OIDC
        //  https://developer.okta.com/blog/2017/06/29/oidc-user-auth-aspnet-core
        // TODO: Check if intercept of callback is possible or what callback does
        private async Task OnTokenValidated(TokenValidatedContext arg)
        {
            // arg.Principal is correct but httpContext user not :S
            arg.HttpContext.User = arg.Principal;
            
         Console.WriteLine(arg.Response.StatusCode);
            Console.WriteLine(arg.Result);
        }

        public async Task OnOktaApiFailure(RemoteFailureContext context)
        {
            await Task.Run(() =>
            {
                context.Response.Redirect("{YOUR-EXCEPTION-HANDLING-ENDPOINT}?message=" + context.Failure.Message);
                context.HandleResponse();
            });
        }

        public async Task OnAuthenticationFailed(AuthenticationFailedContext context)
        {
            await Task.Run(() =>
            {
                context.Response.Redirect("{YOUR-EXCEPTION-HANDLING-ENDPOINT}?message=" + context.Exception.Message);
                context.HandleResponse();
            });
        }
       

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.Use(async (context, next) =>
            {
                // Do work that doesn't write to the Response.
               /* var request = context.Request;
                var stream = new StreamReader(request.Body);
                var body = await stream.ReadToEndAsync();
                Console.WriteLine(body);*/
                await next.Invoke();
                // Do logging or other work that doesn't write to the Response.
            });
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            

        }
    }
}

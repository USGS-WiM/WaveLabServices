using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WaveLabAgent;
using WaveLabAgent.Resources;
using Microsoft.AspNetCore.Mvc;
using WIM.Services.Middleware;
using WIM.Services.Analytics;
using WIM.Utilities.ServiceAgent;
using WIM.Services.Resources;
using WaveLabServices.Filters;
using Microsoft.AspNetCore.Http.Features;
using WIM.Services.Messaging;

namespace WaveLabServices
{
    public class Startup
    {
        private string _hostKey = "USGSWiM_HostName";
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables();
            if (env.IsDevelopment()) {
                builder.AddApplicationInsightsSettings(developerMode: true);
            }

            Configuration = builder.Build();
        }//end startup       

        public IConfigurationRoot Configuration { get; }

        //Method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Angular's default header name for sending the XSRF token.
            services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");

            //add functionality to inject IOptions<T>
            services.AddOptions();
            //Configure injectable obj
            services.Configure<APIConfigSettings>(Configuration.GetSection("APIConfigSettings"));
            services.Configure<ProcedureSettings>(Configuration.GetSection("ProcedureSettings"));
            
            //provides access to httpcontext
            services.AddHttpContextAccessor();

            // Add framework services
            services.AddScoped<IWaveLabAgent, WaveLabAgent.WaveLabAgent>();
            services.AddScoped<IAnalyticsAgent, GoogleAnalyticsAgent>((gaa)=> new GoogleAnalyticsAgent(Configuration["AnalyticsKey"]));

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder => builder.AllowAnyOrigin()
                                                                 .AllowAnyMethod()
                                                                 .AllowAnyHeader()
                                                                 .WithExposedHeaders(new string[] { this._hostKey, X_MessagesDefault.msgheader })
                                                                 .AllowCredentials());
            });
            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue;
                x.MultipartHeadersLengthLimit = int.MaxValue;
            });

            services.AddMvc(options => { options.RespectBrowserAcceptHeader = true;
                options.Filters.Add(new WaveLabHypermedia());})                               
                                .AddJsonOptions(options => loadJsonOptions(options));  
                                
                                
        }     

        // Method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseX_Messages(option => { option.HostKey = this._hostKey; });
            app.UseCors("CorsPolicy");
            app.Use_Analytics();
            app.UseMvc();            
        }

        #region Helper Methods
        private void loadJsonOptions(MvcJsonOptions options)
        {
            options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            options.SerializerSettings.MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore;
            options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            options.SerializerSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None;
            options.SerializerSettings.TypeNameAssemblyFormatHandling = Newtonsoft.Json.TypeNameAssemblyFormatHandling.Simple;
            options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.None;
        }
        #endregion
    }
}

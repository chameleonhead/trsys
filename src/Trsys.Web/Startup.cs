using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trsys.Web.Configurations;
using Trsys.Web.Data;
using Trsys.Web.Infrastructure;
using Trsys.Web.Services;

namespace Trsys.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews(options =>
            {
                options.InputFormatters.Add(new TextPlainInputFormatter());
            })
                .AddRazorRuntimeCompilation()
                .AddSessionStateTempDataProvider();

            services.AddSession();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.LogoutPath = "/logout";
                    options.ReturnUrlParameter = "returnUrl";
                });

            services.AddMediatR(typeof(Startup).Assembly);
            services.AddInMemoryInfrastructure();

            services.AddSingleton(new PasswordHasher(Configuration.GetValue<string>("Trsys.Web:PasswordSalt")));
            var sqliteConnection = Configuration.GetConnectionString("SqliteConnection");
            if (string.IsNullOrEmpty(sqliteConnection))
            {
                services.AddDbContext<TrsysContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
                services.AddRepositories();
            }
            else
            {
                services.AddDbContext<TrsysContext>(options => options.UseSqlite(sqliteConnection));
                services.AddSQLiteRepositories();
            }
            services.AddTransient<EventService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            var sqliteConnection = Configuration.GetConnectionString("SqliteConnection");
            if (string.IsNullOrEmpty(sqliteConnection))
            {
                logger.LogInformation("Using sql server connection.");
            }
            else
            {
                logger.LogInformation("Using sqlite connection.");
            }
            logger.LogInformation("Database initializing.");
            DatabaseInitializer.InitializeAsync(app).Wait();
            logger.LogInformation("Database initialized.");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseSession();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}

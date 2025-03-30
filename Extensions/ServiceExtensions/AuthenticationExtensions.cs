namespace DoAnWeb.Extensions.ServiceExtensions
{
    public static class AuthenticationExtensions
    {
        /// <summary>
        /// Adds authentication services with cookie, Google, and GitHub providers
        /// </summary>
        public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure cookie-based authentication
            services.AddAuthentication("CookieAuth")
                .AddCookie("CookieAuth", options =>
                {
                    options.Cookie.Name = "DevCommunityAuth";
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                })
                .AddGoogle(options =>
                {
                    options.ClientId = configuration["Authentication:Google:ClientId"];
                    options.ClientSecret = configuration["Authentication:Google:ClientSecret"];
                    options.CallbackPath = "/account/signin-google";
                    options.SaveTokens = true;
                })
                .AddGitHub(options =>
                {
                    options.ClientId = configuration["Authentication:GitHub:ClientId"];
                    options.ClientSecret = configuration["Authentication:GitHub:ClientSecret"];
                    options.CallbackPath = "/account/github-signin";
                    options.SaveTokens = true;
                    options.Scope.Add("user:email");
                });

            return services;
        }

        /// <summary>
        /// Adds SignalR services with advanced configuration
        /// </summary>
        public static IServiceCollection AddSignalRServices(this IServiceCollection services)
        {
            // Add SignalR for real-time features with advanced options
            services.AddSignalR(options => 
            {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = 102400; // 100 KB
                options.StreamBufferCapacity = 20;
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            });

            return services;
        }
    }
} 
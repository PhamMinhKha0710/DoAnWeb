using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

namespace DoAnWeb.Extensions.ServiceExtensions
{
    public static class InfrastructureExtensions
    {
        /// <summary>
        /// Adds response compression services for improved performance
        /// </summary>
        public static IServiceCollection AddCompressionServices(this IServiceCollection services)
        {
            // Add response compression to reduce bandwidth usage and improve load times
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true; // Enable compression for HTTPS connections
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                
                // Add SignalR endpoints to compression
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });

            // Configure compression providers for optimal speed
            services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest; // Optimize for speed
            });

            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest; // Optimize for speed
            });

            return services;
        }

        /// <summary>
        /// Adds CORS services
        /// </summary>
        public static IServiceCollection AddCorsServices(this IServiceCollection services)
        {
            // Add CORS policy to allow connections from other devices
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                    builder
                        .WithOrigins("https://example.com", "https://sub.example.com")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
            });

            return services;
        }

        /// <summary>
        /// Adds memory caching
        /// </summary>
        public static IServiceCollection AddCachingServices(this IServiceCollection services)
        {
            // Add in-memory caching for frequently accessed data
            services.AddMemoryCache();

            return services;
        }

        /// <summary>
        /// Adds session services
        /// </summary>
        public static IServiceCollection AddSessionServices(this IServiceCollection services)
        {
            // Add Session services
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            return services;
        }
    }
} 
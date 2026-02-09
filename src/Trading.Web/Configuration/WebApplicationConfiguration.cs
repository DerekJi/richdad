namespace Trading.Web.Configuration;

/// <summary>
/// Web应用程序配置扩展方法
/// </summary>
public static class WebApplicationConfiguration
{
    /// <summary>
    /// 配置 Web 服务（Controllers, CORS, Swagger）
    /// </summary>
    public static IServiceCollection AddWebServices(this IServiceCollection services)
    {
        // 添加控制器
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            });

        // 配置 CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        // 添加 Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "Trading Alert System API", Version = "v1" });
        });

        return services;
    }

    /// <summary>
    /// 配置 Web 应用程序中间件
    /// </summary>
    public static WebApplication ConfigureWebApplication(this WebApplication app)
    {
        // 启用 CORS
        app.UseCors();

        // 启用 Swagger（开发和生产环境）
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Trading Alert System API V1");
            c.RoutePrefix = "swagger";
        });

        // 配置静态文件
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseHttpsRedirection();
        app.UseAuthorization();

        app.MapControllers();

        return app;
    }
}

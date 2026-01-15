// TradingBacktest Web Application
// TODO: 实现Web界面用于参数优化和可视化回测结果

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Placeholder endpoint
app.MapGet("/", () => new 
{ 
    message = "TradingBacktest Web API",
    status = "Under Development",
    version = "1.0.0",
    description = "Web界面开发中，请使用Console应用进行回测"
});

app.Run();

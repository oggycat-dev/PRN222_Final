using DAL.Data;
using DAL.Repositories;
using FinalProject.Hubs;
using Microsoft.EntityFrameworkCore;
using Services.Implementations;
using Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add SignalR
builder.Services.AddSignalR();

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPromptSessionRepository, PromptSessionRepository>();
builder.Services.AddScoped<IAIUsageRepository, AIUsageRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<INovelRepository, NovelRepository>();
builder.Services.AddScoped<IChapterRepository, ChapterRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPromptSessionService, PromptSessionService>();
builder.Services.AddScoped<IAIService, GeminiAIService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
builder.Services.AddScoped<INovelService, NovelService>();
builder.Services.AddScoped<IChapterService, ChapterService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IVNPayService, VNPayService>();
builder.Services.AddScoped<ISignalRHubService, FinalProject.Services.SignalRHubService>();
builder.Services.AddScoped<INotificationService, Services.Implementations.SignalRNotificationService>();

var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
    await seeder.SeedAsync();
    
    // Validate Gemini API connection
    var aiService = scope.ServiceProvider.GetRequiredService<IAIService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var isConnected = await aiService.ValidateAPIConnectionAsync();
        if (isConnected)
        {
            logger.LogInformation("✅ Gemini API connection validated successfully");
        }
        else
        {
            logger.LogWarning("⚠️  Gemini API connection validation failed - API may not be working properly");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Failed to validate Gemini API connection during startup");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add session middleware
app.UseSession();

app.UseAuthorization();

app.MapRazorPages();

// Map SignalR hub
app.MapHub<ChatHub>("/chathub");
app.MapHub<NotificationHub>("/notificationhub");

app.Run();

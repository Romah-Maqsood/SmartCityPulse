using SmartCityPulse.Data;

var builder = WebApplication.CreateBuilder(args);

// ✅ MongoDB Context – manually provide connection strings from config
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connStr = config["MongoDB:ConnectionString"];
    var dbName = config["MongoDB:DatabaseName"];
    return new MongoDbContext(connStr, dbName);
});

// Add SignalR
builder.Services.AddSignalR();

// Add Session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();          // <-- Session comes after Routing, before Authorization
app.UseAuthorization();

// app.MapHub<SmartCityPulse.Hubs.CityHub>("/cityHub");   // Jab SignalR implement karo tab enable karna

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
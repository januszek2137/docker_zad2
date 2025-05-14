using docker_zad1.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<IWeatherService, WeatherService>();

var app = builder.Build();

var author = "Jan O¿ga";
var port = int.Parse(
   (Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:8080")
     .Split(':').Last()
);
app.Logger.LogInformation(
   "[{Time}] Autor: {Author}. App listening on TCP {Port}",
   DateTimeOffset.Now.ToString("u"), author, port
);

// Configure the HTTP request pipeline.
if(!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
   name: "default",
   pattern: "{controller=Weather}/{action=Index}/{id?}");

app.Run();

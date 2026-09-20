using Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.AddWeb();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

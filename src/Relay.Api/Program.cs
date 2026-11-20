using Relay.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>();

var app = builder.Build();

app.UseHttpsRedirection();

app.Run();

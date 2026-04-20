using Microsoft.Extensions.Options;
using TodoApi.Data;
using TodoApi.Options;
using TodoApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TodoDatabaseOptions>(
    builder.Configuration.GetSection(TodoDatabaseOptions.SectionName));

builder.Services.AddScoped<ITodoService, TodoService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var dbOptions = app.Services.GetRequiredService<IOptions<TodoDatabaseOptions>>().Value;
DatabaseInitializer.EnsureCreated(dbOptions.ConnectionString);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

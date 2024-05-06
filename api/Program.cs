var builder = WebApplication.CreateBuilder(args);

builder.Services
  .Configure<RecipesConfig>(
    builder.Configuration.GetSection(nameof(RecipesConfig))
  )
  .AddCors()
  .AddScoped<RecipeGenerator>();

var app = builder.Build();

// 👇 Set up CORS to allow front-end call.
app.UseCors(policy => policy
  .AllowAnyHeader()
  .AllowAnyMethod()
  .WithOrigins(
    "http://localhost:5173"
  ));

// 👇 The main entry point.
app.MapPost("/generate", async (
  HttpContext context,
  RecipeGenerator generator,
  RecipeRequest request,
  CancellationToken cancellation = default
) => {
  context.Response.Headers.ContentType = "text/event-stream";

  await generator.GenerateAsync(
    request,
    // Handler that writes the streaming response, one fragment at a time.
    async (Fragment f) => {
      await context.Response.WriteAsync(
        $"data: {f.Part}|{f.Content}{Environment.NewLine}{Environment.NewLine}",
        cancellation
      );
      await context.Response.Body.FlushAsync(cancellation);
    }
  );
});

// 👇 Start the app.
app.Run();

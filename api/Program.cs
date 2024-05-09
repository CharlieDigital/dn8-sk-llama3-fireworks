using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

var fireworksEndpoint = new Uri("https://api.fireworks.ai/inference/v1/chat/completions");
var groqEndpoint = new Uri("https://api.groq.com/openai/v1/chat/completions");
var togetherEndpoint = new Uri("https://api.together.xyz/v1/chat/completions");

using var togetherHttpClient = new HttpClient(new TogetherHttpHandler());

var config = builder.Configuration
  .GetSection(nameof(RecipesConfig))
  .Get<RecipesConfig>();

// Set up Semantic Kernel
var kernelBuilder = Kernel.CreateBuilder();
var kernel = kernelBuilder
  .AddOpenAIChatCompletion(
    modelId: "accounts/fireworks/models/llama-v3-70b-instruct",
    apiKey: config!.FireworksKey,
    endpoint: fireworksEndpoint,
    serviceId: "70b"
  )
  .AddOpenAIChatCompletion(
    modelId: "accounts/fireworks/models/llama-v3-8b-instruct",
    apiKey: config!.FireworksKey,
    endpoint: fireworksEndpoint,
    serviceId: "8b"
  )
  .AddOpenAIChatCompletion(
    modelId: "meta-llama/Llama-3-70b-chat-hf",
    apiKey: config!.TogetherKey,
    endpoint: togetherEndpoint,
    serviceId: "together-70b",
    httpClient: togetherHttpClient
  )
  .AddOpenAIChatCompletion(
      modelId: "llama3-8b-8192",
      apiKey: config!.GroqKey,
      endpoint: groqEndpoint,
      serviceId: "groq-8b"
  )
  .Build();

builder.Services
  .Configure<RecipesConfig>(
    builder.Configuration.GetSection(nameof(RecipesConfig))
  )
  .AddCors()
  .AddSingleton(kernel)
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

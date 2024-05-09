using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

public partial class RecipeGenerator {
  private static readonly JsonSerializerOptions _options = new () {
    PropertyNameCaseInsensitive = true
  };

  private readonly Channel<Fragment> _channel;
  private readonly Kernel _kernel;

  /// <summary>
  /// Injection constructor invoked by the DI container.
  /// </summary>
  /// <param name="kernel">The injected kernel singleton.</param>
  public RecipeGenerator(
    Kernel kernel
  ) {
    _channel = Channel.CreateUnbounded<Fragment>();
    _kernel = kernel;
  }

  /// <summary>
  /// The main entry point
  /// </summary>
  public async Task GenerateAsync(
    RecipeRequest request,
    Func<Fragment, Task> handler,
    CancellationToken cancellation = default
  ) {
    // (1) Generate the list of recipes and pick one at random.
    var (ingredientsOnHand, prepTime) = request;

    var recipes = await GenerateRecipesAsync(ingredientsOnHand, prepTime, cancellation);
    var recipe = recipes[Random.Shared.Next(0, 2)];

    var alternates = recipes
      .Where(r => r.Name != recipe.Name)
      .Aggregate(new StringBuilder(), (html, r) => {
        html.Append($"<li><b>{r.Name}</b> &nbsp;");
        html.Append($"<i>{r.Intro}</i></li>");

        return html;
      }).ToString();

    var fragmentHandler = async () => {
      while (await _channel.Reader.WaitToReadAsync()) {
        if (_channel.Reader.TryRead(out var fragment)) {
          await handler(fragment); // 👈 This is connected to the HTTP response stream
        }
      }
    };

    var completion = fragmentHandler();

    // (2) Now generate the first parts in parallel for our random recipe.
    Task.WaitAll([
      handler(new ("alt", alternates)),
      GenerateIngredientsAsync(recipe, ingredientsOnHand, request.PrepTime, cancellation),
      GenerateIntroAsync(recipe, cancellation),
      GenerateIngredientIntroAsync(ingredientsOnHand, cancellation),
      GenerateSidesAsync(recipe, cancellation)
    ], cancellation);

    _channel.Writer.Complete();

    await completion;
  }

  /// <summary>
  /// Executes the prompt and writes the result to the channel.
  /// </summary>
  private async Task ExecutePromptAsync(
    string part,
    string prompt,
    OpenAIPromptExecutionSettings settings,
    Action<string>? resultHandler = null,
    string? modelOverride = null,
    CancellationToken cancellation = default
  ) {
    Console.WriteLine($"Running generation for part: {part}");

    var chat = _kernel.GetRequiredService<IChatCompletionService>(modelOverride ?? "together-70b");
    var history = new ChatHistory();
    var buffer = new StringBuilder();

    history.AddUserMessage(prompt);

    await foreach (var message in chat.GetStreamingChatMessageContentsAsync(
        history, settings, _kernel, cancellation
      )
    ) {
        await _channel.Writer.WriteAsync(
          new(part, message.Content ?? ""),
          cancellation
        );

        buffer.Append(message.Content);
    }

    var output = buffer.ToString();

    resultHandler?.Invoke(output); // 👈 If the caller wants the full string, we hand it over here.

    Console.WriteLine("----");
    Console.WriteLine(output);
  }
}

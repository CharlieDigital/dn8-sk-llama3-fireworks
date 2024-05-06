using System.Text.Json;

public partial class RecipeGenerator {
  /// <summary>
  /// First step: generate a list of possible recipes. We'll
  /// only pick one of them.
  /// </summary>
  /// <returns>
  /// The list of recipes that we can make with the list of
  /// ingredients that we have on hand.
  /// </returns>
  private async Task<RecipeSummary[]> GenerateRecipesAsync(
    string ingredientsOnHand,
    string prepTime,
    CancellationToken cancellation = default
  ) {
    var prompt = $$"""
                 You are a writer for America's Test Kitchen
                 You have been given a list of ingredients and prep time
                 Your job is to think of 3 recipes that we can make with these ingredients within the prep time
                 Here is a list of ingredients we have already: {{ingredientsOnHand}}
                 These are just the ingredients we already have on hand
                 You can include recipes that have more ingredients
                 The ideal prep time is {{prepTime}} minutes or less; pick recipes that can be prepared in this time limit
                 WRITE ONLY THE JSON DO NOT WRITE A PROLOGUE; JUST WRITE THE CONTENT
                 DO NOT WASTE TOKENS ON WHITESPACE WRITE THE JSON AS A SINGLE LINE
                 Write your output as JSON using the format:

                 [
                  {
                    "name": "(the name of the recipe)",
                    "intro": "(a sentence describing this recipe)"
                  },
                 ]
                 """;

    var json = "";

    await ExecutePromptAsync(
      "init",
      prompt,
      new () {
        MaxTokens = 500,
        Temperature = 0.25,
        TopP = 0
      },
      (output) => json = output,
      "8b",
      cancellation
    );

    return JsonSerializer.Deserialize<RecipeSummary[]>(json, _options) ?? [];
  }

  /// <summary>
  /// Generates the list of ingredients given the recipe
  /// and the list of ingredients that we have on hand.
  /// </summary>
  private async Task GenerateIngredientsAsync(
    RecipeSummary recipe,
    string ingredientsOnHand,
    string prepTime,
    CancellationToken cancellation = default
  ) {
    var prompt = $"""
                 You are a writer for America's Test Kitchen
                 We are making the recipe: {recipe.Name}
                 Here is the description: {recipe.Intro}
                 Here are the ingredients we already have: {ingredientsOnHand}
                 Write a list of the entire list of ingredients that we need
                 Write each ingredient followed by a "⮑"
                 Example: 1/2 teaspoon salt⮑
                 Write the entire list as a single line
                 WRITE ONLY THE LIST OF INGREDIENTS DO NOT WRITE A PROLOGUE; JUST WRITE THE CONTENT
                 """;

    var ingredients = "";

    await ExecutePromptAsync(
      "add",
      prompt,
      new () {
        MaxTokens = 200,
        Temperature = 0.25,
        TopP = 0
      },
      i => ingredients = i,
      cancellation: cancellation
    );

    await GenerateStepsAsync(recipe, ingredients, prepTime, cancellation);
  }

  /// <summary>
  /// Generates an intro paragraph for the recipe.
  /// </summary>
  private async Task GenerateIntroAsync(
    RecipeSummary recipe,
    CancellationToken cancellation = default
  ) {
    var prompt = $"""
                 You are a writer for America's Test Kitchen
                 We are making the recipe: {recipe.Name}
                 Here is the description: {recipe.Intro}
                 Write a 3 to 5 sentence paragraph introducing the recipe
                 Write about topics like the origin of the recipe, the flavor profile, and best occasions for this recipe.
                 WRITE ONLY THE PARAGRAPH DO NOT WRITE A PROLOGUE; JUST WRITE THE CONTENT
                 """;

    await ExecutePromptAsync(
      "int",
      prompt,
      new () {
        MaxTokens = 250,
        Temperature = 0.55,
        TopP = 0
      },
      cancellation: cancellation
    );
  }

  /// <summary>
  /// An short intro to each ingredient in our recipe.
  /// </summary>
  private async Task GenerateIngredientIntroAsync(
    string ingredientsOnHand,
    CancellationToken cancellation = default
  ) {
    var prompt = $"""
                 You are a writer for America's Test Kitchen
                 You are writing about the nutritional information about food
                 Here are some ingredients we are working with: {ingredientsOnHand}
                 Write each ingredient followed by a "⮑"
                 Then write a short sentence about the ingredient focusing on nutritional information followed by two "⮑"
                 Write your entire output as a single line
                 WRITE ONLY THE LIST OF INGREDIENTS DO NOT WRITE A PROLOGUE; JUST WRITE THE CONTENT

                 EXAMPLE:
                 Bell peppers⮑Bell peppers are high in vitamin C and add color, flavor, and texture to any dish.⮑⮑
                 """;

    await ExecutePromptAsync(
      "ing",
      prompt,
      new () {
        MaxTokens = 200,
        Temperature = 0.25,
        TopP = 0
      },
      cancellation: cancellation
    );
  }

  /// <summary>
  /// Generate the steps for this recipe.  We are cheating here since we don't
  /// use the list of ingredients we created.
  /// </summary>
  private async Task GenerateStepsAsync(
    RecipeSummary recipe,
    string ingredients,
    string time,
    CancellationToken cancellation = default
  ) {
    var prompt = $"""
                 You are a writer for America's Test Kitchen
                 You are writing out the steps for the recipe: {recipe.Name}
                 Here is the description of the recipe: {recipe.Intro}
                 Our target prep time is {time} minutes
                 Write each step starting with a number like "1."
                 End each step with "⮑⮑"
                 Write your entire output as a single line
                 WRITE ONLY THE RECIPE STEPS DO NOT WRITE A PROLOGUE; JUST WRITE THE CONTENT
                 Here are the ingredients:

                 <INGREDIENTS>
                 {ingredients}
                 <END INGREDIENTS>
                 """;

    await ExecutePromptAsync(
      "ste",
      prompt,
      new () {
        MaxTokens = 400,
        Temperature = 0.25,
        TopP = 0
      },
      cancellation: cancellation
    );
  }

  /// <summary>
  /// Generate recommended side dishes
  /// </summary>
  private async Task GenerateSidesAsync(
    RecipeSummary recipe,
    CancellationToken cancellation = default
  ) {
    var prompt = $"""
                 You are a writer for America's Test Kitchen
                 I am making this recipe as my main dish: {recipe.Name}
                 Here is the description of the recipe: {recipe.Intro}
                 Write a list of only 3 suggested side dishes to go with this recipe
                 Separate each suggestion with a comma
                 Example: French Fries, Cole Slaw, Baked Beans
                 WRITE ONLY THE SIDE DISHES DO NOT WRITE A PROLOGUE; JUST WRITE THE CONTENT
                 """;

    await ExecutePromptAsync(
      "sde",
      prompt,
      new () {
        MaxTokens = 72,
        Temperature = 0.25,
        TopP = 0
      },
      cancellation: cancellation
    );
  }
}

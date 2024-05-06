/// <summary>
/// The model for the request payload for the API call.
/// </summary>
public record RecipeRequest(
  string IngredientsOnHand,
  string PrepTime
);

/// <summary>
/// The configuration model.
/// </summary>
public record RecipesConfig {
  public required string FireworksKey { get; set; }
  public required string GroqKey { get; set; }
}

/// <summary>
/// A recipe summary.
/// </summary>
/// <param name="Name">The name of the recipe.</param>
/// <param name="Intro">A short intro for the recipe.</param>
public record RecipeSummary {
  public string Name { get; set; } = "";
  public string Intro { get; set; } = "";
};

/// <summary>
/// A fragment of the content.
/// </summary>
/// <param name="Part">The part that the content belongs to.</param>
/// <param name="Content">The content to append</param>
public record Fragment (
  string Part,
  string Content
);

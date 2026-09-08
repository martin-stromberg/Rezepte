namespace Rezepte.Web.Services;

/// <summary>
/// Holds the fixed demo data set used to seed new accounts.
/// </summary>
public static class DemoDataSource
{
    /// <summary>
    /// The name of the demo cookbook whose recipes are used for the demo calendar events
    /// and the shopping list items.
    /// </summary>
    public const string CalendarCookbookName = "Abendessen";

    private static readonly IReadOnlyList<RecipeCreateStep> DefaultSteps =
        new List<RecipeCreateStep>
        {
            new(null, "Zutaten vorbereiten und nach Belieben anrichten.", 5, false, new List<RecipeCreateIngredient>())
        }.AsReadOnly();

    /// <summary>
    /// Gets the demo cookbooks.
    /// </summary>
    /// <returns>The demo cookbooks.</returns>
    public static IReadOnlyList<DemoCookbook> Cookbooks { get; } = BuildCookbooks();

    /// <summary>
    /// Builds the fixed list of demo cookbooks with their demo recipes.
    /// </summary>
    /// <returns>The demo cookbooks with their recipes.</returns>
    private static IReadOnlyList<DemoCookbook> BuildCookbooks()
    {
        var breakfast = new DemoRecipe[]
        {
            new("Haferbrei", null, 2, DefaultSteps),
            new("Rührei", null, 2, DefaultSteps),
            new("Pancakes", null, 4, DefaultSteps),
            new("Obstsalat", null, 2, DefaultSteps),
            new("Vollkornbrot", null, 6, DefaultSteps),
            new("Joghurt", null, 1, DefaultSteps),
            new("Müsliriegel", null, 8, DefaultSteps),
            new("Smoothie", null, 1, DefaultSteps)
        };

        var carbonaraSteps = new List<RecipeCreateStep>
        {
            new(null, "Spaghetti al dente kochen, Speck anbraten, Eier mit Parmesan verquirlen und alles vermengen.", 20, false,
                new List<RecipeCreateIngredient>
                {
                    new(250, "g", "Spaghetti"),
                    new(100, "g", "Speck"),
                    new(2, null, "Eier"),
                    new(50, "g", "Parmesan"),
                    new(1, "Prise", "Pfeffer")
                })
        }.AsReadOnly();

        var dinner = new DemoRecipe[]
        {
            new("Spaghetti Carbonara", "Klassische italienische Carbonara", 4, carbonaraSteps),
            new("Kartoffelgratin", null, 4, DefaultSteps),
            new("Hähnchencurry", null, 4, DefaultSteps),
            new("Gemüsepfanne", null, 2, DefaultSteps),
            new("Lachsfilet", null, 2, DefaultSteps),
            new("Pizza", null, 4, DefaultSteps),
            new("Burger", null, 4, DefaultSteps),
            new("Lasagne", null, 6, DefaultSteps),
            new("Wraps", null, 2, DefaultSteps),
            new("Eintopf", null, 6, DefaultSteps)
        };

        var snacks = new DemoRecipe[]
        {
            new("Gemüsesticks", null, 4, DefaultSteps),
            new("Nussmischung", null, 1, DefaultSteps),
            new("Käseplatte", null, 4, DefaultSteps),
            new("Hummus", null, 4, DefaultSteps),
            new("Obstkorb", null, 2, DefaultSteps),
            new("Crackers", null, 6, DefaultSteps),
            new("Popcorn", null, 4, DefaultSteps),
            new("Oliven", null, 2, DefaultSteps),
            new("Schokoriegel", null, 1, DefaultSteps)
        };

        var christmas = new DemoRecipe[]
        {
            new("Plätzchen", null, 30, DefaultSteps),
            new("Stollen", null, 12, DefaultSteps),
            new("Lebkuchen", null, 20, DefaultSteps),
            new("Punsch", null, 4, DefaultSteps),
            new("Bratäpfel", null, 4, DefaultSteps),
            new("Zimtsterne", null, 25, DefaultSteps),
            new("Spekulatius", null, 18, DefaultSteps),
            new("Marzipan", null, 10, DefaultSteps)
        };

        var grill = new DemoRecipe[]
        {
            new("Bratwurst", null, 4, DefaultSteps),
            new("Steak", null, 2, DefaultSteps),
            new("Maiskolben", null, 4, DefaultSteps),
            new("Schaschlik", null, 4, DefaultSteps),
            new("Grillgemüse", null, 4, DefaultSteps),
            new("Grill-Burger", null, 4, DefaultSteps),
            new("Lachs", null, 2, DefaultSteps),
            new("Kartoffelsalat", null, 8, DefaultSteps)
        };

        return new List<DemoCookbook>
        {
            new("Frühstück", breakfast),
            new(CalendarCookbookName, dinner),
            new("Snacks", snacks),
            new("Weihnachtszeit", christmas),
            new("Grillsaison", grill)
        }.AsReadOnly();
    }
}

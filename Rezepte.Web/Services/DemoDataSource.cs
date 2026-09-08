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
            new("Haferbrei", null, 2, "Frühstück", DefaultSteps),
            new("Rührei", null, 2, "Frühstück", DefaultSteps),
            new("Pancakes", null, 4, "Frühstück", DefaultSteps),
            new("Obstsalat", null, 2, "Frühstück", DefaultSteps),
            new("Vollkornbrot", null, 6, "Frühstück", DefaultSteps),
            new("Joghurt", null, 1, "Frühstück", DefaultSteps),
            new("Müsliriegel", null, 8, "Frühstück", DefaultSteps),
            new("Smoothie", null, 1, "Frühstück", DefaultSteps)
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
            new("Spaghetti Carbonara", "Klassische italienische Carbonara", 4, CalendarCookbookName, carbonaraSteps),
            new("Kartoffelgratin", null, 4, CalendarCookbookName, DefaultSteps),
            new("Hähnchencurry", null, 4, CalendarCookbookName, DefaultSteps),
            new("Gemüsepfanne", null, 2, CalendarCookbookName, DefaultSteps),
            new("Lachsfilet", null, 2, CalendarCookbookName, DefaultSteps),
            new("Pizza", null, 4, CalendarCookbookName, DefaultSteps),
            new("Burger", null, 4, CalendarCookbookName, DefaultSteps),
            new("Lasagne", null, 6, CalendarCookbookName, DefaultSteps),
            new("Wraps", null, 2, CalendarCookbookName, DefaultSteps),
            new("Eintopf", null, 6, CalendarCookbookName, DefaultSteps)
        };

        var snacks = new DemoRecipe[]
        {
            new("Gemüsesticks", null, 4, "Snacks", DefaultSteps),
            new("Nussmischung", null, 1, "Snacks", DefaultSteps),
            new("Käseplatte", null, 4, "Snacks", DefaultSteps),
            new("Hummus", null, 4, "Snacks", DefaultSteps),
            new("Obstkorb", null, 2, "Snacks", DefaultSteps),
            new("Crackers", null, 6, "Snacks", DefaultSteps),
            new("Popcorn", null, 4, "Snacks", DefaultSteps),
            new("Oliven", null, 2, "Snacks", DefaultSteps),
            new("Schokoriegel", null, 1, "Snacks", DefaultSteps)
        };

        var christmas = new DemoRecipe[]
        {
            new("Plätzchen", null, 30, "Weihnachtszeit", DefaultSteps),
            new("Stollen", null, 12, "Weihnachtszeit", DefaultSteps),
            new("Lebkuchen", null, 20, "Weihnachtszeit", DefaultSteps),
            new("Punsch", null, 4, "Weihnachtszeit", DefaultSteps),
            new("Bratäpfel", null, 4, "Weihnachtszeit", DefaultSteps),
            new("Zimtsterne", null, 25, "Weihnachtszeit", DefaultSteps),
            new("Spekulatius", null, 18, "Weihnachtszeit", DefaultSteps),
            new("Marzipan", null, 10, "Weihnachtszeit", DefaultSteps)
        };

        var grill = new DemoRecipe[]
        {
            new("Bratwurst", null, 4, "Grillsaison", DefaultSteps),
            new("Steak", null, 2, "Grillsaison", DefaultSteps),
            new("Maiskolben", null, 4, "Grillsaison", DefaultSteps),
            new("Schaschlik", null, 4, "Grillsaison", DefaultSteps),
            new("Grillgemüse", null, 4, "Grillsaison", DefaultSteps),
            new("Grill-Burger", null, 4, "Grillsaison", DefaultSteps),
            new("Lachs", null, 2, "Grillsaison", DefaultSteps),
            new("Kartoffelsalat", null, 8, "Grillsaison", DefaultSteps)
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

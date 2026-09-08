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

    /// <summary>
    /// Gets the demo cookbooks.
    /// </summary>
    /// <returns>The demo cookbooks.</returns>
    public static IReadOnlyList<DemoCookbook> Cookbooks { get; } = BuildCookbooks();

    /// <summary>
    /// Creates a single recipe ingredient.
    /// </summary>
    /// <param name="amount">The amount of the ingredient.</param>
    /// <param name="unit">The unit of the amount, or null for pieces.</param>
    /// <param name="name">The ingredient name.</param>
    /// <returns>The recipe ingredient.</returns>
    private static RecipeCreateIngredient Ing(decimal amount, string? unit, string name)
        => new(amount, unit, name);

    /// <summary>
    /// Creates a single preparation step with its ingredients.
    /// </summary>
    /// <param name="description">The step description.</param>
    /// <param name="durationMinutes">The duration of the step in minutes.</param>
    /// <param name="ingredients">The ingredients used in this step.</param>
    /// <returns>The recipe step.</returns>
    private static RecipeCreateStep Step(string description, int durationMinutes, params RecipeCreateIngredient[] ingredients)
        => new(null, description, durationMinutes, false, ingredients);

    /// <summary>
    /// Builds the fixed list of demo cookbooks with their demo recipes.
    /// </summary>
    /// <returns>The demo cookbooks with their recipes.</returns>
    private static IReadOnlyList<DemoCookbook> BuildCookbooks()
    {
        var breakfast = new DemoRecipe[]
        {
            new("Haferbrei mit Beeren", "Cremiger Porridge mit frischen Beeren und Honig.", 2,
                new[]
                {
                    Step("Haferflocken mit Milch und einer Prise Salz aufkochen und bei kleiner Hitze quellen lassen.", 10,
                        Ing(100, "g", "Haferflocken"),
                        Ing(400, "ml", "Milch"),
                        Ing(1, "Prise", "Salz")),
                    Step("Beeren waschen, mit dem Porridge anrichten und mit Honig beträufeln.", 5,
                        Ing(150, "g", "Beerenmischung"),
                        Ing(1, "EL", "Honig"),
                        Ing(1, "EL", "gehackte Mandeln"))
                }),
            new("Rührei auf Vollkornbrot", "Lockeres Rührei mit Schnittlauch auf getoastetem Vollkornbrot.", 2,
                new[]
                {
                    Step("Eier mit Milch, Salz und Pfeffer verquirlen.", 3,
                        Ing(4, null, "Eier"),
                        Ing(2, "EL", "Milch"),
                        Ing(1, "Prise", "Salz"),
                        Ing(1, "Prise", "Pfeffer")),
                    Step("Butter in der Pfanne schmelzen, Eiermasse bei kleiner Hitze stocken lassen und auf das getoastete Brot geben. Mit Schnittlauch bestreuen.", 7,
                        Ing(1, "EL", "Butter"),
                        Ing(2, "Scheiben", "Vollkornbrot"),
                        Ing(1, "EL", "Schnittlauch"))
                }),
            new("Pancakes mit Ahornsirup", "Fluffige amerikanische Pancakes mit Ahornsirup.", 4,
                new[]
                {
                    Step("Mehl, Backpulver, Zucker und Salz mischen. Milch, Ei und geschmolzene Butter unterrühren.", 10,
                        Ing(200, "g", "Mehl"),
                        Ing(2, "TL", "Backpulver"),
                        Ing(2, "EL", "Zucker"),
                        Ing(1, "Prise", "Salz"),
                        Ing(250, "ml", "Milch"),
                        Ing(1, null, "Ei"),
                        Ing(30, "g", "Butter")),
                    Step("Kleine Portionen Teig in einer Pfanne goldbraun backen und mit Ahornsirup servieren.", 15,
                        Ing(50, "ml", "Ahornsirup"),
                        Ing(1, "EL", "Öl"))
                }),
            new("Obstsalat", "Frischer Obstsalat mit Minze und Orangensaft.", 2,
                new[]
                {
                    Step("Obst waschen, schälen, in Stücke schneiden und mit Orangensaft und Minze vermengen.", 15,
                        Ing(1, null, "Apfel"),
                        Ing(1, null, "Banane"),
                        Ing(200, "g", "Weintrauben"),
                        Ing(150, "g", "Erdbeeren"),
                        Ing(2, "EL", "Orangensaft"),
                        Ing(4, "Blätter", "Minze"))
                }),
            new("Schnelles Vollkornbrot", "Einfaches Vollkornbrot mit Körnern für den Sonntagsbrunch.", 6,
                new[]
                {
                    Step("Alle Zutaten zu einem Teig verkneten und 30 Minuten ruhen lassen.", 15,
                        Ing(500, "g", "Vollkornmehl"),
                        Ing(1, "Würfel", "frische Hefe"),
                        Ing(300, "ml", "lauwarmes Wasser"),
                        Ing(1, "TL", "Salz"),
                        Ing(2, "EL", "Sonnenblumenkerne"),
                        Ing(2, "EL", "Leinsamen")),
                    Step("Teig in eine Kastenform füllen und bei 200 Grad ca. 50 Minuten backen.", 50,
                        Ing(1, "EL", "Haferflocken zum Bestreuen"))
                }),
            new("Joghurt-Bowl mit Granola", "Joghurt mit knusprigem Granola und Obst.", 1,
                new[]
                {
                    Step("Joghurt in eine Schüssel geben, mit Granola, Obst und Honig toppen.", 5,
                        Ing(250, "g", "Naturjoghurt"),
                        Ing(50, "g", "Granola"),
                        Ing(100, "g", "Heidelbeeren"),
                        Ing(1, "TL", "Honig"))
                }),
            new("Müsliriegel", "Hausgemachte Müsliriegel mit Nüssen und Trockenfrüchten.", 8,
                new[]
                {
                    Step("Butter, Honig und Zucker schmelzen. Haferflocken, Nüsse und Trockenfrüchte unterrühren.", 10,
                        Ing(100, "g", "Butter"),
                        Ing(100, "g", "Honig"),
                        Ing(50, "g", "brauner Zucker"),
                        Ing(200, "g", "Haferflocken"),
                        Ing(80, "g", "gehackte Nüsse"),
                        Ing(80, "g", "gehackte Aprikosen")),
                    Step("Masse in eine Form drücken und bei 180 Grad ca. 25 Minuten backen. Ausgekühlt in Riegel schneiden.", 25,
                        Ing(1, "Prise", "Zimt"))
                }),
            new("Grüner Smoothie", "Fruchtiger Smoothie mit Spinat, Banane und Ingwer.", 1,
                new[]
                {
                    Step("Alle Zutaten mit Wasser im Mixer pürieren, bis der Smoothie cremig ist.", 5,
                        Ing(1, null, "Banane"),
                        Ing(1, null, "Apfel"),
                        Ing(50, "g", "Blattspinat"),
                        Ing(1, "Stück", "Ingwer"),
                        Ing(200, "ml", "Wasser"),
                        Ing(1, "EL", "Zitronensaft"))
                })
        };

        var dinner = new DemoRecipe[]
        {
            new("Spaghetti Carbonara", "Klassische italienische Carbonara ohne Sahne.", 4,
                new[]
                {
                    Step("Spaghetti in Salzwasser al dente kochen. Speck in einer Pfanne knusprig braten.", 15,
                        Ing(400, "g", "Spaghetti"),
                        Ing(150, "g", "gewürfelter Speck"),
                        Ing(1, "EL", "Salz")),
                    Step("Eier mit Parmesan und Pfeffer verquirlen. Spaghetti mit Speck mischen, Pfanne vom Herd nehmen und die Eimischung unterrühren, bis sie cremig bindet.", 5,
                        Ing(3, null, "Eier"),
                        Ing(80, "g", "geriebener Parmesan"),
                        Ing(1, "TL", "schwarzer Pfeffer"))
                }),
            new("Kartoffelgratin", "Cremiges Kartoffelgratin mit Sahne und Käse.", 4,
                new[]
                {
                    Step("Kartoffeln in dünne Scheiben schneiden. Sahne mit Knoblauch, Salz, Pfeffer und Muskat würzen.", 15,
                        Ing(1, "kg", "festkochende Kartoffeln"),
                        Ing(400, "ml", "Sahne"),
                        Ing(2, "Zehen", "Knoblauch"),
                        Ing(1, "TL", "Salz"),
                        Ing(1, "Prise", "Muskatnuss")),
                    Step("Kartoffeln fächerförmig in eine Auflaufform schichten, mit Sahne übergießen und mit Käse bestreuen. Bei 180 Grad ca. 60 Minuten backen.", 60,
                        Ing(150, "g", "geriebener Gruyère"),
                        Ing(1, "EL", "Butter"))
                }),
            new("Hähnchencurry", "Mildes Kokos-Curry mit Hähnchen und Reis.", 4,
                new[]
                {
                    Step("Hähnchen würfeln und anbraten. Zwiebel, Knoblauch und Ingwer dazugeben und kurz mitdünsten.", 10,
                        Ing(500, "g", "Hähnchenbrust"),
                        Ing(1, null, "Zwiebel"),
                        Ing(2, "Zehen", "Knoblauch"),
                        Ing(1, "Stück", "Ingwer"),
                        Ing(2, "EL", "Öl")),
                    Step("Currypaste anrösten, mit Kokosmilch ablöschen und 15 Minuten köcheln lassen. Mit Reis servieren.", 20,
                        Ing(2, "EL", "rote Currypaste"),
                        Ing(400, "ml", "Kokosmilch"),
                        Ing(1, null, "rote Paprika"),
                        Ing(250, "g", "Basmatireis"),
                        Ing(2, "EL", "Sojasauce"),
                        Ing(0.5m, null, "Limette"))
                }),
            new("Gemüsepfanne mit Reisnudeln", "Schnelle asiatische Gemüsepfanne mit knusprigem Tofu.", 2,
                new[]
                {
                    Step("Tofu würfeln und knusprig anbraten. Gemüse in Streifen schneiden und kurz mitbraten.", 10,
                        Ing(200, "g", "Räuchertofu"),
                        Ing(1, null, "Zucchini"),
                        Ing(1, null, "Paprika"),
                        Ing(1, null, "Karotte"),
                        Ing(100, "g", "Champignons"),
                        Ing(2, "EL", "Erdnussöl")),
                    Step("Mit Sojasauce, Sesamöl und Reisnudeln vermengen und mit Sesam bestreuen.", 5,
                        Ing(150, "g", "Reisnudeln"),
                        Ing(3, "EL", "Sojasauce"),
                        Ing(1, "TL", "Sesamöl"),
                        Ing(1, "EL", "Sesam"))
                }),
            new("Lachsfilet auf Ofengemüse", "Lachsfilet auf buntem Ofengemüse mit Zitrone.", 2,
                new[]
                {
                    Step("Gemüse würfeln, mit Olivenöl, Salz und Rosmarin mischen und auf ein Backblech geben.", 10,
                        Ing(1, null, "Zucchini"),
                        Ing(2, null, "Karotten"),
                        Ing(1, null, "rote Zwiebel"),
                        Ing(2, "EL", "Olivenöl"),
                        Ing(1, "Zweig", "Rosmarin")),
                    Step("Lachsfilets auf das Gemüse legen, mit Zitrone beträufeln und bei 200 Grad ca. 20 Minuten garen.", 20,
                        Ing(2, null, "Lachsfilets"),
                        Ing(0.5m, null, "Zitrone"),
                        Ing(1, "TL", "Salz"),
                        Ing(1, "Prise", "Pfeffer"))
                }),
            new("Pizza Margherita", "Knusprige Pizza mit Tomaten, Mozzarella und Basilikum.", 4,
                new[]
                {
                    Step("Mehl, Hefe, Wasser, Salz und Öl zu einem Teig kneten und 60 Minuten gehen lassen.", 15,
                        Ing(500, "g", "Mehl"),
                        Ing(1, "Päckchen", "Trockenhefe"),
                        Ing(300, "ml", "lauwarmes Wasser"),
                        Ing(1, "TL", "Salz"),
                        Ing(2, "EL", "Olivenöl")),
                    Step("Teig ausrollen, mit Tomatensauce bestreichen, mit Mozzarella belegen und bei 250 Grad ca. 12 Minuten backen. Mit Basilikum servieren.", 15,
                        Ing(200, "g", "passierte Tomaten"),
                        Ing(250, "g", "Mozzarella"),
                        Ing(1, "Bund", "Basilikum"),
                        Ing(1, "TL", "Oregano"))
                }),
            new("Burger mit Kartoffelspalten", "Saftige Rindfleisch-Burger mit selbstgemachten Wedges.", 4,
                new[]
                {
                    Step("Kartoffeln in Spalten schneiden, mit Öl und Gewürzen mischen und bei 220 Grad ca. 30 Minuten backen.", 35,
                        Ing(800, "g", "Kartoffeln"),
                        Ing(2, "EL", "Olivenöl"),
                        Ing(1, "TL", "Paprikapulver"),
                        Ing(1, "TL", "Salz")),
                    Step("Patties formen und braten. Buns toasten und mit Salat, Tomate, Käse und Sauce belegen.", 15,
                        Ing(500, "g", "Rinderhack"),
                        Ing(4, null, "Burgerbrötchen"),
                        Ing(4, "Scheiben", "Cheddar"),
                        Ing(1, null, "Tomate"),
                        Ing(4, "Blätter", "Eisbergsalat"),
                        Ing(4, "EL", "Burgersauce"))
                }),
            new("Lasagne Bolognese", "Klassische Lasagne mit Ragu und Béchamel.", 6,
                new[]
                {
                    Step("Hackfleisch anbraten, Zwiebel und Knoblauch dazugeben. Mit Tomaten ablöschen und 30 Minuten köcheln lassen.", 40,
                        Ing(500, "g", "Rinderhack"),
                        Ing(1, null, "Zwiebel"),
                        Ing(2, "Zehen", "Knoblauch"),
                        Ing(800, "g", "stückige Tomaten"),
                        Ing(1, "EL", "Tomatenmark"),
                        Ing(1, "TL", "Oregano")),
                    Step("Béchamel aus Butter, Mehl und Milch kochen. Saucen und Platten schichten, mit Käse abschließen und bei 180 Grad ca. 40 Minuten backen.", 45,
                        Ing(50, "g", "Butter"),
                        Ing(40, "g", "Mehl"),
                        Ing(500, "ml", "Milch"),
                        Ing(12, null, "Lasagneplatten"),
                        Ing(150, "g", "geriebener Parmesan"),
                        Ing(1, "Prise", "Muskatnuss"))
                }),
            new("Wraps mit Hähnchen", "Gefüllte Wraps mit Hähnchen, Salat und Joghurt-Dip.", 2,
                new[]
                {
                    Step("Hähnchen in Streifen schneiden, würzen und anbraten.", 10,
                        Ing(250, "g", "Hähnchenbrust"),
                        Ing(1, "TL", "Paprikapulver"),
                        Ing(1, "TL", "Salz"),
                        Ing(1, "EL", "Öl")),
                    Step("Wraps mit Joghurt-Dip, Salat, Gemüse und Hähnchen füllen und einrollen.", 10,
                        Ing(2, null, "Tortilla-Wraps"),
                        Ing(100, "g", "Naturjoghurt"),
                        Ing(1, "Zehe", "Knoblauch"),
                        Ing(2, "Blätter", "Eisbergsalat"),
                        Ing(1, null, "Tomate"),
                        Ing(0.5m, null, "Gurke"))
                }),
            new("Deftiger Eintopf", "Würziger Eintopf mit Kartoffeln, Möhren und Würstchen.", 6,
                new[]
                {
                    Step("Gemüse schälen und würfeln. Zwiebeln glasig dünsten, Gemüse kurz mitbraten.", 15,
                        Ing(1, "kg", "Kartoffeln"),
                        Ing(4, null, "Möhren"),
                        Ing(1, "Stange", "Porree"),
                        Ing(2, null, "Zwiebeln"),
                        Ing(2, "EL", "Öl")),
                    Step("Mit Brühe auffüllen, würzen und 30 Minuten köcheln. Würstchen in Scheiben dazugeben und heiß servieren.", 35,
                        Ing(1.5m, "l", "Gemüsebrühe"),
                        Ing(4, null, "Wiener Würstchen"),
                        Ing(1, "TL", "Majoran"),
                        Ing(1, "TL", "Salz"),
                        Ing(1, "Prise", "Muskatnuss"))
                })
        };

        var snacks = new DemoRecipe[]
        {
            new("Gemüsesticks mit Kräuterquark", "Frische Gemüsesticks mit würzigem Kräuterquark.", 4,
                new[]
                {
                    Step("Gemüse waschen und in Sticks schneiden. Quark mit Kräutern, Zitronensaft, Salz und Pfeffer verrühren.", 15,
                        Ing(1, null, "Gurke"),
                        Ing(3, null, "Möhren"),
                        Ing(1, null, "Paprika"),
                        Ing(250, "g", "Magerquark"),
                        Ing(2, "EL", "Schnittlauch"),
                        Ing(1, "EL", "Zitronensaft"),
                        Ing(1, "Prise", "Salz"))
                }),
            new("Geröstete Nussmischung", "Würzig geröstete Nüsse aus dem Ofen.", 1,
                new[]
                {
                    Step("Nüsse mit Öl, Salz und Rosmarin mischen und bei 180 Grad ca. 10 Minuten rösten.", 12,
                        Ing(200, "g", "gemischte Nüsse"),
                        Ing(1, "EL", "Olivenöl"),
                        Ing(1, "TL", "grobes Salz"),
                        Ing(1, "TL", "Rosmarin"))
                }),
            new("Käseplatte", "Käseplatte mit Trauben, Nüssen und Brot.", 4,
                new[]
                {
                    Step("Käse in Scheiben schneiden, mit Trauben, Nüssen und Brot anrichten.", 10,
                        Ing(100, "g", "Bergkäse"),
                        Ing(100, "g", "Camembert"),
                        Ing(100, "g", "Gouda"),
                        Ing(150, "g", "Weintrauben"),
                        Ing(50, "g", "Walnüsse"),
                        Ing(1, null, "Baguette"))
                }),
            new("Hummus", "Cremiger Kichererbsen-Dip mit Tahini und Zitrone.", 4,
                new[]
                {
                    Step("Alle Zutaten pürieren, bis der Hummus cremig ist. Mit Olivenöl und Paprikapulver servieren.", 10,
                        Ing(400, "g", "Kichererbsen (Abtropfgewicht)"),
                        Ing(3, "EL", "Tahini"),
                        Ing(2, "EL", "Zitronensaft"),
                        Ing(2, "Zehen", "Knoblauch"),
                        Ing(3, "EL", "Olivenöl"),
                        Ing(1, "TL", "Kreuzkümmel"),
                        Ing(1, "TL", "Salz"))
                }),
            new("Obstkorb mit Joghurt-Dip", "Bunter Obstkorb mit leichtem Honig-Joghurt-Dip.", 2,
                new[]
                {
                    Step("Obst waschen und mundgerecht schneiden. Joghurt mit Honig und Vanille verrühren.", 10,
                        Ing(1, null, "Apfel"),
                        Ing(1, null, "Birne"),
                        Ing(150, "g", "Erdbeeren"),
                        Ing(150, "g", "Trauben"),
                        Ing(150, "g", "Joghurt"),
                        Ing(1, "TL", "Honig"),
                        Ing(1, "Prise", "Vanillezucker"))
                }),
            new("Rosmarin-Cracker", "Knusprige hausgemachte Cracker mit Rosmarin und Meersalz.", 6,
                new[]
                {
                    Step("Mehl, Salz, Rosmarin, Öl und Wasser zu einem Teig verkneten, dünn ausrollen und in Stücke schneiden.", 15,
                        Ing(200, "g", "Mehl"),
                        Ing(1, "TL", "Salz"),
                        Ing(1, "EL", "Rosmarin"),
                        Ing(3, "EL", "Olivenöl"),
                        Ing(100, "ml", "Wasser")),
                    Step("Cracker bei 200 Grad ca. 15 Minuten goldbraun backen und mit Meersalz bestreuen.", 15,
                        Ing(1, "TL", "Meersalz"))
                }),
            new("Popcorn", "Selbstgemachtes Popcorn aus dem Topf — süß oder salzig.", 4,
                new[]
                {
                    Step("Öl im Topf erhitzen, Mais zugeben, Deckel schließen und bei mittlerer Hitze schütteln, bis das Popen nachlässt.", 10,
                        Ing(100, "g", "Popcornmais"),
                        Ing(2, "EL", "Öl")),
                    Step("Nach Belieben mit Zucker oder Salz würzen.", 2,
                        Ing(2, "EL", "Zucker oder Salz"))
                }),
            new("Marinierte Oliven", "Oliven, mariniert mit Knoblauch, Zitrone und Kräutern.", 2,
                new[]
                {
                    Step("Oliven mit Knoblauch, Zitronenschale, Kräutern und Olivenöl vermengen und mindestens 30 Minuten ziehen lassen.", 10,
                        Ing(200, "g", "Oliven"),
                        Ing(1, "Zehe", "Knoblauch"),
                        Ing(1, "TL", "Zitronenschale"),
                        Ing(1, "TL", "Thymian"),
                        Ing(2, "EL", "Olivenöl"))
                }),
            new("Energieriegel", "Rohkost-Riegel aus Datteln, Nüssen und Kakao.", 1,
                new[]
                {
                    Step("Datteln, Nüsse, Kakao und eine Prise Salz im Mixer zu einer klebrigen Masse verarbeiten.", 10,
                        Ing(200, "g", "Datteln"),
                        Ing(100, "g", "Mandeln"),
                        Ing(2, "EL", "Kakaopulver"),
                        Ing(1, "Prise", "Salz")),
                    Step("Masse in eine Form drücken, kühlen und in Riegel schneiden.", 15,
                        Ing(20, "g", "gehackte Pistazien"))
                })
        };

        var christmas = new DemoRecipe[]
        {
            new("Butterplätzchen", "Klassische Ausstechplätzchen für die Adventszeit.", 30,
                new[]
                {
                    Step("Butter, Zucker, Ei, Vanille und Mehl zu einem glatten Teig verkneten und 30 Minuten kühlen.", 20,
                        Ing(250, "g", "Butter"),
                        Ing(125, "g", "Zucker"),
                        Ing(1, null, "Ei"),
                        Ing(1, "Päckchen", "Vanillezucker"),
                        Ing(375, "g", "Mehl")),
                    Step("Teig ausrollen, Plätzchen ausstechen und bei 180 Grad ca. 10 Minuten hellgelb backen.", 15,
                        Ing(100, "g", "Puderzucker zum Dekorieren"))
                }),
            new("Christstollen", "Saftiger Stollen mit Rosinen, Marzipan und Rum.", 12,
                new[]
                {
                    Step("Rosinen in Rum einweichen. Mehl, Hefe, Milch, Butter, Zucker und Gewürze zu einem Hefeteig verkneten.", 30,
                        Ing(200, "g", "Rosinen"),
                        Ing(50, "ml", "Rum"),
                        Ing(500, "g", "Mehl"),
                        Ing(1, "Würfel", "Hefe"),
                        Ing(250, "ml", "lauwarme Milch"),
                        Ing(150, "g", "Butter"),
                        Ing(100, "g", "Zucker"),
                        Ing(1, "TL", "Kardamom")),
                    Step("Rosinen, Mandeln, Zitronat und Marzipan unterkneten, Stollen formen und bei 170 Grad ca. 60 Minuten backen. Warm mit Butter bestreichen und puderzuckern.", 60,
                        Ing(100, "g", "Mandeln"),
                        Ing(80, "g", "Zitronat"),
                        Ing(200, "g", "Marzipanrohmasse"),
                        Ing(50, "g", "Butter zum Bestreichen"),
                        Ing(50, "g", "Puderzucker"))
                }),
            new("Lebkuchen", "Weiche Lebkuchen mit Lebkuchengewürz und Zuckerguss.", 20,
                new[]
                {
                    Step("Honig und Zucker erwärmen, auskühlen lassen und mit den übrigen Zutaten zu einem Teig verarbeiten. 2 Stunden ruhen lassen.", 20,
                        Ing(250, "g", "Honig"),
                        Ing(125, "g", "Zucker"),
                        Ing(500, "g", "Mehl"),
                        Ing(1, null, "Ei"),
                        Ing(2, "EL", "Lebkuchengewürz"),
                        Ing(1, "TL", "Backpulver"),
                        Ing(100, "g", "gehackte Mandeln")),
                    Step("Teig auf Oblaten setzen, bei 180 Grad ca. 15 Minuten backen und mit Zuckerguss überziehen.", 20,
                        Ing(1, "Packung", "Oblaten"),
                        Ing(100, "g", "Puderzucker"),
                        Ing(2, "EL", "Zitronensaft"))
                }),
            new("Kinderpunsch", "Warmer Früchtepunsch ohne Alkohol für die ganze Familie.", 4,
                new[]
                {
                    Step("Alle Säfte mit Gewürzen aufkochen und 10 Minuten ziehen lassen. Heiß servieren.", 15,
                        Ing(1, "l", "Traubensaft"),
                        Ing(500, "ml", "Apfelsaft"),
                        Ing(2, null, "Zimtstangen"),
                        Ing(4, null, "Nelken"),
                        Ing(1, null, "Orange in Scheiben"),
                        Ing(2, "EL", "Honig"))
                }),
            new("Bratäpfel", "Ofengebackene Äpfel mit Marzipan-Nuss-Füllung.", 4,
                new[]
                {
                    Step("Äpfel entkernen. Marzipan, Nüsse, Rosinen und Zimt vermengen und die Äpfel damit füllen.", 10,
                        Ing(4, null, "säuerliche Äpfel"),
                        Ing(100, "g", "Marzipanrohmasse"),
                        Ing(50, "g", "gehackte Walnüsse"),
                        Ing(30, "g", "Rosinen"),
                        Ing(1, "TL", "Zimt")),
                    Step("Äpfel in eine Form setzen, mit Honig beträufeln und bei 180 Grad ca. 25 Minuten backen.", 25,
                        Ing(2, "EL", "Honig"),
                        Ing(4, "Stück", "Butterflöckchen"))
                }),
            new("Zimtsterne", "Klassische Zimtsterne mit Eiweißglasur.", 25,
                new[]
                {
                    Step("Mandeln, Puderzucker, Zimt und Eiweiß zu einem Teig verkneten und 30 Minuten kühlen.", 15,
                        Ing(400, "g", "gemahlene Mandeln"),
                        Ing(300, "g", "Puderzucker"),
                        Ing(2, "EL", "Zimt"),
                        Ing(3, null, "Eiweiß")),
                    Step("Teig ausrollen, Sterne ausstechen, mit Glasur bestreichen und bei 150 Grad ca. 15 Minuten trocknen/backen.", 20,
                        Ing(1, null, "Eiweiß für die Glasur"),
                        Ing(150, "g", "Puderzucker für die Glasur"))
                }),
            new("Spekulatius", "Würzige Spekulatius-Kekse mit typischem Gewürz.", 18,
                new[]
                {
                    Step("Butter und Zucker cremig rühren, Ei und Gewürze unterrühren, Mehl unterkneten und den Teig 60 Minuten kühlen.", 20,
                        Ing(150, "g", "Butter"),
                        Ing(150, "g", "brauner Zucker"),
                        Ing(1, null, "Ei"),
                        Ing(2, "EL", "Spekulatiusgewürz"),
                        Ing(250, "g", "Mehl"),
                        Ing(1, "Prise", "Salz")),
                    Step("Teig ausrollen, Figuren ausstechen und bei 180 Grad ca. 12 Minuten backen.", 15,
                        Ing(50, "g", "Mandelblättchen"))
                }),
            new("Marzipankartoffeln", "Marzipan-Konfekt in Kakao-Zimt gewälzt.", 10,
                new[]
                {
                    Step("Marzipan mit Puderzucker und Rosenwasser verkneten, Kugeln formen und in Kakao-Zimt wälzen.", 20,
                        Ing(200, "g", "Marzipanrohmasse"),
                        Ing(80, "g", "Puderzucker"),
                        Ing(1, "TL", "Rosenwasser oder Rum"),
                        Ing(2, "EL", "Kakaopulver"),
                        Ing(1, "TL", "Zimt"))
                })
        };

        var grill = new DemoRecipe[]
        {
            new("Bratwurst vom Grill", "Knusprig gegrillte Bratwürste mit Senf und Brot.", 4,
                new[]
                {
                    Step("Grill auf mittlere Hitze vorheizen. Bratwürste mehrmals wendend ca. 12 Minuten goldbraun grillen.", 15,
                        Ing(4, null, "Bratwürste"),
                        Ing(4, null, "Brötchen"),
                        Ing(4, "EL", "Senf"))
                }),
            new("Ribeye-Steak", "Perfekt gegrilltes Steak mit Kräuterbutter.", 2,
                new[]
                {
                    Step("Steaks 30 Minuten vor dem Grillen aus dem Kühlschrank nehmen, salzen und pfeffern. Bei starker Hitze je Seite 3–4 Minuten grillen, danach 5 Minuten ruhen lassen.", 15,
                        Ing(2, null, "Ribeye-Steaks"),
                        Ing(1, "TL", "grobes Salz"),
                        Ing(1, "TL", "schwarzer Pfeffer")),
                    Step("Butter mit Kräutern und Knoblauch verrühren und auf dem heißen Steak schmelzen lassen.", 5,
                        Ing(50, "g", "Butter"),
                        Ing(1, "EL", "Petersilie"),
                        Ing(1, "Zehe", "Knoblauch"))
                }),
            new("Gegrillte Maiskolben", "Maiskolben mit Chili-Limetten-Butter vom Grill.", 4,
                new[]
                {
                    Step("Butter mit Chili, Limettenschale und Salz verrühren.", 5,
                        Ing(80, "g", "weiche Butter"),
                        Ing(1, "TL", "Chiliflocken"),
                        Ing(1, "TL", "Limettenschale"),
                        Ing(0.5m, "TL", "Salz")),
                    Step("Maiskolben ca. 15 Minuten grillen, dabei wenden, und heiß mit der Butter bestreichen.", 15,
                        Ing(4, null, "Maiskolben"))
                }),
            new("Schaschlik-Spieße", "Bunte Fleisch-Gemüse-Spieße in Paprika-Marinade.", 4,
                new[]
                {
                    Step("Fleisch und Gemüse würfeln, mit Öl, Paprika und Knoblauch marinieren und auf Spieße stecken.", 20,
                        Ing(600, "g", "Schweinefilet"),
                        Ing(2, null, "Paprika"),
                        Ing(2, null, "Zwiebeln"),
                        Ing(3, "EL", "Öl"),
                        Ing(2, "EL", "Paprikapulver"),
                        Ing(2, "Zehen", "Knoblauch")),
                    Step("Spieße ca. 12 Minuten grillen, dabei mehrmals wenden.", 12,
                        Ing(8, null, "Holzspieße"))
                }),
            new("Grillgemüse in Folie", "Mediterranes Grillgemüse in der Alufolie.", 4,
                new[]
                {
                    Step("Gemüse in Scheiben schneiden, mit Öl, Knoblauch und Kräutern mischen und in Folienpäckchen einschlagen.", 15,
                        Ing(1, null, "Zucchini"),
                        Ing(1, null, "Aubergine"),
                        Ing(2, null, "Paprika"),
                        Ing(200, "g", "Kirschtomaten"),
                        Ing(3, "EL", "Olivenöl"),
                        Ing(2, "Zehen", "Knoblauch"),
                        Ing(1, "EL", "Thymian")),
                    Step("Päckchen ca. 20 Minuten auf dem Grill garen. Mit Feta bestreut servieren.", 20,
                        Ing(100, "g", "Feta"))
                }),
            new("Grill-Burger", "Saftige Burger-Patties direkt vom Grillrost.", 4,
                new[]
                {
                    Step("Hackfleisch würzen, Patties formen und je Seite ca. 4 Minuten grillen. In der letzten Minute mit Käse belegen.", 15,
                        Ing(600, "g", "Rinderhack"),
                        Ing(1, "TL", "Salz"),
                        Ing(1, "TL", "Pfeffer"),
                        Ing(1, "TL", "Senf"),
                        Ing(4, "Scheiben", "Cheddar")),
                    Step("Buns kurz anrösten und Burger mit Salat, Tomate und Sauce zusammensetzen.", 5,
                        Ing(4, null, "Burgerbrötchen"),
                        Ing(4, "Blätter", "Salat"),
                        Ing(1, null, "Tomate"),
                        Ing(4, "EL", "Burgersauce"))
                }),
            new("Gegrillter Lachs", "Lachsfilet auf der Zedernholzplanke oder in Folie.", 2,
                new[]
                {
                    Step("Lachs mit Öl, Honig, Senf und Dill marinieren und 20 Minuten ziehen lassen.", 10,
                        Ing(2, null, "Lachsfilets"),
                        Ing(1, "EL", "Olivenöl"),
                        Ing(1, "EL", "Honig"),
                        Ing(1, "TL", "Senf"),
                        Ing(1, "EL", "Dill")),
                    Step("Lachs bei indirekter Hitze ca. 12 Minuten garen und mit Zitrone servieren.", 12,
                        Ing(0.5m, null, "Zitrone"),
                        Ing(1, "Prise", "Salz"))
                }),
            new("Kartoffelsalat für den Grillabend", "Lauwarmer Kartoffelsalat mit Speck und Schnittlauch.", 8,
                new[]
                {
                    Step("Kartoffeln in Salzwasser garkochen, pellen und in Scheiben schneiden.", 25,
                        Ing(1, "kg", "festkochende Kartoffeln"),
                        Ing(1, "TL", "Salz")),
                    Step("Speck und Zwiebeln anbraten, mit Brühe, Essig und Senf ablöschen und über die Kartoffeln geben. Ziehen lassen und mit Schnittlauch bestreuen.", 15,
                        Ing(150, "g", "durchwachsener Speck"),
                        Ing(1, null, "Zwiebel"),
                        Ing(200, "ml", "Gemüsebrühe"),
                        Ing(3, "EL", "Essig"),
                        Ing(1, "EL", "Senf"),
                        Ing(1, "Bund", "Schnittlauch"))
                })
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

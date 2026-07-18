using System.Text;
using Rezepte.Import.Abstractions;
using Rezepte.Import.Plugins.Chefkoch;
using Rezepte.Import.Plugins.FifthSource;
using Rezepte.Import.Plugins.FourthSource;
using Rezepte.Import.Plugins.SecondSource;
using Rezepte.Import.Plugins.SixthSource;
using Rezepte.Import.Plugins.ThirdSource;

var plugins = new IImportPlugin[]
{
    new ChefkochImportPlugin(),
    new SecondSourceImportPlugin(),
    new ThirdSourceImportPlugin(),
    new FourthSourceImportPlugin(),
    new FifthSourceImportPlugin(),
    new SixthSourceImportPlugin()
};

var options = RunnerOptions.Parse(args);
if (options.ShowHelp)
{
    PrintHelp(plugins);
    return 0;
}

var selectedPlugin = SelectPlugin(plugins, options.Plugin);
if (selectedPlugin is null)
{
    Console.Error.WriteLine("Unknown plugin. Available plugins:");
    PrintPlugins(plugins);
    return 2;
}

var input = await ResolveInputAsync(options).ConfigureAwait(false);
if (input is null)
{
    Console.Error.WriteLine("Provide either --file <path> or --url <url>.");
    return 2;
}

var handler = (IImportHandler)Activator.CreateInstance(selectedPlugin.HandlerType)!;
await using var stream = input.OpenStream();
var canHandle = await handler.CanHandleAsync(stream, input.FileName).ConfigureAwait(false);
if (!canHandle)
{
    Console.WriteLine($"Plugin '{selectedPlugin.Id}' cannot process '{input.DisplayName}'.");
    return 0;
}

if (stream.CanSeek)
{
    stream.Position = 0;
}

var result = await handler.HandleAsync(stream, input.FileName, input.Uri, "manual-test", "manual-user").ConfigureAwait(false);
PrintResult(result);

if (handler is ICollectionImportHandler collectionHandler)
{
    await using var previewStream = input.OpenStream();
    var preview = await collectionHandler.TryReadCollectionPreviewAsync(previewStream, input.FileName, input.Uri).ConfigureAwait(false);
    if (preview is not null)
    {
        Console.WriteLine();
        Console.WriteLine("Collection preview");
        Console.WriteLine($"Title: {preview.Title}");
        Console.WriteLine($"Source: {preview.SourceUri}");
        Console.WriteLine($"Items: {preview.Items.Count}");
        foreach (var item in preview.Items)
        {
            Console.WriteLine($"- {item.Id}: {item.Title} ({item.Url})");
        }
    }
}

return result.Success ? 0 : 1;

static IImportPlugin? SelectPlugin(IReadOnlyList<IImportPlugin> plugins, string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        PrintPlugins(plugins);
        Console.Write("Plugin ID or number: ");
        value = Console.ReadLine();
    }

    if (int.TryParse(value, out var number) && number >= 1 && number <= plugins.Count)
    {
        return plugins[number - 1];
    }

    return plugins.FirstOrDefault(p => string.Equals(p.Id, value, StringComparison.OrdinalIgnoreCase));
}

static async Task<InputSource?> ResolveInputAsync(RunnerOptions options)
{
    if (!string.IsNullOrWhiteSpace(options.Url))
    {
        using var client = new HttpClient();
        var bytes = await client.GetByteArrayAsync(options.Url).ConfigureAwait(false);
        var fileName = Path.GetFileName(new Uri(options.Url).AbsolutePath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "download.html";
        }

        return new InputSource(options.Url, fileName, options.Url, bytes);
    }

    if (!string.IsNullOrWhiteSpace(options.File))
    {
        var fullPath = Path.GetFullPath(options.File);
        return new InputSource(fullPath, Path.GetFileName(fullPath), null, await File.ReadAllBytesAsync(fullPath).ConfigureAwait(false));
    }

    Console.Write("URL or file path: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        return null;
    }

    if (Uri.TryCreate(input, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
    {
        return await ResolveInputAsync(options with { Url = input }).ConfigureAwait(false);
    }

    return await ResolveInputAsync(options with { File = input }).ConfigureAwait(false);
}

static void PrintPlugins(IReadOnlyList<IImportPlugin> plugins)
{
    for (var i = 0; i < plugins.Count; i++)
    {
        var plugin = plugins[i];
        Console.WriteLine($"{i + 1}. {plugin.Id} - {plugin.DisplayName}");
    }
}

static void PrintResult(ImportResult result)
{
    var importedRecipes = result.ImportedRecipes ?? [];
    Console.WriteLine($"Success: {result.Success}");
    if (!string.IsNullOrWhiteSpace(result.Error))
    {
        Console.WriteLine($"Error: {result.Error}");
    }

    Console.WriteLine($"Recipes: {importedRecipes.Count}");
    foreach (var recipe in importedRecipes)
    {
        Console.WriteLine();
        Console.WriteLine($"Title: {recipe.Title}");
        Console.WriteLine($"Source: {recipe.SourceUri}");
        Console.WriteLine($"Description: {recipe.Description}");
        Console.WriteLine($"Portions: {recipe.Portions}");
        Console.WriteLine($"Work time minutes: {recipe.WorkTimeMinutes}");

        Console.WriteLine("Ingredients:");
        foreach (var ingredient in recipe.Ingredients)
        {
            Console.WriteLine($"- {Join(ingredient.Quantity, ingredient.Name)}");
        }

        Console.WriteLine("Steps:");
        for (var i = 0; i < recipe.Steps.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {recipe.Steps[i].Text}");
        }
    }
}

static string Join(params string?[] values) => string.Join(" ", values.Where(v => !string.IsNullOrWhiteSpace(v)));

static void PrintHelp(IReadOnlyList<IImportPlugin> plugins)
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project Rezepte.Import.PluginRunner -- --plugin <id|number> --file <path>");
    Console.WriteLine("  dotnet run --project Rezepte.Import.PluginRunner -- --plugin <id|number> --url <url>");
    Console.WriteLine();
    Console.WriteLine("Available plugins:");
    PrintPlugins(plugins);
}

internal sealed record InputSource(string DisplayName, string FileName, string? Uri, byte[] Data)
{
    public MemoryStream OpenStream() => new(Data, writable: false);
}

internal sealed record RunnerOptions(string? Plugin, string? File, string? Url, bool ShowHelp)
{
    public static RunnerOptions Parse(string[] args)
    {
        string? plugin = null;
        string? file = null;
        string? url = null;
        var showHelp = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--plugin" when i + 1 < args.Length:
                    plugin = args[++i];
                    break;
                case "--file" when i + 1 < args.Length:
                    file = args[++i];
                    break;
                case "--url" when i + 1 < args.Length:
                    url = args[++i];
                    break;
                case "-h":
                case "--help":
                    showHelp = true;
                    break;
            }
        }

        return new RunnerOptions(plugin, file, url, showHelp);
    }
}

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HOSASBridge.Profiles.Tests;

public sealed class LocalizationTests
{
    private static Dictionary<string, string> Catalog() => JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "localization/pl.json")))!;
    [Fact] public void PolishCatalogHasAllTextAndUniqueKeys()
    {
        var catalog = Catalog(); Assert.True(catalog.Count > 150);
        Assert.All(catalog, pair => { Assert.False(string.IsNullOrWhiteSpace(pair.Key)); Assert.False(string.IsNullOrWhiteSpace(pair.Value)); });
        Assert.Equal(catalog.Count, catalog.Keys.Select(Key).Distinct().Count());
    }
    [Fact] public void EveryXamlTranslationResolves()
    {
        var keys = Catalog().Keys.Select(Key).ToHashSet();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "localization"), "*.xaml", SearchOption.AllDirectories))
            foreach (Match match in Regex.Matches(File.ReadAllText(file), @"\{DynamicResource (L[0-9A-F]{12})\}")) Assert.Contains(match.Groups[1].Value, keys);
    }
    [Fact] public void TranslationRetainsFormattingArguments()
    {
        foreach (var pair in Catalog())
        {
            static string[] Tokens(string text) => Regex.Matches(text, @"\{(\d+)(?:[^}]*)\}").Select(m => m.Groups[1].Value).Order().ToArray();
            Assert.Equal(Tokens(pair.Key), Tokens(pair.Value));
        }
    }
    [Fact] public void PolishDefaultAndEnglishPersistence()
    {
        var settings = new HOSASBridge.Infrastructure.UserSettings(); Assert.Equal(System.Globalization.CultureInfo.InstalledUICulture.TwoLetterISOLanguageName == "pl" ? "pl" : "en", settings.Language);
        var copy = JsonSerializer.Deserialize<HOSASBridge.Infrastructure.UserSettings>(JsonSerializer.Serialize(settings with { Language = "en" })); Assert.Equal("en", copy!.Language);
    }
    private static string Key(string text) => "L" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..12];
    [Fact] public void EnglishAndPolishKeysMatchWithoutDuplicates()
    {
        HashSet<string> Read(string language)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "localization", language + ".json")));
            var keys = new HashSet<string>();
            foreach (var property in doc.RootElement.EnumerateObject()) { Assert.True(keys.Add(property.Name)); Assert.False(string.IsNullOrWhiteSpace(property.Value.GetString())); }
            return keys;
        }
        Assert.True(Read("en").SetEquals(Read("pl")));
    }
    [Fact] public void MarkupExtensionKeysResolve()
    {
        var catalog = Catalog();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "localization"), "*.xaml", SearchOption.AllDirectories))
            foreach (Match match in Regex.Matches(File.ReadAllText(file), @"\{loc:Text (?:'([^']+)'|([^}]+))\}"))
                Assert.Contains(match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value, catalog.Keys);
    }
}

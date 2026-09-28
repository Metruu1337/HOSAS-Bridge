using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;

namespace HOSASBridge.App.Localization;

public static class L
{
    private static readonly IReadOnlyDictionary<string, string> Polish = Load("pl");
    private static readonly IReadOnlyDictionary<string, string> English = Load("en");
    private static IReadOnlyDictionary<string, string> Load(string language)
    {
        using var stream = typeof(L).Assembly.GetManifestResourceStream($"HOSASBridge.Localization.{language}.json") ?? throw new InvalidDataException("Polish localization resource is missing.");
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? throw new InvalidDataException("Invalid localization catalog.");
    }
    public static string T(string text)
    {
        if ((CultureInfo.DefaultThreadCurrentUICulture ?? CultureInfo.CurrentUICulture).TwoLetterISOLanguageName != "pl") return English.GetValueOrDefault(text, text);
        if (Polish.TryGetValue(text, out var translated)) return translated;
        if (text.Contains('\n')) return string.Join(Environment.NewLine, text.Split(["\r\n", "\n"], StringSplitOptions.None).Select(T));
        foreach (var template in StatusTemplates)
        {
            var match = template.Pattern.Match(text);
            if (match.Success) return string.Format(CultureInfo.CurrentCulture, template.Polish, match.Groups.Cast<Group>().Skip(1).Select(g => (object)g.Value).ToArray());
        }
        return text;
    }
    private static readonly (Regex Pattern, string Polish)[] StatusTemplates =
    [
        (new(@"^vJoy (\d+) ready · API 2\.2\.2 · persistent device$"), "vJoy {0} gotowy · API 2.2.2 · urządzenie trwałe"),
        (new(@"^vJoy (\d+) is busy \(owner PID (-?\d+)\)\.$"), "vJoy {0} jest zajęty (PID właściciela: {1})."),
        (new(@"^vJoy Device (\d+) is missing\.$"), "Brak urządzenia vJoy nr {0}."),
        (new(@"^vJoy axis (.+) is missing\.$"), "Brak osi vJoy: {0}."),
        (new(@"^Unsupported vJoy API: DLL (.+), driver (.+); expected 222\.$"), "Niezgodne API vJoy: DLL {0}, sterownik {1}; wymagane 222."),
        (new(@"^vJoy interface unavailable: (.+)$"), "Interfejs vJoy niedostępny: {0}")
    ];
    public static string F(string text, params object?[] arguments) => string.Format(CultureInfo.DefaultThreadCurrentCulture ?? CultureInfo.CurrentCulture, T(text), arguments);
    public static string YesNo(bool value) => T(value ? "Yes" : "No");
    public static string ResourceKey(string text) => "L" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..12];
    public static void LoadResources(ResourceDictionary resources)
    {
        foreach (var text in Polish.Keys) resources[ResourceKey(text)] = T(text);
    }
}
public sealed class LocalizedValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => L.T(value?.ToString() ?? "");
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (targetType.IsEnum)
        {
            foreach (var candidate in Enum.GetValues(targetType))
                if (L.T(candidate.ToString()!) == value?.ToString()) return candidate;
            return Enum.Parse(targetType, value?.ToString() ?? "", true);
        }
        return value;
    }
}

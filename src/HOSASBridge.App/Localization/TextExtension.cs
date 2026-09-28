using System.Windows.Markup;
namespace HOSASBridge.App.Localization;
[MarkupExtensionReturnType(typeof(string))]
public sealed class TextExtension(string text) : MarkupExtension
{
    public string Text { get; set; } = text;
    public override object ProvideValue(IServiceProvider serviceProvider) => L.T(Text);
}

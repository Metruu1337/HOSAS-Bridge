using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HOSASBridge.App.ViewModels;

namespace HOSASBridge.App.Services;

internal static class UiSmokeTest
{
    public static async Task RunAsync(Window window, MainViewModel model, string directory)
    {
        Directory.CreateDirectory(directory);
        using var trace = new StringWriter();
        using var listener = new System.Diagnostics.TextWriterTraceListener(trace);
        System.Diagnostics.PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
        for (var i = 0; i < 8; i++)
        {
            model.SelectedTab = i;
            await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle);
            var dispatcherTranslation = await window.Dispatcher.InvokeAsync(() => L.T("Information"));
            var expectedTranslation = System.Globalization.CultureInfo.DefaultThreadCurrentUICulture?.TwoLetterISOLanguageName == "pl" ? "Informacje" : "Information";
            if (dispatcherTranslation != expectedTranslation) throw new InvalidOperationException("Dispatcher localization differs from the selected language.");
            await Task.Delay(150);
            var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(window);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var file = File.Create(Path.Combine(directory, $"tab-{i}.png")); encoder.Save(file);
        }
        model.SelectedTab = 4; model.Wizard.Reset();
        for (var step = 0; step < 9; step++)
        {
            await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle);
            var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(window); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var file = File.Create(Path.Combine(directory, $"wizard-{step}.png")); encoder.Save(file);
            model.Wizard.Next.Execute(null);
        }
        System.Diagnostics.PresentationTraceSources.DataBindingSource.Listeners.Remove(listener);
        var bindingErrors = trace.ToString();
        File.WriteAllText(Path.Combine(directory, "binding-diagnostics.txt"), bindingErrors);
        if (bindingErrors.Contains("Error", StringComparison.Ordinal)) throw new InvalidOperationException("WPF binding errors: " + bindingErrors);
        File.WriteAllText(Path.Combine(directory, "result.txt"), $"8 real WPF tabs rendered. Culture: {System.Globalization.CultureInfo.CurrentUICulture.Name}. No WPF binding errors. No physical hardware acceptance was performed.");
    }
}

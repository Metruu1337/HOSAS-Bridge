using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace HOSASBridge.App.ViewModels;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; Changed(name); return true; }
}
public sealed class Command(Action action) : ICommand
{
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => action();
    public event EventHandler? CanExecuteChanged { add { } remove { } }
}
public sealed class AsyncCommand(Func<Task> action, Action<Exception> error) : ICommand
{
    private bool busy;
    public bool CanExecute(object? parameter) => !busy;
    public event EventHandler? CanExecuteChanged;
    public async void Execute(object? parameter)
    {
        if (busy) return;
        busy = true; CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try { await action(); } catch (Exception ex) { error(ex); }
        finally { busy = false; CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
    }
}

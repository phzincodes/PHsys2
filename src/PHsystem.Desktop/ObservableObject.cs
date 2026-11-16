using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace PHsystem.Desktop;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}

public sealed class UiCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) { if (CanExecute(parameter)) execute(); }
    public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class UiParameterCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => parameter is not null && (canExecute?.Invoke(parameter) ?? true);
    public void Execute(object? parameter) { if (CanExecute(parameter)) execute(parameter); }
    public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

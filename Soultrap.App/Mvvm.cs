using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
namespace Soultrap.App;
public abstract class Observable : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Changed([CallerMemberName]string? name=null)=>PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(name));
    protected bool Set<T>(ref T field,T value,[CallerMemberName]string? name=null)
    {if(EqualityComparer<T>.Default.Equals(field,value))return false;field=value;Changed(name);return true;}
}
public sealed class AsyncCommand(Func<string,Task> run,Func<bool> enabled) : ICommand
{
    public bool CanExecute(object? parameter)=>enabled();
    public async void Execute(object? parameter)=>await run(parameter?.ToString()??"");
    public event EventHandler? CanExecuteChanged;
    public void Refresh()=>CanExecuteChanged?.Invoke(this,EventArgs.Empty);
}

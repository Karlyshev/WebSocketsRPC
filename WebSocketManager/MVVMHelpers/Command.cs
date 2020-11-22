using System.Windows.Input;
using System;

namespace MVVMPattern
{
    public class Command : ICommand
    {
        private Action<object> execute;
        private Func<object, bool> canExecute;

        //Событие CanExecuteChanged вызывается при изменении условий, указывающий, может ли команда выполняться. 
        //Для этого используется событие CommandManager.RequerySuggested.
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        //Ключевым является метод Execute. Для его выполнения в конструкторе команды передается делегат типа 
        //Action<object>. При этом класс команды не знает какое именно действие будет выполняться.
        public Command(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        //определяет, может ли команда выполняться
        public bool CanExecute(object parameter) => this.canExecute == null || this.canExecute(parameter);

        //собственно выполняет логику команды
        public void Execute(object parameter) => this.execute(parameter);
    }
}
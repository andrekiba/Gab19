using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using Xamarin.Forms;

namespace Gab.Base
{
    public class DependentCommand : Command
    {
        readonly List<string> dependentPropertyNames;

        public DependentCommand(Action execute, Func<bool> canExecute, INotifyPropertyChanged target, params string[] dependentPropertyNames)
            : base(execute, canExecute)
        {
            this.dependentPropertyNames = new List<string>(dependentPropertyNames);
            target.PropertyChanged += TargetPropertyChanged;
        }

        public DependentCommand(Action execute, Func<bool> canExecute, INotifyPropertyChanged target, params Expression<Func<object>>[] dependentPropertyExpressions)
            : base(execute, canExecute)
        {
            dependentPropertyNames = new List<string>();
            foreach (var expression in dependentPropertyExpressions.Select(expression => expression.Body))
            {
                switch (expression)
                {
                    case MemberExpression memberExpression:
                        dependentPropertyNames.Add(memberExpression.Member.Name);
                        break;
                    case UnaryExpression unaryExpression:
                        dependentPropertyNames.Add(((MemberExpression)unaryExpression.Operand).Member.Name);
                        break;
                }
            }
            target.PropertyChanged += TargetPropertyChanged;
        }

        void TargetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!dependentPropertyNames.Contains(e.PropertyName))
                return;
            ChangeCanExecute();
        }
    }
}

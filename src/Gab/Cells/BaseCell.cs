using System.Windows.Input;
using Xamarin.Forms;

namespace Gab.Cells
{
    public class BaseCell : ViewCell
    {
        public static BindableProperty ParentBindingContextProperty = BindableProperty.Create(nameof(ParentBindingContext), typeof(object), typeof(BaseCell));

        public object ParentBindingContext
        {
            get => GetValue(ParentBindingContextProperty);
            set => SetValue(ParentBindingContextProperty, value);
        }

        public static readonly BindableProperty TappedCommandProperty = BindableProperty.Create(nameof(TappedCommand), typeof(ICommand), typeof(BaseCell), default(Command), BindingMode.OneWay,
            propertyChanged: (bindable, oldValue, newValue) =>
            {

            });
        public ICommand TappedCommand
        {
            get => (ICommand)GetValue(TappedCommandProperty);
            set => SetValue(TappedCommandProperty, value);
        }

        public static readonly BindableProperty TappedCommandParameterProperty = BindableProperty.Create(nameof(TappedCommandParameter), typeof(object), typeof(BaseCell));
        public object TappedCommandParameter
        {
            get => GetValue(TappedCommandParameterProperty);
            set => SetValue(TappedCommandParameterProperty, value);
        }

        public BaseCell()
        {
            Tapped += (sender, args) =>
            {
                if (TappedCommand == null || !TappedCommand.CanExecute(TappedCommandParameter))
                    return;
                TappedCommand.Execute(TappedCommandParameter);
            };
        }
    }
}

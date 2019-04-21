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
    }
}

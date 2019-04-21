using System;
using FormsControls.Base;
using Gab.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Gab.Pages
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MeetingRoomsPage : ContentPage, IAnimationPage
	{
		public MeetingRoomsPage()
		{
			InitializeComponent();

		    MeetingRoomList.ItemTapped += (sender, e) =>
		    {
		        if (Device.RuntimePlatform == Device.iOS || Device.RuntimePlatform == Device.Android)
		            MeetingRoomList.SelectedItem = null;
		    };
        }

	    protected override bool OnBackButtonPressed()
	    {
	        //non permetto di navigare indietro tramite bottone fisico
	        return true;
	    }      

	    void PullToRefresh_OnRefreshing(object sender, EventArgs e)
	    {
	        ((MeetingRoomsViewModel)BindingContext).RefreshCommand.Execute(null);
	    }

        #region IAnimationPage

        public void OnAnimationStarted(bool isPopAnimation)
	    {
	    }

	    public void OnAnimationFinished(bool isPopAnimation)
	    {
	    }

	    public IPageAnimation PageAnimation { get; } = new PushPageAnimation
	    {
	        Subtype = AnimationSubtype.FromRight,
	        Duration = AnimationDuration.Medium
	    };

	    #endregion
    }
}
using System;
using System.Threading.Tasks;
using FreshMvvm;
using Xamarin.Forms;

namespace Gab.Base.Navigation
{
    public class FreshIconAnimationNavigationContainer : BaseNavigationPage, IFreshNavigationService
    {
        public string NavigationServiceName { get; }

        public FreshIconAnimationNavigationContainer(Page page)
            : this(page, NavigationContainerNames.DefaultContainer)
        {
        }

        public FreshIconAnimationNavigationContainer(Page page, string navigationPageName)
            : base(page)
        {
            var viewModel = page.GetModel();
            //var viewModel = page.CurrentPage.GetModel();
            viewModel.CurrentNavigationServiceName = navigationPageName;
            NavigationServiceName = navigationPageName;
            RegisterNavigation();
        }

        protected void RegisterNavigation()
        {
            FreshIOC.Container.Register((IFreshNavigationService)this, NavigationServiceName);
        }

        internal Page CreateContainerPageSafe(Page page)
        {
            if (page is NavigationPage || page is MasterDetailPage || page is TabbedPage)
                return page;
            return CreateContainerPage(page);
        }

        protected virtual Page CreateContainerPage(Page page) => new BaseNavigationPage(page);
        

        public virtual Task PushPage(Page page, FreshBasePageModel model, bool modal = false, bool animate = true)
        {
            return modal ? Navigation.PushModalAsync(CreateContainerPageSafe(page), animate) : Navigation.PushAsync(page, animate);
        }

        public virtual Task PopPage(bool modal = false, bool animate = true)
        {
            return modal ? Navigation.PopModalAsync(animate) : Navigation.PopAsync(animate);
        }

        public Task PopToRoot(bool animate = true)
        {
            return Navigation.PopToRootAsync(animate);
        }

        public void NotifyChildrenPageWasPopped()
        {
            this.NotifyAllChildrenPopped();
        }

        public Task<FreshBasePageModel> SwitchSelectedRootPageModel<T>() where T : FreshBasePageModel
        {
            throw new Exception("This navigation container has no selected roots, just a single root");
        }
    }
}

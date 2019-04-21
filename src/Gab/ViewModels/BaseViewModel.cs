using System;
using System.Threading.Tasks;
using Acr.UserDialogs;
using FreshMvvm;
using Gab.Base;
using Gab.Resources;
using Gab.Shared.Base;
using Plugin.Multilingual;
using Polly.CircuitBreaker;
using PropertyChanged;
using Refit;

namespace Gab.ViewModels
{
    public class BaseViewModel : FreshBasePageModel
    {
        #region Properties

        public bool IsBusy { get; set; }
        [DependsOn(nameof(IsBusy))]
        public bool IsNotBusy => !IsBusy;
        public LocalizedResources Resources { get; }

        #endregion

        #region Lifecycle

        public BaseViewModel()
        {
            Resources = new LocalizedResources(typeof(AppResources), CrossMultilingual.Current.CurrentCultureInfo);
        }

        #endregion

        #region Methods

        protected async Task<Result> Do(Func<Task<Result>> func, string loadingMessage = null, string caller = null)
        {
            string error = null;
            Exception ex = null;
            var result = new Result();

            try
            {
                if (IsBusy)
                    return Result.Fail(nameof(IsBusy));

                IsBusy = true;
                if (loadingMessage != null)
                    UserDialogs.Instance.ShowLoading(loadingMessage);

                result = await func();
            }
            catch (OperationCanceledException e)
            {
                ex = e;
                error = AppResources.OperationTimeout;
            }
            catch (ApiException e)
            {
                ex = e;
                error = await e.GetMessage();
            }
            catch (BrokenCircuitException e)
            {
                ex = e.InnerException ?? e;
                if (ex is ApiException apiException)
                    error = await apiException.GetMessage();
                else
                    error = ex.Message;
            }
            catch (Exception e)
            {
                ex = e;
                error = e.Message;
            }
            finally
            {
                IsBusy = false;
                if (loadingMessage != null)
                    UserDialogs.Instance.HideLoading();
            }

            if (ex == null)
                return result.IsFailure ? Result.Fail(result.Error.Translate()) : result;

            return Result.Fail(error.Translate());
        }

        protected async Task<Result<T>> Do<T>(Func<Task<Result<T>>> func, string loadingMessage = null, string caller = null)
        {
            string error = null;
            Exception ex = null;
            var result = new Result<T>();

            try
            {
                if (IsBusy)
                    return Result.Fail<T>(nameof(IsBusy));

                IsBusy = true;
                if (loadingMessage != null)
                    UserDialogs.Instance.ShowLoading(loadingMessage);

                result = await func();
            }
            catch (OperationCanceledException e)
            {
                ex = e;
                error = AppResources.OperationTimeout;
            }
            catch (ApiException e)
            {
                ex = e;
                error = await e.GetMessage();
            }
            catch (BrokenCircuitException e)
            {
                ex = e.InnerException ?? e;
                if (ex is ApiException apiException)
                    error = await apiException.GetMessage();
                else
                    error = ex.Message;
            }
            catch (Exception e)
            {
                ex = e;
                error = e.Message;
            }
            finally
            {
                IsBusy = false;
                if (loadingMessage != null)
                    UserDialogs.Instance.HideLoading();
            }

            if (ex == null)
                return result.IsFailure ? Result.Fail<T>(result.Error.Translate()) : result;

            return Result.Fail<T>(error.Translate());
        }

        #endregion 
    }
}

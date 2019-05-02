using System;
using System.Resources;
using Gab.Resources;
using Gab.Shared.Base;
using Gab.Shared.Messages;
using Plugin.Multilingual;

namespace Gab.Base
{
    public static class TranslationHelper
    {
        static readonly Lazy<ResourceManager> resmgr = new Lazy<ResourceManager>(() => new ResourceManager(typeof(AppResources)));

        public static string Translate(string key)
        {
            var ci = CrossMultilingual.Current.CurrentCultureInfo;

            var translation = resmgr.Value.GetString(key, ci);
            var genericErrorMessage = resmgr.Value.GetString(ErrorMessages.GenericErrorMessage, ci);

            return !translation.IsNullOrWhiteSpace() ? translation : genericErrorMessage;
        }
    }
}

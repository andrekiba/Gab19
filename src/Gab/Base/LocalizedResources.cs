using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using Gab.Messages;
using Xamarin.Forms;

namespace Gab.Base
{
    public class LocalizedResources : INotifyPropertyChanged
    {
        const string DefaultLanguage = "en";

        readonly ResourceManager resourceManager;
        CultureInfo currentCultureInfo;

        public string this[string key] => resourceManager.GetString(key, currentCultureInfo);

        public LocalizedResources(Type resource, string language = null)
            : this(resource, new CultureInfo(language ?? DefaultLanguage))
        { }

        public LocalizedResources(Type resource, CultureInfo cultureInfo)
        {
            currentCultureInfo = cultureInfo;
            resourceManager = new ResourceManager(resource);

            MessagingCenter.Subscribe<object, CultureChangedMessage>(this,
                string.Empty, OnLanguageChanged);
        }

        void OnLanguageChanged(object s, CultureChangedMessage ccm)
        {
            currentCultureInfo = ccm.NewCultureInfo;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

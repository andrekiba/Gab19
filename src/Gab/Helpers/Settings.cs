using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Gab.Shared.Base;
using Plugin.Settings;
using Plugin.Settings.Abstractions;

namespace Gab.Helpers
{
    public class Settings : INotifyPropertyChanged
    {
        static ISettings AppSettings => CrossSettings.Current;
        static Settings settings;

        #region Properties

        public static Settings Current => settings ?? (settings = new Settings());

        public string Email
        {
            get => AppSettings.GetValueOrDefault(nameof(Email), null);
            set
            {
                if (AppSettings.AddOrUpdateValue(nameof(Email), value))
                    OnPropertyChanged(nameof(IsLoggedIn));
            }
        }

        public string Fullname
        {
            get => AppSettings.GetValueOrDefault(nameof(Fullname), null);
            set => AppSettings.AddOrUpdateValue(nameof(Fullname), value);
        }

        public string UserCode
        {
            get => AppSettings.GetValueOrDefault(nameof(UserCode), null);
            set => AppSettings.AddOrUpdateValue(nameof(UserCode), value);
        }

        public string IdUser
        {
            get => AppSettings.GetValueOrDefault(nameof(IdUser), null);
            set => AppSettings.AddOrUpdateValue(nameof(IdUser), value);
        }

        public DateTime DeadlinesLastSync
        {
            get => AppSettings.GetValueOrDefault(nameof(DeadlinesLastSync), DateTime.UtcNow.AddDays(-1));
            set
            {
                if (AppSettings.AddOrUpdateValue(nameof(DeadlinesLastSync), value))
                    OnPropertyChanged();
            }
        }
        public DateTime NewsLastSync
        {
            get => AppSettings.GetValueOrDefault(nameof(NewsLastSync), DateTime.UtcNow.AddDays(-1));
            set
            {
                if (AppSettings.AddOrUpdateValue(nameof(NewsLastSync), value))
                    OnPropertyChanged();
            }
        }


        public bool IsLoggedIn => !Email.IsNullOrWhiteSpace();

        #endregion

        #region Methods

        public static void Clear()
        {
            AppSettings.Clear();
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged([CallerMemberName]string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #endregion
    }
}

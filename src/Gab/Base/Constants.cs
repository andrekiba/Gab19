using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace Gab.Base
{
    public static class Constants
    {

        //azure
        //public const string MeetingRoomsApi = "http://localhost:7071/api";       
        //public const string MeetingRoomsApi = "https://f6cd5ea3.ngrok.io/api";
        public const string MeetingRoomsApi = "https://gab19.azurewebsites.net/api";
        public const string MeetingRoomsFuncKey = "VC4ave2hq0N1O0vxEGqkfDLiawNS0Q5IZ6jakWLqSTQ4yIg2gWsn2A==";
        public static string NotificationUrl => $"{MeetingRoomsApi}/notification";

        //syncfusion
        public const string SyncfusionLicenseKey = "OTI4NzJAMzEzNzJlMzEyZTMwVDhlOGR5YllxWTVTM0Jpa09mUGtyNStnQWVDQXpqS29wVWV6bW1BRzNrbz0=";
        //public const string SyncfusionLicenseKey = "";

        public static class Colors
        {
            public static readonly List<Color> MeetingRoomColors = new List<Color>
            {
                Color.FromHex("#F44336"), //Red
                Color.FromHex("#E91E63"), //Pink
                Color.FromHex("#9C27B0"), //Purple
                Color.FromHex("#673AB7"), //Deep Purple
                Color.FromHex("#3F51B5"), //Indigo
                Color.FromHex("#2196F3"), //Blue
                Color.FromHex("#03A9F4"), //Light Blue
                Color.FromHex("#00BCD4"), //Cyan
                Color.FromHex("#009688"), //Teal
                Color.FromHex("#4CAF50"), //Green
                Color.FromHex("#8BC34A"), //Light Green
                Color.FromHex("#CDDC39"), //Lime
                Color.FromHex("#FFEB3B"), //Yellow
                Color.FromHex("#FFC107"), //Amber
                Color.FromHex("#FF9800"), //Orange
                Color.FromHex("#FF5722"), //Deep Orange
                Color.FromHex("#795548"), //Brown
                Color.FromHex("#9E9E9E"), //Grey
                Color.FromHex("#607D8B"), //Blue Grey
            };

            public static readonly Dictionary<int, List<Color>> EventColors = MeetingRoomColors.Select(x => new
                {
                    Key = MeetingRoomColors.IndexOf(x),
                    Value = x.GenerateShades(10)
                })
                .ToDictionary(x => x.Key, y => y.Value);

            public static readonly Color CompletedColor = new Color(0.2, 0.2, 0.2);

        }

    }
}

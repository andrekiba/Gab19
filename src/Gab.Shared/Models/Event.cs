using System;
using MvvmHelpers;
using Newtonsoft.Json;

namespace Gab.Shared.Models
{
    public class Event : ObservableObject
    {
        public string Id { get; set; }
        public string Subject { get; set; }
        public string BodyPreview { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Organizer { get; set; }
        public string TimeZone { get; set; }
        public ChangeType ChangeType { get; set; } = ChangeType.None;
        [JsonIgnore]
        public bool IsCurrent { get; set; }

        public override bool Equals(object obj) => Equals(obj as Event);

        public bool Equals(Event other)
        {
            return other != null && other.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public class CreateEvent
    {
        public MeetingRoom MeetingRoom { get; set; }
        public string TimeZone { get; set; }
    }

    public class EndsEvent
    {
        public string Id { get; set; }
        public DateTime Ended { get; set; }
        public MeetingRoom MeetingRoom { get; set; }
        public string TimeZone { get; set; }
    }

    public static class EventMappings
    {
        public static Event ToEvent(this Microsoft.Graph.Event e, ChangeType changeType = ChangeType.None)
        {
            return new Event
            {
                Id = e.Id,
                Subject = e.Subject,
                BodyPreview = e.BodyPreview,
                Start = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Parse(e.Start.DateTime), e.OriginalStartTimeZone),
                End = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Parse(e.End.DateTime), e.OriginalStartTimeZone),
                Organizer = e.Organizer.EmailAddress.Name,
                TimeZone = e.OriginalStartTimeZone,
                ChangeType = changeType
            };
        }
    }

    public enum ChangeType
    {
        None = 0,
        Created = 1,
        Updated = 2,
        Deleted = 3
    }
}

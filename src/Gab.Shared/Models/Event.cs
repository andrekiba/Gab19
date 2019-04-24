using System;
using Newtonsoft.Json;
using PropertyChanged;

namespace Gab.Shared.Models
{
    [AddINotifyPropertyChangedInterface]
    public class Event
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

        public Event Update(Event e)
        {
            Start = e.Start;
            End = e.End;
            BodyPreview = e.BodyPreview;
            ChangeType = e.ChangeType;
            Organizer = e.Organizer;
            Subject = e.Subject;
            TimeZone = e.TimeZone;

            return this;
        }

        #region Equals

        public override bool Equals(object obj) => Equals(obj as Event);

        public bool Equals(Event other)
        {
            return other != null && other.Id == Id;
        }

        public override int GetHashCode() => Id.GetHashCode();

        #endregion

        #region Operators

        public static bool operator ==(Event ev1, Event ev2)
        {
            if (ReferenceEquals(ev1, ev2))
                return true;

            if (ReferenceEquals(ev1, null))
                return false;
            
            if (ReferenceEquals(ev2, null))
                return false;

            return ev1.Equals(ev2);
        }

        // this is second one '!='
        public static bool operator !=(Event ev1, Event ev2) => !(ev1 == ev2);

        #endregion 

        //public void RaisePropertyChanged([CallerMemberName] string propertyName = null) => OnPropertyChanged(propertyName);
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
                Start = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Parse(e.Start.DateTime), e.OriginalStartTimeZone, e.OriginalStartTimeZone),
                End = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Parse(e.End.DateTime), e.OriginalStartTimeZone, e.OriginalStartTimeZone),
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

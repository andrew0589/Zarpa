namespace NavigationES.Api.Data.Entities
{
    // Where a user is in one video, saved by the players every few seconds so
    // "Continuar viendo" is the same on the web and in the app. Composite key
    // (UserID, VideoID).
    public class StudyVideoProgressEntity
    {
        public long UserID { get; set; }
        public UserEntity User { get; set; }

        public long VideoID { get; set; }
        public StudyVideoEntity Video { get; set; }

        public int PositionSeconds { get; set; }

        // Set once the user reaches the end; stays set when they rewatch.
        public bool Completed { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

namespace NavigationES.Api.Data.Entities
{
    // The exam blueprint: for a given license, how many questions of this topic the
    // exam draws and how many errors it tolerates in it. Generating any exam-shaped
    // test = one random draw per row of this table. Composite key (LicenseID, TopicID).
    public class LicenseTopicEntity
    {
        public long LicenseID { get; set; }
        public LicenseEntity License { get; set; }

        public long TopicID { get; set; }
        public TopicEntity Topic { get; set; }

        public int QuestionsInExam { get; set; }

        // Null = no per-topic error limit, only the license's MaxTotalErrors applies.
        public int? MaxErrors { get; set; }
    }
}

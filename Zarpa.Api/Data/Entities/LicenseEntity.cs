using System.ComponentModel.DataAnnotations;

namespace Zarpa.Api.Data.Entities
{
    // A nautical qualification (PNB, PER, PY, CY). The exam rules that vary per
    // topic live in LicenseTopicEntity; only the global rules live here.
    public class LicenseEntity
    {
        [Key]
        public long ID { get; set; }

        [Required, MaxLength(10)]
        public string Code { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        // Null until the official exam rules for this license are configured.
        public int? ExamMinutes { get; set; }

        public int? MaxTotalErrors { get; set; }

        public List<LicenseTopicEntity> Topics { get; set; } = [];
    }
}

using System.ComponentModel.DataAnnotations;

namespace Zarpa.Api.Data.Entities
{
    // One official exam sitting (a real paper): which community ran it, for which
    // qualification, when, and which model letter. Its questions are LINKS into the
    // deduplicated bank (ExamQuestionEntity) — never copies, so a question shared by
    // five papers exists once and appears in all five simulations.
    public class ExamEntity
    {
        public long ID { get; set; }

        public long ComunidadAutonomaID { get; set; }
        public ComunidadAutonomaEntity ComunidadAutonoma { get; set; }

        public long LicenseID { get; set; }
        public LicenseEntity License { get; set; }

        public int Year { get; set; }

        // 1–12.
        public int Month { get; set; }

        // The paper's model letter within the sitting ("A", "B", …); null when the
        // sitting had a single paper.
        [MaxLength(10)]
        public string? Model { get; set; }

        // The PDF the exam was transcribed from — traceability for corrections.
        [MaxLength(200)]
        public string? SourceFile { get; set; }
    }
}

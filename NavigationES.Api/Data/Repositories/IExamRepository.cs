using NavigationES.Api.Data.Entities;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Data.Repositories
{
    public interface IExamRepository
    {
        // Comunidad queries live here — exams are their main consumer.
        Task<ComunidadAutonomaEntity?> FindComunidadByNameAsync(string name);

        Task<List<ComunidadAutonomaEntity>> GetAllComunidadesAsync();

        // The picker list: a license's papers, optionally narrowed to one community,
        // newest sitting first, with the user's standing on each paper. Projected
        // straight to the DTO — it is an aggregate, not an entity.
        Task<List<ExamListItemDto>> GetExamsForListAsync(long userId, long licenseId, long? comunidadId);

        void AddExam(ExamEntity exam);

        // Drops everything added but not yet saved — a failed exam in a bulk import
        // must not leak its rows into the next exam's SaveChanges.
        void DiscardChanges();

        void AddExamQuestion(ExamQuestionEntity link);

        Task SaveChangesAsync();
    }
}

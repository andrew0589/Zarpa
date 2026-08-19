using Zarpa.Api.Data.Entities;

namespace Zarpa.Api.Data.Repositories
{
    public interface ILicenseRepository
    {
        Task<List<LicenseEntity>> GetAllAsync();

        // The license with the given code (PNB/PER/PY/CY), or null.
        Task<LicenseEntity?> FindByCodeAsync(string code);

        // Whether the topic is part of the license's exam blueprint.
        Task<bool> IncludesTopicAsync(long licenseId, long topicId);
    }
}

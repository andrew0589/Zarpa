using NavigationES.Api.Data.Entities;

namespace NavigationES.Api.Data.Repositories
{
    public interface ILicenseRepository
    {
        Task<List<LicenseEntity>> GetAllAsync();

        // The license with the given code (PNB/PER/PY/CY), or null.
        Task<LicenseEntity?> FindByCodeAsync(string code);

        // Whether the topic is part of the license's exam blueprint.
        Task<bool> IncludesTopicAsync(long licenseId, long topicId);

        // The topic IDs of the license's blueprint.
        Task<List<long>> GetTopicIdsAsync(long licenseId);
    }
}

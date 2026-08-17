using Zarpa.Api.Data.Entities;

namespace Zarpa.Api.Data.Repositories
{
    public interface ILicenseRepository
    {
        Task<List<LicenseEntity>> GetAllAsync();

        // Whether the topic is part of the license's exam blueprint.
        Task<bool> IncludesTopicAsync(long licenseId, long topicId);
    }
}

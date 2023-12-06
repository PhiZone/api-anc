using Meilisearch;
using PhiZoneApi.Models;
using Index = Meilisearch.Index;

namespace PhiZoneApi.Interfaces;

public interface IMeilisearchService
{
    // ReSharper disable once ParameterTypeCanBeEnumerable.Global
    Task<PaginatedSearchResult<T>> SearchAsync<T>(string query, int perPage, int page,
        int? showOwnerId = null, bool showHidden = false);

    Task AddAsync<T>(T document);

    Task AddBatchAsync<T>(IEnumerable<T> documents);

    Task UpdateAsync<T>(T document);

    Task UpdateBatchAsync<T>(IEnumerable<T> documents);

    Task DeleteAsync<T>(T document) where T : Resource;

    Task DeleteBatchAsync<T>(IEnumerable<T> documents) where T : Resource;

    Task DeleteAsync<T>(Guid id);

    Task DeleteBatchAsync<T>(IEnumerable<Guid> idList);

    Task DeleteAsync<T>(int id);

    Task DeleteBatchAsync<T>(IEnumerable<int> idList);

    Index GetIndex<T>();
}
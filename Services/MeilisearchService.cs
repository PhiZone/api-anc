using Meilisearch;
using Microsoft.Extensions.Options;
using PhiZoneApi.Configurations;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using Index = Meilisearch.Index;

namespace PhiZoneApi.Services;

public class MeilisearchService : IMeilisearchService
{
    private readonly MeilisearchClient _client;

    public MeilisearchService(IOptions<MeilisearchSettings> options, IServiceProvider provider)
    {
        var settings = options.Value;
        _client = new MeilisearchClient(settings.ApiUrl, settings.MasterKey);
        CreateIndexes(provider);
    }

    // ReSharper disable once ParameterTypeCanBeEnumerable.Global
    public async Task<PaginatedSearchResult<T>> SearchAsync<T>(string query, int perPage, int page,
        int? showOwnerId = null, bool showHidden = false)
    {
        return (PaginatedSearchResult<T>)await _client.Index(typeof(T).Name)
            .SearchAsync<T>(query,
                new SearchQuery
                {
                    HitsPerPage = perPage,
                    Page = page,
                    Filter = typeof(PublicResource).IsAssignableFrom(typeof(T)) && !showHidden
                        ? "isHidden = false"
                        : (typeof(Submission).IsAssignableFrom(typeof(T)) || typeof(T) == typeof(Notification) ||
                           typeof(T) == typeof(PetAnswer)) && showOwnerId != null
                            ? $"ownerId = {showOwnerId}"
                            : ""
                });
    }

    public async Task AddAsync<T>(T document)
    {
        await _client.Index(typeof(T).Name).AddDocumentsAsync(new[] { document });
    }

    public async Task AddBatchAsync<T>(IEnumerable<T> documents)
    {
        await _client.Index(typeof(T).Name).AddDocumentsAsync(documents);
    }

    public async Task UpdateAsync<T>(T document)
    {
        await _client.Index(typeof(T).Name).UpdateDocumentsAsync(new[] { document });
    }

    public async Task UpdateBatchAsync<T>(IEnumerable<T> documents)
    {
        await _client.Index(typeof(T).Name).UpdateDocumentsAsync(documents);
    }

    public async Task DeleteAsync<T>(T document) where T : Resource
    {
        await _client.Index(typeof(T).Name).DeleteOneDocumentAsync(document.Id.ToString());
    }

    public async Task DeleteBatchAsync<T>(IEnumerable<T> documents) where T : Resource
    {
        await _client.Index(typeof(T).Name).DeleteDocumentsAsync(documents.Select(document => document.Id.ToString()));
    }

    public async Task DeleteAsync<T>(Guid id)
    {
        await _client.Index(typeof(T).Name).DeleteOneDocumentAsync(id.ToString());
    }

    public async Task DeleteBatchAsync<T>(IEnumerable<Guid> idList)
    {
        await _client.Index(typeof(T).Name).DeleteDocumentsAsync(idList.Select(id => id.ToString()));
    }

    public async Task DeleteAsync<T>(int id)
    {
        await _client.Index(typeof(T).Name).DeleteOneDocumentAsync(id);
    }

    public async Task DeleteBatchAsync<T>(IEnumerable<int> idList)
    {
        await _client.Index(typeof(T).Name).DeleteDocumentsAsync(idList);
    }

    public Index GetIndex<T>()
    {
        return _client.Index(typeof(T).Name);
    }

    private async void CreateIndexes(IServiceProvider provider)
    {
        await _client.Index("Announcement").UpdateSearchableAttributesAsync(new[] { "title", "content" });
        await _client.Index("Application")
            .UpdateSearchableAttributesAsync(new[] { "name", "illustrator", "description", "homepage", "type" });
        await _client.Index("Chapter")
            .UpdateSearchableAttributesAsync(new[] { "title", "subtitle", "illustrator", "description" });
        await _client.Index("Chart")
            .UpdateSearchableAttributesAsync(new[]
            {
                "title", "level", "difficulty", "authorName", "illustrator", "description", "noteCount"
            });
        await _client.Index("ChartSubmission")
            .UpdateSearchableAttributesAsync(new[]
            {
                "title", "level", "difficulty", "authorName", "illustrator", "description", "noteCount"
            });
        await _client.Index("Collection")
            .UpdateSearchableAttributesAsync(new[] { "title", "subtitle", "illustrator", "description" });
        await _client.Index("Event")
            .UpdateSearchableAttributesAsync(new[] { "title", "subtitle", "illustrator", "description" });
        await _client.Index("EventDivision")
            .UpdateSearchableAttributesAsync(new[] { "title", "subtitle", "illustrator", "description" });
        await _client.Index("EventTask")
            .UpdateSearchableAttributesAsync(new[] { "name", "code", "description" });
        await _client.Index("EventTeam")
            .UpdateSearchableAttributesAsync(new[] { "name" });
        await _client.Index("Notification").UpdateSearchableAttributesAsync(new[] { "content" });
        await _client.Index("PetAnswer").UpdateSearchableAttributesAsync(new[] { "answer1", "answer2", "answer3" });
        await _client.Index("PetChoice").UpdateSearchableAttributesAsync(new[] { "content", "language" });
        await _client.Index("PetQuestion").UpdateSearchableAttributesAsync(new[] { "content", "language" });
        await _client.Index("Region").UpdateSearchableAttributesAsync(new[] { "code", "name" });
        await _client.Index("ResourceRecord")
            .UpdateSearchableAttributesAsync(
                new[] { "title", "edition", "authorName", "description", "copyrightOwner" });
        await _client.Index("Song")
            .UpdateSearchableAttributesAsync(new[]
            {
                "title", "edition", "authorName", "illustrator", "lyrics", "description"
            });
        await _client.Index("SongSubmission")
            .UpdateSearchableAttributesAsync(new[]
            {
                "title", "edition", "authorName", "illustrator", "lyrics", "description"
            });
        await _client.Index("User")
            .UpdateSearchableAttributesAsync(new[] { "userName", "biography", "tag", "language", "rks", "experience" });

        await _client.Index("Chapter").UpdateFilterableAttributesAsync(new[] { "isHidden" });
        await _client.Index("Chart").UpdateFilterableAttributesAsync(new[] { "isHidden" });
        await _client.Index("Collection").UpdateFilterableAttributesAsync(new[] { "isHidden" });
        await _client.Index("Event").UpdateFilterableAttributesAsync(new[] { "isHidden" });
        await _client.Index("EventDivision").UpdateFilterableAttributesAsync(new[] { "isHidden" });
        await _client.Index("Song").UpdateFilterableAttributesAsync(new[] { "isHidden" });

        await _client.Index("ChartSubmission").UpdateFilterableAttributesAsync(new[] { "ownerId" });
        await _client.Index("Notification").UpdateFilterableAttributesAsync(new[] { "ownerId" });
        await _client.Index("PetAnswer").UpdateFilterableAttributesAsync(new[] { "ownerId" });
        await _client.Index("SongSubmission").UpdateFilterableAttributesAsync(new[] { "ownerId" });

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await _client.Index("Announcement").AddDocumentsAsync(context.Announcements, "id");
        await _client.Index("Application").AddDocumentsAsync(context.Applications, "id");
        await _client.Index("Chapter").AddDocumentsAsync(context.Chapters, "id");
        await _client.Index("Chart").AddDocumentsAsync(context.Charts, "id");
        await _client.Index("ChartSubmission").AddDocumentsAsync(context.ChartSubmissions, "id");
        await _client.Index("Collection").AddDocumentsAsync(context.Collections, "id");
        await _client.Index("Event").AddDocumentsAsync(context.Events, "id");
        await _client.Index("EventDivision").AddDocumentsAsync(context.EventDivisions, "id");
        await _client.Index("EventTask").AddDocumentsAsync(context.EventTasks, "id");
        await _client.Index("EventTeam").AddDocumentsAsync(context.EventTeams, "id");
        await _client.Index("Notification").AddDocumentsAsync(context.Notifications, "id");
        await _client.Index("PetAnswer").AddDocumentsAsync(context.PetAnswers, "id");
        await _client.Index("PetChoice").AddDocumentsAsync(context.PetChoices, "id");
        await _client.Index("PetQuestion").AddDocumentsAsync(context.PetQuestions, "id");
        await _client.Index("Region").AddDocumentsAsync(context.Regions, "id");
        await _client.Index("ResourceRecord").AddDocumentsAsync(context.ResourceRecords, "id");
        await _client.Index("Song").AddDocumentsAsync(context.Songs, "id");
        await _client.Index("SongSubmission").AddDocumentsAsync(context.SongSubmissions, "id");
        await _client.Index("User").AddDocumentsAsync(context.Users, "id");
    }
}
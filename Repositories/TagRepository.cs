using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class TagRepository(
    ApplicationDbContext context,
    IMeilisearchService meilisearchService,
    IResourceService resourceService) : ITagRepository
{
    public async Task<ICollection<Tag>> GetTagsAsync(List<string> order, List<bool> desc,
        int position, int take, Expression<Func<Tag, bool>>? predicate = null)
    {
        var result = context.Tags.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Tag> GetTagAsync(Guid id)
    {
        IQueryable<Tag> result = context.Tags;
        return (await result.FirstOrDefaultAsync(tag => tag.Id == id))!;
    }

    public async Task<Tag> GetTagAsync(string name)
    {
        IQueryable<Tag> result = context.Tags;
        return (await result.FirstOrDefaultAsync(tag => tag.NormalizedName == name))!;
    }

    public async Task<bool> TagExistsAsync(Guid id)
    {
        return await context.Tags.AnyAsync(tag => tag.Id == id);
    }
    
    public async Task<bool> TagExistsAsync(string name)
    {
        return await context.Tags.AnyAsync(tag => tag.NormalizedName == name);
    }

    public async Task<bool> CreateTagAsync(Tag tag)
    {
        await context.Tags.AddAsync(tag);
        await meilisearchService.AddAsync(tag);
        return await SaveAsync();
    }

    public async Task<bool> CreateTagsAsync(List<Tag> tags)
    {
        await context.Tags.AddRangeAsync(tags);
        await meilisearchService.AddBatchAsync(tags);
        return await SaveAsync();
    }

    public async Task<bool> CreateTagsAsync(IEnumerable<string> tagNames)
    {
        List<Tag> tags = [];
        var now = DateTimeOffset.UtcNow;
        foreach (var tagName in tagNames)
        {
            var normalized = resourceService.Normalize(tagName);
            if (tags.Any(tag => tag.NormalizedName == normalized)) continue;
            var tag = await context.Tags.FirstOrDefaultAsync(e => e.NormalizedName == normalized);
            if (tag != null) continue;
            tags.Add(new Tag
            {
                Name = tagName,
                NormalizedName = normalized,
                Description = null,
                DateCreated = now
            });
        }

        return await CreateTagsAsync(tags);
    }

    public async Task<bool> CreateTagsAsync(IEnumerable<string> tagNames, Song song)
    {
        var (existingTags, newTags, result) = await CreateAndGetTags(tagNames);
        song.Tags.Clear();
        song.Tags.AddRange(existingTags.Where(tag => !song.Tags.Contains(tag)));
        song.Tags.AddRange(newTags);
        context.Songs.Update(song);
        return result && await SaveAsync();
    }

    public async Task<bool> CreateTagsAsync(IEnumerable<string> tagNames, Chart chart)
    {
        var (existingTags, newTags, result) = await CreateAndGetTags(tagNames);
        chart.Tags.Clear();
        chart.Tags.AddRange(existingTags.Where(tag => !chart.Tags.Contains(tag)));
        chart.Tags.AddRange(newTags);
        context.Charts.Update(chart);
        return result && await SaveAsync();
    }

    public async Task<bool> UpdateTagAsync(Tag tag)
    {
        context.Tags.Update(tag);
        await meilisearchService.UpdateAsync(tag);
        return await SaveAsync();
    }

    public async Task<bool> RemoveTagAsync(Guid id)
    {
        context.Tags.Remove(
            (await context.Tags.FirstOrDefaultAsync(tag => tag.Id == id))!);
        await meilisearchService.DeleteAsync<Tag>(id);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountTagsAsync(Expression<Func<Tag, bool>>? predicate = null)
    {
        var result = context.Tags.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }

    private async Task<(List<Tag> existingTags, List<Tag> newTags, bool result)> CreateAndGetTags(
        IEnumerable<string> tagNames)
    {
        List<Tag> existingTags = [];
        List<Tag> newTags = [];
        var now = DateTimeOffset.UtcNow;
        foreach (var tagName in tagNames)
        {
            var normalized = resourceService.Normalize(tagName);
            if (existingTags.Any(tag => tag.NormalizedName == normalized) ||
                newTags.Any(tag => tag.NormalizedName == normalized))
                continue;
            var tag = await context.Tags.FirstOrDefaultAsync(e => e.NormalizedName == normalized);
            if (tag != null)
            {
                existingTags.Add(tag);
                continue;
            }

            newTags.Add(new Tag
            {
                Name = tagName,
                NormalizedName = normalized,
                Description = null,
                DateCreated = now
            });
        }

        return (existingTags, newTags, await CreateTagsAsync(newTags));
    }
}
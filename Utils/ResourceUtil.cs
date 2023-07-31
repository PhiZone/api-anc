using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos.Requests;
using PhiZoneApi.Models;

namespace PhiZoneApi.Utils;

public static partial class ResourceUtil
{
    public static string GetRichText<T>(string id, string display)
    {
        return $"[PZ{typeof(T).Name}:{id}:{display}:PZRT]";
    }

    public static async Task<bool> IsAuthorInfoConsistent(string authorName, List<AuthorshipRequestDto> authorships,
        User currentUser, UserManager<User> userManager)
    {
        var authorNameIds = GetAuthorIds(authorName);
        authorNameIds.Sort();
        var authorshipIds = new List<int>(authorships.Select(authorship => authorship.AuthorId));
        authorshipIds.Sort();
        var tasks = authorships.Select(async authorship =>
        {
            var user = await userManager.FindByIdAsync(authorship.AuthorId.ToString());
            return user != null && await userManager.IsInRoleAsync(user, Roles.Qualified);
        });
        return authorNameIds.SequenceEqual(authorshipIds) &&
               authorships.Any(authorship => authorship.AuthorId == currentUser.Id) &&
               (await Task.WhenAll(tasks)).All(result => result);
    }
    private static List<int> GetAuthorIds(string name)
    {
        var result = new List<int>();
        foreach (Match match in UserRegex().Matches(name))
        {
            var parts = match.Value.Split(':');
            result.Add(int.Parse(parts[1]));
        }

        return result;
    }

    [GeneratedRegex("\\[PZUser:[0-9]+:")]
    private static partial Regex UserRegex();
}
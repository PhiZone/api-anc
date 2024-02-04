using System.ComponentModel.DataAnnotations;
using Ganss.Xss;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Validators;

public class UserInputValidator : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        var resourceService = context.GetRequiredService<IResourceService>();

        if (value == null) return ValidationResult.Success;

        // ReSharper disable once InvertIf
        if (((string)value).Contains('<') || ((string)value).Contains('>'))
        {
            var sanitizer = new HtmlSanitizer();
            if (sanitizer.Sanitize((string)value) != (string)value)
                return new ValidationResult(ErrorMessage ?? "The input content is prohibited.");
        }

        if (resourceService.GetResources()
            .ProhibitedWords.Any(word => ((string)value).Contains(word, StringComparison.CurrentCultureIgnoreCase)))
            return new ValidationResult(ErrorMessage ?? "The input content is prohibited.");

        var httpContextAccessor = context.GetRequiredService<IHttpContextAccessor>();
        var request = httpContextAccessor.HttpContext!.Request;
        var userId = httpContextAccessor.HttpContext!.User.GetClaim(OpenIddictConstants.Claims.Subject);
        var user = userId != null ? context.GetRequiredService<UserManager<User>>().FindByIdAsync(userId).Result : null;
        var messengerService = context.GetRequiredService<IMessengerService>();
        messengerService.SendUserInput(new UserInputDelivererDto
        {
            Content = (string)value,
            IsImage = false,
            MemberName = context.MemberName!,
            ResourceId = (string?)request.RouteValues["id"],
            ActionName = (string)request.RouteValues["action"]!,
            ControllerName = (string)request.RouteValues["controller"]!,
            RequestMethod = request.Method,
            RequestPath = request.Path,
            UserId = userId != null ? int.Parse(userId) : null,
            UserName = user?.UserName,
            UserAvatar = user?.Avatar,
            DateCreated = DateTimeOffset.UtcNow
        });

        return ValidationResult.Success;
    }
}
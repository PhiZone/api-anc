// ReSharper disable UnusedMember.Global

namespace PhiZoneApi.Constants;

public static class ResponseCodes
{
    /// <summary>
    ///     Process was successful.
    /// </summary>
    public const string Ok = "Ok";

    /// <summary>
    ///     Data validation has failed.
    /// </summary>
    public const string InvalidData = "InvalidData";

    /// <summary>
    ///     User's account has been temporarily (only when DateAvailable is present) / permanently locked.
    /// </summary>
    public const string AccountLocked = "AccountLocked";

    /// <summary>
    ///     User with this name already exists.
    /// </summary>
    public const string UserNameOccupied = "UserNameOccupied";

    /// <summary>
    ///     User cannot change their user name during cooldown.
    /// </summary>
    public const string UserNameCooldown = "UserNameCooldown";

    /// <summary>
    ///     User cannot request an email during cooldown.
    /// </summary>
    public const string EmailCooldown = "EmailCooldown";

    /// <summary>
    ///     User with this email address already exists.
    /// </summary>
    public const string EmailOccupied = "EmailOccupied";

    /// <summary>
    ///     The password is incorrect.
    /// </summary>
    public const string PasswordIncorrect = "PasswordIncorrect";

    /// <summary>
    ///     The password should be between 6 and 24 characters in length.
    /// </summary>
    /// <code>^.{6,24}$</code>
    public const string InvalidPassword = "InvalidPassword";

    /// <summary>
    ///     The username can only contain numbers, underscores and English / Chinese / Japanese / Korean characters, and must
    ///     be between 4 and 12 characters in length.
    /// </summary>
    /// <code>^([A-Za-z0-9_]{4,24}|[a-zA-Z0-9_\u4e00-\u9fff\u3041-\u309f\u30a0-\u30ff\uac00-\ud7a3]{3,12}|[\u4e00-\u9fff\u3041-\u309f\u30a0-\u30ff\uac00-\ud7a3]{2,12})$</code>
    public const string InvalidUserName = "InvalidUserName";

    /// <summary>
    ///     The refresh token is outdated.
    /// </summary>
    public const string RefreshTokenOutdated = "RefreshTokenOutdated";

    /// <summary>
    ///     An internal server error has occurred.
    /// </summary>
    public const string InternalError = "InternalError";

    /// <summary>
    ///     A Redis error has occurred.
    /// </summary>
    public const string RedisError = "RedisError";

    /// <summary>
    ///     A mail service error has occurred.
    /// </summary>
    public const string MailError = "MailError";

    /// <summary>
    ///     No value is present on the field.
    /// </summary>
    public const string FieldEmpty = "FieldEmpty";

    /// <summary>
    ///     The input value is not a valid email address.
    /// </summary>
    public const string InvalidEmailAddress = "InvalidEmailAddress";

    /// <summary>
    ///     The input value is not a valid URL.
    /// </summary>
    public const string InvalidUrl = "InvalidUrl";

    /// <summary>
    ///     The input value is not a valid phone number.
    /// </summary>
    public const string InvalidPhoneNumber = "InvalidPhoneNumber";

    /// <summary>
    ///     The input value is not a valid date.
    /// </summary>
    public const string InvalidDate = "InvalidDate";

    /// <summary>
    ///     The input value is not a valid language code.
    /// </summary>
    /// <code>^[a-z]{2}(?:-[A-Z]{2})?$</code>
    public const string InvalidLanguageCode = "InvalidLanguageCode";

    /// <summary>
    ///     The specified language is not supported.
    /// </summary>
    public const string UnsupportedLanguage = "UnsupportedLanguage";

    /// <summary>
    ///     The input value is not a valid region code.
    /// </summary>
    /// <code>^[A-Z]{2}$</code>
    public const string InvalidRegionCode = "InvalidRegionCode";

    /// <summary>
    ///     The specified region is not supported.
    /// </summary>
    public const string UnsupportedRegion = "UnsupportedRegion";

    /// <summary>
    ///     The format of the specified chart is not supported.
    /// </summary>
    public const string UnsupportedChartFormat = "UnsupportedChartFormat";

    /// <summary>
    ///     The format of the specified lyrics is not supported.
    /// </summary>
    public const string UnsupportedLyricsFormat = "UnsupportedLyricsFormat";

    /// <summary>
    ///     The input value is longer than the maximum length allowed.
    /// </summary>
    public const string ValueTooLong = "ValueTooLong";

    /// <summary>
    ///     The input value is shorter than the minimum length allowed.
    /// </summary>
    public const string ValueTooShort = "ValueTooShort";

    /// <summary>
    ///     The input value is out of range.
    /// </summary>
    public const string ValueOutOfRange = "ValueOutOfRange";

    /// <summary>
    ///     The user is not logged in.
    /// </summary>
    public const string UserNotLoggedIn = "UserNotLoggedIn";

    /// <summary>
    ///     The specified user is not found.
    /// </summary>
    public const string UserNotFound = "UserNotFound";

    /// <summary>
    ///     The specified parent resource is not found.
    /// </summary>
    public const string ParentNotFound = "ParentNotFound";

    /// <summary>
    ///     The specified resource is not found.
    /// </summary>
    public const string ResourceNotFound = "ResourceNotFound";

    /// <summary>
    ///     The specified configuration is not found.
    /// </summary>
    public const string ConfigurationNotFound = "ConfigurationNotFound";

    /// <summary>
    ///     The specified application is not found.
    /// </summary>
    public const string ApplicationNotFound = "ApplicationNotFound";

    /// <summary>
    ///     The specified relation is not found.
    /// </summary>
    public const string RelationNotFound = "RelationNotFound";

    /// <summary>
    ///     The user does not have enough permission to perform the action.
    /// </summary>
    public const string InsufficientPermission = "InsufficientPermission";

    /// <summary>
    ///     The input code is not found in the Redis database.
    /// </summary>
    public const string InvalidCode = "InvalidCode";

    /// <summary>
    ///     The input token is not found in the Redis database.
    /// </summary>
    public const string InvalidToken = "InvalidToken";

    /// <summary>
    ///     User has already performed an operation.
    /// </summary>
    public const string AlreadyDone = "AlreadyDone";

    /// <summary>
    ///     The operation is invalid.
    /// </summary>
    public const string InvalidOperation = "InvalidOperation";

    /// <summary>
    ///     The time relation is invalid.
    /// </summary>
    public const string InvalidTimeRelation = "InvalidTimeRelation";

    /// <summary>
    ///     The resource is currently locked.
    /// </summary>
    public const string Locked = "Locked";

    /// <summary>
    ///     User has been blacklisted by the target user / resource owner.
    /// </summary>
    public const string Blacklisted = "Blacklisted";

    /// <summary>
    ///     User has made a song / chart submission whose author info is invalid.
    ///     This can happen to:
    ///     1. song submissions with an originality proof uploaded;
    ///     2. chart submissions;
    ///     when the uploader is not present in <c>AuthorName</c>.
    /// </summary>
    public const string InvalidAuthorInfo = "InvalidAuthorInfo";

    /// <summary>
    ///     The parent resource is private.
    /// </summary>
    public const string ParentIsPrivate = "ParentIsPrivate";

    /// <summary>
    ///     Errors occurred whilst contacting remote servers.
    /// </summary>
    public const string RemoteFailure = "RemoteFailure";

    /// <summary>
    ///     The remote account has already been bound to another user.
    /// </summary>
    public const string BindingOccupied = "BindingOccupied";

    /// <summary>
    ///     A resource with this name already exists.
    /// </summary>
    public const string NameOccupied = "NameOccupied";

    /// <summary>
    ///     There exists prohibited content in user's input text.
    /// </summary>
    public const string ContentProhibited = "ContentProhibited";

    /// <summary>
    ///     The specified authentication provider does not exist.
    /// </summary>
    public const string AuthProviderNotFound = "AuthProviderNotFound";

    /// <summary>
    ///     Failed to authenticate the user on the specified provider with provided credentials.
    /// </summary>
    public const string AuthFailure = "AuthFailure";
}
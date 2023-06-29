// ReSharper disable UnusedMember.Global

namespace PhiZoneApi.Constants;

public static class ResponseCode
{
    /// <summary>
    ///     Process was successful.
    /// </summary>
    public const string Ok = "Ok";

    /// <summary>
    ///     Data validation has failed.
    /// </summary>
    public const string DataInvalid = "DataInvalid";

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
    public const string UserNameCoolDown = "UserNameCoolDown";

    /// <summary>
    ///     User with this email address already exists.
    /// </summary>
    public const string EmailOccupied = "EmailOccupied";

    /// <summary>
    ///     The password is incorrect.
    /// </summary>
    public const string PasswordIncorrect = "PasswordIncorrect";

    /**
     * <summary>
     *     The password should be at least 6 characters long,
     *     contain at least one non-alphanumeric character,
     *     have at least one lowercase letter, one uppercase letter, one digit,
     *     and can have a maximum length of 18 characters.
     * </summary>
     * <code>^(?=.*[^a-zA-Z0-9])(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9]).{6,18}$</code>
     */
    public const string InvalidPassword = "InvalidPassword";

    /**
     * <summary>
     *     The username can only contain numbers, underscores and English / Chinese / Japanese / Korean characters, and must
     *     be between 4 and 12 characters in length.
     * </summary>
     * <code>^([a-zA-Z0-9_\u4e00-\u9fa5\u3040-\u309f\u30a0-\u30ff\uac00-\ud7af]{3,12})|([\u4e00-\u9fa5\u3040-\u309f\u30a0-\u30ff\uac00-\ud7af]{2,12})|([A-Za-z0-9_]{4,18})$</code>
     */
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

    /**
     * <summary>
     *     The input value is not a valid language code.
     * </summary>
     * <code>^[a-z]{2}(?:-[A-Z]{2})?$</code>
     */
    public const string InvalidLanguageCode = "InvalidLanguageCode";

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
    ///     The user does not have enough permission to perform the action.
    /// </summary>
    public const string InsufficientPermission = "InsufficientPermission";

    /// <summary>
    ///     The input value is not found in the Redis database.
    /// </summary>
    public const string InvalidActivationCode = "InvalidActivationCode";

    /// <summary>
    ///     User has already confirmed their email address and activated their account.
    /// </summary>
    public const string AlreadyActivated = "AlreadyActivated";
}
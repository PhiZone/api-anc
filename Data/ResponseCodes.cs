namespace PhiZoneApi.Data;

public static class ResponseCodes
{
    // Process was successful.
    public const string Ok = "Ok";

    // Data validation has failed.
    public const string DataInvalid = "DataInvalid";

    // User's account has been temporarily (only when DateAvailable is present) / permanently locked.
    public const string AccountLocked = "AccountLocked";

    // User with this name already exists.
    public const string UserNameOccupied = "UserNameOccupied";

    // User cannot change their user name during cooldown.
    public const string UserNameCoolDown = "UserNameCoolDown";

    // User with this email address already exists.
    public const string EmailOccupied = "EmailOccupied";

    // The password is incorrect.
    public const string PasswordIncorrect = "PasswordIncorrect";

    // The password should be at least 6 characters long,
    // contain at least one non-alphanumeric character,
    // have at least one lowercase letter, one uppercase letter, one digit,
    // and can have a maximum length of 18 characters.
    // ^(?=.*[^a-zA-Z0-9])(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9]).{6,18}$
    public const string InvalidPassword = "InvalidPassword";

    // An internal server error has occurred.
    public const string InternalError = "InternalError";

    // No value is present on the field.
    public const string FieldEmpty = "FieldEmpty";

    // The input value is not a valid email address.
    public const string InvalidEmailAddress = "InvalidEmailAddress";

    // The input value is not a valid URL.
    public const string InvalidUrl = "InvalidUrl";

    // The input value is not a valid phone number.
    public const string InvalidPhoneNumber = "InvalidPhoneNumber";

    // The input value is not a valid date.
    public const string InvalidDate = "InvalidDate";

    // The input value is not a valid language code.
    // ^[a-z]{2}-[A-Z]{2}$
    public const string InvalidLanguageCode = "InvalidLanguageCode";

    // The input value is longer than the maximum length allowed.
    public const string ValueTooLong = "ValueTooLong";

    // The input value is shorter than the minimum length allowed.
    public const string ValueTooShort = "ValueTooShort";

    // The input value is out of range.
    public const string ValueOutOfRange = "ValueOutOfRange";

    // The user is not logged in.
    public const string UserNotLoggedIn = "UserNotLoggedIn";

    // The user does not have enough permission to perform the action.
    public const string InsufficientPermission = "InsufficientPermission";
}
namespace PhiZoneApi.Enums;

public enum EditionType
{
    Original,                   // the original edition of the song
    EditedByAuthor,             // an edition of the song made by the author themselves
    EditedByFirstParty,         // a version edited by a first party (licensed by the author)
    EditedByUploaderLicensed,   // a version edited by the uploader (licensed by the author)
    EditedByUploaderUnlicensed, // a version edited by the uploader (not licensed by the author)
    EditedByThirdParty          // a version edited by a third party (not licensed by the author)
}
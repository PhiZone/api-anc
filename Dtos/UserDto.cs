namespace PhiZoneApi.Dtos
{
    public class UserDto
    {
        public int Id { get; set; }

        public required string UserName { get; set; }

        public required string Avatar { get; set; }

        public int Gender { get; set; }

        public string? Biography { get; set; }

        public double Experience { get; set; }

        public string? Tag { get; set; }

        public double Rks { get; set; }

        public required string Language { get; set; }

        public DateTime? DateLastLoggedIn { get; set; }

        public DateTime DateJoined { get; set; }

        public DateTime? DateOfBirth { get; set; }
    }
}

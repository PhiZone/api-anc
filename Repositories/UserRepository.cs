using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext context;

        public UserRepository(DataContext context)
        {
            this.context = context;
        }

        public ICollection<User> GetUsers()
        {
            return context.Users.OrderBy(p => p.Id).ToList();
        }

        public User? GetUser(int id)
        {
            return context.Users.FirstOrDefault(p => p.Id.Equals(id));
        }

        public User? GetUser(string name)
        {
            return context.Users.FirstOrDefault(p => p.UserName.Equals(name));
        }

        public bool UserExists(int id)
        {
            return context.Users.Any(p => p.Id.Equals(id));
        }

        public bool Save()
        {
            var saved = context.SaveChanges();
            return saved > 0;
        }

        public bool UpdateUser(User user)
        {
            context.Update(user);
            return Save();
        }
    }
}

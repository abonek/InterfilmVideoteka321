using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Service
{
    public class UserService : IUserService
    {
        private readonly BazaDbContext _context;

        public UserService(BazaDbContext context)
        {
            _context = context;
        }

        public IEnumerable<User> GetUsers() => _context.PopisKorisnika.ToList();

        public User GetUserbyId(int id) => _context.PopisKorisnika.Find(id);

        public User CreateUser(User user)
        {
            _context.PopisKorisnika.Add(user);
            _context.SaveChanges();
            return user;
        }

        public void UpdateUser(User user)
        {
            _context.Entry(user).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void DeleteUser(User user)
        {
            _context.PopisKorisnika.Remove(user);
            _context.SaveChanges();
        }
    }
}

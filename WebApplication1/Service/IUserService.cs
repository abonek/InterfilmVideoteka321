using WebApplication1.Models;

namespace WebApplication1.Service
{
    public interface IUserService
    {
        IEnumerable<WebApplication1.Models.User> GetUsers();
        User GetUserbyId(int id);
        User CreateUser(User create);
        void UpdateUser(User update);
        void DeleteUser(User student);
    }
}

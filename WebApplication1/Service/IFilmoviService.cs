using WebApplication1.Models;

namespace WebApplication1.Service
{
    public interface IFilmoviService
    {
        IEnumerable<WebApplication1.Models.Film> GetFilmovi();
        Film GetFilmbyId(int id);
        Film CreateFilm(Film create);
        void UpdateFilm(Film update);
        void DeleteFilm(Film student);
    }
}

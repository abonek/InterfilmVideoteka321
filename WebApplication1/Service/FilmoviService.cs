using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Service
{
    public class FilmoviService : IFilmoviService
    {
        private readonly BazaDbContext _context;

        public FilmoviService(BazaDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Film> GetFilmovi() => _context.PopisFilmova.ToList();

        public Film GetFilmbyId(int id) => _context.PopisFilmova.Find(id);

        public Film CreateFilm(Film film)
        {
            _context.PopisFilmova.Add(film);
            _context.SaveChanges();
            return film;
        }

        public void UpdateFilm(Film film)
        {
            _context.Entry(film).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void DeleteFilm(Film film)
        {
            _context.PopisFilmova.Remove(film);
            _context.SaveChanges();
        }
    }
}

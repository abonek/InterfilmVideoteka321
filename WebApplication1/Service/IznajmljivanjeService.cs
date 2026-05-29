using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Service
{
    public class IznajmljivanjeService : IIznajmljivanjeService
    {
        private readonly BazaDbContext _context;

        public IznajmljivanjeService(BazaDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Iznajmljivanje> GetByKorisnik(int korisnikId) =>
            _context.Iznajmljivanja.Where(i => i.KorisnikId == korisnikId).OrderByDescending(i => i.DatumPocetka).ToList();

        public IEnumerable<Iznajmljivanje> GetAktivne(int korisnikId) =>
            _context.Iznajmljivanja.Where(i => i.KorisnikId == korisnikId && !i.Vratio).ToList();

        public IEnumerable<Iznajmljivanje> GetAll() =>
            _context.Iznajmljivanja.OrderByDescending(i => i.DatumPocetka).ToList();

        public Iznajmljivanje? GetById(int id) =>
            _context.Iznajmljivanja.Find(id);

        public Iznajmljivanje Create(Iznajmljivanje iznajmljivanje)
        {
            _context.Iznajmljivanja.Add(iznajmljivanje);
            _context.SaveChanges();
            return iznajmljivanje;
        }

        public void Update(Iznajmljivanje iznajmljivanje)
        {
            _context.Iznajmljivanja.Update(iznajmljivanje);
            _context.SaveChanges();
        }

        public void Vrati(int id)
        {
            var zapis = _context.Iznajmljivanja.Find(id);
            if (zapis == null) return;
            zapis.Vratio = true;
            zapis.DatumVracanja = DateTime.UtcNow;
            _context.SaveChanges();
        }

        public void Otkazi(int id)
        {
            var zapis = _context.Iznajmljivanja.Find(id);
            if (zapis == null) return;
            zapis.Vratio = true;
            zapis.Otkazano = true;
            _context.SaveChanges();
        }
    }
}

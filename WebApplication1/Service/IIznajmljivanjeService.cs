using WebApplication1.Models;

namespace WebApplication1.Service
{
    public interface IIznajmljivanjeService
    {
        IEnumerable<Iznajmljivanje> GetByKorisnik(int korisnikId);
        IEnumerable<Iznajmljivanje> GetAktivne(int korisnikId);
        IEnumerable<Iznajmljivanje> GetAll();
        Iznajmljivanje? GetById(int id);
        Iznajmljivanje Create(Iznajmljivanje iznajmljivanje);
        void Update(Iznajmljivanje iznajmljivanje);
        void Vrati(int id);
        void Otkazi(int id);
    }
}

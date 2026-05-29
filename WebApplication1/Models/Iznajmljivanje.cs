using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    [Table("iznajmljivanja")]
    public class Iznajmljivanje
    {
        public int Id { get; set; }
        public int FilmId { get; set; }
        public int KorisnikId { get; set; }
        public string NazivFilma { get; set; }
        public float CijenaRente { get; set; }
        public DateTime DatumPocetka { get; set; }
        public DateTime? DatumPodizanja { get; set; }
        public DateTime DatumPovrata { get; set; }
        public int TipRezervacije { get; set; } = 0;
        public bool Vratio { get; set; } = false;
        public bool Podignut { get; set; } = false;
        public bool Otkazano { get; set; } = false;
        public float KasnjenjaNaknada { get; set; } = 0f;
        public DateTime? DatumVracanja { get; set; }
    }
}

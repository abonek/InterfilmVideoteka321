using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    [Table("filmovi")]
    public class Film
    {
        [Key]
    public int IdFilma { get; set; }

        
        public string NazivFilma { get; set; }
        

        
        public float CijenaKupnje { get; set; }

        
        public float CijenaRente { get; set; }

        public DateOnly DatumRente { get; set; }
        public int Rezervacija { get; set; }

        public int Kolicina { get; set; } = 1;

        public int GodinaSnimanja { get; set; }

        public int IdKupca { get; set; }

    }
}

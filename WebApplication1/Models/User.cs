using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    [Table("korisnici")]
    public class User
    {
        public int Id { get; set; }

        // Ime i prezime korisnika
        public string FullName { get; set; }

        // Email (unikatan u stvarnoj aplikaciji)
        public string Email { get; set; }

        // U proizvodnji spremati samo hash lozinke
        public string PasswordHash { get; set; }

        // true = administrator, false = običan korisnik
        public bool IsAdmin { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
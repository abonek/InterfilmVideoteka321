using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Email je obavezan")]
        [EmailAddress(ErrorMessage = "Neispravan email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Lozinka je obavezna")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public class RegisterModel
    {
        [Required(ErrorMessage = "Ime i prezime su obavezni")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email je obavezan")]
        [EmailAddress(ErrorMessage = "Neispravan email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Lozinka je obavezna")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Lozinka mora imati najmanje {2} znakova.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Lozinka je obavezna")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Lozinke se ne poklapaju.")]
        public string ConfirmPassword { get; set; }

    }

    public class ChangeEmailModel
    {
        [Required(ErrorMessage = "Trenutna lozinka je obavezna")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Nova e-mail adresa je obavezna")]
        [EmailAddress(ErrorMessage = "Neispravan format e-mail adrese")]
        public string NewEmail { get; set; }

        [Required(ErrorMessage = "Potvrda e-mail adrese je obavezna")]
        [EmailAddress]
        [Compare("NewEmail", ErrorMessage = "E-mail adrese se ne poklapaju.")]
        public string ConfirmEmail { get; set; }
    }

    public class ChangePasswordModel
    {
        [Required(ErrorMessage = "Trenutna lozinka je obavezna")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Nova lozinka je obavezna")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Lozinka mora imati najmanje {2} znakova.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Lozinke se ne poklapaju.")]
        public string ConfirmPassword { get; set; }
    }
}
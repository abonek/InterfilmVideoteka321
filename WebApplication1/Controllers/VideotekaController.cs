using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WebApplication1.Models;
using WebApplication1.Service;

namespace WebApplication1.Controllers
{
    public class VideotekaController : Controller
    {
        private readonly IUserService _userService;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IFilmoviService _filmoviService;
        private readonly IIznajmljivanjeService _iznajmljivanjeService;

        public VideotekaController(IUserService userService, IPasswordHasher<User> passwordHasher,
            IFilmoviService filmoviService, IIznajmljivanjeService iznajmljivanjeService)
        {
            _userService = userService;
            _passwordHasher = passwordHasher;
            _filmoviService = filmoviService;
            _iznajmljivanjeService = iznajmljivanjeService;
        }

        private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");
        private bool IsAdmin => HttpContext.Session.GetString("IsAdmin") == "true";

        private void AutoVratiIstekle(int userId)
        {
            var istekle = _iznajmljivanjeService.GetAktivne(userId)
                .Where(i => i.Podignut && i.DatumPovrata < DateTime.UtcNow).ToList();

            foreach (var zapis in istekle)
            {
                var film = _filmoviService.GetFilmbyId(zapis.FilmId);
                if (film != null)
                {
                    film.Rezervacija = Math.Max(0, film.Rezervacija - 1);
                    _filmoviService.UpdateFilm(film);
                }

                int kasniDana = (int)Math.Ceiling((DateTime.UtcNow - zapis.DatumPovrata).TotalDays);
                if (kasniDana > 0) zapis.KasnjenjaNaknada = kasniDana * 0.5f;

                _iznajmljivanjeService.Vrati(zapis.Id);
            }
        }

        // ── Pocetna ─────────────────────────────────────────────────────────
        public IActionResult Pocetna()
        {
            var userId = CurrentUserId;
            if (userId.HasValue) AutoVratiIstekle(userId.Value);

            var sviFilmovi = _filmoviService.GetFilmovi().ToList();
            var dostupni = sviFilmovi.Where(f => f.Kolicina > 0 && f.Rezervacija < f.Kolicina).ToList();

            var popularnost = _iznajmljivanjeService.GetAll()
                .Where(i => !i.Otkazano)
                .GroupBy(i => i.FilmId)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.UkupnoFilmova = sviFilmovi.Count;
            ViewBag.DostupnoFilmova = dostupni.Count;
            ViewBag.AktivneRezervacije = userId.HasValue
                ? _iznajmljivanjeService.GetAktivne(userId.Value).Count()
                : 0;
            ViewBag.IstaknutiFilmovi = dostupni
                .OrderByDescending(f => popularnost.GetValueOrDefault(f.IdFilma, 0))
                .Take(4).ToList();
            ViewBag.BrojIznajmljivanja = popularnost;

            return View("Pocetna");
        }

        // ── Pretrazi ─────────────────────────────────────────────────────────
        public IActionResult Pretrazi(string q, string sort = "az", int page = 1)
        {
            var userId = CurrentUserId;
            if (userId.HasValue) AutoVratiIstekle(userId.Value);

            const int pageSize = 12;

            var filmovi = _filmoviService.GetFilmovi().ToList();
            if (!string.IsNullOrWhiteSpace(q))
                filmovi = filmovi.Where(f => f.NazivFilma.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

            filmovi = sort switch
            {
                "za"           => filmovi.OrderByDescending(f => f.NazivFilma).ToList(),
                "renta-asc"    => filmovi.OrderBy(f => f.CijenaRente).ToList(),
                "renta-desc"   => filmovi.OrderByDescending(f => f.CijenaRente).ToList(),
                "kupnja-asc"   => filmovi.OrderBy(f => f.CijenaKupnje).ToList(),
                "kupnja-desc"  => filmovi.OrderByDescending(f => f.CijenaKupnje).ToList(),
                "godina-asc"   => filmovi.OrderBy(f => f.GodinaSnimanja).ToList(),
                "godina-desc"  => filmovi.OrderByDescending(f => f.GodinaSnimanja).ToList(),
                _              => filmovi.OrderBy(f => f.NazivFilma).ToList()
            };

            var ukupno = filmovi.Count;
            var ukupnoStr = (int)Math.Ceiling(ukupno / (double)pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, ukupnoStr)));

            ViewData["Query"] = q;
            ViewData["Sort"] = sort;
            ViewBag.TrenutnaStr = page;
            ViewBag.UkupnoStr = ukupnoStr;
            ViewBag.UkupnoFilmova = ukupno;
            ViewBag.MojiFilmovi = userId.HasValue
                ? _iznajmljivanjeService.GetAktivne(userId.Value).Select(i => i.FilmId).ToHashSet()
                : new HashSet<int>();

            ViewBag.BrojIznajmljivanja = _iznajmljivanjeService.GetAll()
                .Where(i => !i.Otkazano)
                .GroupBy(i => i.FilmId)
                .ToDictionary(g => g.Key, g => g.Count());

            return View(filmovi.Skip((page - 1) * pageSize).Take(pageSize).ToList());
        }

        // ── RezervacijaFilma (renta i kupnja) ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RezervacijaFilma(int filmId, DateTime datumPodizanja, int tip)
        {
            var userId = CurrentUserId;
            if (userId == null) return RedirectToAction("Login", "Home");

            if (datumPodizanja.Date < DateTime.Today)
            {
                TempData["Error"] = "Datum preuzimanja ne može biti u prošlosti.";
                return RedirectToAction("Pretrazi");
            }
            if (datumPodizanja.Date > DateTime.Today.AddDays(30))
            {
                TempData["Error"] = "Datum preuzimanja ne može biti više od 30 dana unaprijed.";
                return RedirectToAction("Pretrazi");
            }

            var film = _filmoviService.GetFilmbyId(filmId);
            if (film == null || film.Kolicina <= 0 || film.Rezervacija >= film.Kolicina)
            {
                TempData["Error"] = "Film nije dostupan.";
                return RedirectToAction("Pretrazi");
            }

            var sada = DateTime.UtcNow;
            var podizanjaUtc = DateTime.SpecifyKind(datumPodizanja.Date, DateTimeKind.Local).ToUniversalTime();

            var datumPovrata = tip == 0 ? podizanjaUtc.AddDays(7) : podizanjaUtc.AddDays(1);
            var cijenaBaza = tip == 0 ? film.CijenaRente : film.CijenaKupnje;
            var cijena = IsAdmin ? cijenaBaza * 0.9f : cijenaBaza;

            film.Rezervacija++;
            _filmoviService.UpdateFilm(film);

            _iznajmljivanjeService.Create(new Iznajmljivanje
            {
                FilmId = film.IdFilma,
                KorisnikId = userId.Value,
                NazivFilma = film.NazivFilma,
                CijenaRente = cijena,
                DatumPocetka = sada,
                DatumPodizanja = podizanjaUtc,
                DatumPovrata = datumPovrata,
                TipRezervacije = tip,
                Vratio = false
            });

            var tipTekst = tip == 0
                ? $"iznajmljen — preuzimanje {datumPodizanja:dd.MM.yyyy.}, povrat do {datumPovrata.ToLocalTime():dd.MM.yyyy.}"
                : $"rezerviran za kupnju — preuzimanje {datumPodizanja:dd.MM.yyyy.}";
            var popustTekst = IsAdmin ? " (admin popust −10%)" : "";

            TempData["Success"] = $"Film \"{film.NazivFilma}\" {tipTekst}. Cijena: {cijena:F2} €{popustTekst}.";
            return RedirectToAction("TrenutneRezervacije");
        }

        // ── VratiFilm ────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VratiFilm(int iznajmljivanjeId)
        {
            var userId = CurrentUserId;
            if (userId == null) return RedirectToAction("Login", "Home");

            var zapis = _iznajmljivanjeService.GetByKorisnik(userId.Value)
                .FirstOrDefault(i => i.Id == iznajmljivanjeId);

            if (zapis != null)
            {
                if (zapis.Podignut && !IsAdmin)
                {
                    TempData["Error"] = "Povrat filma potvrđuje admin u videoteci.";
                    return RedirectToAction("TrenutneRezervacije");
                }

                var film = _filmoviService.GetFilmbyId(zapis.FilmId);
                if (film != null)
                {
                    film.Rezervacija = Math.Max(0, film.Rezervacija - 1);
                    _filmoviService.UpdateFilm(film);
                }

                bool otkazano = !zapis.Podignut;

                if (!otkazano && DateTime.UtcNow > zapis.DatumPovrata)
                {
                    int kasniDana = (int)Math.Ceiling((DateTime.UtcNow - zapis.DatumPovrata).TotalDays);
                    if (kasniDana > 0) zapis.KasnjenjaNaknada = kasniDana * 0.5f;
                }

                if (otkazano)
                    _iznajmljivanjeService.Otkazi(iznajmljivanjeId);
                else
                    _iznajmljivanjeService.Vrati(iznajmljivanjeId);

                string poruka;
                if (otkazano)
                    poruka = $"Rezervacija za \"{zapis.NazivFilma}\" otkazana.";
                else if (zapis.KasnjenjaNaknada > 0)
                    poruka = $"Film \"{zapis.NazivFilma}\" vraćen. Naknada za kašnjenje: {zapis.KasnjenjaNaknada:F2} €";
                else
                    poruka = $"Film \"{zapis.NazivFilma}\" uspješno vraćen.";
                TempData["Success"] = poruka;
            }

            return RedirectToAction("TrenutneRezervacije");
        }

        // ── TrenutneRezervacije ───────────────────────────────────────────────
        public IActionResult TrenutneRezervacije()
        {
            var userId = CurrentUserId;
            if (userId == null) return RedirectToAction("Login", "Home");

            AutoVratiIstekle(userId.Value);
            var aktivne = _iznajmljivanjeService.GetAktivne(userId.Value).ToList();
            ViewBag.IsAdmin = IsAdmin;
            return View(aktivne);
        }

        // ── Povijest ─────────────────────────────────────────────────────────
        public IActionResult Povijest()
        {
            var userId = CurrentUserId;
            if (userId == null) return RedirectToAction("Login", "Home");

            var povijest = _iznajmljivanjeService.GetByKorisnik(userId.Value)
                .Where(i => i.Vratio).ToList();
            return View(povijest);
        }

        // ── Racun ─────────────────────────────────────────────────────────────
        public IActionResult Racun()
        {
            var userId = CurrentUserId;
            if (userId == null) return RedirectToAction("Login", "Home");

            var user = _userService.GetUserbyId(userId.Value);
            if (user == null) return RedirectToAction("Login", "Home");
            return View(user);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult PromijeniIme(string fullName)
        {
            var userId = CurrentUserId;
            if (userId == null) return RedirectToAction("Login", "Home");

            if (string.IsNullOrWhiteSpace(fullName))
            {
                TempData["ImeError"] = "Ime i prezime ne smiju biti prazni.";
                return RedirectToAction("Racun");
            }

            var user = _userService.GetUserbyId(userId.Value);
            user.FullName = fullName.Trim();
            _userService.UpdateUser(user);
            HttpContext.Session.SetString("UserName", user.FullName);
            TempData["ImeSuccess"] = "Ime uspješno promijenjeno.";
            return RedirectToAction("Racun");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult PromijeniEmail(ChangeEmailModel model)
        {
            var userId = CurrentUserId;
            if (userId == null) return RedirectToAction("Login", "Home");

            if (!ModelState.IsValid)
            {
                TempData["EmailError"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction("Racun");
            }

            var user = _userService.GetUserbyId(userId.Value);
            if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.CurrentPassword) != PasswordVerificationResult.Success)
            {
                TempData["EmailError"] = "Netočna trenutna lozinka.";
                return RedirectToAction("Racun");
            }

            if (_userService.GetUsers().Any(u => u.Email == model.NewEmail && u.Id != userId.Value))
            {
                TempData["EmailError"] = "Ta e-mail adresa već se koristi.";
                return RedirectToAction("Racun");
            }

            user.Email = model.NewEmail;
            _userService.UpdateUser(user);
            TempData["EmailSuccess"] = "E-mail adresa uspješno promijenjena.";
            return RedirectToAction("Racun");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult PromijeniLozinku(ChangePasswordModel model)
        {
            var userId = CurrentUserId;
            if (userId == null) return RedirectToAction("Login", "Home");

            if (!ModelState.IsValid)
            {
                TempData["LozinkaError"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction("Racun");
            }

            var user = _userService.GetUserbyId(userId.Value);
            if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.CurrentPassword) != PasswordVerificationResult.Success)
            {
                TempData["LozinkaError"] = "Netočna trenutna lozinka.";
                return RedirectToAction("Racun");
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
            _userService.UpdateUser(user);
            TempData["LozinkaSuccess"] = "Lozinka uspješno promijenjena.";
            return RedirectToAction("Racun");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ObrisiRacun(string password)
        {
            var userId = CurrentUserId;
            if (userId == null) return RedirectToAction("Login", "Home");

            if (string.IsNullOrWhiteSpace(password))
            {
                TempData["BrisanjeError"] = "Lozinka je obavezna za potvrdu brisanja.";
                return RedirectToAction("Racun");
            }

            var user = _userService.GetUserbyId(userId.Value);
            if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Success)
            {
                TempData["BrisanjeError"] = "Netočna lozinka. Račun nije obrisan.";
                return RedirectToAction("Racun");
            }

            _userService.DeleteUser(user);
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Home");
        }

        // ── Admin ────────────────────────────────────────────────────────────
        public IActionResult Admin(int pf = 1, int pk = 1, int pi = 1, string? qf = null, string? qk = null)
        {
            if (!IsAdmin) return RedirectToAction("Pocetna");

            const int pageSize = 10;

            var sviFilmovi = _filmoviService.GetFilmovi().OrderBy(f => f.NazivFilma).ToList();
            if (!string.IsNullOrWhiteSpace(qf))
                sviFilmovi = sviFilmovi.Where(f => f.NazivFilma.Contains(qf, StringComparison.OrdinalIgnoreCase)).ToList();

            var sviKorisnici = _userService.GetUsers().OrderBy(u => u.FullName).ToList();
            if (!string.IsNullOrWhiteSpace(qk))
                sviKorisnici = sviKorisnici.Where(u => u.FullName.Contains(qk, StringComparison.OrdinalIgnoreCase)).ToList();

            var svaIznajmljivanja = _iznajmljivanjeService.GetAll().ToList();

            int UkupnoStr(int ukupno) => (int)Math.Ceiling(ukupno / (double)pageSize);
            int ClampStr(int p, int ukupno) => Math.Max(1, Math.Min(p, Math.Max(1, UkupnoStr(ukupno))));

            pf = ClampStr(pf, sviFilmovi.Count);
            pk = ClampStr(pk, sviKorisnici.Count);
            pi = ClampStr(pi, svaIznajmljivanja.Count);

            ViewBag.Filmovi          = sviFilmovi.Skip((pf - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.FilmoviStr       = pf;
            ViewBag.FilmoviUkupnoStr = UkupnoStr(sviFilmovi.Count);

            ViewBag.Korisnici          = sviKorisnici.Skip((pk - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.KorisniciStr       = pk;
            ViewBag.KorisniciUkupnoStr = UkupnoStr(sviKorisnici.Count);

            ViewBag.Iznajmljivanja          = svaIznajmljivanja.Skip((pi - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.IznajmiStr              = pi;
            ViewBag.IznajmiUkupnoStr        = UkupnoStr(svaIznajmljivanja.Count);

            ViewBag.BrojIznajmljivanja = svaIznajmljivanja
                .Where(i => !i.Otkazano)
                .GroupBy(i => i.FilmId)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.QF = qf ?? string.Empty;
            ViewBag.QK = qk ?? string.Empty;

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DodajFilm(string nazovFilma, float cijenaKupnje, float cijenaRente, int kolicina = 1, int godinaSnimanja = 0)
        {
            if (!IsAdmin) return RedirectToAction("Pocetna");

            if (string.IsNullOrWhiteSpace(nazovFilma))
            {
                TempData["AdminError"] = "Naziv filma je obavezan.";
                return RedirectToAction("Admin");
            }

            _filmoviService.CreateFilm(new Film
            {
                NazivFilma = nazovFilma.Trim(),
                CijenaKupnje = cijenaKupnje,
                CijenaRente = cijenaRente,
                Kolicina = Math.Max(0, kolicina),
                GodinaSnimanja = godinaSnimanja,
                Rezervacija = 0,
                IdKupca = 0,
                DatumRente = DateOnly.MinValue
            });

            TempData["AdminSuccess"] = $"Film \"{nazovFilma.Trim()}\" uspješno dodan.";
            return RedirectToAction("Admin");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult UrediFilm(int filmId, string nazovFilma, float cijenaKupnje, float cijenaRente, int kolicina, int godinaSnimanja)
        {
            if (!IsAdmin) return RedirectToAction("Pocetna");

            var film = _filmoviService.GetFilmbyId(filmId);
            if (film == null)
            {
                TempData["AdminError"] = "Film nije pronađen.";
                return RedirectToAction("Admin");
            }

            if (string.IsNullOrWhiteSpace(nazovFilma))
            {
                TempData["AdminError"] = "Naziv filma je obavezan.";
                return RedirectToAction("Admin");
            }

            film.NazivFilma = nazovFilma.Trim();
            film.CijenaKupnje = cijenaKupnje;
            film.CijenaRente = cijenaRente;
            film.Kolicina = Math.Max(film.Rezervacija, kolicina);
            film.GodinaSnimanja = godinaSnimanja;
            _filmoviService.UpdateFilm(film);

            TempData["AdminSuccess"] = $"Film \"{film.NazivFilma}\" uspješno ažuriran.";
            return RedirectToAction("Admin");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ObrisiFilm(int filmId)
        {
            if (!IsAdmin) return RedirectToAction("Pocetna");

            var film = _filmoviService.GetFilmbyId(filmId);
            if (film != null)
            {
                _filmoviService.DeleteFilm(film);
                TempData["AdminSuccess"] = $"Film \"{film.NazivFilma}\" obrisan.";
            }
            return RedirectToAction("Admin");
        }

        // ── Admin: toggle admin privilegija ──────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ToggleAdmin(int userId)
        {
            if (!IsAdmin) return RedirectToAction("Pocetna");

            var korisnik = _userService.GetUserbyId(userId);
            if (korisnik == null || korisnik.Id == CurrentUserId)
            {
                TempData["AdminError"] = "Nije moguće promijeniti vlastite privilegije.";
                return RedirectToAction("Admin");
            }

            korisnik.IsAdmin = !korisnik.IsAdmin;
            _userService.UpdateUser(korisnik);

            var poruka = korisnik.IsAdmin
                ? $"{korisnik.FullName} sada je administrator."
                : $"{korisnik.FullName} više nije administrator.";
            TempData["AdminSuccess"] = poruka;
            return RedirectToAction("Admin");
        }

        // ── Admin: korisnikove rezervacije ───────────────────────────────────
        public IActionResult AdminKorisnikRezervacije(int userId)
        {
            if (!IsAdmin) return RedirectToAction("Pocetna");

            var korisnik = _userService.GetUserbyId(userId);
            if (korisnik == null) return RedirectToAction("Admin");

            var aktivne = _iznajmljivanjeService.GetAktivne(userId).ToList();
            ViewBag.Korisnik = korisnik;
            return View(aktivne);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AdminFilmPodignut(int iznajmljivanjeId)
        {
            if (!IsAdmin) return RedirectToAction("Pocetna");

            var zapis = _iznajmljivanjeService.GetById(iznajmljivanjeId);
            if (zapis == null || zapis.Vratio) return RedirectToAction("Admin");

            if (zapis.TipRezervacije == 1)
            {
                var film = _filmoviService.GetFilmbyId(zapis.FilmId);
                if (film != null)
                {
                    film.Kolicina = Math.Max(0, film.Kolicina - 1);
                    film.Rezervacija = Math.Max(0, film.Rezervacija - 1);
                    _filmoviService.UpdateFilm(film);
                }
                _iznajmljivanjeService.Vrati(iznajmljivanjeId);
                TempData["AdminSuccess"] = $"Film \"{zapis.NazivFilma}\" prodan. Preostalo primjeraka: {(film?.Kolicina ?? 0)}.";
            }
            else
            {
                zapis.Podignut = true;
                zapis.DatumPovrata = DateTime.UtcNow.AddDays(7);
                _iznajmljivanjeService.Update(zapis);
                TempData["AdminSuccess"] = $"Film \"{zapis.NazivFilma}\" podignut. Rok povrata: {zapis.DatumPovrata.ToLocalTime():dd.MM.yyyy.}";
            }

            return RedirectToAction("AdminKorisnikRezervacije", new { userId = zapis.KorisnikId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AdminOtkaziRezervaciju(int iznajmljivanjeId)
        {
            if (!IsAdmin) return RedirectToAction("Pocetna");

            var zapis = _iznajmljivanjeService.GetById(iznajmljivanjeId);
            if (zapis == null || zapis.Vratio) return RedirectToAction("Admin");

            var film = _filmoviService.GetFilmbyId(zapis.FilmId);
            if (film != null)
            {
                film.Rezervacija = Math.Max(0, film.Rezervacija - 1);
                _filmoviService.UpdateFilm(film);
            }
            _iznajmljivanjeService.Otkazi(iznajmljivanjeId);
            TempData["AdminSuccess"] = $"Rezervacija za \"{zapis.NazivFilma}\" otkazana.";
            return RedirectToAction("AdminKorisnikRezervacije", new { userId = zapis.KorisnikId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AdminFilmVracen(int iznajmljivanjeId)
        {
            if (!IsAdmin) return RedirectToAction("Pocetna");

            var zapis = _iznajmljivanjeService.GetById(iznajmljivanjeId);
            if (zapis == null || zapis.Vratio) return RedirectToAction("Admin");

            var film = _filmoviService.GetFilmbyId(zapis.FilmId);
            if (film != null)
            {
                film.Rezervacija = Math.Max(0, film.Rezervacija - 1);
                _filmoviService.UpdateFilm(film);
            }

            if (zapis.Podignut && DateTime.UtcNow > zapis.DatumPovrata)
            {
                int kasniDana = (int)Math.Ceiling((DateTime.UtcNow - zapis.DatumPovrata).TotalDays);
                if (kasniDana > 0) zapis.KasnjenjaNaknada = kasniDana * 0.5f;
            }

            _iznajmljivanjeService.Vrati(iznajmljivanjeId);
            var naknadaTekst = zapis.KasnjenjaNaknada > 0 ? $" Naknada za kašnjenje: {zapis.KasnjenjaNaknada:F2} €." : "";
            TempData["AdminSuccess"] = $"Film \"{zapis.NazivFilma}\" vraćen.{naknadaTekst}";
            return RedirectToAction("AdminKorisnikRezervacije", new { userId = zapis.KorisnikId });
        }

        // ── PDF: dostupni filmovi (svi korisnici) ────────────────────────────
        public IActionResult PreuzmiFilmovePdf()
        {
            var filmovi = _filmoviService.GetFilmovi().Where(f => f.Kolicina > 0).OrderBy(f => f.NazivFilma).ToList();
            var dostupnoUkupno = filmovi.Count(f => f.Rezervacija < f.Kolicina);
            var datum = DateTime.Now.ToString("dd.MM.yyyy. HH:mm");

            var bytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().BorderBottom(1).BorderColor("#dddddd").PaddingBottom(8).Row(row =>
                    {
                        row.RelativeItem().Text("Interfilm Videoteka").FontSize(16).Bold();
                        row.ConstantItem(200).AlignRight().Text($"Generirano: {datum}").FontSize(8).FontColor("#666666");
                    });

                    page.Content().PaddingTop(16).Column(col =>
                    {
                        col.Item().Text("Popis filmova").FontSize(13).Bold().FontColor("#1a73e8");
                        col.Item().PaddingTop(4).Text($"Ukupno naslova: {filmovi.Count} — trenutno dostupnih: {dostupnoUkupno}").FontSize(9).FontColor("#666666");
                        col.Item().PaddingTop(12).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(30);
                                c.RelativeColumn(3);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Background("#1a73e8").Padding(6).Text("#").FontColor("#ffffff").Bold();
                                h.Cell().Background("#1a73e8").Padding(6).Text("Naziv filma").FontColor("#ffffff").Bold();
                                h.Cell().Background("#1a73e8").Padding(6).Text("Renta").FontColor("#ffffff").Bold();
                                h.Cell().Background("#1a73e8").Padding(6).Text("Kupnja").FontColor("#ffffff").Bold();
                                h.Cell().Background("#1a73e8").Padding(6).Text("Dostupno").FontColor("#ffffff").Bold();
                            });

                            for (int i = 0; i < filmovi.Count; i++)
                            {
                                var f = filmovi[i];
                                var bg = i % 2 == 0 ? "#ffffff" : "#f5f5f5";
                                var dostupno = f.Kolicina - f.Rezervacija;
                                var dostupnoTekst = dostupno > 0 ? $"{dostupno} / {f.Kolicina}" : "Sve iznajmljeno";
                                var dostupnoBoja = dostupno > 0 ? "#000000" : "#cc0000";
                                table.Cell().Background(bg).Padding(6).Text((i + 1).ToString()).FontColor("#666666");
                                table.Cell().Background(bg).Padding(6).Text(f.NazivFilma);
                                table.Cell().Background(bg).Padding(6).Text($"{f.CijenaRente:F2} €");
                                table.Cell().Background(bg).Padding(6).Text($"{f.CijenaKupnje:F2} €");
                                table.Cell().Background(bg).Padding(6).Text(dostupnoTekst).FontColor(dostupnoBoja);
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Stranica ").FontSize(8).FontColor("#666666");
                        x.CurrentPageNumber().FontSize(8).FontColor("#666666");
                        x.Span(" od ").FontSize(8).FontColor("#666666");
                        x.TotalPages().FontSize(8).FontColor("#666666");
                    });
                });
            }).GeneratePdf();

            return File(bytes, "application/pdf", $"popis-filmova-{DateTime.Now:yyyyMMdd}.pdf");
        }

        // ── PDF: korisnici (samo admin) ───────────────────────────────────────
        public IActionResult PreuzmiKorisniciPdf()
        {
            if (!IsAdmin) return RedirectToAction("Pocetna");

            var korisnici = _userService.GetUsers().OrderBy(u => u.FullName).ToList();
            var datum = DateTime.Now.ToString("dd.MM.yyyy. HH:mm");

            var bytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().BorderBottom(1).BorderColor("#dddddd").PaddingBottom(8).Row(row =>
                    {
                        row.RelativeItem().Text("Interfilm Videoteka — Admin").FontSize(16).Bold();
                        row.ConstantItem(200).AlignRight().Text($"Generirano: {datum}").FontSize(8).FontColor("#666666");
                    });

                    page.Content().PaddingTop(16).Column(col =>
                    {
                        col.Item().Text("Popis korisničkih računa").FontSize(13).Bold().FontColor("#1a73e8");
                        col.Item().PaddingTop(4).Text($"Ukupno korisnika: {korisnici.Count}").FontSize(9).FontColor("#666666");
                        col.Item().PaddingTop(12).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(30);
                                c.RelativeColumn(2);
                                c.RelativeColumn(3);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Background("#1a73e8").Padding(6).Text("#").FontColor("#ffffff").Bold();
                                h.Cell().Background("#1a73e8").Padding(6).Text("Ime i prezime").FontColor("#ffffff").Bold();
                                h.Cell().Background("#1a73e8").Padding(6).Text("Email").FontColor("#ffffff").Bold();
                                h.Cell().Background("#1a73e8").Padding(6).Text("Registriran").FontColor("#ffffff").Bold();
                                h.Cell().Background("#1a73e8").Padding(6).Text("Uloga").FontColor("#ffffff").Bold();
                            });

                            for (int i = 0; i < korisnici.Count; i++)
                            {
                                var k = korisnici[i];
                                var bg = i % 2 == 0 ? "#ffffff" : "#f5f5f5";
                                table.Cell().Background(bg).Padding(6).Text((i + 1).ToString()).FontColor("#666666");
                                table.Cell().Background(bg).Padding(6).Text(k.FullName);
                                table.Cell().Background(bg).Padding(6).Text(k.Email);
                                table.Cell().Background(bg).Padding(6).Text(k.CreatedAt.ToString("dd.MM.yyyy.")).FontColor("#666666");
                                table.Cell().Background(bg).Padding(6).Text(k.IsAdmin ? "Admin" : "Korisnik").FontColor(k.IsAdmin ? "#1a73e8" : "#666666");
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Stranica ").FontSize(8).FontColor("#666666");
                        x.CurrentPageNumber().FontSize(8).FontColor("#666666");
                        x.Span(" od ").FontSize(8).FontColor("#666666");
                        x.TotalPages().FontSize(8).FontColor("#666666");
                    });
                });
            }).GeneratePdf();

            return File(bytes, "application/pdf", $"korisnici-{DateTime.Now:yyyyMMdd}.pdf");
        }
    }
}

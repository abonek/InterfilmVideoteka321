using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApplication1.Controllers;
using WebApplication1.Models;
using WebApplication1.Service;
using WebApplication1.Tests.Helpers;
using Xunit;

namespace WebApplication1.Tests
{
    public class VideotekaControllerTests
    {
        private readonly Mock<IUserService> _userService;
        private readonly Mock<IPasswordHasher<User>> _passwordHasher;
        private readonly Mock<IFilmoviService> _filmoviService;
        private readonly Mock<IIznajmljivanjeService> _iznajmljivanjeService;
        private readonly VideotekaController _controller;

        public VideotekaControllerTests()
        {
            _userService = new Mock<IUserService>();
            _passwordHasher = new Mock<IPasswordHasher<User>>();
            _filmoviService = new Mock<IFilmoviService>();
            _iznajmljivanjeService = new Mock<IIznajmljivanjeService>();
            _controller = new VideotekaController(
                _userService.Object,
                _passwordHasher.Object,
                _filmoviService.Object,
                _iznajmljivanjeService.Object);
            ControllerHelper.SetupController(_controller);
        }

        // ── Pocetna ─────────────────────────────────────────────────────────

        [Fact]
        public void Pocetna_BezSesije_VracaView()
        {
            _filmoviService.Setup(s => s.GetFilmovi()).Returns(new List<Film>());
            _iznajmljivanjeService.Setup(s => s.GetAll()).Returns(new List<Iznajmljivanje>());

            var result = _controller.Pocetna();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Pocetna_SSesijom_VracaView()
        {
            ControllerHelper.SetupController(_controller, userId: 1);
            _filmoviService.Setup(s => s.GetFilmovi()).Returns(new List<Film>());
            _iznajmljivanjeService.Setup(s => s.GetAll()).Returns(new List<Iznajmljivanje>());
            _iznajmljivanjeService.Setup(s => s.GetAktivne(1)).Returns(new List<Iznajmljivanje>());

            var result = _controller.Pocetna();

            Assert.IsType<ViewResult>(result);
        }

        // ── Pretrazi ─────────────────────────────────────────────────────────

        [Fact]
        public void Pretrazi_BezUpita_VracaSveFilmove()
        {
            var filmovi = new List<Film>
            {
                new Film { IdFilma = 1, NazivFilma = "Matrix" },
                new Film { IdFilma = 2, NazivFilma = "Avatar" }
            };
            _filmoviService.Setup(s => s.GetFilmovi()).Returns(filmovi);
            _iznajmljivanjeService.Setup(s => s.GetAll()).Returns(new List<Iznajmljivanje>());

            var result = _controller.Pretrazi("") as ViewResult;

            Assert.NotNull(result);
            var model = result.Model as List<Film>;
            Assert.NotNull(model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public void Pretrazi_SUpitom_FiltriraFilmove()
        {
            var filmovi = new List<Film>
            {
                new Film { IdFilma = 1, NazivFilma = "Matrix" },
                new Film { IdFilma = 2, NazivFilma = "Avatar" }
            };
            _filmoviService.Setup(s => s.GetFilmovi()).Returns(filmovi);
            _iznajmljivanjeService.Setup(s => s.GetAll()).Returns(new List<Iznajmljivanje>());

            var result = _controller.Pretrazi("matrix") as ViewResult;

            Assert.NotNull(result);
            var model = result.Model as List<Film>;
            Assert.NotNull(model);
            Assert.Single(model);
            Assert.Equal("Matrix", model[0].NazivFilma);
        }

        [Fact]
        public void Pretrazi_SortZA_SortiraNazivOpadajuce()
        {
            var filmovi = new List<Film>
            {
                new Film { IdFilma = 1, NazivFilma = "Avatar" },
                new Film { IdFilma = 2, NazivFilma = "Zorro" },
                new Film { IdFilma = 3, NazivFilma = "Matrix" }
            };
            _filmoviService.Setup(s => s.GetFilmovi()).Returns(filmovi);
            _iznajmljivanjeService.Setup(s => s.GetAll()).Returns(new List<Iznajmljivanje>());

            var result = _controller.Pretrazi("", sort: "za") as ViewResult;

            Assert.NotNull(result);
            var model = result.Model as List<Film>;
            Assert.NotNull(model);
            Assert.Equal("Zorro", model[0].NazivFilma);
            Assert.Equal("Matrix", model[1].NazivFilma);
            Assert.Equal("Avatar", model[2].NazivFilma);
        }

        [Fact]
        public void Pretrazi_SortGodinaAsc_SortiraPoGodiniRastuci()
        {
            var filmovi = new List<Film>
            {
                new Film { IdFilma = 1, NazivFilma = "Film A", GodinaSnimanja = 2020 },
                new Film { IdFilma = 2, NazivFilma = "Film B", GodinaSnimanja = 2000 },
                new Film { IdFilma = 3, NazivFilma = "Film C", GodinaSnimanja = 2010 }
            };
            _filmoviService.Setup(s => s.GetFilmovi()).Returns(filmovi);
            _iznajmljivanjeService.Setup(s => s.GetAll()).Returns(new List<Iznajmljivanje>());

            var result = _controller.Pretrazi("", sort: "godina-asc") as ViewResult;

            Assert.NotNull(result);
            var model = result.Model as List<Film>;
            Assert.NotNull(model);
            Assert.Equal(2000, model[0].GodinaSnimanja);
            Assert.Equal(2010, model[1].GodinaSnimanja);
            Assert.Equal(2020, model[2].GodinaSnimanja);
        }

        [Fact]
        public void Pretrazi_SortGodinaDesc_SortiraPoGodiniOpadajuce()
        {
            var filmovi = new List<Film>
            {
                new Film { IdFilma = 1, NazivFilma = "Film A", GodinaSnimanja = 2020 },
                new Film { IdFilma = 2, NazivFilma = "Film B", GodinaSnimanja = 2000 },
                new Film { IdFilma = 3, NazivFilma = "Film C", GodinaSnimanja = 2010 }
            };
            _filmoviService.Setup(s => s.GetFilmovi()).Returns(filmovi);
            _iznajmljivanjeService.Setup(s => s.GetAll()).Returns(new List<Iznajmljivanje>());

            var result = _controller.Pretrazi("", sort: "godina-desc") as ViewResult;

            Assert.NotNull(result);
            var model = result.Model as List<Film>;
            Assert.NotNull(model);
            Assert.Equal(2020, model[0].GodinaSnimanja);
            Assert.Equal(2010, model[1].GodinaSnimanja);
            Assert.Equal(2000, model[2].GodinaSnimanja);
        }

        // ── RezervacijaFilma ─────────────────────────────────────────────────

        [Fact]
        public void RezervacijaFilma_BezSesije_PreusmeraNaLogin()
        {
            var result = _controller.RezervacijaFilma(1, DateTime.Today, 0) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);
            Assert.Equal("Home", result.ControllerName);
        }

        [Fact]
        public void RezervacijaFilma_ProslostDatum_VracaGreskuIRedirect()
        {
            ControllerHelper.SetupController(_controller, userId: 1);

            var result = _controller.RezervacijaFilma(1, DateTime.Today.AddDays(-1), 0) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Pretrazi", result.ActionName);
            Assert.NotNull(_controller.TempData["Error"]);
        }

        [Fact]
        public void RezervacijaFilma_FilmNijePronadjen_VracaGreskuIRedirect()
        {
            ControllerHelper.SetupController(_controller, userId: 1);
            _filmoviService.Setup(s => s.GetFilmbyId(99)).Returns((Film?)null);

            var result = _controller.RezervacijaFilma(99, DateTime.Today, 0) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Pretrazi", result.ActionName);
            Assert.NotNull(_controller.TempData["Error"]);
        }

        [Fact]
        public void RezervacijaFilma_FilmNijeDostan_VracaGreskuIRedirect()
        {
            ControllerHelper.SetupController(_controller, userId: 1);
            var film = new Film { IdFilma = 1, NazivFilma = "Matrix", Kolicina = 2, Rezervacija = 2 };
            _filmoviService.Setup(s => s.GetFilmbyId(1)).Returns(film);

            var result = _controller.RezervacijaFilma(1, DateTime.Today, 0) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Pretrazi", result.ActionName);
            Assert.NotNull(_controller.TempData["Error"]);
        }

        [Fact]
        public void RezervacijaFilma_IspravniPodaci_KreiraRezervacijuIRedirect()
        {
            ControllerHelper.SetupController(_controller, userId: 1);
            var film = new Film { IdFilma = 1, NazivFilma = "Matrix", CijenaRente = 3.5f, Kolicina = 2, Rezervacija = 0 };
            _filmoviService.Setup(s => s.GetFilmbyId(1)).Returns(film);

            var result = _controller.RezervacijaFilma(1, DateTime.Today, 0) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("TrenutneRezervacije", result.ActionName);
            _iznajmljivanjeService.Verify(s => s.Create(It.Is<Iznajmljivanje>(i =>
                i.FilmId == 1 && i.KorisnikId == 1 && i.TipRezervacije == 0)), Times.Once);
            _filmoviService.Verify(s => s.UpdateFilm(It.Is<Film>(f => f.Rezervacija == 1)), Times.Once);
        }

        // ── VratiFilm ────────────────────────────────────────────────────────

        [Fact]
        public void VratiFilm_BezSesije_PreusmeraNaLogin()
        {
            var result = _controller.VratiFilm(1) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);
            Assert.Equal("Home", result.ControllerName);
        }

        [Fact]
        public void VratiFilm_NepodignutaRezervacija_OtkazujeRezervaciju()
        {
            ControllerHelper.SetupController(_controller, userId: 1);
            var zapis = new Iznajmljivanje { Id = 5, FilmId = 1, KorisnikId = 1, NazivFilma = "Matrix", Podignut = false };
            var film = new Film { IdFilma = 1, NazivFilma = "Matrix", Rezervacija = 1, Kolicina = 2 };
            _iznajmljivanjeService.Setup(s => s.GetByKorisnik(1)).Returns(new List<Iznajmljivanje> { zapis });
            _filmoviService.Setup(s => s.GetFilmbyId(1)).Returns(film);

            var result = _controller.VratiFilm(5) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("TrenutneRezervacije", result.ActionName);
            _iznajmljivanjeService.Verify(s => s.Otkazi(5), Times.Once);
            _iznajmljivanjeService.Verify(s => s.Vrati(5), Times.Never);
        }

        [Fact]
        public void VratiFilm_PodignutaRezervacija_VracaFilm()
        {
            ControllerHelper.SetupController(_controller, userId: 1, isAdmin: true);
            var zapis = new Iznajmljivanje { Id = 5, FilmId = 1, KorisnikId = 1, NazivFilma = "Matrix", Podignut = true };
            var film = new Film { IdFilma = 1, NazivFilma = "Matrix", Rezervacija = 1, Kolicina = 2 };
            _iznajmljivanjeService.Setup(s => s.GetByKorisnik(1)).Returns(new List<Iznajmljivanje> { zapis });
            _filmoviService.Setup(s => s.GetFilmbyId(1)).Returns(film);

            var result = _controller.VratiFilm(5) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("TrenutneRezervacije", result.ActionName);
            _iznajmljivanjeService.Verify(s => s.Vrati(5), Times.Once);
            _iznajmljivanjeService.Verify(s => s.Otkazi(5), Times.Never);
        }

        // ── TrenutneRezervacije ───────────────────────────────────────────────

        [Fact]
        public void TrenutneRezervacije_BezSesije_PreusmeraNaLogin()
        {
            var result = _controller.TrenutneRezervacije() as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);
            Assert.Equal("Home", result.ControllerName);
        }

        [Fact]
        public void TrenutneRezervacije_SSesijom_VracaView()
        {
            ControllerHelper.SetupController(_controller, userId: 1);
            _iznajmljivanjeService.Setup(s => s.GetAktivne(1)).Returns(new List<Iznajmljivanje>());

            var result = _controller.TrenutneRezervacije();

            Assert.IsType<ViewResult>(result);
        }

        // ── Povijest ─────────────────────────────────────────────────────────

        [Fact]
        public void Povijest_BezSesije_PreusmeraNaLogin()
        {
            var result = _controller.Povijest() as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);
            Assert.Equal("Home", result.ControllerName);
        }

        [Fact]
        public void Povijest_SSesijom_VracaView()
        {
            ControllerHelper.SetupController(_controller, userId: 1);
            _iznajmljivanjeService.Setup(s => s.GetByKorisnik(1)).Returns(new List<Iznajmljivanje>());

            var result = _controller.Povijest();

            Assert.IsType<ViewResult>(result);
        }

        // ── Racun ─────────────────────────────────────────────────────────────

        [Fact]
        public void Racun_BezSesije_PreusmeraNaLogin()
        {
            var result = _controller.Racun() as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);
            Assert.Equal("Home", result.ControllerName);
        }

        [Fact]
        public void Racun_SSesijom_VracaView()
        {
            ControllerHelper.SetupController(_controller, userId: 1);
            _userService.Setup(s => s.GetUserbyId(1)).Returns(new User { Id = 1, FullName = "Test", Email = "test@test.hr" });

            var result = _controller.Racun();

            Assert.IsType<ViewResult>(result);
        }

        // ── Admin ────────────────────────────────────────────────────────────

        [Fact]
        public void Admin_NijeAdmin_PreusmeraNaPocetna()
        {
            ControllerHelper.SetupController(_controller, userId: 1, isAdmin: false);

            var result = _controller.Admin() as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Pocetna", result.ActionName);
        }

        [Fact]
        public void Admin_JeAdmin_VracaView()
        {
            ControllerHelper.SetupController(_controller, userId: 1, isAdmin: true);
            _filmoviService.Setup(s => s.GetFilmovi()).Returns(new List<Film>());
            _userService.Setup(s => s.GetUsers()).Returns(new List<User>());
            _iznajmljivanjeService.Setup(s => s.GetAll()).Returns(new List<Iznajmljivanje>());

            var result = _controller.Admin();

            Assert.IsType<ViewResult>(result);
        }

        // ── DodajFilm ────────────────────────────────────────────────────────

        [Fact]
        public void DodajFilm_NijeAdmin_PreusmeraNaPocetna()
        {
            ControllerHelper.SetupController(_controller, userId: 1, isAdmin: false);

            var result = _controller.DodajFilm("Matrix", 9.99f, 2.5f) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Pocetna", result.ActionName);
        }

        [Fact]
        public void DodajFilm_PrazanNaziv_VracaGreskuIRedirect()
        {
            ControllerHelper.SetupController(_controller, userId: 1, isAdmin: true);

            var result = _controller.DodajFilm("   ", 9.99f, 2.5f) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Admin", result.ActionName);
            Assert.NotNull(_controller.TempData["AdminError"]);
        }

        [Fact]
        public void DodajFilm_IspravniPodaci_KreiraFilmIRedirect()
        {
            ControllerHelper.SetupController(_controller, userId: 1, isAdmin: true);

            var result = _controller.DodajFilm("Matrix", 9.99f, 2.5f, 3, 1999) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Admin", result.ActionName);
            _filmoviService.Verify(s => s.CreateFilm(It.Is<Film>(f =>
                f.NazivFilma == "Matrix" &&
                f.CijenaKupnje == 9.99f &&
                f.CijenaRente == 2.5f &&
                f.Kolicina == 3 &&
                f.GodinaSnimanja == 1999)), Times.Once);
        }

        // ── UrediFilm ────────────────────────────────────────────────────────

        [Fact]
        public void UrediFilm_NijeAdmin_PreusmeraNaPocetna()
        {
            ControllerHelper.SetupController(_controller, userId: 1, isAdmin: false);

            var result = _controller.UrediFilm(1, "Matrix", 9.99f, 2.5f, 3, 1999) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Pocetna", result.ActionName);
        }

        [Fact]
        public void UrediFilm_FilmNijePronadjen_VracaGreskuIRedirect()
        {
            ControllerHelper.SetupController(_controller, userId: 1, isAdmin: true);
            _filmoviService.Setup(s => s.GetFilmbyId(99)).Returns((Film?)null);

            var result = _controller.UrediFilm(99, "Matrix", 9.99f, 2.5f, 3, 1999) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Admin", result.ActionName);
            Assert.NotNull(_controller.TempData["AdminError"]);
        }

        [Fact]
        public void UrediFilm_IspravniPodaci_AzuriraFilmIRedirect()
        {
            ControllerHelper.SetupController(_controller, userId: 1, isAdmin: true);
            var film = new Film { IdFilma = 1, NazivFilma = "Stari naziv", CijenaKupnje = 5f, CijenaRente = 1f, Kolicina = 2, Rezervacija = 0 };
            _filmoviService.Setup(s => s.GetFilmbyId(1)).Returns(film);

            var result = _controller.UrediFilm(1, "Matrix", 9.99f, 2.5f, 3, 1999) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Admin", result.ActionName);
            _filmoviService.Verify(s => s.UpdateFilm(It.Is<Film>(f =>
                f.NazivFilma == "Matrix" &&
                f.GodinaSnimanja == 1999)), Times.Once);
        }

        // ── ObrisiFilm ───────────────────────────────────────────────────────

        [Fact]
        public void ObrisiFilm_NijeAdmin_PreusmeraNaPocetna()
        {
            ControllerHelper.SetupController(_controller, userId: 1, isAdmin: false);

            var result = _controller.ObrisiFilm(1) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Pocetna", result.ActionName);
        }

        [Fact]
        public void ObrisiFilm_IspravniFilm_BriseFilmIRedirect()
        {
            ControllerHelper.SetupController(_controller, userId: 1, isAdmin: true);
            var film = new Film { IdFilma = 1, NazivFilma = "Matrix" };
            _filmoviService.Setup(s => s.GetFilmbyId(1)).Returns(film);

            var result = _controller.ObrisiFilm(1) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Admin", result.ActionName);
            _filmoviService.Verify(s => s.DeleteFilm(film), Times.Once);
        }
    }
}

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
    public class HomeControllerTests
    {
        private readonly Mock<IUserService> _userService;
        private readonly Mock<IPasswordHasher<User>> _passwordHasher;
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            _userService = new Mock<IUserService>();
            _passwordHasher = new Mock<IPasswordHasher<User>>();
            _controller = new HomeController(_userService.Object, _passwordHasher.Object);
            ControllerHelper.SetupController(_controller);
        }

        // ── Index ────────────────────────────────────────────────────────────

        [Fact]
        public void Index_ReturnsViewResult()
        {
            var result = _controller.Index();

            Assert.IsType<ViewResult>(result);
        }

        // ── Login GET ────────────────────────────────────────────────────────

        [Fact]
        public void Login_Get_ReturnsViewWithLoginModel()
        {
            var result = _controller.Login() as ViewResult;

            Assert.NotNull(result);
            Assert.IsType<LoginModel>(result.Model);
        }

        // ── Login POST ───────────────────────────────────────────────────────

        [Fact]
        public void Login_Post_InvalidModel_ReturnsView()
        {
            _controller.ModelState.AddModelError("Email", "Obavezno");
            var model = new LoginModel();

            var result = _controller.Login(model);

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Login_Post_KorisnikNijePronadjen_VraceViewSGreškom()
        {
            _userService.Setup(s => s.GetUsers()).Returns(new List<User>());
            var model = new LoginModel { Email = "ne@postoji.hr", Password = "lozinka" };

            var result = _controller.Login(model) as ViewResult;

            Assert.NotNull(result);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public void Login_Post_PogresnaLozinka_VraceViewSGreškom()
        {
            var user = new User { Id = 1, Email = "test@test.hr", PasswordHash = "hash" };
            _userService.Setup(s => s.GetUsers()).Returns(new List<User> { user });
            _passwordHasher.Setup(h => h.VerifyHashedPassword(user, "hash", "pogresna"))
                           .Returns(PasswordVerificationResult.Failed);

            var model = new LoginModel { Email = "test@test.hr", Password = "pogresna" };

            var result = _controller.Login(model) as ViewResult;

            Assert.NotNull(result);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public void Login_Post_IspravneKredencijale_RedirectNaPocetna()
        {
            var user = new User { Id = 1, Email = "test@test.hr", PasswordHash = "hash", FullName = "Test" };
            _userService.Setup(s => s.GetUsers()).Returns(new List<User> { user });
            _passwordHasher.Setup(h => h.VerifyHashedPassword(user, "hash", "lozinka"))
                           .Returns(PasswordVerificationResult.Success);

            var model = new LoginModel { Email = "test@test.hr", Password = "lozinka" };

            var result = _controller.Login(model) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Pocetna", result.ActionName);
            Assert.Equal("Videoteka", result.ControllerName);
        }

        [Fact]
        public void Login_Post_StarijHash_RehashiraLozinkuIRedirect()
        {
            var user = new User { Id = 1, Email = "test@test.hr", PasswordHash = "stari_hash", FullName = "Test" };
            _userService.Setup(s => s.GetUsers()).Returns(new List<User> { user });
            _passwordHasher.Setup(h => h.VerifyHashedPassword(user, "stari_hash", "lozinka"))
                           .Returns(PasswordVerificationResult.SuccessRehashNeeded);
            _passwordHasher.Setup(h => h.HashPassword(user, "lozinka")).Returns("novi_hash");

            var model = new LoginModel { Email = "test@test.hr", Password = "lozinka" };

            var result = _controller.Login(model) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Pocetna", result.ActionName);
            _userService.Verify(s => s.UpdateUser(It.Is<User>(u => u.PasswordHash == "novi_hash")), Times.Once);
        }

        // ── Register GET ─────────────────────────────────────────────────────

        [Fact]
        public void Register_Get_ReturnsViewWithRegisterModel()
        {
            var result = _controller.Register() as ViewResult;

            Assert.NotNull(result);
            Assert.IsType<RegisterModel>(result.Model);
        }

        // ── Register POST ────────────────────────────────────────────────────

        [Fact]
        public void Register_Post_InvalidModel_ReturnsView()
        {
            _controller.ModelState.AddModelError("Email", "Obavezno");
            var model = new RegisterModel();

            var result = _controller.Register(model);

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Register_Post_EmailVecPostoji_VraceViewSGreškom()
        {
            var user = new User { Email = "postoji@test.hr" };
            _userService.Setup(s => s.GetUsers()).Returns(new List<User> { user });

            var model = new RegisterModel
            {
                FullName = "Marko Marić",
                Email = "postoji@test.hr",
                Password = "lozinka1",
                ConfirmPassword = "lozinka1"
            };

            var result = _controller.Register(model) as ViewResult;

            Assert.NotNull(result);
            Assert.True(_controller.ModelState.ContainsKey("Email"));
        }

        [Fact]
        public void Register_Post_ValidniPodaci_KreiraKorisnikaIRedirect()
        {
            _userService.Setup(s => s.GetUsers()).Returns(new List<User>());
            _passwordHasher.Setup(h => h.HashPassword(It.IsAny<User>(), "Lozinka1!")).Returns("hash");

            var model = new RegisterModel
            {
                FullName = "Novi Korisnik",
                Email = "novi@test.hr",
                Password = "Lozinka1!",
                ConfirmPassword = "Lozinka1!"
            };

            var result = _controller.Register(model) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);
            _userService.Verify(s => s.CreateUser(It.Is<User>(u =>
                u.Email == "novi@test.hr" &&
                u.FullName == "Novi Korisnik" &&
                !u.IsAdmin)), Times.Once);
        }

        // ── Logout ───────────────────────────────────────────────────────────

        [Fact]
        public void Logout_RedirectNaLogin()
        {
            var result = _controller.Logout() as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace WebApplication1.Tests.Helpers
{
    public class MockSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id => "mock-session";
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }

    public static class ControllerHelper
    {
        public static MockSession SetupController(Controller controller, int? userId = null, bool isAdmin = false)
        {
            var session = new MockSession();

            if (userId.HasValue)
            {
                session.SetInt32("UserId", userId.Value);
                session.SetString("IsAdmin", isAdmin ? "true" : "false");
                session.SetString("UserName", "Test Korisnik");
            }

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.Session).Returns(session);

            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Scheme).Returns("https");
            mockRequest.Setup(r => r.Host).Returns(new HostString("localhost"));
            mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);

            var tempData = new TempDataDictionary(mockHttpContext.Object, Mock.Of<ITempDataProvider>());

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };
            controller.TempData = tempData;

            return session;
        }
    }
}

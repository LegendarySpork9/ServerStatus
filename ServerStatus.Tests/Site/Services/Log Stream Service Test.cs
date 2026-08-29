// Copyright © - Unpublished - Toby Hunter
using ServerStatusSite.Models.Responses.Related;
using ServerStatusSite.Services;

namespace ServerStatus.Tests.Site.Services
{
    [TestClass]
    public class LogStreamServiceTest
    {
        [TestMethod]
        public async Task TestPublish_InvokesSubscribedHandler()
        {
            LogStreamService service = new();
            List<LogEntryModel>? received = null;

            service.Subscribe(
                "TestServer",
                logs =>
                {
                    received = logs;
                    return Task.CompletedTask;
                });

            List<LogEntryModel> logs =
            [
                new()
                {
                    Id = 1,
                    Timestamp = DateTime.UtcNow,
                    Level = "Info",
                    Type = "Tool",
                    Message = "Test"
                }
            ];

            await service.Publish(
                "TestServer",
                logs);

            Assert.IsNotNull(received);
            Assert.AreEqual(
                1,
                received.Count);
        }

        [TestMethod]
        public async Task TestPublish_DoesNotInvokeDifferentServer()
        {
            LogStreamService service = new();
            bool invoked = false;

            service.Subscribe(
                "ServerA",
                _ =>
                {
                    invoked = true;
                    return Task.CompletedTask;
                });

            await service.Publish(
                "ServerB",
                []);

            Assert.IsFalse(invoked);
        }

        [TestMethod]
        public async Task TestUnsubscribe_RemovesHandler()
        {
            LogStreamService service = new();
            int callCount = 0;

            Task Handler(List<LogEntryModel> _)
            {
                callCount++;
                return Task.CompletedTask;
            }

            service.Subscribe(
                "TestServer",
                Handler);

            await service.Publish(
                "TestServer",
                []);

            Assert.AreEqual(
                1,
                callCount);

            service.Unsubscribe(
                "TestServer",
                Handler);

            await service.Publish(
                "TestServer",
                []);

            Assert.AreEqual(
                1,
                callCount);
        }

        [TestMethod]
        public async Task TestPublish_InvokesMultipleHandlers()
        {
            LogStreamService service = new();
            int callCount = 0;

            service.Subscribe(
                "TestServer",
                _ =>
                {
                    callCount++;
                    return Task.CompletedTask;
                });

            service.Subscribe(
                "TestServer",
                _ =>
                {
                    callCount++;
                    return Task.CompletedTask;
                });

            await service.Publish(
                "TestServer",
                []);

            Assert.AreEqual(
                2,
                callCount);
        }

        [TestMethod]
        public async Task TestPublish_IsCaseInsensitive()
        {
            LogStreamService service = new();
            bool invoked = false;

            service.Subscribe(
                "TestServer",
                _ =>
                {
                    invoked = true;
                    return Task.CompletedTask;
                });

            await service.Publish(
                "testserver",
                []);

            Assert.IsTrue(invoked);
        }
    }
}

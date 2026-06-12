using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests
{
    public class ToolkitServicesTests
    {
        private interface ITestService
        {
            string Value { get; }
        }

        private interface ISecondTestService
        {
            string Value { get; }
        }

        private class TestService : ITestService
        {
            public TestService(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        [Fact]
        public void Register_WithNamedService_GetByNameReturnsRegisteredService()
        {
            var service = new TestService("named");
            Toolkit.Services.Register<ITestService>(service, "svc_named_1");

            var result = Toolkit.Services.Get<ITestService>("svc_named_1");

            Assert.Same(service, result);

            Toolkit.Services.UnregisterByName<ITestService>("svc_named_1");
        }

        [Fact]
        public void Register_WithMultipleServices_GetReturnsLastRegisteredService()
        {
            var first = new TestService("first");
            var second = new TestService("second");
            Toolkit.Services.Register<ITestService>(first);
            Toolkit.Services.Register<ITestService>(second);

            var result = Toolkit.Services.Get<ITestService>();

            Assert.Same(second, result);

            Toolkit.Services.Unregister<ITestService>(second);
            Toolkit.Services.Unregister<ITestService>(first);
        }

        [Fact]
        public void GetRequired_WithNameWhenMissing_ThrowsInvalidOperationException()
        {
            // Use a unique type that no other test registers to avoid static state pollution
            Assert.Throws<InvalidOperationException>(() => Toolkit.Services.GetRequired<ISecondTestService>("does_not_exist"));
        }

        [Fact]
        public void UnregisterByName_WhenRegistered_RemovesServiceAndReturnsTrue()
        {
            var service = new TestService("value");
            Toolkit.Services.Register<ITestService>(service, "svc_remove_1");

            var removed = Toolkit.Services.UnregisterByName<ITestService>("svc_remove_1");
            var result = Toolkit.Services.Get<ITestService>("svc_remove_1");

            Assert.True(removed);
            Assert.Null(result);
        }

        [Fact]
        public void Unregister_WhenServiceIsNamed_RemovesNamedMapping()
        {
            var service = new TestService("value");
            Toolkit.Services.Register<ITestService>(service, "svc_remove_2");

            var removed = Toolkit.Services.Unregister<ITestService>(service);
            var byName = Toolkit.Services.Get<ITestService>("svc_remove_2");

            Assert.True(removed);
            Assert.Null(byName);
        }

        [Fact]
        public void GetAll_WhenServicesRegistered_ReturnsTypedServices()
        {
            var a = new TestService("a");
            var b = new TestService("b");
            Toolkit.Services.Register<ITestService>(a);
            Toolkit.Services.Register<ITestService>(b);

            var all = Toolkit.Services.GetAll<ITestService>().ToList();

            Assert.Contains(a, all);
            Assert.Contains(b, all);

            Toolkit.Services.Unregister<ITestService>(b);
            Toolkit.Services.Unregister<ITestService>(a);
        }

        [Fact]
        public void GetAllNamed_WhenNamedServicesRegistered_ReturnsDictionaryWithNames()
        {
            var named = new TestService("named");
            Toolkit.Services.Register<ITestService>(named, "svc_named_2");

            var allNamed = Toolkit.Services.GetAllNamed<ITestService>();

            Assert.True(allNamed.ContainsKey("svc_named_2"));
            Assert.Same(named, allNamed["svc_named_2"]);

            Toolkit.Services.UnregisterByName<ITestService>("svc_named_2");
        }

        [Fact]
        public void GetRequired_WhenServiceExists_ReturnsService()
        {
            var service = new TestService("req");
            Toolkit.Services.Register<ITestService>(service);

            var result = Toolkit.Services.GetRequired<ITestService>();

            Assert.Same(service, result);

            Toolkit.Services.Unregister<ITestService>(service);
        }
    }
}

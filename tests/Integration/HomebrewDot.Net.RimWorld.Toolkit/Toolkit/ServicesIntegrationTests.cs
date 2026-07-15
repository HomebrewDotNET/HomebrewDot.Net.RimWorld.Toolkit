using System;
using System.Linq;
using Xunit;
using ToolkitImpl = HomebrewDot.Net.Rimworld.Toolkit;

namespace HomebrewDot.Net.RimWorld.Tests.ToolkitIntegration
{
    [Trait("Category", "Integration")]
    public class ServicesIntegrationTests
    {
        public ServicesIntegrationTests()
        {
            ToolkitImpl.ConfigureServices();
        }

        private interface IDummy
        {
            string Tag { get; }
        }

        private sealed class Dummy : IDummy
        {
            public Dummy(string tag) { Tag = tag; }
            public string Tag { get; }
        }

        private interface ISecondDummy
        {
            string Tag { get; }
        }

        private sealed class SecondDummy : ISecondDummy
        {
            public SecondDummy(string tag) { Tag = tag; }
            public string Tag { get; }
        }

        [Fact]
        public void Services_Register_WithName_ThenGetByName_ReturnsSameInstance()
        {
            var service = new Dummy("alpha");
            ToolkitImpl.Services.Register<IDummy>(service, "svc_alpha_1");
            var result = ToolkitImpl.Services.Get<IDummy>("svc_alpha_1");
            Assert.Same(service, result);
            ToolkitImpl.Services.UnregisterByName<IDummy>("svc_alpha_1");
        }

        [Fact]
        public void Services_Register_MultipleSameType_ThenGetAll_ReturnsAll()
        {
            var a = new Dummy("a");
            var b = new Dummy("b");
            var c = new Dummy("c");
            try
            {
                ToolkitImpl.Services.Register<IDummy>(a, "svc_multi_a");
                ToolkitImpl.Services.Register<IDummy>(b, "svc_multi_b");
                ToolkitImpl.Services.Register<IDummy>(c, "svc_multi_c");
                var allNamed = ToolkitImpl.Services.GetAllNamed<IDummy>();

                Assert.True(allNamed.ContainsKey("svc_multi_a"));
                Assert.True(allNamed.ContainsKey("svc_multi_b"));
                Assert.True(allNamed.ContainsKey("svc_multi_c"));
                Assert.Same(a, allNamed["svc_multi_a"]);
                Assert.Same(b, allNamed["svc_multi_b"]);
                Assert.Same(c, allNamed["svc_multi_c"]);
            }
            finally
            {
                ToolkitImpl.Services.UnregisterByName<IDummy>("svc_multi_a");
                ToolkitImpl.Services.UnregisterByName<IDummy>("svc_multi_b");
                ToolkitImpl.Services.UnregisterByName<IDummy>("svc_multi_c");
            }
        }

        [Fact]
        public void Services_Unregister_ByName_RemovesEntry()
        {
            var service = new Dummy("to-remove");
            ToolkitImpl.Services.Register<IDummy>(service, "svc_remove_x");
            var removed = ToolkitImpl.Services.UnregisterByName<IDummy>("svc_remove_x");
            var result = ToolkitImpl.Services.Get<IDummy>("svc_remove_x");
            Assert.True(removed);
            Assert.Null(result);
        }

        [Fact]
        public void Services_GetAllNamed_WithCaseInsensitiveNames_ReturnsAll()
        {
            var service = new Dummy("ci");
            ToolkitImpl.Services.Register<IDummy>(service, "svc_case_test");
            try
            {
                var all = ToolkitImpl.Services.GetAllNamed<IDummy>();
                Assert.True(all.ContainsKey("svc_case_test"));
                Assert.True(all.ContainsKey("SVC_CASE_TEST"));
            }
            finally
            {
                ToolkitImpl.Services.UnregisterByName<IDummy>("svc_case_test");
            }
        }

        [Fact]
        public void Services_GetRequired_WithMissing_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => ToolkitImpl.Services.GetRequired<ISecondDummy>("does_not_exist_for_second_dummy"));
        }

        [Fact]
        public void Services_GetAll_OnlyUnnamed_AfterSingleRegister()
        {
            var service = new SecondDummy("only-unnamed");
            try
            {
                ToolkitImpl.Services.Register<ISecondDummy>(service);
                var all = ToolkitImpl.Services.GetAll<ISecondDummy>().ToList();
                Assert.Contains(service, all);
            }
            finally
            {
                ToolkitImpl.Services.Unregister<ISecondDummy>(service);
            }
        }

        [Fact]
        public void Services_RegisterAndUnregister_Roundtrip_NoLeaks()
        {
            var service = new Dummy("roundtrip");
            ToolkitImpl.Services.Register<IDummy>(service, "svc_roundtrip");
            ToolkitImpl.Services.UnregisterByName<IDummy>("svc_roundtrip");
            var afterUnregister = ToolkitImpl.Services.Get<IDummy>("svc_roundtrip");
            Assert.Null(afterUnregister);
        }
    }
}

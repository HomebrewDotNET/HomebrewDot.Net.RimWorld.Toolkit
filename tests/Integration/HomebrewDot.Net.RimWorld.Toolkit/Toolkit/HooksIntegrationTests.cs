using System;
using HomebrewDot.Net.Rimworld.Hooks;
using Xunit;
using ToolkitImpl = HomebrewDot.Net.Rimworld.Toolkit;

namespace HomebrewDot.Net.RimWorld.Tests.ToolkitIntegration
{
    [Trait("Category", "Integration")]
    public class HooksIntegrationTests : IDisposable
    {
        public HooksIntegrationTests()
        {
            ToolkitImpl.ConfigureServices();
        }

        public void Dispose()
        {
            InvokeSafe(() => ToolkitImpl.Hooks.ReloadManager());
        }

        private static void InvokeSafe(Action action) { try { action(); } catch { } }

        public sealed class TestTrigger
        {
            public TestTrigger(string data) { Data = data; }
            public string Data { get; }
        }

        [Fact]
        public void Hooks_RegisterHook_ThenTrigger_CallsDelegate()
        {
            int callCount = 0;
            var owner = new object();
            ToolkitImpl.Hooks.Manager.RegisterHook<TestTrigger>(owner, e => { callCount++; });

            ToolkitImpl.Hooks.Manager.Trigger(new TestTrigger("hi"));

            Assert.Equal(1, callCount);
        }

        [Fact]
        public void Hooks_RegisterHook_WithPriority_HigherPriorityFiresFirst()
        {
            string order = "";
            var owner = new object();
            ToolkitImpl.Hooks.Manager.RegisterHook<TestTrigger>(owner, e => order += "low", priority: 200);
            ToolkitImpl.Hooks.Manager.RegisterHook<TestTrigger>(owner, e => order += "high", priority: 10);

            ToolkitImpl.Hooks.Manager.Trigger(new TestTrigger("ordering"));

            Assert.Equal("highlow", order);
        }

        [Fact]
        public void Hooks_Trigger_WhenNoHooks_DoesNotThrow()
        {
            ToolkitImpl.Hooks.ReloadManager();
            var ex = Record.Exception(() => ToolkitImpl.Hooks.Manager.Trigger(new TestTrigger("nobody")));
            Assert.Null(ex);
        }

        [Fact]
        public void Hooks_ReloadManager_DisposesOldManager()
        {
            var firstManager = ToolkitImpl.Hooks.Manager;
            ToolkitImpl.Hooks.ReloadManager();
            var secondManager = ToolkitImpl.Hooks.Manager;

            Assert.NotSame(firstManager, secondManager);
        }

        [Fact]
        public void Hooks_RegisterHook_Once_OnlyFiresOnce()
        {
            int callCount = 0;
            var owner = new object();
            ToolkitImpl.Hooks.Manager.RegisterHook<TestTrigger>(owner, e => { callCount++; return true; }, once: true);

            ToolkitImpl.Hooks.Manager.Trigger(new TestTrigger("once"));
            ToolkitImpl.Hooks.Manager.Trigger(new TestTrigger("again"));

            Assert.Equal(1, callCount);
        }
    }
}

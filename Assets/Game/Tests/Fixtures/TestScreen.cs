using System.Collections.Generic;
using NinetyNine.UI;

namespace NinetyNine.Tests
{
    /// <summary>Records its lifecycle so navigator tests can assert order and arguments.</summary>
    public class TestScreen : UIScreen
    {
        public readonly List<string> Log = new();
        public object Args;

        protected override void OnCreated() => Log.Add("created");

        protected override void OnOpening(object args)
        {
            Args = args;
            Log.Add("opening");
        }

        protected override void OnOpened() => Log.Add("opened");
        protected override void OnClosing() => Log.Add("closing");
        protected override void OnClosed() => Log.Add("closed");
        protected override void OnCovered() => Log.Add("covered");
        protected override void OnRevealed() => Log.Add("revealed");
    }
}

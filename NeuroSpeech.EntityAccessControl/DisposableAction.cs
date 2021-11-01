using System;

namespace NeuroSpeech.EntityAccessControl
{
    public class DisposableAction : IDisposable
    {
        private Action? action;

        public DisposableAction(Action? action)
        {
            this.action = action;
        }

        public void Dispose()
        {
            action?.Invoke();
            action = null;
        }
    }
}

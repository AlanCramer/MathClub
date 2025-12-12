using System;
using System.Diagnostics;
using System.Windows.Media;

namespace GeomLibTest.Animation
{
    public sealed class RenderLoopAnimator : IDisposable
    {
        private readonly Stopwatch _sw = new();
        private EventHandler? _handler;

        public bool IsRunning { get; private set; }

        public void Start(double durationSeconds, Action<double> onTick, Action? onFinished = null)
        {
            if (onTick == null) throw new ArgumentNullException(nameof(onTick));

            Stop();

            _sw.Restart();
            IsRunning = true;

            _handler = (_, __) =>
            {
                double elapsed = _sw.Elapsed.TotalSeconds;
                double t = durationSeconds <= 0 ? 1.0 : Math.Min(1.0, elapsed / durationSeconds);

                onTick(t);

                if (t >= 1.0)
                {
                    Stop();
                    onFinished?.Invoke();
                }
            };

            CompositionTarget.Rendering += _handler;
        }

        public void Stop()
        {
            if (_handler != null)
                CompositionTarget.Rendering -= _handler;

            _handler = null;
            _sw.Stop();
            IsRunning = false;
        }

        public void Dispose() => Stop();
    }
}

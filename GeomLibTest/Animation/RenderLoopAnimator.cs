using System.Diagnostics;
using System.Windows.Media;

namespace GeomLibTest.Animation
{
    /// <summary>
    /// Animator that uses WPF's CompositionTarget.Rendering event as the render loop.
    /// </summary>
    public sealed class RenderLoopAnimator : IDisposable
    {
        private readonly Stopwatch _sw = new();
        private readonly EventHandler _handler;

        private Action<double>? _onTick;
        private Action? _onFinished;
        private double _durationSeconds;

        public bool IsRunning { get; private set; }

        public RenderLoopAnimator()
        {
            _handler = OnRendering;
        }

        public void Start(double durationSeconds, Action<double> onTick, Action? onFinished = null)
        {
            if (onTick == null) throw new ArgumentNullException(nameof(onTick));

            Stop();

            _durationSeconds = durationSeconds;
            _onTick = onTick;
            _onFinished = onFinished;

            _sw.Restart();
            IsRunning = true;

            CompositionTarget.Rendering += _handler;
        }

        public void Stop()
        {
            if (!IsRunning) return;

            CompositionTarget.Rendering -= _handler;
            IsRunning = false;
            _onFinished?.Invoke();

            _sw.Stop();
            _onTick = null;
            _onFinished = null;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (!IsRunning || _onTick == null) return;

            double elapsed = _sw.Elapsed.TotalSeconds;
            double t = _durationSeconds <= 0 ? 1.0 : Math.Min(1.0, elapsed / _durationSeconds);

            _onTick(t);

            if (t >= 1.0)
            {
                Stop();
                _onFinished?.Invoke();
            }
        }

        public void Dispose() => Stop();
    }
}

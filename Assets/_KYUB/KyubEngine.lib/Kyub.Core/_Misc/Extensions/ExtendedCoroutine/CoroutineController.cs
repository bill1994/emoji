using System.Collections;

namespace Kyub
{
    public enum CoroutineState
    {
        Ready,
        Running,
        Paused,
        Finished
    }

    public class CoroutineController
    {
        private IEnumerator _routine;

        System.DateTime _startTime = System.DateTime.MinValue;

        public float accumulatedTime
        {
            get
            {
                if (_startTime == System.DateTime.MinValue)
                    return 0;
                return (float)(System.DateTime.Now - _startTime).TotalSeconds;
            }
        }

        public CoroutineState state;

        public CoroutineController(IEnumerator routine)
        {
            _routine = routine;
            state = CoroutineState.Ready;
        }

        public IEnumerator Start()
        {
            if (state != CoroutineState.Ready)
            {
                throw new System.InvalidOperationException("Unable to start coroutine in state: " + state);
            }
            _startTime = System.DateTime.Now;

            state = CoroutineState.Running;
            while (_routine.MoveNext())
            {
                yield return _routine.Current;

                while (state == CoroutineState.Paused)
                {
                    yield return null;
                }
                if (state == CoroutineState.Finished)
                {
                    yield break;
                }
            }

            _startTime = System.DateTime.MinValue;
            state = CoroutineState.Finished;
        }

        public void Stop()
        {
            if (state != CoroutineState.Running && state != CoroutineState.Paused)
            {
                throw new System.InvalidOperationException("Unable to stop coroutine in state: " + state);
            }

            _startTime = System.DateTime.MinValue;
            state = CoroutineState.Finished;
        }

        public void Pause()
        {
            if (state != CoroutineState.Running)
            {
                throw new System.InvalidOperationException("Unable to pause coroutine in state: " + state);
            }

            state = CoroutineState.Paused;
        }

        public void Resume()
        {
            if (state != CoroutineState.Paused)
            {
                throw new System.InvalidOperationException("Unable to resume coroutine in state: " + state);
            }

            state = CoroutineState.Running;
        }
    }
}
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace Tasks
{
    internal class HealthMonitor
    {
#if UNITY_EDITOR
        private static bool editorStateChanged;
#endif
        
        private readonly object locker = new object();
        
        private Stopwatch stopWatch;
        private float     duration;
        private string    name;

        public HealthMonitor(float duration)
        {
#if UNITY_EDITOR
            editorStateChanged = false;
#endif
            
            this.duration = duration;
            stopWatch = new Stopwatch();
            UpdateRoutine();
        }


        public void Reset(string name = null)
        {
            lock (locker)
            {
                this.name = name;
                stopWatch.Restart();
            }
        }
        
        public void Stop()
        {
            lock (locker)
            {
                stopWatch = null;
            }
        }

        private async void UpdateRoutine()
        {
            while (true)
            {
#if UNITY_EDITOR
                if (editorStateChanged)
                    break;
#endif
                
                lock (locker)
                {
                    if (stopWatch == null)
                        break;

                    float elapsed = stopWatch.ElapsedMilliseconds;
                    if (elapsed > duration)
                    {
                        UnityEngine.Debug.LogError($"Health monitor timeout: {name}");
                        break;
                    }
                }
                
                await Task.Delay(1000);
            }
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void OnApplicationStarted()
        {
            //UnityEditor.EditorApplication.isPlaying
            
            UnityEditor.EditorApplication.playModeStateChanged += change =>
            {
                editorStateChanged = true;
            };
        }
#endif
    }
}

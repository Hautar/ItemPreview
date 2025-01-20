using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Tasks
{
    public class CoroutineStarter : MonoBehaviour
    {
        private class CoroutineContext
        {
            public readonly IEnumerator Enumerator;
            public readonly string      Name;
            public readonly string      CallStack;
            public readonly Action      OnComplete;
            public readonly Action      OnFailed;
            public readonly bool        EnableLogs;

            public Coroutine Routine;
            public float     Started;

            public CoroutineContext(string name, IEnumerator enumerator, Action onComplete, Action onFailed, bool enableLogs)
            {
                Enumerator = enumerator;
                Name       = name;
                OnComplete = onComplete;
                OnFailed   = onFailed;
                EnableLogs = enableLogs;
                
#if UNITY_EDITOR || DEBUG_MENU
                CallStack = Environment.StackTrace;
#else
                CallStack = "";
#endif
            }
        }
        
        private readonly ConcurrentQueue<CoroutineContext> pendingContexts = new ();
        private readonly List<CoroutineContext> activeContexts = new ();
        private const float CoroutineWarningTime = 60f;
        private const float CoroutineLifeTime    = 120f;
        private const float CoroutineCheckPeriod = 20f;

        private float nextCoroutineCheckTimestamp;

        private static CoroutineStarter instance;

        private static CoroutineStarter Instance
        {
            get
            {
                if (instance == null)
                    CreateInstance();
                return instance;
            }
        }

        private static void CreateInstance()
        {
            GameObject go = new GameObject("CoroutineStarter Root");
            go.hideFlags = HideFlags.HideAndDontSave;
            instance = go.AddComponent<CoroutineStarter>();
            
#if UNITY_EDITOR
            if(UnityEditor.EditorApplication.isPlaying)
#endif
                DontDestroyOnLoad(instance.gameObject);
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            CreateInstance();
        }

        public static void Schedule(string name, IEnumerator enumerator, Action onComplete = null, Action onFailed = null, bool enableLogs = true) =>
            Instance.ScheduleInternal(name, enumerator, onComplete, onFailed, enableLogs);
        
        private void ScheduleInternal(string name, IEnumerator enumerator, Action onComplete, Action onFailed, bool enableLogs)
        {
            var context = new CoroutineContext(name, enumerator, onComplete, onFailed, enableLogs);
            pendingContexts.Enqueue(context);
        }

        private IEnumerator CoroutineWrapper(CoroutineContext context)
        {
            yield return context.Enumerator;
            activeContexts.Remove(context);
            context.OnComplete?.Invoke();
        }

        private void Start()
        {
            UpdateCheckTimestamp(Time.realtimeSinceStartup);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private void Update()
        {
            float now = Time.realtimeSinceStartup;
            StartPendingRoutines(now);

            if (now < nextCoroutineCheckTimestamp)
                return;
            
            UpdateCheckTimestamp(now);
            CheckCoroutineLifeTime(now);
        }

        private void StartPendingRoutines(float now)
        {
            while (pendingContexts.TryDequeue(out var context))
            {
                activeContexts.Add(context);
                context.Started = now;
                context.Routine = StartCoroutine(CoroutineWrapper(context));
            }
        }
        
        private void CheckCoroutineLifeTime(float now)
        {
            for (int i = activeContexts.Count - 1; i >= 0; i--)
            {
                var context = activeContexts[i];
                if (now - context.Started > CoroutineLifeTime)
                {
                    try
                    {
                        activeContexts.RemoveAt(i);
                        if (context.EnableLogs)
                            Debug.LogError($"Aborting coroutine {context.Name} {context.CallStack} because it is running too long or crashed");
                        context.OnFailed?.Invoke();
                        StopCoroutine(context.Routine);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                else if (now - context.Started > CoroutineWarningTime)
                {
                    if (context.EnableLogs)
                        Debug.LogWarning($"Coroutine {context.Name} {context.CallStack} may be aborted because it is running too long.");
                }
            }
        }

        private void UpdateCheckTimestamp(float now)
        {
            nextCoroutineCheckTimestamp = now + CoroutineCheckPeriod;
        }
    }
}

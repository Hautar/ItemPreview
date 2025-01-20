using System;
using System.Threading;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tasks
{
    public static class MainThreadRunner
    {
        private static SynchronizationContext mainThreadSynchronizationContext;
        //private static int mainThreadId;
        private static Thread mainThread;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void CatchMainThreadSynchronizationContext()
        {
            //TODO: >>>>> replace this across all project with IsMainThread!
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "CurrentThread.ManagedThreadId != 1");
            mainThreadSynchronizationContext = SynchronizationContext.Current;
            mainThread = Thread.CurrentThread;
        }
        
        public static bool IsMainThread() => Thread.CurrentThread == mainThread;

        public static void ExecuteAsync(Action action, Action onException = null)
        {
#if UNITY_EDITOR
            if (mainThreadSynchronizationContext == null)
            {
                Debug.Log("Unity editor main thread callback requested");
                EditorApplication.CallbackFunction del = null;
                
                del = () =>
                {
                    Debug.Assert(IsMainThread(), "IsMainThread()");
                    Debug.Assert(!EditorApplication.isPlaying, "!EditorApplication.isPlaying");
                    EditorApplication.update -= del;
                    
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        onException?.Invoke();
                    }
                };

                EditorApplication.update += del;
                
                return;
            }
#endif
            
#if !UNITY_EDITOR
            Debug.LogError("Please dont use this feature outside editor");
#endif
            
            mainThreadSynchronizationContext.Post(_ =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    onException?.Invoke();
                }
            }, null);
        }

        public static void ExecuteAsync<T1>(Action<T1> action, T1 param1, Action onException = null)
        {
#if UNITY_EDITOR
            if (mainThreadSynchronizationContext == null)
            {
                Debug.Log("Unity editor main thread callback requested");
                EditorApplication.CallbackFunction del = null;
                
                del = () =>
                {
                    Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");
                    Debug.Assert(!EditorApplication.isPlaying, "!EditorApplication.isPlaying");
                    EditorApplication.update -= del;
                    
                    try
                    {
                        action(param1);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        onException?.Invoke();
                    }
                };

                EditorApplication.update += del;
                
                return;
            }
#endif
            
#if !UNITY_EDITOR
            Debug.LogError("Please dont use this feature outside editor");
#endif
            
            mainThreadSynchronizationContext.Post(_ =>
            {
                try
                {
                    action(param1);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    onException?.Invoke();
                }
            }, null);
        }
        
        public static void ExecuteAsync<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2, Action onException = null)
        {
#if UNITY_EDITOR
            if (mainThreadSynchronizationContext == null)
            {
                // Debug.Log("Unity editor main thread callback requested");
                EditorApplication.CallbackFunction del = null;
                
                del = () =>
                {
                    Debug.Assert(!EditorApplication.isPlaying, "!EditorApplication.isPlaying");
                    EditorApplication.update -= del;
                    
                    try
                    {
                        action(param1, param2);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        onException?.Invoke();
                    }
                };

                EditorApplication.update += del;
                
                return;
            }
#endif
            
#if !UNITY_EDITOR
            Debug.LogError("Please dont use this feature outside editor");
#endif
            
            mainThreadSynchronizationContext.Post(_ =>
            {
                try
                {
                    action(param1, param2);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    onException?.Invoke();
                }
            }, null);
        }

        public static Action CreateAsyncDelegate(Action action, Action onException = null)
        {
#if UNITY_EDITOR
            if (mainThreadSynchronizationContext == null)
            {
                Debug.Log("Unity editor main thread callback requested");
                
                return () =>
                {
                    EditorApplication.CallbackFunction del = null;

                    del = () =>
                    {
                        Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");
                        Debug.Assert(!EditorApplication.isPlaying, "!EditorApplication.isPlaying");
                        EditorApplication.update -= del;

                        try
                        {
                            action();
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            onException?.Invoke();
                        }
                    };

                    EditorApplication.update += del;
                };
            }
#endif
            
#if !UNITY_EDITOR
            Debug.LogError("Please dont use this feature outside editor");
#endif
            
            return () => mainThreadSynchronizationContext.Post(_ =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    onException?.Invoke();
                }
            }, null);
        }
    }
}

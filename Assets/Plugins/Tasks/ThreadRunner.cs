using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Tasks
{
    public static class ThreadRunner
    {
        public static void ExecuteAsync(string name, Action action, Action onComplete = null, Action onFailed = null
#if UNITY_EDITOR
            ,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0
#endif
            )
        {
#if UNITY_EDITOR
            if (name == null)
                name = "";
            name += $"Name:{memberName}\nPath:{sourceFilePath}\nLine:{sourceLineNumber}";
#endif

            var task    = Task.Run(action);
            var awaiter = task.GetAwaiter();

            awaiter.OnCompleted(() =>
            {
                try
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        onComplete?.Invoke();
                    }
                    else
                    {
                        Debug.LogWarning($"Task {name} failed with status {task.Status}\nException:{task.Exception}");
                        onFailed?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    onFailed?.Invoke();
                }
            });
        }

        public static void ExecuteAsync(Action action, Action onComplete = null, Action onFailed = null
#if UNITY_EDITOR
            ,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0
#endif
            ) =>
            ExecuteAsync("", action, onComplete, onFailed
#if UNITY_EDITOR
            ,
            memberName,
            sourceFilePath,
            sourceLineNumber
#endif
            );
    }
}

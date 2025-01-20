using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Monads;
using UnityEngine;

namespace Tasks
{
    internal class DelegateQueue
    {
        private class ActionContext : IDisposable
        {
            private Func<Result<bool>> callback;
            private readonly string    callStack;
            private readonly string    name;

            public ActionContext(Action del, Action exceptionHandler = null, string name = null
#if UNITY_EDITOR
                ,
                [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0
#endif
                ) :
                this(() =>
                    {
                        del();
                        return Monads.Result.Ok();
                    },
                    exceptionHandler,
                    name
#if UNITY_EDITOR
                    ,
                    memberName,
                    sourceFilePath,
                    sourceLineNumber
#endif
                    )
            {
            }

            public ActionContext(Func<Result<bool>> del, Action exceptionHandler = null, string name = null
#if UNITY_EDITOR
                ,
                [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0
#endif
            )
            {
                callback = () =>
                {
                    try
                    {
                        return del();
                    }
                    catch (Exception outerException)
                    {
                        Debug.LogException(outerException);

                        if (exceptionHandler != null)
                        {
                            try
                            {
                                exceptionHandler();
                            }
                            catch (Exception innerException)
                            {
                                Debug.LogException(innerException);
                            }
                        }

                        return Monads.Result.Error($"Delegate queue entry {this.name}\nfailed with exception{outerException.Message}\n{callStack} ");
                    }
                };
                this.name = name;
#if UNITY_EDITOR
                if (this.name == null)
                    this.name = "";
                this.name += $"Name:{memberName}\nPath:{sourceFilePath}\nLine:{sourceLineNumber}";
#endif

#if UNITY_EDITOR || DEBUG_MENU
                callStack = Environment.StackTrace;
#else
                callStack = "";
#endif
            }

            public override string ToString() => $"Action {name} {callStack} {Result}";

            public Result<bool>? Result { get; private set; }

            public Result<bool> Execute()
            {
                Result = ExecuteInternal();
                return Result.Value;
            }
            
            private Result<bool> ExecuteInternal()
            {
                if (callback == null)
                    return Monads.Result.Error($"Action {name} {callStack} already disposed");
                return callback();
            }

            public void Dispose()
            {
                callback = null;
            }
        }

        private readonly ConcurrentQueue<ActionContext> queue;
        private readonly List<ActionContext>            repeatedActions;
        private readonly int                            processingRate;
        private readonly float                          timeBudget;

        public bool LogsEnabled { get; set; } = true;

        public DelegateQueue(int maxProcessingRate = 50, int maxTimeBudgetMs = 30)
        {
            queue           = new ConcurrentQueue<ActionContext>();
            repeatedActions = new List<ActionContext>();
            processingRate  = maxProcessingRate;
            timeBudget      = maxTimeBudgetMs * 1e-3f;
        }
        
        public void Enqueue(Action del, Action exceptionHandler = null, bool repeat = false, string name = null)
        {
            var context = new ActionContext(del, exceptionHandler, name);
            
            if (repeat)
                repeatedActions.Insert(0, context);
            else
                queue.Enqueue(context);
        }
        
        public async Task<Result<bool>> ExecuteAsync(Func<Result<bool>> del, string name = null, int timeoutMs = 5000)
        {
            var context = new ActionContext(del, null, name);
            queue.Enqueue(context);

            Func<Result<bool>?> condition = () => context.Result;
            return await condition.WaitForMilliseconds(timeoutMs);
        }
        
        public async Task<Result<bool>> ExecuteAsync(Action del, string name = null, int timeoutMs = 5000)
        {
            var context = new ActionContext(del, null, name);
            queue.Enqueue(context);

            Func<Result<bool>?> condition = () => context.Result;
            return await condition.WaitForMilliseconds(timeoutMs);
        }

        public void Update()
        {
            UpdateRepeated();
            UpdateQueue();
        }

        //here we ignore time budget, because caller will expect callback to be called no matter what
        private void UpdateRepeated()
        {
            float start = Time.realtimeSinceStartup;

            for (int i = repeatedActions.Count - 1; i >= 0; i--)
            {
                var context = repeatedActions[i];

                var result = context.Execute();
                if (!result.IsSuccess)
                {
                    context.Dispose();
                    repeatedActions.RemoveAt(i);
                    Debug.LogError(result.Message);
                }
            }

            if (!LogsEnabled)
                return;
            
            float duration = Time.realtimeSinceStartup - start;

            const float repeatedActionTimeBudgetError   = 500 * 1e-3f;
            const float repeatedActionTimeBudgetWarning = 100 * 1e-3f;

            if (duration > repeatedActionTimeBudgetError)
                Debug.LogError($"Performance issues possible. Repeated action time budget consumed ({(int)(duration * 1000)} ms)");
            else if (duration > repeatedActionTimeBudgetWarning)
                Debug.LogWarning($"Performance issues possible. Repeated action time budget consumed ({(int)(duration * 1000)} ms)");
        }

        private void UpdateQueue()
        {
            //this way everything enqueued in delegates will be executed next frame.
            //not necessary, but it looks more controllable
            int processingCount = Math.Min(processingRate, queue.Count);
            float start = Time.realtimeSinceStartup;
            float budget = timeBudget;

            while (processingCount-- >0 && 
                   Time.realtimeSinceStartup - start  < budget &&
                   queue.TryDequeue(out var context))
            {
                var result = context.Execute();
                if (!result.IsSuccess)
                    Debug.LogError(result.Message);
                context.Dispose();
            }
        }
    }
}

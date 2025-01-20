using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Monads;
using Debug = UnityEngine.Debug;

namespace Tasks
{
    /// <summary>
    /// Helper util for building chain of actions (function calls) which may end with success and fail.
    /// </summary>
    public partial class SequentialAction : ITaskAction
    {
        private static event Action abortAction;
        
        private Action onComplete;
        private Action onFailed;
        
        private SequentialActionContainer sequenceContainer;
        private Action        finalizationAction;
        private string        name;
        private Stopwatch     stopWatch;
        private bool          logActionDuration;
        private HealthMonitor healthMonitor;// = new HealthMonitor(10000f);
        private long          previousActionCompletionElapsedMs;

        private bool logErrorsOnAbort = false;
        private bool logErrors = true;
        private bool isNonRecursive;

        //name dont have default null value so that it is harder to miss name input
        public static SequentialAction Create(string name, Action onComplete = null, Action onFailed = null, bool isNonRecursive = false)
        {
            Debug.Assert(!string.IsNullOrEmpty(name), "!string.IsNullOrEmpty(name)");
            
            var action = new SequentialAction()
            {
                sequenceContainer = new SequentialActionContainer(),
                onComplete        = onComplete,
                onFailed          = onFailed,
                name              = name + " " + Guid.NewGuid(),
                isNonRecursive    = isNonRecursive 
            };

            abortAction += action.Abort;

            return action;
        }

        private SequentialAction() { }

        public static void AbortAll()
        {
            Debug.LogWarning($"Abort all sequential actions");
            abortAction?.Invoke();
            abortAction = null;
        }

        public void Abort()
        {
            Debug.LogWarning($"Abort sequential action {name}");
            sequenceContainer?.SetAbortCondition(() => true);
        }

        public SequentialAction Break()
        {
#if UNITY_EDITOR
            sequenceContainer.AddBreakPoint();
#endif
            return this;
        }

        /// <summary>
        /// BEWARE! Never use that for anything starting other threads or tasks, just simple function calls.
        /// This thing is only tracking function completion, it cant detect if function starts another thread inside
        /// and cant understand if such internal thread is done its job or not. So it may complete until actual job is done.
        /// Use other overloaded methods for correct handling of described case
        /// </summary>
        public SequentialAction Then(Action actionWithoutConditions)
        {
            sequenceContainer.AddAction((completeAction, failedAction) =>
            {
                try
                {
                    actionWithoutConditions.Invoke(); 
                    completeAction.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    failedAction.Invoke();
                }
            });
            
            return this;
        }
        
        public SequentialAction Then(Action<Action> actionWithCompleteCondition)
        {
            sequenceContainer.AddAction((completeAction, _) => actionWithCompleteCondition.Invoke(completeAction));
            return this;
        }
        
        public SequentialAction Log(string message)
        {
            return Then(() => Debug.Log(message));
        }
        
        public SequentialAction Log(Func<string> message)
        {
            Debug.Assert(message != null, "message != null");
            return Then(() => Debug.Log(message()));
        }
        
        public SequentialAction LogWarning(string message)
        {
            return Then(() => Debug.LogWarning(message));
        }

        public SequentialAction LogWarning(Func<string> message)
        {
            Debug.Assert(message != null, "message != null");
            return Then(() => Debug.LogWarning(message()));
        }
        
        public SequentialAction LogError(string message)
        {
            return Then(() => Debug.LogError(message));
        }
        
        public SequentialAction LogError(Func<string> message)
        {
            Debug.Assert(message != null, "message != null");
            return Then(() => Debug.LogError(message()));
        }

        public SequentialAction DontLogErrors()
        {
            logErrors = false;
            return this;
        }

        public SequentialAction Then(Action<Action, Action> actionWithFullConditions)
        {
            sequenceContainer.AddAction(actionWithFullConditions);
            return this;
        }
        
        public SequentialAction Then(Func<Task<Result<bool>>> task)
        {
            sequenceContainer.AddAction((completeAction, failedAction) =>
            {
                task().ContinueWith(result =>
                {
                    if (result.IsCompletedSuccessfully && result.Result.IsSuccess)
                        completeAction();
                    else
                    {
                        Debug.LogException(result?.Exception);
                        failedAction();
                    }
                });
            });

            return this;
        }
        
        public SequentialAction Then<TResult>(Func<Task<TResult>> task)
        {
            //use func?
            throw new NotImplementedException();
        }

        public SequentialAction Try(Action<Action, Action> actionWithFullConditions)
        {
            sequenceContainer.AddAction((completeAction, _) =>
                actionWithFullConditions(completeAction, () =>
                {
                    if (logErrors)
                    {
                        Debug.LogError($"sequential action {name} try action failed");
                    }
                    else
                    {
                        Debug.LogWarning($"sequential action {name} try action failed");
                    }
                    completeAction();
                }));
            return this;
        }
        
        public SequentialAction If(Func<bool> condition)
        {
            sequenceContainer.AddLastActionCondition(condition);
            return this;
        }

        public SequentialAction Then(ITaskAction action)
        {
            if (action == null)
                return this;
            
            sequenceContainer.AddAction((completeAction, failedAction) =>
            {
                try
                {
                    action.Run(completeAction, failedAction);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    failedAction.Invoke();
                }
            });
        
            return this;
        }

        public SequentialAction WithThreadOptions(ThreadOptions threadOptions)
        {
            sequenceContainer.SetThreadOptions(threadOptions);
            return this;
        }
        
        public SequentialAction Delay(int delayMs)
        {
            Then(DelayInternal, delayMs);
            return this;
        }
        
        private static async void DelayInternal(int delayMs, Action complete, Action failed)
        {
            await Task.Delay(delayMs);
            complete();
        }
        
        public SequentialAction Yield()
        {
            Then(YieldInternal);
            return this;
        }
        
        private static async void YieldInternal(Action complete, Action failed)
        {
            await Task.Yield();
            complete();
        }

        public SequentialAction WithHealthMonitor(float timeoutMs = 10000f)
        {
            Debug.Assert(healthMonitor == null, $"healthMonitor == null {name}");
            healthMonitor = new HealthMonitor(10000f);
            return this;
        }
        
        
        public SequentialAction WithTimeout(float timeoutSec, string name = null)
        {
            throw new NotImplementedException();
        }

        public SequentialAction FromEngineFixedUpdate()
        {
            throw new NotImplementedException();
        }
        
        public SequentialAction FromEngineUpdate()
        {
            throw new NotImplementedException();
        }
        
        public SequentialAction WithTimeBudget(float timeMs)
        {
            throw new NotImplementedException();
        }

        void ITaskAction.Run(Action onComplete, Action onFailed)
        {
            Debug.Assert(this.onComplete == null, name);
            Debug.Assert(this.onFailed == null, name);

            this.onComplete = onComplete;
            this.onFailed   = onFailed;

            Run();
        }

        public SequentialAction Finally(Action action)
        {
            Debug.Assert(finalizationAction == null, $"finalizationAction == null {name}");
            finalizationAction = action;

            return this;
        }

        public SequentialAction WithAbortCondition(Func<bool> abortAction)
        {
            Debug.Assert(abortAction != null, "abortAction != null");
            sequenceContainer.SetAbortCondition(abortAction);

            return this;
        }

        public void Run()
        {
            stopWatch?.Start();

            if (isNonRecursive)
                RunNonRecursive();
            else
                RunRecursive();
        }
        
        private void RunRecursive()
        {
            if (sequenceContainer == null)
            {
                Debug.LogError($"Sequential action '{name}' is disposed!");
                OnAnyActionFailed();
                return;
            }
            
            healthMonitor?.Reset(name + " at action: " + sequenceContainer.SequencePointer);

            //put into local variable so that we can access IsAborted flag, because sequence container may get disposed in  OnAnyActionFailed
            var container = sequenceContainer;
            if (!container.TryInvokeNext(() => { OnAnyActionComplete(); RunRecursive(); }, OnAnyActionFailed) &&
                !container.IsAborted)
                OnAllActionsComplete();
        }

        private async void RunNonRecursive()
        {
            while (true)
            {
                if (sequenceContainer == null)
                {
                    Debug.LogError($"Sequential action '{name}' is disposed!");
                    OnAnyActionFailed();
                    return;
                }

            
                healthMonitor?.Reset(name + " at action: " + sequenceContainer.SequencePointer);

                var isComplete = false;
                var isFailed = false;

                if (!sequenceContainer.TryInvokeNext(() => isComplete = true, () => isFailed = true))
                {
                    if (sequenceContainer.IsAborted)
                        return;
                    
                    OnAllActionsComplete();
                    break;
                }
                else
                {
                    while (!isComplete && !isFailed)
                        await Task.Yield();

                    if (isComplete)
                        OnAnyActionComplete();
                    if (isFailed)
                    {
                        OnAnyActionFailed();
                        return;
                    }
                }
            }
        }

        public ConcurrentActionAsync AsConcurrentActionAsync()
        {
            var completeCallback = onComplete;
            var failedCallback = onFailed;
            onComplete = null;
            onFailed = null;

            return ConcurrentActionAsync.Create(name, completeCallback, failedCallback)
                .With(this);
        }

        private void OnAllActionsComplete()
        {
            if (stopWatch != null)
            {
                stopWatch.Stop();
                Debug.Log($"Sequential action {name ?? "NULL"} full execution duration: {stopWatch.ElapsedMilliseconds} ms");
            }

            Debug.Assert(sequenceContainer != null, $"sequenceContainer != null {name}");
            Debug.Assert(sequenceContainer.IsComplete, $"sequenceContainer.IsComplete {name}");

            TryHandleFinalizationAction();
            
            var completeCallback = onComplete;
            Dispose();
            completeCallback?.Invoke();
        }

        private void OnAnyActionComplete()
        {
            Debug.Assert(sequenceContainer != null, name);
            
            if (stopWatch != null && logActionDuration)
            {
                long timeSincePreviousActionCompletion = stopWatch.ElapsedMilliseconds - previousActionCompletionElapsedMs;
                previousActionCompletionElapsedMs = stopWatch.ElapsedMilliseconds;
                Debug.Log($"Sequential action {name ?? "NULL"} action {sequenceContainer.SequencePointer} execution duration: {timeSincePreviousActionCompletion} ms");
            }
            
            TryHandleBreakPoint();
        }

        private void TryHandleFinalizationAction()
        {
            try
            {
                finalizationAction?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"finalization action failed '{name}'");
                Debug.LogException(e);
            }

            finalizationAction = null;
        }
        

        private void OnAnyActionFailed()
        {
            Debug.Assert(sequenceContainer != null, $"sequenceContainer != null {name}");
            TryHandleBreakPoint();

            if (!string.IsNullOrEmpty(name))
            {
                if (!sequenceContainer.IsAborted)
                {
                    if (logErrors)
                        Debug.LogError($"Sequential action named: '{name}' failed at row #{sequenceContainer?.SequencePointer}");
                    else
                        Debug.LogWarning($"Sequential action named: '{name}' failed at row #{sequenceContainer?.SequencePointer}");
                }
                else
                {
                    if(logErrorsOnAbort)
                        Debug.LogError($"Sequential action named: '{name}' aborted at row #{sequenceContainer?.SequencePointer}");
                    else
                        Debug.LogWarning($"Sequential action named: '{name}' aborted at row #{sequenceContainer?.SequencePointer}");
                }
            }

            Debug.Assert(sequenceContainer.IsStarted && !sequenceContainer.IsComplete, $"sequenceContainer.IsComplete {name}");

            TryHandleFinalizationAction();
            
            var failedCallback = onFailed;
            Dispose();
            failedCallback?.Invoke();
        }

        private void TryHandleBreakPoint()
        {
#if UNITY_EDITOR
            Debug.Assert(sequenceContainer != null, $"sequenceContainer != null {name}");
            if (sequenceContainer.IsBreakPoint)
            {
                Debug.Break();
                Debug.LogWarning($"Break point reached {name}");
            }
#endif
        }

        public SequentialAction LogDuration(bool logActions = false)
        {
            Debug.Assert(!sequenceContainer.IsStarted, name);
            Debug.Assert(stopWatch == null, $"stopWatch == nul {name}");
            stopWatch = new Stopwatch();
            logActionDuration = logActions;
            return this;
        }

//         public SequentialAction WithProfiler(string sampleName)
//         {
// #if UNITY_EDITOR
//             sequenceContainer.AddProfiler(name + " :: " + sampleName);
// #endif
//             return this;
//         }

        private void Dispose()
        {
            Debug.Assert(name != null, "name != null");
            if (sequenceContainer == null)
                Debug.LogError($"sequenceContainer is null: '{name}'");
            
            abortAction -= Abort;
            
            onComplete        = null;
            onFailed          = null;
            sequenceContainer = null;
            //name              += "[DISPOSED]";
            
            stopWatch?.Stop();
            stopWatch         = null;
            
            healthMonitor?.Stop();
        }

        public ITaskAction Join(ITaskAction action) => Then(action);

        public ITaskAction Join(Action actionWithoutConditions) => Then(actionWithoutConditions);

        public ITaskAction Join(Action<Action> actionWithCompleteCondition) => Then(actionWithCompleteCondition);

        public ITaskAction Join(Action<Action, Action> actionWithFullConditions) => Then(actionWithFullConditions);
    }
}

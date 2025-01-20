using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace Tasks
{
    /// <summary>
    /// Helper util to run several parallel tasks and get success or fail callback.
    /// Every single task runs in separate thread by default.
    /// Handles internal exceptions and calls onFailed callback
    /// You can use it to run even single task if need exception handling.
    /// Use with care - threading issues may occur, thread scheduling consume resources
    /// </summary>
    public partial class ConcurrentActionAsync : ITaskAction
    {
        private static event Action abortAction;
        
        private Action onComplete;
        private Action onFailed;

        private ConcurrentActionContainer actionContainer;
        private Action    finalizationAction;
        private string    name;
        private Stopwatch stopWatch;
        
        private bool logErrors = true;

        public static ConcurrentActionAsync Create(string name, Action onComplete = null, Action onFailed = null)
        {
            Debug.Assert(!string.IsNullOrEmpty(name), "!string.IsNullOrEmpty(name)");
            
            return new ConcurrentActionAsync()
            {
#if UNITY_EDITOR
                actionContainer = new ConcurrentActionContainer(name),
#else
                actionContainer = new ConcurrentActionContainer(),
#endif
                onComplete      = onComplete,
                onFailed        = onFailed,
                name            = name,
            };
        }

        private ConcurrentActionAsync() { }

        public static void AbortAll()
        {
            Debug.LogWarning($"Abort all concurrent actions");
            abortAction?.Invoke();
            abortAction = null;
        }

        public void Abort()
        {
            Debug.LogError($"Abort sequential action {name}");
            actionContainer.SetAbortCondition(() => true);
        }
        
        /// <summary>
        /// BEWARE! Never use that for anything starting other threads or tasks, just simple function calls.
        /// This thing is only tracking function completion, it cant detect if function starts another thread inside
        /// and cant understand if such internal thread is done its job or not. So it may complete until actual job is done.
        /// Use other overloaded methods for correct handling of described case
        /// </summary>
        public ConcurrentActionAsync With(Action actionWithoutConditions)
        {
            actionContainer.AddAction(() =>
            {
                actionWithoutConditions();
                actionContainer.OnCompleteExternalCallback();
            });
            return this;
        }
        
        public SequentialAction With<TResult>(Func<Task<TResult>> task)
        {
            //use func?
            throw new NotImplementedException();
        }
        
        public ConcurrentActionAsync With(Action<Action, Action> actionWithFullConditions)
        {
            actionContainer.AddAction(() =>
                actionWithFullConditions.Invoke(actionContainer.OnCompleteExternalCallback, actionContainer.OnFailExternalCallback));
            return this;
        }
        
        public ConcurrentActionAsync With(ITaskAction action)
        {
            actionContainer.AddAction(() =>
                action.Run(actionContainer.OnCompleteExternalCallback, actionContainer.OnFailExternalCallback));
            return this;
        }
        
        public ConcurrentActionAsync With(Action<Action> actionWithCompleteCondition)
        {
            actionContainer.AddAction(() => actionWithCompleteCondition.Invoke(actionContainer.OnCompleteExternalCallback));
            return this;
        }


        public ConcurrentActionAsync WithTimeout(float timeoutSec, string name = null)
        {
            throw new NotImplementedException();
        }
        
        public ConcurrentActionAsync FromEngineFixedUpdate()
        {
            throw new NotImplementedException();
        }
        
        public ConcurrentActionAsync FromEngineUpdate()
        {
            throw new NotImplementedException();
        }
        
        public ConcurrentActionAsync DontLogErrors()
        {
            logErrors = false;
            return this;
        }


        void ITaskAction.Run(Action onComplete, Action onFailed)
        {
            Debug.Assert(this.onComplete == null, "this.onComplete == null");
            Debug.Assert(this.onFailed == null, "this.onFailed == null");

            this.onComplete = onComplete;
            this.onFailed   = onFailed;

            Run();
        }

        public ConcurrentActionAsync WithThreadOptions(ThreadOptions threadOptions)
        {
            actionContainer.SetThreadOptions(threadOptions);
            return this;
        }

        public ConcurrentActionAsync Finally(Action action)
        {
            Debug.Assert(finalizationAction == null, "finalizationAction == null");
            finalizationAction = action;
            return this;
        }

        public void Run()
        {
            stopWatch?.Reset();
            stopWatch?.Start();

            if (actionContainer == null)
            {
                Debug.LogError($"Concurrent action {name} is already disposed!");
                OnAnyFailed();
                return;
            }
            
            actionContainer.InvokeAll(OnAllComplete, OnAnyFailed);
        }
        
        private void TryHandleFinalizationAction()
        {
            try
            {
                finalizationAction?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"finalization action failed {name}");
                Debug.LogException(e);
            }

            finalizationAction = null;
        }

        private void OnAllComplete()
        {
            if (stopWatch != null)
            {
                stopWatch.Stop();
                Debug.Log($"Concurrent action {name ?? "NULL"} execution duration: {stopWatch.ElapsedMilliseconds} ms");
            }
            
            Debug.Assert(actionContainer.IsComplete, $"actionContainer.IsComplete {name}");

            TryHandleFinalizationAction();
            
            var completeCallback = onComplete;
            Dispose();
            completeCallback?.Invoke();
        }

        private void OnAnyFailed()
        {
            if (logErrors)
                Debug.LogError($"Concurrent action {name} failed");
            else
                Debug.LogWarning($"Concurrent action {name} failed");
            
            if (actionContainer != null)
                Debug.Assert(actionContainer.IsFailed, $"actionContainer.IsFailed {name}");

            TryHandleFinalizationAction();
            
            var failedCallback = onFailed;
            Dispose();
            failedCallback?.Invoke();
        }

        public ConcurrentActionAsync LogDuration()
        {
            Debug.Assert(stopWatch == null, "stopWatch == null");
            stopWatch = new Stopwatch();
            return this;
        }

        private void Dispose()
        {
            Debug.Assert(name != null, "name != null");
            if (actionContainer == null)
                Debug.LogError($"actionContainer is null: {name}");

            onComplete        = null;
            onFailed          = null;
            actionContainer   = null;
            //name              += "[DISPOSED]";
            
            stopWatch?.Stop();
            stopWatch         = null;
        }

        public ITaskAction Join(ITaskAction action) => With(action);

        public ITaskAction Join(Action actionWithoutConditions) => With(actionWithoutConditions);

        public ITaskAction Join(Action<Action> actionWithCompleteCondition) => With(actionWithCompleteCondition);

        public ITaskAction Join(Action<Action, Action> actionWithFullConditions) => With(actionWithFullConditions);
    }
}

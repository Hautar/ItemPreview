using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Tasks
{
    internal class ConcurrentActionContainer
    {
        private long taskFailedCount;
        private long taskIncompleteCount;
        private long startedTasksCount;
        
        private List<Action> actions;
        private List<ThreadOptions> threadOptions;

        private Action onAllComplete;
        private Action onAnyFailed;
        
        private Func<bool> abortCondition;

        private readonly object locker = new object();

        public bool IsStarted  => Interlocked.Read(ref startedTasksCount) > 0;
        public bool IsComplete => Interlocked.Read(ref taskIncompleteCount) == 0;
        public bool IsFailed   => Interlocked.Read(ref taskFailedCount) > 0;

#if UNITY_EDITOR
        private string name;
        
        public ConcurrentActionContainer(string name)
        {
            taskFailedCount     = 0;
            taskIncompleteCount = 0;
            startedTasksCount   = 0;
            actions             = new List<Action>();
            threadOptions       = new List<ThreadOptions>();
            this.name           = name;
        }
#endif
        public ConcurrentActionContainer()
        {
            taskFailedCount     = 0;
            taskIncompleteCount = 0;
            startedTasksCount   = 0;
            actions             = new List<Action>();
            threadOptions       = new List<ThreadOptions>();
        }

        public void AddAction(Action action)
        {
            Debug.Assert(action != null, "action != null");
            Debug.Assert(actions != null, "actions != null");
            Debug.Assert(!IsStarted, "!IsStarted");

            actions.Add(action);

            taskIncompleteCount++;
            threadOptions.Add(ThreadOptions.None);
        }
        
        public void SetAbortCondition(Func<bool> abortCondition)
        {
            lock (locker)
            {
                Debug.Assert(this.abortCondition == null, "this.abortCondition == null");
                this.abortCondition = abortCondition;
            }
        }

        public void InvokeAll(Action onAllComplete, Action onAnyFailed)
        {
            lock (locker)
            {
                if (actions == null)
                {
#if UNITY_EDITOR
                    Debug.LogError($"ALREADY DISPOSED {name}. PLEASE REPORT BUG WITH FULL LOG");
#else
                    Debug.LogError("Action list already disposed. Please report bug");
#endif
                    return;
                }

                Debug.Assert(actions != null, "actions != null");
                Debug.Assert(threadOptions != null, "threadOptions != null");
                Debug.Assert(actions.Count > 0, "empty action list");
                Debug.Assert(!IsStarted, "!IsStarted");
                Debug.Assert(!IsComplete, "!IsComplete");

                this.onAllComplete = onAllComplete;
                this.onAnyFailed = onAnyFailed;

                int count = actions.Count;
                for (int i = 0; i < count; i++)
                {
                    if (actions == null)
                    {
#if UNITY_EDITOR
                        Debug.LogError($"Concurrent action  {i} {name}. PLEASE REPORT BUG WITH FULL LOG");
#else
                        Debug.LogError($"Concurrent action: {i} already disposed. PLEASE REPORT BUG WITH FULL LOG");
#endif
                        return;
                    }

                    Debug.Assert(actions[i] != null, "actions[i] != null");

                    ProcessInternal(actions[i], threadOptions[i]);
                }
            }
        }

        private void ProcessInternal(Action operation, ThreadOptions threadOptions = ThreadOptions.None)
        {
            Interlocked.Increment(ref startedTasksCount);

            switch (threadOptions)
            {
                case ThreadOptions.ForceMainThread:
                case ThreadOptions.EnsureMainThread:
                    RunMainThread();
                    break;
                case ThreadOptions.MonoBehaviourUpdate:
                    RunUpdate();
                    break;
                case ThreadOptions.MonoBehaviourFixedUpdate:
                    RunFixedUpdate();
                    break;
                case ThreadOptions.MonoBehaviourLateUpdate:
                    RunLateUpdate();
                    break;
                case ThreadOptions.ForceNewThread:
                default:
                    RunDefault();
                    break;
            }


            void RunMainThread()
            {
                MainThreadRunner.ExecuteAsync(operation, OnFailSingleTask);
            }
            
            void RunUpdate()
            {
                MonoBehaviourRunner.EnqueueUpdate(operation, OnFailSingleTask);
            }
            
            void RunFixedUpdate()
            {
                MonoBehaviourRunner.EnqueueFixedUpdate(operation, OnFailSingleTask);
            }
            
            void RunLateUpdate()
            {
                MonoBehaviourRunner.EnqueueLateUpdate(operation, OnFailSingleTask);
            }
            

            void RunDefault()
            {
                ThreadRunner.ExecuteAsync(operation, null, OnFailSingleTask);
            }
        }
        
        //need task id
        private void OnCompleteSingleTask(int taskId = -1)
        {
            lock (locker)
            {
                taskIncompleteCount--;
            
                if (actions == null)
                {
#if UNITY_EDITOR
                    Debug.LogError($"ALREADY DISPOSED {name}. PLEASE REPORT BUG WITH FULL LOG");
#else
                    Debug.LogError($"Concurrent action: already disposed. PLEASE REPORT BUG WITH FULL LOG");
#endif
                    return;
                }
            
                Debug.Assert(taskIncompleteCount >= 0, "remainingTasksCount >= 0");
                Debug.Assert(actions != null, "actions != null");
                Debug.Assert(actions?.Count > 0, "actions.Count > 0");

                if (taskIncompleteCount == 0 && taskFailedCount == 0)
                {
                    onAllComplete?.Invoke();
                    Dispose();
                    return;
                }
            
                if (taskFailedCount != 0 && abortCondition != null && abortCondition())
                {
                    Debug.Log($"Concurrent action: abort");
                    OnAnyFailedInternal();
                }
            }
        }

        private void OnFailSingleTask()
        {
            lock (locker)
            {
                OnAnyFailedInternal();
            }
        }

        private void OnAnyFailedInternal()
        {
            taskFailedCount++;
            if (taskFailedCount == 1)
            {
                onAnyFailed?.Invoke();
                Dispose();
            }
        }

        private void Dispose()
        {
            if (actions == null)
            {
                Debug.LogError($"Concurrent action: already disposed. PLEASE REPORT BUG WITH FULL LOG");
            }

            Debug.Assert(onAllComplete != null, "already disposed");
            Debug.Assert(onAnyFailed != null, "already disposed");
            Debug.Assert(threadOptions != null, "already disposed");
            
            actions         = null;
            threadOptions   = null;
            onAllComplete   = null;
            onAnyFailed     = null;
            abortCondition  = null;
        }
        
        public void SetThreadOptions(ThreadOptions options)
        {
            Debug.Assert(!IsStarted, "!IsStarted");
            Debug.Assert(!IsComplete, "!IsComplete");
            
            threadOptions[actions.Count - 1] = options;
        }
        
        internal void OnCompleteExternalCallback() => OnCompleteSingleTask();
        
        internal void OnFailExternalCallback() => OnFailSingleTask();
    }
}

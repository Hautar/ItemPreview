using System;
using System.Collections.Generic;
using System.Threading;
using Tasks;
using UnityEngine;
using UnityEngine.Profiling;

namespace Tasks
{
    //TODO: add conditional sequence break mechanics?
    //TODO: use locks? it is concurrent anyway, but with locks we'll get more reliable code
    internal class SequentialActionContainer
    {
        private readonly List<Action<Action, Action>> sequence;
        private readonly List<Func<bool>>             conditions;
        private readonly List<ThreadOptions>          threadOptions;
        private long                                  sequencePointer;
        private Func<bool>                            abortCondition;
        private bool                                  isAborted;

#if UNITY_EDITOR
        private List<int>                    breakPoints; //TODO: see if this debugging feature is useful
        // private List<int>                    useProfiler;
        // private List<string>                 profilerSampleNames;
#endif

        public bool IsStarted => Interlocked.Read(ref sequencePointer) >= 0;
        
        public bool IsComplete => Interlocked.Read(ref sequencePointer) >= Count;
        
        public int  SequencePointer => (int) Interlocked.Read(ref sequencePointer);
        public int  Count => sequence.Count;
        public bool IsAborted => isAborted;
        
#if UNITY_EDITOR
        public bool IsBreakPoint => breakPoints != null && breakPoints.Contains((int) Interlocked.Read(ref sequencePointer));
#endif

        public SequentialActionContainer()
        {
            sequence        = new List<Action<Action, Action>>();
            threadOptions  = new List<ThreadOptions>();
            sequencePointer = -1;
            conditions      = new List<Func<bool>>();
        }

#if UNITY_EDITOR
        public void AddBreakPoint()
        {
            Debug.Assert(!IsStarted, "!IsStarted");
            Debug.Assert(!IsComplete, "!IsComplete");
            
            breakPoints ??= new List<int>();
            breakPoints.Add(sequence.Count - 1);
        }

        //TODO: must think about how to profile actions, probably enforce running from main thread while profiling?
        // public void AddProfiler(string sampleName)
        // {
        //     useProfiler ??= new List<int>();
        //     useProfiler.Add(sequence.Count - 1);
        //     
        //     profilerSampleNames ??= new List<string>();
        //     profilerSampleNames.Add(sampleName);
        // }
#endif
        
        public void AddAction(Action<Action, Action> action, Func<bool> condition = null, ThreadOptions threadOptions = ThreadOptions.None)
        {
            Debug.Assert(!IsStarted, "!IsStarted");
            Debug.Assert(!IsComplete, "!IsComplete");
            
            sequence.Add(action);
            this.threadOptions.Add(threadOptions);
            conditions.Add(condition);
        }

        public void SetAbortCondition(Func<bool> abortCondition)
        {
            Debug.Assert(this.abortCondition == null, "this.abortCondition == null");
            
            this.abortCondition = abortCondition;
        }
        
        // public void AddTryAction(Action<Action, Action> action, Func<bool> condition = null, bool fromMainThread = false)
        // {
        //     Debug.Assert(!IsStarted, "!IsStarted");
        //     Debug.Assert(!IsComplete, "!IsComplete");
        //     
        //     sequence.Add((complete, failed) =>
        //     {
        //         action.Invoke(complete,() =>
        //         {
        //             Debug.LogError("Sequential try action failed, but execution continues");
        //             complete();
        //         });
        //     });
        //     forceMainThread.Add(fromMainThread);
        //     conditions.Add(condition);
        // }

        public void SetThreadOptions(ThreadOptions options)
        {
            Debug.Assert(!IsStarted, "!IsStarted");
            Debug.Assert(!IsComplete, "!IsComplete");
            
            threadOptions[sequence.Count - 1] = options;
        }
        
        public void AddLastActionCondition(Func<bool> condition)
        {
           Debug.Assert(condition != null, "condition != null");
            
            Debug.Assert(!IsStarted, "!IsStarted");
            Debug.Assert(!IsComplete, "!IsComplete");
            
            conditions[sequence.Count - 1] = condition;
        }

        public bool TryInvokeNext(Action onComplete, Action onFailed)
        {
            if (isAborted)
            {
                Debug.LogError("Should never get here! Sequential action already aborted. PLEASE REPORT BUG WITH FULL LOG");
                return false;
            }

            Debug.Assert(sequence != null, "sequence != null");
            Debug.Assert(threadOptions != null, "forceMainThread != null");

            var sequenceIndex = (int)Interlocked.Increment(ref sequencePointer);
            if (sequenceIndex >= sequence.Count)
                return false;

            Debug.Assert(threadOptions.Count > sequenceIndex, "forceMainThread.Count > sequenceIndex");
            Debug.Assert(sequence.Count > sequenceIndex, "sequence.Count > sequenceIndex");

            if (sequence[sequenceIndex] == null)
            {
                Debug.LogError($"Sequential action: trying to RUN already handled action {sequenceIndex}. PLEASE REPORT BUG WITH FULL LOG");
                onFailed?.Invoke();
                return false;
            }

            if (abortCondition != null && abortCondition.Invoke())
            {
                Debug.Log($"Sequential action: abort");
                isAborted = true;
                onFailed?.Invoke();
                return false;
            }

            if (conditions[sequenceIndex] != null)
            {
                bool result = false;
                try
                {
                    result = !conditions[sequenceIndex].Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    onFailed?.Invoke();
                    return false;
                }

                if (result)
                {
                    OnCompleteHandler(sequenceIndex, onComplete, onFailed);
                    return true;
                }
            }

            switch (threadOptions[sequenceIndex])
            {
                case ThreadOptions.ForceMainThread:
                    RunMainThread();
                    break;
                case ThreadOptions.EnsureMainThread:
                    if (MainThreadRunner.IsMainThread())
                        RunDefault();
                    else
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
                    RunThread();
                    break;
                default:
                    RunDefault();
                    break;
                
            }

            return true;

            void RunMainThread()
            {
                MainThreadRunner.ExecuteAsync(sequence[sequenceIndex],
                    () => OnCompleteHandler(sequenceIndex, onComplete, onFailed),
                    () => OnFailedHandler(sequenceIndex, onFailed), 
                    () => OnFailedHandler(sequenceIndex, onFailed));
            }

            void RunUpdate()
            {
                MonoBehaviourRunner.EnqueueUpdate(() =>
                        sequence[sequenceIndex].Invoke
                        (
                            () => OnCompleteHandler(sequenceIndex, onComplete, onFailed),
                            () => OnFailedHandler(sequenceIndex, onFailed)
                        ),
                    () => OnFailedHandler(sequenceIndex, onFailed)
                );
            }
            
            void RunFixedUpdate()
            {
                MonoBehaviourRunner.EnqueueFixedUpdate(() =>
                        sequence[sequenceIndex].Invoke
                        (
                            () => OnCompleteHandler(sequenceIndex, onComplete, onFailed),
                            () => OnFailedHandler(sequenceIndex, onFailed)
                        ),
                    () => OnFailedHandler(sequenceIndex, onFailed)
                );
            }
            
            void RunLateUpdate()
            {
                MonoBehaviourRunner.EnqueueLateUpdate(() =>
                        sequence[sequenceIndex].Invoke
                        (
                            () => OnCompleteHandler(sequenceIndex, onComplete, onFailed),
                            () => OnFailedHandler(sequenceIndex, onFailed)
                        ),
                    () => OnFailedHandler(sequenceIndex, onFailed)
                );
            }

            void RunThread()
            {
                ThreadRunner.ExecuteAsync(() =>
                        sequence[sequenceIndex].Invoke
                        (
                            () => OnCompleteHandler(sequenceIndex, onComplete, onFailed),
                            () => OnFailedHandler(sequenceIndex, onFailed)
                        ),
                    null,
                    () => OnFailedHandler(sequenceIndex, onFailed)
                );
            }
            
            void RunDefault()
            {
                try
                {
                    sequence[sequenceIndex].Invoke(
                        () => OnCompleteHandler(sequenceIndex, onComplete, onFailed),
                        () => OnFailedHandler(sequenceIndex, onFailed));
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                    OnFailedHandler(sequenceIndex, onFailed);
                }
            }
        }

        private void OnCompleteHandler(int sequenceIndex, Action onComplete, Action onFailed)
        {
            if (sequence[sequenceIndex] != null)
            {
                sequence[sequenceIndex] = null;
                onComplete();
            }
            else
            {
                Debug.LogError($"Sequential action: trying to COMPLETE already handled action {sequenceIndex}. PLEASE REPORT BUG WITH FULL LOG");
            }
        }

        private void OnFailedHandler(int sequenceIndex, Action onFailed)
        {
            if (sequence[sequenceIndex] != null)
            {
                sequence[sequenceIndex] = null;
                onFailed();
            }
            else
            {
                Debug.LogError($"Sequential action: trying to FAIL already handled action {sequenceIndex}. PLEASE REPORT BUG WITH FULL LOG");
            }
        }
    }
}
using System;
using UnityEngine;

//well, it is actually not generated

namespace Tasks
{
    public partial class SequentialAction
    {
        /// <summary>
        /// BEWARE! Never use that for anything starting other threads or tasks, just simple function calls.
        /// This thing is only tracking function completion, it cant detect if function starts another thread inside
        /// and cant understand if such internal thread is done its job or not. So it may complete until actual job is done.
        /// Use other overloaded methods for correct handling of described case
        /// </summary>
        public SequentialAction Then<T>(Action<T> actionWithoutConditions, T param)
        {
            sequenceContainer.AddAction((completeAction, failedAction) =>
            {
                try
                {
                    actionWithoutConditions.Invoke(param); 
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
        
        /// <summary>
        /// BEWARE! Never use that for anything starting other threads or tasks, just simple function calls.
        /// This thing is only tracking function completion, it cant detect if function starts another thread inside
        /// and cant understand if such internal thread is done its job or not. So it may complete until actual job is done.
        /// Use other overloaded methods for correct handling of described case
        /// </summary>
        public SequentialAction Then<T1, T2>(Action<T1, T2> actionWithoutConditions, T1 param1, T2 param2)
        {
            //TODO: should we handle exceptions here, or directly at call time?
            //TODO: Should we make option to avoid exception handling for actions we are sure about?
            sequenceContainer.AddAction((completeAction, failedAction) =>
            {
                try
                {
                    actionWithoutConditions.Invoke(param1, param2); 
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
        

    
        public SequentialAction Then<T1, T2, T3>(Action<T1, T2, T3> actionWithoutConditions, T1 param1, T2 param2, T3 param3)
        {
            sequenceContainer.AddAction((completeAction, failedAction) =>
            {
                try
                {
                    actionWithoutConditions.Invoke(param1, param2, param3);
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
        
        public SequentialAction Then<T1, T2, T3, T4>(Action<T1, T2, T3, T4> actionWithoutConditions, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            sequenceContainer.AddAction((completeAction, failedAction) =>
            {
                try
                {
                    actionWithoutConditions.Invoke(param1, param2, param3, param4);
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

        public SequentialAction Then<T>(Action<T, Action, Action> actionWithFullConditions, T param)
        {
            sequenceContainer.AddAction((completeAction, failedAction) =>
            {
                Debug.Assert(actionWithFullConditions != null, $"{name} {actionWithFullConditions.Method.Name}");
                Debug.Assert(param != null, $"{name} {typeof(T)}");
                
                actionWithFullConditions(param, completeAction, failedAction);
            });
            
            return this;
        }
        
        public SequentialAction Then<T>(Action<T, Action, Action> actionWithFullConditions, Func<T> paramProvider)
        {
           Debug.Assert(paramProvider != null, "paramProvider != null");
            
            sequenceContainer.AddAction((completeAction, failedAction) =>
            {
                Debug.Assert(actionWithFullConditions != null, $"{name} {actionWithFullConditions.Method.Name}");
                Debug.Assert(paramProvider != null, $"{name} {typeof(T)}");

                if (paramProvider == null)
                {
                    Debug.LogError($"parameter provider of type {typeof(T)} for action {name} is null");
                    failedAction();
                    return;
                }

                T param;

                try
                {
                    param = paramProvider();
                }
                catch (Exception e)
                {
                    Debug.LogError($"parameter provider of type {typeof(T)} for action {name} has thrown exception");
                    Debug.LogException(e);
                    failedAction();
                    return;
                }
                
                actionWithFullConditions(param, completeAction, failedAction);
            });
            
            return this;
        }
        
        public SequentialAction Then<T>(Action<Func<T>, Action, Action> actionWithFullConditions, Func<T> paramProvider)
        {
            Debug.Assert(paramProvider != null, "paramProvider != null");
            
            sequenceContainer.AddAction((completeAction, failedAction) =>
            {
                Debug.Assert(actionWithFullConditions != null, $"{name} {actionWithFullConditions.Method.Name}");
                Debug.Assert(paramProvider != null, $"{name} {typeof(T)}");

                if (paramProvider == null)
                {
                    Debug.LogError($"parameter provider of type {typeof(T)} for action {name} is null");
                    failedAction();
                    return;
                }

                actionWithFullConditions(paramProvider, completeAction, failedAction);
            });
            
            return this;
        }

        #region Action with 2 params / paramProviders
        
        public SequentialAction Then<T1, T2>(Action<T1, T2> actionWithoutConditions,
                                             Func<T1> paramProvider1,
                                             T2 param2)
        {
            sequenceContainer.AddAction((completeAction, failedAction) =>
            {
                try
                {
                    actionWithoutConditions.Invoke(paramProvider1(), param2); 
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
        
        public SequentialAction Then<T1, T2>(Action<T1, T2> actionWithoutConditions,
                                             T1 param1,
                                             Func<T2> paramProvider2)
        {
            sequenceContainer.AddAction((completeAction, failedAction) =>
            {
                try
                {
                    actionWithoutConditions.Invoke(param1, paramProvider2()); 
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
        
        public SequentialAction Then<T1, T2>(Action<T1, T2> actionWithoutConditions,
                                             Func<T1> paramProvider1,
                                             Func<T2> paramProvider2)
        {
            sequenceContainer.AddAction((completeAction, failedAction) =>
            {
                try
                {
                    actionWithoutConditions.Invoke(paramProvider1(), paramProvider2()); 
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

        public SequentialAction Then<T1, T2>(Action<T1, T2, Action, Action> actionWithFullConditions,
                                             Func<T1> paramProvider1,
                                             Func<T2> paramProvider2)
        {
            sequenceContainer.AddAction((completeAction, failedAction) =>
            {
                try
                {
                    actionWithFullConditions.Invoke(paramProvider1(), paramProvider2(), completeAction, failedAction); 
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
        
        #endregion
        
        public SequentialAction Then<T>(Action<T> actionWithoutConditions, Func<T> paramProvider)
        {
            Debug.Assert(paramProvider != null, "paramProvider != null");
            
            sequenceContainer.AddAction((completeAction, failedAction) =>
            {
                Debug.Assert(actionWithoutConditions != null, $"{name} {actionWithoutConditions.Method.Name}");
                Debug.Assert(paramProvider != null, $"{name} {typeof(T)}");

                if (paramProvider == null)
                {
                    Debug.LogError($"parameter provider of type {typeof(T)} for action {name} is null");
                    failedAction();
                    return;
                }

                T param;

                try
                {
                    param = paramProvider();
                }
                catch (Exception e)
                {
                    Debug.LogError($"parameter provider of type {typeof(T)} for action {name} has thrown exception");
                    Debug.LogException(e);
                    failedAction();
                    return;
                }
                
                actionWithoutConditions(param);
                completeAction();
            });
            
            return this;
        }

        public SequentialAction Then<T1, T2>(Action<T1, T2, Action, Action> actionWithFullConditions, T1 param1, T2 param2)
        {
            sequenceContainer.AddAction((completeAction, failedAction) =>
            {
                Debug.Assert(actionWithFullConditions != null, $"{name} {actionWithFullConditions.Method.Name}");
                Debug.Assert(param1 != null, $"{name} {typeof(T1)}");
                Debug.Assert(param2 != null, $"{name} {typeof(T2)}");
                
                actionWithFullConditions(param1, param2, completeAction, failedAction);
            });
            
            return this;
        }
        
        public SequentialAction Then<T1, T2, T3>(Action<T1, T2, T3, Action, Action> actionWithFullConditions, T1 param1, T2 param2, T3 param3)
        {
            sequenceContainer.AddAction((completeAction, failedAction) =>
            {
                Debug.Assert(actionWithFullConditions != null, $"{name} {actionWithFullConditions.Method.Name}");
                // Debug.Assert(param1 != null, $"{name} {typeof(T1)}");
                // Debug.Assert(param2 != null, $"{name} {typeof(T2)}");
                // Debug.Assert(param3 != null, $"{name} {typeof(T3)}");
                
                actionWithFullConditions(param1, param2, param3, completeAction, failedAction);
            });
            
            return this;
        }
        
        public SequentialAction Then<T1, T2, T3, T4>(Action<T1, T2, T3, T4, Action, Action> actionWithFullConditions, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            sequenceContainer.AddAction((completeAction, failedAction) =>
            {
                Debug.Assert(actionWithFullConditions != null, $"{name} {actionWithFullConditions.Method.Name}");
                Debug.Assert(param1 != null, $"{name} {typeof(T1)}");
                Debug.Assert(param2 != null, $"{name} {typeof(T2)}");
                Debug.Assert(param3 != null, $"{name} {typeof(T3)}");
                Debug.Assert(param4 != null, $"{name} {typeof(T4)}");
                
                actionWithFullConditions(param1, param2, param3, param4, completeAction, failedAction);
            });
            
            return this;
        }
        
        public SequentialAction Then<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5, Action, Action> actionWithFullConditions, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5)
        {
            sequenceContainer.AddAction((completeAction, failedAction) =>
            {
                Debug.Assert(actionWithFullConditions != null, $"{name} {actionWithFullConditions.Method.Name}");
                Debug.Assert(param1 != null, $"{name} {typeof(T1)}");
                Debug.Assert(param2 != null, $"{name} {typeof(T2)}");
                Debug.Assert(param3 != null, $"{name} {typeof(T3)}");
                Debug.Assert(param4 != null, $"{name} {typeof(T4)}");
                Debug.Assert(param5 != null, $"{name} {typeof(T5)}");
                
                actionWithFullConditions(param1, param2, param3, param4, param5, completeAction, failedAction);
            });
            
            return this;
        }
        
        public SequentialAction Then<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6, Action, Action> actionWithFullConditions,
            T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6)
        {
            sequenceContainer.AddAction((completeAction, failedAction) =>
            {
                Debug.Assert(actionWithFullConditions != null, $"{name} {actionWithFullConditions.Method.Name}");
                Debug.Assert(param1 != null, $"{name} {typeof(T1)}");
                Debug.Assert(param2 != null, $"{name} {typeof(T2)}");
                Debug.Assert(param3 != null, $"{name} {typeof(T3)}");
                Debug.Assert(param4 != null, $"{name} {typeof(T4)}");
                Debug.Assert(param5 != null, $"{name} {typeof(T5)}");
                Debug.Assert(param5 != null, $"{name} {typeof(T6)}");
                
                actionWithFullConditions(param1, param2, param3, param4, param5, param6, completeAction, failedAction);
            });
            
            return this;
        }

        public ITaskAction Join<T>(Action<T> actionWithoutConditions, T param) =>
            Then(actionWithoutConditions, param);

        public ITaskAction Join<T1, T2>(Action<T1, T2> actionWithoutConditions, T1 param1, T2 param2) =>
            Then(actionWithoutConditions, param1, param2);

        public ITaskAction Join<T1, T2, T3>(Action<T1, T2, T3> actionWithoutConditions, T1 param1, T2 param2, T3 param3) =>
            Then(actionWithoutConditions, param1, param2, param3);

        public ITaskAction Join<T1, T2, T3, T4>(Action<T1, T2, T3, T4> actionWithoutConditions, T1 param1, T2 param2, T3 param3, T4 param4) =>
            Then(actionWithoutConditions, param1, param2, param3, param4);

        public ITaskAction Join<T>(Action<T, Action, Action> actionWithFullConditions, T param) =>
            Then(actionWithFullConditions, param);

        public ITaskAction Join<T1, T2>(Action<T1, T2, Action, Action> actionWithFullConditions, T1 param1, T2 param2) =>
            Then(actionWithFullConditions, param1, param2);

        public ITaskAction Join<T1, T2, T3>(Action<T1, T2, T3, Action, Action> actionWithFullConditions, T1 param1, T2 param2, T3 param3) =>
            Then(actionWithFullConditions, param1, param2, param3);

        public ITaskAction Join<T1, T2, T3, T4>(Action<T1, T2, T3, T4, Action, Action> actionWithFullConditions, T1 param1, T2 param2, T3 param3, T4 param4) =>
            Then(actionWithFullConditions, param1, param2, param3, param4);

        public ITaskAction Join<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5, Action, Action> actionWithFullConditions, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5) =>
            Then(actionWithFullConditions, param1, param2, param3, param4, param5);

        public ITaskAction Join<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6, Action, Action> actionWithFullConditions, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6) =>
            Then(actionWithFullConditions, param1, param2, param3, param4, param5, param6);
    }
}

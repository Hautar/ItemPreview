
//well, it is actually not generated

using System;
using UnityEngine;

namespace Tasks
{
    public partial class ConcurrentActionAsync : ITaskAction
    {
        public ConcurrentActionAsync With<T>(Action<T> actionWithoutConditions, T param)
        {
            actionContainer.AddAction(() =>
            {
                actionWithoutConditions(param);
                actionContainer.OnCompleteExternalCallback();
            });
            return this;
        }
        
        public ConcurrentActionAsync With<T1, T2>(Action<T1, T2> actionWithoutConditions, T1 param1, T2 param2)
        {
            actionContainer.AddAction(() =>
            {
                actionWithoutConditions(param1, param2);
                actionContainer.OnCompleteExternalCallback();
            });
            return this;
        }
    
        public ConcurrentActionAsync With<T1, T2, T3>(Action<T1, T2, T3> actionWithoutConditions, T1 param1, T2 param2, T3 param3)
        {
            actionContainer.AddAction(() =>
            {
                actionWithoutConditions(param1, param2, param3);
                actionContainer.OnCompleteExternalCallback();
            });
            return this;
        }
        
        public ConcurrentActionAsync With<T1, T2, T3, T4>(Action<T1, T2, T3, T4> actionWithoutConditions, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            actionContainer.AddAction(() =>
            {
                actionWithoutConditions(param1, param2, param3, param4);
                actionContainer.OnCompleteExternalCallback();
            });
            return this;
        }

        public ConcurrentActionAsync With<T>(Action<T, Action, Action> actionWithFullConditions, T param)
        {
            actionContainer.AddAction(() => actionWithFullConditions.Invoke(param, actionContainer.OnCompleteExternalCallback, actionContainer.OnFailExternalCallback));
            return this;
        }
        
        public ConcurrentActionAsync With<T1, T2>(Action<T1, T2, Action, Action> actionWithFullConditions, T1 param1, T2 param2)
        {
            actionContainer.AddAction(() => actionWithFullConditions.Invoke(param1, param2, actionContainer.OnCompleteExternalCallback, actionContainer.OnFailExternalCallback));
            return this;
        }
        
        public ConcurrentActionAsync With<T1, T2, T3>(Action<T1, T2, T3, Action, Action> actionWithFullConditions, T1 param1, T2 param2, T3 param3)
        {
            actionContainer.AddAction(() => actionWithFullConditions.Invoke(param1, param2, param3, actionContainer.OnCompleteExternalCallback, actionContainer.OnFailExternalCallback));
            return this;
        }
        
        public ConcurrentActionAsync With<T1, T2, T3, T4>(Action<T1, T2, T3, T4, Action, Action> actionWithFullConditions, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            actionContainer.AddAction(() => actionWithFullConditions.Invoke(param1, param2, param3, param4, actionContainer.OnCompleteExternalCallback, actionContainer.OnFailExternalCallback));
            return this;

        }
        
        public ConcurrentActionAsync With<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5, Action, Action> actionWithFullConditions, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5)
        {
            actionContainer.AddAction(() => actionWithFullConditions.Invoke(param1, param2, param3, param4, param5, actionContainer.OnCompleteExternalCallback, actionContainer.OnFailExternalCallback));
            return this;

        }
        
        public ConcurrentActionAsync With<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6, Action, Action> actionWithFullConditions,
            T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6)
        {
            actionContainer.AddAction(() => actionWithFullConditions.Invoke(param1, param2, param3, param4, param5, param6, actionContainer.OnCompleteExternalCallback, actionContainer.OnFailExternalCallback));
            return this;
        }

        public ITaskAction Join<T>(Action<T> actionWithoutConditions, T param) =>
            With(actionWithoutConditions, param);

        public ITaskAction Join<T1, T2>(Action<T1, T2> actionWithoutConditions, T1 param1, T2 param2) =>
            With(actionWithoutConditions, param1, param2);

        public ITaskAction Join<T1, T2, T3>(Action<T1, T2, T3> actionWithoutConditions, T1 param1, T2 param2, T3 param3) =>
            With(actionWithoutConditions, param1, param2, param3);

        public ITaskAction Join<T1, T2, T3, T4>(Action<T1, T2, T3, T4> actionWithoutConditions, T1 param1, T2 param2, T3 param3, T4 param4) =>
            With(actionWithoutConditions, param1, param2, param3, param4);

        public ITaskAction Join<T>(Action<T, Action, Action> actionWithFullConditions, T param) =>
            With(actionWithFullConditions, param);

        public ITaskAction Join<T1, T2>(Action<T1, T2, Action, Action> actionWithFullConditions, T1 param1, T2 param2) =>
            With(actionWithFullConditions, param1, param2);

        public ITaskAction Join<T1, T2, T3>(Action<T1, T2, T3, Action, Action> actionWithFullConditions, T1 param1, T2 param2, T3 param3) =>
            With(actionWithFullConditions, param1, param2, param3);

        public ITaskAction Join<T1, T2, T3, T4>(Action<T1, T2, T3, T4, Action, Action> actionWithFullConditions, T1 param1, T2 param2, T3 param3, T4 param4) =>
            With(actionWithFullConditions, param1, param2, param3, param4);

        public ITaskAction Join<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5, Action, Action> actionWithFullConditions, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5) =>
            With(actionWithFullConditions, param1, param2, param3, param4, param5);

        public ITaskAction Join<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6, Action, Action> actionWithFullConditions, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6) =>
            With(actionWithFullConditions, param1, param2, param3, param4, param5, param6);
    }
}

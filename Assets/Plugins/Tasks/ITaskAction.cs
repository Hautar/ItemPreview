using System;

namespace Tasks
{
    public interface ITaskAction
    {
        public ITaskAction Join(ITaskAction action);
        public ITaskAction Join(Action actionWithoutConditions);
        public ITaskAction Join(Action<Action> actionWithCompleteCondition);
        public ITaskAction Join(Action<Action, Action> actionWithFullConditions);
        public ITaskAction Join<T>(Action<T> actionWithoutConditions, T param);
        public ITaskAction Join<T1, T2>(Action<T1, T2> actionWithoutConditions, T1 param1, T2 param2);
        public ITaskAction Join<T1, T2, T3>(Action<T1, T2, T3> actionWithoutConditions, T1 param1, T2 param2, T3 param3);
        public ITaskAction Join<T1, T2, T3, T4>(Action<T1, T2, T3, T4> actionWithoutConditions, T1 param1, T2 param2, T3 param3, T4 param4);
        public ITaskAction Join<T>(Action<T, Action, Action> actionWithFullConditions, T param);
        public ITaskAction Join<T1, T2>(Action<T1, T2, Action, Action> actionWithFullConditions, T1 param1, T2 param2);
        public ITaskAction Join<T1, T2, T3>(Action<T1, T2, T3, Action, Action> actionWithFullConditions, T1 param1, T2 param2, T3 param3);
        public ITaskAction Join<T1, T2, T3, T4>(Action<T1, T2, T3, T4, Action, Action> actionWithFullConditions, T1 param1, T2 param2, T3 param3, T4 param4);
        public ITaskAction Join<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5, Action, Action> actionWithFullConditions, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5);
        public ITaskAction Join<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6, Action, Action> actionWithFullConditions, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6);

        //public void Finally(Action action);
        
        public void Run();

        internal void Run(Action onComplete, Action onFailed);
    }
}

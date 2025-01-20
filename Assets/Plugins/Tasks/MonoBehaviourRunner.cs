using System;
using System.Threading.Tasks;
using Monads;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tasks
{
    [ExecuteInEditMode]
    public class MonoBehaviourRunner : MonoBehaviour
    {
        private readonly DelegateQueue fixedUpdateQueue = new();
        private readonly DelegateQueue lateUpdateQueue = new();
        private readonly DelegateQueue updateQueue = new();

        private static MonoBehaviourRunner instance;

        private static MonoBehaviourRunner Instance
        {
            get
            {
#if UNITY_EDITOR
               EditorInitialize(); 
#endif

                return instance;
            }
        }

#if UNITY_EDITOR
        public static void EditorInitialize()
        {
            if (instance == null && !EditorApplication.isPlaying)
                CreateInstance();
        }
#endif
        
        private static void CreateInstance()
        {
            Debug.Log("Create MonoBehaviourRunner instance");
            GameObject go = new GameObject("MonoBehaviourRunner Root");
            go.hideFlags = HideFlags.HideAndDontSave;
            instance = go.AddComponent<MonoBehaviourRunner>();
            
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
#endif
                DontDestroyOnLoad(instance.gameObject);
        }

        public void OnDestroy()
        {
            Debug.Log("Destroy MonoBehaviourRunner");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Init()
        {
            CreateInstance();
            
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnPlayModeExit;

            void OnPlayModeExit(PlayModeStateChange playMode)
            {
                if (playMode != PlayModeStateChange.ExitingPlayMode)
                    return;

                EditorApplication.playModeStateChanged -= OnPlayModeExit;
                DestroyImmediate(instance);
            }
#endif
        }

        public static bool LogsEnabled
        {
            set
            {
                instance.fixedUpdateQueue.LogsEnabled = value;
                instance.lateUpdateQueue.LogsEnabled = value;
                instance.updateQueue.LogsEnabled = value;
            }
        }


        public static void EnqueueFixedUpdate(Action del, Action exceptionHandler = null, bool repeat = false, string name = null) =>
            Instance.fixedUpdateQueue.Enqueue(del, exceptionHandler, repeat, name);
        
        public static void EnqueueLateUpdate(Action del, Action exceptionHandler = null, bool repeat = false, string name = null) =>
            Instance.lateUpdateQueue.Enqueue(del, exceptionHandler, repeat, name);

        public static void EnqueueUpdate(Action del, Action exceptionHandler = null, bool repeat = false, string name = null) =>
            Instance.updateQueue.Enqueue(del, exceptionHandler, repeat, name);
        
        public static async Task<Result<bool>> ExecuteAsyncFixedUpdate(Action del,  string name = null) =>
            await Instance.fixedUpdateQueue.ExecuteAsync(del, name);
        
        public static async Task<Result<bool>> ExecuteAsyncLateUpdate(Action del,  string name = null) =>
            await Instance.lateUpdateQueue.ExecuteAsync(del, name);

        public static async Task<Result<bool>> ExecuteAsyncUpdate(Action del,  string name = null) =>
            await Instance.updateQueue.ExecuteAsync(del, name);
        
        public static async Task<Result<bool>> ExecuteAsyncFixedUpdate(Func<Result<bool>> del, string name = null) =>
            await Instance.fixedUpdateQueue.ExecuteAsync(del, name);
        
        public static async Task<Result<bool>> ExecuteAsyncLateUpdate(Func<Result<bool>> del,  string name = null) =>
            await Instance.lateUpdateQueue.ExecuteAsync(del, name);
        
        public static async Task<Result<bool>> ExecuteAsyncUpdate(Func<Result<bool>> del,  string name = null) =>
            await Instance.updateQueue.ExecuteAsync(del, name);

        public static async Task<Result<bool>> ExecuteAsyncFixedUpdate(Action<Action, Action> del, string name = null, int timeoutMs = 5000) =>
            await ExecuteAsyncInternal(Instance.fixedUpdateQueue, del, name, timeoutMs);
                
        public static async Task<Result<bool>> ExecuteAsyncLateUpdate(Action<Action, Action> del, string name = null, int timeoutMs = 5000) =>
            await ExecuteAsyncInternal(Instance.lateUpdateQueue, del, name, timeoutMs);
        
        public static async Task<Result<bool>> ExecuteAsyncUpdate(Action<Action, Action> del, string name = null, int timeoutMs = 5000) =>
            await ExecuteAsyncInternal(Instance.updateQueue, del, name, timeoutMs);

        private static async Task<Result<bool>> ExecuteAsyncInternal(DelegateQueue queue, Action<Action, Action> del, string name, int timeoutMs)
        {
            bool? isComplete = null;
            Func<Result<bool>?> condition = () =>
            {
                if (!isComplete.HasValue)
                    return null;
                
                if (isComplete.Value)
                    return Result.Ok();

                return Result.Error("MonoBehaviourRunner action failed. See logs for details");
            };
            
            await queue.ExecuteAsync(() => del(() => isComplete = true, () => isComplete = false), name);
            return await condition.WaitForMilliseconds(timeoutMs, name: name);
        }

        public static async Task<Result<bool>> ExecuteAsyncLateUpdate<TInput>(Action<TInput, Action, Action> del, TInput input, string name = null, int timeoutMs = 5000) =>
            await ExecuteAsyncInternal(Instance.lateUpdateQueue, del, input, name, timeoutMs);
        
        public static async Task<Result<bool>> ExecuteAsyncLateUpdate<TInput1, TInput2>(Action<TInput1, TInput2, Action, Action> del, TInput1 input1, TInput2 input2, string name = null, int timeoutMs = 5000) =>
            await ExecuteAsyncInternal(Instance.lateUpdateQueue, del, input1, input2, name, timeoutMs);
        
        private static async Task<Result<bool>> ExecuteAsyncInternal<TInput>(DelegateQueue queue, Action<TInput, Action, Action> del, TInput input, string name, int timeoutMs)
        {
            bool? isComplete = null;
            Func<Result<bool>?> condition = () =>
            {
                if (!isComplete.HasValue)
                    return null;
                
                if (isComplete.Value)
                    return Result.Ok();

                return Result.Error("MonoBehaviourRunner action failed. See logs for details");
            };
            
            await queue.ExecuteAsync(() => del(input, () => isComplete = true, () => isComplete = false), name);
            return await condition.WaitForMilliseconds(timeoutMs, name: name);
        }
        
        private static async Task<Result<bool>> ExecuteAsyncInternal<TInput1, TInput2>(DelegateQueue queue, Action<TInput1, TInput2, Action, Action> del, TInput1 input1, TInput2 input2, string name, int timeoutMs)
        {
            bool? isComplete = null;
            Func<Result<bool>?> condition = () =>
            {
                if (!isComplete.HasValue)
                    return null;
                
                if (isComplete.Value)
                    return Result.Ok();

                return Result.Error("MonoBehaviourRunner action failed. See logs for details");
            };
            
            await queue.ExecuteAsync(() => del(input1, input2, () => isComplete = true, () => isComplete = false), name);
            return await condition.WaitForMilliseconds(timeoutMs, name: name);
        }

        private void FixedUpdate() =>
            fixedUpdateQueue.Update();
        
        private void LateUpdate() =>
            lateUpdateQueue.Update();

        private void Update() =>
            updateQueue.Update();
    }
}

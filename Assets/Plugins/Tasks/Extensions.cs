using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Monads;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Tasks
{
    public static class Extensions
    {
        public static async Task<T> RetryAndValidateAsync<T>(this Func<Task<T>> factory, Func<T, bool> retryValidator, int retryCount = 3, int delayMs = 5000, Action onRetry = null, Action<Exception> onRetryException = null)
        {
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    var task = factory();
                    var result = await task;
                    
                    if (task.Status == TaskStatus.RanToCompletion && retryValidator(result))
                    {
                        return result;
                    }
                }
                catch (Exception e)
                {
                    if (onRetryException != null)
                        onRetryException(e);
                    else
                        Debug.LogWarning($"Handled retry exception {e.ToString()}");
                }

                if (onRetry != null)
                    onRetry();
                else
                    Debug.LogWarning($"retrying in {delayMs} ms, remaining attempts: {retryCount - i - 1}...");
                
                await Task.Delay(delayMs);
            }
            
            Debug.LogWarning("Retry task failed");
            return default;
        }
        
        public static async Task<Result<T>> RetryAndValidateAsyncWithResult<T>(this Func<Task<Result<T>>> factory, int retryCount = 3, int delayMs = 5000)
        {
            Result<T>? result = default;
            
            for (int i = 0; i < retryCount; i++)
            {
                result = null;
                
                try
                {
                    var task = factory();
                    result = await task;

                    if (task.Status != TaskStatus.RanToCompletion)
                    {
                        Debug.LogWarning($"Retry task invalid status: {task.Status}\n{task.Exception}");
                        continue;
                    }
                    
                    Debug.Assert(result.HasValue, "result.HasValue");

                    if (!result.Value.IsSuccess)
                    {
                        Debug.LogWarning(result.Value.Message);
                        continue;
                    }

                    return result.Value;
                }
                catch (Exception e)
                {
                    result = Result.Error<T>($"Handled retry exception {e.ToString()}");
                    Debug.LogWarning(result.Value.Message);
                }

                Debug.LogWarning($"Retrying in {delayMs} ms, remaining attempts: {retryCount - i - 1}...");
                await Task.Delay(delayMs);
            }

            if (!result.HasValue)
                result = Result.Error<T>($"Retry task failed after {retryCount} attempts. See logs for details");
            Debug.LogWarning("Retry task failed");
            return result.Value;
        }
        
        //not tested
        // public static async Task<T> RetryAndValidateAsync<T>(this Func<Task<T>> factory,
        //     Func<T, bool> retryValidator,
        //     int retryCount = 3,
        //     int delayMs = 5000,
        //     Action onRetry = null,
        //     Action<Exception> onRetryException = null) =>
        //     await RetryAndValidateAsyncWithExponentialBackoff(factory,
        //         retryValidator,
        //         retryCount, 
        //         delayMs,
        //         delayMs, 
        //         1, 
        //         0,
        //         onRetry,
        //         onRetryException);

        //https://stackoverflow.com/questions/4238345/asynchronously-wait-for-taskt-to-complete-with-timeout
        //future .net versions have task.WaitAsync() we dont have, thats why we do it this way
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, int timeoutMs)
        {
            using var timeoutCancellationTokenSource = new CancellationTokenSource();

            var completedTask = await Task.WhenAny(task, Task.Delay(timeoutMs, timeoutCancellationTokenSource.Token));
            if (completedTask == task) {
                timeoutCancellationTokenSource.Cancel();
                return await task;  // Very important in order to propagate exceptions
            } else {
                throw new TimeoutException("WatchDog: The operation has timed out.");
            }
        }
        
        public static async Task<TResult> TimeoutAfter<TResult>(Func<CancellationToken, Task<TResult>> taskFactory, int timeoutMs)
        {
            using var timeoutCancellationTokenSource = new CancellationTokenSource();
            var token = timeoutCancellationTokenSource.Token;
            var task = taskFactory(token);

            var completedTask = await Task.WhenAny(task, Task.Delay(timeoutMs, timeoutCancellationTokenSource.Token));
            if (completedTask == task) {
                timeoutCancellationTokenSource.Cancel();
                return await task;  // Very important in order to propagate exceptions
            } else 
            {
                timeoutCancellationTokenSource.Cancel();
                throw new TimeoutException("WatchDog: The operation has timed out.");
            }
        }

        //not tested
        //https://habr.com/ru/articles/227225/
        // public static async Task<T> RetryAndValidateAsyncWithExponentialBackoff<T>(this Func<Task<T>> factory,
        //     Func<T, bool> retryValidator,
        //     int retryCount = 3,
        //     int minDelayMs = 500,
        //     int maxDelayMs = 15000,
        //     int expFactor = 4,
        //     float delayJitterRate = 0.1f,
        //     Action onRetry = null,
        //     Action<Exception> onRetryException = null)
        // {
        //     Debug.Assert(minDelayMs > 0, "minDelayMs > 0");
        //     Debug.Assert(maxDelayMs >= minDelayMs, "maxDelayMs >= minDelayMs");
        //     Debug.Assert(expFactor >= 1, "expFactor >= 1");
        //     Debug.Assert(delayJitterRate >= 0, "delayJitterRate >=0");
        //     Debug.Assert(delayJitterRate < 1, "delayJitterRate < 1");
        //     
        //     int delay = 0;
        //     
        //     for (int i = 0; i < retryCount; i++)
        //     {
        //         try
        //         {
        //             var task = factory();
        //             var result = await task;
        //             
        //             if (task.Status == TaskStatus.RanToCompletion && retryValidator(result))
        //             {
        //                 return result;
        //             }
        //         }
        //         catch (Exception e)
        //         {
        //             if (onRetryException != null)
        //                 onRetryException(e);
        //             else
        //                 Debug.LogWarning($"Handled retry exception {e.ToString()}");
        //         }
        //
        //         delay = i == 0 ? minDelayMs : Mathf.Min(delay * expFactor, maxDelayMs);
        //         delay += UnityEngine.Random.Range(0, (int)(delay * delayJitterRate));
        //         
        //         if (onRetry != null)
        //             onRetry();
        //         else
        //             Debug.LogWarning($"retrying in {delay} ms, remaining attempts: {retryCount - i - 1}...");
        //         
        //         await Task.Delay(delay);
        //     }
        //     
        //     Debug.LogWarning("Retry task failed");
        //     return default;
        // }

        public static async Task<bool> RetryAsyncWithResult(this Action action, int retryCount = 3, int delayMs = 5000)
        {
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    var task = Task.Run(action);
                    await task;
                    
                    if (task.Status == TaskStatus.RanToCompletion)
                        return true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Handled retry exception {e.ToString()}");
                }

                Debug.LogWarning($"retrying in {delayMs} ms, remaining attempts: {retryCount - i - 1}...");
                await Task.Delay(delayMs);
            }
            
            Debug.LogWarning("Retry action failed");
            return false;
        }

        public static async Task<bool> RetryAsyncWithResult(this Func<AsyncOperationHandle> action, int retryCount = 3, int delayMs = 5000)
        {
            for (int k = 0; k < retryCount; k++)
            {
                try
                {
                    var asyncHandle = action();
                    await asyncHandle.Task;

                    if (asyncHandle.Status == AsyncOperationStatus.Succeeded)
                        return true;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Handled retry exception {exception}");
                }
                
                Debug.LogWarning($"retrying in {delayMs} ms, remaining attempts: {retryCount - k - 1}...");
                await Task.Delay(delayMs);
            }
            
            Debug.LogWarning("Retry action failed");
            return false;
        }

        public static async Task<bool> WaitForMilliseconds(this Func<bool?> condition, int timeoutMs, int checkPeriodMs = 100)
        {
            Debug.Assert(condition != null, "condition != null");
            Debug.Assert(timeoutMs > 0, "timeoutMs > 0");
            Debug.Assert(timeoutMs > checkPeriodMs, "timeoutMs > checkPeriodMs");
            Debug.Assert(checkPeriodMs > 0, "checkPeriodMs > 0");

            try
            {
                for (int timer = 0; timer < timeoutMs; timer += checkPeriodMs)
                {
                    var state = condition();
                    if (!state.HasValue)
                        await Task.Delay(checkPeriodMs);
                    else
                        return state.Value;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("WaitForSeconds failed\n" + e.ToString());
                return false;
            }

            return false;
        }

        public static IEnumerator WaitForMilliseconds(this Func<bool?> condition, Action<bool> result, int timeoutMs, int checkPeriodMs = 100)
        {
            Debug.Assert(condition != null, "condition != null");
            //no other way to return value from coroutine
            Debug.Assert(result != null, "result != null");
            Debug.Assert(timeoutMs > 0, "timeoutMs > 0");

            var wait = new WaitForSeconds(checkPeriodMs * 1e-3f);

            for (int timer = 0; timer < timeoutMs; timer += checkPeriodMs)
            {
                bool? state;
                try
                {
                    state = condition();
                }
                catch (Exception e)
                {
                    Debug.LogError("WaitForSeconds failed\n" + e.ToString());
                    result(false);
                    yield break;
                }
                
                if (!state.HasValue)
                    yield return wait;
                else
                {
                    result(state.Value);
                    yield break;
                }
            }

            result(false);
        }
        
        public static async Task<bool> WaitForMilliseconds(this Func<bool> condition, int timeoutMs, int checkPeriodMs = 100)
        {
            Debug.Assert(condition != null, "condition != null");
            Debug.Assert(timeoutMs > 0, "timeoutMs > 0");
            Debug.Assert(timeoutMs > checkPeriodMs, "timeoutMs > checkPeriodMs");
            Debug.Assert(checkPeriodMs > 0, "checkPeriodMs > 0");

            try
            {
                for (int timer = 0; timer < timeoutMs; timer += checkPeriodMs)
                {
                    var state = condition();
                    if (!state)
                        await Task.Delay(100);
                    else
                        return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("WaitForSeconds failed\n" + e.ToString());
                return false;
            }

            return false;
        }
        
        public static async Task<Result<T>> WaitForMilliseconds<T>(this Func<Result<T>?> condition, int timeoutMs, int checkPeriodMs = 100, string tag = null, string name = null)
        {
            Debug.Assert(condition != null, "condition != null");
            Debug.Assert(timeoutMs > 0, "timeoutMs > 0");
            Debug.Assert(timeoutMs > checkPeriodMs, "timeoutMs > checkPeriodMs");
            Debug.Assert(checkPeriodMs > 0, "checkPeriodMs > 0");

            try
            {
                for (int timer = 0; timer < timeoutMs; timer += checkPeriodMs)
                {
                    var state = condition();
                    if (!state.HasValue)
                        await Task.Delay(100);
                    else
                        return state.Value;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return Result.Error<T>($"{tag}{name} Exception occured while waiting for result.\n{e.Message}");
            }

            return Result.Error<T>($"{tag}{name}Timeout occured while waiting for result");
        }
        
        public static async Task<bool> WaitForFocusGain(int timeoutMs = 5000)
        {
            const int waitPeriodMs = 100;
            while (!Application.isFocused && timeoutMs > 0)
            {
                timeoutMs -= waitPeriodMs;
                await Task.Delay(waitPeriodMs);
            }

            return Application.isFocused;
        }
        
        public static async Task<bool> WaitForNetworkAvailable(int timeoutMs = 5000)
        {
            const int waitPeriodMs = 100;
            while (Application.internetReachability == NetworkReachability.NotReachable && timeoutMs > 0)
            {
                timeoutMs -= waitPeriodMs;
                await Task.Delay(waitPeriodMs);
            }

            return Application.internetReachability != NetworkReachability.NotReachable;
        }
    }
}
using System;
using System.Collections.Generic;

namespace ItemPreview.CallbackStore
{
    public class CallbackStorage<TKey, TResult>
    {
        private readonly Dictionary<TKey, CallbackContainer<TResult>> containerOverHash = new();

        public void Add(TKey hash, Action<TResult> onComplete, Action onFailed)
        {
            if (containerOverHash.TryGetValue(hash, out var container))
            {
                container.Add(onComplete, onFailed);
            }
            else
            {
                container = new CallbackContainer<TResult>(onComplete, onFailed);
                containerOverHash[hash] = container;
            }
        }

        public void Invoke(TKey hash, TResult sprite, bool result)
        {
            if(!containerOverHash.TryGetValue(hash, out var container))
                return;
            
            containerOverHash.Remove(hash);
            container.Invoke(sprite, result);
        }

        public void Clear()
        {
            containerOverHash.Clear();
        }
    }
}
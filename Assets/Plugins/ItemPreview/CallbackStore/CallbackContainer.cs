using System;
using UnityEngine;

namespace ItemPreview
{
    public class CallbackContainer<T>
    {
        private Action<T> onComplete;
        private Action    onFailed;

        public CallbackContainer(Action<T> onComplete, Action onFailed)
        {
            Debug.Assert(onComplete != null, "onComplete != null");
            Debug.Assert(onFailed != null, "onFailed != null");
            
            this.onComplete = onComplete;
            this.onFailed   = onFailed;
        }

        public void Add(Action<T> onComplete, Action onFailed)
        {
            this.onComplete += onComplete;
            this.onFailed   += onFailed;
        }

        public void Invoke(T sprite, bool result)
        {
            if (result)
            {
                onComplete?.Invoke(sprite);
                return;
            }
            
            onFailed?.Invoke();
        }
    }
}

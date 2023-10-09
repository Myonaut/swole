#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole.UI
{

    public class UITweenable : MonoBehaviour
    {

        public bool queueConsecutiveTweenCalls;

        protected TweenState currentTween;

        protected class TweenState
        {

            public LTDescr tween;

            public bool complete;

        }

        protected virtual void StartNewTween()
        {

            if (currentTween != null && !currentTween.complete && !queueConsecutiveTweenCalls)
            {

                currentTween.tween.callOnCompletes();

                LeanTween.cancel(currentTween.tween.uniqueId);

                currentTween = null;

            }

        }

        protected virtual void AppendTween(LTDescr tween, System.Action OnComplete = null, System.Action OnResume = null)
        {

            StartNewTween();

            System.Action prevOnComplete = tween._optional == null ? null : tween._optional.onComplete;

            if (currentTween != null && !currentTween.complete)
            {

                TweenState lastState = currentTween;

                tween.pause();

                IEnumerator CheckCompletePrevious()
                {

                    while (!lastState.complete)
                    {

                        yield return null;

                    }

                    OnResume?.Invoke();

                    tween.resume();

                }

                CoroutineProxy.Start(CheckCompletePrevious());

            }
            else
            {

                OnResume?.Invoke();

            }

            TweenState state = new TweenState() { tween = tween };

            tween.setOnComplete(() =>
            {

                prevOnComplete?.Invoke();

                state.complete = true;

                OnComplete?.Invoke();

            });

            currentTween = state;

        }

    }

}

#endif

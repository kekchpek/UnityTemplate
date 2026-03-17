using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using kekchpek.Auxiliary.AnimationControllerTool;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace kekchpek.Auxiliary
{
    public class AnimationController : MonoBehaviour
    {
        
        [SerializeField]
        private List<AnimationSequence> _sequences = new();

        private Dictionary<string, UniTaskCompletionSource> _activeSequences = new();
        private Func<string, string> _animationEvaluator;
        private Dictionary<string, Action> _animationCancellationActions = new();


        public bool IsPlaying => _activeSequences.Count > 0;

        public void SetAnimationEvaluator(Func<string, string> animationEvaluator) {
            _animationEvaluator = animationEvaluator;
        }

        /// <summary>
        /// Gets the active Spine skeleton and animation state. Returns the non-null one between SpineSkeleton and SpineSkeletonAnimation.
        /// </summary>
        private (Spine.Skeleton skeleton, Spine.AnimationState animationState) GetActiveSpineComponents(AnimationData animation)
        {
            var spineData = animation.SpineData;
            if (spineData == null)
            {
                return (null, null);
            }

            if (spineData.SpineSkeleton != null)
                return (spineData.SpineSkeleton.Skeleton, spineData.SpineSkeleton.AnimationState);
            if (spineData.SpineSkeletonAnimation != null)
                return (spineData.SpineSkeletonAnimation.Skeleton, spineData.SpineSkeletonAnimation.AnimationState);
            return (null, null);
        }

        private (Spine.Skeleton skeleton, Spine.AnimationState animationState) GetActiveSpineComponents(SpineClearTrackAnimationTypeData spineClearTrackData)
        {
            if (spineClearTrackData == null)
            {
                return (null, null);
            }

            if (spineClearTrackData.SpineSkeleton != null)
                return (spineClearTrackData.SpineSkeleton.Skeleton, spineClearTrackData.SpineSkeleton.AnimationState);
            if (spineClearTrackData.SpineSkeletonAnimation != null)
                return (spineClearTrackData.SpineSkeletonAnimation.Skeleton, spineClearTrackData.SpineSkeletonAnimation.AnimationState);
            return (null, null);
        }

        public async UniTask AwaitAnimationCompletion() 
        {
            await UniTask.WaitUntil(() => !IsPlaying);
        }

        /// <summary>
        /// Plays multiple sequences concurrently.
        /// </summary>
        /// <param name="sequenceNames">The names of the sequences to play concurrently.</param>
        /// <param name="isInstantTransition">If true, the state will be changed to the animation end immediately.</param>
        /// <returns>A UniTask that completes when all sequences have finished playing.</returns>
        public async UniTask PlaySequencesConcurrently(IEnumerable<string> sequenceNames, bool isInstantTransition = false)
        {
            var tasks = new List<UniTask>();
            
            foreach (var sequenceName in sequenceNames)
            {
                tasks.Add(PlaySequence(sequenceName, isInstantTransition, true));
            }
            
            await UniTask.WhenAll(tasks);
        }

        public bool HasSequence(string sequenceName) => _sequences.Any(s => s.SequenceName == sequenceName);

        /// <summary>
        /// Checks if a sequence is valid and can be played (has valid Spine components).
        /// </summary>
        /// <param name="sequenceName">The name of the sequence to check.</param>
        /// <returns>True if the sequence exists and has valid Spine components, otherwise false.</returns>
        public bool IsSequenceValid(string sequenceName)
        {
            var sequence = _sequences.Find(s => s.SequenceName == sequenceName);
            if (sequence == null || sequence.Animations.Count != 1)
                return false;

            var animation = sequence.Animations[0];
            if (animation.Type != AnimationType.Spine)
                return false;

            var activeSkeleton = GetActiveSpineComponents(animation);
            return activeSkeleton.animationState != null && activeSkeleton.skeleton != null;
        }

        /// <summary>
        /// Gets the duration of a sequence that contains a single Spine animation.
        /// </summary>
        /// <param name="sequenceName">The name of the sequence to get the duration for.</param>
        /// <returns>The duration of the sequence in seconds if it contains a single Spine animation, otherwise 0f.</returns>
        public float GetSequenceTime(string sequenceName)
        {
            var sequence = _sequences.Find(s => s.SequenceName == sequenceName);
            if (sequence == null)
            {
                Debug.LogError($"AnimationController: Sequence '{sequenceName}' not found");
                return 0f;
            }

            // Check if sequence contains exactly one animation
            if (sequence.Animations.Count != 1)
            {
                Debug.LogError($"AnimationController: GetSequenceTime only works for sequences with exactly one animation. Sequence '{sequenceName}' has {sequence.Animations.Count} animations.");
                return 0f;
            }

            var animation = sequence.Animations[0];
            
            // Check if the animation is of type Spine
            if (animation.Type == AnimationType.Spine)
            {
                var spineData = animation.SpineData;
                if (spineData == null)
                {
                    return 0f;
                }

                // Get the Spine animation duration
                var spineAnimationName = _animationEvaluator?.Invoke(spineData.AnimationName) ?? spineData.AnimationName;
                var activeSkeleton = GetActiveSpineComponents(animation);
                
                if (activeSkeleton.animationState != null &&
                    activeSkeleton.skeleton != null &&
                    !string.IsNullOrEmpty(spineAnimationName))
                {
                    var spineAnimation = activeSkeleton.skeleton.Data.FindAnimation(spineAnimationName);
                    if (spineAnimation != null)
                    {
                        return spineAnimation.Duration;
                    }
                    else
                    {
                        Debug.LogError($"AnimationController: No Spine animation with name '{spineAnimationName}' found in sequence '{sequenceName}'");
                        return 0f;
                    }
                }
                else
                {
                    return 0f;
                }
            }
            if (animation.Type == AnimationType.Unity)
            {
                var unityData = animation.UnityData;
                if (unityData?.UnityAnimator?.runtimeAnimatorController == null)
                {
                    return 0f;
                }

                foreach (var clip in unityData.UnityAnimator.runtimeAnimatorController.animationClips)
                {
                    if (clip.name == unityData.AnimationStateName)
                    {
                        return clip.length;
                    }
                }
                Debug.LogError($"AnimationController: No Unity animation with name '{unityData.AnimationStateName}' found in sequence '{sequenceName}'");
                return 0f;
            }
            Debug.LogError($"AnimationController: GetSequenceTime only works for sequences containing a single Spine or Unity animation. Sequence '{sequenceName}' contains a {animation.Type} animation.");
            return 0f;
        }

        /// <summary>
        /// Plays the sequence with the given name.
        /// </summary>
        /// <param name="sequenceName">The name of the sequence to play.</param>
        /// <param name="isInstantTransition">If true, the state will be changed to the animation end immediately.</param>
        /// <param name="allowConcurrent">If true, the sequence can be played concurrently with other sequences. If false, it will wait for other sequences to complete first.</param>
        /// <returns>A UniTask that completes when the sequence has finished playing.</returns>
        public async UniTask PlaySequence(string sequenceName, bool isInstantTransition = false, bool allowConcurrent = false)
        {
            if (!allowConcurrent){
                InterruptCurrentAnimations();
            }
            await PlaySequenceWithSpeed(sequenceName, 1.0f, isInstantTransition, allowConcurrent);
        }

        /// <summary>
        /// Plays the sequence with the given name at a specific speed.
        /// </summary>
        /// <param name="sequenceName">The name of the sequence to play.</param>
        /// <param name="speed">Speed multiplier for the sequence.</param>
        /// <param name="isInstantTransition">If true, the state will be changed to the animation end immediately.</param>
        /// <param name="allowConcurrent">If true, the sequence can be played concurrently with other sequences. If false, it will wait for other sequences to complete first.</param>
        /// <returns>A UniTask that completes when the sequence has finished playing. The result is animation time.</returns>
        public async UniTask<float> PlaySequence(string sequenceName, float speed, bool isInstantTransition = false, bool allowConcurrent = false)
        {
            if (!allowConcurrent){
                InterruptCurrentAnimations();
            }
            return await PlaySequenceWithSpeed(sequenceName, speed, isInstantTransition, allowConcurrent);
        }

        /// <summary>
        /// Starts the sequence animation at the given time with 0 timescale (paused).
        /// Only supports sequences with a single Unity or Spine animation.
        /// </summary>
        public void SetSequenceState(string sequenceName, float time)
        {
            var sequence = _sequences.Find(s => s.SequenceName == sequenceName);
            if (sequence == null)
            {
                Debug.LogError($"AnimationController: Sequence '{sequenceName}' not found");
                return;
            }

            if (sequence.Animations.Count == 0)
            {
                return;
            }

            var animation = sequence.Animations[0];
            SetAnimationStateAtTime(animation, time);
        }

        private void SetAnimationStateAtTime(AnimationData animation, float time)
        {
            switch (animation.Type)
            {
                case AnimationType.Unity:
                    SetUnityAnimationStateAtTime(animation.UnityData, time);
                    break;
                case AnimationType.Spine:
                    SetSpineAnimationStateAtTime(animation, time);
                    break;
            }
        }

        private void SetUnityAnimationStateAtTime(UnityAnimationTypeData unityData, float time)
        {
            if (unityData?.UnityAnimator == null || string.IsNullOrEmpty(unityData.AnimationStateName))
            {
                return;
            }

            var animationName = _animationEvaluator?.Invoke(unityData.AnimationStateName) ?? unityData.AnimationStateName;
            float length = 0f;
            if (unityData.UnityAnimator.runtimeAnimatorController != null)
            {
                foreach (var clip in unityData.UnityAnimator.runtimeAnimatorController.animationClips)
                {
                    if (clip.name == animationName)
                    {
                        length = clip.length;
                        break;
                    }
                }
            }

            if (length <= 0f)
            {
                return;
            }

            float normalizedTime = Mathf.Clamp01(time / length);
            unityData.UnityAnimator.speed = 0f;
            unityData.UnityAnimator.Play(animationName, -1, normalizedTime);
            unityData.UnityAnimator.Update(0f);
        }

        private void SetSpineAnimationStateAtTime(AnimationData animation, float time)
        {
            var spineData = animation.SpineData;
            if (spineData == null)
            {
                return;
            }

            var (skeleton, animationState) = GetActiveSpineComponents(animation);
            if (animationState == null || skeleton == null)
            {
                return;
            }

            var spineAnimationName = _animationEvaluator?.Invoke(spineData.AnimationName) ?? spineData.AnimationName;
            if (string.IsNullOrEmpty(spineAnimationName))
            {
                return;
            }

            var spineAnimation = skeleton.Data.FindAnimation(spineAnimationName);
            if (spineAnimation == null)
            {
                return;
            }

            animationState.TimeScale = 0f;
            animationState.SetAnimation(spineData.SpineAnimationLayer, spineAnimationName, false);
            animationState.Update(time);
        }

        /// <summary>
        /// Plays the sequence with the given name in a loop until PlaySequence is called.
        /// </summary>
        /// <param name="sequenceName">The name of the sequence to play in loop.</param>
        /// <param name="speed">Speed multiplier for the sequence.</param>
        public void PlaySequenceLooped(string sequenceName, float speed = 1.0f, bool allowConcurrent = false)
        {
            if (_animationCancellationActions.TryGetValue(sequenceName, out var cancellationAction))
            {
                Debug.Log($"Looped sequence {sequenceName} is already playing");
                return;
            }
            if (!allowConcurrent){
                InterruptCurrentAnimations();
            }
            var cts = new CancellationTokenSource();
            _animationCancellationActions.Add(sequenceName, () =>
            {
                cts.Cancel();
                cts.Dispose();
            });
            PlaySequenceLoopedInternal(sequenceName, speed, cts.Token, allowConcurrent).Forget();
        }

        public void CancelSequence(string sequenceName)
        {
            if (_animationCancellationActions.TryGetValue(sequenceName, out var cancellationAction))
            {
                cancellationAction.Invoke();
                _animationCancellationActions.Remove(sequenceName);
            }
        }

        /// <summary>
        /// Interrupts all currently playing animations.
        /// </summary>
        public void InterruptCurrentAnimations()
        {
            foreach (var cancellationTokenSource in _animationCancellationActions.Values)
            {
                cancellationTokenSource.Invoke();
            }
            _animationCancellationActions.Clear();
            _activeSequences.Clear();
        }

        private async UniTaskVoid PlaySequenceLoopedInternal(string sequenceName, float speed, CancellationToken cancellationToken, bool allowConcurrent = false)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var t = await PlaySequenceWithSpeed(sequenceName, speed, false, allowConcurrent);
                if (this)
                {
                    if (t == 0.0f)
                    {
                        Debug.LogWarning("Looped animation length is 0. Animation canceled.");
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Internal method to play a sequence with a specific speed.
        /// </summary>
        private async UniTask<float> PlaySequenceWithSpeed(string sequenceName, float speed, bool isInstantTransition = false, bool allowConcurrent = false)
        {
            // If not allowing concurrent and other sequences are playing, wait for them to complete
            if (!allowConcurrent && _activeSequences.Count > 0)
            {
                Debug.LogError($"AnimationController: Squence {sequenceName} will not be played due to other sequences are playing.");
                return 0f;
            }

            var sequence = _sequences.Find(s => s.SequenceName == sequenceName);
            if (sequence == null)
            {
                Debug.LogError($"AnimationController: Sequence '{sequenceName}' not found");
                return 0f;
            }

            float passedTime = 0f;
            if (_animationCancellationActions.Remove(sequenceName, out var cancellation))
            {
                cancellation.Invoke();
            }
            var tokenSource = new CancellationTokenSource();
            _animationCancellationActions.Add(sequenceName, () => {
                tokenSource.Cancel();
                tokenSource.Dispose();
            });
            var cancellationToken = tokenSource.Token;

            var completionSource = new UniTaskCompletionSource();
            _activeSequences[sequenceName] = completionSource;

            try
            {
                // Process animations in order, but allow parallel execution for flagged animations
                var activeParallelTasks = new List<UniTask<float>>();
                
                for (int i = 0; i < sequence.Animations.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var animation = sequence.Animations[i];
                    
                    // If this animation should run in parallel
                    if (animation.ExecuteInParallel)
                    {
                        // Start it immediately without waiting
                        activeParallelTasks.Add(PlayAnimation(animation, speed, isInstantTransition, cancellationToken));
                    }
                    else
                    {
                        // For sequential animations, wait for all previous parallel animations to complete
                        if (activeParallelTasks.Count > 0)
                        {
                            float max = 0f;
                            foreach (var t in await UniTask.WhenAll(activeParallelTasks))
                            {
                                if (t > max)
                                {
                                    max = t;
                                }
                            }
                            passedTime += max;
                            activeParallelTasks.Clear();
                        }
                        
                        // Then play this animation
                        passedTime += await PlayAnimation(animation, speed, isInstantTransition, cancellationToken);
                    }
                }
                
                // Wait for any remaining parallel animations to complete
                if (activeParallelTasks.Count > 0)
                {
                    float max = 0f;
                    foreach (var t in await UniTask.WhenAll(activeParallelTasks))
                    {
                        if (t > max)
                        {
                            max = t;
                        }
                    }
                    passedTime += max;
                }

                _activeSequences.Remove(sequenceName);
                completionSource.TrySetResult();
            }
            catch (OperationCanceledException)
            {
                // Animation was interrupted, this is expected
            }
            catch (Exception e)
            {
                completionSource.TrySetException(e);
            }
            finally
            {
                _activeSequences.Remove(sequenceName);
            }
            return passedTime;
        }

        private async UniTask<float> PlayAnimation(AnimationData animation, float speed, bool isInstantTransition, CancellationToken cancellationToken)
        {
            float playTime = 0f;
            switch (animation.Type)
            {
                case AnimationType.Unity:
                    var unityData = animation.UnityData;
                    if (unityData == null)
                    {
                        Debug.LogError($"AnimationController: Unity data is null for animation {animation.UnityData.AnimationStateName}");
                        break;
                    }

                    var animationName = _animationEvaluator?.Invoke(unityData.AnimationStateName) ?? unityData.AnimationStateName;
                    if (unityData.UnityAnimator != null && !string.IsNullOrEmpty(unityData.AnimationStateName))
                    {
                        // Store original speed and apply new speed
                        float originalSpeed = unityData.UnityAnimator.speed;
                        unityData.UnityAnimator.speed = speed;
                        
                        try
                        {
                            if (isInstantTransition)
                            {
                                unityData.UnityAnimator.Play(animationName, -1, 1f);
                                unityData.UnityAnimator.Update(0f);
                            }
                            else
                            {
                                unityData.UnityAnimator.Play(animationName, -1, 0f);
                                unityData.UnityAnimator.Update(0f);
                            }
                            if (!isInstantTransition)
                            {
                                float time = 0f;
                                foreach (var clip in unityData.UnityAnimator.runtimeAnimatorController.animationClips)
                                {
                                    if (clip.name == animationName)
                                    {
                                        time = clip.length;
                                        break;
                                    }
                                }
                                time = time / speed;
                                playTime += time;
                                await UniTask.Delay(TimeSpan.FromSeconds(time), cancellationToken: cancellationToken);
                            }
                        }
                        finally
                        {
                            // Restore original speed
                            if (unityData.UnityAnimator) // could be destroyed
                            {
                                unityData.UnityAnimator.speed = originalSpeed;
                            }
                        }
                    }
                    break;
                    
                case AnimationType.Spine:
                    var spineData = animation.SpineData;
                    if (spineData == null)
                    {
                        Debug.LogError($"AnimationController: Spine data is null for animation {animation.SpineData.AnimationName}");
                        break;
                    }

                    var spineAnimationName = _animationEvaluator?.Invoke(spineData.AnimationName) ?? spineData.AnimationName;
                    var activeSkeleton = GetActiveSpineComponents(animation);
                    if (activeSkeleton.animationState != null &&
                        activeSkeleton.skeleton != null &&
                        !string.IsNullOrEmpty(spineAnimationName))
                    {
                        var spineAnimation = activeSkeleton.skeleton.Data.FindAnimation(spineAnimationName);
                        if (spineAnimation != null)
                        {
                            // Store original time scale and apply new speed
                            activeSkeleton.animationState.TimeScale = speed;
                            activeSkeleton.animationState.SetAnimation(spineData.SpineAnimationLayer, spineAnimationName, false);
                            if (isInstantTransition)
                            {
                                activeSkeleton.animationState.Update(spineAnimation.Duration);
                            }
                            else
                            {
                                // Adjust delay time based on speed multiplier
                                float adjustedDuration = spineAnimation.Duration / speed;
                                if (adjustedDuration > 0f)
                                {
                                    playTime += adjustedDuration;
                                    await UniTask.Delay(TimeSpan.FromSeconds(adjustedDuration), cancellationToken: cancellationToken);
                                }
                            }
                        }
                        else 
                        {
                            Debug.LogError($"No animation with name {spineAnimationName}");
                        }
                    }
                    else 
                    {
                        Debug.LogError("Fail to play spine animation!");
                    }
                    break;
                    
                case AnimationType.AnimationController:
                    var animationControllerData = animation.AnimationControllerData;
                    if (animationControllerData?.TargetAnimationController != null && !string.IsNullOrEmpty(animationControllerData.TargetSequenceName))
                    {
                        // Play the sequence on the target animation controller with the specified speed
                        // Allow concurrent execution for nested controllers to maintain flexibility
                        playTime += await animationControllerData.TargetAnimationController.PlaySequence(animationControllerData.TargetSequenceName, speed, isInstantTransition, true);
                    }
                    else
                    {
                        Debug.LogError("AnimationController: Target animation controller or sequence name is missing");
                    }
                    break;
                case AnimationType.SpineClearTrack:
                    var clearTrackData = animation.SpineClearTrackData;
                    if (clearTrackData == null)
                    {
                        Debug.LogError("AnimationController: SpineClearTrack data is null");
                        break;
                    }

                    var clearTrackSpineComponents = GetActiveSpineComponents(clearTrackData);
                    if (clearTrackSpineComponents.animationState == null)
                    {
                        Debug.LogError("AnimationController: Fail to clear spine track because AnimationState is missing");
                        break;
                    }

                    float duration = Mathf.Max(0f, clearTrackData.Duration);
                    clearTrackSpineComponents.animationState.SetEmptyAnimation(clearTrackData.TrackIndex, duration);
                    if (!isInstantTransition && duration > 0f)
                    {
                        float adjustedDuration = duration / speed;
                        playTime += adjustedDuration;
                        await UniTask.Delay(TimeSpan.FromSeconds(adjustedDuration), cancellationToken: cancellationToken);
                    }
                    break;
            }
            return playTime;
        }

        private void OnDestroy()
        {
            InterruptCurrentAnimations();
        }
    }
}
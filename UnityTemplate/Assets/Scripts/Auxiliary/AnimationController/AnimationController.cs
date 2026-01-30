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
        private CancellationTokenSource _loopCancellationTokenSource;
        private CancellationTokenSource _animationCancellationTokenSource;

        public bool IsPlaying => _activeSequences.Count > 0;

        public void SetAnimationEvaluator(Func<string, string> animationEvaluator) {
            _animationEvaluator = animationEvaluator;
        }

        /// <summary>
        /// Gets the active Spine skeleton and animation state. Returns the non-null one between SpineSkeleton and SpineSkeletonAnimation.
        /// </summary>
        private (Spine.Skeleton skeleton, Spine.AnimationState animationState) GetActiveSpineComponents(AnimationData animation)
        {
            if (animation.SpineSkeleton != null)
                return (animation.SpineSkeleton.Skeleton, animation.SpineSkeleton.AnimationState);
            if (animation.SpineSkeletonAnimation != null)
                return (animation.SpineSkeletonAnimation.Skeleton, animation.SpineSkeletonAnimation.AnimationState);
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
            if (animation.Type != AnimationType.Spine)
            {
                Debug.LogError($"AnimationController: GetSequenceTime only works for sequences containing a single Spine animation. Sequence '{sequenceName}' contains a {animation.Type} animation.");
                return 0f;
            }

            // Get the Spine animation duration
            var spineAnimationName = _animationEvaluator?.Invoke(animation.AnimationName) ?? animation.AnimationName;
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

        /// <summary>
        /// Plays the sequence with the given name.
        /// </summary>
        /// <param name="sequenceName">The name of the sequence to play.</param>
        /// <param name="isInstantTransition">If true, the state will be changed to the animation end immediately.</param>
        /// <param name="allowConcurrent">If true, the sequence can be played concurrently with other sequences. If false, it will wait for other sequences to complete first.</param>
        /// <returns>A UniTask that completes when the sequence has finished playing.</returns>
        public async UniTask PlaySequence(string sequenceName, bool isInstantTransition = false, bool allowConcurrent = false)
        {
            CancelLoopedSequence();
            await PlaySequenceWithSpeed(sequenceName, 1.0f, isInstantTransition, allowConcurrent);
        }

        /// <summary>
        /// Plays the sequence with the given name at a specific speed.
        /// </summary>
        /// <param name="sequenceName">The name of the sequence to play.</param>
        /// <param name="speed">Speed multiplier for the sequence.</param>
        /// <param name="isInstantTransition">If true, the state will be changed to the animation end immediately.</param>
        /// <param name="allowConcurrent">If true, the sequence can be played concurrently with other sequences. If false, it will wait for other sequences to complete first.</param>
        /// <returns>A UniTask that completes when the sequence has finished playing.</returns>
        public async UniTask PlaySequence(string sequenceName, float speed, bool isInstantTransition = false, bool allowConcurrent = false)
        {
            CancelLoopedSequence();
            await PlaySequenceWithSpeed(sequenceName, speed, isInstantTransition, allowConcurrent);
        }

        /// <summary>
        /// Plays the sequence with the given name in a loop until PlaySequence is called.
        /// </summary>
        /// <param name="sequenceName">The name of the sequence to play in loop.</param>
        /// <param name="speed">Speed multiplier for the sequence.</param>
        public void PlaySequenceLooped(string sequenceName, float speed = 1.0f)
        {
            CancelLoopedSequence();
            _loopCancellationTokenSource = new CancellationTokenSource();
            PlaySequenceLoopedInternal(sequenceName, speed, _loopCancellationTokenSource.Token).Forget();
        }

        private void CancelLoopedSequence()
        {
            if (_loopCancellationTokenSource != null)
            {
                _loopCancellationTokenSource.Cancel();
                _loopCancellationTokenSource.Dispose();
                _loopCancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Interrupts all currently playing animations.
        /// </summary>
        public void InterruptCurrentAnimation()
        {
            CancelLoopedSequence();
            _activeSequences.Clear();
            if (_animationCancellationTokenSource != null)
            {
                _animationCancellationTokenSource.Cancel();
                _animationCancellationTokenSource.Dispose();
                _animationCancellationTokenSource = null;
            }
        }

        private async UniTaskVoid PlaySequenceLoopedInternal(string sequenceName, float speed, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await PlaySequenceWithSpeed(sequenceName, speed, false, true);
            }
        }

        /// <summary>
        /// Internal method to play a sequence with a specific speed.
        /// </summary>
        private async UniTask PlaySequenceWithSpeed(string sequenceName, float speed, bool isInstantTransition = false, bool allowConcurrent = false)
        {
            // If not allowing concurrent and other sequences are playing, wait for them to complete
            if (!allowConcurrent && _activeSequences.Count > 0)
            {
                Debug.LogError($"AnimationController: Squence {sequenceName} will not be played due to other sequences are playing.");
                return;
            }

            var sequence = _sequences.Find(s => s.SequenceName == sequenceName);
            if (sequence == null)
            {
                Debug.LogError($"AnimationController: Sequence '{sequenceName}' not found");
                return;
            }

            _animationCancellationTokenSource?.Dispose();
            _animationCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _animationCancellationTokenSource.Token;

            var completionSource = new UniTaskCompletionSource();
            _activeSequences[sequenceName] = completionSource;

            try
            {
                // Process animations in order, but allow parallel execution for flagged animations
                var activeParallelTasks = new List<UniTask>();
                
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
                            await UniTask.WhenAll(activeParallelTasks);
                            activeParallelTasks.Clear();
                        }
                        
                        // Then play this animation
                        await PlayAnimation(animation, speed, isInstantTransition, cancellationToken);
                    }
                }
                
                // Wait for any remaining parallel animations to complete
                if (activeParallelTasks.Count > 0)
                {
                    await UniTask.WhenAll(activeParallelTasks);
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
        }

        private async UniTask PlayAnimation(AnimationData animation, float speed, bool isInstantTransition, CancellationToken cancellationToken)
        {
            switch (animation.Type)
            {
                case AnimationType.Unity:
                    var animationName = _animationEvaluator?.Invoke(animation.AnimationStateName) ?? animation.AnimationStateName;
                    if (animation.UnityAnimator != null && !string.IsNullOrEmpty(animation.AnimationStateName))
                    {
                        // Store original speed and apply new speed
                        float originalSpeed = animation.UnityAnimator.speed;
                        animation.UnityAnimator.speed = speed;
                        
                        try
                        {
                            if (isInstantTransition)
                            {
                                animation.UnityAnimator.Play(animationName, -1, 1f);
                                animation.UnityAnimator.Update(0f);
                            }
                            else
                            {
                                animation.UnityAnimator.Play(animationName, -1, 0f);
                                animation.UnityAnimator.Update(0f);
                            }
                            if (!isInstantTransition)
                            {
                                var stateInfo = animation.UnityAnimator.GetCurrentAnimatorStateInfo(0);
                                await UniTask.Delay(TimeSpan.FromSeconds(stateInfo.length / speed), cancellationToken: cancellationToken);
                            }
                        }
                        finally
                        {
                            // Restore original speed
                            if (animation.UnityAnimator) // could be destroyed
                            {
                                animation.UnityAnimator.speed = originalSpeed;
                            }
                        }
                    }
                    break;
                    
                case AnimationType.Spine:
                    var spineAnimationName = _animationEvaluator?.Invoke(animation.AnimationName) ?? animation.AnimationName;
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
                            
                            activeSkeleton.animationState.SetAnimation(animation.SpineAnimationLayer, spineAnimationName, false);
                            
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
                    if (animation.TargetAnimationController != null && !string.IsNullOrEmpty(animation.TargetSequenceName))
                    {
                        // Play the sequence on the target animation controller with the specified speed
                        // Allow concurrent execution for nested controllers to maintain flexibility
                        await animation.TargetAnimationController.PlaySequence(animation.TargetSequenceName, speed, isInstantTransition, true);
                    }
                    else
                    {
                        Debug.LogError("AnimationController: Target animation controller or sequence name is missing");
                    }
                    break;
            }
        }

        private void OnDestroy()
        {
            InterruptCurrentAnimation();
        }
    }
}
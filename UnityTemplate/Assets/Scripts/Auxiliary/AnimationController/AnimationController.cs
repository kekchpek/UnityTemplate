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
        private bool _unscaledTime = false;
        
        [SerializeField]
        private List<AnimationSequence> _sequences = new();
        

        private Dictionary<string, UniTaskCompletionSource> _activeSequences = new();
        private Func<string, string> _animationEvaluator;
        private Dictionary<string, Action> _animationCancellationActions = new();
        private readonly Dictionary<string, ActiveSequenceDebugData> _activeSequencesDebugData = new();
        private readonly Dictionary<string, float> _sequenceExplicitAlphas = new();


        public bool IsPlaying => _activeSequences.Count > 0;

        public IReadOnlyList<ActiveSequenceDebugInfo> GetActiveSequencesDebugInfo()
        {
            var result = new List<ActiveSequenceDebugInfo>(_activeSequencesDebugData.Count);
            float currentTime = Time.time;
            foreach (var pair in _activeSequencesDebugData)
            {
                var data = pair.Value;
                float elapsedTime = Mathf.Max(0f, currentTime - data.StartTime);
                float cycleDuration = data.IsLooped
                    ? (data.LastCompletedCycleDuration > 0f ? data.LastCompletedCycleDuration : data.EstimatedDuration)
                    : data.EstimatedDuration;
                float progress = CalculateProgress(data.IsLooped, currentTime, data.CycleStartTime, elapsedTime, cycleDuration);
                result.Add(new ActiveSequenceDebugInfo(pair.Key, data.IsLooped, elapsedTime, cycleDuration, progress));
            }

            return result;
        }

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

            return GetSequenceTimelineDuration(sequence, sequenceName);
        }

        private float GetSequenceTimelineDuration(AnimationSequence sequence, string sequenceName)
        {
            float time = 0f;
            for (int i = 0; i < sequence.Animations.Count; i++)
            {
                var animation = sequence.Animations[i];
                var parallelMaxTime = GetAnimationDuration(animation, sequenceName);
                AnimationData nextAnimation = null;
                if (i < sequence.Animations.Count - 1)
                {
                    nextAnimation = sequence.Animations[i + 1];
                }

                while (nextAnimation != null && nextAnimation.ExecuteInParallel)
                {
                    i++;
                    var nextAnimTime = GetAnimationDuration(nextAnimation, sequenceName);
                    if (nextAnimTime > parallelMaxTime)
                    {
                        parallelMaxTime = nextAnimTime;
                    }

                    nextAnimation = null;
                    if (i < sequence.Animations.Count - 1)
                    {
                        nextAnimation = sequence.Animations[i + 1];
                    }
                }

                time += parallelMaxTime;
            }

            return time;
        }

        private float GetAnimationTime(AnimationData animation, string sequenceName) {
            // Object creation is instantaneous and contributes no time to the sequence timeline.
            if (animation.Type == AnimationType.ObjectCreation)
            {
                return 0f;
            }


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

        private float GetAnimationDuration(AnimationData animation, string sequenceName)
        {
            switch (animation.Type)
            {
                case AnimationType.Unity:
                case AnimationType.Spine:
                    return GetAnimationTime(animation, sequenceName);
                case AnimationType.SpineClearTrack:
                    return Mathf.Max(0f, animation.SpineClearTrackData?.Duration ?? 0f);
                case AnimationType.AnimationController:
                    var animationControllerData = animation.AnimationControllerData;
                    if (animationControllerData?.TargetAnimationController == null ||
                        string.IsNullOrEmpty(animationControllerData.TargetSequenceName))
                    {
                        return 0f;
                    }

                    return animationControllerData.TargetAnimationController.GetSequenceTime(animationControllerData.TargetSequenceName);
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Plays the sequence with the given name.
        /// </summary>
        /// <param name="sequenceName">The name of the sequence to play.</param>
        /// <param name="isInstantTransition">If true, the state will be changed to the animation end immediately.</param>
        /// <param name="allowConcurrent">If true, the sequence can be played concurrently with other sequences. If false, it will wait for other sequences to complete first.</param>
        /// <param name="startTime">Sequence timeline time to start playback from, in seconds.</param>
        /// <param name="endTime">Sequence timeline time to stop playback at, in seconds. Values less than 0 play until the sequence ends.</param>
        /// <returns>A UniTask that completes when the sequence has finished playing.</returns>
        public async UniTask PlaySequence(
            string sequenceName,
            bool isInstantTransition = false,
            bool allowConcurrent = false,
            float startTime = 0f,
            float endTime = -1f)
        {
            if (!allowConcurrent){
                InterruptCurrentAnimations();
            }
            RemoveActiveSequenceDebugData(sequenceName);
            if (_animationCancellationActions.Remove(sequenceName, out var cancellation))
            {
                cancellation.Invoke();
            }
            var tokenSource = new CancellationTokenSource();
            _animationCancellationActions.Add(sequenceName, () => {
                tokenSource.Cancel();
                tokenSource.Dispose();
                RemoveActiveSequenceDebugData(sequenceName);
            });
            var cancellationToken = tokenSource.Token;
            await PlaySequenceWithSpeed(sequenceName, 1.0f, cancellationToken, isInstantTransition, allowConcurrent, false, startTime, endTime);
        }

        /// <summary>
        /// Plays the sequence with the given name at a specific speed.
        /// </summary>
        /// <param name="sequenceName">The name of the sequence to play.</param>
        /// <param name="speed">Speed multiplier for the sequence.</param>
        /// <param name="isInstantTransition">If true, the state will be changed to the animation end immediately.</param>
        /// <param name="allowConcurrent">If true, the sequence can be played concurrently with other sequences. If false, it will wait for other sequences to complete first.</param>
        /// <param name="startTime">Sequence timeline time to start playback from, in seconds.</param>
        /// <param name="endTime">Sequence timeline time to stop playback at, in seconds. Values less than 0 play until the sequence ends.</param>
        /// <returns>A UniTask that completes when the sequence has finished playing. The result is animation time.</returns>
        public async UniTask<float> PlaySequence(
            string sequenceName,
            float speed,
            bool isInstantTransition = false,
            bool allowConcurrent = false,
            float startTime = 0f,
            float endTime = -1f)
        {
            if (!allowConcurrent){
                InterruptCurrentAnimations();
            }
            RemoveActiveSequenceDebugData(sequenceName);
            if (_animationCancellationActions.Remove(sequenceName, out var cancellation))
            {
                cancellation.Invoke();
            }
            var tokenSource = new CancellationTokenSource();
            _animationCancellationActions.Add(sequenceName, () => {
                tokenSource.Cancel();
                tokenSource.Dispose();
                RemoveActiveSequenceDebugData(sequenceName);
            });
            var cancellationToken = tokenSource.Token;
            return await PlaySequenceWithSpeed(sequenceName, speed, cancellationToken, isInstantTransition, allowConcurrent, false, startTime, endTime);
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

        /// <summary>
        /// Stores an explicit Spine mix alpha for a playing sequence and applies it to current track entries.
        /// The value is applied again whenever that sequence starts a Spine clip (e.g. each loop iteration). It is removed when the sequence run ends, is cancelled, or is interrupted.
        /// On failure (missing sequence, sequence not playing, or Unity animation in the sequence), logs an error and returns without changing alpha.
        /// </summary>
        /// <param name="sequenceName">Name of the sequence that is currently playing on this controller.</param>
        /// <param name="alpha">Value applied to <see cref="Spine.TrackEntry.Alpha"/> for each relevant track.</param>
        public void SetSequenceAlpha(string sequenceName, float alpha)
        {
            var sequence = _sequences.Find(s => s.SequenceName == sequenceName);
            if (sequence == null)
            {
                Debug.LogError($"AnimationController: SetSequenceAlpha: sequence '{sequenceName}' not found.");
                return;
            }

            foreach (var animation in sequence.Animations)
            {
                if (animation.Type == AnimationType.Unity)
                {
                    Debug.LogError(
                        $"AnimationController: SetSequenceAlpha: sequence '{sequenceName}' contains a Unity animation and cannot be used.");
                    return;
                }
            }

            if (!_activeSequences.ContainsKey(sequenceName))
            {
                Debug.LogError($"AnimationController: SetSequenceAlpha: sequence '{sequenceName}' is not playing.");
                return;
            }

            _sequenceExplicitAlphas[sequenceName] = alpha;

            foreach (var animation in sequence.Animations)
            {
                if (animation.Type != AnimationType.Spine)
                {
                    continue;
                }

                var spineData = animation.SpineData;
                if (spineData == null)
                {
                    continue;
                }

                var (_, animationState) = GetActiveSpineComponents(animation);
                if (animationState == null)
                {
                    continue;
                }

                var trackEntry = animationState.GetCurrent(spineData.SpineAnimationLayer);
                if (trackEntry != null)
                {
                    trackEntry.Alpha = alpha;
                }
            }
        }

        private float ResolveSpineAlphaForSequence(string playingSequenceName, float configuredAlpha)
        {
            if (!string.IsNullOrEmpty(playingSequenceName) &&
                _sequenceExplicitAlphas.TryGetValue(playingSequenceName, out var stored))
            {
                return stored;
            }

            return configuredAlpha;
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

            var track = animationState.SetAnimation(spineData.SpineAnimationLayer, spineAnimationName, false);
            track.Alpha = spineData.AnimationAlpha;
            track.TimeScale = 0f;
            track.TrackTime = time;
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
                RemoveActiveSequenceDebugData(sequenceName);
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
            RemoveActiveSequenceDebugData(sequenceName);
            _sequenceExplicitAlphas.Remove(sequenceName);
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
            _activeSequencesDebugData.Clear();
            _sequenceExplicitAlphas.Clear();
        }

        private async UniTaskVoid PlaySequenceLoopedInternal(string sequenceName, float speed, CancellationToken cancellationToken, bool allowConcurrent = false)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var t = await PlaySequenceWithSpeed(sequenceName, speed, cancellationToken, false, allowConcurrent, true);
                    MarkLoopCycleCompleted(sequenceName, t);
                    if (this)
                    {
                        if (t == 0.0f)
                        {
                            Debug.LogWarning($"Looped animation length is 0. Animation canceled.\nSequence name: {sequenceName}\nGameObject: {gameObject.name}");
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            finally
            {
                _sequenceExplicitAlphas.Remove(sequenceName);
            }
        }

        /// <summary>
        /// Internal method to play a sequence with a specific speed.
        /// </summary>
        private async UniTask<float> PlaySequenceWithSpeed(
            string sequenceName, float speed, 
            CancellationToken cancellationToken,
            bool isInstantTransition = false, bool allowConcurrent = false, bool isLooped = false,
            float startTime = 0f, float endTime = -1f)
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

            var resolvedEndTime = endTime;
            if (resolvedEndTime < 0f)
            {
                resolvedEndTime = GetSequenceTimelineDuration(sequence, sequenceName);
            }

            startTime = Mathf.Max(0f, startTime);
            resolvedEndTime = Mathf.Max(startTime, resolvedEndTime);
            if (startTime >= resolvedEndTime && !isInstantTransition)
            {
                return 0f;
            }

            RegisterActiveSequenceDebugData(sequence, speed, isInstantTransition, isLooped, startTime, resolvedEndTime);

            float passedTime = 0f;

            var completionSource = new UniTaskCompletionSource();
            _activeSequences[sequenceName] = completionSource;

            try
            {
                var activeParallelTasks = new List<UniTask<float>>();
                float timelineOffset = 0f;
                
                for (int i = 0; i < sequence.Animations.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested || timelineOffset >= resolvedEndTime)
                    {
                        break;
                    }
                    
                    var animation = sequence.Animations[i];
                    var parallelAnimations = new List<AnimationData> { animation };
                    
                    AnimationData nextAnimation = null;
                    if (i < sequence.Animations.Count - 1)
                    {
                        nextAnimation = sequence.Animations[i + 1];
                    }
                    while (nextAnimation != null && nextAnimation.ExecuteInParallel)
                    {
                        i++;
                        parallelAnimations.Add(nextAnimation);
                        nextAnimation = null;
                        if (i < sequence.Animations.Count - 1)
                        {
                            nextAnimation = sequence.Animations[i + 1];
                        }
                    }

                    float groupDuration = 0f;
                    foreach (var parallelAnimation in parallelAnimations)
                    {
                        var animationDuration = GetAnimationDuration(parallelAnimation, sequenceName);
                        if (animationDuration > groupDuration)
                        {
                            groupDuration = animationDuration;
                        }
                    }

                    var groupEndTime = timelineOffset + groupDuration;
                    if (groupEndTime <= startTime)
                    {
                        timelineOffset = groupEndTime;
                        continue;
                    }

                    var localStartTime = Mathf.Max(0f, startTime - timelineOffset);
                    var localEndTime = Mathf.Min(groupDuration, resolvedEndTime - timelineOffset);
                    foreach (var parallelAnimation in parallelAnimations)
                    {
                        activeParallelTasks.Add(PlayAnimation(
                            parallelAnimation,
                            speed,
                            isInstantTransition,
                            cancellationToken,
                            sequenceName,
                            localStartTime,
                            localEndTime));
                    }

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
                    timelineOffset = groupEndTime;
                }
                
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
                if (!isLooped || cancellationToken.IsCancellationRequested)
                {
                    _sequenceExplicitAlphas.Remove(sequenceName);
                }

                if (!isLooped)
                {
                    RemoveActiveSequenceDebugData(sequenceName);
                }
            }

            if (isInstantTransition)
            {
                return resolvedEndTime - startTime;
            }

            return passedTime;
        }

        private async UniTask<float> PlayAnimation(
            AnimationData animation,
            float speed,
            bool isInstantTransition,
            CancellationToken cancellationToken,
            string playingSequenceName,
            float animationStartTime = 0f,
            float animationEndTime = -1f)
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
                        float originalSpeed = unityData.UnityAnimator.speed;
                        unityData.UnityAnimator.speed = speed;
                        
                        try
                        {
                            float clipLength = 0f;
                            if (unityData.UnityAnimator.runtimeAnimatorController != null)
                            {
                                foreach (var clip in unityData.UnityAnimator.runtimeAnimatorController.animationClips)
                                {
                                    if (clip.name == animationName)
                                    {
                                        clipLength = clip.length;
                                        break;
                                    }
                                }
                            }

                            if (clipLength <= 0f)
                            {
                                break;
                            }

                            var resolvedAnimationEndTime = animationEndTime < 0f ? clipLength : Mathf.Min(animationEndTime, clipLength);
                            var resolvedAnimationStartTime = Mathf.Clamp(animationStartTime, 0f, resolvedAnimationEndTime);
                            var playDuration = resolvedAnimationEndTime - resolvedAnimationStartTime;
                            if (playDuration <= 0f)
                            {
                                break;
                            }

                            if (isInstantTransition)
                            {
                                unityData.UnityAnimator.Play(animationName, -1, resolvedAnimationEndTime / clipLength);
                                unityData.UnityAnimator.Update(0f);
                                playTime += playDuration / speed;
                            }
                            else
                            {
                                unityData.UnityAnimator.Play(animationName, -1, resolvedAnimationStartTime / clipLength);
                                unityData.UnityAnimator.Update(0f);
                                var adjustedDuration = playDuration / speed;
                                playTime += adjustedDuration;
                                await UniTask.Delay(TimeSpan.FromSeconds(adjustedDuration), cancellationToken: cancellationToken, ignoreTimeScale: _unscaledTime);
                                if (resolvedAnimationEndTime < clipLength && unityData.UnityAnimator)
                                {
                                    unityData.UnityAnimator.speed = 0f;
                                    unityData.UnityAnimator.Play(animationName, -1, resolvedAnimationEndTime / clipLength);
                                    unityData.UnityAnimator.Update(0f);
                                }
                            }
                        }
                        finally
                        {
                            if (unityData.UnityAnimator)
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
                            var resolvedAnimationEndTime = animationEndTime < 0f ? spineAnimation.Duration : Mathf.Min(animationEndTime, spineAnimation.Duration);
                            var resolvedAnimationStartTime = Mathf.Clamp(animationStartTime, 0f, resolvedAnimationEndTime);
                            var playDuration = resolvedAnimationEndTime - resolvedAnimationStartTime;
                            if (playDuration <= 0f)
                            {
                                break;
                            }

                            activeSkeleton.animationState.TimeScale = speed;
                            var spineTrack = activeSkeleton.animationState.SetAnimation(spineData.SpineAnimationLayer, spineAnimationName, false);
                            spineTrack.Alpha = ResolveSpineAlphaForSequence(playingSequenceName, spineData.AnimationAlpha);
                            spineTrack.TrackTime = resolvedAnimationStartTime;
                            if (isInstantTransition)
                            {
                                activeSkeleton.animationState.Update(resolvedAnimationEndTime);
                                playTime += playDuration / speed;
                            }
                            else
                            {
                                var adjustedDuration = playDuration / speed;
                                if (adjustedDuration > 0f)
                                {
                                    playTime += adjustedDuration;
                                    await UniTask.Delay(TimeSpan.FromSeconds(adjustedDuration), cancellationToken: cancellationToken, ignoreTimeScale: _unscaledTime);
                                }

                                if (resolvedAnimationEndTime < spineAnimation.Duration)
                                {
                                    spineTrack.TrackTime = resolvedAnimationEndTime;
                                    spineTrack.TimeScale = 0f;
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
                        if (this)
                        {
                            Debug.LogError("Fail to play spine animation!");
                        }
                    }
                    break;
                    
                case AnimationType.AnimationController:
                    var animationControllerData = animation.AnimationControllerData;
                    if (animationControllerData?.TargetAnimationController != null && !string.IsNullOrEmpty(animationControllerData.TargetSequenceName))
                    {
                        playTime += await animationControllerData.TargetAnimationController.PlaySequence(
                            animationControllerData.TargetSequenceName,
                            speed,
                            isInstantTransition,
                            true,
                            animationStartTime,
                            animationEndTime);
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
                    var resolvedClearEndTime = animationEndTime < 0f ? duration : Mathf.Min(animationEndTime, duration);
                    var resolvedClearStartTime = Mathf.Clamp(animationStartTime, 0f, resolvedClearEndTime);
                    var clearPlayDuration = resolvedClearEndTime - resolvedClearStartTime;
                    if (clearPlayDuration <= 0f)
                    {
                        break;
                    }

                    clearTrackSpineComponents.animationState.SetEmptyAnimation(clearTrackData.TrackIndex, duration);
                    if (!isInstantTransition)
                    {
                        var adjustedDuration = clearPlayDuration / speed;
                        playTime += adjustedDuration;
                        await UniTask.Delay(TimeSpan.FromSeconds(adjustedDuration), cancellationToken: cancellationToken, ignoreTimeScale: _unscaledTime);
                    }
                    else
                    {
                        playTime += clearPlayDuration / speed;
                    }
                    break;
                case AnimationType.ObjectCreation:
                    SpawnObject(animation.ObjectCreationData);
                    break;
            }
            return playTime;
        }

        private static void SpawnObject(ObjectCreationAnimationTypeData objectCreationData)
        {
            if (objectCreationData == null)
            {
                Debug.LogError("AnimationController: ObjectCreation data is null");
                return;
            }

            if (objectCreationData.ObjectToSpawn == null)
            {
                Debug.LogError("AnimationController: ObjectCreation ObjectToSpawn is null");
                return;
            }

            if (objectCreationData.Container == null)
            {
                Debug.LogError("AnimationController: ObjectCreation Container is null");
                return;
            }

            var instance = Instantiate(objectCreationData.ObjectToSpawn, objectCreationData.Container);
            var instanceTransform = instance.transform;
            instanceTransform.localPosition = Vector3.zero;
            instanceTransform.localRotation = Quaternion.identity;
            instanceTransform.localScale = Vector3.one;
        }

        private void OnDestroy()
        {
            InterruptCurrentAnimations();
        }

        private void RegisterActiveSequenceDebugData(
            AnimationSequence sequence,
            float speed,
            bool isInstantTransition,
            bool isLooped,
            float startTime = 0f,
            float endTime = -1f)
        {
            float currentTime = Time.time;
            float estimatedDuration = GetEstimatedSequenceDuration(sequence, speed, isInstantTransition, startTime, endTime);
            _activeSequencesDebugData[sequence.SequenceName] = new ActiveSequenceDebugData(
                isLooped,
                currentTime,
                currentTime,
                estimatedDuration,
                0f);
        }

        private void MarkLoopCycleCompleted(string sequenceName, float cycleDuration)
        {
            if (!_activeSequencesDebugData.TryGetValue(sequenceName, out var data))
            {
                return;
            }

            float currentTime = Time.time;
            _activeSequencesDebugData[sequenceName] = new ActiveSequenceDebugData(
                data.IsLooped,
                data.StartTime,
                currentTime,
                data.EstimatedDuration,
                cycleDuration > 0f ? cycleDuration : data.LastCompletedCycleDuration);
        }

        private void RemoveActiveSequenceDebugData(string sequenceName)
        {
            _activeSequencesDebugData.Remove(sequenceName);
        }

        private float GetEstimatedSequenceDuration(
            AnimationSequence sequence,
            float speed,
            bool isInstantTransition,
            float startTime = 0f,
            float endTime = -1f)
        {
            if (isInstantTransition || speed <= 0f)
            {
                return 0f;
            }

            var resolvedEndTime = endTime;
            if (resolvedEndTime < 0f)
            {
                resolvedEndTime = GetSequenceTimelineDuration(sequence, sequence.SequenceName);
            }

            startTime = Mathf.Max(0f, startTime);
            resolvedEndTime = Mathf.Max(startTime, resolvedEndTime);
            return (resolvedEndTime - startTime) / speed;
        }

        private float GetEstimatedAnimationDuration(AnimationData animation, float speed)
        {
            switch (animation.Type)
            {
                case AnimationType.Unity:
                    return GetEstimatedUnityAnimationDuration(animation.UnityData, speed);
                case AnimationType.Spine:
                    return GetEstimatedSpineAnimationDuration(animation, speed);
                case AnimationType.SpineClearTrack:
                    return GetEstimatedSpineClearTrackDuration(animation.SpineClearTrackData, speed);
                case AnimationType.AnimationController:
                    return GetEstimatedNestedControllerDuration(animation.AnimationControllerData, speed);
                case AnimationType.ObjectCreation:
                    return 0f;
                default:
                    return 0f;
            }
        }

        private float GetEstimatedUnityAnimationDuration(UnityAnimationTypeData unityData, float speed)
        {
            if (unityData?.UnityAnimator?.runtimeAnimatorController == null || speed <= 0f)
            {
                return 0f;
            }

            var animationName = _animationEvaluator?.Invoke(unityData.AnimationStateName) ?? unityData.AnimationStateName;
            if (string.IsNullOrEmpty(animationName))
            {
                return 0f;
            }

            foreach (var clip in unityData.UnityAnimator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == animationName)
                {
                    return clip.length / speed;
                }
            }

            return 0f;
        }

        private float GetEstimatedSpineAnimationDuration(AnimationData animation, float speed)
        {
            if (speed <= 0f)
            {
                return 0f;
            }

            var spineData = animation.SpineData;
            if (spineData == null)
            {
                return 0f;
            }

            var spineAnimationName = _animationEvaluator?.Invoke(spineData.AnimationName) ?? spineData.AnimationName;
            if (string.IsNullOrEmpty(spineAnimationName))
            {
                return 0f;
            }

            var activeSkeleton = GetActiveSpineComponents(animation);
            if (activeSkeleton.skeleton == null)
            {
                return 0f;
            }

            var spineAnimation = activeSkeleton.skeleton.Data.FindAnimation(spineAnimationName);
            if (spineAnimation == null)
            {
                return 0f;
            }

            return spineAnimation.Duration / speed;
        }

        private static float GetEstimatedSpineClearTrackDuration(SpineClearTrackAnimationTypeData clearTrackData, float speed)
        {
            if (clearTrackData == null || speed <= 0f)
            {
                return 0f;
            }

            return Mathf.Max(0f, clearTrackData.Duration) / speed;
        }

        private static float GetEstimatedNestedControllerDuration(AnimationControllerAnimationTypeData animationControllerData, float speed)
        {
            if (animationControllerData?.TargetAnimationController == null || string.IsNullOrEmpty(animationControllerData.TargetSequenceName) || speed <= 0f)
            {
                return 0f;
            }

            return animationControllerData.TargetAnimationController.GetSequenceTime(animationControllerData.TargetSequenceName) / speed;
        }

        private static float CalculateProgress(bool isLooped, float currentTime, float cycleStartTime, float elapsedTime, float cycleDuration)
        {
            if (cycleDuration <= 0f)
            {
                return 0f;
            }

            if (isLooped)
            {
                float cycleElapsed = Mathf.Max(0f, currentTime - cycleStartTime);
                return Mathf.Clamp01(Mathf.Repeat(cycleElapsed, cycleDuration) / cycleDuration);
            }

            return Mathf.Clamp01(elapsedTime / cycleDuration);
        }

        public readonly struct ActiveSequenceDebugInfo
        {
            public readonly string SequenceName;
            public readonly bool IsLooped;
            public readonly float ElapsedTime;
            public readonly float Duration;
            public readonly float Progress;

            public ActiveSequenceDebugInfo(string sequenceName, bool isLooped, float elapsedTime, float duration, float progress)
            {
                SequenceName = sequenceName;
                IsLooped = isLooped;
                ElapsedTime = elapsedTime;
                Duration = duration;
                Progress = progress;
            }
        }

        private readonly struct ActiveSequenceDebugData
        {
            public readonly bool IsLooped;
            public readonly float StartTime;
            public readonly float CycleStartTime;
            public readonly float EstimatedDuration;
            public readonly float LastCompletedCycleDuration;

            public ActiveSequenceDebugData(
                bool isLooped,
                float startTime,
                float cycleStartTime,
                float estimatedDuration,
                float lastCompletedCycleDuration)
            {
                IsLooped = isLooped;
                StartTime = startTime;
                CycleStartTime = cycleStartTime;
                EstimatedDuration = estimatedDuration;
                LastCompletedCycleDuration = lastCompletedCycleDuration;
            }
        }
    }
}
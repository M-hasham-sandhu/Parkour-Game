using System.Collections;
using UnityEngine;

namespace ParkourController
{
    public class ParkourActionController : MonoBehaviour
    {
        [System.Serializable]
        public class ParkourAnimationClip
        {
            [Tooltip("Exact Animator state name for this clip.")]
            public string stateName;
            [Tooltip("Length of the clip in seconds - set this to match the imported clip's actual length, " +
                      "so the scripted movement finishes exactly when the animation does.")]
            public float duration = 1f;
            [Tooltip("How far past the obstacle's front face the player ends up (X/Z only). " +
                      "For Vault this is the landing spot on the far side; for Mantle it's usually left at 0 " +
                      "since topPoint already puts you on the obstacle.")]
            public float forwardOvershoot = 0.4f;
            [Tooltip("Shapes the movement over time: X = normalized time (0-1), Y = normalized distance (0-1). " +
                      "Default is linear - try an ease-out curve for vault (fast start, settle at the end).")]
            public AnimationCurve movementCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }

        [System.Serializable]
        public class ParkourAnimationSet
        {
            public ObstacleType obstacleType;
            [Tooltip("Multiple clips = randomized variety. One is picked at random each time this type triggers.")]
            public ParkourAnimationClip[] clips;
        }

        [Header("Animation Sets")]
        public ParkourAnimationSet[] animationSets;

        [Header("Blend / Return")]
        public float blendInTime = 0.1f;
        [Tooltip("State name to return to once the action finishes (your locomotion/blend-tree state).")]
        public string locomotionStateName = "Locomotion";
        public float blendOutTime = 0.15f;

        private Animator _animator;
        private CharacterController _characterController;

        public bool IsPerforming { get; private set; }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _characterController = GetComponent<CharacterController>();
        }

        public void PerformAction(ObstacleInfo info)
        {
            if (IsPerforming) return; // already mid-action, ignore re-triggers

            ParkourAnimationClip clip = PickClip(info.type);
            if (clip == null)
            {
                Debug.LogWarning($"ParkourActionController: no animation clips configured for {info.type}.");
                return;
            }

            Vector3 faceDirection = info.hitPoint - transform.position;
            faceDirection.y = 0f;
            if (faceDirection.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(faceDirection.normalized);

            Vector3 startPos = transform.position;
            Vector3 targetPos = CalculateTargetPosition(info, startPos);

            _animator.applyRootMotion = false;
            _animator.CrossFadeInFixedTime(clip.stateName, blendInTime);

            IsPerforming = true;
            StartCoroutine(RunAction(clip, startPos, targetPos));
        }

        private Vector3 CalculateTargetPosition(ObstacleInfo info, Vector3 startPos)
        {
            Vector3 fwd = transform.forward;

            if (info.type == ObstacleType.Mantle)
            {
                // End up standing on top of the obstacle.
                Vector3 target = info.topPoint;
                target.y = info.topPoint.y; 
                return target;
            }

           
            float distanceToClear = Vector3.Distance(new Vector3(startPos.x, 0, startPos.z),
                                                       new Vector3(info.hitPoint.x, 0, info.hitPoint.z));
            Vector3 landingXZ = startPos + fwd * (distanceToClear + 0.6f);
            landingXZ.y = startPos.y;
            return landingXZ;
        }

        private ParkourAnimationClip PickClip(ObstacleType type)
        {
            foreach (var set in animationSets)
            {
                if (set.obstacleType != type) continue;
                if (set.clips == null || set.clips.Length == 0) continue;
                return set.clips[Random.Range(0, set.clips.Length)];
            }
            return null;
        }

        private IEnumerator RunAction(ParkourAnimationClip clip, Vector3 startPos, Vector3 targetPos)
        {
            float elapsed = 0f;
            Vector3 lastPos = startPos;

            while (elapsed < clip.duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / clip.duration);
                float normalizedDistance = clip.movementCurve.Evaluate(normalizedTime);

                Vector3 desiredPos = Vector3.LerpUnclamped(startPos, targetPos, normalizedDistance);
                Vector3 delta = desiredPos - lastPos;

                if (_characterController != null && _characterController.enabled)
                    _characterController.Move(delta);
                else
                    transform.position = desiredPos;

                lastPos = desiredPos;
                yield return null;
            }

            // Snap to the exact target at the end so small float drift from
            // repeated Move() calls doesn't leave the player slightly off.
            Vector3 finalDelta = targetPos - lastPos;
            if (_characterController != null && _characterController.enabled)
                _characterController.Move(finalDelta);
            else
                transform.position = targetPos;

            _animator.CrossFadeInFixedTime(locomotionStateName, blendOutTime);
            IsPerforming = false;
        }
    }
}
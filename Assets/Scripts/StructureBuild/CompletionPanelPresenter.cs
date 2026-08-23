using UnityEngine;

namespace StructureBuild
{
    /// <summary>
    /// Places the transient level-complete card in front of the active XR
    /// view. The card is world space (not head locked): its pose is sampled
    /// once when the level completes so players can lean or walk around it.
    /// </summary>
    public sealed class CompletionPanelPresenter : MonoBehaviour
    {
        public DesignPlayerStart designStart;
        [Min(0.8f)] public float viewingDistance = 2.35f;
        public float eyeHeightOffset = -0.16f;

        public void PlaceInFrontOfPlayer()
        {
            var view = Camera.main;
            var eye = view != null ? view.transform.position : designStart != null ? designStart.DesignedEyePosition : transform.position;
            var forward = view != null ? view.transform.forward : designStart != null ? designStart.DesignedEyeRotation * Vector3.forward : Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();

            var position = eye + forward * viewingDistance;
            position.y = Mathf.Max(0.95f, eye.y + eyeHeightOffset);
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(eye - position, Vector3.up));
        }
    }
}

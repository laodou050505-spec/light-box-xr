using UnityEngine;

namespace StructureBuild
{
    /// <summary>
    /// The single authored opening pose for both the desktop Game View and the
    /// PICO XR rig.  It is a generated gameplay anchor, not a user-authored
    /// scene model: the puzzle table remains the spatial landmark.
    /// </summary>
    public sealed class DesignPlayerStart : MonoBehaviour
    {
        [Header("Shared composition")]
        public Transform focalAnchor;
        public Vector3 focalOffset = Vector3.up * 0.85f;
        [Tooltip("Horizontal angle around the puzzle table.  45° is the authored opening three-quarter view.")]
        public float yaw = 45f;
        [Tooltip("Horizontal table-viewing distance from the shared puzzle focus to the designed eye point.")]
        public float viewingDistance = 9.18f;
        [Tooltip("World floor-to-eye height for the authored opening composition. The XR rig preserves the tracked head's local 6DoF offset.")]
        public float eyeHeight = 7.49f;
        [Tooltip("The non-tracked local head pose used only to draw the editable rig-start anchor in the Scene view.")]
        public Vector3 fallbackHeadLocalPosition = Vector3.up * 1.60f;

        public Vector3 FocusPosition => focalAnchor != null ? focalAnchor.TransformPoint(focalOffset) : transform.position + transform.forward * viewingDistance;

        public Vector3 DesignedEyePosition
        {
            get
            {
                // This is deliberately a horizontal orbit plus a single floor
                // eye height. The desktop pitch is derived from this same
                // point; it is not a second, independently authored camera.
                var horizontalOffset = Quaternion.Euler(0f, yaw, 0f) * Vector3.back * viewingDistance;
                return new Vector3(
                    FocusPosition.x + horizontalOffset.x,
                    eyeHeight,
                    FocusPosition.z + horizontalOffset.z);
            }
        }

        public Quaternion DesignedEyeRotation
        {
            get
            {
                var direction = FocusPosition - DesignedEyePosition;
                return direction.sqrMagnitude > 0.0001f
                    ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                    : Quaternion.Euler(0f, yaw, 0f);
            }
        }

        public Vector3 DesignedRigPosition => DesignedEyePosition - DesignedRigRotation * fallbackHeadLocalPosition;
        public Quaternion DesignedRigRotation => Quaternion.Euler(0f, DesignedEyeRotation.eulerAngles.y, 0f);

        public float DerivedDesktopPitch
        {
            get
            {
                var offset = DesignedEyePosition - FocusPosition;
                var horizontalDistance = new Vector2(offset.x, offset.z).magnitude;
                return Mathf.Atan2(offset.y, Mathf.Max(0.001f, horizontalDistance)) * Mathf.Rad2Deg;
            }
        }

        public float DerivedDesktopDistance => Vector3.Distance(DesignedEyePosition, FocusPosition);

        /// <summary>Sync the visible editor anchor from the same values used at runtime.</summary>
        public void SynchronizeAnchor()
        {
            transform.SetPositionAndRotation(DesignedRigPosition, DesignedRigRotation);
        }

        public void ApplyDesktopPose(DesktopCameraOrbit orbit)
        {
            if (orbit == null) return;
            orbit.SetPose(FocusPosition, yaw, DerivedDesktopPitch, DerivedDesktopDistance);
        }

        /// <summary>
        /// Restores the authored XR root.  The camera's local tracking pose is
        /// deliberately not overwritten, so the player can still physically
        /// lean, walk and look around the tabletop after reset/recenter.
        /// </summary>
        public void ApplyXrRigPose(Transform rig, Transform trackedHead = null)
        {
            if (rig == null) return;
            var rotation = DesignedRigRotation;
            // Read the live local head offset before moving the root. This
            // keeps real 6DoF lean/walk tracking intact while placing the
            // player's actual eye at the single authored design eye point.
            var localHeadOffset = trackedHead != null ? trackedHead.localPosition : fallbackHeadLocalPosition;
            rig.SetPositionAndRotation(DesignedEyePosition - rotation * localHeadOffset, rotation);
            SynchronizeAnchor();
        }

        public void FaceWorldPanel(Transform panel)
        {
            if (panel == null) return;
            var direction = DesignedEyePosition - panel.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = -transform.forward;
            panel.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}

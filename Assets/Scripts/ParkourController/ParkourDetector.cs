using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ParkourController
{
    public class ParkourDetector : MonoBehaviour
    {
        [Header("Ray Origin")]
        [Tooltip("Place at shin/knee height on the rig.")]
        public Transform feetRayOrigin;

        [Header("Detection")]
        public float forwardRayDistance = 1f;
        public LayerMask obstacleMask;
        public float topSearchHeight = 2f; // how high above feet we search downward for the top surface

        [Header("Thresholds - tune to your character")]
        public float minVaultHeight = 0.25f;
        public float maxVaultHeight = 0.9f;
        public float maxMantleHeight = 1.5f;

        // --- Debug cache, populated every DetectObstacle() call, used only for gizmos ---
        private bool _hasDebugData;
        private bool _forwardHit;
        private Vector3 _downOrigin;
        private bool _downHit;
        private ObstacleInfo _lastInfo;

        public ObstacleInfo DetectObstacle()
        {
            _hasDebugData = true;
            _forwardHit = false;
            _downHit = false;
            _lastInfo = ObstacleInfo.None;

            if (feetRayOrigin == null)
            {
                Debug.LogWarning("ParkourDetector: feetRayOrigin not assigned.");
                return ObstacleInfo.None;
            }

            Vector3 fwd = transform.forward;
            float feetY = feetRayOrigin.position.y;

            // 1) Does an obstacle exist in front of us?
            if (!Physics.Raycast(feetRayOrigin.position, fwd, out RaycastHit hit, forwardRayDistance, obstacleMask))
                return ObstacleInfo.None;
            _forwardHit = true;

            // 2) How tall is it? Cast down from above the hit point, nudged slightly
            // forward so we land on TOP of the obstacle rather than back on its face.
            Vector3 downOrigin = hit.point + fwd * 0.1f;
            downOrigin.y = feetY + topSearchHeight;
            _downOrigin = downOrigin;

            if (!Physics.Raycast(downOrigin, Vector3.down, out RaycastHit topHit, topSearchHeight + 1f, obstacleMask))
                return ObstacleInfo.None; // too tall to find a top within our search range
            _downHit = true;

            float height = topHit.point.y - feetY;

            // 3) Classify by height alone.
            ObstacleType type;
            if (height >= minVaultHeight && height <= maxVaultHeight)
                type = ObstacleType.Vault;
            else if (height <= maxMantleHeight)
                type = ObstacleType.Mantle;
            else
                type = ObstacleType.None;

            var info = new ObstacleInfo
            {
                detected = true,
                type = type,
                height = height,
                topPoint = topHit.point,
                hitPoint = hit.point
            };
            _lastInfo = info;
            return info;
        }

        /// <summary>
        /// Draws a fixed vertical scale off to the player's side showing the
        /// height thresholds as labeled tick marks, plus a marker for the
        /// current obstacle's measured height. This is what actually answers
        /// "is this obstacle in the vault zone or the mantle zone" at a glance,
        /// instead of relying on remembering what green/orange mean.
        /// </summary>
        private void DrawHeightRuler()
        {
            Vector3 basePos = feetRayOrigin.position + transform.right * 0.6f; // offset to the side so it doesn't overlap the obstacle
            float topOfRuler = maxMantleHeight + 0.3f;

            // Spine of the ruler
            Gizmos.color = Color.white;
            Gizmos.DrawLine(basePos, basePos + Vector3.up * topOfRuler);

            DrawTick(basePos, minVaultHeight, Color.green, "Vault min");
            DrawTick(basePos, maxVaultHeight, new Color(1f, 0.6f, 0f), "Vault max / Mantle starts");
            DrawTick(basePos, maxMantleHeight, Color.red, "Mantle max (too tall above this)");

            // Marker for the obstacle we actually detected this call, if any.
            if (_hasDebugData && _lastInfo.detected)
            {
                Color markerColor = ColorForType(_lastInfo.type);
                Vector3 markerPos = basePos + Vector3.up * _lastInfo.height;
                Gizmos.color = markerColor;
                Gizmos.DrawSphere(markerPos, 0.05f);
                Gizmos.DrawLine(basePos + Vector3.up * _lastInfo.height, markerPos + transform.right * 0.15f);
#if UNITY_EDITOR
                Handles.color = markerColor;
                Handles.Label(markerPos + transform.right * 0.18f,
                    $"THIS: {_lastInfo.type} ({_lastInfo.height:F2}m)",
                    new GUIStyle { normal = { textColor = markerColor }, fontStyle = FontStyle.Bold, fontSize = 12 });
#endif
            }
        }

        private void DrawTick(Vector3 basePos, float height, Color color, string label)
        {
            Vector3 tickPos = basePos + Vector3.up * height;
            Gizmos.color = color;
            Gizmos.DrawLine(tickPos - transform.right * 0.1f, tickPos + transform.right * 0.1f);
#if UNITY_EDITOR
            Handles.color = color;
            Handles.Label(tickPos - transform.right * 0.35f,
                $"{label} ({height:F2}m)",
                new GUIStyle { normal = { textColor = color }, fontSize = 10 });
#endif
        }

        private static Color ColorForType(ObstacleType type)
        {
            switch (type)
            {
                case ObstacleType.Vault: return Color.green;
                case ObstacleType.Mantle: return new Color(1f, 0.6f, 0f); // orange
                default: return Color.red;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (feetRayOrigin == null) return;
            Vector3 fwd = transform.forward;

            DrawHeightRuler();

            // --- Ray 1: forward probe from the feet ---
            Gizmos.color = _hasDebugData && _forwardHit ? Color.yellow : new Color(1f, 1f, 1f, 0.35f);
            Gizmos.DrawRay(feetRayOrigin.position, fwd * forwardRayDistance);
            Gizmos.DrawWireSphere(feetRayOrigin.position, 0.03f);

            if (!_hasDebugData) return;

            // --- Ray 2: downward probe for the top surface ---
            if (_forwardHit)
            {
                Gizmos.color = _downHit ? Color.cyan : new Color(1f, 0f, 0f, 0.5f);
                Gizmos.DrawRay(_downOrigin, Vector3.down * (topSearchHeight + 1f));
                Gizmos.DrawWireSphere(_downOrigin, 0.03f); // where the down-ray starts, for sanity-checking placement
            }

            if (!_lastInfo.detected)
            {
                // Obstacle existed at feet height but we couldn't resolve a usable top -
                // draw the fail point in red so it's obvious detection failed here, not silently.
                if (_forwardHit)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(_downOrigin + Vector3.down * (topSearchHeight + 1f), 0.04f);
                }
                return;
            }

            // --- Key points ---
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(_lastInfo.hitPoint, 0.04f);   // where the wall/front face was hit
            Gizmos.color = ColorForType(_lastInfo.type);
            Gizmos.DrawSphere(_lastInfo.topPoint, 0.05f);   // resolved top surface point

            // Vertical line showing measured height, from feet level straight up to the top point.
            Vector3 feetPoint = new Vector3(_lastInfo.topPoint.x, feetRayOrigin.position.y, _lastInfo.topPoint.z);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(feetPoint, _lastInfo.topPoint);

#if UNITY_EDITOR
            // Text readout right above the obstacle - type/height at a glance while tuning.
            GUIStyle style = new GUIStyle
            {
                normal = { textColor = ColorForType(_lastInfo.type) },
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
            string label = $"{_lastInfo.type}\nH: {_lastInfo.height:F2}m";
            Handles.Label(_lastInfo.topPoint + Vector3.up * 0.15f, label, style);
#endif
        }
    }
}
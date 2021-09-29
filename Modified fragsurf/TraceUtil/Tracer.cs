using UdonFragsurf;
using UdonSharp;
using UnityEngine;

namespace Fragsurf.TraceUtil {
    public class Tracer : UdonSharpBehaviour {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="collider"></param>
        /// <param name="origin"></param>
        /// <param name="end"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        public static Trace TraceCollider (Trace trace, Collider collider, Vector3 origin, Vector3 end, int layerMask, float colliderScale = 1f) {
            var colliderType = collider.GetType();
            if (colliderType == typeof(BoxCollider)) {

                // Box collider trace
                return TraceBox (trace, origin, end, collider.bounds.extents, collider.contactOffset, layerMask, colliderScale);

            } else if (colliderType == typeof(CapsuleCollider)) {

                // Capsule collider trace
                var capc = (CapsuleCollider)collider;

                Vector3 point1, point2;
                Movement.SurfPhysics.GetCapsulePoints (capc, origin, out point1, out point2);

                return TraceCapsule (trace, point1, point2, capc.radius, origin, end, capc.contactOffset, layerMask, colliderScale);

            }

            UdonFragsurfShims.Shim_NotImplementedException("Trace missing for collider: " + colliderType);
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Trace TraceCapsule (Trace result, Vector3 point1, Vector3 point2, float radius, Vector3 start, Vector3 destination, float contactOffset, int layerMask, float colliderScale = 1f) {

            result._Pristine();
            result.startPos = start;
            result.endPos = destination;

            var longSide = Mathf.Sqrt (contactOffset * contactOffset + contactOffset * contactOffset);
            radius *= (1f - contactOffset);
            var direction = (destination - start).normalized;
            var maxDistance = Vector3.Distance (start, destination) + longSide;

            RaycastHit hit;
            if (Physics.CapsuleCast (
                point1: point1 - Vector3.up * colliderScale * 0.5f,
                point2: point2 + Vector3.up * colliderScale * 0.5f,
                radius: radius * colliderScale,
                direction: direction,
                hitInfo: out hit,
                maxDistance: maxDistance,
                layerMask: layerMask,
                queryTriggerInteraction: QueryTriggerInteraction.Ignore)) {

                result.fraction = hit.distance / maxDistance;
                result.hitCollider = hit.collider;
                result.hitPoint = hit.point;
                result.planeNormal = hit.normal;
                result.distance = hit.distance;
                
                RaycastHit normalHit;
                Ray normalRay = new Ray (hit.point - direction * 0.001f, direction);
                if (hit.collider.Raycast (normalRay, out normalHit, 0.002f)) {
                    
                    result.planeNormal = normalHit.normal;

                }
                
            } else
                result.fraction = 1;

            return result;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Trace TraceBox (Trace result, Vector3 start, Vector3 destination, Vector3 extents, float contactOffset, int layerMask, float colliderScale = 1f)
        {
            result._Pristine();
            result.startPos = start;
            result.endPos = destination;

            var longSide = Mathf.Sqrt (contactOffset * contactOffset + contactOffset * contactOffset);
            var direction = (destination - start).normalized;
            var maxDistance = Vector3.Distance (start, destination) + longSide;
            extents *= (1f - contactOffset);

            RaycastHit hit;
            if (Physics.BoxCast (center: start,
                halfExtents: extents * colliderScale,
                direction: direction,
                orientation: Quaternion.identity,
                maxDistance: maxDistance,
                hitInfo: out hit,
                layerMask: layerMask,
                queryTriggerInteraction: QueryTriggerInteraction.Ignore)) {

                result.fraction = hit.distance / maxDistance;
                result.hitCollider = hit.collider;
                result.hitPoint = hit.point;
                result.planeNormal = hit.normal;
                result.distance = hit.distance;
                
                RaycastHit normalHit;
                Ray normalRay = new Ray (hit.point - direction * 0.001f, direction);
                if (hit.collider.Raycast (normalRay, out normalHit, 0.002f)) {
                    
                    result.planeNormal = normalHit.normal;

                }
                
            } else
                result.fraction = 1;

            return result;

        }

    }
}

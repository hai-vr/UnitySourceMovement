using UdonSharp;
using UnityEngine;

namespace Fragsurf.Movement {

    public class ISurfControllable : UdonSharpBehaviour {

        public virtual MoveType XGet_moveType() { return MoveType.None; }
        public virtual MoveData XGet_moveData() { return null; }
        public virtual Collider XGet_collider() { return null; }
        public virtual GameObject XGet_groundObject() { return null; }
        public virtual void XSet_groundObject(GameObject value) { }
        public virtual Vector3 XGet_forward() { return Vector3.zero; }
        public virtual Vector3 XGet_right() { return Vector3.zero; }
        public virtual Vector3 XGet_up() { return Vector3.zero; }
        public virtual Vector3 XGet_baseVelocity() { return Vector3.zero; }

    }
}

using UdonSharp;
using UnityEngine;

namespace Fragsurf.Movement {

    public class ISurfControllable : UdonSharpBehaviour {

        public MoveType moveType { get; }
        public MoveData moveData { get; }
        public Collider collider { get; }
        public GameObject groundObject { get; set; }
        public Vector3 forward { get; }
        public Vector3 right { get; }
        public Vector3 up { get; }
        public Vector3 baseVelocity { get; }

    }
}

using UdonSharp;
using UnityEngine;

public class VectorExtensions : UdonSharpBehaviour {

    public static Vector3 VectorMa (Vector3 start, float scale, Vector3 direction) {

        var dest = new Vector3 (
            start.x + direction.x * scale,
            start.y + direction.y * scale,
            start.z + direction.z * scale
        );

        return dest;

    }

}

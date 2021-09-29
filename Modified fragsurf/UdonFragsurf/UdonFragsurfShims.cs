using UdonSharp;
using UnityEngine;

namespace UdonFragsurf
{
    public class UdonFragsurfShims : UdonSharpBehaviour
    {
        public static int Shim_NotImplementedException(string reason)
        {
            Debug.LogError(reason);
            return (int)(1 / Mathf.Sqrt(0));
        }
    }
}

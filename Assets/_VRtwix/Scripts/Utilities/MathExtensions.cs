using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public static class MathExtensions
{
    [PublicAPI]
    public static float Distance(this MonoBehaviour from, in Vector3 to)
        => Vector3.Distance(a: from.transform.position, b: to);
    
    [PublicAPI]
    public static float Distance(this Transform from, in Vector3 to)
        => Vector3.Distance(a: from.position, b: to);
    
    [PublicAPI]
    public static float Distance(this Vector3 from, in Vector3 to)
        => Vector3.Distance(a: from, b: to);
}

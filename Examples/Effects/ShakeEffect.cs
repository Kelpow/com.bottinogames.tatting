using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShakeEffect : Tatting.MeshTextEffect
{
    public Vector3 scale;
    protected override Tatting.TRS TextEffect(Vector2 head, int index)
    {
        return new Tatting.TRS(Vector3.Scale(scale, Random.insideUnitSphere), Quaternion.identity, Vector3.one);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshTextWaveEffect : Tatting.MeshTextEffect
{
    public Vector3 scale;

    protected override Tatting.TRS TextEffect(Vector2 head, int index)
    {
        return new Tatting.TRS(scale * Mathf.Sin(Time.time + head.x), Quaternion.identity, Vector3.one);
    }
}

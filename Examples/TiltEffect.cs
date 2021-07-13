using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TiltEffect : Tatting.MeshTextEffect
{
    public Vector3 axisRotation;
    public float frequency = .5f;
    public float speed = 1;

    protected override void TextEffect(int i, ref Vector3 offset, ref Vector3 rotationaloffset)
    {
        rotationaloffset += axisRotation * Mathf.Sin((Time.time + (frequency * i)) * speed);
    }
}

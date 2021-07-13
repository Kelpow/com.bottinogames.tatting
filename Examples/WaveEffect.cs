using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveEffect : Tatting.MeshTextEffect
{

    private void Start()
    {
        Debug.Log("Wave");
    }

    public float intensity = 0.25f;
    public float frequency = 0.3f;
    public float speed = 2f;

    protected override void TextEffect(int i, ref Vector3 offset, ref Vector3 rotationaloffset)
    {
        float t = frequency * i;
        offset += Vector3.up * Mathf.Sin(t - (Time.time * speed)) * intensity;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlipTheDEffect : Tatting.MeshTextEffect
{
    public float characterHeight;
    public float characterWidth;

    Tatting.MeshText text;

    private void Start()
    {
        text = GetComponent<Tatting.MeshText>();
    }

    protected override Tatting.TRS TextEffect(Vector2 head, int index)
    {
        Vector3 center = new Vector3(characterWidth / 2, characterHeight / 2);
        int d = text.Text.IndexOf('d');
        if (index == d)
        {
            Quaternion rot = Quaternion.identity;
            rot = Quaternion.Euler(Time.time * 360f, Time.time * 90f, 0f);

            Vector3 diff = center - (rot * center);

            Vector3 scale = Vector3.LerpUnclamped(Vector3.one, Vector3.one * 1.3f, Mathf.Sin(Time.time * 0.7f));

            return new Tatting.TRS(diff,rot,scale);
        }
        else
            return Tatting.TRS.identity;
        
    }
}

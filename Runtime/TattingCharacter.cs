using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TattingCharacter : MonoBehaviour
{
    private MeshRenderer _renderer;
    public new MeshRenderer renderer { get { if (!_renderer) { _renderer = GetComponent<MeshRenderer>(); } return _renderer; } }




    private MeshFilter _filter;
    public MeshFilter filter { get { if (!_filter) { _filter = GetComponent<MeshFilter>(); } return _filter; } }




    private Vector3 _position;
    public Vector3 position { get { return _position; } set { _position = value; UpdatePosition(); } }

    private Vector3 _offset;
    public Vector3 offset { get { return _offset; } set { _offset = value; UpdatePosition(); } }
    
    
    
    
    private Vector3 _rotation;
    public Vector3 rotation { get { return _rotation; } set { _rotation = value; UpdatePosition(); } }

    private Vector3 _rotationaloffset;
    public Vector3 rotationaloffset { get { return _rotationaloffset; } set { _rotationaloffset = value; UpdatePosition(); } }



    private void UpdatePosition()
    {
        transform.localPosition = position + offset;
        transform.localRotation = Quaternion.Euler(rotation);
        transform.Rotate(rotationaloffset, Space.Self);
    }
}

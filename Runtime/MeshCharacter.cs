using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tatting
{
    public class MeshCharacter : MonoBehaviour
    {
        private MeshRenderer _renderer;
        public new MeshRenderer renderer { get { if (!_renderer) { _renderer = GetComponent<MeshRenderer>(); } return _renderer; } }




        private MeshFilter _filter;
        public MeshFilter filter { get { if (!_filter) { _filter = GetComponent<MeshFilter>(); } return _filter; } }




        public Transform holder;


        private Vector3 _position;
        public Vector3 position { get { return _position; } set { _position = value; UpdatePosition(); } }

        private Vector3 _offset;
        public Vector3 offset { get { return _offset; } set { _offset = value; UpdatePosition(); } }




        private Vector3 _rotation;
        public Vector3 rotation { get { return _rotation; } set { _rotation = value; UpdatePosition(); } }

        private Vector3 _rotationaloffset;
        public Vector3 rotationaloffset { get { return _rotationaloffset; } set { _rotationaloffset = value; UpdatePosition(); } }


        public Vector3 pivot { get { return -transform.localPosition; } set { transform.localPosition = -value; } }

        public void UpdatePosition(Vector3 position, Vector3 rotation)
        {
            _position = position;
            _rotation = rotation;
            UpdatePosition();
        }
        public void UpdatePosition(Vector3 position, Vector3 offset, Vector3 rotation, Vector3 rotationaloffset)
        {
            _position = position;
            _offset = offset;
            _rotation = rotation;
            _rotationaloffset = rotationaloffset;
            UpdatePosition();
        }
        public void UpdateOffset(Vector3 offset, Vector3 rotationaloffset)
        {
            _offset = offset;
            _rotationaloffset = rotationaloffset;
            UpdatePosition();
        }
        private void UpdatePosition()
        {
            holder.localPosition = position + offset;
            holder.localRotation = Quaternion.Euler(rotation);

            holder.Rotate(rotationaloffset, Space.Self);
        }
    }
}
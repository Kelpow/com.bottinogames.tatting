using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tatting
{
    /// <summary> Handles data and positioning of individual characters in a MeshTextRenderer. Handled internally. </summary>
    public class MeshCharacter : MonoBehaviour
    {

        private MeshRenderer _renderer;
        private MeshFilter _filter;
        public Transform holder;
        private Vector3 _position;
        private Vector3 _positionlOffset;
        private Vector3 _rotation;
        private Vector3 _rotationaloffset;



        public new MeshRenderer renderer { get { if (!_renderer) { _renderer = GetComponent<MeshRenderer>(); } return _renderer; } }

        public MeshFilter filter { get { if (!_filter) { _filter = GetComponent<MeshFilter>(); } return _filter; } }
   
        public Vector3 position { get { return _position; } set { _position = value; UpdatePosition(); } }

        public Vector3 positionalOffset { get { return _positionlOffset; } set { _positionlOffset = value; UpdatePosition(); } }

        public Vector3 rotation { get { return _rotation; } set { _rotation = value; UpdatePosition(); } }

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
            _positionlOffset = offset;
            _rotation = rotation;
            _rotationaloffset = rotationaloffset;
            UpdatePosition();
        }
        public void UpdateOffset(Vector3 offset, Vector3 rotationaloffset)
        {
            _positionlOffset = offset;
            _rotationaloffset = rotationaloffset;
            UpdatePosition();
        }
        private void UpdatePosition()
        {
            holder.localPosition = position + positionalOffset;
            holder.localRotation = Quaternion.Euler(rotation);

            holder.Rotate(rotationaloffset, Space.Self);
        }
    }
}
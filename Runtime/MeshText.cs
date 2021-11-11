using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tatting
{
    /// <summary>
    /// A renderer for displaying Tatting 3D mesh text's.
    /// </summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent (typeof(MeshRenderer), typeof(MeshFilter) )]
    public class MeshText : MonoBehaviour
    {
        const int TOP = 1;
        const int MID = 2;
        const int BOT = 4;
        const int LFT = 8;
        const int CEN = 16;
        const int RIT = 32;

        public enum Alignment : int
        {
            TopLeft         =   TOP+LFT,
            TopCenter       =   TOP+CEN,
            TopRight        =   TOP+RIT,
            MiddleLeft      =   MID+LFT,
            MiddleCenter    =   MID+CEN,
            MiddleRight     =   MID+RIT,
            BottomLeft      =   BOT+LFT,
            BottomCenter    =   BOT+CEN,
            BottomRight     =   BOT+RIT
        }


        //public
        public MeshFont font;
        
        [SerializeField] [TextArea] private string text;
        public string Text
        {
            get { return text; }
            set
            {
                if (text != value)
                {
                    text = value;
                    UpdateMesh();
                }
            }
        }

        public float lineSpacing = 1f;

        public Alignment alignment = Alignment.TopLeft;


        //internal
        CombineInstance[] combineArray = new CombineInstance[16];
        
        
        //References
        MeshFilter filter;

        private void Awake()
        {
            filter = GetComponent<MeshFilter>();
        }

        private void UpdateMesh()
        {
            if (text.Length > combineArray.Length)
            {
                int l = combineArray.Length;
                while (l < text.Length)
                    l *= 2;
                CombineInstance[] newArray = new CombineInstance[l];
                combineArray.CopyTo(newArray, 0);
                combineArray = newArray;
            }

            Vector2 head = Vector2.zero;
            Vector2 extents = Vector2.zero;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    combineArray[i].mesh = MeshFont.CharacterInfo.emptyMesh;
                    head.y -= lineSpacing;
                    extents.x = Mathf.Max(head.x, extents.x);

                    head.x = 0f;
                    extents.y = head.y;
                    continue;
                }

                MeshFont.CharacterInfo info = font.GetCharacterInfo(text[i]);
                combineArray[i].mesh = info.mesh;
                combineArray[i].transform = Matrix4x4.TRS(new Vector3(head.x, head.y, 0f), Quaternion.identity, Vector3.one);

                head.x += info.width;
            }
            extents.x = Mathf.Max(head.x, extents.x);
            extents.y += lineSpacing;

            if (alignment != Alignment.TopLeft)
            {
                float x = 0f;
                if (((int)alignment & CEN) != 0)
                    x = -extents.x / 2;
                if (((int)alignment & RIT) != 0)
                    x = -extents.x;

                float y = 0f;
                if (((int)alignment & MID) != 0)
                    y = -extents.y / 2;
                if (((int)alignment & BOT) != 0)
                    y = -extents.y;

                Matrix4x4 shift = Matrix4x4.Translate(new Vector3(x, y, 0f));

                for (int i = 0; i < text.Length; i++)
                {
                    combineArray[i].transform = combineArray[i].transform * shift;
                }
            }

            for (int i = text.Length; i < combineArray.Length; i++)
            {
                combineArray[i].mesh = MeshFont.CharacterInfo.emptyMesh;
            }

            filter.mesh.CombineMeshes(combineArray);
        }

        private void OnValidate()
        {
            UpdateMesh();
        }


        //internal
        private void FontHasChanged()
        {
            UpdateMesh();
        }

        [ContextMenu("Go")]
        void Test()
        {
            UpdateMesh();
        }

    }





    // ||||||||||||||||||||| E D I T O R ||||||||||||||||||||| 
    

#if UNITY_EDITOR
    [CustomEditor(typeof(MeshText))]
    public class MeshTextInspector : Editor
    {

    }

#endif
}
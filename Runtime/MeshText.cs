using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Tatting
{
    /// <summary>
    /// A renderer for displaying Tatting 3D mesh text's.
    /// </summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MeshText : MonoBehaviour
    {
        const int TOP = 1;
        const int MID = 2;
        const int BOT = 4;
        const int LFT = 8;
        const int CEN = 16;
        const int RIT = 32;

        public enum TextAlignment : int
        {
            TopLeft = TOP + LFT,
            TopCenter = TOP + CEN,
            TopRight = TOP + RIT,
            MiddleLeft = MID + LFT,
            MiddleCenter = MID + CEN,
            MiddleRight = MID + RIT,
            BottomLeft = BOT + LFT,
            BottomCenter = BOT + CEN,
            BottomRight = BOT + RIT
        }


        //public
        [SerializeField] private MeshFont font;
        public MeshFont Font
        {
            get { return font; }
            set
            {
                if(font != value)
                {
                    font = value;
                    UpdateMesh();
                }
            }
        }

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
        
        [Header("Formating")]
        
        [SerializeField] private float lineSpacing = 1f;
        public float LineSpacing
        {
            get { return lineSpacing; }
            set
            {
                if(lineSpacing != value)
                {
                    lineSpacing = value;
                    UpdateMesh();
                }
            }
        }
        
        [SerializeField] private TextAlignment alignment = TextAlignment.TopLeft;
        public TextAlignment Alignment
        {
            get { return alignment; }
            set
            {
                if(alignment != value)
                {
                    alignment = value;
                    UpdateMesh();
                }
            }
        }


        [Tatting.Foldout("Advanced", true, true)]

        [SerializeField] [Min(0)] private float maxWidth = 0f;
        public float MaxWidth
        {
            get { return maxWidth; }
            set
            {
                if(maxWidth != value)
                {
                    maxWidth = value;
                    UpdateMesh();
                }
            }
        }



        [System.NonSerialized] public List<MeshTextEffectDelegate> effects = new List<MeshTextEffectDelegate>();

        //internal
        [HideInInspector] Mesh _mesh;
        Mesh mesh
        {
            get
            {
                if (_mesh == null)
                    _mesh = new Mesh();
                return _mesh;
            }
        }

        CombineInstance[] combineArray = new CombineInstance[16];
        
        
        //References
        MeshFilter filter;

        private void Awake()
        {
            filter = GetComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            if(combineArray == null)
                combineArray = new CombineInstance[16];
            if (text == null)
                text = "";

            UpdateMesh();
        }

        private void UpdateMesh()
        {
            if (!font)
                return;


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


                Vector3 translation = new Vector3(head.x, head.y, 0f);
                Quaternion rotation = Quaternion.identity;
                Vector3 scale = Vector3.one;

                if(effects != null)
                {
                    foreach (MeshTextEffectDelegate del in effects)
                    {
                        TRS trs = del.Invoke(head, i);

                        translation += trs.translation;
                        rotation *= trs.rotation;
                        scale = Vector3.Scale(scale, trs.scale);
                    }
                }

                combineArray[i].transform = Matrix4x4.TRS(translation,rotation,scale);

                head.x += info.width;
            }

            extents.x = Mathf.Max(head.x, extents.x);
            extents.y += lineSpacing;

            float x = 0f;
            if (((int)alignment & CEN) != 0)
                x = -extents.x / 2;
            if (((int)alignment & RIT) != 0)
                x = -extents.x;

            float y = 0f;
            if (((int)alignment & TOP) != 0)
                y = -lineSpacing;
            if (((int)alignment & MID) != 0)
                y = -extents.y / 2;
            if (((int)alignment & BOT) != 0)
                y = -extents.y;

            Matrix4x4 shift = Matrix4x4.Translate(new Vector3(x, y, 0f));

            for (int i = 0; i < text.Length; i++)
            {
                combineArray[i].transform = shift * combineArray[i].transform;
            }

            for (int i = text.Length; i < combineArray.Length; i++)
            {
                combineArray[i].mesh = MeshFont.CharacterInfo.emptyMesh;
            }

            mesh.CombineMeshes(combineArray);
        }

        private void OnValidate()
        {
            UpdateMesh();
        }

        private void OnDestroy()
        {
            TryDestroyInOnDestroy(GetComponent<MeshFilter>());
            TryDestroyInOnDestroy(GetComponent<MeshRenderer>());
        }


        bool active;
        private void Update()
        {
            //This updatemesh was here and I'm really hoping it didn't /need/ to be there. Commenting just in case.
            //UpdateMesh();
            if (effects != null)
            {
                active = true;
            } else if (active)
            {
                UpdateMesh();
                active = false;
            }
        }

        //Stolen wholesale from Freya Holmer, hopes she doesn't mind ♥ https://acegikmo.medium.com/the-cascading-workarounds-of-feature-gaps-b5ff1cc65ca2
        static void TryDestroyInOnDestroy(Object obj)
        {
            if (obj == null) return;
#if UNITY_EDITOR
            if(Application.isEditor && Application.isPlaying == false)
            {
                EditorApplication.delayCall += () =>
                {
                    if (Application.isPlaying == false && obj != null)
                        Object.DestroyImmediate(obj);
                };
            }
            else
            {
                Object.Destroy(obj);
            }
#else
            Object.Destroy(obj);
#endif
        }



#if UNITY_EDITOR

        //===== Editor Functionality =====


        [ContextMenu("Set name from text")]
        void SetNameFromText()
        {
            gameObject.name = text ;
        }


        //Adds "create new gameobject" functionality for 3D Mesh Text Renderers
        //TODO: Have object be created similarly to other assets, follow hiearchy selection and all that
        [MenuItem("GameObject/3D Object/3D Text - Tatting", priority = 29)] 
        static void ObjectCreationMenuItem(MenuCommand command)
        {
            GameObject newGameObject = ObjectFactory.CreateGameObject("Text (Tatting)", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshText));

            StageUtility.PlaceGameObjectInCurrentStage(newGameObject);

            GameObject context = command.context as GameObject;
            if (context != null)
            {
                GameObjectUtility.SetParentAndAlign(newGameObject, context);
            }
            else
            {
                newGameObject.transform.position = SceneView.lastActiveSceneView.pivot;
            }

            //Set your default data and whatnot here
            MeshFilter filter = newGameObject.GetComponent<MeshFilter>();
            filter.hideFlags = HideFlags.None;

            Undo.RegisterCreatedObjectUndo(newGameObject, $"Create {newGameObject.name}");

            Selection.activeObject = newGameObject;
        }



        //internal debug
        [ContextMenu("Force Mesh Update")]
        public void ForceMeshUpdate()
        {
            UpdateMesh();
        }


        //===== Inspector ====
        [CustomEditor(typeof(MeshText))]
        public class Inspector : Editor
        {
            private void OnEnable()
            {
                MeshText target = (MeshText)base.target;
                MeshFilter filter = target.GetComponent<MeshFilter>();
                //MeshRenderer renderer = target.GetComponent<MeshRenderer>();

                filter.hideFlags = HideFlags.HideInInspector;
                //renderer.hideFlags = HideFlags.None;
            }
        }

#endif
    }
     
}
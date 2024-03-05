using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Tatting
{
    /// <summary>
    /// A renderer for displaying Tatting 3D mesh text's.
    /// </summary>
    [ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(-100)]
    public class MeshText : MonoBehaviour
    {
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
                    UpdateText();
                    UpdateMesh();
                }
            }
        }
        
        

        [Header("Formating")]
        
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

        [SerializeField] WidthLimiting widthLimitMode = WidthLimiting.None;
        public WidthLimiting WidthLimitMode
        {
            get { return widthLimitMode; }
            set
            {
                if(widthLimitMode != value)
                {
                    widthLimitMode = value;
                    UpdateText();
                    UpdateMesh();
                }
            }
        }

        [SerializeField] [Min(0)] private float maxWidth = 0f;
        public float MaxWidth
        {
            get { return maxWidth; }
            set
            {
                value = Mathf.Min(value, 0f);
                if(maxWidth != value)
                {
                    maxWidth = value;
                    UpdateText();
                    UpdateMesh();
                }
            }
        }

        //private aka inspector-only
        [Header("Rendering")]
        [SerializeField]
        private RenderType renderType;

        [SerializeField, HideInInspector, UnityEngine.Serialization.FormerlySerializedAs("material")]
        public Material directDrawMaterial;

        private List<Line> lines;
        private int countToDraw = 0;
        private CombineInstance[] combineArray = new CombineInstance[16];
        private TRS[] trsArray = new TRS[16];
        private char[] drawnChar = new char[16];


        // direct draw stuff
        private Bounds directDrawLocalBounds = new Bounds();
        private MaterialPropertyBlock directDrawPropertyBlock;

        [System.NonSerialized] public List<DirectDrawEffectDelegate> directDrawEffects = new List<DirectDrawEffectDelegate>();

        private Mesh _mesh;
        Mesh mesh
        {
            get
            {
                if (_mesh == null)
                    _mesh = new Mesh();
                return _mesh;
            }
        }
        
        //References
        MeshFilter filter;

        private void Awake()
        {
            if (renderType == RenderType.MeshRenderer)
            {
                filter = GetComponent<MeshFilter>();
                filter.sharedMesh = mesh;
            }

            if(combineArray == null)
                combineArray = new CombineInstance[16];
            if (trsArray == null)
                trsArray = new TRS[16];
            if (drawnChar == null)
                drawnChar = new char[16];
            if (text == null)
                text = "";

            directDrawPropertyBlock = new MaterialPropertyBlock();

            UpdateText();
            UpdateMesh();
        }

        public void UpdateText()
        {
            if (lines == null)
                lines = new List<Line>();
            else
                lines.Clear();
            if (text == null)
                text = "";

            string[] baselines = text.Split('\n');

            WidthLimiting mode = maxWidth > 0 ? widthLimitMode : WidthLimiting.None;

            foreach (string baseline in baselines)
            {
                Line newline;
                switch (mode)
                {
                    case WidthLimiting.None:
                        newline =  new Line(baseline, 0f, 1f);
                        foreach (char c in baseline)
                            newline.width += font.GetCharacterInfo(c).width;

                        lines.Add(newline);
                        break;
                    case WidthLimiting.Scaling:
                        newline = new Line(baseline, 0f, 1f);
                        foreach (char c in baseline)
                            newline.width += font.GetCharacterInfo(c).width;

                        if (newline.width > maxWidth)
                        {
                            newline.scale = maxWidth / newline.width;
                            newline.width = maxWidth;
                        } 
                            
                        lines.Add(newline);
                        break;

                    case WidthLimiting.WordWrap:
                        newline = new Line("", 0f, 1f);
                        int lineStart = 0;
                        float spacewidth = 0f;
                        for (int i = 0; i < baseline.Length; i++)
                        {
                            char c = baseline[i];
                            if (c == ' ') //someday should be converted to char.IsWhitespace(c)   ---   honestly don't know why I shouldn't now, but don't wanna fuck it up lmao
                            {
                                var info = font.GetCharacterInfo(c);
                                spacewidth += info.width;
                            } 
                            else
                            {
                                var word = GetNextWord(baseline, i);
                                if(newline.width + spacewidth + word.width > maxWidth)
                                {
                                    newline.content = baseline.Substring(lineStart, i - lineStart);
                                    lines.Add(newline);
                                    
                                    newline.width = word.width;
                                    lineStart = i;
                                    spacewidth = 0f;
                                    i += word.length - 1;
                                }
                                else
                                {
                                    newline.width += spacewidth;
                                    spacewidth = 0f;
                                    newline.width += word.width;
                                    i += word.length - 1;
                                }
                            }
                        }
                        newline.content = baseline.Substring(lineStart);
                        lines.Add(newline);
                        break;

                    case WidthLimiting.CharacterWrap:
                        newline = new Line("", 0f, 1f);
                        var sbcw = new System.Text.StringBuilder(baseline.Length);
                        foreach (char c in baseline)
                        {
                            var info = font.GetCharacterInfo(c);
                            if (newline.width + info.width > maxWidth)
                            {
                                newline.content = sbcw.ToString();
                                lines.Add(newline);

                                sbcw.Clear();
                                newline.width = 0f;
                            }
                            sbcw.Append(c);
                            newline.width += info.width;
                        }
                        newline.content = sbcw.ToString();
                        lines.Add(newline);
                        break;

                    default:
                        break;
                }
            }
        }

        (int length, float width) GetNextWord(string baseline, int startIndex)
        {
            float width = 0;
            int length = 0;
            for (int i = startIndex; i < baseline.Length; i++)
            {
                char c = baseline[i];
                if (c == ' ') //someday should be converted to char.IsWhitespace(c)
                    break;
                length++;
                var info = font.GetCharacterInfo(c);
                width += info.width;
            }
            return (length, width);
        }



        private void UpdateMesh()
        {
            if (!font)
                return;


            if (text.Length > combineArray.Length)
            {
                int length = combineArray.Length;
                while (length < text.Length)
                    length *= 2;
                combineArray = new CombineInstance[length];
                trsArray = new TRS[length];
                drawnChar = new char[length];
            }

            bool centerAligned = ((int)alignment & CEN) != 0;
            bool rightAligned = ((int)alignment & RIT) != 0;

            bool middleAligned = ((int)alignment & MID) != 0;
            bool bottomAligned = ((int)alignment & BOT) != 0;

            int cai = 0; //Combine Array Index
            if (lines.Count > 0)
            {
                float xMin = 0f, xMax = 0f, yMin = 0f, yMax = 0f;

                float lineheight = 0f;
                foreach (Line line in lines)
                {
                    lineheight -= font.lineSpacing * line.scale;
                    float lineStart;
                    if (centerAligned)
                        lineStart = -line.width / 2f;
                    else if (rightAligned)
                        lineStart = -line.width;
                    else
                        lineStart = 0f;

                    TRS characterTRS = new TRS(new Vector3(lineStart, lineheight), Quaternion.identity, Vector3.one * line.scale);
                    foreach (char c in line.content)
                    {
                        var info = font.GetCharacterInfo(c);
                        combineArray[cai].mesh = info.mesh;
                        trsArray[cai] = characterTRS;
                        drawnChar[cai] = c;
                        cai++;

                        characterTRS.translation += new Vector3(info.width * line.scale, 0f);
                    }
                }

                Vector3 verticalAlignmentShift = Vector3.zero;
                if (middleAligned)
                    verticalAlignmentShift = new Vector3(0f, -lineheight / 2f);
                else if (bottomAligned)
                    verticalAlignmentShift = new Vector3(0f, -lineheight);

                for (int i = 0; i < cai; i++)
                {
                    trsArray[i].translation += verticalAlignmentShift;

                    if (renderType == RenderType.DirectDraw)
                    {
                        xMin = Mathf.Min(trsArray[i].translation.x, xMin);
                        xMax = Mathf.Max(trsArray[i].translation.x, xMax);
                        yMin = Mathf.Min(trsArray[i].translation.y, yMin);
                        yMax = Mathf.Max(trsArray[i].translation.y, yMax);
                    }
                }


                // TODO: make this shit not a straight up guess lol, these additions should be 
                if (renderType == RenderType.DirectDraw)
                {
                    const float CHAR_HALF_DEPTH = 0.05f;
                    const float CHAR_WIDTH = .6f;
                    const float CHAR_HEIGHT = 1f;
                    const float MARGIN = 0.2f;
                    directDrawLocalBounds.SetMinMax(
                        new Vector3(xMin - MARGIN, yMin - MARGIN, -CHAR_HALF_DEPTH - MARGIN),
                        new Vector3(xMax + CHAR_WIDTH + MARGIN, yMax + CHAR_HEIGHT + MARGIN, CHAR_HALF_DEPTH + MARGIN));
                }
            }

            for (int i = 0; i < cai; i++)
            {
                combineArray[i].transform = trsArray[i].ToMatrix4x4();
            }

            countToDraw = cai;

            if (renderType == RenderType.MeshRenderer)
            {
                for (int i = cai; i < combineArray.Length; i++)
                {
                    //CombineMesh doesn't like null values or empty matices, so we have to flush the end of the array with empty meshes and identity matices.
                    combineArray[i].mesh = MeshFont.CharacterInfo.emptyMesh;
                    combineArray[i].transform = Matrix4x4.identity;
                }

                try
                {
                    mesh.CombineMeshes(combineArray);
                }
                catch
                {
                    Debug.LogWarning("The number of vertices in the combined mesh exceded Unity's max vertext count. (65535 vertices) ((probably idk man this is a try catch))", this);
                }

                
            } 
            else if (renderType == RenderType.DirectDraw)
            {

            }
        }

        private void OnValidate()
        {
            UpdateText();
            UpdateMesh();
            if (renderType != RenderType.DirectDraw)
                directDrawMaterial = null;
        }

        private void OnDestroy()
        {
            TryDestroyInOnDestroy(GetComponent<MeshFilter>());
            TryDestroyInOnDestroy(GetComponent<MeshRenderer>());
        }


        private void LateUpdate()
        {
            if(renderType == RenderType.DirectDraw && directDrawMaterial != null)
            {
                Matrix4x4 ltw = transform.localToWorldMatrix;

                RenderParams renderParams = new RenderParams(directDrawMaterial);
                renderParams.matProps = directDrawPropertyBlock;

                renderParams.worldBounds = TransformBounds(in directDrawLocalBounds, in ltw);
                renderParams.layer = gameObject.layer;

                for (int i = 0; i < countToDraw; i++)
                {
                    if (directDrawEffects != null)
                    {
                        directDrawPropertyBlock.Clear();
                        for (int di = 0; di < directDrawEffects.Count; di++)
                        {
                            if (directDrawEffects[di] != null)
                                directDrawEffects[di].Invoke(ref directDrawPropertyBlock, i, drawnChar[i]);
                        }
                    }

                    Graphics.RenderMesh(in renderParams, combineArray[i].mesh, 0, ltw * combineArray[i].transform);
                }
            }
        }

        // Stolen from here: https://discussions.unity.com/t/can-39-t-convert-bounds-from-world-coordinates-to-local-coordinates/57667/7
        private Bounds TransformBounds(in Bounds bounds, in Matrix4x4 matrix)
        {
            Vector4 xa = matrix.GetColumn(0) * bounds.min.x;
            Vector4 xb = matrix.GetColumn(0) * bounds.max.x;
            
            Vector4 ya = matrix.GetColumn(1) * bounds.min.y;
            Vector4 yb = matrix.GetColumn(1) * bounds.max.y;
            
            Vector4 za = matrix.GetColumn(2) * bounds.min.z;
            Vector4 zb = matrix.GetColumn(2) * bounds.max.z;
            
            Vector4 col4Pos = matrix.GetColumn(3);

            Vector3 min = new Vector3();
            min.x = Mathf.Min(xa.x, xb.x) + Mathf.Min(ya.x, yb.x) + Mathf.Min(za.x, zb.x) + col4Pos.x;
            min.y = Mathf.Min(xa.y, xb.y) + Mathf.Min(ya.y, yb.y) + Mathf.Min(za.y, zb.y) + col4Pos.y;
            min.z = Mathf.Min(xa.z, xb.z) + Mathf.Min(ya.z, yb.z) + Mathf.Min(za.z, zb.z) + col4Pos.z;

            Vector3 max = new Vector3();
            max.x = Mathf.Max(xa.x, xb.x) + Mathf.Max(ya.x, yb.x) + Mathf.Max(za.x, zb.x) + col4Pos.x;
            max.y = Mathf.Max(xa.y, xb.y) + Mathf.Max(ya.y, yb.y) + Mathf.Max(za.y, zb.y) + col4Pos.y;
            max.z = Mathf.Max(xa.z, xb.z) + Mathf.Max(ya.z, yb.z) + Mathf.Max(za.z, zb.z) + col4Pos.z;

            Bounds outbounds = new Bounds();
            outbounds.SetMinMax(min, max);

            return outbounds;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Matrix4x4 ltw = transform.localToWorldMatrix;
            Bounds test = TransformBounds(in directDrawLocalBounds, in ltw);
            Gizmos.color = Color.grey;
            Gizmos.DrawWireCube(test.center, test.size);
        }
#endif

        // Stolen wholesale from Freya Holmer, hope she doesn't mind ♥ https://acegikmo.medium.com/the-cascading-workarounds-of-feature-gaps-b5ff1cc65ca2
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
            gameObject.name = Text ;
        }


        // Adds "create new gameobject" functionality for 3D Mesh Text Renderers
        // TODO: Have object be created similarly to other assets, follow hiearchy selection and all that
        [MenuItem("GameObject/3D Object/3D Text - Tatting", priority = 29)] 
        static void ObjectCreationMenuItem(MenuCommand command)
        {
            GameObject newGameObject = ObjectFactory.CreateGameObject("3D Mesh Text", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshText));

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

            Material defaultMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
            newGameObject.GetComponent<MeshRenderer>().sharedMaterial = defaultMat;

            MeshFont[] allfonts = Resources.FindObjectsOfTypeAll(typeof(MeshFont)) as MeshFont[];
            if (allfonts != null && allfonts.Length > 0) 
            {
                MeshText text = newGameObject.GetComponent<MeshText>();
                text.font = allfonts[0];
                text.Text = "Text";
            }

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
            new MeshText target;

            private void OnEnable()
            {
                target = (MeshText)base.target;
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                if(target.renderType == RenderType.MeshRenderer)
                {
                    if(target.GetComponent<MeshFilter>() == null)
                        EditorGUILayout.HelpBox("No MeshFilter on the object!", MessageType.Error);
                    if(target.GetComponent<MeshRenderer>() == null)
                        EditorGUILayout.HelpBox("No MeshRenderer on the object!", MessageType.Error);
                }

                if(target.renderType == RenderType.DirectDraw)
                {
                    EditorGUI.BeginChangeCheck();

                    Material mat = (Material)EditorGUILayout.ObjectField(target.directDrawMaterial, typeof(Material), false);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "MeshText Inspector");
                        target.directDrawMaterial = mat;
                    }
                }

            }
        }

#endif


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

        public enum WidthLimiting
        {
            None,
            Scaling,
            WordWrap,
            CharacterWrap
        }

        public enum RenderType
        {
            MeshRenderer,
            DirectDraw
        }
    }

    [System.Serializable]
    public struct Line
    {
        public string content;
        public float width;
        public float scale;

        public Line(string content, float width, float scale)
        {
            this.content = content;
            this.width = width;
            this.scale = scale;
        }
    }

    public struct TRS
    {
        public Vector3 translation;
        public Quaternion rotation;
        public Vector3 scale;

        public TRS(Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            this.translation = translation;
            this.rotation = rotation;
            this.scale = scale;
        }

        public static TRS identity { get { return new TRS(Vector3.zero, Quaternion.identity, Vector3.one); } }

        public Matrix4x4 ToMatrix4x4() { return Matrix4x4.TRS(this.translation, this.rotation, this.scale); }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tatting
{
    /// <summary>
    /// A renderer for displaying Tatting 3D mesh text's.
    /// </summary>
    [ExecuteAlways,DisallowMultipleComponent]
    public class MeshTextRenderer : MonoBehaviour
    {
        readonly Color GIZMOS_SELECTION_BOX_COLOR = new Color(0f, 1f, 0.5f, 0.25f);
        readonly Vector3 GIZMOS_SELECTION_BOX_SIZEOFFSET = Vector3.one * 0.01f;
        readonly Color GIZMOS_CHARACTER_BOX_COLOR = new Color(1f, 0.4f, 0.1f, 1f);
        readonly Vector3 GIZMOS_CHARACTER_BOX_SIZEOFFSET = Vector3.one * 0.005f;
        const float GIZMOS_CHARACTER_PIVOT_SIZE = 0.2f;

        /// <summary> Tatting font to use for display</summary>
        public MeshFont font;

        private List<MeshCharacter> _characterObjects;

        [SerializeField] private string _text = "";
        /// <summary> Display text </summary>
        public string Text
        {
            get { return _text; }
            set
            {
                if (value != _text)
                {
                    _text = value;
                    RefreshCharacters();
#if UNITY_EDITOR
                    _oldText = _text;
#endif
                }
            }
        }

#if UNITY_EDITOR
        //Exists purely to track when _text is changed by Undo-Redo
        private string _oldText = "";
#endif



        [SerializeField] private TextAnchor _anchor = TextAnchor.MiddleCenter;

        //Tatting places characters left to right, from the bottom left coner. This is used to offs
        private Vector3 basePosition;



        [SerializeField] private Material[] _materials = new Material[1];

        public Material[] materials { get { return _materials; } }
        

        [SerializeField] private UnityEngine.Rendering.ShadowCastingMode _shadowCastingMode;

        [SerializeField] private bool _receiveShadows;

        public TextAnchor Anchor
        {
            get { return _anchor; }
            set { if (value != _anchor) { _anchor = value; UpdateAllCharacterPositions(); } }
        }
        public UnityEngine.Rendering.ShadowCastingMode ShadowCastingMode
        {
            get { return _shadowCastingMode; }
            set { if (value != _shadowCastingMode) { _shadowCastingMode = value; UpdateAllCharacterRenderers(); } }
        }
        public bool ReceiveShadows
        {
            get { return _receiveShadows; }
            set { if (value != _receiveShadows) { _receiveShadows = value; UpdateAllCharacterRenderers(); } }
        }




        public void SetMaterials(Material[] materials) { _materials = materials; UpdateAllCharacterRenderers(); }

        //Keeps the Mesh Characters up to date with the display text
        private void RefreshCharacters()
        {
            if (!font)
                return;

            if (_characterObjects == null)
                _characterObjects = new List<MeshCharacter>();

            for (int i = 0; i < Text.Length; i++)
            {
                char character = Text[i];
                if (font.meshCharacters.ContainsKey(character) && font.meshCharacters[character] != null)
                {
                    GetCharacter(i).filter.mesh = font.meshCharacters[character];
                    GetCharacter(i).renderer.enabled = true;
                }
                else
                {
                    GetCharacter(i).renderer.enabled = false;
#if UNITY_EDITOR
                    GetCharacter(i).position = Vector3.zero;
#endif
                }
            }

            for (int i = Text.Length; i < _characterObjects.Count; i++)
            {
                _characterObjects[i].renderer.enabled = false;
#if UNITY_EDITOR
                _characterObjects[i].position = _characterObjects[0].position; //Position of disabled objects only matter when using the editors 'Frame Selected' function (f in scene view);
#endif
            }

            UpdateAllCharacterPositions();
        }


        //===== Unity Events =====

        private void OnAwake()
        {
            PurgeAllMeshCharacters();
        }

        private void OnEnable()
        {
            RefreshCharacters();
            UpdateAllCharacterRenderers();
        }

#if UNITY_EDITOR
        //This code exists purely to track any changes made by the Undo-Redo system, or any other editor system which may edit _text directly.
        //Uses isDirty as RefreshCharacters has operations which should not be used in OnValidate.
        private bool _isDirty;
        private void OnValidate()
        {
            if (_text != _oldText)
            {
                _isDirty = true;
                _oldText = _text;
            }
        }
#endif

        private void Update()
        {
#if UNITY_EDITOR
            if (_isDirty)
                RefreshCharacters();
#endif

            if (textEffects == null)
                return;

            for (int i = 0; i < Text.Length; i++)
            {
                Vector3 o = Vector3.zero;
                Vector3 ro = Vector3.zero;
                textEffects.Invoke(i, ref o, ref ro);
                GetCharacter(i).UpdateOffset(o, ro);
            }
        }




        //===== Character Objects =====

        private MeshCharacter GetCharacter(int i)
        {
            if (_characterObjects == null)
                _characterObjects = new List<MeshCharacter>();

            for (int j = 0; j < i; j++)
                CreateNewCharacterObject();

            return _characterObjects[i];
        }

        private void CreateNewCharacterObject()
        {
            MeshCharacter newCharacter = new GameObject("Tatting Character Object", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCharacter)).GetComponent<MeshCharacter>();
            Transform newHolder = new GameObject("Tatting Character Holder").transform;

            newCharacter.holder = newHolder;

            newHolder.parent = this.transform;
            newHolder.localScale = Vector3.one;
            newHolder.localRotation = Quaternion.identity;

            newCharacter.transform.parent = newHolder;
            newCharacter.transform.localScale = Vector3.one;
            newCharacter.transform.localRotation = Quaternion.identity;

            newCharacter.renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            newCharacter.renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            newHolder.gameObject.hideFlags = HideFlags.HideAndDontSave;
            newCharacter.gameObject.hideFlags = HideFlags.HideAndDontSave;

            _characterObjects.Add(newCharacter);
            UpdateCharacterPosition(_characterObjects.Count - 1);
            UpdateCharacterRenderer(_characterObjects.Count - 1);
        }

        /// <summary> Removes all Childed Mesh Characters. Used for clearing any straggler MeshCharacters if object is duplicated through editor or instantiation. Will be called automatically </summary>
        [ContextMenu("Purge")]
        public void PurgeAllMeshCharacters()
        {
            _characterObjects = null;
            MeshCharacter[] found = GetComponentsInChildren<MeshCharacter>();
            for (int i = 0; i < found.Length; i++)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(found[i].holder.gameObject);
                else
#endif
                    Destroy(found[i].holder.gameObject);
            }
        }

        //===== Modifying Character Objects =====

        private void UpdateBasePosition()
        {
            if (Text.Length == 0)
                return;

            basePosition.z = 0f;

            switch (Anchor)
            {
                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter:
                    basePosition.x = -font.distanceBetweenCharacters * (Text.Length - 1) * 0.5f;
                    break;
                case TextAnchor.UpperLeft:
                case TextAnchor.MiddleLeft:
                case TextAnchor.LowerLeft:
                    basePosition.x = font.characterBounds.extents.x;
                    break;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    basePosition.x = (-font.distanceBetweenCharacters * (Text.Length - 1)) - font.characterBounds.extents.x;
                    break;
            }

            switch (Anchor)
            {
                case TextAnchor.MiddleLeft:
                case TextAnchor.MiddleCenter:
                case TextAnchor.MiddleRight:
                    basePosition.y = 0f;
                    break;
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    basePosition.y = font.characterBounds.extents.y;
                    break;
                case TextAnchor.UpperLeft:
                case TextAnchor.UpperCenter:
                case TextAnchor.UpperRight:
                    basePosition.y = -font.characterBounds.extents.y;
                    break;
            }
        }

        private void UpdateCharacterPosition(int i)
        {
            _characterObjects[i].UpdatePosition(basePosition + new Vector3(font.distanceBetweenCharacters * i, 0f, 0f), font.characterRotation);
            _characterObjects[i].pivot = font.characterBounds.center;
        }

        private void UpdateCharacterRenderer(int i)
        {
            _characterObjects[i].renderer.sharedMaterials = _materials;
            _characterObjects[i].renderer.shadowCastingMode = ShadowCastingMode;
            _characterObjects[i].renderer.receiveShadows = ReceiveShadows;
        }

        /// <summary> Refreshes all character positions. </summary>
        public void UpdateAllCharacterPositions()
        {
            UpdateBasePosition();
            for (int i = 0; i < Text.Length; i++)
            {
                UpdateCharacterPosition(i);
            }
            if(textUpdated != null)
                textUpdated.Invoke();
        }
        
        //
        public void UpdateAllCharacterRenderers()
        {
            if (_characterObjects == null)
                return;
            for (int i = 0; i < _characterObjects.Count; i++)
            {
                UpdateCharacterRenderer(i);
            }
        }


        //===== Text Effects =====

        private MeshTextEffectDelegate _textEffects;
        public MeshTextEffectDelegate textEffects
        {
            get { return _textEffects; }
            set
            {
                _textEffects = value;

                if(_textEffects == null)
                {
                    for (int i = 0; i < Text.Length; i++)
                    {
                        GetCharacter(i).offset = Vector3.zero;
                        GetCharacter(i).rotationaloffset = Vector3.zero;
                    }
                }
            }
        }



        //===== External Events =====
        
        /// <summary>
        /// Called anytime the 'text' string is changed. Useful for updating visual elements that rely on text content or length.
        /// </summary>
        public System.Action textUpdated;




        //===== Data =====
        

        public Bounds LocalBounds
        {
            get 
            {
                Bounds bounds = new Bounds();
                if (Text.Length == 0)
                    return bounds;
                bounds.min = _characterObjects[0].position - font.characterBounds.extents;
                bounds.max = _characterObjects[Text.Length - 1].position + font.characterBounds.extents;
                return bounds;
            }
        }








        //===== Gizmos =====

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!font || Text.Length == 0)
            {
                return;
            }

            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;

            if (Selection.activeObject && Selection.activeObject == ((Object)font))
                Gizmos.color = GIZMOS_SELECTION_BOX_COLOR;
            else
                Gizmos.color = Color.clear;

            Vector3 textExtent = Vector3.right * font.distanceBetweenCharacters * (Text.Length - 1);


            Gizmos.DrawCube(basePosition + (textExtent / 2), font.characterBounds.size + textExtent + GIZMOS_SELECTION_BOX_SIZEOFFSET);


            if (Selection.activeObject && Selection.activeObject == ((Object)font))
            {
                Gizmos.color = GIZMOS_CHARACTER_BOX_COLOR;
                Gizmos.DrawWireCube(basePosition, font.characterBounds.size);

                Gizmos.color = new Color(0.1f, 0.4f, 1f, 1f);
                Gizmos.DrawLine(basePosition + new Vector3(-GIZMOS_CHARACTER_PIVOT_SIZE, -GIZMOS_CHARACTER_PIVOT_SIZE), basePosition + new Vector3(GIZMOS_CHARACTER_PIVOT_SIZE, GIZMOS_CHARACTER_PIVOT_SIZE));
                Gizmos.DrawLine(basePosition + new Vector3(GIZMOS_CHARACTER_PIVOT_SIZE, -GIZMOS_CHARACTER_PIVOT_SIZE), basePosition + new Vector3(-GIZMOS_CHARACTER_PIVOT_SIZE, GIZMOS_CHARACTER_PIVOT_SIZE));
            }

        }
#endif
    }





    // ||||||||||||||||||||| E D I T O R ||||||||||||||||||||| 


#if UNITY_EDITOR
    [CustomEditor(typeof(MeshTextRenderer))]
    public class TattingRendererInspector : Editor
    {

        bool materialsDropdown;
        public override void OnInspectorGUI()
        {
            MeshTextRenderer rend = (MeshTextRenderer)target;


            Undo.RecordObject(rend, "Mesh Text Renderer Inspector");


            rend.font = EditorGUILayout.ObjectField(rend.font, typeof(MeshFont), allowSceneObjects: false) as MeshFont;

            EditorGUI.BeginChangeCheck();
            rend.Text = EditorGUILayout.TextField("Text", rend.Text);
            if(EditorGUI.EndChangeCheck())
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews(); //Forces Unity to re-draw the game view when text is changed

            GUILayout.Space(6);
            
            EditorGUI.BeginChangeCheck();
            rend.Anchor = (TextAnchor)EditorGUILayout.EnumPopup("Text anchor", rend.Anchor);
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(rend); //Forces Unity to re-draw the objects when the anchor is changed.

            GUILayout.Space(6);


            EditorGUI.BeginChangeCheck();

            materialsDropdown = EditorGUILayout.BeginFoldoutHeaderGroup(materialsDropdown, "Materials");
            if(materialsDropdown)
            {
                EditorGUI.indentLevel++;

                int l = EditorGUILayout.DelayedIntField("Size",rend.materials.Length);
                if(l != rend.materials.Length)
                {
                    Material[] newArray = new Material[l];
                    for (int i = 0; i < l; i++)
                    {
                        if (i < rend.materials.Length)
                            newArray[i] = rend.materials[i];
                        else
                            newArray[i] = rend.materials[rend.materials.Length - 1];
                    }
                    rend.SetMaterials(newArray);
                }

                for (int i = 0; i < rend.materials.Length; i++)
                {
                    EditorGUILayout.ObjectField($"Element {i}", rend.materials[i], typeof(Material), allowSceneObjects: false);
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_shadowCastingMode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_receiveShadows"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                rend.UpdateAllCharacterRenderers();

                if (materialEditor != null)
                {
                    // Free the memory used by the previous MaterialEditor
                    DestroyImmediate(materialEditor);
                }

                if (rend.materials != null && rend.materials.Length > 0 && rend.materials[0] != null)
                {
                    // Create a new instance of the default MaterialEditor
                    materialEditor = (MaterialEditor)CreateEditor(rend.materials[0]);
                }
            }

            GUILayout.Space(10);

            if (materialEditor != null)
            {
                // Draw the material's foldout and the material shader field
                // Required to call _materialEditor.OnInspectorGUI ();
                materialEditor.DrawHeader();

                //  We need to prevent the user to edit Unity default materials
                bool isDefaultMaterial = !AssetDatabase.GetAssetPath(rend.materials[0]).StartsWith("Assets");

                using (new EditorGUI.DisabledGroupScope(isDefaultMaterial))
                {

                    // Draw the material properties
                    // Works only if the foldout of _materialEditor.DrawHeader () is open
                    materialEditor.OnInspectorGUI();
                }
            }

            GUILayout.Space(10);
        }


        MaterialEditor materialEditor;
        void OnEnable()
        {
            MeshTextRenderer rend = (MeshTextRenderer)target;

            if (rend.materials != null && rend.materials[0] != null)
            {
                // Create an instance of the default MaterialEditor
                materialEditor = (MaterialEditor)CreateEditor(rend.materials[0]);
            }
        }

        void OnDisable()
        {
            if (materialEditor != null)
            {
                // Free the memory used by default MaterialEditor
                DestroyImmediate(materialEditor);
            }
        }
    }

#endif
}
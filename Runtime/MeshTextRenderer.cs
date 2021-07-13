using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tatting
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class MeshTextRenderer : MonoBehaviour
    {

        [HideInInspector] public MeshFont font;

        [SerializeField] [HideInInspector] private string _text = "";
        public string text
        {
            get { return _text; }
            set
            {
                if (value != _text)
                {
                    _text = value;
                    RefreshCharacters();
                }
            }
        }

        [SerializeField] [HideInInspector] private TextAnchor _anchor = TextAnchor.MiddleCenter;

        public TextAnchor anchor
        {
            get { return _anchor; }
            set { if (value != _anchor) { _anchor = value; UpdateAllCharacterPositions(); } }
        }


        private void RefreshCharacters()
        {
            if (!font)
                return;

            if (_characterObjects == null)
                _characterObjects = new List<MeshCharacter>();

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (font.meshCharacters.ContainsKey(c))
                {
                    Mesh m = font.meshCharacters[c];
                    if (m != null)
                    {
#if UNITY_EDITOR

#endif
                        GetCharacter(i).filter.mesh = m;
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
            }

            for (int i = text.Length; i < _characterObjects.Count; i++)
            {
                _characterObjects[i].renderer.enabled = false;
#if UNITY_EDITOR
                _characterObjects[i].position = _characterObjects[0].position;
#endif
            }

            UpdateAllCharacterPositions();
        }


        //===== Unity Events =====

        private void OnDisable()
        {
            Purge();
        }

        private void OnEnable()
        {
            RefreshCharacters();
            UpdateAllCharacterRenderers();
        }




        //===== Character Objects =====

        private List<MeshCharacter> _characterObjects;

        private MeshCharacter GetCharacter(int i)
        {
            if (_characterObjects == null)
                _characterObjects = new List<MeshCharacter>();

            while (_characterObjects.Count <= i)
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

        [ContextMenu("Purge")]
        private void Purge()
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

        private Vector3 basePoint;

        private void UpdateBasePoint()
        {
            if (text.Length == 0)
                return;

            basePoint.z = 0f;

            switch (anchor)
            {
                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter:
                    basePoint.x = -font.distanceBetweenCharacters * (text.Length - 1) * 0.5f;
                    break;
                case TextAnchor.UpperLeft:
                case TextAnchor.MiddleLeft:
                case TextAnchor.LowerLeft:
                    basePoint.x = font.characterBounds.extents.x;
                    break;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    basePoint.x = (-font.distanceBetweenCharacters * (text.Length - 1)) - font.characterBounds.extents.x;
                    break;
            }

            switch (anchor)
            {
                case TextAnchor.MiddleLeft:
                case TextAnchor.MiddleCenter:
                case TextAnchor.MiddleRight:
                    basePoint.y = 0f;
                    break;
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    basePoint.y = font.characterBounds.extents.y;
                    break;
                case TextAnchor.UpperLeft:
                case TextAnchor.UpperCenter:
                case TextAnchor.UpperRight:
                    basePoint.y = -font.characterBounds.extents.y;
                    break;
            }
        }
        private void UpdateCharacterPosition(int i)
        {
            _characterObjects[i].UpdatePosition(basePoint + new Vector3(font.distanceBetweenCharacters * i, 0f, 0f), font.characterRotation);
            _characterObjects[i].pivot = font.characterBounds.center;
        }

        private void UpdateCharacterRenderer(int i)
        {
            _characterObjects[i].renderer.sharedMaterials = _materials;
            _characterObjects[i].renderer.shadowCastingMode = shadowCastingMode;
            _characterObjects[i].renderer.receiveShadows = receiveShadows;
        }

        public void UpdateAllCharacterPositions()
        {
            UpdateBasePoint();
            for (int i = 0; i < text.Length; i++)
            {
                UpdateCharacterPosition(i);
            }
            if(TextUpdated != null)
                TextUpdated.Invoke();
        }
        public void UpdateAllCharacterRenderers()
        {
            if (_characterObjects == null)
                return;
            for (int i = 0; i < _characterObjects.Count; i++)
            {
                UpdateCharacterRenderer(i);
            }
        }


        //===== Renderer Settings =====

        [SerializeField] private Material[] _materials = new Material[1];
        public Material[] materials { get { return _materials; } }
        public void SetMaterials(Material[] materials)
        {
            _materials = materials;
            UpdateAllCharacterRenderers();
        }

        [SerializeField] [HideInInspector] private UnityEngine.Rendering.ShadowCastingMode _shadowCastingMode;
        public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode
        {
            get { return _shadowCastingMode; }
            set
            {
                if (value != _shadowCastingMode)
                {
                    _shadowCastingMode = value;
                    UpdateAllCharacterRenderers();
                }
            }
        }

        [SerializeField] [HideInInspector] private bool _receiveShadows;
        public bool receiveShadows
        {
            get { return _receiveShadows; }
            set
            {
                if (value != _receiveShadows)
                {
                    _receiveShadows = value;
                    UpdateAllCharacterRenderers();
                }
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
                    for (int i = 0; i < text.Length; i++)
                    {
                        GetCharacter(i).offset = Vector3.zero;
                        GetCharacter(i).rotationaloffset = Vector3.zero;
                    }
                }
            }
        }

        private void Update()
        {
            if (textEffects == null)
                return;

            for (int i = 0; i < text.Length; i++)
            {
                Vector3 o = Vector3.zero;
                Vector3 ro = Vector3.zero;
                textEffects.Invoke(i, ref o, ref ro);
                GetCharacter(i).UpdateOffset(o, ro);
            }
        }



        //===== External Events =====

        public System.Action TextUpdated;




        //===== Data =====
        
        public Bounds LocalBounds
        {
            get 
            {
                Bounds bounds = new Bounds();
                if (text.Length == 0)
                    return bounds;
                bounds.min = _characterObjects[0].position - font.characterBounds.extents;
                bounds.max = _characterObjects[text.Length - 1].position + font.characterBounds.extents;
                return bounds;
            }
        }








        //===== Gizmos =====

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!font || text.Length == 0)
            {
                return;
            }

            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;

            if (Selection.activeObject && Selection.activeObject == ((Object)font))
                Gizmos.color = new Color(0f, 1f, .5f, .25f);
            else
                Gizmos.color = Color.clear;

            Vector3 textExtent = Vector3.right * font.distanceBetweenCharacters * (text.Length - 1);


            Gizmos.DrawCube(basePoint + (textExtent * 0.5f), font.characterBounds.size + textExtent + Vector3.one * 0.01f);


            if (Selection.activeObject && Selection.activeObject == ((Object)font))
            {
                Gizmos.color = new Color(1f, .4f, .1f, .25f);
                Gizmos.DrawCube(basePoint, font.characterBounds.size + Vector3.one * 0.005f);

                Gizmos.color = new Color(.1f, .4f, 1f, 1f);
                Gizmos.DrawLine(basePoint + new Vector3(-.2f, -.2f), basePoint + new Vector3(.2f, .2f));
                Gizmos.DrawLine(basePoint + new Vector3(.2f, -.2f), basePoint + new Vector3(-.2f, .2f));
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


            rend.font = EditorGUILayout.ObjectField(rend.font, typeof(MeshFont), allowSceneObjects: false) as MeshFont;
            rend.text = EditorGUILayout.TextField("Text", rend.text);

            GUILayout.Space(6);

            rend.anchor = (TextAnchor)EditorGUILayout.EnumPopup("Text anchor", rend.anchor);

            GUILayout.Space(6);


            EditorGUI.BeginChangeCheck();

            materialsDropdown = EditorGUILayout.BeginFoldoutHeaderGroup(materialsDropdown, "Materials");
            if(materialsDropdown)
            {
                EditorGUI.indentLevel = 1;

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

                EditorGUI.indentLevel = 0;
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
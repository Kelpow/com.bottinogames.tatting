using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class TattingRenderer : MonoBehaviour
{
    [HideInInspector] public TattingFont font;

    [SerializeField] [HideInInspector] private string _text = "";
    public string text
    {
        get { return _text; }
        set
        {
            if(value != _text)
            {
                _text = value;
                RefreshCharacters();
            }
        }
    }

    public void RefreshCharacters ()
    {
        if (!font)
            return;
        
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (font.meshCharacters.ContainsKey(c))
            {
                Mesh m = font.meshCharacters[c];
                if(m != null)
                {
                    GetCharacter(i).filter.mesh = m;
                    GetCharacter(i).renderer.enabled = true;
                }
                else
                {
                    GetCharacter(i).renderer.enabled = false;
                }
            }
        }

        for (int i = text.Length; i < _characterObjects.Count; i++)
        {
            _characterObjects[i].renderer.enabled = false;
        }
    }


    //===== Unity Events =====

    private void OnDisable()
    {
        _characterObjects = null;
        TattingCharacter[] found = GetComponentsInChildren<TattingCharacter>();
        for (int i = 0; i < found.Length; i++)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(found[i].gameObject);
            else
#endif
                Destroy(found[i].gameObject);
        }
    }

    private void OnEnable()
    {
        RefreshCharacters();
        UpdateAllCharacterRenderers();
    }




    //===== Character Objects =====

    private List<TattingCharacter> _characterObjects;

    private TattingCharacter GetCharacter(int i)
    {
        if (_characterObjects == null)
            _characterObjects = new List<TattingCharacter>();

        while (_characterObjects.Count <= i)
            CreateNewCharacterObject();

        return _characterObjects[i];
    }

    private void CreateNewCharacterObject()
    {
        TattingCharacter newCharacter = new GameObject("Tatting Character Object", typeof(MeshFilter), typeof(MeshRenderer), typeof(TattingCharacter)).GetComponent<TattingCharacter>();
        
        newCharacter.transform.parent = this.transform;

        newCharacter.renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        newCharacter.renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        newCharacter.gameObject.hideFlags = HideFlags.HideAndDontSave;

        _characterObjects.Add(newCharacter);
        UpdateCharacterPosition(_characterObjects.Count - 1);
        UpdateCharacterRenderer(_characterObjects.Count - 1);
    }


    //===== Modifying Character Objects =====
    private void UpdateCharacterPosition(int i)
    {
        _characterObjects[i].position = new Vector3(font.distanceBetweenCharacters * i, 0f, 0f);
        _characterObjects[i].rotation = font.characterRotation;
    }
    private void UpdateCharacterRenderer(int i)
    {
        _characterObjects[i].renderer.sharedMaterials = _materials;
        _characterObjects[i].renderer.shadowCastingMode = shadowCastingMode;
        _characterObjects[i].renderer.receiveShadows = receiveShadows;
    }

    public void UpdateAllCharacterPositions()
    {
        for (int i = 0; i < _characterObjects.Count; i++)
        {
            UpdateCharacterPosition(i);
        }
    }
    public void UpdateAllCharacterRenderers()
    {
        for (int i = 0; i < _characterObjects.Count; i++)
        {
            UpdateCharacterRenderer(i);
        }
    } 


    //===== Renderer Settings =====

    [SerializeField] private Material[] _materials = new Material[1];
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
            if(value != _shadowCastingMode)
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




#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!font || text.Length == 0)
            return;

        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.matrix = rotationMatrix;

        if (Selection.activeObject && Selection.activeObject == ((Object)font))
            Gizmos.color = new Color(0f, 1f, .5f, .25f);
        else
            Gizmos.color = Color.clear;

        Vector3 textExtent = Vector3.right * font.distanceBetweenCharacters * (text.Length - 1);
        Gizmos.DrawCube((Quaternion.Euler(font.characterRotation) * font.fontBounds.center) + (textExtent * 0.5f), font.fontBounds.size + textExtent + Vector3.one * 0.01f);
    }
#endif
}



#if UNITY_EDITOR
[CustomEditor(typeof(TattingRenderer))]
public class TattingRendererInspector : Editor
{

    public override void OnInspectorGUI()
    {
        TattingRenderer rend = (TattingRenderer)target;


        rend.font = EditorGUILayout.ObjectField(rend.font, typeof(TattingFont), allowSceneObjects: false) as TattingFont;
        rend.text = EditorGUILayout.TextField("Text", rend.text);

        EditorGUI.BeginChangeCheck();

        //I didn't want to bother with making a gui for the array so I use the default
        DrawDefaultInspector();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_shadowCastingMode"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_receiveShadows"));

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            rend.UpdateAllCharacterRenderers();
        }
    }

}

#endif
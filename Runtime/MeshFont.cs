using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tatting
{

    [System.Serializable]
    [CreateAssetMenu(fileName = "New Tatting Mesh Font", menuName = "Tatting Mesh Font", order = 540)]
    public class MeshFont : ScriptableObject, ISerializationCallbackReceiver
    {

        private static CharacterInfo zeroWidthCharacter;
        public static CharacterInfo ZeroWidthCharacter
        {
            get
            {
                if (zeroWidthCharacter == null)
                {
                    zeroWidthCharacter = new CharacterInfo('~');
                    zeroWidthCharacter.mesh = CharacterInfo.emptyMesh;
                    zeroWidthCharacter.width = 0f;
                }
                return zeroWidthCharacter;
            }
        }

        private CharacterInfo _whitespaceCharacter;
        private CharacterInfo WhitespaceCharacter
        {
            get
            {
                if (_whitespaceCharacter != null)
                {
                    _whitespaceCharacter.width = whitespaceWidth;
                    return _whitespaceCharacter;
                }

                _whitespaceCharacter = new CharacterInfo(' ');
                _whitespaceCharacter.mesh = CharacterInfo.emptyMesh;
                _whitespaceCharacter.width = whitespaceWidth;
                return _whitespaceCharacter;
            }
        }

        private CharacterInfo _fallbackCharacter;
        private CharacterInfo FallbackCharacter
        {
            get
            {
                if(characterDictionary.Count <= 0)
                {
                    return zeroWidthCharacter;
                }    

                _fallbackCharacter = new CharacterInfo('~');
                Mesh fallbackMesh = new Mesh();

                CharacterInfo info = null;
                string basicSearchSet = "ABCMWabcmw0?";
                foreach (char c in basicSearchSet)
                {
                    if (characterDictionary.TryGetValue(c, out info))
                        break;
                }
                if (info == null)
                    info = characterDictionary.Values.First();

                _fallbackCharacter.width = info.width;

                Bounds b = info.mesh.bounds;

                Vector3[] vertices = new Vector3[] {
                    b.min,
                    new Vector3(b.max.x,b.min.y,b.min.z),
                    new Vector3(b.min.x,b.min.y,b.max.z),
                    new Vector3(b.max.x,b.min.y,b.max.z),
                    new Vector3(b.min.x,b.max.y,b.min.z),
                    new Vector3(b.max.x,b.max.y,b.min.z),
                    new Vector3(b.min.x,b.max.y,b.max.z),
                    b.max
                };
                int[] triangles = new int[] {
                    0,1,3,
                    0,3,2,

                    0,4,5,
                    0,5,1,

                    0,2,6,
                    0,6,4,

                    7,6,2,
                    7,2,3,

                    7,3,1,
                    7,1,5,

                    7,5,4,
                    7,4,6,
                };

                fallbackMesh.SetVertices(vertices);
                fallbackMesh.SetTriangles(triangles, 0);
                

                _fallbackCharacter.mesh = fallbackMesh;
                return _fallbackCharacter;
            }
        }


        //spacing to use for whitespace character (' ')
        public float whitespaceWidth = 1f;

        //default spacing between lines
        public float lineSpacing = 1f;

        public float unitsPerEM = .001f;

        [SerializeField] private List<CharacterInfo> _dictionarySerializationHelper = new List<CharacterInfo>();
        public Dictionary<char, CharacterInfo> characterDictionary = new Dictionary<char, CharacterInfo>();


        public CharacterInfo GetCharacterInfo(char c)
        {
            if (characterDictionary.TryGetValue(c, out var info))
            {
                return info;
            } 
            else if (c == ' ')
            {
                return WhitespaceCharacter;
            }
            else
            {
                return FallbackCharacter;
            }
        }



        public void OnBeforeSerialize()
        {
            _dictionarySerializationHelper.Clear();
            foreach (var kvp in characterDictionary)
                _dictionarySerializationHelper.Add(kvp.Value);
        }

        public void OnAfterDeserialize()
        {
            characterDictionary = new Dictionary<char, CharacterInfo>();
            foreach (var info in _dictionarySerializationHelper)
                characterDictionary.Add(info.character, info);

        }



        [System.Serializable]
        public class CharacterInfo
        {
            private static Mesh _emptyMesh;
            public static Mesh emptyMesh { get { if (!_emptyMesh) { _emptyMesh = new Mesh(); } return _emptyMesh; } }

            public char character;
            public Mesh mesh;
            public float width;

            public CharacterInfo(char c)
            {
                character = c;
                mesh = null;
                width = 0f;
            }
        }


#if UNITY_EDITOR
        
        void LoadFromMeshes(IEnumerable<Mesh> meshes, bool addToCharSet, bool overwriteExistingMeshes, bool clearUnfoundCharacters)
        {
            HashSet<char> foundChars = new HashSet<char>();

            foreach (Mesh mesh in meshes)
            {
                if (mesh.name.Length != 1)
                    continue;
                
                char c = mesh.name[0];
                
                
                if(characterDictionary.TryGetValue(c, out var info))
                {
                    if (overwriteExistingMeshes)
                        info.mesh = mesh;
                }
                else
                {
                    if (addToCharSet) 
                    {
                        var newInfo = new CharacterInfo(c);
                        newInfo.mesh = mesh;
                        characterDictionary.Add(c, newInfo);
                    }
                }

                if (clearUnfoundCharacters)
                    foundChars.Add(c);
            }
            if (clearUnfoundCharacters)
            {
                List<char> unfoundChars = new List<char>();
                foreach (var kvp in characterDictionary)
                {
                    if (!foundChars.Contains(kvp.Key))
                        unfoundChars.Add(kvp.Key);
                }
                foreach (var c in unfoundChars)
                {
                    characterDictionary.Remove(c);
                }
            }
        }


#endif
    }







    // ||||||||||||||||||||| E D I T O R |||||||||||||||||||||

#if UNITY_EDITOR
    [CustomEditor(typeof(MeshFont))]
    public class TattingFontInspector : Editor
    {

        public override void OnInspectorGUI()
        {
            MeshFont font = (MeshFont)target;

            Undo.RecordObject(font, "Tatting MeshFont Inspector");



            string set = "";
            foreach (var kvp in font.characterDictionary)
                set += kvp.Key;


            EditorGUI.BeginChangeCheck();

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Edit Character Set"))
                {
                    PopupWindow.Show(GUILayoutUtility.GetLastRect(), new CharacterSetPopupWindow(font));
                }
                if (GUILayout.Button("Automatic Setup"))
                {
                    PopupWindow.Show(GUILayoutUtility.GetLastRect(), new AutomaticSetupPopupWindow(font));
                }
                if (GUILayout.Button("Load Kerning from Font"))
                {
                    PopupWindow.Show(GUILayoutUtility.GetLastRect(), new AutomaticKerningPopupWindow(font));
                }
            }
            GUILayout.EndHorizontal();


            GUILayout.BeginVertical("HelpBox");
            {
                font.whitespaceWidth = EditorGUILayout.FloatField("Whitespace width", font.whitespaceWidth);
            }
            GUILayout.EndVertical();


            for (int y = 0; y < set.Length; y+=3)
            {
                GUILayout.BeginHorizontal();
                {
                    for (int x = 0; x < 3; x++)
                    {
                        if (x + y < set.Length)
                        {
                            char c = set[y + x];
                            GUILayout.BeginVertical("HelpBox");
                            {
                                GUILayout.BeginHorizontal();
                                {

                                    GUILayout.Label(c.ToString(), GUILayout.Width(14));
                                    font.characterDictionary[c].mesh = (Mesh)EditorGUILayout.ObjectField(font.characterDictionary[c].mesh, typeof(Mesh), allowSceneObjects: false);
                                    GUILayout.Space(4);
                                }
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label("Width", GUILayout.ExpandWidth(false));
                                    if (Event.current.isMouse && Event.current.button == 1 && Event.current.rawType == EventType.MouseUp && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                                    {
                                        GenericMenu gen = new GenericMenu();
                                        if (font.characterDictionary[c].mesh)
                                            gen.AddItem(new GUIContent("Set Automatically From Bounds", "Sets the width automatically using the mesh bounds"), false, () => { font.characterDictionary[c].width = font.characterDictionary[c].mesh.bounds.max.x; });
                                        else
                                            gen.AddDisabledItem(new GUIContent("Set Automatically From Bounds", "Sets the width automatically using bounds.max.x"));

                                        gen.ShowAsContext();
                                    }
                                    font.characterDictionary[c].width = EditorGUILayout.FloatField(font.characterDictionary[c].width);
                                }
                                GUILayout.EndHorizontal();

                                if (font.characterDictionary[c].mesh)
                                {
                                    //GUILayout.Box(AssetPreview.GetAssetPreview(font.characterDictionary[c].mesh), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinWidth(70), GUILayout.MinHeight(70));
                                }
                            }
                            GUILayout.EndVertical();
                        }
                        else
                        {
                            GUI.color = Color.clear;
                            GUI.enabled = false;
                            GUILayout.BeginVertical("HelpBox");
                            {

                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label("a", GUILayout.Width(14));
                                    EditorGUILayout.ObjectField(null, typeof(Mesh), allowSceneObjects: false);
                                    GUILayout.Space(4);
                                }
                                GUILayout.EndHorizontal();
                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label("Width", GUILayout.ExpandWidth(false));
                                    EditorGUILayout.FloatField(0f);
                                }
                                GUILayout.EndHorizontal();
                                GUILayout.Box(GUIContent.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinWidth(70), GUILayout.MinHeight(70));
                            }
                            GUILayout.EndVertical();
                            GUI.color = Color.white;
                            GUI.enabled = false;
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(font);
                UpdateAllTextMeshesSharingFont(font);
            }   
        }


        public static void UpdateAllTextMeshesSharingFont(MeshFont font)
        {
            foreach (MeshText text in Resources.FindObjectsOfTypeAll<MeshText>())
                if (!EditorUtility.IsPersistent(text.transform.root.gameObject) && text.Font == font)
                    text.SendMessage("ForceMeshUpdate");
        }
    }


    public class CharacterSetPopupWindow : PopupWindowContent
    {
        string startSet;
        string set;
        MeshFont font;

        public CharacterSetPopupWindow(MeshFont font)
        {
            this.font = font;
            set = "";
            foreach (var kvp in font.characterDictionary)
                set += kvp.Key;

            startSet = set;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(200, 100);
        }

        public override void OnGUI(Rect rect)
        {
            if (Event.current != null && Event.current.isKey && Event.current.keyCode == KeyCode.Return)
                editorWindow.Close();

            set = GUILayout.TextArea(set,GUILayout.Height(74));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel"))
            {
                set = startSet;
                editorWindow.Close();
            }
            if (GUILayout.Button("Submit"))
                editorWindow.Close();
            if (GUILayout.Button(EditorGUIUtility.IconContent("AlphabeticalSorting")))
            {
                resort = true;
                editorWindow.Close();
            }
            GUILayout.EndHorizontal();
        }

        bool resort = false;

        public override void OnClose()
        {
            if (!resort && set == startSet)
                return;

            Dictionary<char, MeshFont.CharacterInfo> newDict = new Dictionary<char, MeshFont.CharacterInfo>();
            if (set.Length > 0)
            {
                char[] sortedSet = set.Replace(" ", "").ToCharArray();
                Array.Sort(sortedSet);
                foreach (char c in sortedSet)
                {
                    if (c == '\n')
                        continue;

                    if (!newDict.ContainsKey(c))
                    {
                        if (font.characterDictionary.ContainsKey(c))
                            newDict.Add(c, font.characterDictionary[c]);
                        else
                            newDict.Add(c, new MeshFont.CharacterInfo(c));
                    }
                }
            }
            font.characterDictionary = newDict;

            TattingFontInspector.UpdateAllTextMeshesSharingFont(font);
        }
    }

    public class AutomaticSetupPopupWindow : PopupWindowContent
    {
        MeshFont font;

        public AutomaticSetupPopupWindow(MeshFont font)
        {
            this.font = font;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(300, 85);
        }

        bool overwrite;
        bool overwriteSet;

        public override void OnGUI(Rect rect)
        {
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Automatic Font Setup", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            overwrite = GUILayout.Toggle(overwrite, "Overwrite");
            GUILayout.BeginHorizontal();
            GUI.enabled = overwrite;
            GUILayout.Space(10);
            overwriteSet = GUILayout.Toggle(overwriteSet, "Overwrite Character Set");
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel"))
                editorWindow.Close();
            if (GUILayout.Button("Load File"))
            {

                string fontPath = AssetDatabase.GetAssetPath(font);
                string path = EditorUtility.OpenFilePanel("Open 3D object file", fontPath.Remove(fontPath.LastIndexOf('/')), "obj,fbx,blend");

                if(path != "")
                {
                    string relativePath = "Assets" + path.Replace(Application.dataPath, "");
                    UnityEngine.Object[] objects = AssetDatabase.LoadAllAssetsAtPath(relativePath);

                    List<Mesh> loadedMeshes = new List<Mesh>();
                    for (int i = 0; i < objects.Length; i++)
                    {
                        Mesh mesh = objects[i] as Mesh;
                        if (mesh)
                        {
                            loadedMeshes.Add(mesh);
                        }
                    }

                    for (int i = 0; i < loadedMeshes.Count; i++)
                    {
                        Mesh mesh = loadedMeshes[i];
                        if (mesh.name.Length != 1)
                        {
                            loadedMeshes.RemoveAt(i);
                            i--;
                            continue;
                        }
                        char c = mesh.name[0];
                        if (!overwrite)
                        {
                            if (font.characterDictionary.ContainsKey(c) && font.characterDictionary[c].mesh != null)
                            {
                                loadedMeshes.RemoveAt(i);
                                i--;
                                continue;
                            }
                            else
                                continue;
                        }
                    }

                    if(loadedMeshes.Count > 0)
                    {
                        if (overwriteSet)
                        {
                            Dictionary<char, MeshFont.CharacterInfo> newDict = new Dictionary<char, MeshFont.CharacterInfo>();
                            foreach (Mesh mesh in loadedMeshes)
                            {
                                char c = mesh.name[0];
                                if (newDict.ContainsKey(c))
                                    continue;

                                MeshFont.CharacterInfo info = new MeshFont.CharacterInfo(c);
                                info.mesh = mesh;
                                info.width = mesh.bounds.max.x;
                                newDict.Add(c, info);
                            }
                            font.characterDictionary = newDict;
                        }
                        else
                        {
                            foreach (Mesh mesh in loadedMeshes)
                            {
                                char c = mesh.name[0];
                                if (font.characterDictionary.ContainsKey(c))
                                    font.characterDictionary[c].mesh = mesh;
                                else
                                {
                                    MeshFont.CharacterInfo info = new MeshFont.CharacterInfo(c);
                                    info.mesh = mesh;
                                    info.width = mesh.bounds.max.x;
                                    font.characterDictionary.Add(c, info);
                                }
                            }
                        }
                    }

                    TattingFontInspector.UpdateAllTextMeshesSharingFont(font);
                }
            }
            GUILayout.EndHorizontal();
        }

    }

    public class AutomaticKerningPopupWindow : PopupWindowContent
    {
        MeshFont font;

        public AutomaticKerningPopupWindow(MeshFont font)
        {
            this.font = font;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(400, 120);
        }

        float widthMultiplier = 1f;

        public override void OnGUI(Rect rect)
        {
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Automatic Kerning", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            GUILayout.Label(
                "Load width values from a .ttf or .otf file.\n" +
                "EM width is the width of 1 EM in Unity units.\n" +
                "1 is the default multiplier of EM in Blender. Usually. ¯\\_(ツ)_/¯");

            widthMultiplier = EditorGUILayout.FloatField("EM Width", widthMultiplier);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel"))
                editorWindow.Close();

            if (GUILayout.Button("Load File"))
            {
                string fontPath = AssetDatabase.GetAssetPath(font);
                string path = EditorUtility.OpenFilePanel("Open font file", fontPath.Remove(fontPath.LastIndexOf('/')), "ttf,otf");
                Debug.Log(path);

                if (path != "")
                {

                    string set = "";
                    foreach (var kvp in font.characterDictionary)
                        set += kvp.Key;

                    Typography.OpenFont.OpenFontReader openFontReader = new Typography.OpenFont.OpenFontReader();
                    System.IO.Stream stream = new System.IO.FileStream(path, System.IO.FileMode.Open);
                    Typography.OpenFont.Typeface typeface = openFontReader.Read(stream);
                    stream.Close();

                    Typography.TextLayout.GlyphLayout gl = new Typography.TextLayout.GlyphLayout();
                    gl.Typeface = typeface;


                    gl.Layout(set.ToCharArray(), 0, set.Length);

                    foreach (var item in gl.GetUnscaledGlyphPlanIter())
                    {
                        Debug.Log($"{set[item.input_cp_offset]} : {item.AdvanceX}, {typeface.UnitsPerEm}, {(float)item.AdvanceX / (float)typeface.UnitsPerEm}");
                        font.characterDictionary[set[item.input_cp_offset]].width = ((float)item.AdvanceX / (float)typeface.UnitsPerEm) * widthMultiplier;
                    }

                    gl.Layout(new char[] { ' ' }, 0, 1);
                    foreach (var item in gl.GetUnscaledGlyphPlanIter())
                    {
                        font.whitespaceWidth = ((float)item.AdvanceX / (float)typeface.UnitsPerEm) * widthMultiplier;
                    }

                    TattingFontInspector.UpdateAllTextMeshesSharingFont(font);
                }
            }
            GUILayout.EndHorizontal();
        }
    }
#endif

}
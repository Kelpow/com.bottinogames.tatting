using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tatting
{

    [System.Serializable]
    [CreateAssetMenu(fileName = "New Tatting Mesh Font", menuName = "Tatting Mesh Font", order = 540)]
    public class MeshFont : ScriptableObject, ISerializationCallbackReceiver
    {
        private static CharacterInfo _zeroWidthCharacter;
        public static CharacterInfo zeroWidthCharacter
        {
            get
            {
                if (_zeroWidthCharacter == null)
                {
                    _zeroWidthCharacter = new CharacterInfo('~');
                    _zeroWidthCharacter.mesh = CharacterInfo.emptyMesh;
                    _zeroWidthCharacter.width = 0f;
                }
                return _zeroWidthCharacter;
            }
        }

        //spacing to use for whitespace (' ') if the dictionary does not already contain a space character
        public float whitespaceWidth = 1f;

        public bool hasFallbackCharacter = false;


        [SerializeField] private List<CharacterInfo> _dictionarySerializationHelper = new List<CharacterInfo>();
        public Dictionary<char, CharacterInfo> characterDictionary = new Dictionary<char, CharacterInfo>();
        private CharacterInfo _whitespaceCharacter;
        public CharacterInfo whitespaceCharacter
        {
            get
            {
                if (_whitespaceCharacter == null)
                {
                    _whitespaceCharacter = new CharacterInfo(' ');
                    _whitespaceCharacter.width = whitespaceWidth;
                    _whitespaceCharacter.mesh = CharacterInfo.emptyMesh;
                } else if (_whitespaceCharacter.mesh == null)
                {
                    _whitespaceCharacter.mesh = CharacterInfo.emptyMesh;
                }


                if (_whitespaceCharacter.width != whitespaceWidth)
                    _whitespaceCharacter.width = whitespaceWidth;

                return _whitespaceCharacter;
            }
        }
        [SerializeField] private CharacterInfo _fallbackCharacter;
        public CharacterInfo fallbackCharacter
        {
            get
            {
                if (hasFallbackCharacter)
                {
                    return _fallbackCharacter;
                }
                else
                {
                    return zeroWidthCharacter;
                }
            }
        }




        public CharacterInfo GetCharacterInfo(char c)
        {
            if (characterDictionary.ContainsKey(c))
            {
                return characterDictionary[c];
            } 
            else if (c == ' ')
            {
                return whitespaceCharacter;
            } 
            else
            {
                return fallbackCharacter;
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
                if (GUILayout.Button("Automatic Kerning"))
                {
                    PopupWindow.Show(GUILayoutUtility.GetLastRect(), new AutomaticKerningPopupWindow(font));
                }
            }
            GUILayout.EndHorizontal();


            GUILayout.BeginVertical("HelpBox");
            {
                font.whitespaceWidth = EditorGUILayout.FloatField("Whitespace width", font.whitespaceWidth);
                font.hasFallbackCharacter = EditorGUILayout.Toggle("Fallback Character", font.hasFallbackCharacter);
                if (font.hasFallbackCharacter)
                {
                    GUILayout.BeginVertical("HelpBox");
                    {
                        font.fallbackCharacter.mesh = (Mesh)EditorGUILayout.ObjectField("Fallback Mesh", font.fallbackCharacter.mesh, typeof(Mesh), allowSceneObjects: false);
                        font.fallbackCharacter.width = EditorGUILayout.FloatField("Width", font.fallbackCharacter.width);
                    }
                    GUILayout.EndVertical();
                }
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
                                    GUILayout.Box(AssetPreview.GetAssetPreview(font.characterDictionary[c].mesh), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinWidth(70), GUILayout.MinHeight(70));
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
                if (!EditorUtility.IsPersistent(text.transform.root.gameObject) && text.font == font)
                    text.SendMessage("FontHasChanged");
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
                char[] sortedSet = set.ToCharArray();
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

        float widthMultiplier = 0.001f;

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
                "Unit width is the width of 1 font design unit in Unity units.\n" +
                "0.001 is the default width of a design unit in Blender.");

            EditorGUILayout.FloatField("Unit width", widthMultiplier);

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
                        font.characterDictionary[set[item.input_cp_offset]].width = item.AdvanceX / 1000f;
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

    }
#endif

}
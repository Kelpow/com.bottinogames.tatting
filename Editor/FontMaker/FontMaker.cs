using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Tatting
{
    internal class FontMaker : EditorWindow
    {
#if UNITY_EDITOR_WIN
        const string MENUITEMNAME = "Window/Tatting Font Maker";
#else
        const string MENUITEMNAME = "Window/Tatting Font Maker - WINDOWS ONLY";
#endif

        enum CharacterSet
        {
            Latin,
            Extended_Latin,
            All_supported_characters
        }

        // Add menu named "My Window" to the Window menu
        [MenuItem(MENUITEMNAME, false)]
        static void Init()
        {
#if UNITY_EDITOR_WIN
            //Icon reference can be found at: https://github.com/halak/unity-editor-icons
            errorIcon = EditorGUIUtility.IconContent("TestFailed");
            okIcon = EditorGUIUtility.IconContent("TestPassed");

            folderIcon = EditorGUIUtility.IconContent("d_Project");
            refreshIcon = EditorGUIUtility.IconContent("d_Refresh@2x");
            eyedropperIcon = EditorGUIUtility.IconContent("d_eyeDropper.Large");

            FontMakerData.instance.ValidateBlenderInstallPath();
            if (!FontMakerData.instance.validBlenderInstall)
                FontMakerData.instance.FindBlenderInRegistry();

            
            FontMaker window = (FontMaker)EditorWindow.GetWindow(typeof(FontMaker), true, "Tatting Font Maker");
            window.minSize = new Vector2(600, 800); 
            window.maxSize = new Vector2(600, 800); 

            window.ShowUtility();
#endif
        }


        // Add menu named "My Window" to the Window menu
        [MenuItem(MENUITEMNAME, true)]
        static bool ValidateMenuItem()
        {
#if UNITY_EDITOR_WIN
            return true;
#else
            return false;
#endif
        }

#if UNITY_EDITOR_WIN

        FontMakerReview child;
        bool focusException = false;

        static GUIContent errorIcon;
        static GUIContent okIcon;

        static GUIContent folderIcon;
        static GUIContent eyedropperIcon;
        static GUIContent refreshIcon;

        string FontFilePath
        {
            get { return tfData.fontFilePath; }
            set
            {
                tfData.fontFilePath = value;
                if (System.IO.File.Exists(value))
                {
                    string ext = System.IO.Path.GetExtension(value).ToLower();
                    validfontpath = ext == ".otf" || ext == ".ttf";
                }
                else
                    validfontpath = false;
            }
        }
        bool validfontpath = false;
        int objectpickerID = int.MinValue;

        public const string TEMPMODELPATH = "Assets/TEMP_FONTMAKER_PREVIEW.fbx";    

        TextToFBXData tfData = new TextToFBXData();

        CharacterSet selectedCharacterSet;

        Mesh previewMesh;
        bool previewAccurate = false;

        Editor previewEditor;

        private void OnEnable()
        {
            this.position = FontMakerData.instance.position;

            tfData.extrude = 0.1f;
            tfData.resolution = 3;

            tfData.doBevel = false;

            tfData.bevelWidth = 0;
            tfData.bevelResolution = 0;

            tfData.previewString = "Preview!";


            EditorApplication.LockReloadAssemblies();
        }

        private void OnFocus()
        {
            if (child != null)
            {
                child.Focus();
            }
        }

        private void OnLostFocus()
        {
            if (!Application.isFocused)
                return;
            Debug.Log(EditorWindow.focusedWindow);
            if (child == null)
            {
                if (!focusException)
                {
                    Focus();
                }

                focusException = false;
            }
        }

        private void OnGUI()
        {
            if(Event.current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == objectpickerID)
            {
                Font font = (Font)EditorGUIUtility.GetObjectPickerObject();
                if (font != null)
                {
                    FontFilePath = System.IO.Path.GetFullPath(AssetDatabase.GetAssetPath(font));
                }
                Repaint();
                objectpickerID = int.MinValue;
            }


            var data = FontMakerData.instance;



            #region ====================== BLENDER INSTALL ======================
            //===================================================================
            //===================================================================

            GUI.color = data.validBlenderInstall ? Color.green : Color.red;
            GUILayout.BeginVertical("HelpBox");
            {
                GUI.color = Color.white;
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Blender Installation", EditorStyles.boldLabel, GUILayout.Height(20f));
                    if (!data.validBlenderInstall)
                        EditorGUILayout.HelpBox("No instance of 'blender.exe' at the selected path", MessageType.None);
                }
                GUILayout.EndHorizontal();


                GUI.color = data.validBlenderInstall ? Color.green : Color.red;
                GUILayout.BeginHorizontal("HelpBox");
                {
                    GUI.color = Color.white;

                    GUILayout.Label(data.validBlenderInstall ? okIcon : errorIcon, GUILayout.Width(20));
                    data.BlenderInstallPath = GUILayout.TextField(data.BlenderInstallPath, GUILayout.Width(this.position.width - 102), GUILayout.Height(20), GUILayout.ExpandWidth(false));
                    if (GUILayout.Button(folderIcon, GUILayout.Width(25), GUILayout.Height(20)))
                    {
                        string openPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
                        string path = EditorUtility.OpenFilePanel("Locate blender.exe...", openPath, "exe");
                        if (path != "")
                            data.BlenderInstallPath = path;
                    }
                    if (GUILayout.Button(refreshIcon, GUILayout.Width(25), GUILayout.Height(20)))
                    {
                        data.FindBlenderInRegistry();
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            #endregion =======================================================



            #region ====================== FONT PATH ======================
            //=============================================================
            //=============================================================

            GUI.color = validfontpath ? Color.green : Color.red;
            GUILayout.BeginVertical("HelpBox");
            {
                GUI.color = Color.white;
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Font", EditorStyles.boldLabel, GUILayout.Height(20f));
                    if (!validfontpath)
                        EditorGUILayout.HelpBox("No valid .otf or .ttf file at the selected path", MessageType.None);
                }
                GUILayout.EndHorizontal();


                GUI.color = validfontpath ? Color.green : Color.red;
                GUILayout.BeginHorizontal("HelpBox");
                {
                    GUI.color = Color.white;

                    GUILayout.Label(validfontpath ? okIcon : errorIcon, GUILayout.Width(20));
                    FontFilePath = GUILayout.TextField(FontFilePath, GUILayout.Width(this.position.width - 102), GUILayout.Height(20), GUILayout.ExpandWidth(false));
                    if(GUILayout.Button(folderIcon, GUILayout.Width(25), GUILayout.Height(20)))
                    {
                        string openPath;
                        if (System.IO.Directory.Exists(FontFilePath))
                            openPath = FontFilePath;
                        else
                        {
                            string p = "";
                            if (System.IO.Path.IsPathRooted(FontFilePath))
                                p = System.IO.Path.GetDirectoryName(FontFilePath);
                            if (System.IO.Directory.Exists(p))
                                openPath = p;
                            else
                                openPath = Application.dataPath;
                        }
                        
                        string path = EditorUtility.OpenFilePanel("Open font file...", openPath, "otf,ttf");
                        if (path != "")
                            FontFilePath = path;
                    }
                    if (GUILayout.Button(eyedropperIcon, GUILayout.Width(25), GUILayout.Height(20)))
                    {
                        objectpickerID = EditorGUIUtility.GetControlID(235, FocusType.Passive);
                        focusException = true;
                        EditorGUIUtility.ShowObjectPicker<Font>(null, false, string.Empty, objectpickerID);
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            #endregion =======================================================


            GUILayout.Space(5f);
            GUILayout.Box(EditorGUIUtility.whiteTexture, GUILayout.Height(2f), GUILayout.ExpandWidth(true));
            GUILayout.Space(5f);

            bool setupValid = data.validBlenderInstall && validfontpath;
            GUI.enabled = setupValid;

            #region ====================== Generation Settings ======================
            //=============================================================
            //=============================================================
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginVertical("HelpBox");
            {
                GUILayout.Label("Generation Settings", EditorStyles.boldLabel);
                GUILayout.Space(7f);

                tfData.resolution = EditorGUILayout.IntSlider("Resolution", tfData.resolution, 1, 10);
                tfData.extrude = Mathf.Max(0f,EditorGUILayout.FloatField("Extrusion", tfData.extrude));

                tfData.doBevel = GUILayout.Toggle(tfData.doBevel, "Bevel");

                GUI.enabled = setupValid && tfData.doBevel;
                GUILayout.BeginVertical("HelpBox");
                {
                    tfData.bevelWidth = Mathf.Max(0f, EditorGUILayout.FloatField("Bevel Width", tfData.bevelWidth));
                    tfData.bevelResolution = EditorGUILayout.IntSlider("Bevel Resolution", tfData.bevelResolution, 0, 10);
                }
                GUILayout.EndVertical();
                GUI.enabled = setupValid;
            }
            GUILayout.EndVertical();
            #endregion =======================================================



            #region ====================== Preview Character ======================
            //=====================================================================
            //=====================================================================
            tfData.previewString = EditorGUILayout.TextField("Preview text", tfData.previewString);
            if (EditorGUI.EndChangeCheck())
                previewAccurate = false;
            GUI.enabled = !previewAccurate && setupValid;
            if(GUILayout.Button("Generate Preview Model"))
            {
                var typeface = GetTypefaceFromFile(tfData.fontFilePath);
                GenerateTextFBX(typeface, true, tfData);
                Focus();

                if (System.IO.File.Exists(TEMPMODELPATH))
                {
                    AssetDatabase.ImportAsset(TEMPMODELPATH, ImportAssetOptions.ForceUpdate);
                    Mesh assetMesh = AssetDatabase.LoadAssetAtPath<Mesh>(TEMPMODELPATH);
                    if (assetMesh != null)
                    {
                        previewMesh = Object.Instantiate(assetMesh);
                        if (previewEditor != null)
                            DestroyImmediate(previewEditor);
                        previewEditor = Editor.CreateEditor(previewMesh);
                        previewAccurate = true;
                        SetEditorViewDir(previewEditor);
                    }
                    AssetDatabase.DeleteAsset(TEMPMODELPATH);
                }
            }
            GUI.enabled = setupValid;

            GUILayout.BeginVertical("HelpBox", GUILayout.Height(425));
            {
                if (previewEditor != null)
                {
                    Rect rect = EditorGUILayout.GetControlRect(false, 400);
                    previewEditor.OnInteractivePreviewGUI(rect, GUIStyle.none);
                    GUILayout.Label($"Triangles: {previewMesh.triangles.Length} | Vertices: {previewMesh.vertices.Length}"); 
                }
                else
                {
                        GUILayout.FlexibleSpace();
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();
                            GUILayout.Label("No preview...");
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.FlexibleSpace();
                }
            }
            GUILayout.EndVertical();

            #endregion ============================================================


            GUILayout.Space(5f);
            GUILayout.Box(EditorGUIUtility.whiteTexture, GUILayout.Height(2f), GUILayout.ExpandWidth(true));
            GUILayout.Space(5f);

            #region ====================== Generate Full Character Set ======================
            //=====================================================================
            //=====================================================================

            GUILayout.BeginVertical("HelpBox");
            {
                selectedCharacterSet = (CharacterSet)EditorGUILayout.EnumPopup("Character Set", selectedCharacterSet);
                if(GUILayout.Button("Generate Full Character Set"))
                {
                    switch (selectedCharacterSet)
                    {
                        case CharacterSet.Latin:
                            tfData.characterSet = new (int, int)[] { 
                                (0x0000,0x007F) 
                            };
                            break;
                        case CharacterSet.Extended_Latin:
                            tfData.characterSet = new (int, int)[] { 
                                (0x0000, 0x007F), 
                                (0x0080, 0x00FF),
                                (0x0000, 0x017F),
                                (0x0180, 0x024F)
                            };
                            break;
                        case CharacterSet.All_supported_characters:
                            tfData.characterSet = new (int, int)[] { (0, int.MaxValue) };
                            break;
                        default:
                            tfData.characterSet = new (int, int)[] { (0, int.MaxValue) };
                            break;
                    }

                    var typeface = GetTypefaceFromFile(tfData.fontFilePath);
                    GenerateTextFBX(typeface, false, tfData);

                    string path = FontMakerData.instance.TempPath;
                    float advanceToBlend = 1f;

                    GetWidthsFromTypeface(typeface, tfData.setString, out float advanceWidthOfSpace, out Dictionary<char, float> widthDict);

                    if(float.TryParse(System.IO.File.ReadAllText(path), out float blendWidthOfSpace))
                    {
                        tfData.widthOfSpace = blendWidthOfSpace;
                        advanceToBlend = blendWidthOfSpace / advanceWidthOfSpace; 
                    }
                    tfData.advanceToBlend = advanceToBlend;

                    MakerToFontData newData = new MakerToFontData();
                    newData.advanceToBlend = tfData.advanceToBlend;
                    newData.widthOfSpace = tfData.widthOfSpace;
                    newData.xAdvances = widthDict;

                    focusException = true;
                    child = FontMakerReview.Init(this, newData);
                }
            }
            GUILayout.EndVertical();
            #endregion ============================================================
        }

        public static void SetEditorViewDir(Editor editor)
        {
            try
            {
                editor.OnInteractivePreviewGUI(new Rect(), GUIStyle.none);
                var setfield = editor.GetType().GetField("m_Settings", BindingFlags.NonPublic | BindingFlags.Instance);
                var settings = setfield.GetValue(editor);
                var pdirfield = settings.GetType().GetField("previewDir");
                pdirfield.SetValue(settings, new Vector2(16f, -8f));
                setfield.SetValue(editor, settings);
            }
            catch
            {
                Debug.LogWarning("Failed to set preview view direction.");
            }
        }

        private void OnDisable()
        {
            AssetDatabase.DeleteAsset(TEMPMODELPATH);

            FontMakerData.instance.position = this.position;

            if (previewEditor != null)
                DestroyImmediate(previewEditor);

            EditorApplication.UnlockReloadAssemblies(); 
        }


        static string GetCharacterSetFromTypeface(Typography.OpenFont.Typeface typeface, (int, int)[] characterSet)
        {
            List<uint> unicodes = new List<uint>();
            typeface.CollectUnicode(unicodes);
            
            var characters = unicodes.Distinct();
            
            var sb = new System.Text.StringBuilder();
            foreach (uint ui in characters)
            {
                foreach ((int start, int end) range in characterSet)
                {
                    if (ui >= range.start && ui <= range.end) 
                    {
                        try
                        {
                            char c = System.Convert.ToChar(ui);
                            if (char.IsControl(c) || char.IsWhiteSpace(c))
                                continue;

                            sb.Append(c);
                        }
                        catch { }
                        break; 
                    }
                }
            }
            string setString = sb.ToString();
            return setString;
        }

        void GetWidthsFromTypeface(Typography.OpenFont.Typeface typeface, string set, out float widthOfSpace, out Dictionary<char,float> widthDict)
        {
            Typography.TextLayout.GlyphLayout gl = new Typography.TextLayout.GlyphLayout();
            gl.Typeface = typeface;

            widthOfSpace = float.NaN;
            gl.Layout(new char[] { ' ' }, 0, 1);
            foreach (var item in gl.GetUnscaledGlyphPlanIter())
            {
                widthOfSpace = (float)item.AdvanceX;
            }

            widthDict = new Dictionary<char, float>();
            gl.Layout(set.ToCharArray(), 0, set.Length);
            int i = 0;
            foreach (var item in gl.GetUnscaledGlyphPlanIter())
            {
                widthDict.Add(set[i], (float)item.AdvanceX);
                i++;
            }
        }

        Typography.OpenFont.Typeface GetTypefaceFromFile(string path)
        {
            Typography.OpenFont.OpenFontReader openFontReader = new Typography.OpenFont.OpenFontReader();
            System.IO.Stream stream = new System.IO.FileStream(path, System.IO.FileMode.Open);
            Typography.OpenFont.Typeface typeface = openFontReader.Read(stream);
            stream.Close();
            return typeface;
        }

        static string WriteCharacterSetToFile(string set)
        {
            string path = FontMakerData.instance.TempPath;

            System.IO.File.WriteAllText(path, set);
            
            return path;
        }


        private static void GenerateTextFBX(Typography.OpenFont.Typeface typeface, bool preview, TextToFBXData tfData)
        {
            string blenderFilepath = FontMakerData.instance.BlenderInstallPath;
            string outputFilepath = System.IO.Path.GetFullPath(TEMPMODELPATH);

            string stringSet;
            if (preview)
                stringSet = tfData.previewString;
            else
                stringSet = GetCharacterSetFromTypeface(typeface, tfData.characterSet);

            tfData.setString = stringSet;

            string charSetPath = WriteCharacterSetToFile(stringSet);

#if TATTING_INTERNAL
            string genScriptFilepath = System.IO.Path.GetFullPath("Assets/com.bottinogames.tatting/Editor/FontMaker/Python/fontgen.py");
#else
            string genScriptFilepath = System.IO.Path.GetFullPath("Packages/com.bottinogames.tatting/Editor/FontMaker/Python/fontgen.py");
#endif
            var process = new Process();
            process.StartInfo.FileName = blenderFilepath;

            float bWidth = tfData.doBevel ? tfData.bevelWidth : 0f;
            float bRes = tfData.doBevel ? tfData.bevelResolution : 0f;

            process.StartInfo.Arguments = $"-b -P \"{genScriptFilepath}\" -- " +
                $"--output \"{outputFilepath}\" " +
                $"--font \"{tfData.fontFilePath}\" " +
                $"--char \"{charSetPath}\" " +
                $"--extrude {tfData.extrude} " +
                $"--bevel {bWidth} " +
                $"--resolution {tfData.resolution} " +
                $"--bevelres {bRes} " +
                $"--preview {preview}";

            Debug.Log(blenderFilepath);
            Debug.Log(process.StartInfo.Arguments);

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            process.StartInfo.RedirectStandardOutput = true;

            process.Start();

            Debug.Log(process.StandardOutput.ReadToEnd());

            process.WaitForExit();
        }

        internal class TextToFBXData
        {
            public string fontFilePath;

            public bool isPreview;
            public string previewString;

            public (int, int)[] characterSet;
            public string setString;

            public float extrude;
            public int resolution;
            public bool doBevel;
            public float bevelWidth;
            public int bevelResolution;

            public float widthOfSpace;
            public float advanceToBlend;
        }

        internal class MakerToFontData
        {
            public float advanceToBlend;
            public float widthOfSpace;
            public string modelPath;
            public Dictionary<char, float> xAdvances;
        }
#endif
    }
}
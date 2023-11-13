using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Tatting
{
#if UNITY_EDITOR_WIN
    internal class FontMakerReview : EditorWindow
    {
        const int ASSETSPERROW = 4;
        const float ASSETVIEWHEIGHT = 370;
        readonly Color HIGHLIGHTCOLOR = new Color(.8f, 1f, .8f);
        internal static FontMakerReview Init(FontMaker parent, FontMaker.MakerToFontData data)
        {
            FontMakerReview window = (FontMakerReview)EditorWindow.GetWindow(typeof(FontMakerReview), true, "Tatting Font Maker - Full Review");
            window.minSize = new Vector2(600, 800);
            window.maxSize = new Vector2(600, 800);

            window.parent = parent;

            window.data = data;

            AssetDatabase.ImportAsset(FontMaker.TEMPMODELPATH, ImportAssetOptions.ForceUpdate);
            Object[] objs = AssetDatabase.LoadAllAssetsAtPath(FontMaker.TEMPMODELPATH);

            window.meshes = new List<Mesh>();
            foreach (var obj in objs)
                if (obj is Mesh mesh)
                    window.meshes.Add(mesh);

            window.meshes.Sort((a, b) => ((byte)a.name[0]).CompareTo((byte)b.name[0]));

            if (window.previewEditor != null)
                DestroyImmediate(window.previewEditor);
            window.previewEditor = Editor.CreateEditor(window.meshes[0]);

            window.ShowUtility();
            return window;
        }


        FontMaker parent;
        FontMakerReviewSaveModal child;
        bool focusException = false;


        FontMaker.MakerToFontData data;
        List<Mesh> meshes;



        int _viewing;
        int Viewing
        {
            get { return _viewing; }
            set
            {
                value = Mathf.Min(Mathf.Max(value, 0), meshes.Count - 1);
                if (value == _viewing) return;
                _viewing = value;
                SetEditor(meshes[_viewing]);
            }
        }

        Editor previewEditor;

        Vector2 assetScrollView;

        private void OnFocus()
        {
            if (child)
            {
                child.Focus();
            }
        }

        private void OnLostFocus()
        {
            if (child == null)
            {
                if (!focusException)
                {
                    Focus();
                }

                focusException = false;
            }
        }

        private void OnDestroy()
        {
            parent.Focus();
        }

        internal void SetEditor(Mesh mesh)
        {
            if (previewEditor != null)
                DestroyImmediate(previewEditor);
            previewEditor = Editor.CreateEditor(mesh);
            FontMaker.SetEditorViewDir(previewEditor);
        }

        private void OnGUI()
        {
            bool scrollToCurrentView = false;
            if (Event.current.isKey && Event.current.type == EventType.KeyDown)
            {
                int start = Viewing;
                switch (Event.current.keyCode)
                {
                    case KeyCode.LeftArrow:
                    case KeyCode.A:
                        Viewing--;
                        break;

                    case KeyCode.RightArrow:
                    case KeyCode.D:
                        Viewing++;
                        break;

                    case KeyCode.UpArrow:
                    case KeyCode.W:
                        Viewing -= ASSETSPERROW;
                        break;

                    case KeyCode.DownArrow:
                    case KeyCode.S:
                        Viewing += ASSETSPERROW;
                        break;

                    default:
                        break;
                }

                if (Viewing != start)
                {
                    Event.current.Use();
                    Repaint();
                    scrollToCurrentView = true;
                }
            }

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("<"))
                    Viewing--;
                Viewing = Mathf.Clamp(EditorGUILayout.DelayedIntField(Viewing+1, GUILayout.Width(25))-1,0,meshes.Count-1);
                GUI.enabled = false;
                GUILayout.Label("/", GUILayout.Width(9));
                EditorGUILayout.DelayedIntField(meshes.Count, GUILayout.Width(25));
                GUI.enabled = true;
                if (GUILayout.Button(">"))
                    Viewing++;
            }
            GUILayout.EndHorizontal();
            Rect previewEditorRect;
            GUILayout.BeginVertical("HelpBox");
            {
                previewEditorRect = EditorGUILayout.GetControlRect(false, 350);
                GUILayout.Label($"Character: {meshes[Viewing].name} | Triangles: {meshes[Viewing].triangles.Length} | Vertices: {meshes[Viewing].vertices.Length}");
            }
            GUILayout.EndVertical();
            assetScrollView = GUILayout.BeginScrollView(assetScrollView, GUILayout.Height(ASSETVIEWHEIGHT));
            {
                GUILayout.BeginHorizontal();
                int rowNum = 0;
                for (int i = 0; i < meshes.Count; i++)
                {
                    GUILayout.FlexibleSpace();
                    if (Viewing == i)
                        GUI.color = HIGHLIGHTCOLOR;
                    if (GUILayout.Button("", GUILayout.Width(120), GUILayout.Height(120)))
                    {
                        Viewing = i;
                    }

                    Rect lastRect = GUILayoutUtility.GetLastRect();
                    Vector2 margins = Vector2.one * 10;
                    lastRect.position += margins;
                    lastRect.size -= margins * 2;
                    if (lastRect.yMax > assetScrollView.y && lastRect.yMin < assetScrollView.y + ASSETVIEWHEIGHT)
                    {
                        Texture2D preview = AssetPreview.GetAssetPreview(meshes[i]);
                        if (preview != null)
                            GUI.DrawTextureWithTexCoords(lastRect, preview, new Rect(0f, 0f, -1f, 1f), true); //flipped UV's cause unity is duuumb
                    } 
                    else if (Viewing == i && scrollToCurrentView)
                    {
                        Debug.Log(lastRect.yMin.ToString() + assetScrollView.y.ToString());
                        if (lastRect.yMin < assetScrollView.y)
                            assetScrollView.y = lastRect.yMin;
                        if (lastRect.yMax > assetScrollView.y + ASSETVIEWHEIGHT)
                            assetScrollView.y = lastRect.yMax - ASSETVIEWHEIGHT;
                    }
                    GUI.color = Color.white;

                    rowNum = (i % ASSETSPERROW) + 1;
                    if (rowNum == ASSETSPERROW)
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                    }
                }
                if (rowNum != ASSETSPERROW) 
                {
                    GUI.enabled = false;
                    GUI.color = Color.clear;
                    for (int i = rowNum; i < ASSETSPERROW; i++)
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Button("", GUILayout.Width(120), GUILayout.Height(120));
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUI.enabled = true;
                    GUI.color = Color.white;
                }
                else
                    GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel"))
                {
                    this.Close();
                }
                GUI.enabled = false;
                if (GUILayout.Button("Append to existing MeshFont"))
                {
                    focusException = true;
                    child = FontMakerReviewSaveModal.Init(this, true);
                }
                GUI.enabled = true;
                if (GUILayout.Button("Save as new MeshFont"))
                {
                    focusException = true;
                    child = FontMakerReviewSaveModal.Init(this, false);
                }
            }
            GUILayout.EndHorizontal();

            //draw interactive preview at the end to prevent it greedily consuming the scrollwheel event
            previewEditor.OnInteractivePreviewGUI(previewEditorRect, GUIStyle.none);
        }

        private void OnDisable()
        {
            if (previewEditor != null)
                DestroyImmediate(previewEditor);
        }


        internal void SaveNew(string assetPath, string modelPath)
        {
            data.modelPath = "Assets/" + modelPath;
            AssetDatabase.MoveAsset(FontMaker.TEMPMODELPATH, data.modelPath);
#if TATTING_INTERNAL
            string importSettingsPath = "Assets/com.bottinogames.tatting/meshfont.preset";
#else
            string importSettingsPath = "Packages/com.bottinogames.tatting/meshfont.preset";
#endif
            AssetDatabase.Refresh();
            var fontModelPreset = AssetDatabase.LoadAssetAtPath<UnityEditor.Presets.Preset>(importSettingsPath);
            var modelImporter = AssetImporter.GetAtPath(data.modelPath);

            fontModelPreset.ApplyTo(modelImporter);
            modelImporter.SaveAndReimport();

            MeshFont newFont = ScriptableObject.CreateInstance<MeshFont>();
            newFont.PopulateFromFontMaker(data.advanceToBlend, data.widthOfSpace, data.modelPath, data.xAdvances);
            AssetDatabase.CreateAsset(newFont, "Assets/" + assetPath);
            AssetDatabase.SaveAssets();

            FullClose(); 
        }

        internal void Append(MeshFont selected)
        {

        }

        internal void FullClose()
        {
            Close();
            parent.Close();
        }
    }


    internal class FontMakerReviewSaveModal : EditorWindow
    {
        static GUIContent errorIcon;
        static GUIContent okIcon;

        static GUIContent folderIcon;
        static GUIContent eyedropperIcon;
        static GUIContent refreshIcon;

        internal static FontMakerReviewSaveModal Init(FontMakerReview parent, bool append)
        {
            FontMakerReviewSaveModal window = (FontMakerReviewSaveModal)EditorWindow.GetWindow(typeof(FontMakerReviewSaveModal), true, append? "Tatting Font Maker - Save and Append Files" : "Tatting Font Maker - Save Files");
            window.minSize = new Vector2(900, 300);
            window.maxSize = new Vector2(900, 300);

            window.parent = parent;
            window.append = append;

            //Icon reference can be found at: https://github.com/halak/unity-editor-icons
            errorIcon = EditorGUIUtility.IconContent("TestFailed");
            okIcon = EditorGUIUtility.IconContent("TestPassed");

            folderIcon = EditorGUIUtility.IconContent("d_Project");
            refreshIcon = EditorGUIUtility.IconContent("d_Refresh@2x");
            eyedropperIcon = EditorGUIUtility.IconContent("d_eyeDropper.Large");

            window.ShowUtility();
            return window;
        }

        FontMakerReview parent;
        FontMakerReviewSaveModal child;
        bool append;

        int objectpickerID = int.MinValue;

        string _assetFolderPath = "";
        string AssetFolderPath
        {
            get { return _assetFolderPath; }
            set
            {
                _assetFolderPath = value;

                string fullPath = GetFullPath(_assetFolderPath);

                if (!System.IO.Directory.Exists(fullPath))
                {
                    validAssetPath = false;
                    assetFolderErrorMessage = "The directory does not exist";
                    return;
                }

                if (System.IO.File.Exists(fullPath))
                {
                    validAssetPath = false;
                    assetFolderErrorMessage = "The path is a file and not a directory";
                    return;
                }

                validAssetPath = true;
                return;
            }
        }
        string assetFolderErrorMessage;
        bool validAssetPath = false;

        string _assetFileName = "";
        string AssetFileName
        {
            get { return _assetFileName; }
            set
            {
                _assetFileName = value;

                if (string.IsNullOrEmpty(_assetFileName))
                {
                    validAssetFileName = false;
                    assetFileNameErrorMessage = $"Filename cannot be empty";
                    return;
                }

                string invalidChars = "";
                foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                {
                    if (_assetFileName.IndexOf(c) >= 0)
                        invalidChars += c;
                }

                if (!string.IsNullOrEmpty(invalidChars))
                {
                    validAssetFileName = false;
                    assetFileNameErrorMessage = $"Filename cannot contain certain characters ({invalidChars})";
                    return;
                }

                string fullpath = GetFullPath(AssetFolderPath, _assetFileName, ".asset");
                if (System.IO.File.Exists(fullpath))
                {
                    validAssetFileName = false;
                    assetFileNameErrorMessage = $"File already exists ({fullpath})";
                    return;
                }
                if (!seperateFBXName)
                {
                    string fbxPath = GetFullPath(AssetFolderPath, _assetFileName, ".fbx");
                    if (System.IO.File.Exists(fbxPath))
                    {
                        validAssetFileName = false;
                        assetFileNameErrorMessage = $"File already exists ({fbxPath})";
                        return;
                    }
                }

                validAssetFileName = true;
            }
        }
        string assetFileNameErrorMessage;
        bool validAssetFileName;

        bool seperateFBXName = false;

        string _fbxFileName = "";
        string FbxFileName
        {
            get { return _fbxFileName; }
            set
            {
                _fbxFileName = value;

                if (string.IsNullOrEmpty(_fbxFileName))
                {
                    validFbxFileName = false;
                    fbxFileNameErrorMessage = $"Filename cannot be empty";
                    return;
                }

                string invalidChars = "";
                foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                {
                    if (_fbxFileName.IndexOf(c) >= 0)
                        invalidChars += c;
                }

                if (!string.IsNullOrEmpty(invalidChars))
                {
                    validFbxFileName = false;
                    fbxFileNameErrorMessage = $"Filename cannot contain certain characters ({invalidChars})";
                    return;
                }

                string fullpath = GetFullPath(AssetFolderPath, _fbxFileName, ".fbx");
                if (System.IO.File.Exists(fullpath))
                {
                    validFbxFileName = false;
                    fbxFileNameErrorMessage = $"File already exists ({fullpath})";
                    return;
                }

                validFbxFileName = true;
            }
        }
        string fbxFileNameErrorMessage;
        bool validFbxFileName;
        
        public string GetFullPath(string relativeDirectory)
        {
            return System.IO.Path.Combine(Application.dataPath, relativeDirectory).Replace('\\', '/');
        }
        public string GetFullPath(string relativeDirectory, string relativeFile)
        {
            return System.IO.Path.Combine(Application.dataPath, relativeDirectory, relativeFile).Replace('\\', '/');
        }
        public string GetFullPath(string relativeDirectory, string relativeFile, string extension)
        {
            return System.IO.Path.Combine(Application.dataPath, relativeDirectory, relativeFile).Replace('\\', '/') + extension;
        }

        public string GetRelativePath(string relativeDirectory, string relativeFile, string extension)
        {
            return System.IO.Path.Combine(relativeDirectory, relativeFile).Replace('\\', '/') + extension;
        }

        private void OnLostFocus()
        {
            Focus();
        }

        private void OnDestroy()
        {
            parent.Focus();
        }

        private void OnGUI()
        {
            #region ============ DIRECTORY ============
            GUI.color = validAssetPath ? Color.green : Color.red;
            GUILayout.BeginVertical("HelpBox");
            {
                GUI.color = Color.white;
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Save Directory", EditorStyles.boldLabel, GUILayout.Height(20f));
                    if (!validAssetPath)
                        EditorGUILayout.HelpBox(assetFolderErrorMessage, MessageType.None);
                }
                GUILayout.EndHorizontal();
            
            
                GUI.color = validAssetPath ? Color.green : Color.red;
                GUILayout.BeginHorizontal("HelpBox");
                {
                    GUI.color = Color.white;
            
                    GUILayout.Label(validAssetPath ? okIcon : errorIcon, GUILayout.Width(20));
                    AssetFolderPath = GUILayout.TextField(AssetFolderPath, GUILayout.Width(this.position.width - 102), GUILayout.Height(20), GUILayout.ExpandWidth(false));
                    if (GUILayout.Button(folderIcon, GUILayout.Width(25), GUILayout.Height(20)))
                    {
                        string openPath;
                        if (System.IO.Directory.Exists(AssetFolderPath))
                            openPath = AssetFolderPath;
                        else
                        {
                            string p = "";
                            if (System.IO.Path.IsPathRooted(AssetFolderPath))
                                p = System.IO.Path.GetDirectoryName(AssetFolderPath);
                            if (System.IO.Directory.Exists(p))
                                openPath = p;
                            else
                                openPath = Application.dataPath;
                        }

                        string path;
                        if (!append)
                            path = EditorUtility.SaveFolderPanel("Save Font assets to directory", openPath, "MeshFonts");
                        else
                            path = EditorUtility.OpenFilePanel("Append new character to Font asset", openPath, ".asset");

                        if (path.IndexOf(Application.dataPath) == 0)
                        {
                            path = path.Substring(Application.dataPath.Length+1);
                            AssetFolderPath = path;
                        }
                    }
                    if (append) {
                        if (GUILayout.Button(eyedropperIcon, GUILayout.Width(25), GUILayout.Height(20)))
                        {
                            objectpickerID = EditorGUIUtility.GetControlID(99127, FocusType.Passive);
                            EditorGUIUtility.ShowObjectPicker<MeshFont> (null, false, string.Empty, objectpickerID);
                        } 
                    }
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.HelpBox(GetFullPath(AssetFolderPath), MessageType.None);
            }
            GUILayout.EndVertical();
            #endregion

            #region ============ ASSET ============
            GUI.color = validAssetFileName ? Color.green : Color.red;
            GUILayout.BeginVertical("HelpBox");
            {
                GUI.color = Color.white;
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Asset File Name", EditorStyles.boldLabel, GUILayout.Height(20f));
                    if (!validAssetFileName)
                        EditorGUILayout.HelpBox(assetFileNameErrorMessage, MessageType.None);
                }
                GUILayout.EndHorizontal();
            
                GUI.color = validAssetFileName ? Color.green : Color.red;
                GUILayout.BeginHorizontal("HelpBox");
                {
                    GUI.color = Color.white;
                    
                    GUILayout.Label(validAssetFileName ? okIcon : errorIcon, GUILayout.Width(20));
                    AssetFileName = GUILayout.TextField(AssetFileName, GUILayout.Width(this.position.width - 152), GUILayout.Height(20), GUILayout.ExpandWidth(false));
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.HelpBox(GetFullPath(AssetFolderPath, AssetFileName, ".asset"), MessageType.None);
                if (!seperateFBXName)
                    EditorGUILayout.HelpBox(GetFullPath(AssetFolderPath, AssetFileName, ".fbx"), MessageType.None);
            }
            GUILayout.EndVertical();
            #endregion

            #region ============  FBX  ============
            bool temp = seperateFBXName;
            seperateFBXName = GUILayout.Toggle(seperateFBXName, "Seperate filename for FBX");
            if(seperateFBXName != temp)
            {
                AssetFileName = AssetFileName;
                FbxFileName = FbxFileName;
            }

            if (seperateFBXName)
            {
                GUI.color = validFbxFileName ? Color.green : Color.red;
                GUILayout.BeginVertical("HelpBox");
                {
                    GUI.color = Color.white;
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Asset File Name", EditorStyles.boldLabel, GUILayout.Height(20f));
                        if (!validFbxFileName)
                            EditorGUILayout.HelpBox(fbxFileNameErrorMessage, MessageType.None);
                    }
                    GUILayout.EndHorizontal();

                    GUI.color = validFbxFileName ? Color.green : Color.red;
                    GUILayout.BeginHorizontal("HelpBox");   
                    {
                        GUI.color = Color.white;

                        GUILayout.Label(validFbxFileName ? okIcon : errorIcon, GUILayout.Width(20));
                        FbxFileName = GUILayout.TextField(FbxFileName, GUILayout.Width(this.position.width - 152), GUILayout.Height(20), GUILayout.ExpandWidth(false));
                    }
                    GUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox(GetFullPath(AssetFolderPath, FbxFileName, ".fbx"), MessageType.None);
                }
                GUILayout.EndVertical();
            }
            #endregion
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Save"))
                {
                    Close();
                    parent.SaveNew(GetRelativePath(AssetFolderPath, AssetFileName, ".asset"), GetRelativePath(AssetFolderPath, seperateFBXName ? FbxFileName : AssetFileName, ".fbx"));
                }

                if (GUILayout.Button("Cancel"))
                { 
                    Close();
                }

            }
            GUILayout.EndHorizontal();
            
            float y = GUILayoutUtility.GetLastRect().yMax;
            if (y > 10f)
            {
                maxSize = new Vector2(maxSize.x, y+4);
                minSize = new Vector2(minSize.x, y+4);
            }
        }

    }
#endif
}
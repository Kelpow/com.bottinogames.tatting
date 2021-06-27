using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tatting
{

    [System.Serializable]
    [CreateAssetMenu(fileName = "New Tatting Mesh Font", menuName = "Tatting Mesh Font", order = 540)]
    public class MeshFont : ScriptableObject
    {
#if UNITY_EDITOR
        public MeshFontSetupWizard setupWizard;
#endif


        [SerializeField] private bool _caseless = false;
        public bool caseless
        {
            get { return _caseless; }
            set
            {
                if (value != _caseless)
                {
                    isDirty = true;
                    _caseless = value;
                }
            }
        }

        [SerializeField] public float distanceBetweenCharacters = 1f;
        [SerializeField] public Vector3 characterRotation;




        public const int UPPERCASE_START = 26;
        public const int UPPERCASE_END = 51;

        public static bool isUpperDefault(int i) { return UPPERCASE_START <= i && i <= UPPERCASE_END; }

        public readonly char[] defaultCharacters = {
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ' '
    };
        public Mesh[] defaultCharactersMeshes = new Mesh[63];


        public List<char> extraCharacters = new List<char>() { '-', ',', '.', '!', '?' };
        public List<Mesh> extraMeshes = new List<Mesh>() { null, null, null, null, null };

        public void RemoveExtra(int i)
        {
            extraCharacters.RemoveAt(i);
            extraMeshes.RemoveAt(i);
        }
        public void AddExtra(char c)
        {
            extraCharacters.Add(c);
            extraMeshes.Add(null);
        }

        public void ShiftExtra(int i, int dir)
        {
            int target = i + dir;
            if (target >= 0 && target < extraCharacters.Count)
            {
                char tempC = extraCharacters[target];
                Mesh tempM = extraMeshes[target];
                extraCharacters[target] = extraCharacters[i];
                extraMeshes[target] = extraMeshes[i];

                extraCharacters[i] = tempC;
                extraMeshes[i] = tempM;
            }
        }

        public Mesh defaultMesh;


        public bool isDirty;

        private Dictionary<char, Mesh> _meshCharacters = null;
        public Dictionary<char, Mesh> meshCharacters
        {
            get
            {
                if (_meshCharacters == null || isDirty)
                {
                    _meshCharacters = ToDictionary();
                    isDirty = false;
                }
                return _meshCharacters;
            }
        }

        public Dictionary<char, Mesh> ToDictionary()
        {
            Dictionary<char, Mesh> newDictionary = new Dictionary<char, Mesh>();

            for (int i = 0; i < defaultCharacters.Length; i++)
            {
                if (caseless && isUpperDefault(i))
                {
                    newDictionary.Add(defaultCharacters[i], defaultCharactersMeshes[i - UPPERCASE_START]);
                    continue;
                }

                if (defaultCharactersMeshes[i] == null)
                    newDictionary.Add(defaultCharacters[i], defaultMesh);
                else
                {
                    newDictionary.Add(defaultCharacters[i], defaultCharactersMeshes[i]);
                }
            }

            for (int i = 0; i < extraCharacters.Count; i++)
            {
                if (extraMeshes[i] == null)
                    newDictionary.Add(extraCharacters[i], defaultMesh);
                else
                {
                    newDictionary.Add(extraCharacters[i], extraMeshes[i]);

                    characterBounds.min = Vector3.Min(characterBounds.min, extraMeshes[i].bounds.min);
                    characterBounds.max = Vector3.Max(characterBounds.max, extraMeshes[i].bounds.max);
                }
            }

            return newDictionary;
        }


        [SerializeField] public Bounds characterBounds;

        public void SetAutomaticCharacterBounds()
        {
            bool boundsSet = false;

            foreach (Mesh m in defaultCharactersMeshes)
            {
                if (m == null)
                    continue;

                if (!boundsSet)
                {
                    characterBounds = m.bounds;
                    boundsSet = true;
                }
                else
                {
                    characterBounds.min = Vector3.Min(characterBounds.min, m.bounds.min);
                    characterBounds.max = Vector3.Max(characterBounds.max, m.bounds.max);
                }
            }
            foreach (Mesh m in extraMeshes)
            {
                if (m == null)
                    continue;

                if (!boundsSet)
                {
                    characterBounds = m.bounds;
                    boundsSet = true;
                }
                else
                {
                    characterBounds.min = Vector3.Min(characterBounds.min, m.bounds.min);
                    characterBounds.max = Vector3.Max(characterBounds.max, m.bounds.max);
                }
            }
        }

    }









    // ||||||||||||||||||||| E D I T O R |||||||||||||||||||||

#if UNITY_EDITOR
    [CustomEditor(typeof(MeshFont))]
    public class TattingFontInspector : Editor
    {

        string charToAdd = "";


        public override void OnInspectorGUI()
        {
            MeshFont font = (MeshFont)target;


            GUILayout.Space(15);
            GUI.enabled = !font.setupWizard;
            if (GUILayout.Button("Launch Setup Wizard"))
                font.setupWizard = MeshFontSetupWizard.NewWizard(font, this);

            if (font.setupWizard)
            {
                GUILayout.Space(15);
                GUILayout.Label("Waiting for Setup Wizard...", EditorStyles.boldLabel);

                return;
            }




            //===== Settings =====

            GUILayout.Space(15);
            GUILayout.Label("Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            font.caseless = EditorGUILayout.Toggle("Caseless", font.caseless);
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(font);

            EditorGUI.BeginChangeCheck();
            font.distanceBetweenCharacters = EditorGUILayout.FloatField("Character separation", font.distanceBetweenCharacters);
            font.characterRotation = EditorGUILayout.Vector3Field("Character rotation", font.characterRotation);

            font.characterBounds = EditorGUILayout.BoundsField("Character bounds", font.characterBounds);

            if (GUILayout.Button("Set bounds automatically"))
                font.SetAutomaticCharacterBounds();


            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(font);
                foreach (MeshTextRenderer rend in Resources.FindObjectsOfTypeAll<MeshTextRenderer>())
                    if (!EditorUtility.IsPersistent(rend.transform.root.gameObject))
                        rend.UpdateAllCharacterPositions();
            }





            //===== Default Characters =====
            GUILayout.Space(15);
            GUILayout.Label("Default Characters", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            for (int i = 0; i < font.defaultCharacters.Length; i++)
            {
                DrawCharacterMeshCombo(true, i, font);
            }


            //===== Non-Default Characters =====
            GUILayout.Space(15);
            GUILayout.Label("Non-Default Characters", EditorStyles.boldLabel);

            for (int i = 0; i < font.extraCharacters.Count; i++)
            {
                DrawCharacterMeshCombo(false, i, font);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(font);
                font.isDirty = true;

                foreach (MeshTextRenderer rend in Resources.FindObjectsOfTypeAll<MeshTextRenderer>())
                    if (!EditorUtility.IsPersistent(rend.transform.root.gameObject))
                        rend.SendMessage("RefreshCharacters");
            }


            GUI.enabled = true;
            GUILayout.Space(5);

            EditorGUI.BeginChangeCheck();

            GUILayout.BeginHorizontal();
            {

                string newCharToAdd = GUILayout.TextField(charToAdd, GUILayout.Width(20));

                int removal = newCharToAdd.LastIndexOfAny(font.defaultCharacters);
                if (removal == -1)
                {
                    if (newCharToAdd.Length > 0)
                    {
                        if (!font.extraCharacters.Contains(newCharToAdd[newCharToAdd.Length - 1]))
                            charToAdd = newCharToAdd[newCharToAdd.Length - 1].ToString();
                    }
                    else
                        charToAdd = "";
                }

                GUI.enabled = charToAdd.Length != 0;
                if (GUILayout.Button("Add Extra Character"))
                {
                    font.AddExtra(charToAdd[0]);
                }
            }
            GUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(font);
            }
        }



        private void DrawCharacterMeshCombo(bool isDefault, int i, MeshFont font)
        {
            if (font.caseless && isDefault)
                if (MeshFont.isUpperDefault(i))
                    return;

            char c = isDefault ?
                font.defaultCharacters[i] :
                font.extraCharacters[i];

            Mesh m = isDefault ?
                font.defaultCharactersMeshes[i] :
                font.extraMeshes[i];

            bool isSpace = (c == ' ');

            GUILayout.BeginHorizontal();
            {

                GUI.enabled = false;
                GUILayout.TextField(c.ToString(), GUILayout.Width(20));


                GUI.enabled = !isSpace;
                if (isDefault)
                {
                    font.defaultCharactersMeshes[i] = EditorGUILayout.ObjectField(m, typeof(Mesh), allowSceneObjects: false) as Mesh;
                }
                else
                {
                    font.extraMeshes[i] = EditorGUILayout.ObjectField(m, typeof(Mesh), allowSceneObjects: false) as Mesh;
                }


                if (!isDefault)
                {
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        font.RemoveExtra(i);
                    }

                    GUI.enabled = i != 0;
                    if (GUILayout.Button("▲", GUILayout.Width(20)))
                    {
                        font.ShiftExtra(i, -1);
                    }

                    GUI.enabled = i != font.extraCharacters.Count - 1;
                    if (GUILayout.Button("▼", GUILayout.Width(20)))
                    {
                        font.ShiftExtra(i, 1);
                    }
                }
            }
            GUILayout.EndHorizontal();

            if (isDefault)
            {
                if (i == MeshFont.UPPERCASE_START - 1 || i == MeshFont.UPPERCASE_END)
                    GUILayout.Space(8);
            }


        }
    }




    public class MeshFontSetupWizard : EditorWindow
    {
        public static MeshFontSetupWizard NewWizard(MeshFont font, TattingFontInspector inspector)
        {
            MeshFontSetupWizard newWizard = EditorWindow.CreateInstance<MeshFontSetupWizard>();
            newWizard.SetSize(new Vector2(300f, 85f));
            newWizard.font = font;
            newWizard.titleContent = new GUIContent("Tatting Setup Wizard: " + font.name);
            newWizard.inspector = inspector;
            newWizard.ShowUtility();
            return newWizard;
        }

        public void OnLostFocus() { Focus(); }
        public void OnDisable() { inspector.Repaint(); }

        public void SetSize(Vector2 size) { this.minSize = size; this.maxSize = size; }

        enum States
        {
            Start,
            Automatic,
            Manual,
            Ending
        }


        MeshFont font;
        TattingFontInspector inspector;

        States currentState = States.Start;

        //Automatic
        bool importAsCaseless = false;
        string path = "";
        List<Mesh> loadedMeshes = null;

        List<Mesh> importedMeshes;

        public void OnGUI()
        {
            GUILayout.Space(4);

            switch (currentState)
            {
                case States.Start:

                    GUI.enabled = true;
                    if (GUILayout.Button("Automatic Setup", GUILayout.Height(55)))
                        currentState = States.Automatic;

                    break;

                case States.Automatic:
                    if (loadedMeshes == null)
                    {
                        importAsCaseless = EditorGUILayout.Toggle("Import as caseless", importAsCaseless);

                        if (GUILayout.Button("Open 3D object file", GUILayout.Height(55)))
                        {
                            string fontPath = AssetDatabase.GetAssetPath(font);
                            path = EditorUtility.OpenFilePanel("Open 3D object file", fontPath.Remove(fontPath.LastIndexOf('/')), "obj,fbx,blend");

                            if (path != "")
                            {
                                font.caseless = importAsCaseless;

                                string relativePath = "Assets" + path.Replace(Application.dataPath, "");
                                Object[] objects = AssetDatabase.LoadAllAssetsAtPath(relativePath);
                                loadedMeshes = new List<Mesh>();
                                for (int i = 0; i < objects.Length; i++)
                                {
                                    Mesh mesh = objects[i] as Mesh;
                                    if (mesh)
                                    {
                                        loadedMeshes.Add(mesh);
                                    }
                                }

                                foreach (Mesh mesh in loadedMeshes)
                                {
                                    if (mesh.name.Length > 1)
                                        continue;

                                    for (int i = 0; i < font.defaultCharacters.Length; i++)
                                    {
                                        if (importAsCaseless && MeshFont.isUpperDefault(i))
                                            continue;

                                        if ((importAsCaseless ? mesh.name.ToLower()[0] : mesh.name[0]) == font.defaultCharacters[i])
                                        {
                                            font.defaultCharactersMeshes[i] = mesh;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        this.Close();
                    }
                    break;
            }
        }
    }
#endif

}
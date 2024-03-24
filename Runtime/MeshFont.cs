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

        private static CharacterInfo _zeroWidthCharacter;
        public static CharacterInfo ZeroWidthCharacter
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

        private CharacterInfo _whitespaceCharacter;
        private CharacterInfo WhitespaceCharacter
        {
            get
            {
                if (_whitespaceCharacter != null)
                {
                    _whitespaceCharacter.width = whitespaceWidth;
                    _whitespaceCharacter.mesh = CharacterInfo.emptyMesh;
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
                if (characterDictionary.Count <= 0)
                {
                    return ZeroWidthCharacter;
                }
                if (_fallbackCharacter != null && _fallbackCharacter.mesh != null)
                    return _fallbackCharacter;

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

        public float unitScale = .001f;

        [SerializeField] private List<CharacterInfo> _dictionarySerializationHelper = new List<CharacterInfo>();
        public Dictionary<char, CharacterInfo> characterDictionary = new Dictionary<char, CharacterInfo>();


        public CharacterInfo GetCharacterInfo(char c)
        {
            if (characterDictionary.TryGetValue(c, out var info))
            {
                return info;
            }
            else if (char.IsWhiteSpace(c))
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
            public static Mesh emptyMesh { get { if (_emptyMesh == null) { _emptyMesh = new Mesh(); } return _emptyMesh; } }

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


#if UNITY_EDITOR_WIN

        public void PopulateFromFontMaker(float advanceToBlend, float widthOfSpace, string modelPath, Dictionary<char, float> xAdvances)
        {
            unitScale = advanceToBlend;
            whitespaceWidth = widthOfSpace;

            characterDictionary = new Dictionary<char, CharacterInfo>();

            var objs = AssetDatabase.LoadAllAssetsAtPath(modelPath);
            
            foreach (var o in objs)
            {
                if(o is Mesh mesh)
                {
                    char c = mesh.name[0];
                    if (characterDictionary.ContainsKey(c))
                        continue;
                    if (!xAdvances.ContainsKey(c))
                    {
                        Debug.Log("Couldn't find advance for: " + c);
                        continue;
                    }
                    float advance = xAdvances[c];
                    CharacterInfo info = new CharacterInfo(c);
                    info.mesh = mesh;
                    info.width = advance * advanceToBlend;

                    characterDictionary.Add(c, info);
                }
            }
            

        }

        [ContextMenu("Convert To Edge Font")]
        public void ConverToEdge()
        {
            string path = AssetDatabase.GetAssetPath(this);

            Dictionary<char, MeshFont.CharacterInfo> newCD = new Dictionary<char, MeshFont.CharacterInfo>();
            foreach (var entry in this.characterDictionary)
            {
                Mesh origin = entry.Value.mesh;
                if (origin == null)
                    continue;

                Mesh wire = MakeNewLineMesh(origin);
                AssetDatabase.AddObjectToAsset(wire, path);

                var ci = new MeshFont.CharacterInfo(entry.Key);
                ci.width = entry.Value.width;
                ci.mesh = wire;
                newCD.Add(entry.Key, ci);
            }
            this.characterDictionary = newCD;

            AssetDatabase.SaveAssets();

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);


            // functions copied from Willow.Library.Utilities, dependancies are stinky

            void MakeLineMesh(Mesh basemesh, Mesh outmesh)
            {
                HashSet<(int a, int b)> edges = new HashSet<(int a, int b)>();
                HashSet<(int a, int b)> doubled = new HashSet<(int a, int b)>();
                var triangles = basemesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int a = triangles[i];
                    int b = triangles[i + 1];
                    int c = triangles[i + 2];

                    if (edges.Contains((b, a)) || edges.Contains((a, b)))
                        doubled.Add((a, b));
                    else
                        edges.Add((a, b));

                    if (edges.Contains((c, b)) || edges.Contains((b, c)))
                        doubled.Add((b, c));
                    else
                        edges.Add((b, c));

                    if (edges.Contains((a, c)) || edges.Contains((c, a)))
                        doubled.Add((c, a));
                    else
                        edges.Add((c, a));
                }

                List<int> newEdges = new List<int>();
                foreach (var edge in edges)
                {
                    if (doubled.Contains((edge.a, edge.b)) || doubled.Contains((edge.b, edge.a)))
                        continue;

                    newEdges.Add(edge.a);
                    newEdges.Add(edge.b);
                }
                outmesh.SetTriangles(new int[0], 0);
                outmesh.SetVertices(basemesh.vertices);
                outmesh.SetNormals(basemesh.normals);
                outmesh.SetColors(basemesh.colors);
                outmesh.SetUVs(0, basemesh.uv);
                outmesh.SetIndices(newEdges, MeshTopology.Lines, 0);
            }

            Mesh MakeNewLineMesh(Mesh basemesh)
            {
                if (basemesh == null)
                    return null;

                Mesh newMesh = new Mesh();
                newMesh.name = basemesh.name + "_EDGE";

                MakeLineMesh(basemesh, newMesh);
                return newMesh;
            }
        }
#endif
    }
}
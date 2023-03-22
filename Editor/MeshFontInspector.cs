using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace Tatting
{
    [CustomEditor(typeof(MeshFont))]
    public class TattingFontInspector : Editor
    {
        new MeshFont target;

        int currentTab;
        readonly string[] tabs = new string[] { "Details", "Glyphs" };

        string[] statStrings;

        float whitespaceWidth;
        float lineSpacing;

        bool changed = false; 

        private void OnEnable()
        {
            target = (MeshFont)base.target;

            // gather stats
            Bounds glyphBounds = new Bounds();
            foreach (var kvp in target.characterDictionary)
                glyphBounds.Encapsulate(kvp.Value.mesh.bounds);

            // populate stats
            statStrings = new string[]
            {
                "Glyph count: " + target.characterDictionary.Count,
                "Glyph bounds: \n" +
                "  Min: " + glyphBounds.min.ToString() + "   Max: " + glyphBounds.max.ToString() + "\n" +
                "  Center: " + glyphBounds.center.ToString() + "   Extents: " + glyphBounds.center.ToString()
            };

            PopulateSettings();
        }

        private void PopulateSettings()
        {
            whitespaceWidth = target.whitespaceWidth;
            lineSpacing = target.lineSpacing;
        }

        private void ApplySettings()
        {
            Undo.RecordObject(target, "MeshFont Changes");
            target.whitespaceWidth = whitespaceWidth;
            target.lineSpacing = lineSpacing;
        }

        public override void OnInspectorGUI()
        {
            currentTab = GUILayout.Toolbar(currentTab, tabs);
            switch (currentTab)
            {
                case 0:
                    DrawDetailsTab();
                    break;
                case 1:
                    DrawGlyphsTab();
                    break;
                default:
                    break;
            }
        }

        public void DrawDetailsTab()
        {
            // STATS
            GUILayout.Label("Stats", EditorStyles.boldLabel);
            GUILayout.BeginVertical("HelpBox");
            {
                foreach (string stat in statStrings)
                {
                    GUILayout.Label(stat);
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);



            EditorGUI.BeginChangeCheck();

            // SETTINGS
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            GUILayout.BeginVertical("HelpBox");
            {
                whitespaceWidth = EditorGUILayout.FloatField("Whitespace width", whitespaceWidth);
                lineSpacing = EditorGUILayout.FloatField("Line spacing", lineSpacing);
            }

            changed |= EditorGUI.EndChangeCheck();

            // we do some list-based shenanigans for serialize/deserialize, so it's better to not re-serialize on every single change
            GUI.enabled = changed;
            GUILayout.BeginHorizontal("HelpBox");
            {
                if (GUILayout.Button("Save"))
                {
                    ApplySettings();
                    changed = false;
                }
                if (GUILayout.Button("Cancel"))
                {   
                    PopulateSettings();
                    changed = false;
                }
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            GUILayout.EndVertical();
        }

        public void DrawGlyphsTab()
        {
            GUILayout.Label("¯\\_(ツ)_/¯");
        }
    }

}


/*
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
*/

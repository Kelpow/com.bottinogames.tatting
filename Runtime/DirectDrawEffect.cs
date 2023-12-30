using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tatting
{
    public delegate void DirectDrawEffectDelegate(ref MaterialPropertyBlock materialPropertyBlock, int i, char c);

    public abstract class DirectDrawEffect : MonoBehaviour
    {
        [HideInInspector]
        public bool startOnAwake;

        private DirectDrawEffectDelegate del;

        protected virtual void Awake()
        {
            del = new DirectDrawEffectDelegate(TextEffect);

            if (startOnAwake)
                Activate();
        }

        public void Activate()
        {
            MeshText text = GetComponent<MeshText>();
            if (text)
            {
                if (!text.directDrawEffects.Contains(del))
                    text.directDrawEffects.Add(del);
            }
        }

        public void Deactivate()
        {
            MeshText text = GetComponent<MeshText>();
            if (text)
            {
                if (text.directDrawEffects.Contains(del))
                    text.directDrawEffects.Remove(del);
            }
        }


        protected abstract void TextEffect(ref MaterialPropertyBlock materialPropertyBlock, int i, char c);

        



#if UNITY_EDITOR
        [CustomEditor(typeof(DirectDrawEffect), true)]
        public class TattingEffectInspector : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                DirectDrawEffect effect = (DirectDrawEffect)target;

                Undo.RecordObject(effect, "Inspector Change");

                GUILayout.Space(4);

                GUILayout.BeginVertical("HelpBox");
                {

                    effect.startOnAwake = EditorGUILayout.Toggle("Start on Awake", effect.startOnAwake);

                    GUILayout.Space(4);

                    GUI.enabled = Application.isPlaying;
                    if (GUILayout.Button("Activate"))
                        ((DirectDrawEffect)target).Activate();

                    if (GUILayout.Button("Deactivate"))
                        ((DirectDrawEffect)target).Deactivate();
                }
                GUILayout.EndVertical();
            }
        }
#endif
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tatting
{
    public delegate TRS MeshTextEffectDelegate(Vector2 head, int index);

    public abstract class MeshTextEffect : MonoBehaviour
    {
        [HideInInspector]
        public bool startOnAwake;

        private MeshTextEffectDelegate del;

        private void Awake()
        {
            del = new MeshTextEffectDelegate(TextEffect);

            if (startOnAwake)
                Activate();

        }

        public void Activate()
        {
            MeshText text = GetComponent<MeshText>();
            if (text)
            {
                if (!text.effects.Contains(del))
                    text.effects.Add(del);
            }
        }

        public void Deactivate()
        {
            MeshText text = GetComponent<MeshText>();
            if (text)
            {
                if (text.effects.Contains(del))
                    text.effects.Remove(del);
            }
        }


        protected abstract TRS TextEffect(Vector2 head, int index);

        



#if UNITY_EDITOR
        [CustomEditor(typeof(MeshTextEffect), true)]
        public class TattingEffectInspector : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                MeshTextEffect effect = (MeshTextEffect)target;

                GUILayout.Space(4);

                GUILayout.BeginVertical("HelpBox");
                {

                    effect.startOnAwake = EditorGUILayout.Toggle("Start on Awake", effect.startOnAwake);

                    GUILayout.Space(4);

                    GUI.enabled = Application.isPlaying;
                    if (GUILayout.Button("Activate"))
                        ((MeshTextEffect)target).Activate();

                    if (GUILayout.Button("Deactivate"))
                        ((MeshTextEffect)target).Deactivate();
                }
                GUILayout.EndVertical();
            }
        }
#endif
    }
}

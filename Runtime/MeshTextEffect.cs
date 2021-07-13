using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tatting {

    public delegate void MeshTextEffectDelegate(int i, ref Vector3 offset, ref Vector3 rotationaloffset);

    public abstract class MeshTextEffect : MonoBehaviour
    {

        [HideInInspector]
        public bool startOnAwake;

        private void Awake()
        {
            if (startOnAwake)
                Activate();
        }

        public void Activate()
        {
            MeshTextRenderer rend = GetComponent<MeshTextRenderer>();
            if (rend)
            {
                rend.textEffects -= TextEffect;
                rend.textEffects += TextEffect;
            }
        }

        public void Deactivate()
        {
            MeshTextRenderer rend = GetComponent<MeshTextRenderer>();
            if (rend)
                rend.textEffects -= TextEffect;
        }

        protected abstract void TextEffect(int i, ref Vector3 offset, ref Vector3 rotationaloffset);
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MeshTextEffect), true)]
    public class TattingEffectInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            MeshTextEffect effect = (MeshTextEffect)target;

            GUILayout.Space(4);

            effect.startOnAwake = EditorGUILayout.Toggle("Start on Awake",effect.startOnAwake);

            GUILayout.Space(8);

            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Activate"))
                ((MeshTextEffect)target).Activate();

            if (GUILayout.Button("Deactivate"))
                ((MeshTextEffect)target).Deactivate();
        }
    }
#endif
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tatting;


[RequireComponent(typeof(MeshText))]
public class TattingSlider : MonoBehaviour
{
    MeshText text;

    public int max;
    public int value;

    [SerializeField] private char onCharacter;
    [SerializeField] private char offCharacter;

    string[] displayStrings;

    private void Start()
    {
        text = GetComponent<MeshText>();
        value = Mathf.Clamp(value, 0, max);
        displayStrings = new string[max+1];

        for (int i = 0; i < max+1; i++)
        {
            for (int o = 0; o < i; o++)
            {
                displayStrings[i] += onCharacter;
            }
            for (int o = i; o < max; o++)
            {
                displayStrings[i] += offCharacter;
            }
        }

        SetValue(value);
    }

    public UnityEngine.Events.UnityEvent<int,int> valueChanged;

    public void SetValue(int value)
    {
        this.value = Mathf.Clamp(value, 0, max);
        text.Text = displayStrings[this.value];
        if (valueChanged != null)
            valueChanged.Invoke(max, value);
    }

    [ContextMenu("Increment")]
    public void Increment()
    {
        SetValue(value + 1);
    }

    [ContextMenu("Decrement")]
    public void Decrement()
    {
        SetValue(value - 1);
    }

    [RequireComponent(typeof(TattingSlider))]
    public class Behaviour : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<TattingSlider>().valueChanged.AddListener(OnValueChange);
        }

        public virtual void OnValueChange(int max, int value) {}
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tatting;


[RequireComponent(typeof(MeshText))]
public class TattingSlider : MonoBehaviour
{
    MeshText text;
    [Min(0)]
    public int value;

    public string[] displayStrings;
    private int max { get { return displayStrings.Length - 1; } }

    private void Start()
    {
        text = GetComponent<MeshText>();

        value = Mathf.Clamp(value, 0, max);


        SetValue(value);
    }

    public System.Action<int,int> valueChanged;

    public void SetValue(int value)
    {
        this.value = Mathf.Clamp(value, 0, max);
        text.Text = displayStrings[this.value];
        if (valueChanged != null)
            valueChanged.Invoke(max, this.value);
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
        protected TattingSlider slider;

        private void OnEnable()
        {
            slider = GetComponent<TattingSlider>();
            slider.valueChanged  += OnValueChange;
        }

        private void OnDisable()
        {
            slider.valueChanged -= OnValueChange;
        }

        public virtual void OnValueChange(int max, int value) {}
    }
}

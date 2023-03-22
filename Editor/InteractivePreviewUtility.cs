using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public class InteractivePreviewUtility
{
    private Object target;
    private Editor editor;
    
    
    public Object Target
    {
        get { return target; }
        set
        {
            if (value == target)
                return;
            if (target != null)
                Clear();

            Setup(value);
        }
    }

    public void Setup(Object target)
    {
        this.target = target;
        if (editor != null)
            Object.DestroyImmediate(editor);
        editor = Editor.CreateEditor(target);
    }

    public void Clear()
    {
        if (editor != null)
            Object.DestroyImmediate(editor);
    }

    InteractivePreviewUtility(Object target)
    {
        Setup(target);
    }

    ~InteractivePreviewUtility() { Clear(); Debug.Log("IPU Deconstructed"); }





    public bool TrySet3DViewDir(Vector2 dir)
    {
        try
        {
            var setfield = editor.GetType().GetField("m_Settings", BindingFlags.NonPublic | BindingFlags.Instance);
            var settings = setfield.GetValue(editor);
            if(settings == null) //settings isn't initialized until the 
                editor.OnInteractivePreviewGUI(new Rect(), GUIStyle.none);
            settings = setfield.GetValue(editor);
            var pdirfield = settings.GetType().GetField("previewDir");
            pdirfield.SetValue(settings, dir);
            setfield.SetValue(editor, settings);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TestingGrounds : EditorWindow
{
    [MenuItem("TESTING/GROUNDS")]
    static void Init()
    {

        TestingGrounds window = EditorWindow.CreateInstance(typeof(TestingGrounds)) as TestingGrounds; //(FontMaker)EditorWindow.GetWindow(typeof(FontMaker));
        window.minSize = new Vector2(1024, 1024);
        window.position = Tatting.FontMakerData.instance.position;
        window.Show();
    }

    Rect windowRect = new Rect(20, 20, 120, 50);

    private void OnGUI()
    {
        windowRect = GUI.Window(0, windowRect, DoMyWindow, "My Window");
    }

    void DoMyWindow(int windowID)
    {
        if (GUI.Button(new Rect(10, 20, 100, 20), "Hello World"))
        {

        }
    }
}

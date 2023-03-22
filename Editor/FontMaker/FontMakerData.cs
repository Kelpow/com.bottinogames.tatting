#if UNITY_EDITOR_WIN
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if UNITY_2022_1_OR_NEWER
using Microsoft.Win32.Registry;
#else
using Microsoft.Win32;
#endif
namespace Tatting
{
    [FilePath("com.bottinogames.tatting/prefs.cfg", FilePathAttribute.Location.PreferencesFolder)]
    public class FontMakerData : ScriptableSingleton<FontMakerData>
    {
        [SerializeField] private string blenderInstallPath = "";
        public string BlenderInstallPath
        {
            get { return blenderInstallPath; }
            set
            {
                blenderInstallPath = value;
                ValidateBlenderInstallPath();
            }
        }
        public bool validBlenderInstall = false;
        public void ValidateBlenderInstallPath()
        {
            try
            {
                validBlenderInstall = System.IO.Path.GetFileName(blenderInstallPath).ToLower() == "blender.exe" && System.IO.File.Exists(blenderInstallPath);
            }
            catch
            {
                validBlenderInstall = false;
            }
        }



        [SerializeField] private string tempPath = "";
        public string TempPath
        {
            get
            {
                if (tempPath == "")
                {
                    tempPath = System.IO.Path.GetFullPath(FileUtil.GetUniqueTempPathInProject());
                    Debug.Log("TempPath set to " + tempPath);
                }
                return tempPath;
            }
        }


        public Rect position = new Rect(50,70,600,600);

        public bool FindBlenderInRegistry()
        {
            RegistryKey classesRoot = Registry.ClassesRoot;
            RegistryKey fileKey = classesRoot.OpenSubKey("blendfile\\shell\\open\\command");
            if (fileKey != null)
            {
                string value = fileKey.GetValue(string.Empty).ToString();
                int start = value.IndexOf('\"') + 1;
                int end = value.IndexOf('\"', start);
                if (start != 0 && end != -1)
                {
                    string path = value.Substring(start, end - start).Replace("blender-launcher.exe", "blender.exe");
                    if (System.IO.File.Exists(path))
                    {
                        BlenderInstallPath = path;
                        return true;
                    }
                }
            }
            return false;
        }
    }



}
#endif
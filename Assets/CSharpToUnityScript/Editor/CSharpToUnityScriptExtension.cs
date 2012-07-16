
using UnityEngine;
using UnityEditor;
using System.Collections; // Stack
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO; // StreamReader/Writer


public class CSharpToUnityScriptExtension : EditorWindow {

    public struct Script {
        // relative path
        public string relativePath;
        public string text;
        public string name;

        public Script (string p_relativePath, string p_text) {
            text = p_text;
            relativePath = p_relativePath.Replace (".cs", "").Replace ("\\", "/");

            int lastSlashIndex = relativePath.LastIndexOf ("/");
            name = relativePath.Substring (lastSlashIndex);
            relativePath = relativePath.Substring (0, lastSlashIndex);
        }
    }

    // you can edit blow a few variable to fit your needs

    // the directory where to get the scripts to be converted
    private string m_sourceDirectory = "/CSharpToUnityScript/ScriptsToBeConverted/";

    // the directory where to put the converted scripts
    private string m_targetDirectory = "/CSharpToUnityScript/ConvertedScripts/";

    // do not edit anything below this point

    // ----------------------------------------------------------------------------------


    // a list of structure that contains all needed infos about the script to be converted
    protected List<Script> m_scriptsToConvertList = new List<Script> ();


    // ----------------------------------------------------------------------------------


    [MenuItem ("Script Converter/C# To UnityScript")]
    static void ShowWindow () {
        CSharpToUnityScriptExtension window = (CSharpToUnityScriptExtension)EditorWindow.GetWindow (typeof (CSharpToUnityScriptExtension));
        window.title = "C#>UnityScript";
    }

    
    void OnGUI () {
        GUILayout.Label ("The two paths below are relative to the \"Assets\" directory");

        m_sourceDirectory = EditorGUILayout.TextField ("Source directory : ", m_sourceDirectory);
        m_targetDirectory = EditorGUILayout.TextField ("Target directory : ", m_targetDirectory);
        GUILayout.Label ("A copy of the content of the source directory will be created inside the target directory with the converted scripts.");

        GUILayout.Space (20);

        if (GUILayout.Button ("Convert", GUILayout.MaxWidth (200))) {
            if (m_scriptsToConvertList.Count > 0)
                return;

             if (!m_sourceDirectory.StartsWith ("/"))
                m_sourceDirectory = "/"+m_sourceDirectory;

            if ( ! Directory.Exists (Application.dataPath+m_sourceDirectory)) {
                Debug.LogError ("C# to UnityScript converter : Abording preparation work because the source directory ["+m_sourceDirectory+"] does not exists !");
                return;
            }


            string[] paths = Directory.GetFiles (Application.dataPath+m_sourceDirectory, "*.cs", SearchOption.AllDirectories); // only C# scripts in the whole hyerarchie of the source directory

            // fill m_scriptsToConvertList
            foreach (string path in paths) {
                StreamReader reader = new StreamReader (path);
                string text = reader.ReadToEnd ();
                reader.Close ();

                string relativePath = path.Replace (Application.dataPath+m_sourceDirectory, ""); // just keep the relative path from the source directory
                m_scriptsToConvertList.Add (new Script (relativePath, text));
            }
            

            if (!m_targetDirectory.StartsWith ("/"))
                m_targetDirectory = "/"+m_targetDirectory;
            
            if (m_scriptsToConvertList.Count > 0)
                Debug.Log ("Proceeding with the conversion of "+m_scriptsToConvertList.Count+" scripts.");
                //proceedWithConvertion = true;
            else
                Debug.Log ("No Scripts to convert.");
        }


        if (GUILayout.Button ("Reset/Abord", GUILayout.MaxWidth (200))) {
            m_scriptsToConvertList.Clear ();
            AssetDatabase.Refresh ();
        }
    }


    // ----------------------------------------------------------------------------------

    /// <summaray>
    ///
    /// </summary>
    void Update () {
        //if (proceedWithConvertion && scriptIndex < m_scriptsToConvertList.Count) {
        if (m_scriptsToConvertList.Count > 0) {
            Script m_scriptInConversion = m_scriptsToConvertList[0];
            
            // makes sure the target directory exists
            string targetScriptPath = Application.dataPath+m_targetDirectory+m_scriptInConversion.relativePath;
            Directory.CreateDirectory (targetScriptPath); // make sure the directory exist, or create it

            // write the converted code into the file
            CSharpToUnityScriptConverter converter = new CSharpToUnityScriptConverter (m_scriptInConversion.text);

            StreamWriter writer = new StreamWriter (targetScriptPath+m_scriptInConversion.name+".js");
            writer.Write (converter.convertedCode);
            writer.Flush ();
            writer.Close ();


            Debug.Log ("Converted "+m_scriptInConversion.relativePath+m_scriptInConversion.name);

            m_scriptsToConvertList.RemoveAt (0);

            // auto refresh the project once all files have been converted
            if (m_scriptsToConvertList.Count <= 0) 
                AssetDatabase.Refresh ();
        }
    }
} // end of class CSharpToUnityScriptExtension

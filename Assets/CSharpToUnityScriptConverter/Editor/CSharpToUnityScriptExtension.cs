/// <summary>
/// CSharpToUnityScriptExtension class for Unity3D
///
/// This class is part of the "C# To UnityScript Converter" extension for Unity3D.
/// It uses the CSharpToUnityScriptConverter class to convert scripts from C# to UnityScript.
///
/// Check out the online manual at : http://florentpoujol.github.com/CSharpToUnityScriptConverter
///
/// Created by Florent POUJOL
/// florent.poujol@gmail.com
/// http://www.florent-poujol.fr/en
/// Profile on Unity's forums : http://forum.unity3d.com/members/23148-Lion
/// </summary>


using UnityEngine;
using UnityEditor;
using System.Collections; // Stack
using System.Collections.Generic; // List
using System.IO; // StreamReader/Writer


public class CSharpToUnityScriptExtension : EditorWindow 
{

    /// <summary>
    /// Structure that store some information about a particular script
    /// </summary>
    public struct Script 
    {
        // relative path
        public string relativePath;
        public string text;
        public string name;

        public Script(string p_relativePath, string p_text) 
        {
            text = p_text;
            relativePath = p_relativePath.Replace(".cs", "").Replace("\\", "/");

            int lastSlashIndex = relativePath.LastIndexOf("/");

            if (lastSlashIndex == -1) 
            { // no slash found => the file is a the root of the source directory, so relativePath is it's name
                name = relativePath;
                relativePath = "";
            }
            else 
            {
                name = relativePath.Substring(lastSlashIndex);
                relativePath = relativePath.Substring(0, lastSlashIndex);
            }
        }
    }


    // the directory where to get the scripts to be converted
    public string sourceDirectory = "/CSharpToUnityScriptConverter/Source/";

    // the directory where to write the converted scripts
    public string targetDirectory = "/CSharpToUnityScriptConverter/ConvertedScripts/";

    // a list of structure that contains all needed infos about the script to be converted
    protected List<Script> scriptsToConvertList = new List<Script>();

    // the instance of the converter class
    CSharpToUnityScriptConverter converter;


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Method that will be called when the extension will be openen in the Unity editor
    // The MenuItem attribute defineds which menu item will trigger the call to this method
    /// </summary>
    [MenuItem("Window/C# To UnityScript Converter")]
    static void ShowWindow() 
    {
        CSharpToUnityScriptExtension window = (CSharpToUnityScriptExtension)EditorWindow.GetWindow(typeof(CSharpToUnityScriptExtension));
        window.title = "C# to UnityScript";
    }


    // ----------------------------------------------------------------------------------
    
    Vector2 scrollPosition;

    /// <summary>
    /// Draw the GUI
    /// </summary>
    void OnGUI() 
    {
        // scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("The two paths below are relative to the \"Assets\" directory");

            sourceDirectory = EditorGUILayout.TextField("Source directory : ", sourceDirectory);
            targetDirectory = EditorGUILayout.TextField("Target directory : ", targetDirectory);
            //GUILayout.Label("A copy of the content of the source directory will be created inside the target directory with the converted scripts.");

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();

                if (GUILayout.Button("Convert", GUILayout.MaxWidth(200))) 
                {
                    if (scriptsToConvertList.Count > 0)
                        return;

                    if (!sourceDirectory.StartsWith("/"))
                        sourceDirectory = "/"+sourceDirectory;

                    if (!Directory.Exists(Application.dataPath+sourceDirectory)) 
                    {
                        Debug.LogError("C# to UnityScript converter : Abording convertion because the source directory ["+sourceDirectory+"] does not exists !");
                        return;
                    }


                    string[] paths = Directory.GetFiles(Application.dataPath+sourceDirectory, "*.cs", SearchOption.AllDirectories); // only C# scripts in the whole hyerarchie of the source directory

                    // fill scriptsToConvertList
                    foreach (string path in paths) 
                    {
                        StreamReader reader = new StreamReader(path);
                        string text = reader.ReadToEnd();
                        reader.Close();

                        string relativePath = path.Replace(Application.dataPath+sourceDirectory, ""); // just keep the relative path from the source directory
                        scriptsToConvertList.Add(new Script(relativePath, text));
                    }
                    

                    if (!targetDirectory.StartsWith("/"))
                        targetDirectory = "/"+targetDirectory;
                    
                    if (scriptsToConvertList.Count > 0) 
                    {
                        Debug.Log("Proceeding with the conversion of "+scriptsToConvertList.Count+" scripts.");
                        CSharpToUnityScriptConverter.importedAssemblies.Clear();
                    }
                    else
                        Debug.Log("No Scripts to convert.");
                }


                if (GUILayout.Button("Abord", GUILayout.MaxWidth(200))) 
                {
                    Debug.LogWarning("Abording conversion and refreshing project.");
                    scriptsToConvertList.Clear();
                    converter = null;
                    AssetDatabase.Refresh();
                }

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (scriptsToConvertList.Count > 0)
                GUILayout.Label(scriptsToConvertList.Count+" scripts left to convert.");

            GUILayout.Space(10);

            GUILayout.Label("-----------");
            GUILayout.Label("Options : ");

            CSharpToUnityScriptConverter.convertMultipleVarDeclaration = GUILayout.Toggle(CSharpToUnityScriptConverter.convertMultipleVarDeclaration, "Convert multiple variable declaration.");

        // GUILayout.EndScrollView();
    }


    // ----------------------------------------------------------------------------------
    
    /// <summaray>
    /// Called 100 times per second
    /// Perform the conversion, one file per frame
    /// </summary>
    void Update() 
    {
        if (scriptsToConvertList.Count > 0) 
        {
            Script scriptInConversion = scriptsToConvertList[0];
            
            // makes sure the target directory exists
            string targetScriptPath = Application.dataPath+targetDirectory+scriptInConversion.relativePath;
            Directory.CreateDirectory(targetScriptPath); // make sure the directory exist, or create it

            // write the converted code into the file
            if (converter == null)
                converter = new CSharpToUnityScriptConverter(sourceDirectory);

            // converter.Convert(scriptInConversion.text);
            //CSharpToUnityScriptConverter converter = new CSharpToUnityScriptConverter(scriptInConversion.text);

            StreamWriter writer = new StreamWriter(targetScriptPath+scriptInConversion.name+".js");
            writer.Write(converter.Convert(scriptInConversion.text));
            writer.Flush();
            writer.Close();


            Debug.Log("Converted "+scriptInConversion.relativePath+scriptInConversion.name);

            scriptsToConvertList.RemoveAt(0);
            Repaint(); // update the GUI

            // auto refresh the project once all files have been converted
            if (scriptsToConvertList.Count <= 0) 
            {
                Debug.LogWarning("Conversion done ! Refreshing project.");
                AssetDatabase.Refresh();
            }
        }
    }
} // end of class CSharpToUnityScriptExtension

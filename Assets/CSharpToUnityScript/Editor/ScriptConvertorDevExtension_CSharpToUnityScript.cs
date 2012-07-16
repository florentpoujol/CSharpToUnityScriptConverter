using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections;

class ScriptConvertorDevExtension_CSharpToUnityScript : EditorWindow { 

	[MenuItem ("Window/ScriptConvertorDevelopementExtension_CSharpToUnityScript")]
    static void ShowWindow () {
        ScriptConvertorDevExtension_CSharpToUnityScript window = (ScriptConvertorDevExtension_CSharpToUnityScript)EditorWindow.GetWindow (typeof(ScriptConvertorDevExtension_CSharpToUnityScript));
        window.title = "ScriptConvertorDevExtension_CSharpToUnityScript";
    }
    
    string m_scriptName = "aaTest";
    //public DateTime m_scriptLastWriteTime = DateTime.Now.ToLocalTime ();
    string m_scriptRelativePath = "/CSharpToUnityScript/ScriptsToBeConverted/";

    // the interval (in second) time between two project checking
	// can be set in the extension's window
	double m_checkInterval = 1;
	public DateTime m_lastCheckTime = DateTime.Now.ToLocalTime ();

    // script's extension in the source language 
	string m_sourceScriptExtension = ".cs";

	// script's extension in the target language 
	// must be .cs .js or .boo
	string m_targetScriptExtension = ".js";


    void Update () {
    	if ( DateTime.Now.ToLocalTime () > m_lastCheckTime.AddSeconds (m_checkInterval) ) {
			m_lastCheckTime = DateTime.Now.ToLocalTime ();

			Convert (false);
		}
    }


    void OnGUI () {
		if (GUILayout.Button ("Force Conversion"))
    		Convert (true);
    }


    void Convert (bool forceConversion) {
		string sourceScriptPath = Application.dataPath + m_scriptRelativePath + m_scriptName + m_sourceScriptExtension;
		string targetScriptPath = sourceScriptPath.Replace (m_sourceScriptExtension, m_targetScriptExtension);

		if ( ! File.Exists (sourceScriptPath)) {
			Debug.LogError ("CustomScriptDev.Convert() : source script does not exists at path ["+sourceScriptPath+"]");
			return;
		}

		// convert if forcer, or target does not exist, or source newer than target
		if (forceConversion || ! File.Exists (targetScriptPath) || File.GetLastWriteTime (sourceScriptPath) > File.GetLastWriteTime (targetScriptPath)) {
			StreamReader reader = new StreamReader (sourceScriptPath);
			string inputCode = reader.ReadToEnd ();
			reader.Close ();

			string outputCode;

			CSharpToUnityScriptConverter convertor = new CSharpToUnityScriptConverter (inputCode);
			outputCode = convertor.convertedCode;
			
			StreamWriter writer = new StreamWriter (targetScriptPath);
			writer.Write(outputCode);
			writer.Flush ();
			writer.Close ();

			Debug.Log ("Convert "+m_scriptName+" at "+DateTime.Now.ToLocalTime ());
		}
    }
} // end of class CustomScriptDev_CSharpToUnityScript

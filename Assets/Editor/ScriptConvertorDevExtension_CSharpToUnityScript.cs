using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections;

class ScriptConvertorDevExtension_CSharpToUnityScript : EditorWindow { 

	[MenuItem ("Script Converters/Dev. extension")]
    static void ShowWindow () {
        ScriptConvertorDevExtension_CSharpToUnityScript window = (ScriptConvertorDevExtension_CSharpToUnityScript)EditorWindow.GetWindow (typeof(ScriptConvertorDevExtension_CSharpToUnityScript));
        window.title = "ScriptConvertorDevExtension_CSharpToUnityScript";
    }
    
    public string m_scriptName = "Test1";
    //public DateTime m_scriptLastWriteTime = DateTime.Now.ToLocalTime ();
    public string m_scriptRelativePath = "/GitIgnore/ScriptsToBeConverted/";

    // the interval (in second) time between two project checking
	// can be set in the extension's window
	double m_checkInterval = 1;
	public DateTime m_lastCheckTime = DateTime.Now.ToLocalTime ();

    // script's extension in the source language 
	public string m_sourceScriptExtension = ".cs";

	// script's extension in the target language 
	// must be .cs .js or .boo
	public string m_targetScriptExtension = ".js";


	public bool doConvert = false;


	// ----------------------------------------------------------------------------------


	void OnGUI () {

		m_scriptRelativePath = EditorGUILayout.TextField ("Script relative path : ", m_scriptRelativePath);
		m_scriptName = EditorGUILayout.TextField ("Script name : ", m_scriptName);
		m_sourceScriptExtension = EditorGUILayout.TextField ("Source extension : ", m_sourceScriptExtension);
		m_targetScriptExtension = EditorGUILayout.TextField ("Target extension : ", m_targetScriptExtension);

		doConvert = GUILayout.Toggle(doConvert, "Do Convert");

		CSharpToUnityScriptConverter.convertMultipleVarDeclaration = GUILayout.Toggle(CSharpToUnityScriptConverter.convertMultipleVarDeclaration, "ConvertMultipleVarDeclaration");
		CSharpToUnityScriptConverter.removeRefKeyword = GUILayout.Toggle(CSharpToUnityScriptConverter.removeRefKeyword, "removeRefKeyword");
		CSharpToUnityScriptConverter.removeOutKeyword = GUILayout.Toggle(CSharpToUnityScriptConverter.removeOutKeyword, "removeOutKeyword");


		if (GUILayout.Button ("Force Conversion", GUILayout.MinHeight (100)))
    		Convert (true);
    }


    void Update () {
    	if ( DateTime.Now.ToLocalTime () > m_lastCheckTime.AddSeconds (m_checkInterval) ) {
			m_lastCheckTime = DateTime.Now.ToLocalTime ();

			if (doConvert)
				Convert (false);
		}
    }

    CSharpToUnityScriptConverter converter;

    void Convert (bool forceConversion) {
    	converter = new CSharpToUnityScriptConverter(m_scriptRelativePath);
    	
		string sourceScriptPath = Application.dataPath + m_scriptRelativePath + m_scriptName + m_sourceScriptExtension;
		string targetScriptPath = sourceScriptPath.Replace (m_sourceScriptExtension, m_targetScriptExtension);

		if ( ! File.Exists (sourceScriptPath)) {
			Debug.LogError ("CustomScriptDev.Convert() : source script does not exists at path ["+sourceScriptPath+"]");
			return;
		}

		// convert if forcer, or target does not exist, or source newer than target
		if (forceConversion || 
			! File.Exists (targetScriptPath) || 
			File.GetLastWriteTime (sourceScriptPath) > File.GetLastWriteTime (targetScriptPath)) {

			CSharpToUnityScriptConverter.importedAssemblies.Clear ();

			StreamReader reader = new StreamReader (sourceScriptPath);
			string inputCode = reader.ReadToEnd ();
			reader.Close ();

			string outputCode;

            converter.Convert( inputCode );
			//CSharpToUnityScriptConverter convertor = new CSharpToUnityScriptConverter (inputCode);
			outputCode = converter.convertedCode;
			
			StreamWriter writer = new StreamWriter (targetScriptPath);
			writer.Write(outputCode);
			writer.Flush ();
			writer.Close ();

			Debug.Log ("Convert "+m_scriptName+" at "+DateTime.Now.ToLocalTime ());
			AssetDatabase.Refresh ();
		}
    }
} // end of class CustomScriptDev_CSharpToUnityScript

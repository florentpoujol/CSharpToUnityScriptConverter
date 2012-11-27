/// <summary>
/// CSharpToUnityScriptDemo class for Unity3D
///
/// This script allow uses the CSarpToUnityScriptConverter class
/// to convert snippet of code from C# to UnityScript.
/// 
/// Created by Florent POUJOL
/// florent.poujol@gmail.com
/// http://www.florent-poujol.fr/en
/// Profile on Unity's forums : http://forum.unity3d.com/members/23148-Lion
/// </summary>

using UnityEngine;

public class CSharpToUnityScriptDemo : MonoBehaviour
{

	string inputCode = "Write in this textarea your C# code then hit the \"Convert\" button to see the translated code in UnityScript in the right text area."+
	" The input code is limited to 100 lines.";
	string convertedCode = "";

	int textAreaWidth = 390;
	int textAreaHeight = 350;

	int buttonWidth = 50;
	int buttonHeight = 350;

	// dimension of the webplayer : 850*400

	Vector2 inputScrollPosition;
	Vector2 ouputScrollPosition;

	void OnGUI () 
	{
		GUI.Label(new Rect(150, 10, 100, 50), "C#");
		
		GUI.Label(new Rect(620, 10, 100, 50), "UnityScript");
	
		GUILayout.Space(40);

		GUILayout.BeginHorizontal();
			inputScrollPosition = 
			GUILayout.BeginScrollView(inputScrollPosition, GUILayout.MinWidth(textAreaWidth), GUILayout.MinHeight(textAreaHeight));
				inputCode = CropString(GUILayout.TextArea(inputCode));
				
			GUILayout.EndScrollView();

			if (GUILayout.Button("Convert !", GUILayout.MinWidth(buttonWidth), GUILayout.MinHeight(buttonHeight))) 
			{
				CSharpToUnityScriptConverter converter = new CSharpToUnityScriptConverter("null");
				convertedCode = converter.Convert(inputCode);
				//Debug.Log ("convertedCode = "+convertedCode);
			}

			ouputScrollPosition = 
			GUILayout.BeginScrollView(ouputScrollPosition, GUILayout.MinWidth (textAreaWidth), GUILayout.MinHeight (textAreaHeight));
				convertedCode = GUILayout.TextArea(convertedCode);
			GUILayout.EndScrollView();
			//convertedCode = GUILayout.TextArea (convertedCode, GUILayout.MinWidth (textAreaWidth), GUILayout.MinHeight (textAreaWidth));
		GUILayout.EndHorizontal();
	}


	//----------------------------------------------------------------------------------

	/// <summary>
	/// Crop the input string to certain amount of lines
	/// </summary>
	/// <param name="input">The input text to be cropped</param>
	/// <returns>The cropped text</returns>
	string CropString(string input)
	{
		string output = "";
		int lineCount = 100; // number of lines
		char EOL = '\n';

		string[] lines = input.Split(EOL);

        if (lines.Length == 1) // file has Mac line ending
        {
        	EOL = '\r';
            lines = convertedCode.Split('\r');
        }
        
        if (lines.Length < lineCount)
        	return input;

        for (int i = 0; i <= lineCount; i++)
        {
            output += lines[i]+EOL;
        }

        return output;
	}
}

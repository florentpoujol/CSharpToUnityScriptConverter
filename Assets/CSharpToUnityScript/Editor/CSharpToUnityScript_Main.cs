/// <summary>
/// UnityScript (JavaScript) to C# converter script.
/// UnityScript is a JavaScript-looking scripting language for use with the Unity3D Engine
///
/// Created by Florent POUJOL
/// florent.poujol[gmail.com]
/// http://www.florent-poujol.fr
/// Profile on Unity's forums : http://forum.unity3d.com/members/23148-Lion
/// 
/// Script"s dedicated thread on Unity"s forums : 
/// </summary>


/// <summary>
/// Use instructions :
/// 
/// Put this script anywhere in your project asset folder and attach it to a GameObject
///
/// Create a folder "[your project]/Assets/ScriptsToBeConverted".
/// You may put in this folder any .js file (and the folder they may be in) to be converted.
/// 
/// Run the scene. One script is converted per frame, but the convertion of one script may often takes longer than 1/60 seconds. The convertion speed is///approximately* 20 files/seconds.
/// A label on the "Game" view shows the overall progress of the convertion and each convertion is logged in the console.
/// When it's complete, refresh the project tab for the new files/folder to be shown (right-click on the "Project" tab, then click on "Refresh" (or hit Ctrl+R on Windows)) 
///
/// Upon convertion, a folder "[your project]/Assets/ConvertedScripts" is created with all converted scripts (and their folder hyerarchie)
/// </summary>


// ----------------------------------------------------------------------------------


using UnityEngine;
using UnityEditor; // EditorGUILayout
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions; // Directory.GetFiles() Directory.CreateDirectory() StreamReader  StreamWriter
using System.IO; // Regex.Replace(), Match, Matches, MatchCollection...


// ----------------------------------------------------------------------------------


public class CSharpToUnityScript_Main: CSharpToUnityScript {

    // the directory where to get the scripts to be converted
    private string sourceDirectory = "/CSharpToUnityScript/ScriptsToBeConverted/";

    // the directory where to put the converted scripts
    private string targetDirectory = "/CSharpToUnityScript/ConvertedScripts/";


    // index (in paths) of the file currently being converted
    private int scriptIndex = 0;

    private bool proceedWithConvertion = false;


    // ----------------------------------------------------------------------------------


    [MenuItem ("Script Converter/C# To UnityScript")]
    static void ShowWindow () {
        CSharpToUnityScript_Main window = (CSharpToUnityScript_Main)EditorWindow.GetWindow (typeof(CSharpToUnityScript_Main));
        window.title = "C# to UnityScript";
    }

    
    void OnGUI () {
        GUILayout.Label ("The two paths below are relative to the \"Assets\" directory");

        sourceDirectory = EditorGUILayout.TextField ("Source directory : ", sourceDirectory);
        targetDirectory = EditorGUILayout.TextField ("Target directory : ", targetDirectory);
        GUILayout.Label ("A copy of the content of the source directory will be created inside the target directory with the converted scripts.");

        GUILayout.Space (20);

        if ( GUILayout.Button ( "Convert", GUILayout.MaxWidth ( 200 ) ) ) {
            Reset();

            if ( !sourceDirectory.StartsWith ("/"))
                sourceDirectory = "/"+sourceDirectory;

            if ( !Directory.Exists (Application.dataPath+sourceDirectory)) {
                Debug.LogError ("UnityScript to C# converter : Abording preparation work because the source directory ["+sourceDirectory+"] does not exists !");
                return;
            }


            string[] paths = Directory.GetFiles (Application.dataPath+sourceDirectory, "*.cs", SearchOption.AllDirectories); // only C# scripts in the whole hyerarchie of the source directory

            // fill scriptsList
            foreach (string path in paths) {
                StreamReader reader = new StreamReader (path);
                string text = reader.ReadToEnd ();
                reader.Close ();

                string relativePath = path.Replace (Application.dataPath+sourceDirectory, ""); // just keep the relative path from the source directory
                scriptsList.Add (new Script (relativePath, text));
            }
            

            if ( !targetDirectory.StartsWith ("/"))
                targetDirectory = "/"+targetDirectory;
            
            if ( scriptsList.Count > 0)
                proceedWithConvertion = true;
            else
                Debug.Log ("Nothing to convert");
        }


        if (GUILayout.Button ("Reset/Abord", GUILayout.MaxWidth (200) ) ) {
            Reset ();
        }
    }


    // ----------------------------------------------------------------------------------

    /// <summaray>
    ///
    /// </summary>
    void Update () {
        if (proceedWithConvertion && scriptIndex < scriptsList.Count) {
            script = scriptsList[scriptIndex++];
            
            ConvertScript ();
            
            string path = Application.dataPath+targetDirectory+script.path;
            //Debug.Log (path);
            Directory.CreateDirectory (path); // make sure the directory exist, or create it

            StreamWriter writer = new StreamWriter (path+script.name+".js");
            writer.Write (script.text);
            writer.Flush ();
            writer.Close ();

            if (scriptIndex >= scriptsList.Count)
                proceedWithConvertion = false;
        }
    }

    
    // ----------------------------------------------------------------------------------

    /// <summaray>
    /// Reset a few variabale related to the convertion pocess
    /// </summary>
    void Reset () {
        proceedWithConvertion = false;
        scriptsList.Clear ();
        scriptIndex = 0;
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Main function for the convertion
    /// </summary>
    void ConvertScript () {
        
        // generic collections

            // Add a dot before the opening chevron  List.<float>
            patterns.Add ( genericCollections+optWS+"<" );
            replacements.Add ( "$1$2.<" );

            // Add a whitespace between two closing chevron   Dictionary.<string,List<string> > 
            patterns.Add ( "("+genericCollections+optWS+"<.+)>>" );
            replacements.Add ( "$1> >" );


        // Loops

            // foreach ( in ) > for ( in )
            patterns.Add ( "foreach("+optWS+"\\(.+in"+oblWS+".+\\))" );
            replacements.Add ( "for$1" );


        // GetComponent (also works for GetComponentInChildren)

            //Getcomponent(T) GetComponent<T>() GetComponent<T>()  => Getcompoenent<T>()
            // GetComponent<T>() will have been modified to GetComponent<T>() at this point
            patterns.Add ( "(GetComponent|GetComponents|GetComponentInChildren|GetComponentsInChildren)"+optWS+"<"+optWS+commonChars+optWS+">" );
            replacements.Add ( "$1.<$4>" );


        // abstract things

            patterns.Add ("((public|private|protected|static)"+oblWS+")abstract"+oblWS);
            replacements.Add ("$1");


        // Yields

            /*patterns.Add ( "yield"+optWS+";" );
            replacements.Add ( "yield return 0;" );

            // yield return  ;
            patterns.Add (  "yield("+oblWS+commonChars+optWS+";)" );
            replacements.Add ( "yield return $2;" );
        
            // yield return new WaitForSeconds(3.5f);
            patterns.Add ( "yield"+oblWS+commonChars+optWS+"\\(" );
            replacements.Add ( "yield return new $2$3(" );

            patterns.Add ( "yield return new" );
            replacements.Add ( "yield return new" );*/


        


        // do the search/replace in script
        DoReplacements ();
    

        // Convert stuffs related to classes : declaration, inheritance, parent constructor call
        CSharpToUnityScript_Classes.Classes ();

        // convert variables declarations
        // it will always resolve the variable type unless when the value is returned from a function (see VariablesTheReturn() void below)
        CSharpToUnityScript_Variables.Variables ();

        

        // convert void declarations, including arguments declaration
        CSharpToUnityScript_Functions.Functions ();
        
        // convert properties declarations
        CSharpToUnityScript_Variables.Properties ();
        
        // add the keyword public when no visibility (or just static) is set (the default visibility in JS is public but private in C#)
        // works also for functions
        CSharpToUnityScript_Classes.AddVisibility ();


        // #region
        patterns.Add ("\\#(region|REGION)"+oblSpaces+commonName+"("+oblSpaces+commonName+")*");
        replacements.Add ("");
        patterns.Add ("\\#(endregion|ENDREGION)");
        replacements.Add ("");

        // define
        patterns.Add ("\\#(define|DEFINE)"+oblSpaces+commonName+"("+oblSpaces+commonName+")*");
        replacements.Add ("");



        //script.text = "#pragma strict"+EOL+script.text;


        Debug.Log ("Convertion done for ["+script.path+script.name+".cs]." );
    } // end Convert()
} // end of class CSharpToUnityScript_Main


/*/// <summary>
    /// Read the file ItemsAndTypes.txt and extract the key/value pairs in the itemsAndTypes List
    /// </summary>
    /// <param name="getEmptyValues">Tell wether or not adding the keys without a value to the list</param>
    void GetItemsAndTypes (bool getEmptyValues) {
        itemsAndTypes.Clear ();
        
        string path = Application.dataPath+"/CSharpToUnityScript/ItemsAndTypes.txt";

        if ( ! File.Exists (path))
            return;

        // read ItemsAndTypes.txt
        StreamReader reader = new StreamReader (path);

        while (true) {
            string line = reader.ReadLine ();
            if (line == null)
                break;

            if (line.Trim () == "" || line.StartsWith ("#") || ! line.Contains ("=")) // an empty line, a comment, or a line that does not contains an equal sign (that would cause errors below)
                continue;

            string[] items = line.Split ('='); // item[0] is the item/value    item[1] is the type

            if ( ! itemsAndTypes.ContainsKey (items[0].Trim ())) {
                if ( ! getEmptyValues && items[1].Trim () != "")
                    itemsAndTypes.Add (items[0].Trim (), items[1].Trim ());
                else
                    itemsAndTypes.Add (items[0].Trim (), items[1].Trim ());
            }    
        }

        reader.Close ();
    }

    void GetItemsAndTypes () {
        GetItemsAndTypes (false);
    }*/
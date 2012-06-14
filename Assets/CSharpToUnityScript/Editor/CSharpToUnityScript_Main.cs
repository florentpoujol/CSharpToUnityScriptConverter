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
using UnityEditor;
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

    

    

    // the text (script.text) of the file currently being converted
    //private string file = "";

    // the name (File.name) of the file currently being converted
    //private string fileName = "";

    // index (in paths) of the file currently being converted
    private int scriptIndex = 0;

    private bool preparationsDone = false;
    private bool proceedWithConvertion = false;


    //--------------------


    private string preparationState = "Waiting to begin.";

    private string convertionState = "Waiting to begin.";


    // ----------------------------------------------------------------------------------




    [MenuItem ("Script Converter/C# To UnityScript")]
    static void ShowWindow () {
        CSharpToUnityScript_Main window = (CSharpToUnityScript_Main)EditorWindow.GetWindow (typeof(CSharpToUnityScript_Main));
        window.title = "C# to UnityScript";
    }

    
    void OnGUI () {
        GUILayout.Label ("The two paths below are relative to the \"Assets\" directory");
        //GUILayout.Space (10);
        sourceDirectory = EditorGUILayout.TextField ("Source directory : ", sourceDirectory);
        //GUILayout.Space (10);
        targetDirectory = EditorGUILayout.TextField ("Target directory : ", targetDirectory);
        GUILayout.Label ("A copy of the content of the source directory will be created inside the target directory with the converted scripts.");

        GUILayout.Space (20);

        if ( GUILayout.Button ( "Convert", GUILayout.MaxWidth ( 200 ) ) ) {
            Reset();

            if ( ! sourceDirectory.StartsWith ("/"))
                sourceDirectory = "/"+sourceDirectory;

            if ( ! Directory.Exists (Application.dataPath+sourceDirectory)) {
                preparationState = "Abording ! Source directory does not exists !";
                Debug.LogError ("UnityScript to C# converter : Abording preparation work because the source directory ["+sourceDirectory+"] does not exists !");
                return;
            }

            string[] paths = Directory.GetFiles (Application.dataPath+sourceDirectory, "*.cs", SearchOption.AllDirectories); // only C# scripts in the whole hyerarchie of the source directory
            string _path;
            scriptsList.Clear ();
            scriptIndex = 0;


            // fill scriptsList
            foreach (string path in paths) {
                StreamReader reader = new StreamReader (path);
                string text = reader.ReadToEnd ();
                reader.Close ();

                _path = path.Replace (Application.dataPath+sourceDirectory, ""); // just keep the relative path from the source directory
                scriptsList.Add (new Script (_path, text));
            }
            

            if ( !targetDirectory.StartsWith ( "/" ) )
                targetDirectory = "/"+targetDirectory;
            
            if ( scriptsList.Count > 0 )
                proceedWithConvertion = true;
            else
                Debug.Log ( "Nothing to convert" );
        }


        if ( GUILayout.Button ( "Reset/Abord", GUILayout.MaxWidth ( 200 ) ) ) {
            Reset ();
        }
    }

    void Reset () {
        proceedWithConvertion = false;
        scriptsList.Clear ();
        scriptIndex = 0;
    }

    void Update () {
        if (proceedWithConvertion && scriptIndex < scriptsList.Count) {
            script = scriptsList[scriptIndex++];
            
            ConvertFile ();
            Debug.Log ("Convertion done for ["+script.path+script.name+".cs]." );

            string path = Application.dataPath+targetDirectory+script.path;
            //Debug.Log (path);
            Directory.CreateDirectory (path); // make sure the directory exist, or create it

            StreamWriter writer = new StreamWriter (path+script.name+".js");
            writer.Write (script.text);
            writer.Flush ();
            writer.Close ();

            //convertionState = "Converting : "+(scriptIndex+0.0 / scriptsList.Count+0.0 * 100.0)+"%";

            if (scriptIndex >= scriptsList.Count-1) {
                //convertionState = "Convertion done !";
                proceedWithConvertion = false;
                scriptIndex = 0;
            }
        }
    }

    
    /// <summary>
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
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Main function for the convertion
    /// </summary>
    void ConvertFile () {

        
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

            // convert leftover "foreach (string"
            //patterns.Add ( "(foreach"+optWS+"\\("+optWS+")string" );
            //replacements.Add ( "$1string" );
        
            // foreach (Type name in array) > foreach (Type name in array)
            // patterns.Add ( "foreach"+optWS+"\\("+optWS+"var"+oblWS+commonName+optWS+":"+optWS+commonChars+"("+optWS+"in(.+)\\))" );
            // replacements.Add ( "foreach$1($7 $4$8" );


        // GetComponent (also works for GetComponentInChildren)

            //Getcomponent(T) GetComponent<T>() GetComponent<T>()  => Getcompoenent<T>()
            // GetComponent<T>() will have been modified to GetComponent<T>() at this point
            patterns.Add ( "(GetComponent|GetComponents|GetComponentInChildren|GetComponentsInChildren)"+optWS+"<"+optWS+commonChars+optWS+">" );
            replacements.Add ( "$1.<$4>" );
        
            /*patterns.Add ( "(GetComponent|GetComponents|GetComponentInChildren|GetComponentsInChildren)"+optWS+"\\("+optWS+"[\"']{1}"+commonChars+"[\"']{1}"+optWS+"\\)" );
            replacements.Add ( "$1<$4>()" );

            patterns.Add ( "(GetComponent|GetComponents|GetComponentInChildren|GetComponentsInChildren)"+optWS+"\\("+optWS+commonChars+optWS+"\\)" );
            replacements.Add ( "$1<$4>()" );*/

            // convert var declaraion
            /*patterns.Add ( "var"+oblWS+commonName+"("+optWS+"="+optWS+"("+commonName+")?(GetComponent|GetComponents|GetComponentInChildren|GetComponentsInChildren)"+optWS+"<"+optWS+commonChars+optWS+">)" );
            replacements.Add ( "$11 $2$3" );

            // convert var declaraion
            patterns.Add ( "var"+oblWS+commonName+"("+optWS+"="+optWS+"("+commonName+")?(GetComponents|GetComponentsInChildren)"+optWS+"<"+optWS+commonChars+optWS+">)" );
            replacements.Add ( "$11[] $2$3" );*/


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

        // convert properties declarations
        CSharpToUnityScript_Variables.Properties ();

        // convert void declarations, including arguments declaration
        CSharpToUnityScript_Functions.Functions ();
    
        // add the keyword public when no visibility (or just static) is set (the default visibility in JS is public but private in C#)
        // works also for functions
        CSharpToUnityScript_Classes.AddVisibility ();


        


        //script.text = "#pragma strict"+EOL+script.text;



        // add typeof() where needed
            // new GameObject("name", typeof(Type));)
            //patterns.Add ( "(new"+oblWS+"GameObject"+optWS+"\\(.*,"+optWS+")"+commonName+"("+optWS+"\\)"+optWS+";)" );
            //replacements.Add ( "$1typeof($5)$6" );


        // ----------------------------------------------------------------------------------


        // we near the end of the convertion, it's time for patching things up 

            // convert leftover String and Boolean
            /*patterns.Add ( "((public|private|protected)"+oblWS+")String("+optWS+"\\["+optWS+"\\])?" );
            replacements.Add ( "$1string$3");

            patterns.Add ( "((public|private|protected)"+oblWS+")boolean("+optWS+"\\["+optWS+"\\])?" );
            replacements.Add ( "$1bool$3");

            // also in generic collections
            patterns.Add ( "((,|<)"+optWS+")?String("+optWS+"(,|>))?" ); // whitespaces should have been removed, but just in case...
            replacements.Add ( "$2string$6" );

            patterns.Add ( "((,|<)"+optWS+")?boolean("+optWS+"(,|>))?" ); // whitespaces should have been removed, but just in case...
            replacements.Add ( "$2bool$6" );


            // System.String got replaced by System.string
            patterns.Add ( "(System"+optWS+"."+optWS+")string" );
            replacements.Add ( "$1String" );

            // ToString got replaced by Tostring
            patterns.Add ( "(\\."+optWS+")Tostring("+optWS+"\\()" );
            replacements.Add ( "$1ToString$3" );

			// single quotation have to be replaced by double quotation marks 
			// but I can't just do :
			// patterns.Add ("'(.{2,})'");
			// replacements.Add ("\"$1\"");
			// it would cause to many artifacts
			
			// replac simple by double quotation mark in when returning a litteral string
			patterns.Add ("return"+oblWS+"'(.+)'"+optWS+";");
			replacements.Add ("return$1\"$2\"$3;");

			// return char
			//patterns.Add ("return"+oblWS+"(\"|'){1}(.+)(\"|'){1}"+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+";");
			//replacements.Add ("return$1'$2'$3;");
			
			
            // bugs/artefacts
			//patterns.Add ( "((public|private|protected)"+oblWS+")ring" );
			//replacements.Add ( "$1string");*/


        //DoReplacements();

    } // end Convert()
} // end of class CSharpToUnityScript_Main


 /*// ----------------------------------------------------------------------------------

    /// <summary>
    /// Read the project's scripts and map the list of all class, method, variable and their corresponding type
    /// </summary>
    void MapProjectItems () {


        // make a list of all encountered class in the project's scripts
        foreach (Script _script in scriptsList) {
            // search for class declaration pattern
            pattern = "class"+oblWS+commonName+"("+oblWS+"extends"+oblWS+commonName+")?"+optWS+"{";
            MatchCollection allClassesDeclarations = Regex.Matches (_script.text, pattern);
            
            MatchCollection allVariablesDeclarations; // for later use
            bool isMappingComplete = false; // tell wether the mapping is complete or not
            // it won't be complete if the script is MonoBehaviour derived script without a class declaration

            foreach (Match aClassDeclaration in allClassesDeclarations) {
                string className = aClassDeclaration.Groups[2].Value;

                if (className == _script.name)
                    isMappingComplete = true;

                new ProjectItem (className);


                // now look for methods inside the class
                Block classBlock = new Block (aClassDeclaration, _script.text);

                pattern = "function"+oblWS+commonName+optWS+"\\((?<args>.*)\\)("+optWS+":"+optWS+"(?<returnType>"+commonChars+"))?"+optWS+"{";
                MatchCollection allFunctionsDeclarations = Regex.Matches (classBlock.text, pattern);
                List<string> functionVariablesList = new List<string> (); // list of variable fuound inside functions

                foreach (Match aFunctionDeclaration in allFunctionsDeclarations) {
                    string functionName = aFunctionDeclaration.Groups[2].Value;
                    string functionType = aFunctionDeclaration.Groups["returnType"].Value; // functionType == "" if there is none

                    new ProjectItem (className, functionName, functionType);


                    // now look for variable inside the function
                    Block functionBlock = new Block (aFunctionDeclaration, classBlock.text);

                    pattern = "var"+oblWS+commonName+optWS+"(:"+optWS+commonChars+optWS+")?=";
                    allVariablesDeclarations = Regex.Matches (functionBlock.text, pattern);

                    foreach (Match aVariableDeclaration in allVariablesDeclarations) {
                        string variableName = aVariableDeclaration.Groups[2].Value;
                        string variableType = aVariableDeclaration.Groups[6].Value;

                        functionVariablesList.Add (variableName);

                        new ProjectItem (className, functionName, variableName, variableType);
                    } // end loop variable in that function


                    // now look for arguments
                    string rawArgs = aFunctionDeclaration.Groups["args"].Value;

                    if (rawArgs.Contains (":")) {
                        string[] args = rawArgs.Split (',');
                        
                        for (int i=0; i < args.Length; i++) {
                            string[] arg = args[i].Split (':');
                            new ProjectItem (className, functionName, arg[0].Trim(), arg[1].Trim());
                        }
                    }

                } // end loop functions in that class


                // now look for members inside that class
                pattern = "var"+oblWS+commonName+optWS+"(:"+optWS+commonChars+optWS+")?=";
                allVariablesDeclarations = Regex.Matches (classBlock.text, pattern); // this will also match the var declaration inside functions

                foreach (Match aVariableDeclaration in allVariablesDeclarations) {
                    string variableName = aVariableDeclaration.Groups[2].Value;

                    if (functionVariablesList.Contains (variableName)) // this variable is declared inside a function, so it can't be also declared in the class    argument can have the same name as a class variable
                        continue;

                    string variableType = aVariableDeclaration.Groups[6].Value;

                    new ProjectItem (className, "", variableName, variableType);

                } // end loop variable in that class

            } // end loop classes in that file


            //--------------------


            // if the script is a MonoBehaviour derived script without a class declaration, some members or function are still not mapped
            if ( ! isMappingComplete) {
                // do everything al avoer again
            }


        }

        
    } // end MapProjectItems
    */
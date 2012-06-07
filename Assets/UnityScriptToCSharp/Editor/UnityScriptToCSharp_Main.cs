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

public class UnityScriptToCSharp_Main: UnityScriptToCSharp {

    // the directory where to get the scripts to be converted
    private string sourceDirectory = "/UnityScriptToCSharp/ScriptsToBeConverted/";

    // the directory where to put the converted scripts
    private string targetDirectory = "/UnityScriptToCSharp/ConvertedScripts/";

    

    

    // the text (script.text) of the file currently being converted
    //private string file = "";

    // the name (File.name) of the file currently being converted
    //private string fileName = "";

    // index (in paths) of the file currently being converted
    private int fileIndex = 0;

    private bool proceedWithConvertion = false;


    //--------------------


    private string preparationState = "Waiting to begin.";

    private string convertionState = "Waiting to begin.";


    // ----------------------------------------------------------------------------------




    [MenuItem ("Window/UnityScript To C# Converter")]
    static void ShowWindow () {
        UnityScriptToCSharp window = (UnityScriptToCSharp)EditorWindow.GetWindow (typeof(UnityScriptToCSharp));
        window.title = "UniScript to C#";       
    }

    
    void OnGUI () {
        GUILayout.Label ("The two paths below are relative to the \"Assets\" directory");
        //GUILayout.Space (10);
        sourceDirectory = EditorGUILayout.TextField ("Source directory : ", sourceDirectory);
        //GUILayout.Space (10);
        targetDirectory = EditorGUILayout.TextField ("Target directory : ", targetDirectory);
        GUILayout.Label ("A copy of the content of the source directory will be created inside the target directory with the converted scripts.");

        GUILayout.Space (20);



        GUILayout.Space (20);

        if (GUILayout.Button ("(1) Do preparation work", GUILayout.MaxWidth (200))) {
            DoPreparations ();
        }

        GUILayout.Label (preparationState);

        GUILayout.Space (20);

        if (GUILayout.Button ("(2) Perfrom actual convertion", GUILayout.MaxWidth (200))) {
            GetItemsAndTypes ();

            // loop throught files and convert
            convertionState = "Converting : 0%";
            
            if ( ! targetDirectory.StartsWith ("\\"))
                targetDirectory = "/"+targetDirectory;
            
            proceedWithConvertion = true; // allow the convertion in Update()
        }

        GUILayout.Label (convertionState);


        /*GUILayout.BeginHorizontal ();

            if (GUILayout.Button ("Stop and reset")) {
                Reset ();
            }

            if (GUILayout.Button ("Do preparation work")) {
                DoPreparations ();
            }


            if (GUILayout.Button ("Begin actual convertion")) {
                GetItemsAndTypes ();

                // loop throught files and convert
                convertionState = "Converting : 0%";
                
                if ( ! targetDirectory.StartsWith ("\\"))
                    targetDirectory = "/"+targetDirectory;
                
                proceedWithConvertion = true;
            }



        GUILayout.EndHorizontal ();

        GUILayout.Space (20);

        GUILayout.BeginHorizontal ();
            GUILayout.FlexibleSpace ();
            GUILayout.Label ("State of the convertion : ");
            GUILayout.FlexibleSpace ();
        GUILayout.EndHorizontal ();

        GUILayout.Space (20);

        GUILayout.BeginHorizontal ();
            GUILayout.FlexibleSpace ();
            GUILayout.Label (convertionState);
            GUILayout.FlexibleSpace ();
        GUILayout.EndHorizontal ();

        GUILayout.Space (20);*/

        GUILayout.Label ("fileList.Count = "+scriptsList.Count);
        GUILayout.Label ("fileIndex = "+fileIndex);
        GUILayout.Label ("proceed with convertion ? "+proceedWithConvertion);

         if (GUILayout.Button ("Stop and reset", GUILayout.MaxWidth (200))) {
            Reset ();
        }
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Call for the convertion and write the new .cs scripts, once proceedWithConvertion=true
    /// </summary>
    void Update() {
        if (proceedWithConvertion && fileIndex < scriptsList.Count) {
            script = scriptsList[fileIndex++];
            
            ConvertFile ();
            Debug.Log ("Convertion done for ["+script.path+script.name+".cs]." );

            string path = Application.dataPath+targetDirectory+script.path;
            //Debug.Log (path);
            Directory.CreateDirectory (path); // make sure the directory exist, or create it

            StreamWriter writer = new StreamWriter (path+script.name+".cs");
            writer.Write (script.text);
            writer.Flush ();
            writer.Close ();

            convertionState = "Converting : "+(fileIndex+0.0 / scriptsList.Count+0.0 * 100.0)+"%";

            if (fileIndex == scriptsList.Count-1)
                convertionState = "Convertion done !";
        }
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Reset the value of the variable
    /// </summary>
    void Reset () {
        preparationState = "Waiting";
        convertionState = "";
        proceedWithConvertion = false;
        fileIndex = 0;
        scriptsList.Clear ();
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// - Read all files and fill the "scriptsList" list
    /// - Make a list of all classes  (1) in the file "UnityClases",  (2) in the file "MyClasses" and  (3) encountered in the the project's scripts then fill the "classesList" list
    /// </summary>
    void DoPreparations () {
        preparationState = "Preparing ...";

        // get the path of all fles to be converted
        if ( ! sourceDirectory.StartsWith ("/"))
            sourceDirectory = "/"+sourceDirectory;

        if ( ! Directory.Exists (Application.dataPath+sourceDirectory)) {
            preparationState = "Abording ! Source directory does not exists !";
            return;
        }

        string[] paths = Directory.GetFiles (Application.dataPath+sourceDirectory, "*.js", SearchOption.AllDirectories); // only JS scripts
        string _path;
        StreamReader reader;
        scriptsList.Clear ();

        // fill scriptsList
        foreach (string path in paths) {
            reader = new StreamReader (path);
            string text = reader.ReadToEnd ();
            reader.Close ();

            _path = path.Replace (Application.dataPath+sourceDirectory, ""); // just keep the relative path from the source directory
            scriptsList.Add (new Script (_path, text));
        }
        

        //--------------------


        // make a list of all encountered class in the project's scripts
        foreach (Script script in scriptsList) {
            // search for class declaration pattern
            pattern = "class"+oblWS+commonName+"("+oblWS+"extends"+oblWS+commonName+")?"+optWS+"{";
            MatchCollection allClassesDeclarations = Regex.Matches (script.text, pattern);

            foreach (Match aClassDeclaration in allClassesDeclarations) {
                string className = aClassDeclaration.Groups[2].Value;

                if ( ! classesList.Contains (className))
                    classesList.Add (className);
            }

            // always add the name of the script (in most cases, that's the name of the class inside the script)
            if ( ! classesList.Contains (script.name))
                classesList.Add (script.name);
        }

        // do the same but for the C# scripts in the project folder
        paths = Directory.GetFiles (Application.dataPath+sourceDirectory.TrimEnd ('/'), "*.cs", SearchOption.AllDirectories);
        foreach (string path in paths) {
            reader = new StreamReader (path);
            string text = reader.ReadToEnd ();
            reader.Close ();

            pattern = "class"+oblWS+commonName+"("+optWS+":"+optWS+commonName+")?"+optWS+"{";
            MatchCollection allClassesDeclarations = Regex.Matches (text, pattern);

            foreach (Match aClassDeclaration in allClassesDeclarations) {
                string className = aClassDeclaration.Groups[2].Value;

                if ( ! classesList.Contains (className))
                    classesList.Add (className);
            }
        }

        // do the same but for the Boo scripts in the project folder
        paths = Directory.GetFiles (Application.dataPath+sourceDirectory.TrimEnd ('/'), "*.boo", SearchOption.AllDirectories);
        foreach (string path in paths) {
            reader = new StreamReader (path);
            string text = reader.ReadToEnd ();
            reader.Close ();

            pattern = "class"+oblWS+commonName+"("+optWS+"\\("+optWS+commonName+optWS+"\\))?";
            MatchCollection allClassesDeclarations = Regex.Matches (text, pattern);

            foreach (Match aClassDeclaration in allClassesDeclarations) {
                string className = aClassDeclaration.Groups[2].Value;

                if ( ! classesList.Contains (className))
                    classesList.Add (className);
            }
        }


        //--------------------


        // append the content of UnityClasses.txt to classesList
        _path = Application.dataPath+"/UnityScriptToCSharp/UnityClasses.txt";
        if (File.Exists (_path)) { // StreamReader will throw an FileNotFoundException if the file is not found
            reader = new StreamReader (_path);

            while (true) {
                string className = reader.ReadLine ();
                if (className == null)
                    break;

                if (className.Trim () == "" || className.StartsWith ("#")) // an epty line or a comment
                    continue;

                if ( ! classesList.Contains (className))
                    classesList.Add (className);
            }

            reader.Close ();
        }

        // append the content of MyClasses.txt to classesList
        _path = Application.dataPath+"/UnityScriptToCSharp/MyClasses.txt";
        if (File.Exists (_path)) {
            reader = new StreamReader (_path);

            while (true) {
                string className = reader.ReadLine ();
                if (className == null)
                    break;

                if (className.Trim () == "" || className.StartsWith ("#")) // an epty line or a comment
                    continue;

                if ( ! classesList.Contains (className))
                    classesList.Add (className);
            }

            reader.Close ();
        }


        //--------------------
        

        // read all the files again and look for variable declaration without type where the value is another variable that is not declared somewhere in the project
        // => add the value to the itemsAndTypes.txt file
        // the script can't resolve itself the type of these variable, unless the user associate an item with a type in this file
        GetItemsAndTypes (true);

        StreamWriter writer = new StreamWriter (Application.dataPath+"/UnityScriptToCSharp/ItemsAndTypes.txt", true);
        //writer.WriteLine ("# all keys below are values that need to be associated with a type.");
        List<string> addedValues = new List<string> ();

        foreach (Script _script in scriptsList) {
            // search for variable declaration pattern
            pattern = "var"+oblWS+commonName+optWS+"="+optWS+"([a-zA-Z_]{1}"+commonName+")("+optWS+"\\(.*\\))?"+optWS+";";
            MatchCollection allVarDeclarations = Regex.Matches (_script.text, pattern);

            foreach (Match aVarDeclaration in allVarDeclarations) {
                string value = aVarDeclaration.Groups[5].Value;

                // value can't be a , a class instanciation, a char, a string, a numeric value
                // it must then be a variable name, a function call or a boolean
                if (value == "true" || value == "false")
                    continue;
                
                if (addedValues.Contains (value) || itemsAndTypes.ContainsKey (value) || value.Contains ("GetComponent") || value.Contains (".") || classesList.Contains (value) || collections.Contains (value) || genericCollections.Contains (value))
                    continue;
                else {
                    addedValues.Add (value);
                    writer.WriteLine (value+"=");
                }
            }


            // search for variable return pattern
            pattern = "return"+oblWS+"([A-Z]{1}"+commonName+")("+optWS+"\\(.*\\))?"+optWS+";"; // why [A-Z] => see coment below       but why only look for var in other class or function call ?=> because that's where I can't resolve the type
            MatchCollection allReturnStatements = Regex.Matches (_script.text, pattern);

            foreach (Match aReturnStatement in allReturnStatements) {
                string value = aReturnStatement.Groups[2].Value;

                // value can't be a class instanciation, a char, a string, a numeric value, a variable name nor a boolean (because the first letter is uppercase)
                // it must then be a variable from another class or a function call (from this class or another)
                
                if (addedValues.Contains (value) || itemsAndTypes.ContainsKey (value) || value.Contains ("GetComponent") || value.Contains (".") || classesList.Contains (value) || collections.Contains (value) || genericCollections.Contains (value))
                    continue;
                else {
                    addedValues.Add (value);
                    writer.WriteLine (value+"=");
                }
            }
        }

        writer.Flush ();
        writer.Close ();
// You may want to edit the \"UnityScriptToCSharp/MyClasses.txt\" file before to proceed with the actual convertion of the files
        preparationState = "Preparation done. Added "+addedValues.Count+" entries to ItemsAndTypes.txt. Ready to convert "+scriptsList.Count+" scripts.";
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Read the file ItemsAndTypes.txt and extract the key/value pairs in the itemsAndTypes List
    /// </summary>
    /// <param name="getEmptyValues">Tell wether or not adding the keys without a value to the list</param>
    void GetItemsAndTypes (bool getEmptyValues) {
        itemsAndTypes.Clear ();
        
        string path = Application.dataPath+"/UnityScriptToCSharp/ItemsAndTypes.txt";

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

        // Replace ArrayList() (Array form Unity that works only for JS) by ArrayList ()
            patterns.Add ( "Array"+optWS+"\\(" );
            replacements.Add ( "ArrayList$1(" );
            patterns.Add ( ":"+optWS+"(Array)"+optWS+"=" );
            replacements.Add ( ":$1ArrayList$3=" );

        

        //Array gets converted to ArrayList, so convert .length to .Count
        pattern = "var"+oblWS+commonName+optWS+":"+optWS+"Array"+optWS+"=";
        MatchCollection allVariables = Regex.Matches (script.text, pattern);

        foreach (Match aVariable in allVariables) {
            string variableName = aVariable.Groups[2].Value;
            patterns.Add ( "("+variableName+optWS+"\\."+optWS+")length" );
            replacements.Add ( "$1Count" );
        }


        // search for string with single quotation mark
            //patterns.add("'(.{2,})'");
            //replacements.add("\"$1\"");


        // generic collections

            // Remove the dot in List<float>
            patterns.Add ( optWS+"\\."+optWS+"<" );
            replacements.Add ( "<" );

            // remove whitespaces after a chevron < and before a>
            patterns.Add ( genericCollections+optWS+"<"+optWS+commonChars );
            replacements.Add ( "$1<$4" );
            patterns.Add ( commonChars+optWS+">" );
            replacements.Add ( "$1>" );

            // remove space after the coma
            patterns.Add ( "<"+optWS+commonChars+optWS+","+optWS+commonChars+optWS+">" );
            replacements.Add ( "<$2,$5>" );

            // Remove the obligatory space between two>     Dictionary<string,List<string>> 
            patterns.Add ( ">"+optWS+">" );
            replacements.Add ( ">>" );





        // Loops

            // for ( in ) > foreach
            patterns.Add ( "for("+optWS+"\\(.+in"+oblWS+".+\\))" );
            replacements.Add ( "foreach$1" );

            // convert leftover "foreach (string"
            patterns.Add ( "(foreach"+optWS+"\\("+optWS+")string" );
            replacements.Add ( "$1string" );
        
            // foreach (Type name in array) > foreach (Type name in array)
            // patterns.Add ( "foreach"+optWS+"\\("+optWS+"var"+oblWS+commonName+optWS+":"+optWS+commonChars+"("+optWS+"in(.+)\\))" );
            // replacements.Add ( "foreach$1($7 $4$8" );


        // GetComponent (also works for GetComponentInChildren)

            //Getcomponent(T) GetComponent<T>() GetComponent<T>()  => Getcompoenent<T>()
            // GetComponent<T>() will have been modified to GetComponent<T>() at this point
            patterns.Add ( "(GetComponent|GetComponents|GetComponentInChildren|GetComponentsInChildren)"+optWS+"<"+optWS+commonChars+optWS+">" );
            replacements.Add ( "$1<$4>" );
        
            patterns.Add ( "(GetComponent|GetComponents|GetComponentInChildren|GetComponentsInChildren)"+optWS+"\\("+optWS+"[\"']{1}"+commonChars+"[\"']{1}"+optWS+"\\)" );
            replacements.Add ( "$1<$4>()" );

            patterns.Add ( "(GetComponent|GetComponents|GetComponentInChildren|GetComponentsInChildren)"+optWS+"\\("+optWS+commonChars+optWS+"\\)" );
            replacements.Add ( "$1<$4>()" );

            // convert var declaraion
            patterns.Add ( "var"+oblWS+commonName+"("+optWS+"="+optWS+"("+commonName+")?(GetComponent|GetComponents|GetComponentInChildren|GetComponentsInChildren)"+optWS+"<"+optWS+commonChars+optWS+">)" );
            replacements.Add ( "$11 $2$3" );

            // convert var declaraion
            patterns.Add ( "var"+oblWS+commonName+"("+optWS+"="+optWS+"("+commonName+")?(GetComponents|GetComponentsInChildren)"+optWS+"<"+optWS+commonChars+optWS+">)" );
            replacements.Add ( "$11[] $2$3" );


        // Yields

            patterns.Add ( "yield"+optWS+";" );
            replacements.Add ( "yield return 0;" );

            // yield return  ;
            patterns.Add (  "yield("+oblWS+commonChars+optWS+";)" );
            replacements.Add ( "yield return $2;" );
        
            // yield return new WaitForSeconds(3.5f);
            patterns.Add ( "yield"+oblWS+commonChars+optWS+"\\(" );
            replacements.Add ( "yield return new $2$3(" );

            patterns.Add ( "yield return new" );
            replacements.Add ( "yield return new" );


        // #pragma
        patterns.Add ( "\\#pragma"+oblWS+"(strict|implicit|downcast)" );
        replacements.Add ( "" );


        // replace .Length by .Length
        patterns.Add ( "\\."+optWS+"length" );
        replacements.Add ( ".Length" );


        // do the search/replace in script
        DoReplacements ();

    

        // Convert stuffs related to classes : declaration, inheritance, parent constructor call
        UnityScriptToCSharp_Classes.Classes ();
    
        // Add the "new" keyword before classes instanciation where it is missing
        UnityScriptToCSharp_Classes.AddNewKeyword ();

        // add the keyword public when no visibility (or just static) is set (the default visibility in JS is public but private in C#)
        // works also for functions
        UnityScriptToCSharp_Classes.AddVisibility ();

        // convert variables declarations
        // it will always resolve the variable type unless when the value is returned from a function (see VariablesTheReturn() void below)
        UnityScriptToCSharp_Variables.Variables ();

        // convert properties declarations
        UnityScriptToCSharp_Variables.Properties ();

        // convert void declarations, including arguments declaration
        UnityScriptToCSharp_Functions.Functions ();
   
        // convert variable declaration where the value is returned from a void now that almost all functions got their returned type resolved
        UnityScriptToCSharp_Variables.VariablesTheReturn ();

        // functionSec
        UnityScriptToCSharp_Functions.FunctionsTheReturn ();


        // Assembly imports
        // can"t do that in Classes() because it wouldn"t take into account the IEnumerator return type for the coroutine that may be added by Function()

            // move and convert the existing imports 
            pattern = "import"+oblWS+commonChars+";"; 
            MatchCollection matches = Regex.Matches (script.text, pattern);

            foreach (Match match in matches) {
                script.text = script.text.Insert (0, "using "+match.Groups[2].Value+";"+EOL).Replace (match.Value, "");
            }


            // add some using instruction based on terms found in the script
            // Generic Collections
            pattern = "using"+oblWS+"System"+optWS+"\\."+optWS+"Collections"+optWS+"\\."+optWS+"Generic"+optWS+";";
            if ( ! Regex.Match (script.text, pattern).Success) {
                // now check if I need to add this
                // just look for any Collections type in the script
                if (Regex.Match (script.text, genericCollections).Success)
                    script.text = script.text.Insert (0, "using System.Collections.Generic;"+EOL);
            }

            // Collections
            pattern = "using"+oblWS+"System"+optWS+"\\."+optWS+"Collections"+optWS+";";
            if ( ! Regex.Match (script.text, pattern).Success) {
                // now check if I need to add this
                // just look for any Collections type in the script.text
                if (Regex.Match (script.text, collections).Success)
                    script.text = script.text.Insert (0, "using System.Collections;"+EOL);
            }

            // UnityEngine
            pattern = "using"+oblWS+"UnityEngine"+optWS+";";
            if ( ! Regex.Match (script.text, pattern).Success)
               script.text = script.text.Insert (0, "using UnityEngine;"+EOL);


        // add typeof() where needed
            // new GameObject("name", typeof(Type));)
            patterns.Add ( "(new"+oblWS+"GameObject"+optWS+"\\(.*,"+optWS+")"+commonName+"("+optWS+"\\)"+optWS+";)" );
            replacements.Add ( "$1typeof($5)$6" );


        // ----------------------------------------------------------------------------------


        // we near the end of the convertion, it's time for patching things up 

            // convert leftover String and Boolean
            patterns.Add ( "((public|private|protected)"+oblWS+")String("+optWS+"\\["+optWS+"\\])?" );
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
			//replacements.Add ( "$1string");


        DoReplacements();

    } // end Convert()
} // end of class UnityScriptToCSharp_Main

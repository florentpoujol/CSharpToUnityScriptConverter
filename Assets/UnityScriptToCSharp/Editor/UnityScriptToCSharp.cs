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


public class UnityScriptToCSharp: EditorWindow {

    // the directory where to get the scripts to be converted
    private string sourceDirectory = "/UnityScriptToCSharp/ScriptsToBeConverted/";

    // the directory where to put the converted scripts
    private string targetDirectory = "/UnityScriptToCSharp/ConvertedScripts/";

    // a list of structure that contains all needed infos about the files to be converted
    private List<Script> filesList = new List<Script> ();

    // a list of classes that exists in the pool of files that will be converted
    private List<string> classesList = new List<string> ();

    // a list of items (variable or functions) and their coresponding type
    private Dictionary<string, string> itemsAndTypes = new Dictionary<string, string> ();

    // list of classes and their items (variable or function) and corresponding type
    private Dictionary<string, Dictionary<string, string>> projectItems = new Dictionary<string, Dictionary<string, string>> ();

    // the text (File.text) of the file currently being converted
    private string file = "";

    // the name (File.name) of the file currently being converted
    private string fileName = "";

    // index (in paths) of the file currently being converted
    private int fileIndex = 0;

    private bool proceedWithConvertion = false;


    //--------------------


    // regex that include most common expressions but arrays and generic collections
    private string commonName = "([A-Za-z0-9\\-_\\.]+)";

    // same as common name but includes also arrays and generic collections  
    private string commonChars = "([A-Za-z0-9<>,'\"\\-_\\[\\]\\.]+)"; // 
    private string commonCharsWithoutComma = "([A-Za-z0-9<>'\"\\-_\\[\\]\\.]+)"; 

    // white spaces (and/or tabs, and/or new line)
    private string optWS = "(\\s*)"; // optionnal white space   0 or more white space
    private string oblWS = "(\\s+)"; // obligatory white space  1 or more white space

    private string  collections = "(ArrayList|BitArray|CaseInsensitiveComparer|Comparer|Hashtable|Queue|SortedList|Stack|StructuralComparisons|DictionnaryEntry"+
        "|ICollection|IComparer|IDictionary|IDictionaryEnumerator|IEnumerable|IEnumerator|IEqualityComparer|IHashCodeProvider|IList|IStructuralComparable|IStructuralEquatable)";
    private string  genericCollections = "(Comparer|Dictionary|HashSet|KeyedByTypeCollection|LinkedList|LinkedListNode|List|Queue|SortedDictionary|SortedList|SortedSet|Stack|SynchronizedCollection"+
        "|SynchronizedKeyedCollection|SynchronizedReadOnlyCollection|ISet)";

    // list of the patterns and corresponding replacements to be processed by DoReplacements()
    private List<string> patterns = new List<string> ();
    private List<string> replacements = new List<string> ();
	private string pattern;
	
    private string EOL = "\r\n"; // works also with on Windows 7


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

        if (GUILayout.Button ("Do preparation work", GUILayout.MaxWidth (200))) {
            DoPreparations ();
        }

        GUILayout.Label (preparationState);

        GUILayout.Space (20);

        if (GUILayout.Button ("Begin actual convertion", GUILayout.MaxWidth (200))) {
            GetItemsAndTypes ();

            // loop throught files and convert
            convertionState = "Converting : 0%";
            
            if ( ! targetDirectory.StartsWith ("\\"))
                targetDirectory = "/"+targetDirectory;
            
            proceedWithConvertion = true;
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

        GUILayout.Label ("fileList.Count = "+filesList.Count);
        GUILayout.Label ("fileIndex = "+fileIndex);
        GUILayout.Label ("proceed with convertion ? "+proceedWithConvertion);

         if (GUILayout.Button ("Stop and reset", GUILayout.MaxWidth (200))) {
            Reset ();
        }
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// - Read all files and fill the "filesList" list
    /// - Make a list of all classes  (1) in the file "UnityClases",  (2) in the file "MyClasses" and  (3) encountered in the the project's scripts and fill the "classesList" list
    /// </summary>
    void DoPreparations () {
        preparationState = "Preparing ...";

        // get the path of all fles to be converted
        if ( ! sourceDirectory.StartsWith ("/"))
            sourceDirectory = "/"+sourceDirectory;

        if ( ! Directory.Exists (Application.dataPath+sourceDirectory)) {
            convertionState = "Abording ! Source directory does not exists !";
            return;
        }

        string[] paths = Directory.GetFiles (Application.dataPath+sourceDirectory, "*.js", SearchOption.AllDirectories);
        StreamReader reader;
        filesList.Clear ();

        // fill filesList
        foreach (string path in paths) {
            reader = new StreamReader (path);
            filesList.Add (new Script (path.Replace (Application.dataPath+sourceDirectory, ""), reader.ReadToEnd ()));
            reader.Close ();
        }
        

        //--------------------


        // make a list of all encountered class in the project's scripts
        foreach (Script file in filesList) {
            // search for class declaration pattern
            pattern = "class"+oblWS+commonName+"("+oblWS+"extends"+oblWS+commonName+")?"+optWS+"{";
            MatchCollection allClassesDeclarations = Regex.Matches (file.text, pattern);

            foreach (Match aClassDeclaration in allClassesDeclarations) {
                string className = aClassDeclaration.Groups[2].Value;

                if ( ! classesList.Contains (className))
                    classesList.Add (className);
            }

            // always add the name of the file (in most cases, that's the name of the class inside the file)
            if ( ! classesList.Contains (file.name))
                classesList.Add (file.name);
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

        // do the same but for the C# scripts in the project folder
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
        string path2 = Application.dataPath+"/UnityScriptToCSharp/UnityClasses.txt";
        if (File.Exists (path2)) { // StreamReader will throw an FileNotFoundException if the file is not found
            reader = new StreamReader (path2);

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
        path2 = Application.dataPath+"/UnityScriptToCSharp/MyClasses.txt";
        if (File.Exists (path2)) {
            reader = new StreamReader (path2);

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

        foreach (Script file in filesList) {
            // search for variable declaration pattern
            pattern = "var"+oblWS+commonName+optWS+"="+optWS+"([a-zA-Z_]{1}"+commonName+")("+optWS+"\\(.*\\))?"+optWS+";";
            MatchCollection allVarDeclarations = Regex.Matches (file.text, pattern);

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
            pattern = "return"+oblWS+"([A-Z]{1}"+commonName+")("+optWS+"\\(.*\\))?"+optWS+";";
            MatchCollection allReturnStatements = Regex.Matches (file.text, pattern);

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
        preparationState = "Preparation done. Ready to convert "+filesList.Count+" scripts.";
    }


    // ----------------------------------------------------------------------------------


    void DoConvertion () {
        
    }


    void Update() {
        Script _file;

        if (proceedWithConvertion && fileIndex < filesList.Count) {
            _file = filesList[fileIndex++];
            file = _file.text;
            fileName = _file.name;

            ConvertFile ();
            Debug.Log ("Convertion done for ["+_file.path+_file.name+".cs]." );

            string path = Application.dataPath+targetDirectory+_file.path;
            //Debug.Log (path);
            Directory.CreateDirectory (path);

            StreamWriter writer = new StreamWriter (path+_file.name+".cs");
            writer.Write (file);
            writer.Flush ();
            writer.Close ();

            convertionState = "Converting : "+(fileIndex+0.0 / filesList.Count+0.0 * 100.0)+"%";

            if (fileIndex == filesList.Count-1)
                convertionState = "Convertion done !";
        }
    }

    // ----------------------------------------------------------------------------------

    /// <summary>
    /// 
    /// </summary>
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


    void Reset () {
        convertionState = "Resetted";
        proceedWithConvertion = false;
        fileIndex = 0;
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Process the patterns/replacements
    /// </summary>
    void DoReplacements () {
        file = DoReplacements (file);
    }

    string DoReplacements (string text) {
        for (int i = 0; i < patterns.Count; i++)
            text = Regex.Replace (text, patterns[i], replacements[i]);

        patterns.Clear ();
        replacements.Clear ();
        return text;
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Do a Regex.Matches but return the result in the inverse order
    /// </summary>
    List<Match> ReverseMatches (string text, string pattern) {
        MatchCollection matches = Regex.Matches (text, pattern);
        Stack stack = new Stack ();

        foreach (Match match in matches)
            stack.Push (match);

        // the matches piles up in the stack, so the lastest match in matches is now the first one in stack

        List<Match> newMatches = new List<Match> ();

        foreach (Match match in stack)
            newMatches.Add (match);

        return newMatches;
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
        pattern = "var"+oblWS+commonName+optWS+":"+optWS+"ArrayList"+optWS+"=";
        MatchCollection allVariables = Regex.Matches (file, pattern);

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


        // do the search/replace
        DoReplacements ();

    

        // Convert stuffs related to classes : declaration, inheritance, parent constructor call
        Classes ();
    
        // Add the "new" keyword before classes instanciation where it is missing
        AddNewKeyword ();

        // add the keyword public when no visibility (or just static) is set (the default visibility in JS is public but private in C#)
        // works also for functions
        AddVisibility ();

        // convert variables declarations
        // it will always resolve the variable type unless when the value is returned from a function (see VariablesTheReturn() void below)
        Variables ();

        // convert properties declarations
        Properties ();

        // convert void declarations, including arguments declaration
        Functions ();
   
        // convert variable declaration where the value is returned from a void now that almost all functions got their returned type resolved
        VariablesTheReturn ();

        // functionSec
        FunctionsTheReturn ();


        // Assembly imports
        // can"t do that in Classes() because it wouldn"t take into account the IEnumerator return type for the coroutine that may be added by Function()

            // move and convert the existing assemblies 
            pattern = "import"+oblWS+commonChars+";"; 
            MatchCollection matches = Regex.Matches (file, pattern);

            foreach (Match match in matches) {
                file = file.Insert (0, "using "+match.Groups[2].Value+";"+EOL).Replace (match.Value, "");
            }


            // add some using instruction based on terms found in the file
            // Generic Collections
            pattern = "using"+oblWS+"System"+optWS+"\\."+optWS+"Collections"+optWS+"\\."+optWS+"Generic"+optWS+";";
            if ( ! Regex.Match (file, pattern).Success) {
                // now check if I need to add this
                // just look for any Collections type in the file
                //pattern = "(Comparer<|Dictionary<|HashSet<|KeyedByTypeCollection<|LinkedList<|LinkedListNode<|List<|Queue<|SortedDictionary<|SortedList<|SortedSet<|Stack<|SynchronizedCollection<|SynchronizedKeyedCollection<|SynchronizedReadOnlyCollection<)";

                if (Regex.Match (file, genericCollections).Success)
                    file = file.Insert (0, "using System.Collections.Generic;"+EOL);
            }

            // Collections
            pattern = "using"+oblWS+"System"+optWS+"\\."+optWS+"Collections"+optWS+";";
            if ( ! Regex.Match (file, pattern).Success) {
                // now check if I need to add this
                // just look for any Collections type in the file
                //pattern = "(ArrayList|BitArray|CaseInsensitiveComparer|Comparer|Hashtable|Queue|SortedList|Stack|StructuralComparisons|DictionnaryEntry)";

                if (Regex.Match (file, collections).Success)
                    file = file.Insert (0, "using System.Collections;"+EOL);
            }

            // UnityEngine
            pattern = "using"+oblWS+"UnityEngine"+optWS+";";
            if ( ! Regex.Match (file, pattern).Success)
               file = file.Insert (0, "using UnityEngine;"+EOL);


        // add typeof() where needed
            // new GameObject("name", typeof(Type));)
            patterns.Add ( "(new"+oblWS+"GameObject"+optWS+"\\(.*,"+optWS+")"+commonName+"("+optWS+"\\)"+optWS+";)" );
            replacements.Add ( "$1typeof($5)$6" );


        // time for patching things up 

            // convert leftover string and Boolean
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

            // System.String got replaced by System.string
            patterns.Add ( "(\\."+optWS+")Tostring("+optWS+"\\()" );
            replacements.Add ( "$1ToString$3" );

			// single quotation have to be replaced by double quotation marks 
			// but I can't just do :
			// patterns.Add ("'(.{2,})'");
			// replacements.Add ("\"$1\"");
			// it would cause to many artifacts
			
			// string
			patterns.Add ("return"+oblWS+"'(.+)'"+optWS+";");
			replacements.Add ("return$1\"$2\"$3;");

			// return char
			//patterns.Add ("return"+oblWS+"(\"|'){1}(.+)(\"|'){1}"+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+";");
			//replacements.Add ("return$1'$2'$3;");
			
			
            // bugs/artefacts
			//patterns.Add ( "((public|private|protected)"+oblWS+")ring" );
			//replacements.Add ( "$1string");


        DoReplacements ();
    }


    // ----------------------------------------------------------------------------------

    /// <summary> 
    /// Convert stuffs related to classes : declaration, inheritance, parent constructor call, Assembly imports
    /// </summary>
    void Classes () {
        // loop the classes declarations
        string pattern = "class"+oblWS+commonName+"("+oblWS+"extends"+oblWS+commonName+")?"+optWS+"{";
		//MatchCollection classesMatches = Regex.Matches (file, pattern);
		//MatchCollection matches;
        //int fileOffset = 0;
        
        List<Match> allClasses = ReverseMatches (file, pattern);

        // list of functions and variable declaration that are found within a class
        int itemsInClasses = 0;
        
        foreach (Match aClass in allClasses) {
			Block _class = new Block (aClass, file);
			
			if (_class.isEmpty)
				continue;

            // look for constructors in the modified class
			pattern = _class.name+optWS+"\\(.*\\)"+optWS+"{";
			//MatchCollection constructorsMatches = Regex.Matches (_class.newText, pattern);
			//int classNextTextOffset = 0;

            List<Match> allConstructors = ReverseMatches (_class.text, pattern); // all constructors in that class
            _class.newText = _class.text;

			foreach (Match aConstructor in allConstructors) {
				// here, we are inside one of the constructors of the current class (_class)
				Block constructor = new Block (aConstructor, _class.text);
				// start and end index are relative to _class.text, not relative to file !


                // first task :
                // look for parent constructor call : super();
                pattern = "super"+optWS+"\\((?<args>.*)\\)"+optWS+";";
				Match theParentClassConstructorCall = Regex.Match (constructor.text, pattern);

				if (theParentClassConstructorCall.Success) {
                    constructor.newText = constructor.text.Replace (theParentClassConstructorCall.Value, ""); 

					_class.newText = _class.newText.Insert (constructor.startIndex-1, " : base("+theParentClassConstructorCall.Groups["args"].Value+")");
                    _class.newText = _class.newText.Replace (constructor.text, constructor.newText);

                    continue;
                }


                // second task :
                // look for alternate constructor call for that current class : NameOfTheClass ( );
                pattern = _class.name+optWS+"\\((?<args>.*)\\)"+optWS+";";
                Match alternateConstructorCall = Regex.Match (constructor.text, pattern);
                

                if (alternateConstructorCall.Success) {
                    // add the new syntax to _class.newText
                    _class.newText = _class.newText.Insert (constructor.startIndex-1, " : this("+alternateConstructorCall.Groups["args"].Value+")");
                    
                    constructor.newText = constructor.text.Replace (alternateConstructorCall.Value, ""); 
                    _class.newText = _class.newText.Replace (constructor.text, constructor.newText);

                }

            } // end looping throught constructors of _class
            
			// we won't do more search/replace for this class _class.newText
			// now replace in file, the old _class.text by the new
			file = file.Replace (_class.text, _class.newText);
			//fileOffset = (_class.text.Length - _class.newText.Length);


            //--------------------

            // makes the count of all functions and variables inside the class 
            pattern = "function"+oblWS+commonName+optWS+"\\(";
            //allFunctions = Regex.Matches (_class.text, pattern); // it doesn't matter here if the search is done in _class.text or newText beau the have thse functions and variables
            itemsInClasses += Regex.Matches (_class.text, pattern).Count;
			//foreach (Match function in matches)
            //    classesContent.Add (function.Groups[2].Value);

            pattern = "var"+oblWS+commonName+optWS+"(:|;|=)";
            itemsInClasses += Regex.Matches (_class.text, pattern).Count;
			//matches = Regex.Matches (_class.text, pattern);

            //foreach (Match variable in matches)
			//	classesContent.Add (variable.Groups[2].Value);
		} // end looping through classes      foreach (Match classMatch in classesMatches)


        // we made a list of functions and variables inside classes
        // now make a list of functions and variable inside the file ...
        int itemsInFile = 0;

        pattern = "function"+oblWS+commonName+optWS+"\\(";
        itemsInFile += Regex.Matches (file, pattern).Count;
        //matches = Regex.Matches (file, pattern);

        //foreach (Match function in matches)
		//	fileContent.Add (function.Groups[2].Value);

        pattern = "var"+oblWS+commonName+optWS+"(:|;|=)";
        itemsInFile += Regex.Matches (file, pattern).Count;
        //matches = Regex.Matches (file, pattern);

        //foreach (Match variable in matches)
		//	fileContent.Add (variable.Groups[2].Value);


        // ... compare the two list
        // if there is a difference, that mean that some variable or function declaration lies outside a public class
        // that means that the file is a MonoBehaviour derived public class
		if (itemsInClasses != itemsInFile) {// no class declaration within the file
            file = file.Insert (0, EOL+"public class "+fileName+" : MonoBehaviour {"+EOL);
            file = file+EOL+"} // end of class "+fileName; // the closing public class bracket
        }


        // Attributes / Flags
            // move some of them to the beginning of the file before converting
            pattern = "@?"+optWS+"(script"+oblWS+")?RequireComponent"+optWS+"\\("+optWS+commonChars+optWS+"\\)("+optWS+";)?";
            MatchCollection matches = Regex.Matches (file, pattern);
            foreach (Match match in matches)
				file = file.Replace (match.Value, ""). Insert (0, match.Value+EOL);
        
            pattern = "@?"+optWS+"(script"+oblWS+")?ExecuteInEditMode"+optWS+"(\\("+optWS+"\\))?("+optWS+";)?";
            matches = Regex.Matches (file, pattern);
            foreach (Match match in matches)
                file = file.Replace (match.Value, "").Insert (0, "[ExecuteInEditMode]"+EOL);

            pattern = "@?"+optWS+"(script"+oblWS+")?AddComponentMenu(.*\\))("+optWS+";)?";
            matches = Regex.Matches (file, pattern);
            foreach (Match match in matches)
                file = file.Replace (match.Value, "").Insert (0, match.Value+EOL);


			patterns.Add ( "@"+optWS+"script" );
            replacements.Add ( "@" );

            //patterns.Add ( "@"+optWS+"script"+optWS+"AddComponentMenu("+optWS+"\\("+optWS+"\""+optWS+commonChars+optWS"\""+optWS+"\\))" );
            //patterns.Add ( "@"+optWS+"AddComponentMenu(.*\\))("+optWS+";)?" );
            //replacements.Add ( "[AddComponentMenu$2]" );

            //  => [RequireComponent (typeof(T))]
			patterns.Add ( "@?"+optWS+"RequireComponent"+optWS+"\\("+optWS+commonChars+optWS+"\\)("+optWS+";)?" );
			replacements.Add ( "[RequireComponent$2(typeof($4))]" );

            //  => [ExecuteInEditMode]
            // patterns.Add ( "@"+optWS+"ExecuteInEditMode"+optWS+"\\("+optWS+"\\)("+optWS+";)?" );
            // replacements.Add ( "[ExecuteInEditMode]" );
       

            // flags  [something (something2) => [something (something2)]]    [something] => [something]
            patterns.Add ( "@"+optWS+commonName+"("+optWS+"\\(.*\\))?("+optWS+";)?" );
            replacements.Add ( "[$2$3]" );



        // struct
        // in JS, the way to define struct is to makes a public class inherits from System.ValueType
        patterns.Add ( "class"+oblWS+commonName+oblWS+"extends"+oblWS+"System"+optWS+"\\."+optWS+"ValueType" );
        replacements.Add ( "struct$1$2" );

        // public class inheritance
        patterns.Add ( oblWS+"extends"+oblWS+commonName );
        replacements.Add ( "$1:$2$3" );

        // base. => base.      
        patterns.Add ( "super"+optWS+"\\." );
        replacements.Add ( "base$1." );


        DoReplacements ();
    } // end Classes()


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Add the "new" keyword before classes instanciation where it is missing
    /// </summary>
    void AddNewKeyword () {
        // get pattern like "Type name = ClassOrMethod ();"  and search for "Class name = Class ();"
        pattern = "var"+oblWS+commonName+optWS+":"+optWS+commonChars+"("+optWS+"="+optWS+commonChars+optWS+"\\(.*\\)"+optWS+";)";
        List<Match> allMatches = ReverseMatches (file, pattern);

        foreach (Match match in allMatches)
            if (match.Groups[5].Value == match.Groups[9].Value) // if the type == the class/method name
                file = file.Insert (match.Groups[9].Index, "new "); // add "new " in front of Class ()

        
        //also add a new keyword in front of collections
        pattern = "="+optWS+collections+optWS+"\\("; // when setting the value of a variable
        InsertInPatterns (pattern, 1, " new");


        pattern = "return"+oblWS+collections+optWS+"\\("; // when returning an empty instance
        allMatches = ReverseMatches (file, pattern);

        foreach (Match match in allMatches)
            file = file.Insert (match.Groups[2].Index, "new ");


        // and Generic collections
        pattern = "="+optWS+genericCollections+"<"+commonChars+">"+optWS+"\\(";
        InsertInPatterns (pattern, 1, " new");


        pattern = "return"+oblWS+genericCollections+optWS+"\\(";
        allMatches = ReverseMatches (file, pattern);
        
        foreach (Match match in allMatches)
            file = file.Insert (match.Groups[2].Index, "new ");


        // and classes in ClassesList
        foreach (string className in classesList) {
            pattern = "="+optWS+className+optWS+"\\(";
            InsertInPatterns (pattern, 1, " new");

            // do the same with return keyword
            pattern = "return"+oblWS+className+optWS+"\\(";
            allMatches = ReverseMatches (file, pattern);
            
            foreach (Match match in allMatches)
                file = file.Insert (match.Groups[2].Index, "new ");
        }
    } // end AddNewKeyword ()


    // ----------------------------------------------------------------------------------

    /// <summary> 
    /// Insert [text] at the fixed position [patternOffset] in all [pattern]s found
    /// </summary>
    void InsertInPatterns (string pattern, int patternOffset, string text) {
        List<Match> allMatches = ReverseMatches (file, pattern);

        foreach (Match match in allMatches)
            file = file.Insert (match.Index + patternOffset, text);
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Add the keyword public when no visibility (or just static) is set (the default visibility in JS is public but private in C#)
    /// Works also for functions
    /// </summary>
    void AddVisibility () {
        // the default visibility for variable and functions is public in JS but private in C# => add the keyword public when no visibility (or just static) is set 
        patterns.Add ( "([;{}\\]]+"+optWS+")((var|function|enum|class)"+oblWS+")" );
        replacements.Add ( "$1public $3" );

        patterns.Add ( "(\\*/"+optWS+")((var|function|enum|class)"+oblWS+")" );
        replacements.Add ( "$1public $3" );

        patterns.Add ( "(//.*"+optWS+")((var|function|enum|class)"+oblWS+")" );
        replacements.Add ( "$1public $3" );

        patterns.Add ( "((\\#else|\\#endif)"+oblWS+")((var|function|enum|class)"+oblWS+")" );
        replacements.Add ( "$1public $4" );


        // static
        patterns.Add ( "([;{}\\]]+"+optWS+")static"+oblWS+"((var|function)"+oblWS+")" );
        replacements.Add ( "$1public static $4" );

        patterns.Add ( "(\\*/"+optWS+")static"+oblWS+"((var|function)"+oblWS+")" );
        replacements.Add ( "$1public static $4" );

        patterns.Add ( "(//.*"+optWS+")static"+oblWS+"((var|function)"+oblWS+")" );
        replacements.Add ( "$1public static $4" );

        patterns.Add ( "((\\#else|\\#endif)"+oblWS+")((var|function)"+oblWS+")" );
        replacements.Add ( "$public static $4" );

        DoReplacements ();


        // all variable gets a public or static public visibility but this shouldn't happend inside functions, so remove that

        pattern = "function"+oblWS+commonName+optWS+"\\(.*\\)"+optWS+"(:"+optWS+commonChars+optWS+")?{";
        List<Match> allFunctions = ReverseMatches (file, pattern);

        foreach (Match aFunction in allFunctions) {
            Block function = new Block (aFunction, file);

            if (function.isEmpty)
                continue;

            patterns.Add ( "public"+oblWS+"(static"+oblWS+"var)" );
            replacements.Add ( "$2" );
            patterns.Add ( "(static"+oblWS+")?public"+oblWS+"var" );
            replacements.Add ( "$1var" );

            function.newText = DoReplacements (function.text);
            file = file.Replace (function.text, function.newText);
            //fileOffset += (function.newText.Length-function.text.Length);
           
        } // end for
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Search for and convert variables declarations
    /// </summary>
    void Variables () {
        // add an f at the end of a float (and double) value 
        patterns.Add ( "([0-9]+\\.{1}[0-9]+)(f|F){0}" );
        replacements.Add ( "$1f" );


        // arrays

            // replace square brackets by curly brackets
            // works for variable declaration and litteral array in foreach loop "foreach(type var in {array})"

            patterns.Add ( "(=|in)"+optWS+"\\[(.*)\\]"+optWS+"(;|\\))" );
            replacements.Add ( "$1$2{$3}$4$5" );

            // replace signle quotation marks by double quotation marks (between brackets)
                patterns.Add ( "({|,){1}"+optWS+"'{1}"+commonCharsWithoutComma+"'{1}"+optWS+"(}|,){1}" );
                replacements.Add ( "$1$2\"$3\"$4$5" );
                // now, as regex doesn't overlap themselves, only half of the argument have been convertes
                // I need to run the regex again
                patterns.Add ( "({|,){1}"+optWS+"'{1}"+commonCharsWithoutComma+"'{1}"+optWS+"(}|,){1}" );
                replacements.Add ( "$1$2\"$3\"$4$5" );


            // array with type declaration (with or without value setting)
            // arrays with type declaration without space    "string[]" instead of "string [ ]"" are already converted because [ and ] are among commonChars

                // string 
                patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"String"+optWS+"\\["+optWS+"\\]"+optWS+"(=|;)" );
                replacements.Add ( "string[] $2$7$8" );

                // bool
                patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"boolean"+optWS+"\\["+optWS+"\\]"+optWS+"(=|;)" );
                replacements.Add ( "bool[] $2$7$8" );

                // general case
                patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+commonName+optWS+"\\["+optWS+"\\]"+optWS+"(=|;)" );
                replacements.Add ( "$5[] $2$8$9" );


            // arrays with value setting but no type declaration. Have to guess the type with the value's look
            // square brackets have already been converted to curly brackets by now

                // string
                patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"{"+optWS+"((\"|'){1}"+commonCharsWithoutComma+"(\"|'){1}"+optWS+",?"+optWS+")*"+optWS+"})" );
                replacements.Add ( "string[] $2" );

                // char
                patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"{"+optWS+"((\"|'){1}"+commonCharsWithoutComma+"(\"|'){1}"+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+",?"+optWS+")*"+optWS+"})" );
                replacements.Add ( "char[] $2" );

                // replace "a'[0] and "a"[0] by 'a" in char[] declaration
                patterns.Add ( "({|,){1}"+optWS+"(\"|'){1}"+commonCharsWithoutComma+"(\"|'){1}"+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+"(}|,){1}" );
                replacements.Add ( "$1$2'$4'$9$10" );
                // now, as regex doesn't overlap themselves, only half of the argument have been converted
                // I need to run the regex again
                patterns.Add ( "({|,){1}"+optWS+"(\"|'){1}"+commonCharsWithoutComma+"(\"|'){1}"+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+"(}|,){1}" );
                replacements.Add ( "$1$2'$4'$9$10" );

                // bool
                patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"{"+optWS+"((true|false)"+optWS+",?"+optWS+")*"+optWS+"})" );
                replacements.Add ( "bool[] $2" );

                //int
                patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"{"+optWS+"(-?[0-9]+"+optWS+",?"+optWS+")*"+optWS+"})" );
                replacements.Add ( "int[] $2" );

                //float
                patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"{"+optWS+"(-?[0-9]+\\.{1}[0-9]+(f|F){1}"+optWS+",?"+optWS+")*"+optWS+"})" );
                replacements.Add ( "float[] $2" );
 

            // empty arrays declarations without type declaration  bool[] _array11 = new bool[ 4] ;
            
                // string
                patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"new"+oblWS+")String("+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+";)" );
                replacements.Add ( "string[] $2string$7" );

                // // replace leftover ": string[" or "new string["
                // // patterns.Add ( "(new|:)"+optWS+"string("+optWS+"\\[)" );
                // // replacements.Add ( "$1$2string$3" );

                // bool
                patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"new"+oblWS+")boolean("+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+";)" );
                replacements.Add ( "bool[] $2bool$7" );
            

                // general case
                patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"new"+oblWS+commonName+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+";)" );
                replacements.Add ( "$7[] $2" );


        // ----------------------------------------------------------------------------------


        // variable with type declaration but no value setting      string test;

            // particular case : string     string _string2;
            patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"String"+optWS+";" );
            replacements.Add ( "string $2$5;" );

            // particular case : Boolean     bool _bool2;
            // works also for bool with value setting
            // patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"bool"+optWS+";" );
            // replacements.Add ( "bool $2$5;" );

            // general case :     int _int3;    also works for arrays   int[] _array;
            // patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+commonChars+optWS+";" );
            // replacements.Add ( "$5 $2$6;" );

            // others cases than string are actually handled below :

        // variables with type declaration and value setting    int test = 5;

            // particular case : string    put string lowercase and replace "' by ""       string _string6 = "& ";
            patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"String"+optWS+"="+optWS+"(\"|'){1}(.*)(\"|'){1}"+optWS+";" );
            replacements.Add ( "string $2$5=$6\"$8\"$10;" );

            // particular case : char       char _char4 = 'a'[0];    char _char5 = "a';    => char _char4 = 'a";
            patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"char"+optWS+"="+optWS+"(\"|'){1}(.*)(\"|'){1}"+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+";" );
            replacements.Add ( "char $2$5=$6'$8'$13;" );

            // particular case : bool    bool _bool3 = true;
            patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"boolean"+optWS+"(=|;|in)" );
            replacements.Add ( "bool $2$5$6" );

            // particular case : float
			//patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"float"+optWS+"="+optWS+"(-?[0-9]+\\.[0-9]+(f|F){1})"+optWS+";" );
			//replacements.Add ( "float $2$5=$6$7$8;" );

            // general case for variable with type definition.   
            // Also works in foreach loops 
            patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+commonChars+optWS+"(=|;|in)" );
            replacements.Add ( "$5 $2$6$7" );
            // This would Also works for some arrays when there is not space around and between square brackets
            // but it would also really fuck up with string[]

			// remove the f at the end of value when it's a double
			patterns.Add ("(double"+oblWS+commonName+optWS+"="+optWS+"-?[0-9]+\\.[0-9]+)(f|F){1}"+optWS+";");
			replacements.Add ("$1$7;");


        // variable with value setting but no type declaration. Have to guess the type with the value's look
    
            // string
            patterns.Add ( "var"+oblWS+commonName+optWS+"="+optWS+"(\"|'){1}(.*)(\"|'){1}"+optWS+";" );
            replacements.Add ( "string $2$3=$4\"$6\"$8;" );

            // char    char _char2 = 'a'[0];    var _char3 = "a';
            patterns.Add ( "var"+oblWS+commonName+optWS+"="+optWS+"(\"|'){1}(.*)(\"|'){1}"+optWS+"(\\["+optWS+"[0-9]+"+optWS+"\\])"+optWS+";" );
            replacements.Add ( "char $2$3=$4'$6'$12;" );

            // bool
            patterns.Add ( "var("+oblWS+commonName+optWS+"="+optWS+"(true|false)"+optWS+";)" );
            replacements.Add ( "bool$1" );

            // int
            patterns.Add ( "var("+oblWS+commonName+optWS+"="+optWS+"-?[0-9]+"+optWS+";)" ); // long will be converted to int
            replacements.Add ( "int$1" );

            // float     value already contains an f or F   3.5f  or 1.0f
            patterns.Add ( "var("+oblWS+commonName+optWS+"="+optWS+"-?[0-9]+\\.{1}[0-9]+(f|F){1}"+optWS+";)" );
            replacements.Add ( "float$1" );
        

        // variable without type declaration or value setting

            // if a variable name begins by "is" there is a chance that's a bool
            patterns.Add ( "var("+oblWS+"is"+commonName+optWS+";)" );
            replacements.Add ( "bool$1" );

            // if a variable name ends by "Rect" there is a chance that's a Rect
            patterns.Add ( "var("+oblWS+commonName+"Rect"+optWS+";)" );
            replacements.Add ( "Rect$1" );

            // one thing I could do is guessing the type the first time the value is set


        //--------------------


        // other types (classes instantiation)    Test _test3 = new Test();
        // "new" keywords are already added everywhere they are needed by the method "AddNewKeyword()"
        patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"new"+oblWS+commonChars+optWS+"\\((.*);)" );
        replacements.Add ( "$7 $2" );

		
        //--------------------


        // var declaration vithout a type and the value comes from a function :
        // The type can be resolved if the function exists in itemsAndTypes (see below) or if the function declaration is done in the file, 
        // As UnityScript allows not to specify which type returns a void, wait until the functions declarations are processed (in Functions(), below Properties()) to try to convert those variables

        // meanwhile, check for values and function calls that are within itemsAndTypes
        foreach (KeyValuePair<string, string> item in itemsAndTypes) {
            if (file.Contains (item.Key)) { // it just reduce the number of elements in patterns and replacements lists
                
                // if the item is a method
                patterns.Add ( "var("+oblWS+commonName+optWS+"="+optWS+item.Key+optWS+"(\\(.*\\)"+optWS+")?;)" );
                replacements.Add ( item.Value+"$1" );
                
                // if the item is a variable
                //patterns.Add ( "var("+oblWS+commonName+optWS+"="+optWS+item.Key+optWS+";)" );
                //replacements.Add ( item.Value+"$1" );
            }
        }
        // about the same code is run again in Function()


        //--------------------


        //casting while using Instanciate method
        patterns.Add ( "("+commonName+oblWS+commonName+optWS+"="+optWS+")(Instantiate"+optWS+"\\()" );
        replacements.Add ( "$1($2)$7" );

        
        //--------------------





        //--------------------


        // patching time !
        // sometimes float value gets 2 f ??????
            patterns.Add ("([0-9]+\\.{1}[0-9]+)(f|F){2,}");
            replacements.Add ("$1f");


        DoReplacements ();
    } // end VariablesDeclarations ()


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Convert Properties declarations
    /// </summary>
    void Properties () {
        // change string return type to string, works also for functions (properties in JS are functions)
        patterns.Add ( "(\\)"+optWS+":"+optWS+")String" );
        replacements.Add ( "$1string" );

        // change bool return type to bool
        patterns.Add ( "(\\)"+optWS+":"+optWS+")boolean" );
        replacements.Add ( "$1bool" );

        DoReplacements ();


        //--------------------


        // first, get all property getters (if a property exists, I assume that a getter always exists for it)
        pattern = "(?<visibility>public|private|protected)"+oblWS+"function"+oblWS+"get"+oblWS+"(?<propName>"+commonName+")"+optWS+"\\("+optWS+"\\)"+optWS+":"+optWS+"(?<returnType>"+commonChars+")"+optWS+
        "{"+optWS+"return"+oblWS+"(?<varName>"+commonName+")"+optWS+";"+optWS+"}";
        List<Match> allGetters = ReverseMatches (file, pattern);
        
        string unModifiedFile = file;

        foreach (Match aGetter in allGetters) {
            string propName = aGetter.Groups["propName"].Value;
            string variableName = aGetter.Groups["varName"].Value;

            // now I have all infos nedded to start building the property in C#
            // match.Groups[1].Value is the getter's visibility. It always exists by now because it has been added by the AddVisibility() method above
            // match.Groups[10].Value is the getter's return type
            string property = aGetter.Groups["visibility"].Value+" "+aGetter.Groups["returnType"].Value+" "+propName+" {"+EOL 
            +"\t\tget { return "+variableName+"; }"+EOL; // getter


            // now look for the corresponding setter
            pattern = "(public|private|protected)"+oblWS+"function"+oblWS+"set"+oblWS+propName+".*}";
            Match theSetter = Regex.Match (unModifiedFile, pattern);

            if (theSetter.Success)
                property += "\t\t"+theSetter.Groups[1].Value+" set { "+variableName+" = value; }"+EOL; // setter

            property +="\t}"+EOL; // property closing bracket


            // now do the modifs in the file
            file = file.Replace (aGetter.Value, property); // replace getter by property

            if (theSetter.Success)
                file = file.Replace (theSetter.Value, ""); // remove setter if it existed
        }
    } // end Properties ()


    // ----------------------------------------------------------------------------------

    /// <summary> 
    /// Convert function declarations
    /// Try to resolve the return type (if any) when there is none declared with the look of what is returned
    /// </summary>
    void Functions () {
        // UnityScript allow arugments without types, when they are not needed
        // just remove them
        //patterns.Add ( "(function"+oblWS+commonName+optWS+")(\\(|,){1}"+optWS+"[a-zA-z\\.]+"+optWS+"(\\)|,){1}" );
        patterns.Add ( "(function"+oblWS+commonName+optWS+")\\("+optWS+"[a-zA-z_\\.]+"+optWS+"\\)" );
        replacements.Add ( "$1()" );


        // arguments declaration
        patterns.Add ( "(\\(|,){1}"+optWS+commonName+optWS+":"+optWS+commonCharsWithoutComma+optWS+"(\\)|,){1}" );
        replacements.Add ( "$1$2$6 $3$7$8" );
        // as regex doesn't overlap themselves, only half of the argument have been convertes
        // I need to run the regex 
        patterns.Add ( "(\\(|,){1}"+optWS+commonName+optWS+":"+optWS+commonCharsWithoutComma+optWS+"(\\)|,){1}" );
        replacements.Add ( "$1$2$6 $3$7$8" );



        // search for return keyword to see if the void actually returns something, then try to resolve the return type with the returned variable type or returned value
        // if the void does not return anything, a "void" return type is added
        // if the type can't be resolved, a "MISSING_RETURN_TYPE" return type is added
        // coroutines gets a IEnumerator return type and thair call is wrapped by StartCoroutine( )

        // look for all JS functions that has no explicit return type
        pattern = " function"+oblWS+commonName+optWS+"\\(.*\\)"+optWS+"{"; 
        List<Match> allFunctions = ReverseMatches (file, pattern);

		foreach (Match aFunction in allFunctions) {
            //Debug.Log ("aFunction="+aFunction.Value+" ["+file[aFunction.Index-1]+file[aFunction.Index]+file[aFunction.Index+1]+"]");

			Block function = new Block (aFunction, file);
			function.name = aFunction.Groups[2].Value;


            // look for return keyword patterns
            pattern = "return"+oblWS+".+;";

            // if there is none, add ""
			if ( ! Regex.Match (function.text, pattern).Success) { // no return keyword : add "void" return type
                file = file.Replace (function.declaration, function.declaration.Replace (" function ", " void ")); 
                continue;
            }

        
            // Below this point, we know that the function returns some value but we don't know the type yet
            // Look in function.text for several return patterns 
        
            // yield / coroutine
            pattern = "yield"+oblWS+"return";
            
			if (Regex.Match (function.text, pattern).Success) { 
                file = file.Replace (function.declaration, function.declaration.Replace (" function ", " IEnumerator "));

                // In C# (and Boo), a coroutine call must be wrapped by the StartCoroutine() method : StartCoroutine( CoroutineName() );
                // The current function is a coroutine, so search for it's call in the file
                patterns.Add ( "("+function.name+optWS+"\\(.*\\))"+optWS+";" );
				replacements.Add ( "StartCoroutine( $1 );" );

                continue;
            }


            // this pattern will match an int, a float, a bool or a variable name
            pattern = "return"+oblWS+commonName+optWS+";";
			Match variableMatch = Regex.Match (function.text, pattern);
            string variableName = "";

			if (variableMatch.Success) {
				variableName = variableMatch.Groups[2].Value;
				
                // bool
                if (Regex.Match (variableName, "^(true|false)$").Success) { 
                    file = file.Replace (function.declaration, function.declaration.Replace (" function ", " bool "));
                    continue;
                }

                // float
                if (Regex.Match (variableName, "^-?[0-9]+\\.{1}[0-9]+(f|F){1}$").Success) { 
                    file = file.Replace (function.declaration, function.declaration.Replace (" function ", " float "));
                    continue;
                }

				// double
				if (Regex.Match (variableName, "^-?[0-9]+\\.{1}[0-9]+(f|F){0}$").Success) {
					file = file.Replace (function.declaration, function.declaration.Replace (" function ", " double "));
					continue;
				}

                // int
                if (Regex.Match (variableName, "^-?[0-9]+$").Success) { 
                    file = file.Replace (function.declaration, function.declaration.Replace (" function ", " int "));
                    continue;
                }

                // variableName seems to be a variable name after all
                // search for the variable declaration in the function
                // variable declarations are already C#-style
                pattern = commonChars+oblWS+variableName+optWS+"="; // it will also match non converted var declarations : "public var _theVariable ="
				variableMatch = Regex.Match (function.text, pattern);

				if (variableMatch.Success && variableMatch.Groups[1].Value != "var")
                    file = file.Replace (function.declaration, function.declaration.Replace (" function ", " "+variableMatch.Groups[1].Value+" "));

                else { // declaration not found in the function, maybe it's somewhere in the file, or in itemsAndTypes
					variableMatch = Regex.Match (file, pattern);

					if (variableMatch.Success && variableMatch.Groups[1].Value != "var")
						file = file.Replace (function.declaration, function.declaration.Replace (" function ", " "+variableMatch.Groups[1].Value+" "));
                    else { // no it's nowhere in the file either
                        bool isFinded = false;

                        // now check if it's in itemsAndTypes
                        foreach (KeyValuePair<string, string> item in itemsAndTypes) {
                            if (variableName == item.Key) {
                                file = file.Replace (function.declaration, function.declaration.Replace (" function ", " "+item.Value+" "));
                                isFinded = true;
                                break;
                            }
                        }

                        if ( ! isFinded) // no, it's really anywhere ...
                            file = file.Replace (function.declaration, function.declaration.Replace (" function ", " MISSING_VAR_TYPE "));
                    } // end if found in the file
                } // end if found in the function

                continue;
            }


            // this pattern will match string and char
            pattern = "return"+oblWS+commonChars+optWS+";";
            Match stringCharMatch = Regex.Match (function.text, pattern);

			if (stringCharMatch.Success) {
				variableName = stringCharMatch.Groups[2].Value;
            
                // char
                pattern = "(\"|'){1}"+commonChars+"(\"|'){1}"+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]";

                if (Regex.Match (variableName, pattern).Success)
                    file = file.Replace (function.declaration, function.declaration.Replace (" function ", " char "));


                // string
                pattern = "(\"|'){1}"+commonChars+"(\"|'){1}";

                if (Regex.Match (variableName, pattern).Success)
                    file = file.Replace (function.declaration, function.declaration.Replace (" function ", " string "));

                continue;
            }


            // class instanciation : return new Class ();
            pattern = "return"+oblWS+"new"+oblWS+commonName;
            Match returnNewInstanceMatch = Regex.Match (function.text, pattern);

			if (returnNewInstanceMatch.Success) {
                file = file.Replace (function.declaration, function.declaration.Replace (" function ", " "+returnNewInstanceMatch.Groups[3].Value+" "));
                continue;
            }


            // look for "empty" return keyword, allowed in void functions
            pattern = "return"+optWS+";";

            if (Regex.Match (function.text, pattern).Success) { 
                file = file.Replace (function.declaration, function.declaration.Replace (" function ", " void "));
                continue;
            }


            // last possible pattern : function call : return Something();
            // do that in FunctionTheReturn()

            // last thing to do is testing if the function name begins by "is" like "IsSomething ()" because usally that function will return a bool
            pattern = "^(i|I)s";

            if (Regex.Match (function.name, pattern).Success) { 
                file = file.Replace (function.declaration, function.declaration.Replace (" function ", " bool "));
                continue;
            }


            // can't resolve anything ...
            file = file.Replace (function.declaration, function.declaration.Replace (" function ", " MISSING_RETURN_TYPE "));

        } // end looping function declarations
    

        // now convert the declaration that have a return type
        patterns.Add ( "function"+oblWS+"("+commonName+optWS+"\\((.*)\\))"+optWS+":"+optWS+commonChars );
        replacements.Add ( "$8 $2" );


        // classes constructors gets a void return type that has to be removed
        pattern = "class"+oblWS+commonName;
        MatchCollection allClasses = Regex.Matches (file, pattern);

        foreach (Match aClass in allClasses) {
            // look for constructors
            patterns.Add ( "void"+oblWS+"("+aClass.Groups[2].Value+optWS+"\\()" );
            replacements.Add ( "$2" );
        }


        





        DoReplacements ();
    } // end Functions ()


    // ----------------------------------------------------------------------------------

    /// <summary> 
    /// Now that all functions have a return type, try to convert the few variables whose value are set from a function
    /// </summary>
    void VariablesTheReturn () {
        pattern = "var"+oblWS+"("+commonName+optWS+"="+optWS+commonName+optWS+"\\()";
        List<Match> allVariableDeclarations = ReverseMatches (file, pattern);

        foreach (Match aVarDeclaration in allVariableDeclarations) {
            // look for the function declaration that match the function name
            pattern = commonChars+oblWS+aVarDeclaration.Groups[6].Value+optWS+"\\("; // aVarDeclaration.Groups[6].Value is the function name
            Match theFunction = Regex.Match (file, pattern); // quid if the same function name return sevral types of values ??

            if (theFunction.Success) // function.Groups[1].Value is the return type
                file = file.Replace (aVarDeclaration.Value, aVarDeclaration.Value.Replace ("var ", theFunction.Groups[1].Value+" "));
        }

        DoReplacements ();
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Try to resolve the return type of function where it wasn't possible before mostly because the type of 
    /// the returned variable couldn't be resolved before the first pass of function converting
    /// </summary>
    void FunctionsTheReturn () {
        // look only for function where the return type of the returned variable is still unresolved
        pattern = " MISSING_VAR_TYPE("+oblWS+commonName+optWS+"\\(.*\\)"+optWS+"{)"; 
        List<Match> allFunctions = ReverseMatches (file, pattern);

		foreach (Match aFunction in allFunctions) {
			Block function = new Block (aFunction, file);
			// here, function.newText will actually be the new declaration, and not the new function content
			
            // search only for variable return pattern     all functions with a "MISSING_VAR_TYPE" should return a variable anyway
            pattern = "return"+oblWS+commonName+optWS+";"; 
            Match theVariable = Regex.Match (function.text, pattern);

			if (theVariable.Success) { // if the current function returns a variable
				string variableName = theVariable.Groups[2].Value;

                // search for the variable declaration in the function
                pattern = commonChars+oblWS+variableName+optWS+"="; // it will also match non converted var declarations : "public var varName ="
				Match variableMatch = Regex.Match (function.text, pattern);

                if (variableMatch.Success && variableMatch.Groups[1].Value != "var")
                    file = file.Replace (function.declaration, function.declaration.Replace (" MISSING_VAR_TYPE ", " "+variableMatch.Groups[1].Value+" "));

                else { // declaration not found in the function, maybe it's somewhere in the file
                    variableMatch = Regex.Match (file, pattern);

                    if (variableMatch.Success && variableMatch.Groups[1].Value != "var")
                        file = file.Replace (function.declaration, function.declaration.Replace (" MISSING_VAR_TYPE ", " "+variableMatch.Groups[1].Value+" "));
                    else // no, it's really anywhere ...
                        continue; // do nothing, leave the MISSING_VAR_TYPE
                }

            }
        } // end looping functions


        // at last, look for function whose return type is still unresolved and that returns a value which comes directly from another function
        // ie : return Mathf.Abs(-5);
        // the "returned" function will be searched in file and in 
        pattern = " MISSING_RETURN_TYPE("+oblWS+commonName+optWS+"\\(.*\\)"+optWS+"{)"; 
        allFunctions = ReverseMatches (file, pattern);

        foreach (Match aFunction in allFunctions) {
            Block function = new Block (aFunction, file);
            
            // search only for function return pattern     bt now, most of the function with a "MISSING_RETURN_TYPE" should return a function
            pattern = "return"+oblWS+commonName+optWS+"\\(.*\\)"+optWS+";"; 
            Match theReturnedFunction = Regex.Match (function.text, pattern);
 
            if (theReturnedFunction.Success) { // if the current function returns a variable
                string returnedFunctionName = theReturnedFunction.Groups[2].Value;
                Debug.Log ("returned function="+returnedFunctionName);
                
                // search for the function declaration in file
                pattern = commonChars+oblWS+returnedFunctionName+optWS+"\\(.*\\)"+optWS+"{";
                Match returnedFunctionMatch = Regex.Match (file, pattern);

                if (returnedFunctionMatch.Success)
                    file = file.Replace (function.declaration, function.declaration.Replace (" MISSING_RETURN_TYPE ", " "+returnedFunctionMatch.Groups[1].Value+" "));

                else { // declaration not found in the function, maybe it's in itemsAndTypes
                    foreach (KeyValuePair<string, string> item in itemsAndTypes) {
                        if (returnedFunctionName == item.Key) {
                            file = file.Replace (function.declaration, function.declaration.Replace (" MISSING_RETURN_TYPE ", " "+item.Value+" "));
                            break;
                        }
                    }
                }
            } // end if function return pattern
        } // end looping functions
    } // end FunctionTheReturn()

} // end of public class JSToCSharp

/*
switch (endOfLine) {
            case EndOfLine.CRLF_Win:
                EOL = "\r\n";
                break;

            case EndOfLine.CR_Mac:
                EOL = "\r";
                break;
            
            case EndOfLine.LF_Unix:
                EOL = "\n";
                break;
            
            default:
                EOL = "\r\n";
                break;
        }
        */
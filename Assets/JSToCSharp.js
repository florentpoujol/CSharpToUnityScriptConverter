/*
 * UnityScript (JavaScript) to C# converter script.
 * UnityScript is a JavaScript-looking scripting language for use with the Unity3D Engine
 *
 * Created by Florent POUJOL
 * florent.poujol@gmail.com
 * http://www.florent-poujol.fr
 * Profile on Unity's forums : http://forum.unity3d.com/members/23148-Lion
 * 
 * Script's dedicated thread on Unity's forums : 
 */


/*
 * Use instructions :
 * 
 * Put this script anywhere in your project asset folder and attach it to a GameObject
 *
 * Create a folder "[your project]/Assets/ScriptsToBeConverted".
 * You may put in this folder any .js file (and the folder they may be in) to be converted.
 * 
 * Run the scene. One script is converted per frame, but the convertion of one script may often takes longer than 1/60 seconds. The convertion speed is *approximately* 20 files/seconds.
 * A label on the "Game" view shows the overall progress of the convertion and each convertion is logged in the console.
 * When it's complete, refresh the project tab for the new files/folder to be shown (right-click on the "Project" tab, then click on "Refresh" (or hit Ctrl+R on Windows)) 
 *
 * Upon convertion, a folder "[your project]/Assets/ConvertedScripts" is created with all converted scripts (and their folder hyerarchie)
 */


// ----------------------------------------------------------------------------------


import System.IO; // Directory.GetFiles() Directory.CreateDirectory() StreamReader  StreamWriter
import System.Text.RegularExpressions; // Regex.Replace(), Match, Matches, MatchCollection...


// custom classes names
var myClasses: String[];

// list of classes that exists in the pool of files that will be converted
private var ClassesList: List.<String> = new List.<String> (); 


/*private class FunctionInfos {
    var className: String;
    var functionName: String;
    var returnType: String;

    function FunctionInfos (className: String, functionName: String, returnType: String) {
        this.className = className;
        this.functionName = functionName;
        this.returnType = returnType;
    }
}

private var FunctionsList: List.<FunctionInfos> = new List.<FunctionInfos> ();*/

// list of the files and their paths to be converted
private var paths: String[];

// content of the file currently being converted
private var file: String;
private var fileName: String;
// index of the file currently being converted
private var fileIndex: int = 0;

// regex that include most common expressions but arrays and generic collections
private var commonName: String = "([A-Za-z0-9\\-_\\.]+)";

// same as common name but includes also arrays and generic collections  
private var commonChars: String = "([A-Za-z0-9<>,'\"\\-_\\[\\]\\.]+)"; // 
private var commonCharsWithoutComma: String = "([A-Za-z0-9<>'\"\\-_\\[\\]\\.]+)"; 

// white spaces
private var optWS: String = "(\\s*)"; // optionnal white space   0 or more white space
private var oblWS: String = "(\\s+)"; // obligatory white space  1 or more white space

private var collections: String = "(ArrayList|BitArray|CaseInsensitiveComparer|Comparer|Hashtable|Queue|SortedList|Stack|StructuralComparisons|DictionnaryEntry"+
    "|ICollection|IComparer|IDictionary|IDictionaryEnumerator|IEnumerable|IEnumerator|IEqualityComparer|IHashCodeProvider|IList|IStructuralComparable|IStructuralEquatable)";
private var genericCollections: String = "(Comparer|Dictionary|HashSet|KeyedByTypeCollection|LinkedList|LinkedListNode|List|Queue|SortedDictionary|SortedList|SortedSet|Stack|SynchronizedCollection"+
    "|SynchronizedKeyedCollection|SynchronizedReadOnlyCollection|ISet)";

// list of the patterns and corresponding replacements to be processed by DoReplacements()
private var patterns: List.<String> = new List.<String> ();
private var replacements: List.<String> = new List.<String> ();

// end of line
private var EOL = "\n"; // works also with \r\n on Windows 7



// ----------------------------------------------------------------------------------


function Start () {
    // get the path of all fles to be converted
    paths = Directory.GetFiles (Application.dataPath+"/ScriptsToBeConverted", "*.js", SearchOption.AllDirectories);
    Debug.Log ("Beginning converting "+paths.Length+" files.");

    // read all file and make a list of all encountered classes
    BuildClassesList (paths);
    //BuildFunctionsList (paths);
}


// allow for a convertion of one file per frame
function Update () {
    if (fileIndex < paths.Length) {
        var path: String = paths[fileIndex++];
        var reader: StreamReader = StreamReader (path);
        file = reader.ReadToEnd ();
        reader.Close();

        path = path.Remove (path.Length-3).Replace ("ScriptsToBeConverted", "ConvertedScripts"); // remove ".js"

        var lastSlashIndex: int = path.LastIndexOf ("\\");
        fileName = path.Substring (lastSlashIndex+1);
        path = path.Remove (lastSlashIndex);

        ConvertFile ();

        Directory.CreateDirectory(path);
        var writer: StreamWriter = StreamWriter (path+"/"+fileName+".cs");
        writer.Write (file);
        writer.Flush ();
        writer.Close ();
    }
}


var rect: Rect = new Rect (0, 0, 100, 100);
function OnGUI () {
    var text: String = "Begining Convertion ...";

    if (fileIndex < paths.Length)
        text = "Converting ... "+( (fileIndex+0.0) * 100.0 / (paths.Length+0.0) ).ToString ()+"%";
    else
        text = "Convertion done !";

    GUI.Label (rect, text);
}


// ----------------------------------------------------------------------------------

/*
 * Read all file and make a list of all encountered classes
 */
function BuildClassesList (paths: String[]) {
    for (var path: String in paths) {
        var reader: StreamReader = StreamReader (path);
        file = reader.ReadToEnd ();
        reader.Close();

        fileName = path.Remove (path.Length-3).Substring (path.LastIndexOf ("\\")+1); // remove .js and get the part after the last slash
        ClassesList.Add (fileName); // always add the fileName because it will often be the class name

        // search for class declaration pattern
        var pattern: String = "class"+oblWS+commonName+"("+oblWS+"extends"+oblWS+commonName+")?"+optWS+"{";
        var matches: MatchCollection = Regex.Matches (file, pattern);

        for (var match: Match in matches) {
            if ( ! ClassesList.Contains (match.Groups[2].Value))
                ClassesList.Add (match.Groups[2].Value);
        }
    }
}


// ----------------------------------------------------------------------------------

/*
 * Process the patterns/replacements
 */
function DoReplacements () {
    file = DoReplacements (file);
}

function DoReplacements (text: String): String {
    for (var i: int = 0; i < patterns.Count; i++)
        text = Regex.Replace (text, patterns[i], replacements[i]);

    patterns.Clear ();
    replacements.Clear ();
    return text;
}


// ----------------------------------------------------------------------------------

/*
 * Main function for the convertion
 */
function ConvertFile () {

    // Replace Array() (Array form Unity that works only for JS) by ArrayList ()
        patterns.Add ( "Array"+optWS+"\\(" );
        replacements.Add ( "ArrayList$1(" );
        patterns.Add ( ":"+optWS+"(Array)"+optWS+"=" );
        replacements.Add ( ":$1ArrayList$3=" );

    // search for string with single quotation mark
        patterns.Add ( "'(.{2,})'" );
        replacements.Add ( "\"$1\"" );


    // generic collections

        // Remove the dot in List.<float>
        patterns.Add ( optWS+"\\."+optWS+"<" );
        replacements.Add ( "<" );

        // remove whitespaces after a chevron < and before a >
        patterns.Add ( genericCollections+optWS+"<"+optWS+commonChars );
        replacements.Add ( "$1<$4" );
        patterns.Add ( commonChars+optWS+">" );
        replacements.Add ( "$1>" );

        // remove space after the coma
        patterns.Add ( "<"+optWS+commonChars+optWS+","+optWS+commonChars+optWS+">" );
        replacements.Add ( "<$2,$5>" );

        // Remove the obligatory space between two >     Dictionary.<String, List.<String> > 
        patterns.Add ( ">"+optWS+">" );
        replacements.Add ( ">>" );





    // Loops

        // for ( in ) > foreach
        patterns.Add ( "for("+optWS+"\\(.+in"+oblWS+".+\\))" );
        replacements.Add ( "foreach$1" );

        // convert leftover "foreach (String"
        patterns.Add ( "(foreach"+optWS+"\\("+optWS+")String" );
        replacements.Add ( "$1string" );
        
        // foreach (var name: Type in array) > foreach (Type name in array)
        // patterns.Add ( "foreach"+optWS+"\\("+optWS+"var"+oblWS+commonName+optWS+":"+optWS+commonChars+"("+optWS+"in(.+)\\))" );
        // replacements.Add ( "foreach$1($7 $4$8" );


    // GetComponent (also works for GetComponentInChildren !?)

        //Getcomponent(T) GetComponent("T") GetComponent.<T>()  => Getcompoenent<T>()
        // GetComponent.< T >() will have been modified to GetComponent<T>() at this point
        patterns.Add ( "(GetComponent|GetComponentInChildren)"+optWS+"<"+optWS+commonChars+optWS+">" );
        replacements.Add ( "$1<$4>" );
        
        patterns.Add ( "(GetComponent|GetComponentInChildren)"+optWS+"\\("+optWS+"[\"']{1}"+commonChars+"[\"']{1}"+optWS+"\\)" );
        replacements.Add ( "$1<$4>()" );

        patterns.Add ( "(GetComponent|GetComponentInChildren)"+optWS+"\\("+optWS+commonChars+optWS+"\\)" );
        replacements.Add ( "$1<$4>()" );


    // Yields

        patterns.Add ( "yield"+optWS+";" );
        replacements.Add ( "yield return 0;" );

        // yield something;
        patterns.Add (  "yield("+oblWS+commonChars+optWS+";)" );
        replacements.Add ( "yield return $2;" );
        
        // yield WaitForSeconds(3.5);
        patterns.Add ( "yield"+oblWS+commonChars+optWS+"\\(" );
        replacements.Add ( "yield return new $2$3(" );

        patterns.Add ( "yield new" );
        replacements.Add ( "yield return new" );


    // #pragma
    patterns.Add ( "\\#pragma"+oblWS+"(strict|implicit|downcast)" );
    replacements.Add ( "" );


    // replace .length by .Length
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
    // it will always resolve the variable type unless when the value is returned from a function (see VariablesTheReturn() function below)
    Variables ();

    // convert properties declarations
    Properties ();

    // convert function declarations, including arguments declaration
    Functions ();
   
    // convert variable declaration where the value is returned from a function now that almost all functions got their returned type resolved
    VariablesTheReturn ();

    // functionSec
    FunctionsTheReturn ();


    // Assembly imports
    // can't do that in Classes() because it wouldn't take into account the IEnumerator return type for the coroutine that may be added by Function()

        // move and convert the existing assemblies 
        pattern = "import"+oblWS+commonChars+";"; 
        matches = Regex.Matches (file, pattern);

        for (var match: Match in matches) {
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
        // new GameObject("name", Type);
        patterns.Add ( "(new"+oblWS+"GameObject"+optWS+"\\(.*,"+optWS+")"+commonName+"("+optWS+"\\)"+optWS+";)" );
        replacements.Add ( "$1typeof($5)$6)" );


    // time for patching things up 

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


        // System.String is a collateral damage and got replaced by System.string
        patterns.Add ( "(System"+optWS+"."+optWS+")string" );
        replacements.Add ( "$1String" );


        // bugs/artefacts
        patterns.Add ( "((public|private|protected)"+oblWS+")ring" );
        replacements.Add ( "$1string");


    DoReplacements ();


    Debug.Log ("Convertion done for ["+fileName+"]. Id="+fileIndex );
}


// ----------------------------------------------------------------------------------

/* 
 * Convert stuffs related to classes : declaration, inheritance, parent constructor call, Assembly imports
 */
function Classes () {
    // several classes may have beeen defined within the same file
    // loop the classes declarations
    var pattern: String = "class"+oblWS+commonName+"("+oblWS+"extends"+oblWS+commonName+")?"+optWS+"{";
    var classes: MatchCollection = Regex.Matches (file, pattern);
    var offset: int = 0;

    // list of functions and variable declaration that are found within a class
    var classesContent: List.<String> = new List.<String> ();

    for (var match: Match in classes) {
        offset = 0;
        var classMatch: Match = match;
        var className: String = match.Groups[2].Value;
        
        // look for constructors
        pattern = className+optWS+"\\(.*\\)"+optWS+"{";
        var constructors: MatchCollection = Regex.Matches (file, pattern);

        for (var match: Match in constructors) {
            var functionStartIndex: int = match.Index + offset + match.Length;
            var functionEndIndex: int = GetEndOfBlockIndex (functionStartIndex, file);
            var functionString: String = file.Substring (functionStartIndex, functionEndIndex-functionStartIndex);
           
            // look for parent constructor call : super();
            pattern = "super"+optWS+"\\((.*)\\)"+optWS+";";
            match = Regex.Match (functionString, pattern);

            if (match.Success) {
                file = file.Insert (functionStartIndex-1, ": base("+match.Groups[2].Value+") ");
                offset += 9+match.Groups[2].Length;

                file = file.Replace (functionString, functionString.Replace (match.Value, ""));
                offset -= match.Length;
            }
        }

        // look for functions and var inside the class
        var classStartIndex: int = classMatch.Index + offset + classMatch.Length;
        var classEndIndex: int = GetEndOfBlockIndex (classStartIndex, file);
        var classString: String = file.Substring (classStartIndex, classEndIndex-classStartIndex);

        pattern = "function"+oblWS+commonName+optWS+"\\(";
        matches = Regex.Matches (classString, pattern);

        for (var match: Match in matches)
            classesContent.Add (match.Groups[2].Value);

        pattern = "var"+oblWS+commonName+optWS+"(:|;|=)";
        matches = Regex.Matches (classString, pattern);

        for (var match: Match in matches)
            classesContent.Add (match.Groups[2].Value);
    }


    // we made a list of functions and variables inside classes
    // now make a list of functions and variable inside the file ...
    var fileContent: List.<String> = new List.<String> ();

    pattern = "function"+oblWS+commonName+optWS+"\\(";
    matches = Regex.Matches (file, pattern);

    for (var match: Match in matches)
        fileContent.Add (match.Groups[2].Value);

    pattern = "var"+oblWS+commonName+optWS+"(:|;|=)";
    matches = Regex.Matches (file, pattern);

    for (var match: Match in matches)
        fileContent.Add (match.Groups[2].Value);


    // ... compare the two list
    // if there is a difference, that mean that some variable or function declaration lies outside a class
    // that means that the file is a MonoBehaviour derived class
    if (classes.Count == 0 || classesContent.Count != fileContent.Count ) {// no class declaration within the file
        file = file.Insert (0, EOL+"public class "+fileName+" : MonoBehaviour {"+EOL);
        file = file+EOL+"} // end of class "+fileName; // the closing class bracket
    }


    // Attributes / Flags
        // move some of them to the beginning of the file before converting
        pattern = "@"+optWS+"(script"+oblWS+")?RequireComponent"+optWS+"\\("+optWS+commonChars+optWS+"\\)("+optWS+";)?";
        matches = Regex.Matches (file, pattern);
        for (var match: Match in matches)
            file = file.Insert (0, "[RequireComponent$4(typeof($6))]").Replace (match.Value, "");

        
        pattern = "@"+optWS+"(script"+oblWS+")?ExecuteInEditMode"+optWS+"\\("+optWS+"\\)("+optWS+";)?";
        matches = Regex.Matches (file, pattern);
        for (var match: Match in matches)
            file = file.Insert (0, "[ExecuteInEditMode]").Replace (match.Value, "");
        

        
        patterns.Add ( "@"+optWS+"script" );
        replacements.Add ( "@" );

        //patterns.Add ( "@"+optWS+"script"+optWS+"AddComponentMenu("+optWS+"\\("+optWS+"\""+optWS+commonChars+optWS"\""+optWS+"\\))" );
        patterns.Add ( "@"+optWS+"AddComponentMenu(.*\\))("+optWS+";)?" );
        replacements.Add ( "[AddComponentMenu$2]" );

        // @RequireComponent (T) => [RequireComponent (typeof(T))]
        // patterns.Add ( "@"+optWS+"RequireComponent"+optWS+"\\("+optWS+commonChars+optWS+"\\)("+optWS+";)?" );
        // replacements.Add ( "[RequireComponent$2(typeof($4))]" );

        // @ExecuteInEditMode () => [ExecuteInEditMode]
        // patterns.Add ( "@"+optWS+"ExecuteInEditMode"+optWS+"\\("+optWS+"\\)("+optWS+";)?" );
        // replacements.Add ( "[ExecuteInEditMode]" );
       

        // flags  @ something (something2) => [something (something2)]    @something => [something]
        patterns.Add ( "@"+optWS+commonName+"("+optWS+"\\(.*\\))?("+optWS+";)?" );
        replacements.Add ( "[$2$3]" );



    // struct
    // in JS, the way to define struct is to makes a class inherits from System.ValueType
    patterns.Add ( "class"+oblWS+commonName+oblWS+"extends"+oblWS+"System"+optWS+"\\."+optWS+"ValueType" );
    replacements.Add ( "struct$1$2" );

    // class inheritance
    patterns.Add ( oblWS+"extends"+oblWS+commonName );
    replacements.Add ( "$1:$2$3" );

    // super. => base.      
    patterns.Add ( "super"+optWS+"\\." );
    replacements.Add ( "base$1." );


    DoReplacements ();

    // Assembly import, see in ConvertFile()
}


// ----------------------------------------------------------------------------------

/*
 * Find the end of a block opened by a bracket
 * Returns the index in [text] of the bracket that ends the block opened by the bracket for which [startIndex] is the index in [text]
 *
 * @param startIndex The index in [text] of the block's opening bracket
 * @param text The text that contains the block
 *
 * @return The index in [text] of the bracket that ends the block
 */
function GetEndOfBlockIndex (startIndex: int, text: String): int {
    var openedBrackets: int = 1;
    
    for (var i: int = startIndex+1; i< text.Length; i++) {
        if (text[i] == '{'[0])
            openedBrackets++;

        if (text[i] == '}'[0]) {
            openedBrackets--;

            if (openedBrackets == 0)
                return i-1;
        }
    }
}


// ----------------------------------------------------------------------------------

/*
 * Add the "new" keyword before classes instanciation where it is missing
 */
function AddNewKeyword () {
    // get pattern like "var name: Type = ClassOrMethod ();"  and search for "var name: Class = Class ();"
    pattern = "var"+oblWS+commonName+optWS+":"+optWS+commonChars+"("+optWS+"="+optWS+commonChars+optWS+"\\(.*\\)"+optWS+";)";
    var matches: MatchCollection = Regex.Matches (file, pattern);
    var match: Match;
    offset = 0;
    for (match in matches) {
        if (match.Groups[5].Value == match.Groups[9].Value) { // if the type == the class/method name
            file = file.Insert (match.Groups[9].Index+offset , "new "); // add "new " in front of Class ()
            offset += 4;// 4 is the length of "new "
            // each time I add a "new " keyword, I change the index of all characters in file after the addition
            // but the regex search has been completed only once with the unmodified version of file
            // so I need to use this variable offset that keep track of the characters delta
        }
    }

    
    //also add a new keyword in front of collections
        pattern = "="+optWS+collections+optWS+"\\("; // when setting the value of a variable
        InsertInPatterns (pattern, 1, " new");
        // var matches: MatchCollection = Regex.Matches (file, pattern);
        // offset = 0;
        // for (match in matches) {
        //     file = file.Insert (match.Index+1+offset, " new"); // insert " new" just after the =
        //     offset += 4;
        // }

        pattern = "return"+oblWS+collections+optWS+"\\("; // when returning an empty instance
        matches = Regex.Matches (file, pattern);
        offset = 0;
        for (match in matches) {
            file = file.Insert (match.Groups[2].Index+offset, "new ");
            offset += 4;
        }

    // the code above can also be written as below :
    // it perform the regex search again each time a "new" keyword is added and pick the first result found until no more pattern is found
    // this way the offset thing is not needed but :
    // BEWARE : there is a risk of infinite loop if all matched patterns are not modified
    /*pattern = "";
    while (true) {
        var match: Match = Regex.Match (file, pattern);
        if ( ! match.Success)
            break;

        file = file.Insert (match.Index+1, " new");
    }*/


    // and Generic collections
        pattern = "="+optWS+genericCollections+"<"+commonChars+">"+optWS+"\\(";
        InsertInPatterns (pattern, 1, " new");
        // var matches: MatchCollection = Regex.Matches (file, pattern);
        // offset = 0;
        // for (match in matches) {
        //     file = file.Insert (match.Index+1+offset, " new");
        //     offset += 4;
        // }

        pattern = "return"+oblWS+genericCollections+optWS+"\\(";
        matches = Regex.Matches (file, pattern);
        offset = 0;
        for (match in matches) {
            file = file.Insert (match.Groups[2].Index+offset, "new ");
            offset += 4;
        }


    // append the myClasses list to ClassesList
    for (var _class: String in myClasses) {
        if ( ! ClassesList.Contains (_class))
            ClassesList.Add (_class);
    }


    // append the content of UnityClasses to ClassesList
    var reader: StreamReader = new StreamReader (Application.dataPath+"/UnityClasses.txt");

    while (true) {
        var _class: String = reader.ReadLine ();
        if (_class == null)
            break;

        ClassesList.Add (_class);
    }


    // and the class in ClassesList
    for (_class in ClassesList) {
        pattern = "="+optWS+_class+optWS+"\\(";
        InsertInPatterns (pattern, 1, " new");

        // do the same with return keyword
        pattern = "return"+oblWS+_class+optWS+"\\(";
        matches = Regex.Matches (file, pattern);
        offset = 0;

        for (match in matches) {
            file = file.Insert (match.Groups[2].Index+offset, "new ");
            offset += 4;
        }
    }

    // built a pattern like "(MyClass1|MyClass2|...)"
    /*var myClassesPattern: String = "(";

    for (var _class: String in myClasses)
        myClassesPattern += _class+"|";

    myClassesPattern = myClassesPattern.Remove (myClassesPattern.Length-1); // remove the last |
    myClassesPattern += ")";

    pattern = "="+optWS+myClassesPattern+optWS+"\\(";*/
    // var matches: MatchCollection = Regex.Matches (file, pattern);
    // offset = 0;
    // for (match in matches) {
    //     file = file.Insert (match.Index+1+offset, " new");
    //     ofset += 4;
    // }
    
    
} // end AddNewKeyword ()


// ----------------------------------------------------------------------------------

/* 
 * Insert [text] at the fixed position [patternOffset] in all [pattern]s found
 */
function InsertInPatterns (pattern: String, patternOffset: int, text: String) {
    var matches: MatchCollection = Regex.Matches (file, pattern);
    var _offset: int = 0;

    for (var match: Match in matches) {
        file = file.Insert (match.Index + patternOffset + _offset, text);
        _offset += text.Length;
    }
}


// ----------------------------------------------------------------------------------

/*
 * Add the keyword public when no visibility (or just static) is set (the default visibility in JS is public but private in C#)
 * Works also for functions
 */
function AddVisibility () {
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
    var matches: MatchCollection = Regex.Matches (file, pattern);
    offset = 0;

    for (var match: Match in matches) {
        var functionStartIndex: int = match.Index + offset + match.Length;
        var functionEndIndex: int = GetEndOfBlockIndex (functionStartIndex, file);
        
        if (functionEndIndex == 0) // appends for empty functions
            continue;

        var functionString: String = file.Substring (functionStartIndex, functionEndIndex-functionStartIndex); // it's an issue if an array is declared within the function as this method is called before the JS array's square brackets are converted to C# curly brackets
        if (functionString == "")
            Debug.Log ("=============== AddVisibility    empty function :"+fileName+"."+match.Groups[2].Value);

        var scriptList = List.<String>();
        scriptList.Add ("MechAttackMoveController");
        scriptList.Add ("SpiderAttackMoveController");
        scriptList.Add ("PlayerAnimation");
        scriptList.Add ("Spawner");
        scriptList.Add ("EndOfLevel");
        scriptList.Add ("SwarmAI");

        // if (scriptList.Contains (fileName))
        //     Debug.Log ("============ "+fileName+"."+match.Groups[2].Value+" = "+functionString);

        patterns.Add ( "public"+oblWS+"var" );
        replacements.Add ( "var" );
        patterns.Add ( "public"+oblWS+"(static"+oblWS+"var)" );
        replacements.Add ( "$2" );

        var newFunctionString: String = DoReplacements (functionString);

        offset += (newFunctionString.Length-functionString.Length);

        if (functionString != "") // prevent a "ArgumentException: oldValue is the empty string." that happend somethimes
            file = file.Replace (functionString, newFunctionString);
    } // end for
}


// ----------------------------------------------------------------------------------

/*
 * Search for and convert variables declarations
 */
function Variables () {
    // add an f at the end of a float (and double) value 
    patterns.Add ( "([0-9]+\\.{1}[0-9]+)[f|F]{0}" );
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
        // arrays with type declaration without space    "String[]" instead of "String [ ]"" are already converted because [ and ] are among commonChars

            // string 
            patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"String"+optWS+"\\["+optWS+"\\]"+optWS+"(=|;)" );
            replacements.Add ( "string[] $2$7$8" );

            // boolean
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

            // replace 'a'[0] and "a"[0] by 'a' in char[] declaration
            patterns.Add ( "({|,){1}"+optWS+"(\"|'){1}"+commonCharsWithoutComma+"(\"|'){1}"+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+"(}|,){1}" );
            replacements.Add ( "$1$2'$4'$9$10" );
            // now, as regex doesn't overlap themselves, only half of the argument have been converted
            // I need to run the regex again
            patterns.Add ( "({|,){1}"+optWS+"(\"|'){1}"+commonCharsWithoutComma+"(\"|'){1}"+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+"(}|,){1}" );
            replacements.Add ( "$1$2'$4'$9$10" );

            // boolean
            patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"{"+optWS+"((true|false)"+optWS+",?"+optWS+")*"+optWS+"})" );
            replacements.Add ( "bool[] $2" );

            //int
            patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"{"+optWS+"([-0-9]+"+optWS+",?"+optWS+")*"+optWS+"})" );
            replacements.Add ( "int[] $2" );

            //float
            patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"{"+optWS+"([-0-9]+\\.{1}[0-9]+(f|F){1}"+optWS+",?"+optWS+")*"+optWS+"})" );
            replacements.Add ( "float[] $2" );
 

        // empty arrays declarations without type declaration  var _array11 = new boolean[ 4] ;
            
            // String
            patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"new"+oblWS+")String("+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+";)" );
            replacements.Add ( "string[] $2string$7" );

            // // replace leftover ": String[" or "new String["
            // // patterns.Add ( "(new|:)"+optWS+"String("+optWS+"\\[)" );
            // // replacements.Add ( "$1$2string$3" );

            // boolean
            patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"new"+oblWS+")boolean("+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+";)" );
            replacements.Add ( "bool[] $2bool$7" );
            

            // general case
            patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"new"+oblWS+commonName+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+";)" );
            replacements.Add ( "$7[] $2" );


    // =====================================================================================================================================================

    // variable with type declaration but no value setting      var test: String;

        // particular case : String     var _string2: String;
        patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"String"+optWS+";" );
        replacements.Add ( "string $2$5;" );

        // particular case : Boolean     var _bool2: boolean;
        // works also for boolean with value setting
        // patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"boolean"+optWS+";" );
        // replacements.Add ( "bool $2$5;" );

        // general case :     var _int3: int;    also works for arrays   var _array: int[];
        // patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+commonChars+optWS+";" );
        // replacements.Add ( "$5 $2$6;" );

        // others cases than String are actually handled below :

    // variables with type declaration and value setting    var test: int = 5;

        // particular case : String    put String lowercase and replace '' by ""       var _string6: String = '& ';
        patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"String"+optWS+"="+optWS+"(\"|'){1}(.*)(\"|'){1}"+optWS+";" );
        replacements.Add ( "string $2$5=$6\"$8\"$10;" );

        // particular case : char       var _char4: char = 'a'[0];    var _char5: char = "a"[0];    => char _char4 = 'a';
        patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"char"+optWS+"="+optWS+"(\"|'){1}(.*)(\"|'){1}"+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+";" );
        replacements.Add ( "char $2$5=$6'$8'$13;" );

        // particular case : boolean    var _bool3: boolean = true;
        patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"boolean"+optWS+"(=|;|in)" );
        replacements.Add ( "bool $2$5$6" );

        // particular case : float
        patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"float"+optWS+"="+optWS+"([-0-9]+\\.[0-9]+[fF]{1})"+optWS+";" );
        replacements.Add ( "float $2$5=$6$7$8;" );

        // general case for variable with type definition.   
        // Also works in foreach loops 
        patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+commonChars+optWS+"(=|;|in)" );
        replacements.Add ( "$5 $2$6$7" );
        // This would Also works for some arrays when there is not space around and between square brackets
        // but it would also really fuck up with String[]


    // variable with value setting but no type declaration. Have to guess the type with the value's look
    
        // string
        patterns.Add ( "var"+oblWS+commonName+optWS+"="+optWS+"(\"|'){1}(.*)(\"|'){1}"+optWS+";" );
        replacements.Add ( "string $2$3=$4\"$6\"$8;" );

        // char    var _char2 = 'a'[0];    var _char3 = "a"[0];
        patterns.Add ( "var"+oblWS+commonName+optWS+"="+optWS+"(\"|'){1}(.*)(\"|'){1}"+optWS+"(\\["+optWS+"[0-9]+"+optWS+"\\])"+optWS+";" );
        replacements.Add ( "char $2$3=$4'$6'$12;" );

        // boolean
        patterns.Add ( "var("+oblWS+commonName+optWS+"="+optWS+"(true|false)"+optWS+";)" );
        replacements.Add ( "bool$1" );

        // int
        patterns.Add ( "var("+oblWS+commonName+optWS+"="+optWS+"([-0-9]+)"+optWS+";)" ); // long will be converted to int
        replacements.Add ( "int$1" );

        // float     value already contains an f or F   3.5f  or 1.0F
        patterns.Add ( "var"+oblWS+commonName+optWS+"="+optWS+"([-0-9]+\\.{1}[0-9]+(f|F){1})"+optWS+";" );
        replacements.Add ( "float $2$3=$4$5$7;" );
        

    // other types (classes instantiation)    var _test3 = new Test();

        // I can't do anything if there is no "new" keyword, I can't guess the type of the variable and I can't tell if Test() is a class or a method
        // new keyword are already added everywhere they are needed by the method "AddNewKeyword()"
        patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"new"+oblWS+commonChars+optWS+"\\((.*);)" );
        replacements.Add ( "$7 $2" );


    // var declaration vithout a type and the value comes from a function
    // The type can be resolved if the function declaration is done in the file, but JS allows not to specify which type returns a function
    // Wait until the functions declarations are processed (in Functions()) to try to convert those variables
        

    DoReplacements ();
} // end VariablesDeclarations ()


// ----------------------------------------------------------------------------------

/*
 * Convert Properties declarations
 */
function Properties () {
    // change String return type to string, works also for function (properties in JS are functions)
    patterns.Add ( "(\\)"+optWS+":"+optWS+")String" );
    replacements.Add ( "$1string" );

    // change boolean return type to bool
    patterns.Add ( "(\\)"+optWS+":"+optWS+")boolean" );
    replacements.Add ( "$1bool" );

    DoReplacements ();

    /* JS
    protected var moo = 1;
    function get Foo(): int { return foo; }
    function set Foo(value: int) { foo = value; }

    C#
    protected int _foo;
    public int Foo {
        get { return _foo; }
        protected set { _foo = value; }
    }*/

    // first, get all property getters (if a property exists, I assume that a getter always exists for it)
    pattern = "(public|private|protected)"+oblWS+"function"+oblWS+"get"+oblWS+commonName+optWS+"\\("+optWS+"\\)"+optWS+":"+optWS+commonChars+optWS+
    "{"+optWS+"return"+oblWS+commonName+optWS+";"+optWS+"}";
    var matches: MatchCollection = Regex.Matches (file, pattern);
    offset = 0;
    var unModifiedFile: String = file;

    for (var match: Match in matches) {
        var propName: String = match.Groups[5].Value;
        var varName: String = match.Groups[14].Value;

        // now I have all infos nedded to start building the property in C#
        // match.Groups[1].Value is the getter's visibility. It always exists by now because it has been added by the AddVisibility() method above
        // match.Groups[10].Value is the getter's return type
        var property: String = match.Groups[1].Value+" "+match.Groups[10].Value+" "+propName+" {"+EOL 
        +"\t\tget { return "+varName+"; }"+EOL; // getter


        // now look for the corresponding setter
        pattern = "(public|private|protected)"+oblWS+"function"+oblWS+"set"+oblWS+propName+".*}";
        var setMatch: Match = Regex.Match (unModifiedFile, pattern);

        if (setMatch.Success)
            property += "\t\t"+setMatch.Groups[1].Value+" set { "+varName+" = value; }"+EOL; // setter

        property +="\t}"+EOL; // property closing bracket


        // do the modifs in the file
        file = file.Insert (match.Index + offset, property);
        offset += property.Length;

        file = file.Remove (match.Index + offset, match.Length); // remove getter
        offset -= match.Length;

        if (setMatch.Success) {
            file = file.Remove (setMatch.Index + offset, setMatch.Value.Length);
            offset -= setMatch.Value.Length;
        }

        // setter must follow it's getter
        // a getter must be followed by it's setter
        // can add anything between a getter and setter but another getter/setter
    }
} // end Properties ()


// ----------------------------------------------------------------------------------

/* 
 * Convert function declarations
 * Try to resolve the return type (if any) when there is none declared with the look of what is returned
 */
function Functions () {
    // search for return keyword to see if the function actually returns something, then try to resolve the return type with the returned variable type or returned value
    // if the function does not return anything, a "void" return type is added
    // if the type can't be resolved, a "MISSING_RETURN_TYPE" return type is added
    // coroutines gets a IEnumerator return type and thair call is wrapped by StartCoroutine( )

    pattern = "function"+oblWS+commonName+optWS+"\\(.*\\)"+optWS+"{"; // look for all JS functions that has no explicit return type
    var matches: MatchCollection = Regex.Matches (file, pattern);
    offset = 0;

    for (var match: Match in matches) {
        var functionName: String = match.Groups[2].Value;
        var functionStartIndex: int = match.Index + match.Length + offset;
        var functionEndIndex: int = GetEndOfBlockIndex (functionStartIndex, file);
        var functionString: String = file.Substring (functionStartIndex, functionEndIndex-functionStartIndex );


        

        // look for return keyword patterns
        pattern = "return.+;";
        if ( ! Regex.Match (functionString, pattern).Success) { // no return keyword : add "void" return type
            file = file.Insert (functionStartIndex-1, ": void "); 
            offset += 7;
            continue;
        }

        
        // Below this point, we know that the function returns some value but we don't know the type yet
        // Look in functionString for sevral return pattern 
        
        // yield / coroutine
        pattern = "yield"+oblWS+"return";
        match = Regex.Match (functionString, pattern);

        if (match.Success) { 
            file = file.Insert (functionStartIndex-1, ": IEnumerator ");
            offset += 14;

            // In C# (and Boo), a coroutine call must be wrapped by the StartCoroutine() method : StartCoroutine( CoroutineName() );
            // The current function is a coroutine, so search for it's call in the file
            pattern = "("+functionName+optWS+"\\(.*\\))("+optWS+";)";
            var coroutinesMatches: MatchCollection = Regex.Matches (file, pattern);

            for (var match: Match in coroutinesMatches) {
                var coroutineCall: String = "StartCoroutine( "+match.Groups[1].Value+" );";

                file = file.Replace (match.Value, coroutineCall);
                offset += (19-match.Groups[3].Length); // 19 is the length of "StartCoroutine(  );"    match.Groups[3].Length is the length of '"+optWS+";"'
            }

            continue;
        }


        // this pattern will match an int, a float, a boolean ar a variable name
        pattern = "return"+oblWS+commonName+optWS+";"; 
        match = Regex.Match (functionString, pattern);
        var varName: String = "";

        if (match.Success) { 
            varName = match.Groups[2].Value;

            // boolean
            if (Regex.Match (varName, "(true|false)").Success) { 
                file = file.Insert (functionStartIndex-1, ": bool ");
                offset += 7;
                continue;
            }

            // float
            if (Regex.Match (varName, "[-0-9]+\\.{1}[0-9]+[fF]{0,1}").Success) { 
                file = file.Insert (functionStartIndex-1, ": float ");
                offset += 8;
                continue;
            }

            // int
            if (Regex.Match (varName, "^[-0-9]+$").Success) { 
                file = file.Insert (functionStartIndex-1, ": int ");
                offset += 6;
                continue;
            }

            // varName seems to be a variable name after all
            // search for the variable declaration in the function
            // variable declarations are already C#-style
            pattern = commonChars+oblWS+varName+optWS+"="; // it will also match non converted var declarations : "var _theVariable ="
            match = Regex.Match (functionString, pattern);

            if (match.Success && match.Groups[1].Value != "var") {
                file = file.Insert (functionStartIndex-1, ": "+match.Groups[1].Value+" ");
                offset += (3+match.Groups[1].Length);
            }
            else { // declaration not found in the function, maybe it's somewhere in the file
                match = Regex.Match (file, pattern);

                if (match.Success && match.Groups[1].Value != "var") {

                    file = file.Insert (functionStartIndex-1, ": "+match.Groups[1].Value+" ");
                    offset += (3+match.Groups[1].Length);
                }
                else { // no, it's really anywhere ...
                    file = file.Insert (functionStartIndex-1, ": MISSING_RETURN_TYPE ");
                    offset += 22;
                }
            }

            continue;
        }


        // this pattern will match string and char
        pattern = "return"+oblWS+commonChars+optWS+";";
        match = Regex.Match (functionString, pattern);
        
        if (match.Success) {
            varName = match.Groups[2].Value;
            
            // char
            pattern = "(\"|'){1}"+commonChars+"(\"|'){1}"+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]";
            if (Regex.Match (varName, pattern).Success) { 
                file = file.Insert (functionStartIndex-1, ": char ");
                offset += 7;
                continue;
            }

            // string
            pattern = "(\"|'){1}"+commonChars+"(\"|'){1}";
            if (Regex.Match (varName, pattern).Success) {
                file = file.Insert (functionStartIndex-1, ": string ");
                offset += 9;
                continue;
            }
        }


        // class instanciation  return new Class ();
        pattern = "return"+oblWS+"new"+oblWS+commonName;
        match = Regex.Match (functionString, pattern);
        
        if (match.Success) {
            file = file.Insert (functionStartIndex-1, ": "+match.Groups[3].Value+" ");
            offset += (3+match.Groups[3].Length);
            continue;
        }


        // look for "empty" return keyword, allowed in void functions
        pattern = "return"+optWS+";"; 
        if (Regex.Match (functionString, pattern).Success) { 
            file = file.Insert (functionStartIndex-1, ": void "); // functionStartIndex-1 is the index of the opening bracket
            offset += 7;
            continue;
        }

        // can't resolve anything ...
        file = file.Insert (functionStartIndex-1, ": MISSING_RETURN_TYPE ");
        offset += 22;
    } // end for
    

    // now actually convert the declaration that have a return type (all function should, even if it's the missing return type)
    patterns.Add ( "function"+oblWS+commonName+"("+optWS+"\\((.*)\\))"+optWS+":"+optWS+commonChars );
    replacements.Add ( "$8 $2$3" );

    // without return type (not needed anymore)
    // patterns.Add ( "function"+oblWS+commonChars+"("+optWS+"\\((.*)\\))" ); // classes constructor will get a void return type
    // replacements.Add ( "void $2$3" );


    // classes constructors gets a void return type that has to be removed
    pattern = "class"+oblWS+commonName;
    matches = Regex.Matches (file, pattern);

    for (var match: Match in matches) {
        // look for constructors
        patterns.Add ( "void"+oblWS+"("+match.Groups[2].Value+optWS+"\\()" );
        replacements.Add ( "$2" );
    }


    // argument declaration
    patterns.Add ( "(\\(|,){1}"+optWS+commonName+optWS+":"+optWS+commonCharsWithoutComma+optWS+"(\\)|,){1}" );
    replacements.Add ( "$1$2$6 $3$7$8" );
    // as regex doesn't overlap themselves, only half of the argument have been convertes
    // I need to run the regex 
    patterns.Add ( "(\\(|,){1}"+optWS+commonName+optWS+":"+optWS+commonCharsWithoutComma+optWS+"(\\)|,){1}" );
    replacements.Add ( "$1$2$6 $3$7$8" );

    DoReplacements ();
} // end Functions ()


// ----------------------------------------------------------------------------------

/* 
 * Now that all functions have a return type, try to convert the few variable whose value is returned from a function
 */
function VariablesTheReturn () {
    pattern = "var"+oblWS+"("+commonName+optWS+"="+optWS+commonName+optWS+"\\()";
    var matches: MatchCollection = Regex.Matches (file, pattern);

    for (var match: Match in matches) {
        var varName: String = match.Groups[3].Value;
        var functionName: String = match.Groups[6].Value;
        
        // look for the function declaration
        pattern = commonChars+oblWS+"("+functionName+")"+optWS+"\\(";
        match = Regex.Match (file, pattern);

        if (match.Success) { // function declaration found in file    match.Groups[1].Value is the return type
            patterns.Add ( "var("+oblWS+varName+optWS+"="+optWS+functionName+optWS+"\\()" );
            replacements.Add ( match.Groups[1].Value+"$1" );
        }
    }

    DoReplacements ();
}


// ----------------------------------------------------------------------------------

/*
 * Try to resolve the return type of function where it wasn't possible before mostly because the type of 
 * the returned variable couldn't be resolved before the first pass of function converting
 * 
 */
function FunctionsTheReturn () {
    pattern = "MISSING_RETURN_TYPE("+oblWS+commonName+optWS+"\\(.*\\)"+optWS+"{)"; // look only for function where the return type is still unresolved
    var matches: MatchCollection = Regex.Matches (file, pattern);

    for (var match: Match in matches) {
        var functionStartIndex: int = match.Index + match.Length;
        var functionEndIndex: int = GetEndOfBlockIndex (functionStartIndex, file);
        var functionString: String = file.Substring (functionStartIndex, functionEndIndex-functionStartIndex );
        
        var functionDeclaration: String = match.Value;
        var newFunctionDeclaration: String = "";

        // search only for variable return pattern
        pattern = "return"+oblWS+commonName+optWS+";"; 
        match = Regex.Match (functionString, pattern);

        if (match.Success) { // if the current function returns a variable
            var varName: String = match.Groups[2].Value;

            // search for the variable declaration in the function
            pattern = commonChars+oblWS+varName+optWS+"="; // it will also match non converted var declarations : "var _theVariable ="
            match = Regex.Match (functionString, pattern);

            if (match.Success && match.Groups[1].Value != "var") // var declaration found in the function
                newFunctionDeclaration = functionDeclaration.Replace ("MISSING_RETURN_TYPE", match.Groups[1].Value);
                
            else { // declaration not found in the function, maybe it's somewhere in the class
                // search the whole file
                match = Regex.Match (file, pattern);

                if (match.Success && match.Groups[1].Value != "var")
                    newFunctionDeclaration = functionDeclaration.Replace ("MISSING_RETURN_TYPE", match.Groups[1].Value);
                
                else // no, it's really anywhere ...
                    continue;
            }

            //Debug.Log (match.Value+" "+match.Groups[2].Value);

            file = file.Replace (functionDeclaration, newFunctionDeclaration);
        }
    }
}


// ----------------------------------------------------------------------------------

/*
 * read all file and make a list of all encountered functions, if they have a return type
 */
//function BuildFunctionsList (paths: String[]) {
    /*
    for (var path: String in paths) {
        var reader: StreamReader = StreamReader (path);
        var _file: String[] = reader.ReadToEnd ().Split ('\n'[0]);
        reader.Close();

        

        // loop class declaration pattern
        pattern = "class"+oblWS+commonName+"("+oblWS+"extends"+oblWS+commonName+")?"+optWS+"{";
        var matches: MatchCollection = Regex.Matches (_file, pattern);

        for (var match: Match in matches) {
            var className: String = match.Groups[2].Value;

            // lopp function declarations
            pattern = "function"+oblWS+commonName+optWS+"\\(.*\\)"+optWS+":"+optWS+commonChars+optWS+"{";
            var functions: MatchCollection = Regex.Matches (_file, pattern);

            for (match in functions) {
                FunctionsList.Add (new FunctionInfos (className, match.Groups[2].Value, match.Groups[6].Value));
            }
        }

        // there may be functions outside a class declaration
        var _fileName: String = path.Remove (path.Length-3).Substring (path.LastIndexOf ("\\")+1); // remove .js and get the part after the last slash

        pattern = "function"+oblWS+commonName+optWS+"\\(.*\\)"+optWS+":"+optWS+commonChars+optWS+"{";
        var functions: MatchCollection = Regex.Matches (_file, pattern);
        for (match in functions) {

            if ()


            FunctionsList.Add (new FunctionInfos (className, match.Groups[2].Value, match.Groups[6].Value));
        }
    }*/
//}


// ----------------------------------------------------------------------------------

/*
 * Tell if pattern can be found in text, begining at startIndex
 * startIndex is the index in text of the first char of pattern 
 */
// function IndexIsTheStartOf (startIndex: int, pattern: String, text: String): boolean {
//     // if ( ! pattern.StartsWith ('(') && ! pattern.EndsWith (')'))
//     //     pattern = '('+pattern+')';

//     var match: Match = Regex.Match (text.Substring (startIndex), pattern);

//     if (match.Success && match.Index == 0)
//         return true;
//     else
//         return false;

//     /*for (var i: int = 0; i < pattern.length; i++) {
//         if (text[startIndex+i] != pattern[i])
//             return false;
//     }

//     return true;*/
// }




// ----------------------------------------------------------------------------------

/*
 * Returns the portion of [text] from [startIndex] to the end of [pattern]
 */
/*function GetSubstring (startIndex: int, pattern: String, text: String): String {
    var match: Match = Regex.Match (text.Substring (startIndex), pattern);

    if (match.Success)
        return text.Substring (startIndex, (startIndex+(match.Index+match.Length)) - startIndex);
    else
        return "";
}
*/
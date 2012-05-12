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
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions; // Directory.GetFiles() Directory.CreateDirectory() StreamReader  StreamWriter
using System.IO; // Regex.Replace(), Match, Matches, MatchCollection...


public struct Block {
	public Match match;

	public int startIndex;
	public int endIndex; // index of the opening and closing bracket of block in reftext
	public string refText; //

	public string name; // name of the function or class
	public string declaration; // full block's declaration (up util the opening bracket)
	public string text; // text inside the block between the opening and closing bracket
	public string newText;


	// ----------------------------------------------------------------------------------

	/// <summary>
	/// 
	/// </summary>
	/// <param name="match"></param>
	/// <param name="refText"></param>
	/// <param name="offset"></param>
	public Block (Match match, string refText, int offset) {
		this.match = match;
		declaration = match.Value;
		name = match.Groups[2].Value; // can't do that now, it depends of the regex, or I coult use match.Groups["name"]
		this.refText = refText;
		
		startIndex = match.Index + offset + match.Length - 1;
		endIndex = 0;
		text = "";
		newText = "";

		endIndex = this.GetEndOfBlockIndex ();
		
		if (endIndex <= startIndex) {
			Debug.LogWarning ("Block:Block() : endIndex <= startIndex. Can't get block text. match=["+match.Value+"] startIndex=["+startIndex+"] endIndex=["+endIndex+"] refText=["+refText+"].");
			return;
		}

		text = refText.Substring (startIndex, endIndex-startIndex);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="match"></param>
	/// <param name="refText"></param>
	public Block (Match match, string refText) : this (match, refText, 0) {}


	// ----------------------------------------------------------------------------------

	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
	int GetEndOfBlockIndex () {
		int openedBrackets = 1;

		for (int i = startIndex+1; i < refText.Length; i++) {
			//Debug.Log ("char at index["+i+" =["+refText[i]+"]");
			if (refText[i] == '{')
				openedBrackets++;

			if (refText[i] == '}') {
				openedBrackets--;

				if (openedBrackets == 0)
					return i+1;
			}
		}

		// no matching closing bracket has been found
		Debug.LogError ("Block:GetEndOfBlockIndex() : No matching closing bracket has been found ! Returning 0.  match=["+match.Value+"] startIndex=["+startIndex+"] ["+
			refText[startIndex-1]+"|"+refText[startIndex]+"|"+refText[startIndex+1]+"] text=["+refText+"].");
		return 0;
	}
}


public class JSToCSharp: MonoBehaviour {


    // custom classes names
    public string[] myClasses;

    // list of classes that exists in the pool of files that will be converted
    private List<string> ClassesList = new List<string> (); 


    // list of the files and their paths to be converted
    private string[] paths;

    // content of the file currently being converted
    private string file;
    private string fileName;
    // index of the file currently being converted
    private int fileIndex = 0;

    // regex that include most common expressions but arrays and generic collections
    private string commonName = "([A-Za-z0-9\\-_\\.]+)";

    // same as common name but includes also arrays and generic collections  
    private string commonChars = "([A-Za-z0-9<>,'\"\\-_\\[\\]\\.]+)"; // 
    private string commonCharsWithoutComma = "([A-Za-z0-9<>'\"\\-_\\[\\]\\.]+)"; 

    // white spaces
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

	//
	private int offset;

    // end of line
	public enum EndOfLine {
		CRLF_Win, // Window
        CR_Mac, // Mac
		LF_Unix // Unix
	}

	public EndOfLine endOfLine;
	
    private string EOL = "\r\n"; // works also with \r\n on Windows 7



    // ----------------------------------------------------------------------------------


    void Start () {
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

        // get the path of all fles to be converted
        paths = Directory.GetFiles (Application.dataPath+"/ScriptsToBeConverted", "*.js", SearchOption.AllDirectories);
        Debug.Log ("Beginning converting "+paths.Length+" files.");

        // read all file and make a list of all encountered classes
        BuildClassesList (paths);
        //BuildFunctionsList (paths);
    }


    // allow for a convertion of one file per frame
    void Update ()  {
        if (fileIndex < paths.Length) {
            string path = paths[fileIndex++];
            StreamReader reader = new StreamReader (path);
            file = reader.ReadToEnd ();
            reader.Close();

            path = path.Remove (path.Length-3).Replace ("ScriptsToBeConverted", "ConvertedScripts"); // remove ".js"

            int lastSlashIndex = path.LastIndexOf ("\\");
            fileName = path.Substring (lastSlashIndex+1);
            path = path.Remove (lastSlashIndex);

            ConvertFile ();

            Directory.CreateDirectory(path);
            StreamWriter writer = new StreamWriter (path+"/"+fileName+".cs");
            writer.Write (file);
            writer.Flush ();
            writer.Close ();
        }
    }


    private Rect rect = new Rect (0, 0, 100, 100);
    void OnGUI () {
        string text = "Begining Convertion ...";

        if (fileIndex < paths.Length)
            text = "Converting ... "+( (fileIndex+0.0f) * 100.0f / (paths.Length+0.0f) ).ToString ()+"%";
        else
            text = "Convertion done !";

        GUI.Label (rect, text);
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Read all file and make a list of all encountered classes
    /// </summary>
    void BuildClassesList (string[] paths) {
        foreach (string path in paths) {
            StreamReader reader = new StreamReader (path);
            file = reader.ReadToEnd ();
            reader.Close();

            fileName = path.Remove (path.Length-3).Substring (path.LastIndexOf ("\\")+1); // remove .js and get the part after the last slash
            ClassesList.Add (fileName); // always add the fileName because it will often be the public class name

            // search for class declaration pattern
            string pattern = "class"+oblWS+commonName+"("+oblWS+"extends"+oblWS+commonName+")?"+optWS+"{";
            MatchCollection matches = Regex.Matches (file, pattern);

            foreach (Match match in matches) {
                if ( ! ClassesList.Contains (match.Groups[2].Value))
                    ClassesList.Add (match.Groups[2].Value);
            }
        }
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
    /// Main function for the convertion
    /// </summary>
    void ConvertFile () {

        // Replace ArrayList() (Array form Unity that works only for JS) by ArrayList ()
            patterns.Add ( "Array"+optWS+"\\(" );
            replacements.Add ( "ArrayList$1(" );
            patterns.Add ( ":"+optWS+"(Array)"+optWS+"=" );
            replacements.Add ( ":$1ArrayList$3=" );

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


        // GetComponent (also works for GetComponentInChildren !?)

            //Getcomponent(T) GetComponent<T>() GetComponent<T>()  => Getcompoenent<T>()
            // GetComponent<T>() will have been modified to GetComponent<T>() at this point
            patterns.Add ( "(GetComponent|GetComponentInChildren)"+optWS+"<"+optWS+commonChars+optWS+">" );
            replacements.Add ( "$1<$4>" );
        
            patterns.Add ( "(GetComponent|GetComponentInChildren)"+optWS+"\\("+optWS+"[\"']{1}"+commonChars+"[\"']{1}"+optWS+"\\)" );
            replacements.Add ( "$1<$4>()" );

            patterns.Add ( "(GetComponent|GetComponentInChildren)"+optWS+"\\("+optWS+commonChars+optWS+"\\)" );
            replacements.Add ( "$1<$4>()" );


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
            replacements.Add ( "$1typeof($5)$6)" );


        // time for patching things up 

            // convert leftover string and Boolean
            patterns.Add ( "((public|private|protected)"+oblWS+")string("+optWS+"\\["+optWS+"\\])?" );
            replacements.Add ( "$1string$3");

            patterns.Add ( "((public|private|protected)"+oblWS+")bool("+optWS+"\\["+optWS+"\\])?" );
            replacements.Add ( "$1bool$3");

            // also in generic collections
            patterns.Add ( "((,|<)"+optWS+")?String("+optWS+"(,|>))?" ); // whitespaces should have been removed, but just in case...
            replacements.Add ( "$2string$6" );

            patterns.Add ( "((,|<)"+optWS+")?boolean("+optWS+"(,|>))?" ); // whitespaces should have been removed, but just in case...
            replacements.Add ( "$2bool$6" );


            // System.String got replaced by System.string
            patterns.Add ( "(System"+optWS+"."+optWS+")string" );
            replacements.Add ( "$1String" );

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


        Debug.Log ("Convertion done for ["+fileName+"]. Id="+fileIndex );
    }


    // ----------------------------------------------------------------------------------

    /// <summary> 
    /// Convert stuffs related to classes : declaration, inheritance, parent constructor call, Assembly imports
    /// </summary>
    void Classes () {
        // loop the classes declarations
        string pattern = "class"+oblWS+commonName+"("+oblWS+"extends"+oblWS+commonName+")?"+optWS+"{";
		MatchCollection classesMatches = Regex.Matches (file, pattern);
		MatchCollection matches;
        int fileOffset = 0;
       
        // list of functions and variable declaration that are found within a class
        List<string> classesContent = new List<string> ();
        
        foreach (Match classMatch in classesMatches) {
			Block _class  = new Block (classMatch, file, fileOffset);
			_class.name = classMatch.Groups[2].Value;

			if (_class.text.Trim() == "")
				continue;

			_class.newText = _class.text;

            // look for constructors in the modified class
			pattern = _class.name+optWS+"\\(.*\\)"+optWS+"{";
			MatchCollection constructorsMatches = Regex.Matches (_class.newText, pattern);
			int classNextTextOffset = 0;

			foreach (Match constructorMatch in constructorsMatches) {
				// here, we are inside one of the constructors of the current class (_class)
				Block constructor = new Block (constructorMatch, _class.newText, classNextTextOffset);
				// start and end index are relative to _class.text, not relative to file !


                // first task :
                // look for parent constructor call : super();
                pattern = "super"+optWS+"\\((.*)\\)"+optWS+";";
				Match parentConstructorCallMatch = Regex.Match (constructor.text, pattern);

				if (parentConstructorCallMatch.Success) {
					Match m = parentConstructorCallMatch;
					// the current constructor make a call to the parent class's constructor
					
					// add the new syntax to _class.newText
					_class.newText = _class.newText.Insert (constructor.startIndex-1, ": base("+m.Groups[2].Value+") ");
					
					// delete the old syntax from _class.newText :
					// first create the new constructor
					constructor.newText = constructor.text.Replace (m.Value, ""); 
					// then replace the old contructor by the new
					_class.newText = _class.newText.Replace (constructor.text, constructor.newText);

					// calculating the offset
					classNextTextOffset += 9 + m.Groups[2].Length; // what has been added to _class.newText
					classNextTextOffset -= m.Length; // what has been removed in constructor.newText, hence in _class.newText

                    continue;
                }


                // second task :
                // look for other class's constructo call
                pattern = _class.name+optWS+"\\((.*)\\)"+optWS+";";
                Match otherClassConstructorCallMatch = Regex.Match (constructor.text, pattern);

                if (otherClassConstructorCallMatch.Success) {
                    Match m = otherClassConstructorCallMatch;
                    // the current constructor call another constructor of the current class
                    
                    // add the new syntax to _class.newText
                    _class.newText = _class.newText.Insert (constructor.startIndex-1, ": this("+m.Groups[2].Value+") ");
                    
                    // delete the old syntax from _class.newText :
                    // first create the new constructor
                    constructor.newText = constructor.text.Replace (m.Value, ""); 
                    // then replace the old contructor by the new
                    _class.newText = _class.newText.Replace (constructor.text, constructor.newText);

                    // calculating the offset
                    classNextTextOffset += 9 + m.Groups[2].Length; // what has been added to _class.newText
                    classNextTextOffset -= m.Length; // what has been removed in constructor.newText, hence in _class.newText
                }

            } // end looping throught constructors of _class
            
			// we won't do more search/replace for this class _class.newText
			// now replace in file, the old _class.text by the new
			file = file.Replace (_class.text, _class.newText);
			fileOffset = (_class.text.Length - _class.newText.Length);


            // look for functions and var inside the class
			//int classStartIndex = classMatch.Index + offset + classMatch.Length;
			//int classEndIndex = GetEndOfBlockIndex (classStartIndex, file);
			
            pattern = "function"+oblWS+commonName+optWS+"\\(";
            matches = Regex.Matches (_class.text, pattern); // it doesn't matter here if the search is done in _class.text or newText beau the have thse functions and variables

			foreach (Match function in matches)
                classesContent.Add (function.Groups[2].Value);

            pattern = "var"+oblWS+commonName+optWS+"(:|;|=)";
			matches = Regex.Matches (_class.text, pattern);

            foreach (Match variable in matches)
				classesContent.Add (variable.Groups[2].Value);
		} // end looping through classes      foreach (Match classMatch in classesMatches)


        // we made a list of functions and variables inside classes
        // now make a list of functions and variable inside the file ...
        List<string> fileContent = new List<string> ();

        pattern = "function"+oblWS+commonName+optWS+"\\(";
        matches = Regex.Matches (file, pattern);

        foreach (Match function in matches)
			fileContent.Add (function.Groups[2].Value);

        pattern = "var"+oblWS+commonName+optWS+"(:|;|=)";
        matches = Regex.Matches (file, pattern);

        foreach (Match variable in matches)
			fileContent.Add (variable.Groups[2].Value);


        // ... compare the two list
        // if there is a difference, that mean that some variable or function declaration lies outside a public class
        // that means that the file is a MonoBehaviour derived public class
		if (classesMatches.Count == 0 || classesContent.Count != fileContent.Count) {// no class declaration within the file
            file = file.Insert (0, EOL+"public class "+fileName+" : MonoBehaviour {"+EOL);
            file = file+EOL+"} // end of class "+fileName; // the closing public class bracket
        }


        // Attributes / Flags
            // move some of them to the beginning of the file before converting
            pattern = "@"+optWS+"(script"+oblWS+")?RequireComponent"+optWS+"\\("+optWS+commonChars+optWS+"\\)("+optWS+";)?";
            matches = Regex.Matches (file, pattern);
            foreach (Match match in matches)
				file = file.Replace (match.Value, ""). Insert (0, match.Value);
                //file = file.Insert (0, "[RequireComponent$4(typeof($6))]").Replace (match.Value, "");

        
            pattern = "@"+optWS+"(script"+oblWS+")?ExecuteInEditMode"+optWS+"\\("+optWS+"\\)("+optWS+";)?";
            matches = Regex.Matches (file, pattern);
            foreach (Match match in matches)
                file = file.Replace (match.Value, "").Insert (0, "[ExecuteInEditMode]");

			patterns.Add ( "@"+optWS+"script" );
            replacements.Add ( "@" );

            //patterns.Add ( "@"+optWS+"script"+optWS+"AddComponentMenu("+optWS+"\\("+optWS+"\""+optWS+commonChars+optWS"\""+optWS+"\\))" );
            patterns.Add ( "@"+optWS+"AddComponentMenu(.*\\))("+optWS+";)?" );
            replacements.Add ( "[AddComponentMenu$2]" );

            //  => [RequireComponent (typeof(T))]
			//patterns.Add ( "@"+optWS+"RequireComponent"+optWS+"\\("+optWS+commonChars+optWS+"\\)("+optWS+";)?" );
			//replacements.Add ( "[RequireComponent$2(typeof($4))]" );

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

        // Assembly import, see in ConvertFile()
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Find the end of a block opened by a bracket
    /// Returns the index in [text] of the bracket that ends the block opened by the bracket for which [startIndex] is the index in [text]
    ///
    /// [param] startIndex The index in [text] of the block's opening bracket
    /// [param] text The text that contains the block
    ///
    /// [return] The index in [text] of the bracket that ends the block
    /// </summary>
    int GetEndOfBlockIndex (int startIndex, string text) {
        int openedBrackets = 1;
    
        for (int i = startIndex+1; i < text.Length; i++) {
            if (text[i] == '{')
                openedBrackets++;

            if (text[i] == '}') {
                openedBrackets--;

                if (openedBrackets == 0)
                    return i;
            }
        }

		// no matching closing bracket has been found
		Debug.LogError ("JSToCSharp:GetEndOfBlockIndex(int startindex, string text) : No matching closing bracket has been found ! Returning 0. startIndex=["+startIndex+"] ["+text[startIndex-1]+text[startIndex]+text[startIndex+1]+"] text=["+text+"].");
		return 0;
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Add the "new" keyword before classes instanciation where it is missing
    /// </summary>
    void AddNewKeyword () {
        // get pattern like "Type name = ClassOrMethod ();"  and search for "Class name = Class ();"
        pattern = "var"+oblWS+commonName+optWS+":"+optWS+commonChars+"("+optWS+"="+optWS+commonChars+optWS+"\\(.*\\)"+optWS+";)";
        MatchCollection matches = Regex.Matches (file, pattern);
        int offset = 0;

        foreach (Match match in matches) {
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
            // MatchCollection matches = Regex.Matches (file, pattern);
            // offset = 0;
            // foreach (Match match in matches) {
            //     file = file.Insert (match.Index+1+offset, " new"); // insert " new" just after the =
            //     offset += 4;
            // }

            pattern = "return"+oblWS+collections+optWS+"\\("; // when returning an empty instance
            matches = Regex.Matches (file, pattern);
            offset = 0;

            foreach (Match match in matches) {
                file = file.Insert (match.Groups[2].Index+offset, "new ");
                offset += 4;
            }

        // the code above can also be written as below :
        // it perform the regex search again each time a "new" keyword is added and pick the first result found until no more pattern is found
        // this way the offset thing is not needed but :
        // BEWARE : there is a risk of infinite loop if all matched patterns are not modified
        /*pattern = "";
        while (true) {
            Match match = Regex.Match (file, pattern);
            if ( ! match.Success)
                break;

            file = file.Insert (match.Index+1, " new");
        }*/


        // and Generic collections
            pattern = "="+optWS+genericCollections+"<"+commonChars+">"+optWS+"\\(";
            InsertInPatterns (pattern, 1, " new");
            // MatchCollection matches = Regex.Matches (file, pattern);
            // offset = 0;
            // foreach (Match match in matches) {
            //     file = file.Insert (match.Index+1+offset, " new");
            //     offset += 4;
            // }

            pattern = "return"+oblWS+genericCollections+optWS+"\\(";
            matches = Regex.Matches (file, pattern);
            offset = 0;

            foreach (Match match in matches) {
                file = file.Insert (match.Groups[2].Index+offset, "new ");
                offset += 4;
            }


        // append the myClasses list to ClassesList
        foreach (string _class in myClasses) {
            if ( ! ClassesList.Contains (_class))
                ClassesList.Add (_class);
        }


        // append the content of UnityClasses to ClassesList
        StreamReader reader = new StreamReader (Application.dataPath+"/UnityClasses.txt");

        while (true) {
            string _class = reader.ReadLine ();
            if (_class == null)
                break;

            ClassesList.Add (_class);
        }


        // and the public class in ClassesList
        foreach (string _class in ClassesList) {
            pattern = "="+optWS+_class+optWS+"\\(";
            InsertInPatterns (pattern, 1, " new");

            // do the same with return keyword
            pattern = "return"+oblWS+_class+optWS+"\\(";
            matches = Regex.Matches (file, pattern);
            offset = 0;

            foreach (Match match in matches) {
                file = file.Insert (match.Groups[2].Index+offset, "new ");
                offset += 4;
            }
        }

        // built a pattern like "(MyClass1|MyClass2|...)"
        /*string myClassesPattern = "(";

        foreach (string _class in myClasses)
            myClassesPattern += _class+"|";

        myClassesPattern = myClassesPattern.Remove (myClassesPattern.Length-1); // remove the last |
        myClassesPattern += ")";

        pattern = "="+optWS+myClassesPattern+optWS+"\\(";*/
        // MatchCollection matches = Regex.Matches (file, pattern);
        // offset = 0;
        // foreach (Match match in matches) {
        //     file = file.Insert (match.Index+1+offset, " new");
        //     ofset += 4;
        // }
    
    
    } // end AddNewKeyword ()


    // ----------------------------------------------------------------------------------

    /// <summary> 
    /// Insert [text] at the fixed position [patternOffset] in all [pattern]s found
    /// </summary>
    void InsertInPatterns (string pattern, int patternOffset, string text) {
        MatchCollection matches = Regex.Matches (file, pattern);
        int _offset = 0;

        foreach (Match match in matches) {
            file = file.Insert (match.Index + patternOffset + _offset, text);
            _offset += text.Length;
        }
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
        MatchCollection matches = Regex.Matches (file, pattern);
        offset = 0;

        foreach (Match match in matches) {
            int functionStartIndex = match.Index + offset + match.Length;
            int functionEndIndex = GetEndOfBlockIndex (functionStartIndex, file);
        
            if (functionEndIndex == 0) // appends for empty functions
                continue;

            string functionString = file.Substring (functionStartIndex, functionEndIndex-functionStartIndex); // it"s an issue if an array is declared within the void as this method is called before the JS array"s square brackets are converted to C# curly brackets
            if (functionString == "")
                Debug.Log ("=============== AddVisibility    empty function :"+fileName+"."+match.Groups[2].Value);

        
            // if (scriptList.Contains (fileName))
            //     Debug.Log ("============ "+fileName+"."+match.Groups[2].Value+" = "+functionString);

            patterns.Add ( "public"+oblWS+"var" );
            replacements.Add ( "var" );
            patterns.Add ( "public"+oblWS+"(static"+oblWS+"var)" );
            replacements.Add ( "$2" );

            string newfunctionString = DoReplacements (functionString);

            offset += (newfunctionString.Length-functionString.Length);

            if (functionString != "") // prevent a "ArgumentException: oldValue is the empty string." that happend somethimes
                file = file.Replace (functionString, newfunctionString);
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


        // =====================================================================================================================================================

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
        

        // other types (classes instantiation)    Test _test3 = new Test();

            // I can"t do anything if there is no "new" keyword, I can't guess the type of the variable and I can"t tell if Test() is a public class or a method
            // new keyword are already added everywhere they are needed by the method "AddNewKeyword()"
            patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"new"+oblWS+commonChars+optWS+"\\((.*);)" );
            replacements.Add ( "$7 $2" );



		// something float value gets 2 f ??
			patterns.Add ("([0-9]+\\.{1}[0-9]+)(f|F){2,}");
			replacements.Add ("$1f");


        // var declaration vithout a type and the value comes from a function
        // The type can be resolved if the function declaration is done in the file, but JS allows not to specify which type returns a void
        // Wait until the functions declarations are processed (in Functions()) to try to convert those variables
        

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

        /*
        protected int moo = 1;
        public int Foo {
		    get { return foo; }
		    public set { foo = value; }
	    }

    

        C#
        protected int _foo;
        public int Foo {
            get { return _foo; }
            protected set { _foo = value; }
        }*/

        // first, get all property getters (if a property exists, I assume that a getter always exists for it)
        pattern = "(public|private|protected)"+oblWS+"function"+oblWS+"get"+oblWS+commonName+optWS+"\\("+optWS+"\\)"+optWS+":"+optWS+commonChars+optWS+
        "{"+optWS+"return"+oblWS+commonName+optWS+";"+optWS+"}";
        MatchCollection matches = Regex.Matches (file, pattern);
        offset = 0;
        string unModifiedFile = file;

        foreach (Match match in matches) {
            string propName = match.Groups[5].Value;
            string variableName = match.Groups[14].Value;

            // now I have all infos nedded to start building the property in C#
            // match.Groups[1].Value is the getter's visibility. It always exists by now because it has been added by the AddVisibility() method above
            // match.Groups[10].Value is the getter's return type
            string property = match.Groups[1].Value+" "+match.Groups[10].Value+" "+propName+" {"+EOL 
            +"\t\tget { return "+variableName+"; }"+EOL; // getter


            // now look for the corresponding setter
            pattern = "(public|private|protected)"+oblWS+"function"+oblWS+"set"+oblWS+propName+".*}";
            Match setMatch = Regex.Match (unModifiedFile, pattern);

            if (setMatch.Success)
                property += "\t\t"+setMatch.Groups[1].Value+" set { "+variableName+" = value; }"+EOL; // setter

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

    /// <summary> 
    /// Convert function declarations
    /// Try to resolve the return type (if any) when there is none declared with the look of what is returned
    /// </summary>
    void Functions () {
        // search for return keyword to see if the void actually returns something, then try to resolve the return type with the returned variable type or returned value
        // if the void does not return anything, a "void" return type is added
        // if the type can't be resolved, a "MISSING_RETURN_TYPE" return type is added
        // coroutines gets a IEnumerator return type and thair call is wrapped by StartCoroutine( )

        pattern = "function"+oblWS+commonName+optWS+"\\(.*\\)"+optWS+"{"; // look for all JS functions that has no explicit return type
        MatchCollection functionMatches = Regex.Matches (file, pattern);
        int fileOffset = 0;

		foreach (Match functionMatch in functionMatches) {
			Block function = new Block (functionMatch, file, fileOffset);
			function.name = functionMatch.Groups[2].Value;

			Debug.Log ("function="+function.name+" string=["+function.text+"].");
			

            // look for return keyword patterns
            pattern = "return.+;";

			if (!Regex.Match (function.text, pattern).Success) { // no return keyword : add "void" return type
                file = file.Insert (function.startIndex-1, ": void "); 
                fileOffset += 7;
                continue;
            }

        
            // Below this point, we know that the void returns some value but we don't know the type yet
            // Look in functionString for sevral return pattern 
        
            // yield / coroutine
            pattern = "yield"+oblWS+"return";

			if (Regex.Match (function.text, pattern).Success) { 
                file = file.Insert (function.startIndex-1, ": IEnumerator ");
                fileOffset += 14;

                // In C# (and Boo), a coroutine call must be wrapped by the StartCoroutine() method : StartCoroutine( CoroutineName() );
                // The current void is a coroutine, so search for it's call in the file
                pattern = "("+function.name+optWS+"\\(.*\\))("+optWS+";)";
                MatchCollection coroutinesMatches = Regex.Matches (file, pattern);

                foreach (Match coroutine in coroutinesMatches) {
					string coroutineCall = "StartCoroutine( "+coroutine.Groups[1].Value+" );";

					file = file.Replace (coroutine.Value, coroutineCall);
					fileOffset += (19-coroutine.Groups[3].Length); // 19 is the length of "StartCoroutine(  );"    match.Groups[3].Length is the length of 'optWS+";"'
                }

                continue;
            }


            // this pattern will match an int, a float, a bool ar a variable name
            pattern = "return"+oblWS+commonName+optWS+";";
			Match variableMatch = Regex.Match (function.text, pattern);
            string variableName = "";

			if (variableMatch.Success) {
				variableName = variableMatch.Groups[2].Value;
				
                // bool
                if (Regex.Match (variableName, "^(true|false)$").Success) { 
                    file = file.Insert (function.startIndex-1, ":bool");
                    fileOffset += 5;
                    continue;
                }

                // float
                if (Regex.Match (variableName, "^-?[0-9]+\\.{1}[0-9]+(f|F){1}$").Success) { 
                    file = file.Insert (function.startIndex-1, ":float");
					fileOffset += 7;
                    continue;
                }

				// double
				if (Regex.Match (variableName, "^-?[0-9]+\\.{1}[0-9]+(f|F){0}$").Success) {
					file = file.Insert (function.startIndex-1, ": double");
					fileOffset += 7;
					continue;
				}

                // int
                if (Regex.Match (variableName, "^-?[0-9]+$").Success) { 
                    file = file.Insert (function.startIndex-1, ":int");
					fileOffset += 4;
                    continue;
                }

                // variableName seems to be a variable name after all
                // search for the variable declaration in the function
                // variable declarations are already C#-style
                pattern = commonChars+oblWS+variableName+optWS+"="; // it will also match non converted var declarations : "public var _theVariable ="
				variableMatch = Regex.Match (function.text, pattern);

				if (variableMatch.Success && variableMatch.Groups[1].Value != "var") {
					file = file.Insert (function.startIndex-1, ": "+variableMatch.Groups[1].Value+" ");
					fileOffset += (3 + variableMatch.Groups[1].Length);
                }
                else { // declaration not found in the function, maybe it's somewhere in the file
					variableMatch = Regex.Match (file, pattern);

					if (variableMatch.Success && variableMatch.Groups[1].Value != "var") {

						file = file.Insert (function.startIndex-1, ":"+variableMatch.Groups[1].Value);
						fileOffset += (1 + variableMatch.Groups[1].Length);
                    }
                    else { // no, it's really anywhere ...
                        file = file.Insert (function.startIndex-1, ":MISSING_VAR_TYPE");
                        fileOffset += 17;
                    }
                }

                continue;
            }


            // this pattern will match string and char
            pattern = "return"+oblWS+commonChars+optWS+";";
            Match stringCharMatch = Regex.Match (function.text, pattern);

			if (stringCharMatch.Success) {
				variableName = stringCharMatch.Groups[2].Value;
            
                // char
                pattern = "(\"|'){1}"+commonChars+"(\"|'){1}"+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]";

                if (Regex.Match (variableName, pattern).Success) { 
                    file = file.Insert (function.startIndex-1, ": char ");
                    fileOffset += 7;
                    continue;
                }

                // string
                pattern = "(\"|'){1}"+commonChars+"(\"|'){1}";

                if (Regex.Match (variableName, pattern).Success) {
                    file = file.Insert (function.startIndex-1, ": string ");
                    fileOffset += 9;
                    continue;
                }
            }


            // class instanciation  return new Class ();
            pattern = "return"+oblWS+"new"+oblWS+commonName;
            Match classMatch = Regex.Match (function.text, pattern);

			if (classMatch.Success) {
				file = file.Insert (function.startIndex-1, ": "+classMatch.Groups[3].Value+" ");
				fileOffset += (3 + classMatch.Groups[3].Length);
                continue;
            }


            // look for "empty" return keyword, allowed in void functions
            pattern = "return"+optWS+";";

            if (Regex.Match (function.text, pattern).Success) { 
                file = file.Insert (function.startIndex-1, ": void "); // functionStartIndex-1 is the index of the opening bracket
                fileOffset += 7;
                continue;
            }

            // can't resolve anything ...
            file = file.Insert (function.startIndex-1, ": MISSING_RETURN_TYPE ");
            fileOffset += 22;
        } // end looping function declarations
    

        // now actually convert the declaration that have a return type (all functions should, even if it's the missing return type)
        patterns.Add ( "function"+oblWS+commonName+"("+optWS+"\\((.*)\\))"+optWS+":"+optWS+commonChars );
        replacements.Add ( "$8 $2$3" );

        // without return type (not needed anymore)
        // patterns.Add ( "function"+oblWS+commonChars+"("+optWS+"\\((.*)\\))" ); // classes constructor will get a void return type
        // replacements.Add ( "void $2$3" );


        // classes constructors gets a void return type that has to be removed
        pattern = "class"+oblWS+commonName;
        MatchCollection matches = Regex.Matches (file, pattern);

        foreach (Match match in matches) {
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

    /// <summary> 
    /// Now that all functions have a return type, try to convert the few variable whose value is returned from a function
    /// </summary>
    void VariablesTheReturn () {
        pattern = "var"+oblWS+"("+commonName+optWS+"="+optWS+commonName+optWS+"\\()";
        MatchCollection matches = Regex.Matches (file, pattern);

        foreach (Match match in matches) {
            string variableName = match.Groups[3].Value;
            string functionName = match.Groups[6].Value;
        
            // look for the function declaration that match the function name
            pattern = commonChars+oblWS+functionName+optWS+"\\(";
            Match function = Regex.Match (file, pattern);

            if (function.Success) { // function declaration found in file    function.Groups[1].Value is the return type
                patterns.Add ( "var("+oblWS+variableName+optWS+"="+optWS+functionName+optWS+"\\()" );
                replacements.Add ( function.Groups[1].Value+"$1" );
            }
        }

        DoReplacements ();
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Try to resolve the return type of function where it wasn't possible before mostly because the type of 
    /// the returned variable couldn't be resolved before the first pass of function converting
    /// </summary>
    void FunctionsTheReturn () {
        pattern = "MISSING_VAR_TYPE("+oblWS+commonName+optWS+"\\(.*\\)"+optWS+"{)"; // look only for function where the return type is still unresolved
        MatchCollection functionsMatches = Regex.Matches (file, pattern);
		int fileOffset = 0;

		foreach (Match functionMatch in functionsMatches) {
			Block function = new Block (functionMatch, file, fileOffset);
			// here, function.newText will actually be the new declaration, and not the new function content
			
            // search only for variable return pattern     all functions with a "MISSING_VAR_TYPE" should return a variable
            pattern = "return"+oblWS+commonName+optWS+";"; 
            Match variableMatch = Regex.Match (function.text, pattern);

			if (variableMatch.Success) { // if the current function returns a variable
				string variableName = variableMatch.Groups[2].Value;

                // search for the variable declaration in the function
                pattern = commonChars+oblWS+variableName+optWS+"="; // it will also match non converted var declarations : "public var _theVariable ="
				Match varDeclarationMatch = Regex.Match (function.text, pattern);

				if (varDeclarationMatch.Success && varDeclarationMatch.Groups[1].Value != "var") // var declaration found in the void
					function.newText = function.declaration.Replace ("MISSING_VAR_TYPE", varDeclarationMatch.Groups[1].Value); // function.declaration == functionMatch.Value
                
                else { // declaration not found in the function, maybe it's somewhere in the public class
                    // search the whole file
					varDeclarationMatch = Regex.Match (file, pattern);

					if (varDeclarationMatch.Success && varDeclarationMatch.Groups[1].Value != "var")
						function.newText = function.declaration.Replace ("MISSING_VAR_TYPE", varDeclarationMatch.Groups[1].Value);
                
                    else // no, it's really anywhere ...
						continue; // do nothing, leave the MISSING_VAR_TYPE
                }

                file = file.Replace (function.declaration, function.newText);
				fileOffset += (function.newText.Length - function.declaration.Length);
            }
			// else do nothing, leave the MISSING_VAR_TYPE    that should actually never happend
        } // end looping functions
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// read all file and make a list of all encountered functions, if they have a return type
    /// </summary>
	/*
    //void BuildFunctionsList (string[] paths) {
        
        foreach (string path in paths) {
            public StreamReader reader = new StreamReader (path);
            public string[] _file = reader.ReadToEnd ().Split ("\n"[0]);
            reader.Close();

        

            // loop public class declaration pattern
            pattern = "class"+oblWS+commonName+"("+oblWS+"extends"+oblWS+commonName+")?"+optWS+"{";
            public MatchCollection matches = Regex.Matches (_file, pattern);

            foreach (Match match in matches) {
                public string  className = match.Groups[2].Value;

                // lopp void declarations
                pattern = "function"+oblWS+commonName+optWS+"\\(.*\\)"+optWS+":"+optWS+commonChars+optWS+"{";
                public MatchCollection functions = Regex.Matches (_file, pattern);

                foreach (Match match in functions) {
                    FunctionsList.Add (new FunctionInfos (className, match.Groups[2].Value, match.Groups[6].Value));
                }
            }

            // there may be functions outside a class declaration
            public string  _fileName = path.Remove (path.Length-3).Substring (path.LastIndexOf ("\\")+1); // remove .js and get the part after the last slash

            pattern = "function"+oblWS+commonName+optWS+"\\(.*\\)"+optWS+":"+optWS+commonChars+optWS+"{";
            public MatchCollection functions = Regex.Matches (_file, pattern);
            foreach (Match match in functions) {

                if ()


                FunctionsList.Add (new FunctionInfos (className, match.Groups[2].Value, match.Groups[6].Value));
            }
        }*/
    //}


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Tell if pattern can be found in text, begining at startIndex
    /// startIndex is the index in text of the first char of pattern 
    /// </summary>
    // public bool IndexIsTheStartOf (int startIndex,string pattern,string text) {
    //     // if ( ! pattern.StartsWith ("(') && ! pattern.EndsWith (')"))
    //     //     pattern = "('+pattern+')";

    //     Match match = Regex.Match (text.Substring (startIndex), pattern);

    //     if (match.Success && match.Index == 0)
    //         return true;
    //     else
    //         return false;

    //     /// <summary>for (int i = 0; i < pattern.Length; i++) {
    //         if (text[startIndex+i] != pattern[i])
    //             return false;
    //     }

    //     return true;*/
    // }




    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Returns the portion of [text] from [startIndex] to the end of [pattern]
    /// </summary>
    /*string GetSubstring (int startIndex,string pattern,string text) {
        Match match = Regex.Match (text.Substring (startIndex), pattern);

        if (match.Success)
            return text.Substring (startIndex, (startIndex+(match.Index+match.Length)) - startIndex);
        else
            return "";
    }
   */
} // end of public class JSToCSharp
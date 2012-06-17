
using UnityEngine;
using UnityEditor;
using System.Collections; // Stack
using System.Collections.Generic;
using System.Text.RegularExpressions;


public class CSharpToUnityScript : EditorWindow {

    public struct Block {
        public Match match;

        public int startIndex;
        public int endIndex; // index of the opening and closing bracket of block in reftext
        public string refText; //

        public string name; // name of the block (Block is used only with functions or classes)
        public string type;
        public string declaration; // full block's declaration (up util the opening bracket). Usually the match's value
        public string newDeclaration;
        public string text; // text inside the block between the opening and closing bracket
        public string newText;

        public bool isEmpty; // tell wether text is empty or not


        // ----------------------------------------------------------------------------------

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_match"></param>
        /// <param name="p_refText"></param>
        public Block (Match p_match, string p_refText) {
            match = p_match;
            refText = p_refText;

            declaration = match.Value;
            newDeclaration = "";
            
            name = match.Groups[2].Value;

            try {
                name = match.Groups["blockName"].Value;
            }
            catch {}

            type = "";
            try {
                type = match.Groups["blockType"].Value;
            }
            catch {}
            
            startIndex = match.Index + match.Length - 1;
            endIndex = 0;
            text = "";
            newText = "";
            isEmpty = true;

            endIndex = GetEndOfBlockIndex ();
            
            if (endIndex == -1)
                return;

            if (endIndex <= startIndex) {
                Debug.LogWarning ("Block::Block() : endIndex <= startIndex. Can't get block text. match=["+match.Value+"] startIndex=["+startIndex+"] endIndex=["+endIndex+"] refText=["+refText+"].");
                return;
            }

            text = refText.Substring (startIndex, endIndex-startIndex);
            isEmpty = (text.Trim() == "");
            newText = text;
        }


        // ----------------------------------------------------------------------------------

        /// <summary>
        /// Look for the block's closing curcly bracket, given the index in refText (startIndex) of the opening bracket
        /// </summary>
        int GetEndOfBlockIndex () {
            int openedBrackets = 0;

            for (int i = startIndex; i < refText.Length; i++) {
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
            Debug.LogError ("Block::GetEndOfBlockIndex() : No matching closing bracket has been found ! Returning -1. match=["+match.Value+"] startIndex=["+startIndex+"] ["+
                refText[startIndex-1]+"|"+refText[startIndex]+"|"+refText[startIndex+1]+"] text=["+refText+"].");
            return -1;
        }
    }


    // ----------------------------------------------------------------------------------


    public struct Script { // can't use FILE since it's a class in System.IO
        public string path;
        public string name;
        public string text;
        public string newText;

        public Script (string _path, string _text) {
            path = _path.Remove (_path.Length-3); // remove ".js"
            
            int lastIndexOf = path.LastIndexOf ("\\");
            name = path.Substring (lastIndexOf+1); // isolate the name
            path = path.Replace (name, "");// the path ends by a slash
            
            text = _text;
            newText = "";
        }
    }


    // ----------------------------------------------------------------------------------


	// regex that include most common expressions but arrays and generic collections
    protected static string commonName = "([A-Za-z0-9_\\.]+)";

    // same as common name but includes also arrays and generic collections  
    protected static string commonChars = "([A-Za-z0-9<>,'\"_\\[\\]\\.]{2,})"; // 
    protected static string commonCharsWithoutComma = "([A-Za-z0-9<>'\"_\\[\\]\\.]{2,})"; // for use with variable or type as function argument

    protected static string argumentsChars = "([A-Za-z0-9<>,:_\\[\\]\\s]*)"; // characters seen in function arguments

    // white spaces (and/or tabs, and/or new line)
    protected static string optWS = "(\\s*)"; // optionnal white space   0 or more white space
    protected static string oblWS = "(\\s+)"; // obligatory white space  1 or more white space
    protected static string oblSpaces = "( +)"; // space only not tab, cariage return...

    protected static string collections = "(ArrayList|BitArray|CaseInsensitiveComparer|Comparer|Hashtable|Queue|SortedList|Stack|StructuralComparisons|DictionnaryEntry"+
        "|ICollection|IComparer|IDictionary|IDictionaryEnumerator|IEnumerable|IEnumerator|IEqualityComparer|IHashCodeProvider|IList|IStructuralComparable|IStructuralEquatable)";
    protected static string genericCollections = "(Comparer|Dictionary|HashSet|KeyedByTypeCollection|LinkedList|LinkedListNode|List|Queue|SortedDictionary|SortedList|SortedSet|Stack|SynchronizedCollection"+
        "|SynchronizedKeyedCollection|SynchronizedReadOnlyCollection|ISet)";

    // list of the patterns and corresponding replacements to be processed by DoReplacements()
    protected static List<string> patterns = new List<string> ();
    protected static List<string> replacements = new List<string> ();
	protected static string pattern;
    protected static string replacement;
	
    // end of line
    protected static string EOL = "\n"; // works also with on Windows 7    


    //--------------------


    // a list of structure that contains all needed infos about the script to be converted
    protected static List<Script> scriptsList = new List<Script> ();

    // the script currently being converted
    protected static Script script;
    
    // a list of classes that exists in the pool of files that will be converted
    protected static List<string> classesList = new List<string> ();

    // a list of items (variable or functions) and their coresponding type
    protected static Dictionary<string, string> itemsAndTypes = new Dictionary<string, string> ();

    // list of classes and their items (variable or function) and corresponding type
    protected static Dictionary<string, Dictionary<string, string>> projectItems = new Dictionary<string, Dictionary<string, string>> ();

    protected static List<string> importedAssemblies = new List<string> ();
    

    // ----------------------------------------------------------------------------------


    /// <summary>
    /// Process the patterns/replacements
    /// </summary>
    protected static void DoReplacements () {
        script.text = DoReplacements (script.text);
    }

    protected static string DoReplacements (string text) {
        if (patterns.Count != replacements.Count) {
            Debug.LogError ("Patterns and replacements count mismatch : patterns.Count="+patterns.Count+" replacements.Count="+replacements.Count);
            return text;
        }

        try { // some regex throws nasty exceptions
            //Debug.LogWarning (patterns.Count+" "+replacements.Count+" "+i);
            for (int i = 0; i < patterns.Count; i++) {
                //Debug.LogWarning (i+" | pattern="+patterns[i]+" | replacement="+replacements[i]);
                text = Regex.Replace (text, patterns[i], replacements[i]);
            }

            patterns.Clear ();
            replacements.Clear ();
        }
        catch (System.Exception e) {
            Debug.LogError (patterns.Count+" "+replacements.Count+" "+e);
            Debug.Log (text.Substring(100));
        }

        return text;
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Do a Regex.Matches but return the result in the inverse order
    /// </summary>
    protected static List<Match> ReverseMatches (string text, string pattern) {
        //Debug.Log ("Reversematch patern="+pattern+" | text="+text.Substring(50));

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
} // end of CSharpToUnityScript class


using UnityEngine;
using UnityEditor;
using System.Collections; // Stack
using System.Collections.Generic;
using System.Text.RegularExpressions;


public class UnityScriptToCSharp: EditorWindow {
	// regex that include most common expressions but arrays and generic collections
    protected static string commonName = "([A-Za-z0-9\\-_\\.]+)";

    // same as common name but includes also arrays and generic collections  
    protected static string commonChars = "([A-Za-z0-9<>,'\"\\-_\\[\\]\\.]+)"; // 
    protected static string commonCharsWithoutComma = "([A-Za-z0-9<>'\"\\-_\\[\\]\\.]+)"; 

    // white spaces (and/or tabs, and/or new line)
    protected static string optWS = "(\\s*)"; // optionnal white space   0 or more white space
    protected static string oblWS = "(\\s+)"; // obligatory white space  1 or more white space

    protected static string  collections = "(ArrayList|BitArray|CaseInsensitiveComparer|Comparer|Hashtable|Queue|SortedList|Stack|StructuralComparisons|DictionnaryEntry"+
        "|ICollection|IComparer|IDictionary|IDictionaryEnumerator|IEnumerable|IEnumerator|IEqualityComparer|IHashCodeProvider|IList|IStructuralComparable|IStructuralEquatable)";
    protected static string  genericCollections = "(Comparer|Dictionary|HashSet|KeyedByTypeCollection|LinkedList|LinkedListNode|List|Queue|SortedDictionary|SortedList|SortedSet|Stack|SynchronizedCollection"+
        "|SynchronizedKeyedCollection|SynchronizedReadOnlyCollection|ISet)";

    // list of the patterns and corresponding replacements to be processed by DoReplacements()
    protected static List<string> patterns = new List<string> ();
    protected static List<string> replacements = new List<string> ();
	protected static string pattern;
	
    protected static string EOL = "\r\n"; // works also with on Windows 7


    // a list of structure that contains all needed infos about the files to be converted
    protected static List<Script> scriptsList = new List<Script> ();

    protected static Script script;
    
    // a list of classes that exists in the pool of files that will be converted
    protected static List<string> classesList = new List<string> ();


    // ----------------------------------------------------------------------------------


    /// <summary>
    /// Process the patterns/replacements
    /// </summary>
    protected static void DoReplacements () {
        script.text = DoReplacements (script.text);
    }

    protected static string DoReplacements (string text) {
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
    protected static List<Match> ReverseMatches (string text, string pattern) {
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
}
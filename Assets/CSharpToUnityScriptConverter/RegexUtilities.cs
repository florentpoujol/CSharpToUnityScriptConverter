/// <summary>
/// RegexUtilities class for Unity3D
///
/// This class provide some structs, variables and methods 
///   to facilitate the use of regexes by the CSharpToUnityScriptConverter class
///
/// Version : Goes with CSharpToUnityScriptConverter v1.0
/// Release : 02 december 2012 
///
/// Created by Florent POUJOL
/// florent.poujol@gmail.com
/// http://www.florent-poujol.fr/en
/// Profile on Unity's forums : http://forum.unity3d.com/members/23148-Lion
/// </summary>


using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;


public class RegexUtilities 
{

    /// <summary>
    /// A block is what is writtent between an opening and its matching closing curly brackets
    /// This struct
    /// </summary>
	public struct Block 
    {
        public Match match; // the Match of the block's declaration

        public string refText; // the text in which the match has been found, and where to search for the block
        public int startIndex;
        public int endIndex; // index of the opening and closing bracket of block in reftext
        
        public string name; // name of the block (Block is used only with methods or classes)
        public string type; // type if the block is a method

        public string declaration; // full block's declaration (up util the opening bracket). Usually the match's value (match.Value)
        public string newDeclaration;

        public string text; // text inside the block between the opening and closing bracket which are included
        public string newText;

        public bool isEmpty; // tell wether text is empty or not


        // ----------------------------------------------------------------------------------

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_match">Match of the block's declaration</param>
        /// <param name="p_refText">full reference text in which to search for the block</param>
        public Block(Match p_match, string p_refText) 
        {
            match = p_match;
            refText = p_refText;

            startIndex = match.Index + match.Length - 1;
            endIndex = 0;

            name = match.Groups[2].Value;
            try { // match.Groups has no ContainsKey() method
                name = match.Groups["blockName"].Value;
            } catch {}

            type = "";
            try {
                type = match.Groups["blockType"].Value;
            } catch {}
            
            declaration = match.Value;
            newDeclaration = "";

            text = "";
            newText = "";

            isEmpty = true;

            endIndex = GetEndOfBlockIndex();
            
            if (endIndex == -1)
                return;

            if (endIndex <= startIndex) 
            {
                Debug.LogError ("RegexUtilities.Block.Block() : endIndex <= startIndex. Can't get block text. match=["+match.Value+"] startIndex=["+startIndex+"] endIndex=["+endIndex+"] refText=["+refText+"].");
                return;
            }

            text = refText.Substring( startIndex, endIndex-startIndex ); // the openeing and closing brackets are included in text
            isEmpty = (text.Trim() == "");
            newText = text;
        }


        // ----------------------------------------------------------------------------------

        /// <summary>
        /// Search for the block's closing curcly bracket, given the index in refText (startIndex) of the opening bracket
        /// </summary>
        /// <returns>The index in refText of the closing bracket, if one is found</returns>
        int GetEndOfBlockIndex() 
        {
            int openedBrackets = 0;

            for (int i = startIndex; i < refText.Length; i++) 
            {
                if (refText[i] == '{')
                    openedBrackets++;

                if (refText[i] == '}') 
                {
                    openedBrackets--;

                    if (openedBrackets == 0)
                        return i+1;
                }
            }

            // no matching closing bracket has been found
            Debug.LogError("RegexUtilities.Block.GetEndOfBlockIndex() : No matching closing bracket has been found ! Returning -1. match=["+match.Value+"] startIndex=["+startIndex+"] ["+
                refText[startIndex-1]+"|"+refText[startIndex]+"|"+refText[startIndex+1]+"] text=["+refText+"].");
            return -1;
        }
    } // end of struct Block


    // ----------------------------------------------------------------------------------


 	// most common characters used in names. Does not match arrays or generic collections
    protected string commonName = "([A-Za-z_]{1}[A-Za-z0-9_\\.]*)";
    protected string commonNameWithoutDot = "([A-Za-z_]{1}[A-Za-z0-9_]*)";
    protected string commonNameWithSpace = "([A-Za-z_]{1}[A-Za-z0-9_\\. ]*)"; // Allows to have space in expression like "System . Something"
    protected string commonNameWithSpaceAndComa = "([A-Za-z_]{1}[A-Za-z0-9_\\. ,]*)";

    // same as common name but includes also arrays, generic collections, strings
    // usefull when looking for a type (of variable or method)
    protected string commonChars = "([A-Za-z_]{1}[A-Za-z0-9<>,'\"_\\[\\]\\.]*)"; // 
    protected string commonCharsWithSpace = "([A-Za-z_]{1}[A-Za-z0-9<>,'\"_\\[\\]\\. ]*)"; // generic collections likes dictionnaries may have a space after the coma, for instance
    protected string commonCharsWithSpaceAndParenthesis = "([A-Za-z_]{1}[A-Za-z0-9<>,'\"_\\[\\]\\. \\(\\)]*)";
    protected string commonCharsWithoutComma = "([A-Za-z_]{1}[A-Za-z0-9<>'\"_\\[\\]\\.]*)"; // for use with variable or type as method parameter
    protected string commonCharsWoCommaWSpace = "([A-Za-z_]{1}[A-Za-z0-9<>'\"_\\[\\]\\. ]*)";

    // white spaces (or new line)
    protected string optWS = "(\\s|\\n)*"; // optionnal white space
    protected string oblWS = "(\\s|\\n)+"; // obligatory white space
    protected string oblSpaces = "( +)"; // space only not tab, cariage return...
    protected string optSpaces = "( *)";


    // any numerical value (including the "f" for the float in C#)
    protected string number = "(-?[0-9]+(\\.{1}[0-9]+(f|F)?)?)";
    // same but makes the "f" mandatory for float
    protected string cSharpFloat = "(-?[0-9]+(\\.{1}[0-9]+(f|F){1})?)";
    
    // list of characters that may be found before and after an instruction
    //protected string instructionStart = "(?<instructionStart>({|}|;|\\(|\\)|else|:|\\?|,)\\s*)";
    //protected string instructionEnd = "(?<instructionEnd>(;|\\))\\s*)";

    protected string allButParenthesis = "([^\\(\\)]*)";

    // characters seen in method parameters (works for both C# and UnityScript parameters)
    protected string argumentsChars = "([A-Za-z0-9<>,:_\\[\\]\\s\\n]*)"; 

    // names separated by coma (likes Interface in class declaration)
    protected string commaSeparatedNames = "([A-Za-z0-9_\\. ,]+)"; // common Name + space and coma
    // redundant with commonNameWithSpaceAndComa

    protected string visibility = "(?<visibility>public|private|protected)";
    protected string visibilityAndStatic = "(public\\s+static|static\\s+public|public|"+
                                            "private\\s+static|static\\s+private|private|"+
                                            "protected\\s+static|static\\s+protected|protected|"+
                                            "static|override|abstract)";

    /* 
    // same with delegate and event
    protected string visibilityAndStatic = "(public\\s+static|static\\s+public|public|public\\s+static\\sevent|public\\delegate|"+
                                            "private\\s+static|static\\s+private|private|private\\s+static\\sevent|private\\delegate|"+
                                            "protected\\s+static|static\\s+protected|protected|protected\\s+static\\sevent|protected\\delegate|"+
                                            "static|override|abstract|delegate)";
    */
    //protected string methodPrefix = "(?<ethodPrefix>public|private|protected|static|override|abstract)";

    

    //protected string anyCharsAndNewLine = "([.\\n]*)"; // any chars + New Line char


    protected string collections = "(ArrayList|BitArray|CaseInsensitiveComparer|Comparer|Hashtable|Queue|SortedList|Stack|StructuralComparisons|DictionnaryEntry"+
        "|ICollection|IComparer|IDictionary|IDictionaryEnumerator|IEnumerable|IEnumerator|IEqualityComparer|IHashCodeProvider|IList|IStructuralComparable|IStructuralEquatable)";
    protected string genericCollections = "(Comparer|Dictionary|KeyValuePair|HashSet|KeyedByTypeCollection|LinkedList|LinkedListNode|List|Queue|SortedDictionary"+
        "|SortedList|SortedSet|Stack|SynchronizedCollection|SynchronizedKeyedCollection|SynchronizedReadOnlyCollection|"+
        "ICollection|IComparer|IDictionary|IEnumerable|IEnumerator|IEqualityComparer|IList|IReadOnlyCollection|IReadOnlyDictionary|IReadOnlyList|ISet|"+
        "Action|Func)";

    // regular data types (value types)
    protected string regularTypes = "(byte|char|string|String|short|int|long|float|double|decimal|bool|boolean|"+
                                    "byte\\[\\]|char\\[\\]|string\\[\\]|String\\[\\]|short\\[\\]|int\\[\\]|long\\[\\]|float\\[\\]|double\\[\\]|decimal\\[\\]|bool\\[\\]|boolean\\[\\])";

    // end of line
    protected string EOL = System.Environment.NewLine; // may throw some "inconsistent line ending blabla" warnings in the console

    // list of the patterns and corresponding replacements to be processed by DoReplacements()
    protected List<string> patterns = new List<string>();
    protected List<string> replacements = new List<string>();

	protected string pattern;
    protected string replacement;

    protected MatchCollection allMatches;
	
    // translated code to be returned
    public string convertedCode = "";

 
    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Process the patterns/replacements on convertedCode
    /// </summary>
    protected void DoReplacements() {
        convertedCode = DoReplacements( convertedCode );
    }

    /// <summary>
    /// Process the patterns/replacements on the input text
    /// </summary>
    /// <param name="text">The string to applies the patterns/replacements to</param>
    /// <returns>The input string on to which all patterns/replacements have been applied to</returns>
    protected string DoReplacements( string text ) {
        if( patterns.Count != replacements.Count ) {
            Debug.LogError( "Patterns and replacements count mismatch : patterns.Count="+patterns.Count+" replacements.Count="+replacements.Count );
            return text;
        }

        try { // some regex may throws nasty exceptions
            for( int i = 0; i < patterns.Count; i++ ) {
                // Log( "pattern="+patterns[i] );
                // Log( "replacement="+replacements[i] );
                text = Regex.Replace( text, patterns[i], replacements[i] );
            }
        }
        catch( System.Exception exception ) {
            Debug.LogError( "Error in patterns/replacements." );
            Debug.LogError( exception );
            Debug.LogWarning( text.Substring( 0, 100 ) );
        }

        patterns.Clear();
        replacements.Clear();

        return text;
    }


    // ----------------------------------------------------------------------------------

    static StreamWriter writer;

    /// <summary>
    /// Log infos
    /// </summary>
    protected void Log( string line ) {
        // if( writer == null )
        //     writer = new StreamWriter( Application.dataPath+"/GitIgnore/csharptounityscript_log.txt", false ); // empty the file first

        // writer.WriteLine( line );
        // writer.Flush();
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Do a Regex.Matches but returns the result in the inverse order
    /// </summary>
    /// <param name="text">The string to search the pattern in</param>
    /// <param name="pattern">The pattern to search for in text</param>
    /// <returns>A List<Match> containing the result of the Regex.Matches() in the inverse order</returns>
    protected List<Match> ReverseMatches( string text, string pattern ) {
        Stack<Match> stack = new Stack<Match>();
        MatchCollection matches = Regex.Matches( text, pattern );
        
        foreach( Match match in matches )
            stack.Push( match );
        // the matches piles up in the stack, so the lastest match in matches is now the first one in stack

        return new List<Match>(stack);
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Escape some chars in strings that must be used in Regexes
    /// </summary>
    /// <param name="input">The string in which to escape regex characters</param>
    /// <returns>The string where the relevant characters have been escaped</returns>
    protected string EscapeRegexChars( string input ) {
        string[] chars = { "[", "]", ".", "|", "(", ")", "*", "+", "?", "{", "}" };

        foreach( string _char in chars )
            input = input.Replace( _char, Regex.Escape(_char) );

        return input;
    }
} // end of class RegexUtilities
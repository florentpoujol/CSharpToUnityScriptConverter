



using System.Collections.Generic;
using System.Text.RegularExpressions; 


public class UnityScriptToCSharp_Classes: UnityScriptToCSharp {
	                                      
    /// <summary> 
    /// Convert stuffs related to classes : declaration, inheritance, parent constructor call, Assembly imports
    /// </summary>
    public static void Classes () {
        // loop the classes declarations
        pattern = "class"+oblWS+commonName+"("+oblWS+"extends"+oblWS+commonName+")?"+optWS+"{";
        List<Match> allClasses = ReverseMatches (script.text, pattern);

        // list of functions and variable declaration that are found within a class
        int itemsInClasses = 0;
        
        foreach (Match aClass in allClasses) {
			Block _class = new Block (aClass, script.text);
			
			if (_class.isEmpty)
				continue;

            // look for constructors in the class
			pattern = _class.name+optWS+"\\(.*\\)"+optWS+"{";
            List<Match> allConstructors = ReverseMatches (_class.text, pattern); // all constructors in that class
            _class.newText = _class.text;

			foreach (Match aConstructor in allConstructors) {
				// here, we are inside one of the constructors of the current class (_class)
				Block constructor = new Block (aConstructor, _class.text);
				// start and end index are relative to _class.text, not relative to script.text !


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
                    constructor.newText = constructor.text.Replace (alternateConstructorCall.Value, "");

                    // add the new syntax to _class.newText
                    _class.newText = _class.newText.Insert (constructor.startIndex-1, " : this("+alternateConstructorCall.Groups["args"].Value+")"); 
                    _class.newText = _class.newText.Replace (constructor.text, constructor.newText);
                }
            } // end looping throught constructors of _class
            
			// we won't do more search/replace for this class 
			// now replace in script.text, the old _class.text by the new
			script.text = script.text.Replace (_class.text, _class.newText);


            //--------------------


            // makes the count of all functions and variables inside the class 
            pattern = "function"+oblWS+commonName+optWS+"\\(";
            itemsInClasses += Regex.Matches (_class.text, pattern).Count;

            pattern = "var"+oblWS+commonName+optWS+"(:|;|=)";
            itemsInClasses += Regex.Matches (_class.text, pattern).Count;
			//matches = Regex.Matches (_class.text, pattern);
		} // end looping through classes in that file


        // we made a list of functions and variables inside classes
        // now make a list of functions and variable inside the script ...
        int itemsInScript = 0;

        pattern = "function"+oblWS+commonName+optWS+"\\(";
        itemsInScript += Regex.Matches (script.text, pattern).Count;

        pattern = "var"+oblWS+commonName+optWS+"(:|;|=)";
        itemsInScript += Regex.Matches (script.text, pattern).Count;


        // ... then compare the two lists
        // if there is a difference, that mean that some variable or function declaration lies outside a class
        // that means that the script is a MonoBehaviour derived class
		if (itemsInClasses != itemsInScript) { 
            script.text = script.text.Insert (0, EOL+"public class "+script.name+" : MonoBehaviour {"+EOL);
            script.text = script.text+EOL+"} // end of class "+script.name; // the closing public class bracket
        }


        // Attributes / Flags
            // move some of them to the beginning of the script.text before converting
            pattern = "@?"+optWS+"(script"+oblWS+")?RequireComponent"+optWS+"\\("+optWS+commonChars+optWS+"\\)("+optWS+";)?";
            MatchCollection matches = Regex.Matches (script.text, pattern);
            foreach (Match match in matches)
				script.text = script.text.Replace (match.Value, "").Insert (0, match.Value+EOL);
        
            pattern = "@?"+optWS+"(script"+oblWS+")?ExecuteInEditMode"+optWS+"(\\("+optWS+"\\))?("+optWS+";)?";
            matches = Regex.Matches (script.text, pattern);
            foreach (Match match in matches)
                script.text = script.text.Replace (match.Value, "").Insert (0, "[ExecuteInEditMode]"+EOL);

            pattern = "@?"+optWS+"(script"+oblWS+")?AddComponentMenu(.*\\))("+optWS+";)?";
            matches = Regex.Matches (script.text, pattern);
            foreach (Match match in matches)
                script.text = script.text.Replace (match.Value, "").Insert (0, match.Value+EOL);


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
        // in JS, the way to define struct is to makes a public class inherits from System.ValueType (or just a regular class)
        patterns.Add ( "class"+oblWS+commonName+oblWS+"extends"+oblWS+"System"+optWS+"\\."+optWS+"ValueType" );
        replacements.Add ( "struct$1$2" );

        // public class inheritance
        patterns.Add ( oblWS+"extends("+oblWS+commonName+optWS+"{)" );
        replacements.Add ( "$1:$2" );

        // super. => base.      
        patterns.Add ( "super"+optWS+"\\." );
        replacements.Add ( "base$1." );

        DoReplacements ();
    } // end Classes()


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Add the "new" keyword before classes instanciation where it is missing
    /// </summary>
    public static void AddNewKeyword () {
        // get pattern like "var name: Type = ClassOrMethod ();"  and search for "Class name = Class ();"
        pattern = "var"+oblWS+commonName+optWS+":"+optWS+commonChars+"("+optWS+"="+optWS+commonChars+optWS+"\\(.*\\)"+optWS+";)";
        List<Match> allMatches = ReverseMatches (script.text, pattern);

        foreach (Match match in allMatches)
            if (match.Groups[5].Value == match.Groups[9].Value) // if the type == the class/method name
                script.text = script.text.Insert (match.Groups[9].Index, "new "); // add "new " in front of Class ()

        
        //also add a new keyword in front of collections
        pattern = "="+optWS+collections+optWS+"\\("; // when setting the value of a variable
        InsertInPatterns (pattern, 1, " new");

        pattern = "return"+oblWS+collections+optWS+"\\("; // when returning an empty instance
        InsertInPatterns (pattern, 6, " new");
        

        // and Generic collections
        pattern = "="+optWS+genericCollections+"<"+commonChars+">"+optWS+"\\(";
        InsertInPatterns (pattern, 1, " new");

        pattern = "return"+oblWS+genericCollections+optWS+"\\(";
        InsertInPatterns (pattern, 6, " new");


        // and classes in ClassesList
        foreach (string className in classesList) {
            pattern = "="+optWS+className+optWS+"\\(";
            InsertInPatterns (pattern, 1, " new");

            // do the same with return keyword
            pattern = "return"+oblWS+className+optWS+"\\(";
            InsertInPatterns (pattern, 6, " new");
        }
    } // end AddNewKeyword ()


    // ----------------------------------------------------------------------------------

    /// <summary> 
    /// Insert [text] at the fixed position [patternOffset] in all [pattern]s found
    /// </summary>
    private static void InsertInPatterns (string pattern, int patternOffset, string text) {
        List<Match> allMatches = ReverseMatches (script.text, pattern);

        foreach (Match match in allMatches)
            script.text = script.text.Insert (match.Index + patternOffset, text);
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Add the keyword public when no visibility (or just static) is set (the default visibility is public in JS but private in C#)
    /// Works also for functions, classes and enums
    /// </summary>
    public static void AddVisibility () {
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


        // all variables gets a public or static public visibility but this shouldn't happend inside functions, so remove that

        pattern = "function"+oblWS+commonName+optWS+"\\(.*\\)"+optWS+"(:"+optWS+commonChars+optWS+")?{";
        List<Match> allFunctions = ReverseMatches (script.text, pattern);

        foreach (Match aFunction in allFunctions) {
            Block function = new Block (aFunction, script.text);

            if (function.isEmpty)
                continue;

            patterns.Add ( "public"+oblWS+"(static"+oblWS+"var)" );
            replacements.Add ( "$2" );
            patterns.Add ( "(static"+oblWS+")?public"+oblWS+"var" );
            replacements.Add ( "$1var" );

            function.newText = DoReplacements (function.text);
            script.text = script.text.Replace (function.text, function.newText);
        } // end for
    } // end AddVisibility ()
}


using UnityEngine;


class UnityScriptToCSharp_Classes {
	
	UnityScriptToCSharp main;

	UnityScriptToCSharp_Classes (UnityScriptToCSharp main) {
		this.main = main;
		
	}

	// ----------------------------------------------------------------------------------

    /// <summary> 
    /// Convert stuffs related to classes : declaration, inheritance, parent constructor call, Assembly imports
    /// </summary>
    static void Classes () {
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
}

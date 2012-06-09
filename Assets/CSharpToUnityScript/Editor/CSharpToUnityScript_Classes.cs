


using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions; 
using System.IO;


public class CSharpToUnityScript_Classes: CSharpToUnityScript {
	                                      
    /// <summary> 
    /// Convert stuffs related to classes : declaration, inheritance, parent constructor call, Assembly imports
    /// </summary>
    public static void Classes () {
        // classes declarations with inheritance
        patterns.Add ( "(class"+oblWS+commonName+optWS+"):"+optWS+"("+commonName+optWS+"{)" );
        replacements.Add ( "$1extends $6" );

        DoReplacements ();


        // now convert parent and alternate constructor call

        // loop the classes declarations in the file
        pattern = "class"+oblWS+commonName+"("+oblWS+"extends"+oblWS+commonName+")?"+optWS+"{";
        List<Match> allClasses = ReverseMatches ( script.text, pattern );
        
        foreach ( Match aClass in allClasses ) {
			Block classBlock = new Block ( aClass, script.text );
			
			if ( classBlock.isEmpty )
				continue;

            List<Match> allConstructors;

            // look for constructors in the class that call the parent constructor
            if ( classBlock.declaration.Contains ( "extends" ) ) { // if the class declaration doesn't contains "extends", a constructor has no parent to call
    			pattern = "public"+optWS+classBlock.name+optWS+"\\(.*\\)(?<base>"+optWS+":"+optWS+"base"+optWS+"\\((?<args>.*)\\))"+optWS+"{";
                allConstructors = ReverseMatches ( classBlock.text, pattern ); // all constructors in that class
                //classBlock.newText = classBlock.text;

    			foreach ( Match aConstructor in allConstructors ) {
    				Block constructorBlock = new Block ( aConstructor, classBlock.text );

                    // first task : add "super();" to the constructor's body
                   constructorBlock.newText = constructorBlock.text+EOL+"super("+aConstructor.Groups["args"]+");";
                   classBlock.newText = classBlock.text.Replace ( constructorBlock.text, constructorBlock.newText );

                   // second tacks : remove ":base()" in the constructor declaration
                   constructorBlock.newDeclaration = constructorBlock.declaration.Replace ( aConstructor.Groups["base"].Value, "" );
                   classBlock.newText = classBlock.text.Replace ( constructorBlock.declaration, constructorBlock.newDeclaration );
                }
            }


            // look for constructors in the class that call others constructors (in the same class)
            pattern = "public"+optWS+classBlock.name+optWS+"\\(.*\\)(?<this>"+optWS+":"+optWS+"this"+optWS+"\\((?<args>.*)\\))"+optWS+"{";
            allConstructors = ReverseMatches ( classBlock.text, pattern ); // all constructors in that class
            classBlock.newText = classBlock.text;

            foreach ( Match aConstructor in allConstructors ) {
                Block constructorBlock = new Block ( aConstructor, classBlock.text );

                // first task : add "classname();" to the constructor's body
               constructorBlock.newText = constructorBlock.text+EOL+classBlock.name+"("+aConstructor.Groups["args"]+");";
               classBlock.newText = classBlock.text.Replace ( constructorBlock.text, constructorBlock.newText );

               // second tacks : remove ":this()" in the constructor declaration
               constructorBlock.newDeclaration = constructorBlock.declaration.Replace ( aConstructor.Groups["this"].Value, "" );
               classBlock.newText = classBlock.text.Replace ( constructorBlock.declaration, constructorBlock.newDeclaration );
            }
            
			// we won't do more search/replace for this class 
			// now replace in script.text, the old classBlock.text by the new
			script.text = script.text.Replace ( classBlock.text, classBlock.newText );
		} // end looping through classes in that file


        //--------------------


        // Attributes
        string path = Application.dataPath+"/CSharpToUnityScript/Editor/Attributes.txt";
        if ( File.Exists ( path) ) {
            StreamReader reader = new StreamReader ( path );
            List<string> attributesList = new List<string> ();

            while ( true ) {
                string line = reader.ReadLine ();
                if ( line == null )
                    break;

                if ( line.StartsWith ( "#" ) || line.Trim () == "" )
                    continue;

                attributesList.Add ( line.Trim () );
            }

            foreach ( string attr in attributesList ) {
                if ( attr == "RPC" ) {
                    patterns.Add ( "\\["+optWS+"RPC"+optWS+"\\]" );
                    replacements.Add ( "@RPC" );
                    continue;
                }

                if ( attr == "RequireComponent" ) {
                    patterns.Add ( "\\["+optWS+"RequireComponent"+optWS+"\\("+optWS+"typeof"+optWS+"\\("+commonName+"\\)"+optWS+"\\)"+optWS+"\\]" );
                    replacements.Add ( "@script RequireComponent($6)" );
                    continue;
                }

                patterns.Add ( "\\["+optWS+attr+optWS+"(\\(.*\\))?"+optWS+"\\]" );
                replacements.Add ( "@script "+attr+"$3" );
            }
        }
        else
            Debug.LogError ("Attributes.txt does not exist, not converting attributes !");

        
        // struct
        // in JS, the way to define struct is to makes a public class inherits from System.ValueType (or just a regular class)
        patterns.Add ( "struct"+oblWS+commonName+optWS+"{" );
        replacements.Add ( "class $2 {" );


        // base. => this.      
        patterns.Add ( "base"+optWS+"\\." );
        replacements.Add ( "super$1." );


        // Assembly imports
        patterns.Add ( "using("+oblWS+commonName+optWS+";)" );
        replacements.Add ( "import$1");


        DoReplacements ();
    } // end Classes()


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Add the keyword public when no visibility (or just static) is set (the default visibility is public in JS but private in C#)
    /// Works also for functions, classes and enums
    /// </summary>
    public static void AddVisibility () {
        // the default visibility for variable and functions is public in JS but private in C# => add the keyword public when no visibility (or just static) is set 
        /* patterns.Add ( "([;{}\\]]+"+optWS+")((var|function|enum|class)"+oblWS+")" );
        replacements.Add ( "$1private $3" );

        patterns.Add ( "(\\*"+optWS+")((var|function|enum|class)"+oblWS+")" ); // add a / after \\*
        replacements.Add ( "$1private $3" );

        patterns.Add ( "(//.*"+optWS+")((var|function|enum|class)"+oblWS+")" );
        replacements.Add ( "$1private $3" );

        patterns.Add ( "((\\#else|\\#endif)"+oblWS+")((var|function|enum|class)"+oblWS+")" );
        replacements.Add ( "$1private $4" );


        // static
        patterns.Add ( "([;{}\\]]+"+optWS+")static"+oblWS+"((var|function)"+oblWS+")" );
        replacements.Add ( "$1private static $4" );

        patterns.Add ( "(\\*"+optWS+")static"+oblWS+"((var|function)"+oblWS+")" );  // add a / after \\*
        replacements.Add ( "$1private static $4" );

        patterns.Add ( "(//.*"+optWS+")static"+oblWS+"((var|function)"+oblWS+")" );
        replacements.Add ( "$1private static $4" );

        patterns.Add ( "((\\#else|\\#endif)"+oblWS+")((var|function)"+oblWS+")" );
        replacements.Add ( "$1private static $4" );

        DoReplacements ();*/


        // all variables gets a public or static public visibility but this shouldn't happend inside functions, so remove that
/*
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
        */
    } // end AddVisibility ()
}

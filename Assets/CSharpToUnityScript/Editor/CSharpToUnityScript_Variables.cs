





public class CSharpToUnityScript_Variables: CSharpToUnityScript {
	                                      
    /// <summary> 
    /// Convert stuffs related to functions : declaration
    /// </summary>
    public static void Variables () {
        
        // var declaration with value   general case
        patterns.Add ( commonChars+oblWS+commonName+optWS+"(=.*;)" );
        replacements.Add ( "var $3: $1$4$5" );

         // var declaration without value   also works in for loop
        patterns.Add ( commonChars+oblWS+commonName+optWS+"(;|in)" );
        replacements.Add ( "var $3: $1$4$5" );

        // pathcing
            // assembly imports got converted
           patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"import"+optWS+";" ); // will mess up if a custom class is named "import"...
           replacements.Add ( "import $2;" );



        // string 
        patterns.Add ( "(var"+oblWS+commonName+optWS+":"+optWS+")string(("+optWS+"\\["+optWS+"\\])?"+optWS+"(=|;))" );
        replacements.Add ( "$1String$6" );

        // bool
        patterns.Add ( "(var"+oblWS+commonName+optWS+":"+optWS+")bool(("+optWS+"\\["+optWS+"\\])?"+optWS+"(=|;))" );
        replacements.Add ( "$1boolean$6" );


        // arrays

            // replace curly brackets by square bracket
            patterns.Add ( "(="+optWS+commonName+optWS+"){(.*)}("+optWS+";)" );
            replacements.Add ( "$1[$2]$3" );


        // char 'a' => 'a'[0]
        patterns.Add ( "'[a-zA-z0-9]{1}'" );
        replacements.Add ( "$0[0]" );

        // remove f to Float values
        // actually not needed, in works with the f


        DoReplacements ();
    } // end Functions()


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Convert Properties declarations
    /// </summary>
    public static void Properties () {
        /*// change string return type to string, works also for functions (properties in JS are functions)
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
        List<Match> allGetters = ReverseMatches (script.text, pattern);
        
        string unModifiedScript = script.text;

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
            Match theSetter = Regex.Match (unModifiedScript, pattern);

            if (theSetter.Success)
                property += "\t\t"+theSetter.Groups[1].Value+" set { "+variableName+" = value; }"+EOL; // setter

            property +="\t}"+EOL; // property closing bracket


            // now do the modifs in the script
            script.text = script.text.Replace (aGetter.Value, property); // replace getter by property

            if (theSetter.Success)
                script.text = script.text.Replace (theSetter.Value, ""); // remove setter if it existed
        }*/
    } // end Properties ()
} // end class CSharpToUnityScript_Functions

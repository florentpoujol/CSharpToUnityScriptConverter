





public class CSharpToUnityScript_Variables: CSharpToUnityScript {
	                                      
    /// <summary> 
    /// Convert stuffs related to functions : declaration
    /// </summary>
    public static void Variables () {
        
        // "= aVar as AType" => "= aVar";
        // "as aType" might appear in other location
        patterns.Add ( commonName+oblWS+"as"+oblWS+commonChars+optWS+";" );
        replacements.Add ( "$1;" );

        // var declaration with value
        patterns.Add ( commonChars+oblWS+commonName+optWS+"(=.*;)" );
        replacements.Add ( "var $3: $1$4$5" );

        // var declaration without value   
        patterns.Add ( commonChars+oblWS+commonName+optWS+";" ); 
        replacements.Add ( "var $3: $1$4;" );

        // var declaration in foreach loop 
        patterns.Add ( commonChars+oblWS+commonName+"("+oblWS+"in"+oblWS+")" );
        replacements.Add ( "var $3: $1$4" );


        // pathcing   converting var declaration leads to some garbage
           // assembly imports
           patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"import"+optWS+";" ); // will mess up if a custom class is named "import"...
           replacements.Add ( "import $2;" );

           // returned values
           patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"return"+optWS+";" ); // will mess up if a custom class is named "import"...
           replacements.Add ( "return $2;" );

           // "else aVar = aValue;" got converted in  "var aVar: else = aValue;"
           patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"else"+optWS+"=" ); // will mess up if a custom class is named "import"...
           replacements.Add ( "else $1 =" );


        // public Type var, var, var;


        // casting
        // we can make it a little more secure by checking that the to commonChars are the same
        patterns.Add ( "(var"+oblWS+commonName+optWS+":"+optWS+commonChars+optWS+"="+optWS+")\\("+optWS+commonChars+optWS+"\\)("+optWS+commonName+optWS+";)" );
        replacements.Add ( "$1$12" );

        patterns.Add ( "((\\(|=)"+optWS+")\\("+optWS+commonChars+optWS+"\\)("+optWS+commonName+")" ); // match if and loop without brackets !!
        replacements.Add ( "$1$7" );



        // string  works var declaraion with or without value (the same is run after that the function argument are converted)
        patterns.Add ( "("+commonName+optWS+":"+optWS+")string(("+optWS+"\\["+optWS+"\\])?"+optWS+"(=|;))" );
        replacements.Add ( "$1String$5" );

        // bool
        patterns.Add ( "("+commonName+optWS+":"+optWS+")bool(("+optWS+"\\["+optWS+"\\])?"+optWS+"(=|;))" );
        replacements.Add ( "$1boolean$5" );


        // arrays

            // replace curly brackets by square bracket
            patterns.Add ( "(="+optWS+commonName+optWS+"){(.*)}("+optWS+";)" );
            replacements.Add ( "$1[$2]$3" );


        // char 'a' => 'a'[0]
        patterns.Add ( "'[a-zA-z0-9]{1}'" );
        replacements.Add ( "$0[0]" );


        // remove f to Float values
        // actually not needed, in works with the f
        patterns.Add ( "([0-9]{1}(\\.[0-9]{1})?)(f|F)" );
        replacements.Add ( "$1" );





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

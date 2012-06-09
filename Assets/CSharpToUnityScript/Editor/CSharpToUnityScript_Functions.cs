


using System.Collections.Generic;
using System.Text.RegularExpressions;


public class CSharpToUnityScript_Functions: CSharpToUnityScript {
	                                      
    /// <summary> 
    /// Convert stuffs related to functions : declaration
    /// </summary>
    public static void Functions () {
        
        // function declaration ...
        // using a simple regex cause too much garbage
        //patterns.Add ( commonChars+oblWS+commonName+optWS+"(\\(.*\\))"+optWS+"{" ); // here I don't care if the method is public, private, static, abstract or whatever since a signature is always composed of a type followed by the name of the method
        //replacements.Add ( "function $3$5: $1 {" );

        pattern = "(?<returnType>"+commonChars+")"+oblWS+"(?<functionName>"+commonName+")"+optWS+"(\\(.*\\))("+optWS+"{)";
        replacement = "function $5$7: $2$8";
        List<Match> allFunctionsDeclarations = ReverseMatches (script.text, pattern);

        foreach (Match aFunctionDeclaration in allFunctionsDeclarations) {
            if ( aFunctionDeclaration.Groups["returnType"].Value != "else" && aFunctionDeclaration.Groups["functionName"].Value != "if") // do not match else if () statement
                continue;

            patterns.Add (aFunctionDeclaration.Value);

            switch ( aFunctionDeclaration.Groups["returnType"].Value ) {
                case "void" : replacements.Add ( "function $5$7$8" ); continue;
                case "string" : replacements.Add ( "function $5$7: String$8" ); continue;
                case "string[]" : replacements.Add ( "function $5$7: String$8" ); continue; 
                case "bool" : replacements.Add ( "function $5$7: boolean$8" ); continue; 
                case "bool[]" : replacements.Add ( "function $5$7: boolean[]$8" ); continue; 
            }
            
            
            //script.text = Regex.Replace (script.text, pattern, replacement);


            replacements.Add (replacement);
        }




        /*patterns.Add ( ": void"+optWS+"{" );
        replacements.Add ( "" );

        patterns.Add ( ": string(("+optWS+"\\["+optWS+"\\])?"+optWS+"{)" );
        replacements.Add ( ": String$1" );

        patterns.Add ( ": bool(("+optWS+"\\["+optWS+"\\])?"+optWS+"{)" );
        replacements.Add ( ": boolean$1" );*/


        // arguments declaration
        patterns.Add ( "(\\(|,){1}"+optWS+commonCharsWithoutComma+oblWS+commonName+optWS+"(\\)|,){1}" );
        replacements.Add ( "$1$2$5: $3$6$7" );
        // as regex doesn't overlap themselves, only half of the argument have been converted
        // I need to run the regex  a second time
        patterns.Add ( "(\\(|,){1}"+optWS+commonCharsWithoutComma+oblWS+commonName+optWS+"(\\)|,){1}" );
        replacements.Add ( "$1$2$5: $3$6$7" );

        // remove out keyword in arguments
        patterns.Add ( ","+optWS+"out("+oblWS+commonName+")" );
        replacements.Add ( ",$2" );

        // remove ref keyword in arguments
        patterns.Add ( ","+optWS+"ref("+oblWS+commonCharsWithoutComma+")" );
        replacements.Add ( ",$2" );


        DoReplacements ();
    } // end Functions()


} // end class CSharpToUnityScript_Functions



using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;


public class CSharpToUnityScript_Functions: CSharpToUnityScript {
	                                      
    /// <summary> 
    /// Convert stuffs related to functions : declaration
    /// </summary>
    public static void Functions () {
        
        // function declaration ...
        // using a simple pattern/replacement regex as below match way too much things it shouldn't
        // So I need to check each match before allowing the replacement

        // patterns.Add ( commonChars+oblWS+commonName+optWS+"(\\(.*\\))"+optWS+"{" ); // here I don't care if the method is public, private, static, abstract or whatever since a signature is always composed of a type followed by the name of the method
        // replacements.Add ( "function $3$5: $1 {" );

        pattern = "(?<returnType>"+commonChars+")"+oblWS+"(?<functionName>"+commonName+")"+optWS+"(\\("+argumentsChars+"\\))("+optWS+"{)"; // match two words followed by a set of parenthesis followed by an opening curly bracket
        List<Match> allFunctionsDeclarations = ReverseMatches (script.text, pattern);

        Debug.Log ("functions="+allFunctionsDeclarations.Count);

        foreach (Match aFunctionDeclaration in allFunctionsDeclarations) {
            string returnType = aFunctionDeclaration.Groups["returnType"].Value;
            string functionName = aFunctionDeclaration.Groups["functionName"].Value;
            
            Debug.Log ("returnType="+returnType+" | functionName="+functionName);

            if ( returnType == "else" && functionName == "if") // do not match else if () statement
                continue;

            if ( returnType == "new" ) // do not match class instanciatton inside an if statement   ie : new Rect ()) {}  => it shouldn't anymore anyway, thanks to argumentsChars instead of ".*"
                continue;

            // if we are there, it's really a function declaration that has to be converted
            patterns.Add ( "("+returnType+")"+oblWS+"("+functionName+")"+optWS+"(\\("+argumentsChars+"\\))("+optWS+"{)" ); 
            // I can't use aFunctionDeclaration.Value as the pattern because square brackets that may be found in the argments (if some args ar arrays) wouldn't be escaped and would cause an exception

            switch ( returnType ) {
                case "void" : replacements.Add ( "function $3$5$7" ); continue;
                case "string" : replacements.Add ( "function $3$5: String$7" ); continue;
                case "string[]" : replacements.Add ( "function $3$5: String[]$7" ); continue; 
                case "bool" : replacements.Add ( "function $3$5: boolean$7" ); continue; 
                case "bool[]" : replacements.Add ( "function $3$5: boolean[]$7" ); continue; 
                case "public" /* it's a constructor */ : replacements.Add ( "$1 function $3$5$7" ); continue;
            }
            
            
            

            // if we are there, it's that the functiondeclaration has nothing special
            replacements.Add ( "function $3$5: $1$7" );
        }




        /*patterns.Add ( ": void"+optWS+"{" );
        replacements.Add ( "" );

        patterns.Add ( ": string(("+optWS+"\\["+optWS+"\\])?"+optWS+"{)" );
        replacements.Add ( ": String$1" );

        patterns.Add ( ": bool(("+optWS+"\\["+optWS+"\\])?"+optWS+"{)" );
        replacements.Add ( ": boolean$1" );*/


        // remove out keyword in arguments
        patterns.Add ( ","+optWS+"out("+oblWS+commonName+")" );
        replacements.Add ( ",$2" );

        // remove ref keyword in arguments
        patterns.Add ( ","+optWS+"ref("+oblWS+commonCharsWithoutComma+")" );
        replacements.Add ( ",$2" );


        // arguments declaration      if out and ref keyword where not removed before this point it would also convert them ("out hit" in Physics.Raycast() calls) or prevent the convertion ("ref aType aVar" as function argument)
        patterns.Add ( "(\\(|,){1}"+optWS+commonCharsWithoutComma+oblWS+commonName+optWS+"(\\)|,){1}" );
        replacements.Add ( "$1$2$5: $3$6$7" );
        // as regex doesn't overlap themselves, only half of the argument have been converted
        // I need to run the regex  a second time
        patterns.Add ( "(\\(|,){1}"+optWS+commonCharsWithoutComma+oblWS+commonName+optWS+"(\\)|,){1}" );
        replacements.Add ( "$1$2$5: $3$6$7" );


        // string
        patterns.Add ( "("+commonName+optWS+":"+optWS+")string(("+optWS+"\\["+optWS+"\\])?"+optWS+"(,|\\)))" );
        replacements.Add ( "$1String$5" );

        // bool
        patterns.Add ( "("+commonName+optWS+":"+optWS+")bool(("+optWS+"\\["+optWS+"\\])?"+optWS+"(,|\\)))" );
        replacements.Add ( "$1boolean$5" );


        


        DoReplacements ();
    } // end Functions()


} // end class CSharpToUnityScript_Functions

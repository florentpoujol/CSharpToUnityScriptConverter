
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class UnityScriptToCSharp_Functions: UnityScriptToCSharp {



    // ----------------------------------------------------------------------------------

    /// <summary> 
    /// Convert function declarations
    /// Try to resolve the return type (if any) when there is none declared with the look of what is returned
    /// </summary>
    void Functions () {
        // UnityScript allow arugments without types, when they are not needed
        // just remove them
        //patterns.Add ( "(function"+oblWS+commonName+optWS+")(\\(|,){1}"+optWS+"[a-zA-z\\.]+"+optWS+"(\\)|,){1}" );
        patterns.Add ( "(function"+oblWS+commonName+optWS+")\\("+optWS+"[a-zA-z_\\.]+"+optWS+"\\)" );
        replacements.Add ( "$1()" );


        // arguments declaration
        patterns.Add ( "(\\(|,){1}"+optWS+commonName+optWS+":"+optWS+commonCharsWithoutComma+optWS+"(\\)|,){1}" );
        replacements.Add ( "$1$2$6 $3$7$8" );
        // as regex doesn't overlap themselves, only half of the argument have been convertes
        // I need to run the regex 
        patterns.Add ( "(\\(|,){1}"+optWS+commonName+optWS+":"+optWS+commonCharsWithoutComma+optWS+"(\\)|,){1}" );
        replacements.Add ( "$1$2$6 $3$7$8" );



        // search for return keyword to see if the void actually returns something, then try to resolve the return type with the returned variable type or returned value
        // if the void does not return anything, a "void" return type is added
        // if the type can't be resolved, a "MISSING_RETURN_TYPE" return type is added
        // coroutines gets a IEnumerator return type and thair call is wrapped by StartCoroutine( )

        // look for all JS functions that has no explicit return type
        pattern = " function"+oblWS+commonName+optWS+"\\(.*\\)"+optWS+"{"; 
        List<Match> allFunctions = ReverseMatches (file, pattern);

		foreach (Match aFunction in allFunctions) {
            //Debug.Log ("aFunction="+aFunction.Value+" ["+file[aFunction.Index-1]+file[aFunction.Index]+file[aFunction.Index+1]+"]");

			Block function = new Block (aFunction, file);
			function.name = aFunction.Groups[2].Value;


            // look for return keyword patterns
            pattern = "return"+oblWS+".+;";

            // if there is none, add ""
			if ( ! Regex.Match (function.text, pattern).Success) { // no return keyword : add "void" return type
                file = file.Replace (function.declaration, function.declaration.Replace (" function ", " void ")); 
                continue;
            }

        
            // Below this point, we know that the function returns some value but we don't know the type yet
            // Look in function.text for several return patterns 
        
            // yield / coroutine
            pattern = "yield"+oblWS+"return";
            
			if (Regex.Match (function.text, pattern).Success) { 
                file = file.Replace (function.declaration, function.declaration.Replace (" function ", " IEnumerator "));

                // In C# (and Boo), a coroutine call must be wrapped by the StartCoroutine() method : StartCoroutine( CoroutineName() );
                // The current function is a coroutine, so search for it's call in the file
                patterns.Add ( "("+function.name+optWS+"\\(.*\\))"+optWS+";" );
				replacements.Add ( "StartCoroutine( $1 );" );

                continue;
            }


            // this pattern will match an int, a float, a bool or a variable name
            pattern = "return"+oblWS+commonName+optWS+";";
			Match variableMatch = Regex.Match (function.text, pattern);
            string variableName = "";

			if (variableMatch.Success) {
				variableName = variableMatch.Groups[2].Value;
				
                // bool
                if (Regex.Match (variableName, "^(true|false)$").Success) { 
                    file = file.Replace (function.declaration, function.declaration.Replace (" function ", " bool "));
                    continue;
                }

                // float
                if (Regex.Match (variableName, "^-?[0-9]+\\.{1}[0-9]+(f|F){1}$").Success) { 
                    file = file.Replace (function.declaration, function.declaration.Replace (" function ", " float "));
                    continue;
                }

				// double
				if (Regex.Match (variableName, "^-?[0-9]+\\.{1}[0-9]+(f|F){0}$").Success) {
					file = file.Replace (function.declaration, function.declaration.Replace (" function ", " double "));
					continue;
				}

                // int
                if (Regex.Match (variableName, "^-?[0-9]+$").Success) { 
                    file = file.Replace (function.declaration, function.declaration.Replace (" function ", " int "));
                    continue;
                }

                // variableName seems to be a variable name after all
                // search for the variable declaration in the function
                // variable declarations are already C#-style
                pattern = commonChars+oblWS+variableName+optWS+"="; // it will also match non converted var declarations : "public var _theVariable ="
				variableMatch = Regex.Match (function.text, pattern);

				if (variableMatch.Success && variableMatch.Groups[1].Value != "var")
                    file = file.Replace (function.declaration, function.declaration.Replace (" function ", " "+variableMatch.Groups[1].Value+" "));

                else { // declaration not found in the function, maybe it's somewhere in the file, or in itemsAndTypes
					variableMatch = Regex.Match (file, pattern);

					if (variableMatch.Success && variableMatch.Groups[1].Value != "var")
						file = file.Replace (function.declaration, function.declaration.Replace (" function ", " "+variableMatch.Groups[1].Value+" "));
                    else { // no it's nowhere in the file either
                        bool isFinded = false;

                        // now check if it's in itemsAndTypes
                        foreach (KeyValuePair<string, string> item in itemsAndTypes) {
                            if (variableName == item.Key) {
                                file = file.Replace (function.declaration, function.declaration.Replace (" function ", " "+item.Value+" "));
                                isFinded = true;
                                break;
                            }
                        }

                        if ( ! isFinded) // no, it's really anywhere ...
                            file = file.Replace (function.declaration, function.declaration.Replace (" function ", " MISSING_VAR_TYPE "));
                    } // end if found in the file
                } // end if found in the function

                continue;
            }


            // this pattern will match string and char
            pattern = "return"+oblWS+commonChars+optWS+";";
            Match stringCharMatch = Regex.Match (function.text, pattern);

			if (stringCharMatch.Success) {
				variableName = stringCharMatch.Groups[2].Value;
            
                // char
                pattern = "(\"|'){1}"+commonChars+"(\"|'){1}"+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]";

                if (Regex.Match (variableName, pattern).Success)
                    file = file.Replace (function.declaration, function.declaration.Replace (" function ", " char "));


                // string
                pattern = "(\"|'){1}"+commonChars+"(\"|'){1}";

                if (Regex.Match (variableName, pattern).Success)
                    file = file.Replace (function.declaration, function.declaration.Replace (" function ", " string "));

                continue;
            }


            // class instanciation : return new Class ();
            pattern = "return"+oblWS+"new"+oblWS+commonName;
            Match returnNewInstanceMatch = Regex.Match (function.text, pattern);

			if (returnNewInstanceMatch.Success) {
                file = file.Replace (function.declaration, function.declaration.Replace (" function ", " "+returnNewInstanceMatch.Groups[3].Value+" "));
                continue;
            }


            // look for "empty" return keyword, allowed in void functions
            pattern = "return"+optWS+";";

            if (Regex.Match (function.text, pattern).Success) { 
                file = file.Replace (function.declaration, function.declaration.Replace (" function ", " void "));
                continue;
            }


            // last possible pattern : function call : return Something();
            // do that in FunctionTheReturn()

            // last thing to do is testing if the function name begins by "is" like "IsSomething ()" because usally that function will return a bool
            pattern = "^(i|I)s";

            if (Regex.Match (function.name, pattern).Success) { 
                file = file.Replace (function.declaration, function.declaration.Replace (" function ", " bool "));
                continue;
            }


            // can't resolve anything ...
            file = file.Replace (function.declaration, function.declaration.Replace (" function ", " MISSING_RETURN_TYPE "));

        } // end looping function declarations
    

        // now convert the declaration that have a return type
        patterns.Add ( "function"+oblWS+"("+commonName+optWS+"\\((.*)\\))"+optWS+":"+optWS+commonChars );
        replacements.Add ( "$8 $2" );


        // classes constructors gets a void return type that has to be removed
        pattern = "class"+oblWS+commonName;
        MatchCollection allClasses = Regex.Matches (file, pattern);

        foreach (Match aClass in allClasses) {
            // look for constructors
            patterns.Add ( "void"+oblWS+"("+aClass.Groups[2].Value+optWS+"\\()" );
            replacements.Add ( "$2" );
        }


        





        DoReplacements ();
    } // end Functions ()


    
    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Try to resolve the return type of function where it wasn't possible before mostly because the type of 
    /// the returned variable couldn't be resolved before the first pass of function converting
    /// </summary>
    void FunctionsTheReturn () {
        // look only for function where the return type of the returned variable is still unresolved
        pattern = " MISSING_VAR_TYPE("+oblWS+commonName+optWS+"\\(.*\\)"+optWS+"{)"; 
        List<Match> allFunctions = ReverseMatches (file, pattern);

        foreach (Match aFunction in allFunctions) {
            Block function = new Block (aFunction, file);
            // here, function.newText will actually be the new declaration, and not the new function content
            
            // search only for variable return pattern     all functions with a "MISSING_VAR_TYPE" should return a variable anyway
            pattern = "return"+oblWS+commonName+optWS+";"; 
            Match theVariable = Regex.Match (function.text, pattern);

            if (theVariable.Success) { // if the current function returns a variable
                string variableName = theVariable.Groups[2].Value;

                // search for the variable declaration in the function
                pattern = commonChars+oblWS+variableName+optWS+"="; // it will also match non converted var declarations : "public var varName ="
                Match variableMatch = Regex.Match (function.text, pattern);

                if (variableMatch.Success && variableMatch.Groups[1].Value != "var")
                    file = file.Replace (function.declaration, function.declaration.Replace (" MISSING_VAR_TYPE ", " "+variableMatch.Groups[1].Value+" "));

                else { // declaration not found in the function, maybe it's somewhere in the file
                    variableMatch = Regex.Match (file, pattern);

                    if (variableMatch.Success && variableMatch.Groups[1].Value != "var")
                        file = file.Replace (function.declaration, function.declaration.Replace (" MISSING_VAR_TYPE ", " "+variableMatch.Groups[1].Value+" "));
                    else // no, it's really anywhere ...
                        continue; // do nothing, leave the MISSING_VAR_TYPE
                }

            }
        } // end looping functions


        // at last, look for function whose return type is still unresolved and that returns a value which comes directly from another function
        // ie : return Mathf.Abs(-5);
        // the "returned" function will be searched in file and in 
        pattern = " MISSING_RETURN_TYPE("+oblWS+commonName+optWS+"\\(.*\\)"+optWS+"{)"; 
        allFunctions = ReverseMatches (file, pattern);

        foreach (Match aFunction in allFunctions) {
            Block function = new Block (aFunction, file);
            
            // search only for function return pattern     bt now, most of the function with a "MISSING_RETURN_TYPE" should return a function
            pattern = "return"+oblWS+commonName+optWS+"\\(.*\\)"+optWS+";"; 
            Match theReturnedFunction = Regex.Match (function.text, pattern);
 
            if (theReturnedFunction.Success) { // if the current function returns a variable
                string returnedFunctionName = theReturnedFunction.Groups[2].Value;
                Debug.Log ("returned function="+returnedFunctionName);
                
                // search for the function declaration in file
                pattern = commonChars+oblWS+returnedFunctionName+optWS+"\\(.*\\)"+optWS+"{";
                Match returnedFunctionMatch = Regex.Match (file, pattern);

                if (returnedFunctionMatch.Success)
                    file = file.Replace (function.declaration, function.declaration.Replace (" MISSING_RETURN_TYPE ", " "+returnedFunctionMatch.Groups[1].Value+" "));

                else { // declaration not found in the function, maybe it's in itemsAndTypes
                    foreach (KeyValuePair<string, string> item in itemsAndTypes) {
                        if (returnedFunctionName == item.Key) {
                            file = file.Replace (function.declaration, function.declaration.Replace (" MISSING_RETURN_TYPE ", " "+item.Value+" "));
                            break;
                        }
                    }
                }
            } // end if function return pattern
        } // end looping functions
    } // end FunctionTheReturn()
}

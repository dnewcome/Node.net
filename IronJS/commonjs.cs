using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Node.Net
{
	class commonjs
	{
		public static Stack<string> RequireStack = new Stack<string>();
		
		/**
		 * Find a a given js file according to CommonJS search 
		 * rules and the path stack.
		 * 
		 * in_filename - RAW filename, no extension, no path
		 * returns: fully-qualified filename with path and ext
		 */
		public static string FindFile( string in_filename ) {
			// first we look for the file at the current location
			string fullname = 
				RequireStack.Peek() + 
				Path.DirectorySeparatorChar + 
				in_filename + ".js";
			
			if( File.Exists( fullname ) ) {
				return fullname;
			}

			// look under node_modules for index.js
			fullname = RequireStack.Peek() +
				Path.DirectorySeparatorChar +
				"node_modules" +
				Path.DirectorySeparatorChar +
				in_filename +
				Path.DirectorySeparatorChar +
				"index.js";
			
			if( File.Exists( fullname ) ) {
				return fullname;
			}

			throw new FileNotFoundException( "file specified in require() was not found" );
		}

		/**
		 * Currently called at startup to set up the path where
		 * we find js files to require()
		 * 
		 * in_pathexp - could be relative or abs path with or
		 * without a filename. Filename is not examined, however.
		 */
		public static void SetRequireStack( string in_pathexp ) {
			string pathpart = Path.GetDirectoryName( in_pathexp );

			// if abs path is given, use as starting path stack value
			if( Path.IsPathRooted( pathpart ) ) {
				RequireStack.Push( pathpart );
			}
			else {
				// start stack at curr dir if stack is empty
				if( RequireStack.Count == 0 ) {
					RequireStack.Push( Directory.GetCurrentDirectory() );
				}

				RequireStack.Push(
					Path.GetFullPath(
						RequireStack.Peek() + Path.DirectorySeparatorChar + pathpart
					)
				);
			}

		}
	} // class
} // namespace

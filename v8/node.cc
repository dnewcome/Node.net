#include <v8.h>

// atlstr lets us use CString
#include <atlstr.h>

// Extracts a C string from a V8 Utf8Value.
const char* ToCString(const v8::String::Utf8Value& value) {
  return *value ? *value : "<string conversion failed>";
}



using namespace v8;

// .net System namespace - we end up with conflicting String typenames
// using namespace System;

// x is a value that we want to expose to js
int x;

// XGetter/setter are functions wired up as accessors called when accessing
// js global variable `x' - done for testing
Handle<Value> XGetter(Local<String> property, 
					const AccessorInfo& info) {
	return Integer::New(x);
}
void XSetter(Local<String> property, Local<Value> value,
		   const AccessorInfo& info) {
	x = value->Int32Value();
}
  
  /**
  * FunctionCallback is wired up to the javascript `puts()' function.
  * we want to use this for doing sys.puts() node API call
  */
  v8::Handle<v8::Value> FunctionCallback(const v8::Arguments& args) {
	
	// not sure why we would need a handle scope.. the shell.cc example
	// does it this way, but it seems to work without it.. probably has 
	// to do with automatic GC when exiting the function here..
	// without it we migh be leaking handles
	HandleScope handle_scope;
	
	// convert first arg to cstring
	v8::String::Utf8Value str(args[0]);
    const char* cstr = ToCString(str);
	
	// use c to print to console
	printf( "%s\n", cstr );
	
	/* Use Console.WriteLine() to print
	System::String^ text = gcnew System::String( cstr, 0, lstrlen( cstr ) );
	System::Console::WriteLine( text );
	*/
	
	return v8::Undefined();
  }
  
int main(int argc, char* argv[]) {

  // Create a stack-allocated handle scope.
  HandleScope handle_scope;

  // set up the javascript context with the 'x' global object
  Handle<ObjectTemplate> global_templ = ObjectTemplate::New();
  global_templ->SetAccessor(String::New("x"), XGetter, XSetter);
  
  Handle<FunctionTemplate> putstemplate = FunctionTemplate::New( FunctionCallback );
  global_templ->Set(String::New("puts"), putstemplate);
  
  Persistent<Context> context = Context::New(NULL, global_templ);

 
  // Enter the created context for compiling and
  // running the hello world script. 
  Context::Scope context_scope(context);

	// read js file in from filesystem....
	char* fileArg = argv[1];
	System::String^ fileName = gcnew System::String( fileArg, 0, lstrlen(fileArg) );
	System::String^ fileContents;
	System::IO::StreamReader^ streamReader = gcnew System::IO::StreamReader( fileName );
	fileContents = streamReader->ReadToEnd();
	streamReader->Close();

  // Create a string containing the JavaScript source code.
	CString str3( fileContents); 
	Handle<String> source = String::New( str3 );

  // Compile the source code.
  Handle<Script> script = Script::Compile(source);
  
  // Run the script to get the result.
  Handle<Value> result = script->Run();
  
  // Dispose the persistent context.
  context.Dispose();

  // Convert the result to an ASCII string and print it.
  String::AsciiValue ascii(result);
  printf("%s\n", *ascii);
  System::Console::WriteLine( "CLR Working" );
  return 0;
}



#include <v8.h>

using namespace v8;
  
/** 
* Test to compile a js function, get a reference to it by name, and invoke it
*	from c++. The critical bits were found in the v8 sample project process.cc
*/
int main( int argc, char* argv[] ) 
{
	HandleScope handle_scope;

	Handle<ObjectTemplate> global_templ = ObjectTemplate::New();
	Persistent<Context> context = Context::New( NULL, global_templ );
	Context::Scope context_scope(context);

	// compile and run script containing the function we want a ref to
	Handle<Script> script = Script::Compile( String::New( "function fn(){ return 2; }") );
	Handle<Value> scriptresult = script->Run();
	
	// get ref to compiled fn by name and cast to Function
	Handle<String> function_name = String::New("fn");
	Handle<Value> process = context->Global()->Get( function_name );
	Handle<Function> process_fun = Handle<Function>::Cast(process);

	// invoke function using global object as `this' and no args
	Handle<Value> result = process_fun->Call( context->Global(), NULL, NULL );

	String::AsciiValue ascii(result);
	printf("%s\n", *ascii);
	
	context.Dispose();
	return 0;
}



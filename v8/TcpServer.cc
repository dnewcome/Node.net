/**
* Node.net is a .NET implementation of Node.js and is free 
*	software provided under the MIT license.
*
*	See LICENSE file for full text of the license.
*	Copyright 2010 Dan Newcome.
*/

#include <v8.h>
#include <vector>
#using <System.dll>

// atlstr lets us use CString
#include <atlstr.h>

using namespace v8;
using namespace System::Net::Sockets;
using namespace System::Net;
using namespace System::Threading;
using namespace System::IO;
using namespace System::Runtime::InteropServices;

const char* ToCString( const v8::String::Utf8Value& value ) {
  return *value ? *value : "<string conversion failed>";
}

// prototypes
v8::Handle<v8::Value> NetStreamAddListenerCallback( const v8::Arguments& args );
System::String^ StringV8ToDotnet( Handle<v8::Value> in_string );

ref class NetStream {
private:
	Stream^ stream;
	static const int bufferSize = 1024;
	std::vector<Handle<Function>>* dataCallbacks;
	std::vector<Handle<Function>>* endCallbacks;
	
	Handle<v8::Object>* callbackReceiver;
	
public:
	NetStream( Stream^ in_stream, Handle<v8::Object>* in_receiver ) {
		dataCallbacks = new std::vector<Handle<Function>>();
		endCallbacks = new std::vector<Handle<Function>>();
		stream = in_stream;
		
		// TODO: we are temporarily passing receiver object here
		callbackReceiver = in_receiver;

	}
	
	void read() {
		array<unsigned char>^ buffer = gcnew array<unsigned char>( bufferSize );
		System::AsyncCallback^ ReadCallbackDelegate = 
			gcnew System::AsyncCallback( this, &NetStream::ReadCallback );
		this->stream->BeginRead( buffer, 0, bufferSize, ReadCallbackDelegate, buffer );
	}
	
	void ReadCallback( System::IAsyncResult^ result ) {
		array<unsigned char>^ buffer = ( array<unsigned char>^ )result->AsyncState;
		int bytesRead = this->stream->EndRead( result );
		printf( "ReadCallback(): read %i bytes from stream\n", bytesRead );
		System::String^ chunk = 
			System::Text::Encoding::UTF8->GetString( buffer, 0, bytesRead );
		
		if( bytesRead > 0 ) {
			// TODO: we haven't implemented any queue or locking yet
			// queueWorkItem( { callback: raiseDataEvent, args: [ chunk ] } );
			raiseDataEvent( chunk );
			
			array<unsigned char>^ nextBuffer = 
				gcnew array<unsigned char>( bufferSize );
			System::AsyncCallback^ ReadCallbackDelegate = 
				gcnew System::AsyncCallback( this, &NetStream::ReadCallback );
			this->stream->BeginRead( 
				nextBuffer, 0, bufferSize, ReadCallbackDelegate, nextBuffer 
			);
		}
		else {
			// queueWorkItem( { callback: raiseEndEvent, args: [] } );
		}
		
	}
	
	
	void write( System::String^ chunk /*, TODO: encoding */ ) {
		array<unsigned char>^ buffer = 
			System::Text::Encoding::UTF8->GetBytes( chunk );
		this->stream->Write( buffer, 0, buffer->Length );
	}
	
	void addListener( System::String^ eventname, Handle<Function> callback ) {
		if( eventname == "data" ) {
			dataCallbacks->push_back( callback );
		}
		else if( eventname == "end" ) {
			endCallbacks->push_back( callback );
		}
	}
	
	void raiseDataEvent( System::String^ chunk ) {
		printf( "net.stream.raiseDataEvent()\n" );

		CString str( chunk ); 
		Handle<String> chunkString = String::New( str );
		Handle<Value> args[] = { chunkString };
		
		for( int i=0; i < dataCallbacks->size(); i++ ) {
			Handle<Function> fn = dataCallbacks->at(i);
			fn->Call( *callbackReceiver, 1, args );
		}
	}
	
	void raiseEndEvent() {
		printf( "net.stream.raiseEndEvent()\n" );
		for( int i=0; i < endCallbacks->size(); i++ ) {
			Handle<Function> fn = endCallbacks->at(i);
			// fn->Call();
		}
	}
};

ref class NetServer {
private:
	TcpListener^ tcpListener;
	std::vector<Handle<Function>>* listeningCallbacks;
	std::vector<Handle<Function>>* connectionCallbacks;
	std::vector<Handle<Function>>* closeCallbacks;
	
	Handle<v8::Object>* callbackReceiver;
	
public:
	// constructor takes a v8 handle for callback
	NetServer( Handle<Function> in_callback, Handle<v8::Object>* in_receiver ) {
		listeningCallbacks = new std::vector<Handle<Function>>();
		connectionCallbacks = new std::vector<Handle<Function>>();
		closeCallbacks = new std::vector<Handle<Function>>();
		
		// TODO: we are temporarily passing receiver object here
		callbackReceiver = in_receiver;
		
		AddListener( "connection", in_callback );
	}
	
	NetServer() {
		listeningCallbacks = new std::vector<Handle<Function>>();
		connectionCallbacks = new std::vector<Handle<Function>>();
		closeCallbacks = new std::vector<Handle<Function>>();
	}
	
	void Listen( System::Int32 port, System::String^ host ) {
		
		IPHostEntry^ hostInfo = Dns::Resolve( host );
		IPAddress^ ipAddress = hostInfo->AddressList[0];
		
		this->tcpListener = gcnew TcpListener( ipAddress, port );
		// print( 'net.Server.listen(): starting tcp server' );
		this->tcpListener->Start();
		
		// queueWorkItem( { callback: raiseListeningEvent, args: [] } );
		RaiseListeningEvent();
		
		System::AsyncCallback^ ListenerCallbackDelegate = 
			gcnew System::AsyncCallback( this, &NetServer::ListenerCallback );
		this->tcpListener->BeginAcceptTcpClient( ListenerCallbackDelegate, nullptr );
	}
	
	void ListenerCallback( System::IAsyncResult^ result ) {
		printf("%s\n", "listener called" );
		System::AsyncCallback^ ListenerCallbackDelegate = 
			gcnew System::AsyncCallback( this, &NetServer::ListenerCallback );
		this->tcpListener->BeginAcceptTcpClient( ListenerCallbackDelegate, nullptr );
		
		TcpClient^ client = this->tcpListener->EndAcceptTcpClient( result );
		NetStream^ stream = gcnew NetStream( client->GetStream(), callbackReceiver );
		
		//queueWorkItem( { callback: raiseConnectionEvent, args: [ stream ] } );
		
		// TODO: we need wrapper object around NetStream to pass here
		Handle<ObjectTemplate> streamObjTempl = ObjectTemplate::New();
		streamObjTempl->SetInternalFieldCount(1);
		streamObjTempl->Set( 
			String::New( "addListener" ), FunctionTemplate::New( NetStreamAddListenerCallback ) 
		);
		Local<v8::Object> obj = streamObjTempl->NewInstance();
		
		GCHandle p = GCHandle::Alloc( stream );
		void* pv = (void*)GCHandle::ToIntPtr(p);
		obj->SetInternalField( 0, External::New( pv ) );
		
		RaiseConnectionEvent( obj );
		
		// kick off async read
		stream->read();
	}

	void RaiseListeningEvent() {
		printf( "%s\n", "http.server.raiseListeningEvent()" );
		printf( 
			"http.server.raiseListeningEvent(): calling %i callbacks\n", 
			( int )this->listeningCallbacks->size() 
		);
		for( int i=0; i < listeningCallbacks->size(); i++ ) {
			Handle<Function> fn = listeningCallbacks->at(i);
			fn->Call( *callbackReceiver, NULL, NULL );
		}
	}
	void RaiseConnectionEvent( Handle<Value> stream ) {
		printf( "%s\n", "http.server.RaiseConnectionEvent()" );
		for( int i=0; i < connectionCallbacks->size(); i++ ) {
			Handle<Function> fn = connectionCallbacks->at(i);
			Handle<Value> args[] = { stream };
			fn->Call( *callbackReceiver, 1, args );
		}
	}
	
	void AddListener( System::String^ eventname, Handle<Function> callback ) {
		if( eventname == "listening" ) {
			listeningCallbacks->push_back( callback );
		}
		else if( eventname == "connection" ) {
			connectionCallbacks->push_back( callback );
		}
		else if( eventname == "close" ) {
			closeCallbacks->push_back( callback );
		}
		else {
			throw "addListener called for unsupported event";
		}
	}
};

/**
* listenCallback is installed as the c++ callback that handles calling 
* listen() on a javascript NetServer instance.
*/
v8::Handle<v8::Value> listenCallback( const v8::Arguments& args ) {
	HandleScope handle_scope;
	printf( "listenCallback()\n" );
	
	Local<Object> self = args.This();
	Local<Value> p = self->GetInternalField(0);
	Local<External> wrap = Local<External>::Cast( p );
    void* ptr = wrap->Value();
	NetServer^ h = (NetServer^)(GCHandle::FromIntPtr( System::IntPtr( ptr ) ) ).Target;
	
	System::String^ hostname = StringV8ToDotnet( args[1] );
	int portnumber = args[0]->Int32Value();
	
	h->Listen( portnumber, hostname );
	return v8::Undefined();
}


/**
* createServerCallback handles the creation of a new NetServer.
* the global js function `createServer' is installed in the context, and 
* this function handles the call on the c++ side of things
*/
v8::Handle<v8::Value> createServerCallback( const v8::Arguments& args ) {
	HandleScope handle_scope;
	printf( "called v8 createServer callback\n" );		
	
	// callback must be persistent since we call it outside of this scope
	// TODO: need to figure out when to dispose -- probably only on unregister, 
	// which we haven't implemented yet.
	Persistent<Function> fn = Persistent<Function>::New( Handle<Function>::Cast( args[0] ) );
	
	String::Utf8Value str( (*fn)->ToString() );
	printf( "function is: %s\n", ToCString( str ) );
	
	NetServer^ netServer = gcnew NetServer( fn, &args.This() );
	// TODO: create js wrapper around netServer and return it
	Handle<ObjectTemplate> serverObjTempl = ObjectTemplate::New();
	serverObjTempl->SetInternalFieldCount(1);
	serverObjTempl->Set( String::New( "listen" ), FunctionTemplate::New( listenCallback ) );
	
	Local<Object> obj = serverObjTempl->NewInstance();
	// pin_ptr<NetServer^> p = &netServer;
	GCHandle p = GCHandle::Alloc( netServer );
	void* pv = (void*)GCHandle::ToIntPtr(p);
	obj->SetInternalField( 0, External::New( pv ) );
	
	return obj;
}

/**
* FunctionCallback is wired up to the javascript `puts()' function.
* we want to use this for doing sys.puts() node API call
*/
v8::Handle<v8::Value> PutsCallback( const v8::Arguments& args ) {
	HandleScope handle_scope;

	// convert first arg to cstring
	v8::String::Utf8Value str( args[0] );
	const char* cstr = ToCString( str );

	printf( "%s\n", cstr );
	return v8::Undefined();
}

/**
* Convert a v8 value to a .net string
*/
System::String^ StringV8ToDotnet( Handle<v8::Value> in_string ) {
	v8::String::Utf8Value str( in_string );
	const char* cstr = ToCString( str );
	System::String^ retstring = gcnew System::String( cstr, 0, lstrlen( cstr ) );
	return retstring;
}

v8::Handle<v8::Value> NetStreamAddListenerCallback( const v8::Arguments& args ) {
	HandleScope handle_scope;
	printf( "NetStreamAddListenerCallback()\n" );	
	
	Local<v8::Object> self = args.This();
	Local<Value> p = self->GetInternalField(0);
	Local<External> wrap = Local<External>::Cast( p );
    void* ptr = wrap->Value();
	NetStream^ h = (NetStream^)(GCHandle::FromIntPtr( System::IntPtr( ptr ) ) ).Target;
	
	System::String^ eventType = StringV8ToDotnet( args[0] );
	
	Persistent<Function> fn = Persistent<Function>::New( Handle<Function>::Cast( args[1] ) );
	
	// note we have listener type hard coded
	h->addListener( eventType, fn );
	return v8::Undefined();
}	

int main(int argc, char* argv[]) {
	printf("%s\n", "entered main()" );
	HandleScope handle_scope;	
	
	// create global and context
	v8::Handle<v8::ObjectTemplate> global = v8::ObjectTemplate::New();
	Local<FunctionTemplate> createServerCallbackTemplate = FunctionTemplate::New( createServerCallback );
	global->Set( String::New( "createServer" ), createServerCallbackTemplate );

	Local<FunctionTemplate> putstemplate = FunctionTemplate::New( PutsCallback );
	global->Set( String::New("puts"), putstemplate );

	v8::Handle<v8::Context> context = v8::Context::New(NULL, global);
	v8::Context::Scope context_scope(context);

	// TODO: test script is hard coded here, want to read from file
	Handle<Script> script = Script::Compile( String::New( 
			"createServer( function( stream ){ stream.addListener( 'data', function( data ){ puts(data);} ); } ).listen( 9980, 'localhost' );"
	));
	Handle<Value> scriptresult = script->Run();
	
	// TODO: main thread should process dispatch queue
	Thread::Sleep( Timeout::Infinite );
}
#include <v8.h>
#include <vector>
// needed for gcroot
#include <vcclr.h>
#using <System.dll>

using namespace v8;
using namespace System::Net::Sockets;
using namespace System::Net;
using namespace System::Threading;
using namespace System::IO;

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
		System::AsyncCallback^ ReadCallbackDelegate = gcnew System::AsyncCallback( this, &NetStream::ReadCallback );
		this->stream->BeginRead( buffer, 0, bufferSize, ReadCallbackDelegate, buffer );
	}
	
	void ReadCallback( System::IAsyncResult^ result ) {
		array<unsigned char>^ buffer = ( array<unsigned char>^ )result->AsyncState;
		int bytesRead = this->stream->EndRead( result );
		printf( "ReadCallback(): read %i bytes from stream\n", bytesRead );
		System::String^ chunk = System::Text::Encoding::UTF8->GetString( buffer, 0, bytesRead );
		
		if( bytesRead > 0 ) {

			System::Console::WriteLine( chunk );

			// queueWorkItem( { callback: raiseDataEvent, args: [ chunk ] } );
			array<unsigned char>^ nextBuffer = gcnew array<unsigned char>( bufferSize );
			System::AsyncCallback^ ReadCallbackDelegate = gcnew System::AsyncCallback( this, &NetStream::ReadCallback );
			this->stream->BeginRead( nextBuffer, 0, bufferSize, ReadCallbackDelegate, nextBuffer );
		}
		else {
			// queueWorkItem( { callback: raiseEndEvent, args: [] } );
		}
		
	}
	
	
	void write( System::String^ chunk /*, TODO: encoding */ ) {
		array<unsigned char>^ buffer = System::Text::Encoding::UTF8->GetBytes( chunk );
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
		for( int i=0; i < dataCallbacks->size(); i++ ) {
			Handle<Function> fn = dataCallbacks->at(i);
			// fn->Call( chunk );
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
		
		AddListener( "listening", in_callback );
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
		
		System::AsyncCallback^ ListenerCallbackDelegate = gcnew System::AsyncCallback( this, &NetServer::ListenerCallback );
		this->tcpListener->BeginAcceptTcpClient( ListenerCallbackDelegate, NULL );
	}
	
	void ListenerCallback( System::IAsyncResult^ result ) {
		printf("%s\n", "listener called" );
		System::AsyncCallback^ ListenerCallbackDelegate = gcnew System::AsyncCallback( this, &NetServer::ListenerCallback );
		this->tcpListener->BeginAcceptTcpClient( ListenerCallbackDelegate, NULL );
		
		TcpClient^ client = this->tcpListener->EndAcceptTcpClient( result );
		NetStream^ stream = gcnew NetStream( client->GetStream(), callbackReceiver );
		
		//queueWorkItem( { callback: raiseConnectionEvent, args: [ stream ] } );
		
		// TODO: we need wrapper object around NetStream to pass here
		RaiseConnectionEvent( v8::Undefined() );
		
		// kick off async read
		stream->read();
	}
	
	void RaiseListeningEvent() {
		printf( "%s\n", "http.server.raiseListeningEvent()" );
		printf( "http.server.raiseListeningEvent(): calling %i callbacks\n", (int)this->listeningCallbacks->size() );
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

// This callback is the native code associated with a v8 function.
// Eventually all of the v8 functions that we call will come from compiled
// scripts
v8::Handle<v8::Value> listenCallback( const v8::Arguments& args ) {
	HandleScope handle_scope;
	printf( "called v8 listen callback\n" );
	
	Local<Object> self = args.This();
	Local<Value> p = self->GetInternalField(0);
	Local<External> wrap = Local<External>::Cast( p );
    void* ptr = wrap->Value();
    // NetServer value = static_cast<NetServer*>(ptr);

	return v8::Undefined();
}

v8::Handle<v8::Value> addListenerCallback( const v8::Arguments& args ) {
	HandleScope handle_scope;
	printf( "called v8 addListenerCallback\n" );		
	return v8::Undefined();
}

v8::Handle<v8::Value> createServerCallback( const v8::Arguments& args ) {
	HandleScope handle_scope;
	printf( "called v8 createServer callback\n" );		
	
	Handle<Function> fn = Handle<Function>::Cast( args[0] );
	NetServer^ netServer = gcnew NetServer( fn, &args.This() );
	// TODO: create js wrapper around netServer and return it
	Handle<ObjectTemplate> serverObjTempl = ObjectTemplate::New();
	serverObjTempl->SetInternalFieldCount(1);
	serverObjTempl->Set( String::New( "listen" ), FunctionTemplate::New( listenCallback ) );
	
	Local<Object> obj = serverObjTempl->NewInstance();
	// pin_ptr<NetServer^> p = &netServer;
	gcroot<NetServer^>* p = netServer;
	obj->SetInternalField( 0, External::New( p ) );
	
	return obj;
	// return v8::Undefined();
}

int main(int argc, char* argv[]) {
	printf("%s\n", "entered main()" );
	HandleScope handle_scope;	
	
	// create global and context
	v8::Handle<v8::ObjectTemplate> global = v8::ObjectTemplate::New();
	Local<FunctionTemplate> createServerCallbackTemplate = FunctionTemplate::New( createServerCallback );
	global->Set( String::New( "createServer" ), createServerCallbackTemplate );
	v8::Handle<v8::Context> context = v8::Context::New(NULL, global);
	v8::Context::Scope context_scope(context);

	// note: GetFunction() fails if there is no context 
	Local<FunctionTemplate> cbTemplate = FunctionTemplate::New( listenCallback );
	Local<Function> cbFunction = cbTemplate->GetFunction();

	Handle<Script> script = Script::Compile( String::New( "createServer( function( stream ){ }, this ).listen( 9980, 'localhost' );") );
	Handle<Value> scriptresult = script->Run();
	
	// NetServer^ netServer = gcnew NetServer( cbFunction, &context->Global() );
	// netServer->Listen( 9980, "localhost" );
	
	Thread::Sleep( Timeout::Infinite );
}
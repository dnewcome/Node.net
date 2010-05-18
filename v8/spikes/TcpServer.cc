#include <v8.h>
#include <vector>
#using <System.dll>

using namespace v8;
using namespace System::Net::Sockets;
using namespace System::Net;
using namespace System::Threading;

ref class NetServer {
private:
	TcpListener^ tcpListener;
	std::vector<Handle<Function>>* listeningCallbacks;
	std::vector<Handle<Function>>* connectionCallbacks;
	std::vector<Handle<Function>>* closeCallbacks;
	
public:
	// constructor takes a v8 handle for callback
	NetServer( Handle<Function> in_callback ) {
		listeningCallbacks = new std::vector<Handle<Function>>();
		connectionCallbacks = new std::vector<Handle<Function>>();
		closeCallbacks = new std::vector<Handle<Function>>();
		// listeningCallbacks->push_back( in_callback );
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
		System::IO::Stream^ stream = client->GetStream();
		//queueWorkItem( { callback: raiseConnectionEvent, args: [ stream ] } );
		RaiseConnectionEvent();
		
		// kick off async read
		// stream.read();
		System::IO::StreamReader^ sr = gcnew System::IO::StreamReader( stream );
		System::String^ data = sr->ReadToEnd();
		System::Console::WriteLine( data );
		
	}
	
	void RaiseListeningEvent() {
		printf( "%s\n", "http.server.raiseListeningEvent()" );
		printf( "http.server.raiseListeningEvent(): calling %i callbacks\n", (int)this->listeningCallbacks->size() );
		for( int i=0; i < listeningCallbacks->size(); i++ ) {
			Handle<Function> fn = listeningCallbacks->at(i);
			// fn->Call();
		}
	}
	void RaiseConnectionEvent() {
		printf( "%s\n", "http.server.RaiseConnectionEvent()" );
		for( int i=0; i < connectionCallbacks->size(); i++ ) {
			Handle<Function> fn = connectionCallbacks->at(i);
			// fn->Call();
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

class NetStream {

};

int main(int argc, char* argv[]) {
	HandleScope handle_scope;
	
	// Handle<FunctionTemplate> createservertemplate = FunctionTemplate::New( createServerCallback );
	// global_templ->Set( String::New("createServer"), putstemplate );
	
	printf("%s\n", "working" );
	
	// create global and context
	v8::Handle<v8::ObjectTemplate> global = v8::ObjectTemplate::New();
	v8::Handle<v8::Context> context = v8::Context::New(NULL, global);
	v8::Context::Scope context_scope(context);

	// note: GetFunction() fails if there is no context 
	Local<FunctionTemplate> cbTemplate = FunctionTemplate::New();
	Local<Function> cbFunction = cbTemplate->GetFunction();
	
	NetServer^ netServer = gcnew NetServer( cbFunction );
	netServer->Listen( 9980, "localhost" );
	Thread::Sleep( Timeout::Infinite );
}
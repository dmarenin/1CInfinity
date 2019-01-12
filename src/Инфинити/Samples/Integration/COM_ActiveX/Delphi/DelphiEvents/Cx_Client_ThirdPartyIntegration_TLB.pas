unit Cx_Client_ThirdPartyIntegration_TLB;

// ************************************************************************ //
// WARNING                                                                    
// -------                                                                    
// The types declared in this file were generated from data read from a       
// Type Library. If this type library is explicitly or indirectly (via        
// another type library referring to this type library) re-imported, or the   
// 'Refresh' command of the Type Library Editor activated while editing the   
// Type Library, the contents of this file will be regenerated and all        
// manual modifications will be lost.                                         
// ************************************************************************ //

// PASTLWTR : 1.2
// File generated on 11.08.2013 16:32:52 from Type Library described below.

// ************************************************************************  //
// Type Lib: D:\Projects\1\Cx.Client.ThirdPartyIntegration.tlb (1)
// LIBID: {B8886771-17A4-4E19-85F3-5174571DED96}
// LCID: 0
// Helpfile: 
// HelpString: 
// DepndLst: 
//   (1) v2.0 stdole, (C:\Windows\SysWOW64\stdole2.tlb)
//   (2) v2.0 mscorlib, (D:\Projects\1\mscorlib.tlb)
//   (3) v1.0 Cx_Integration_AgatInfinityConnectorInterfaces, (D:\Projects\1\Cx.Integration.AgatInfinityConnectorInterfaces.tlb)
//   (4) v2.0 System, (C:\Windows\Microsoft.NET\Framework\v2.0.50727\System.tlb)
//   (5) v2.0 System_Windows_Forms, (C:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Windows.Forms.tlb)
// ************************************************************************ //
{$TYPEDADDRESS OFF} // Unit must be compiled without type-checked pointers. 
{$WARN SYMBOL_PLATFORM OFF}
{$WRITEABLECONST ON}
{$VARPROPSETTER ON}
interface

uses Windows, ActiveX, Classes, Cx_Integration_AgatInfinityConnectorInterfaces_TLB, Graphics, mscorlib_TLB, OleServer, StdVCL, 
System_TLB, System_Windows_Forms_TLB, Variants;
  

// *********************************************************************//
// GUIDS declared in the TypeLibrary. Following prefixes are used:        
//   Type Libraries     : LIBID_xxxx                                      
//   CoClasses          : CLASS_xxxx                                      
//   DISPInterfaces     : DIID_xxxx                                       
//   Non-DISP interfaces: IID_xxxx                                        
// *********************************************************************//
const
  // TypeLibrary Major and minor versions
  Cx_Client_ThirdPartyIntegrationMajorVersion = 1;
  Cx_Client_ThirdPartyIntegrationMinorVersion = 0;

  LIBID_Cx_Client_ThirdPartyIntegration: TGUID = '{B8886771-17A4-4E19-85F3-5174571DED96}';

  CLASS_CxUser: TGUID = '{46397187-BA0E-4E38-A3C3-298011915BD3}';
  IID_IComCallsConnectionsSeances: TGUID = '{9F10F096-03F5-4CD4-B08B-50CB02F910C7}';
  DIID_IComCallsConnectionsSeancesEvents: TGUID = '{658E7DE6-FCD4-4E79-995E-61462AE82186}';
  CLASS_CxCampaignsManagement: TGUID = '{A0C0DF9A-15A4-4F1B-B66E-03E5C3A880E4}';
  IID_IComCallManagement: TGUID = '{A14F2FF4-9904-489B-B3B6-5635145A6007}';
  DIID_ICallManagementEvents: TGUID = '{2CE69487-AB7F-4EA6-BA0A-1D1625B53FB0}';
  CLASS_CxCall: TGUID = '{CBB64491-25C6-47D1-BB31-D13FED9A5DAE}';
  CLASS_CxStatCall: TGUID = '{60F26000-7294-4E34-A4B7-E723E4F1B13D}';
  CLASS_CxCallManagement: TGUID = '{383CB250-FDFF-4D61-A325-FC783D256881}';
  CLASS_CxUtilsManagerInterface: TGUID = '{0D50D544-7E57-46E3-A409-914C7E9C3021}';
  IID_IComUsersManagement: TGUID = '{77BC4DBD-FA4F-40A6-8FD5-E0E1738BDE2F}';
  DIID_IUsersManagementEvents: TGUID = '{A1C9ED36-0DA9-4868-AEC6-6A575A91C741}';
  CLASS_CxCallsConnectionsSeancesInterface: TGUID = '{68A27F61-4902-4395-A604-5C14B3946F16}';
  CLASS_CxUsersManagement: TGUID = '{5E957264-52F0-4A05-B36D-145CF53C177B}';
  IID_ICxIntegrationCore: TGUID = '{28897AAB-AC3D-43BA-843A-DF16B048565B}';
  CLASS_CxComConnector: TGUID = '{182DE9A3-3FCE-4FA2-8408-72D03EE8F5F7}';
  DIID_IHostCtrlEvents: TGUID = '{09C624B0-DAEA-4642-B81F-125CE41BB6BF}';
  IID_HostCtrl: TGUID = '{9066DD6F-50C7-458C-86D8-48049DE9DFA8}';
  CLASS_ActiveXHostCtrl: TGUID = '{8747C50E-CF35-45B0-A4A7-1A1494367F31}';
  CLASS_CxConnection: TGUID = '{7DF9464F-5E34-4E27-BFA4-33D8CB68292E}';
  CLASS_CxConnectionByMonitoring: TGUID = '{FD2751A0-1586-48F6-9404-DD6D10802F0B}';
type

// *********************************************************************//
// Forward declaration of types defined in TypeLibrary                    
// *********************************************************************//
  IComCallsConnectionsSeances = interface;
  IComCallsConnectionsSeancesDisp = dispinterface;
  IComCallsConnectionsSeancesEvents = dispinterface;
  IComCallManagement = interface;
  IComCallManagementDisp = dispinterface;
  ICallManagementEvents = dispinterface;
  IComUsersManagement = interface;
  IComUsersManagementDisp = dispinterface;
  IUsersManagementEvents = dispinterface;
  ICxIntegrationCore = interface;
  ICxIntegrationCoreDisp = dispinterface;
  IHostCtrlEvents = dispinterface;
  HostCtrl = interface;
  HostCtrlDisp = dispinterface;

// *********************************************************************//
// Declaration of CoClasses defined in Type Library                       
// (NOTE: Here we map each CoClass to its Default Interface)              
// *********************************************************************//
  CxUser = IComUser;
  CxCampaignsManagement = ICampaignsManagement;
  CxCall = IComCall;
  CxStatCall = IComCall;
  CxCallManagement = IComCallManagement;
  CxUtilsManagerInterface = IUtilsManager;
  CxCallsConnectionsSeancesInterface = IComCallsConnectionsSeances;
  CxUsersManagement = IComUsersManagement;
  CxComConnector = ICxIntegrationCore;
  ActiveXHostCtrl = HostCtrl;
  CxConnection = IConnection;
  CxConnectionByMonitoring = IConnection;


// *********************************************************************//
// Interface: IComCallsConnectionsSeances
// Flags:     (4416) Dual OleAutomation Dispatchable
// GUID:      {9F10F096-03F5-4CD4-B08B-50CB02F910C7}
// *********************************************************************//
  IComCallsConnectionsSeances = interface(IDispatch)
    ['{9F10F096-03F5-4CD4-B08B-50CB02F910C7}']
    function GetConnectionsBySeanceID(const seanceID_: WideString): OleVariant; safecall;
    function GetConnectionsByCallID(const CallID: WideString): OleVariant; safecall;
    function GetConnections(timeStartFrom_: TDateTime; timeStartTo_: TDateTime; 
                            const number_: WideString; const lineID_: WideString): OleVariant; safecall;
    function GetConnectionByID(connectionID_: Int64): OleVariant; safecall;
    function GetCalls(timeStartFrom_: TDateTime; timeStartTo_: TDateTime; IDUser_: Int64): OleVariant; safecall;
    function GetCurrentConnections: OleVariant; safecall;
    procedure _Set_ConnectionCreatedEvent(Param1: OleVariant); safecall;
    procedure _Set_ConnectionDeletedEvent(Param1: OleVariant); safecall;
    procedure _Set_ConnectionChangedEvent(Param1: OleVariant); safecall;
  end;

// *********************************************************************//
// DispIntf:  IComCallsConnectionsSeancesDisp
// Flags:     (4416) Dual OleAutomation Dispatchable
// GUID:      {9F10F096-03F5-4CD4-B08B-50CB02F910C7}
// *********************************************************************//
  IComCallsConnectionsSeancesDisp = dispinterface
    ['{9F10F096-03F5-4CD4-B08B-50CB02F910C7}']
    function GetConnectionsBySeanceID(const seanceID_: WideString): OleVariant; dispid 10000;
    function GetConnectionsByCallID(const CallID: WideString): OleVariant; dispid 10001;
    function GetConnections(timeStartFrom_: TDateTime; timeStartTo_: TDateTime; 
                            const number_: WideString; const lineID_: WideString): OleVariant; dispid 10002;
    function GetConnectionByID(connectionID_: {??Int64}OleVariant): OleVariant; dispid 10003;
    function GetCalls(timeStartFrom_: TDateTime; timeStartTo_: TDateTime; 
                      IDUser_: {??Int64}OleVariant): OleVariant; dispid 10004;
    function GetCurrentConnections: OleVariant; dispid 10005;
  end;

// *********************************************************************//
// DispIntf:  IComCallsConnectionsSeancesEvents
// Flags:     (4096) Dispatchable
// GUID:      {658E7DE6-FCD4-4E79-995E-61462AE82186}
// *********************************************************************//
  IComCallsConnectionsSeancesEvents = dispinterface
    ['{658E7DE6-FCD4-4E79-995E-61462AE82186}']
    procedure ConnectionCreated(const Connection: IConnection); dispid 1001;
    procedure ConnectionDeleted(const Connection: IConnection); dispid 1002;
    procedure ConnectionChanged(const Connection: IConnection); dispid 1003;
  end;

// *********************************************************************//
// Interface: IComCallManagement
// Flags:     (4416) Dual OleAutomation Dispatchable
// GUID:      {A14F2FF4-9904-489B-B3B6-5635145A6007}
// *********************************************************************//
  IComCallManagement = interface(IDispatch)
    ['{A14F2FF4-9904-489B-B3B6-5635145A6007}']
    function Get_Extension: WideString; safecall;
    function Get_Calls: OleVariant; safecall;
    function MakeCall(const number_: WideString; const callerName_: WideString; 
                      const extension_: WideString): IComCall; safecall;
    function SetExtensionState(state_: ExtensionState; const extension_: WideString): WordBool; safecall;
    procedure _Set_CallCreatedEvent(Param1: OleVariant); safecall;
    procedure _Set_CallDeletedEvent(Param1: OleVariant); safecall;
    procedure _Set_ExtensionStateChangedEvent(Param1: OleVariant); safecall;
    procedure _Set_DisposedEvent(Param1: OleVariant); safecall;
    procedure _Set_StateChangedEvent(Param1: OleVariant); safecall;
    procedure _Set_NumberChangedEvent(Param1: OleVariant); safecall;
    procedure _Set_NameChangedEvent(Param1: OleVariant); safecall;
    procedure _Set_DialedNumberChangedEvent(Param1: OleVariant); safecall;
    procedure _Set_CommandsStateChangedEvent(Param1: OleVariant); safecall;
    procedure _Set_DigitsSentEvent(Param1: OleVariant); safecall;
    procedure _Set_AbonentCallInfoChangedEvent(Param1: OleVariant); safecall;
    property Extension: WideString read Get_Extension;
    property Calls: OleVariant read Get_Calls;
  end;

// *********************************************************************//
// DispIntf:  IComCallManagementDisp
// Flags:     (4416) Dual OleAutomation Dispatchable
// GUID:      {A14F2FF4-9904-489B-B3B6-5635145A6007}
// *********************************************************************//
  IComCallManagementDisp = dispinterface
    ['{A14F2FF4-9904-489B-B3B6-5635145A6007}']
    property Extension: WideString readonly dispid 10000;
    property Calls: OleVariant readonly dispid 10001;
    function MakeCall(const number_: WideString; const callerName_: WideString; 
                      const extension_: WideString): IComCall; dispid 10002;
    function SetExtensionState(state_: ExtensionState; const extension_: WideString): WordBool; dispid 10003;
  end;

// *********************************************************************//
// DispIntf:  ICallManagementEvents
// Flags:     (4096) Dispatchable
// GUID:      {2CE69487-AB7F-4EA6-BA0A-1D1625B53FB0}
// *********************************************************************//
  ICallManagementEvents = dispinterface
    ['{2CE69487-AB7F-4EA6-BA0A-1D1625B53FB0}']
    procedure CallCreated(const call_: IComCall); dispid 1001;
    procedure CallDeleted(const call_: IComCall); dispid 1002;
    procedure ExtensionStateChanged(const Extension: WideString; State: ExtensionState); dispid 1003;
    procedure Disposed; dispid 1004;
    procedure StateChanged(const call_: IComCall; oldState: CallState; State: CallState); dispid 1005;
    procedure NumberChanged(const call_: IComCall); dispid 1006;
    procedure NameChanged(const call_: IComCall); dispid 1007;
    procedure DialedNumberChanged(const call_: IComCall); dispid 1008;
    procedure CommandsStateChanged(const call_: IComCall); dispid 1009;
    procedure DigitsSent(const call_: IComCall; const digits: WideString); dispid 1010;
    procedure AbonentCallInfoChanged(const call_: IComCall); dispid 1011;
  end;

// *********************************************************************//
// Interface: IComUsersManagement
// Flags:     (4416) Dual OleAutomation Dispatchable
// GUID:      {77BC4DBD-FA4F-40A6-8FD5-E0E1738BDE2F}
// *********************************************************************//
  IComUsersManagement = interface(IDispatch)
    ['{77BC4DBD-FA4F-40A6-8FD5-E0E1738BDE2F}']
    function Get_Users: OleVariant; safecall;
    function Get_CurrentUser: IComUser; safecall;
    procedure _Set_StateChangedEvent(Param1: OleVariant); safecall;
    property Users: OleVariant read Get_Users;
    property CurrentUser: IComUser read Get_CurrentUser;
  end;

// *********************************************************************//
// DispIntf:  IComUsersManagementDisp
// Flags:     (4416) Dual OleAutomation Dispatchable
// GUID:      {77BC4DBD-FA4F-40A6-8FD5-E0E1738BDE2F}
// *********************************************************************//
  IComUsersManagementDisp = dispinterface
    ['{77BC4DBD-FA4F-40A6-8FD5-E0E1738BDE2F}']
    property Users: OleVariant readonly dispid 10000;
    property CurrentUser: IComUser readonly dispid 10001;
  end;

// *********************************************************************//
// DispIntf:  IUsersManagementEvents
// Flags:     (4096) Dispatchable
// GUID:      {A1C9ED36-0DA9-4868-AEC6-6A575A91C741}
// *********************************************************************//
  IUsersManagementEvents = dispinterface
    ['{A1C9ED36-0DA9-4868-AEC6-6A575A91C741}']
    procedure StateChanged(const User: IComUser; oldState: OleVariant; State: OleVariant); dispid 1001;
  end;

// *********************************************************************//
// Interface: ICxIntegrationCore
// Flags:     (4416) Dual OleAutomation Dispatchable
// GUID:      {28897AAB-AC3D-43BA-843A-DF16B048565B}
// *********************************************************************//
  ICxIntegrationCore = interface(IDispatch)
    ['{28897AAB-AC3D-43BA-843A-DF16B048565B}']
    function Get_CoreID: WideString; safecall;
    procedure SetUseExceptions(Value: WordBool); safecall;
    function Get_LastError: WideString; safecall;
    function Get_LastErrorDetailed: WideString; safecall;
    procedure Connect(const ConnectionString: WideString); safecall;
    function Logon: WordBool; safecall;
    function LogonEx(const Login: WideString; const Password: WideString; const Role: WideString; 
                     const ServerAddress: WideString; ServerPort: Integer): Integer; safecall;
    procedure Logoff; safecall;
    procedure Disconnect; safecall;
    procedure Close; safecall;
    function Get_IsConnected: WordBool; safecall;
    function LogonResultToString(Result: Integer): WideString; safecall;
    function GetCallManagement(const Extension: WideString): IComCallManagement; safecall;
    function GetCallsConnectionsSeances: IComCallsConnectionsSeances; safecall;
    function GetUsersManagement: IComUsersManagement; safecall;
    function GetCampaignsManagement: ICampaignsManagement; safecall;
    function GetUtilsManager: IUtilsManager; safecall;
    function GetCxObjectType(obj: OleVariant): Integer; safecall;
    function Get_ObjectsList: OleVariant; safecall;
    function GetObjects(const name: WideString): OleVariant; safecall;
    function GetObjectByID(const name: WideString; id: Int64): OleVariant; safecall;
    function GetFields(obj: OleVariant): OleVariant; safecall;
    procedure DataObjectPost(obj: OleVariant); safecall;
    procedure DataObjectClose(obj: OleVariant); safecall;
    function ValueToString(obj: OleVariant): WideString; safecall;
    function Get_NamedTools: OleVariant; safecall;
    function Get_Tools: OleVariant; safecall;
    property CoreID: WideString read Get_CoreID;
    property LastError: WideString read Get_LastError;
    property LastErrorDetailed: WideString read Get_LastErrorDetailed;
    property IsConnected: WordBool read Get_IsConnected;
    property ObjectsList: OleVariant read Get_ObjectsList;
    property NamedTools: OleVariant read Get_NamedTools;
    property Tools: OleVariant read Get_Tools;
  end;

// *********************************************************************//
// DispIntf:  ICxIntegrationCoreDisp
// Flags:     (4416) Dual OleAutomation Dispatchable
// GUID:      {28897AAB-AC3D-43BA-843A-DF16B048565B}
// *********************************************************************//
  ICxIntegrationCoreDisp = dispinterface
    ['{28897AAB-AC3D-43BA-843A-DF16B048565B}']
    property CoreID: WideString readonly dispid 10000;
    procedure SetUseExceptions(Value: WordBool); dispid 10001;
    property LastError: WideString readonly dispid 10002;
    property LastErrorDetailed: WideString readonly dispid 10003;
    procedure Connect(const ConnectionString: WideString); dispid 10004;
    function Logon: WordBool; dispid 10005;
    function LogonEx(const Login: WideString; const Password: WideString; const Role: WideString; 
                     const ServerAddress: WideString; ServerPort: Integer): Integer; dispid 10006;
    procedure Logoff; dispid 10007;
    procedure Disconnect; dispid 10008;
    procedure Close; dispid 10009;
    property IsConnected: WordBool readonly dispid 10010;
    function LogonResultToString(Result: Integer): WideString; dispid 10011;
    function GetCallManagement(const Extension: WideString): IComCallManagement; dispid 10012;
    function GetCallsConnectionsSeances: IComCallsConnectionsSeances; dispid 10013;
    function GetUsersManagement: IComUsersManagement; dispid 10014;
    function GetCampaignsManagement: ICampaignsManagement; dispid 10015;
    function GetUtilsManager: IUtilsManager; dispid 10016;
    function GetCxObjectType(obj: OleVariant): Integer; dispid 20000;
    property ObjectsList: OleVariant readonly dispid 20001;
    function GetObjects(const name: WideString): OleVariant; dispid 20002;
    function GetObjectByID(const name: WideString; id: {??Int64}OleVariant): OleVariant; dispid 20003;
    function GetFields(obj: OleVariant): OleVariant; dispid 20004;
    procedure DataObjectPost(obj: OleVariant); dispid 20005;
    procedure DataObjectClose(obj: OleVariant); dispid 20006;
    function ValueToString(obj: OleVariant): WideString; dispid 20007;
    property NamedTools: OleVariant readonly dispid 20008;
    property Tools: OleVariant readonly dispid 20009;
  end;

// *********************************************************************//
// DispIntf:  IHostCtrlEvents
// Flags:     (4096) Dispatchable
// GUID:      {09C624B0-DAEA-4642-B81F-125CE41BB6BF}
// *********************************************************************//
  IHostCtrlEvents = dispinterface
    ['{09C624B0-DAEA-4642-B81F-125CE41BB6BF}']
  end;

// *********************************************************************//
// Interface: HostCtrl
// Flags:     (4416) Dual OleAutomation Dispatchable
// GUID:      {9066DD6F-50C7-458C-86D8-48049DE9DFA8}
// *********************************************************************//
  HostCtrl = interface(IDispatch)
    ['{9066DD6F-50C7-458C-86D8-48049DE9DFA8}']
    function GetCoreID: WideString; safecall;
    procedure SetCoreID(const Value: WideString); safecall;
    function Get_Content: OleVariant; safecall;
    procedure ShowContent(const name: WideString); safecall;
    procedure ShowContentByGuid(const name: WideString); safecall;
    procedure Clear; safecall;
    function Get_Visible: WordBool; safecall;
    procedure Set_Visible(pRetVal: WordBool); safecall;
    function Get_Enabled: WordBool; safecall;
    procedure Set_Enabled(pRetVal: WordBool); safecall;
    procedure Refresh; safecall;
    property Content: OleVariant read Get_Content;
    property Visible: WordBool read Get_Visible write Set_Visible;
    property Enabled: WordBool read Get_Enabled write Set_Enabled;
  end;

// *********************************************************************//
// DispIntf:  HostCtrlDisp
// Flags:     (4416) Dual OleAutomation Dispatchable
// GUID:      {9066DD6F-50C7-458C-86D8-48049DE9DFA8}
// *********************************************************************//
  HostCtrlDisp = dispinterface
    ['{9066DD6F-50C7-458C-86D8-48049DE9DFA8}']
    function GetCoreID: WideString; dispid 10000;
    procedure SetCoreID(const Value: WideString); dispid 10001;
    property Content: OleVariant readonly dispid 10002;
    procedure ShowContent(const name: WideString); dispid 10003;
    procedure ShowContentByGuid(const name: WideString); dispid 10004;
    procedure Clear; dispid 10005;
    property Visible: WordBool dispid 20000;
    property Enabled: WordBool dispid 20001;
    procedure Refresh; dispid 20002;
  end;

// *********************************************************************//
// The Class CoCxUser provides a Create and CreateRemote method to          
// create instances of the default interface IComUser exposed by              
// the CoClass CxUser. The functions are intended to be used by             
// clients wishing to automate the CoClass objects exposed by the         
// server of this typelibrary.                                            
// *********************************************************************//
  CoCxUser = class
    class function Create: IComUser;
    class function CreateRemote(const MachineName: string): IComUser;
  end;

// *********************************************************************//
// The Class CoCxCampaignsManagement provides a Create and CreateRemote method to          
// create instances of the default interface ICampaignsManagement exposed by              
// the CoClass CxCampaignsManagement. The functions are intended to be used by             
// clients wishing to automate the CoClass objects exposed by the         
// server of this typelibrary.                                            
// *********************************************************************//
  CoCxCampaignsManagement = class
    class function Create: ICampaignsManagement;
    class function CreateRemote(const MachineName: string): ICampaignsManagement;
  end;

// *********************************************************************//
// The Class CoCxCall provides a Create and CreateRemote method to          
// create instances of the default interface IComCall exposed by              
// the CoClass CxCall. The functions are intended to be used by             
// clients wishing to automate the CoClass objects exposed by the         
// server of this typelibrary.                                            
// *********************************************************************//
  CoCxCall = class
    class function Create: IComCall;
    class function CreateRemote(const MachineName: string): IComCall;
  end;

// *********************************************************************//
// The Class CoCxStatCall provides a Create and CreateRemote method to          
// create instances of the default interface IComCall exposed by              
// the CoClass CxStatCall. The functions are intended to be used by             
// clients wishing to automate the CoClass objects exposed by the         
// server of this typelibrary.                                            
// *********************************************************************//
  CoCxStatCall = class
    class function Create: IComCall;
    class function CreateRemote(const MachineName: string): IComCall;
  end;

// *********************************************************************//
// The Class CoCxCallManagement provides a Create and CreateRemote method to          
// create instances of the default interface IComCallManagement exposed by              
// the CoClass CxCallManagement. The functions are intended to be used by             
// clients wishing to automate the CoClass objects exposed by the         
// server of this typelibrary.                                            
// *********************************************************************//
  CoCxCallManagement = class
    class function Create: IComCallManagement;
    class function CreateRemote(const MachineName: string): IComCallManagement;
  end;

// *********************************************************************//
// The Class CoCxUtilsManagerInterface provides a Create and CreateRemote method to          
// create instances of the default interface IUtilsManager exposed by              
// the CoClass CxUtilsManagerInterface. The functions are intended to be used by             
// clients wishing to automate the CoClass objects exposed by the         
// server of this typelibrary.                                            
// *********************************************************************//
  CoCxUtilsManagerInterface = class
    class function Create: IUtilsManager;
    class function CreateRemote(const MachineName: string): IUtilsManager;
  end;

// *********************************************************************//
// The Class CoCxCallsConnectionsSeancesInterface provides a Create and CreateRemote method to          
// create instances of the default interface IComCallsConnectionsSeances exposed by              
// the CoClass CxCallsConnectionsSeancesInterface. The functions are intended to be used by             
// clients wishing to automate the CoClass objects exposed by the         
// server of this typelibrary.                                            
// *********************************************************************//
  CoCxCallsConnectionsSeancesInterface = class
    class function Create: IComCallsConnectionsSeances;
    class function CreateRemote(const MachineName: string): IComCallsConnectionsSeances;
  end;

// *********************************************************************//
// The Class CoCxUsersManagement provides a Create and CreateRemote method to          
// create instances of the default interface IComUsersManagement exposed by              
// the CoClass CxUsersManagement. The functions are intended to be used by             
// clients wishing to automate the CoClass objects exposed by the         
// server of this typelibrary.                                            
// *********************************************************************//
  CoCxUsersManagement = class
    class function Create: IComUsersManagement;
    class function CreateRemote(const MachineName: string): IComUsersManagement;
  end;

// *********************************************************************//
// The Class CoCxComConnector provides a Create and CreateRemote method to          
// create instances of the default interface ICxIntegrationCore exposed by              
// the CoClass CxComConnector. The functions are intended to be used by             
// clients wishing to automate the CoClass objects exposed by the         
// server of this typelibrary.                                            
// *********************************************************************//
  CoCxComConnector = class
    class function Create: ICxIntegrationCore;
    class function CreateRemote(const MachineName: string): ICxIntegrationCore;
  end;

// *********************************************************************//
// The Class CoActiveXHostCtrl provides a Create and CreateRemote method to          
// create instances of the default interface HostCtrl exposed by              
// the CoClass ActiveXHostCtrl. The functions are intended to be used by             
// clients wishing to automate the CoClass objects exposed by the         
// server of this typelibrary.                                            
// *********************************************************************//
  CoActiveXHostCtrl = class
    class function Create: HostCtrl;
    class function CreateRemote(const MachineName: string): HostCtrl;
  end;

// *********************************************************************//
// The Class CoCxConnection provides a Create and CreateRemote method to          
// create instances of the default interface IConnection exposed by              
// the CoClass CxConnection. The functions are intended to be used by             
// clients wishing to automate the CoClass objects exposed by the         
// server of this typelibrary.                                            
// *********************************************************************//
  CoCxConnection = class
    class function Create: IConnection;
    class function CreateRemote(const MachineName: string): IConnection;
  end;

// *********************************************************************//
// The Class CoCxConnectionByMonitoring provides a Create and CreateRemote method to          
// create instances of the default interface IConnection exposed by              
// the CoClass CxConnectionByMonitoring. The functions are intended to be used by             
// clients wishing to automate the CoClass objects exposed by the         
// server of this typelibrary.                                            
// *********************************************************************//
  CoCxConnectionByMonitoring = class
    class function Create: IConnection;
    class function CreateRemote(const MachineName: string): IConnection;
  end;

implementation

uses ComObj;

class function CoCxUser.Create: IComUser;
begin
  Result := CreateComObject(CLASS_CxUser) as IComUser;
end;

class function CoCxUser.CreateRemote(const MachineName: string): IComUser;
begin
  Result := CreateRemoteComObject(MachineName, CLASS_CxUser) as IComUser;
end;

class function CoCxCampaignsManagement.Create: ICampaignsManagement;
begin
  Result := CreateComObject(CLASS_CxCampaignsManagement) as ICampaignsManagement;
end;

class function CoCxCampaignsManagement.CreateRemote(const MachineName: string): ICampaignsManagement;
begin
  Result := CreateRemoteComObject(MachineName, CLASS_CxCampaignsManagement) as ICampaignsManagement;
end;

class function CoCxCall.Create: IComCall;
begin
  Result := CreateComObject(CLASS_CxCall) as IComCall;
end;

class function CoCxCall.CreateRemote(const MachineName: string): IComCall;
begin
  Result := CreateRemoteComObject(MachineName, CLASS_CxCall) as IComCall;
end;

class function CoCxStatCall.Create: IComCall;
begin
  Result := CreateComObject(CLASS_CxStatCall) as IComCall;
end;

class function CoCxStatCall.CreateRemote(const MachineName: string): IComCall;
begin
  Result := CreateRemoteComObject(MachineName, CLASS_CxStatCall) as IComCall;
end;

class function CoCxCallManagement.Create: IComCallManagement;
begin
  Result := CreateComObject(CLASS_CxCallManagement) as IComCallManagement;
end;

class function CoCxCallManagement.CreateRemote(const MachineName: string): IComCallManagement;
begin
  Result := CreateRemoteComObject(MachineName, CLASS_CxCallManagement) as IComCallManagement;
end;

class function CoCxUtilsManagerInterface.Create: IUtilsManager;
begin
  Result := CreateComObject(CLASS_CxUtilsManagerInterface) as IUtilsManager;
end;

class function CoCxUtilsManagerInterface.CreateRemote(const MachineName: string): IUtilsManager;
begin
  Result := CreateRemoteComObject(MachineName, CLASS_CxUtilsManagerInterface) as IUtilsManager;
end;

class function CoCxCallsConnectionsSeancesInterface.Create: IComCallsConnectionsSeances;
begin
  Result := CreateComObject(CLASS_CxCallsConnectionsSeancesInterface) as IComCallsConnectionsSeances;
end;

class function CoCxCallsConnectionsSeancesInterface.CreateRemote(const MachineName: string): IComCallsConnectionsSeances;
begin
  Result := CreateRemoteComObject(MachineName, CLASS_CxCallsConnectionsSeancesInterface) as IComCallsConnectionsSeances;
end;

class function CoCxUsersManagement.Create: IComUsersManagement;
begin
  Result := CreateComObject(CLASS_CxUsersManagement) as IComUsersManagement;
end;

class function CoCxUsersManagement.CreateRemote(const MachineName: string): IComUsersManagement;
begin
  Result := CreateRemoteComObject(MachineName, CLASS_CxUsersManagement) as IComUsersManagement;
end;

class function CoCxComConnector.Create: ICxIntegrationCore;
begin
  Result := CreateComObject(CLASS_CxComConnector) as ICxIntegrationCore;
end;

class function CoCxComConnector.CreateRemote(const MachineName: string): ICxIntegrationCore;
begin
  Result := CreateRemoteComObject(MachineName, CLASS_CxComConnector) as ICxIntegrationCore;
end;

class function CoActiveXHostCtrl.Create: HostCtrl;
begin
  Result := CreateComObject(CLASS_ActiveXHostCtrl) as HostCtrl;
end;

class function CoActiveXHostCtrl.CreateRemote(const MachineName: string): HostCtrl;
begin
  Result := CreateRemoteComObject(MachineName, CLASS_ActiveXHostCtrl) as HostCtrl;
end;

class function CoCxConnection.Create: IConnection;
begin
  Result := CreateComObject(CLASS_CxConnection) as IConnection;
end;

class function CoCxConnection.CreateRemote(const MachineName: string): IConnection;
begin
  Result := CreateRemoteComObject(MachineName, CLASS_CxConnection) as IConnection;
end;

class function CoCxConnectionByMonitoring.Create: IConnection;
begin
  Result := CreateComObject(CLASS_CxConnectionByMonitoring) as IConnection;
end;

class function CoCxConnectionByMonitoring.CreateRemote(const MachineName: string): IConnection;
begin
  Result := CreateRemoteComObject(MachineName, CLASS_CxConnectionByMonitoring) as IConnection;
end;

end.

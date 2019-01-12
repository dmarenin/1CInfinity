unit Cx_Integration_AgatInfinityConnectorInterfaces_TLB;

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
// Type Lib: D:\Projects\1\Cx.Integration.AgatInfinityConnectorInterfaces.tlb (1)
// LIBID: {3C4A287E-16FF-4ECB-BB83-3F5B8C218568}
// LCID: 0
// Helpfile: 
// HelpString: 
// DepndLst: 
//   (1) v2.0 stdole, (C:\Windows\SysWOW64\stdole2.tlb)
//   (2) v2.0 mscorlib, (D:\Projects\1\mscorlib.tlb)
// Parent TypeLibrary:
//   (0) v1.0 Cx_Client_ThirdPartyIntegration, (D:\Projects\1\Cx.Client.ThirdPartyIntegration.tlb)
// ************************************************************************ //
{$TYPEDADDRESS OFF} // Unit must be compiled without type-checked pointers. 
{$WARN SYMBOL_PLATFORM OFF}
{$WRITEABLECONST ON}
{$VARPROPSETTER ON}
interface

uses Windows, ActiveX, Classes, Graphics, mscorlib_TLB, StdVCL, Variants;
  

// *********************************************************************//
// GUIDS declared in the TypeLibrary. Following prefixes are used:        
//   Type Libraries     : LIBID_xxxx                                      
//   CoClasses          : CLASS_xxxx                                      
//   DISPInterfaces     : DIID_xxxx                                       
//   Non-DISP interfaces: IID_xxxx                                        
// *********************************************************************//
const
  // TypeLibrary Major and minor versions
  Cx_Integration_AgatInfinityConnectorInterfacesMajorVersion = 1;
  Cx_Integration_AgatInfinityConnectorInterfacesMinorVersion = 0;

  LIBID_Cx_Integration_AgatInfinityConnectorInterfaces: TGUID = '{3C4A287E-16FF-4ECB-BB83-3F5B8C218568}';

  IID_ICampaignsManagement: TGUID = '{2037C657-C41F-4BCB-9DCB-E4EC4656C265}';
  IID_IUtilsManager: TGUID = '{9F96C567-D123-45BF-BF4E-C5280581B89F}';
  IID_IComUser: TGUID = '{957F291F-ABC6-4700-BF19-00C256B0DE76}';
  IID_IComCall: TGUID = '{3A36C9B6-9D14-430B-B179-A9EA07A789B6}';
  IID_IConnection: TGUID = '{78BA85D8-415B-4431-BF0F-E6377E58AE00}';

// *********************************************************************//
// Declaration of Enumerations defined in Type Library                    
// *********************************************************************//
// Constants for enum CallState
type
  CallState = TOleEnum;
const
  CallState_Unknown = $00000000;
  CallState_Ringing = $00000001;
  CallState_Reminder = $00000002;
  CallState_Dialtone = $0000000B;
  CallState_DialNumber = $0000000C;
  CallState_Alerting = $0000000D;
  CallState_Connected = $00000015;
  CallState_Conference = $0000001F;
  CallState_InConference = $00000020;
  CallState_Hold = $00000029;
  CallState_OnHold = $0000002A;
  CallState_Transfer = $00000033;
  CallState_Disconnected = $00000063;
  CallState_Finished = $00000064;

// Constants for enum CallDirection
type
  CallDirection = TOleEnum;
const
  CallDirection_Unknown = $00000000;
  CallDirection_In = $00000001;
  CallDirection_Out = $00000002;

// Constants for enum ExtensionState
type
  ExtensionState = TOleEnum;
const
  ExtensionState_Unknown = $00000000;
  ExtensionState_Online = $00000001;
  ExtensionState_Away = $00000002;
  ExtensionState_NotAvailable = $00000003;
  ExtensionState_Timeout = $00000004;

// Constants for enum ConnectionState
type
  ConnectionState = TOleEnum;
const
  ConnectionState_Unknown = $00000000;
  ConnectionState_Waiting = $00000001;
  ConnectionState_Talking = $00000015;
  ConnectionState_Conference = $0000001F;
  ConnectionState_Hold = $00000029;
  ConnectionState_Disconnected = $00000063;
  ConnectionState_Finished = $00000064;

type

// *********************************************************************//
// Forward declaration of types defined in TypeLibrary                    
// *********************************************************************//
  ICampaignsManagement = interface;
  ICampaignsManagementDisp = dispinterface;
  IUtilsManager = interface;
  IUtilsManagerDisp = dispinterface;
  IComUser = interface;
  IComUserDisp = dispinterface;
  IComCall = interface;
  IComCallDisp = dispinterface;
  IConnection = interface;
  IConnectionDisp = dispinterface;

// *********************************************************************//
// Interface: ICampaignsManagement
// Flags:     (4416) Dual OleAutomation Dispatchable
// GUID:      {2037C657-C41F-4BCB-9DCB-E4EC4656C265}
// *********************************************************************//
  ICampaignsManagement = interface(IDispatch)
    ['{2037C657-C41F-4BCB-9DCB-E4EC4656C265}']
    procedure StartCampaign(IDCampaign: Int64); safecall;
    procedure StopCampaign(IDCampaign: Int64; bForce: WordBool); safecall;
    function GetCampaignStatus(IDCampaign: Int64): LongWord; safecall;
  end;

// *********************************************************************//
// DispIntf:  ICampaignsManagementDisp
// Flags:     (4416) Dual OleAutomation Dispatchable
// GUID:      {2037C657-C41F-4BCB-9DCB-E4EC4656C265}
// *********************************************************************//
  ICampaignsManagementDisp = dispinterface
    ['{2037C657-C41F-4BCB-9DCB-E4EC4656C265}']
    procedure StartCampaign(IDCampaign: {??Int64}OleVariant); dispid 10000;
    procedure StopCampaign(IDCampaign: {??Int64}OleVariant; bForce: WordBool); dispid 10001;
    function GetCampaignStatus(IDCampaign: {??Int64}OleVariant): LongWord; dispid 10002;
  end;

// *********************************************************************//
// Interface: IUtilsManager
// Flags:     (4416) Dual OleAutomation Dispatchable
// GUID:      {9F96C567-D123-45BF-BF4E-C5280581B89F}
// *********************************************************************//
  IUtilsManager = interface(IDispatch)
    ['{9F96C567-D123-45BF-BF4E-C5280581B89F}']
    function GetConfigurationParameter(const Name: WideString; IDObject: Int64): OleVariant; safecall;
    function TryGetConfigurationParameter(const Name: WideString; IDObject: Int64; 
                                          out Value: OleVariant): WordBool; safecall;
    procedure SetConfigurationParameter(const Name: WideString; IDObject: Int64; Value: OleVariant); safecall;
  end;

// *********************************************************************//
// DispIntf:  IUtilsManagerDisp
// Flags:     (4416) Dual OleAutomation Dispatchable
// GUID:      {9F96C567-D123-45BF-BF4E-C5280581B89F}
// *********************************************************************//
  IUtilsManagerDisp = dispinterface
    ['{9F96C567-D123-45BF-BF4E-C5280581B89F}']
    function GetConfigurationParameter(const Name: WideString; IDObject: {??Int64}OleVariant): OleVariant; dispid 10000;
    function TryGetConfigurationParameter(const Name: WideString; IDObject: {??Int64}OleVariant; 
                                          out Value: OleVariant): WordBool; dispid 10001;
    procedure SetConfigurationParameter(const Name: WideString; IDObject: {??Int64}OleVariant; 
                                        Value: OleVariant); dispid 10002;
  end;

// *********************************************************************//
// Interface: IComUser
// Flags:     (4416) Dual OleAutomation Dispatchable
// GUID:      {957F291F-ABC6-4700-BF19-00C256B0DE76}
// *********************************************************************//
  IComUser = interface(IDispatch)
    ['{957F291F-ABC6-4700-BF19-00C256B0DE76}']
    function Get_ID: Int64; safecall;
    function Get_Login: WideString; safecall;
    function Get_State: Int64; safecall;
    procedure Set_State(pRetVal: Int64); safecall;
    function Get_Extensions: OleVariant; safecall;
    function Get_IsLoggedIn: WordBool; safecall;
    function Logon(const Password: WideString; IDRole: OleVariant): WordBool; safecall;
    function LogonEx(const Password: WideString; IDRole: OleVariant): Int64; safecall;
    procedure Logoff; safecall;
    property ID: Int64 read Get_ID;
    property Login: WideString read Get_Login;
    property State: Int64 read Get_State write Set_State;
    property Extensions: OleVariant read Get_Extensions;
    property IsLoggedIn: WordBool read Get_IsLoggedIn;
  end;

// *********************************************************************//
// DispIntf:  IComUserDisp
// Flags:     (4416) Dual OleAutomation Dispatchable
// GUID:      {957F291F-ABC6-4700-BF19-00C256B0DE76}
// *********************************************************************//
  IComUserDisp = dispinterface
    ['{957F291F-ABC6-4700-BF19-00C256B0DE76}']
    property ID: {??Int64}OleVariant readonly dispid 10000;
    property Login: WideString readonly dispid 10001;
    property State: {??Int64}OleVariant dispid 10002;
    property Extensions: OleVariant readonly dispid 10003;
    property IsLoggedIn: WordBool readonly dispid 10004;
    function Logon(const Password: WideString; IDRole: OleVariant): WordBool; dispid 10005;
    function LogonEx(const Password: WideString; IDRole: OleVariant): {??Int64}OleVariant; dispid 10006;
    procedure Logoff; dispid 10007;
  end;

// *********************************************************************//
// Interface: IComCall
// Flags:     (4416) Dual OleAutomation Dispatchable
// GUID:      {3A36C9B6-9D14-430B-B179-A9EA07A789B6}
// *********************************************************************//
  IComCall = interface(IDispatch)
    ['{3A36C9B6-9D14-430B-B179-A9EA07A789B6}']
    function Get_ID: WideString; safecall;
    function Get_SeanceID: WideString; safecall;
    function Get_Extension: WideString; safecall;
    function Get_Number: WideString; safecall;
    function Get_Name: WideString; safecall;
    function Get_DialedNumber: WideString; safecall;
    function Get_State: CallState; safecall;
    function Get_Direction: CallDirection; safecall;
    function Get_StartTime: TDateTime; safecall;
    function Get_LastStateTime: TDateTime; safecall;
    function Get_Duration: TimeSpan; safecall;
    function Get_AbonentCallInfoStr: WideString; safecall;
    function Get_AbonentCallInfoCollection: OleVariant; safecall;
    function Get_CanMake: WordBool; safecall;
    function Get_CanDrop: WordBool; safecall;
    function Get_CanAnswer: WordBool; safecall;
    function Get_CanHold: WordBool; safecall;
    function Get_CanUnHold: WordBool; safecall;
    function Get_CanQuickTransfer: WordBool; safecall;
    function Get_CanQuickConference: WordBool; safecall;
    function Get_CanStartTransfer: WordBool; safecall;
    function Get_CanFinishTransfer: WordBool; safecall;
    function Get_CanStartConference: WordBool; safecall;
    function Get_CanFinishConference: WordBool; safecall;
    function Get_CanSendDigits: WordBool; safecall;
    function Drop: WordBool; safecall;
    function Answer: WordBool; safecall;
    function Hold: WordBool; safecall;
    function UnHold: WordBool; safecall;
    function QuickTransfer(const number_: WideString; const callerName_: WideString): WordBool; safecall;
    function QuickConference(const number_: WideString; const callerName_: WideString): WordBool; safecall;
    function StartTransfer(const number_: WideString; const callerName_: WideString): WordBool; safecall;
    function FinishTransfer: WordBool; safecall;
    function StartConference(const number_: WideString; const callerName_: WideString): WordBool; safecall;
    function FinishConference: WordBool; safecall;
    function SendDigits(const digits_: WideString): WordBool; safecall;
    function Get_ParentCallID: WideString; safecall;
    property ID: WideString read Get_ID;
    property SeanceID: WideString read Get_SeanceID;
    property Extension: WideString read Get_Extension;
    property Number: WideString read Get_Number;
    property Name: WideString read Get_Name;
    property DialedNumber: WideString read Get_DialedNumber;
    property State: CallState read Get_State;
    property Direction: CallDirection read Get_Direction;
    property StartTime: TDateTime read Get_StartTime;
    property LastStateTime: TDateTime read Get_LastStateTime;
    property Duration: TimeSpan read Get_Duration;
    property AbonentCallInfoStr: WideString read Get_AbonentCallInfoStr;
    property AbonentCallInfoCollection: OleVariant read Get_AbonentCallInfoCollection;
    property CanMake: WordBool read Get_CanMake;
    property CanDrop: WordBool read Get_CanDrop;
    property CanAnswer: WordBool read Get_CanAnswer;
    property CanHold: WordBool read Get_CanHold;
    property CanUnHold: WordBool read Get_CanUnHold;
    property CanQuickTransfer: WordBool read Get_CanQuickTransfer;
    property CanQuickConference: WordBool read Get_CanQuickConference;
    property CanStartTransfer: WordBool read Get_CanStartTransfer;
    property CanFinishTransfer: WordBool read Get_CanFinishTransfer;
    property CanStartConference: WordBool read Get_CanStartConference;
    property CanFinishConference: WordBool read Get_CanFinishConference;
    property CanSendDigits: WordBool read Get_CanSendDigits;
    property ParentCallID: WideString read Get_ParentCallID;
  end;

// *********************************************************************//
// DispIntf:  IComCallDisp
// Flags:     (4416) Dual OleAutomation Dispatchable
// GUID:      {3A36C9B6-9D14-430B-B179-A9EA07A789B6}
// *********************************************************************//
  IComCallDisp = dispinterface
    ['{3A36C9B6-9D14-430B-B179-A9EA07A789B6}']
    property ID: WideString readonly dispid 10000;
    property SeanceID: WideString readonly dispid 10001;
    property Extension: WideString readonly dispid 10002;
    property Number: WideString readonly dispid 10003;
    property Name: WideString readonly dispid 10004;
    property DialedNumber: WideString readonly dispid 10005;
    property State: CallState readonly dispid 10006;
    property Direction: CallDirection readonly dispid 10007;
    property StartTime: TDateTime readonly dispid 10008;
    property LastStateTime: TDateTime readonly dispid 10009;
    property Duration: {??TimeSpan}OleVariant readonly dispid 10010;
    property AbonentCallInfoStr: WideString readonly dispid 10011;
    property AbonentCallInfoCollection: OleVariant readonly dispid 10012;
    property CanMake: WordBool readonly dispid 10013;
    property CanDrop: WordBool readonly dispid 10014;
    property CanAnswer: WordBool readonly dispid 10015;
    property CanHold: WordBool readonly dispid 10016;
    property CanUnHold: WordBool readonly dispid 10017;
    property CanQuickTransfer: WordBool readonly dispid 10018;
    property CanQuickConference: WordBool readonly dispid 10019;
    property CanStartTransfer: WordBool readonly dispid 10020;
    property CanFinishTransfer: WordBool readonly dispid 10021;
    property CanStartConference: WordBool readonly dispid 10022;
    property CanFinishConference: WordBool readonly dispid 10023;
    property CanSendDigits: WordBool readonly dispid 10024;
    function Drop: WordBool; dispid 10025;
    function Answer: WordBool; dispid 10026;
    function Hold: WordBool; dispid 10027;
    function UnHold: WordBool; dispid 10028;
    function QuickTransfer(const number_: WideString; const callerName_: WideString): WordBool; dispid 10029;
    function QuickConference(const number_: WideString; const callerName_: WideString): WordBool; dispid 10030;
    function StartTransfer(const number_: WideString; const callerName_: WideString): WordBool; dispid 10031;
    function FinishTransfer: WordBool; dispid 10032;
    function StartConference(const number_: WideString; const callerName_: WideString): WordBool; dispid 10033;
    function FinishConference: WordBool; dispid 10034;
    function SendDigits(const digits_: WideString): WordBool; dispid 10035;
    property ParentCallID: WideString readonly dispid 10036;
  end;

// *********************************************************************//
// Interface: IConnection
// Flags:     (4416) Dual OleAutomation Dispatchable
// GUID:      {78BA85D8-415B-4431-BF0F-E6377E58AE00}
// *********************************************************************//
  IConnection = interface(IDispatch)
    ['{78BA85D8-415B-4431-BF0F-E6377E58AE00}']
    function Get_ID: Int64; safecall;
    function Get_TimeStart: TDateTime; safecall;
    function Get_DurationTalk: TimeSpan; safecall;
    function Get_State: ConnectionState; safecall;
    function Get_ANumber: WideString; safecall;
    function Get_BNumber: WideString; safecall;
    function Get_ADisplayText: WideString; safecall;
    function Get_BDisplayText: WideString; safecall;
    function Get_IsRecorded: WordBool; safecall;
    function Get_ID_AsVariant: OleVariant; safecall;
    procedure SaveRecordedFileToStream(const stream_: _Stream); safecall;
    procedure SaveRecordedFile(const fileName_: WideString); safecall;
    procedure PlayRecordedFile; safecall;
    procedure StartRecord; safecall;
    procedure StopRecord; safecall;
    property ID: Int64 read Get_ID;
    property TimeStart: TDateTime read Get_TimeStart;
    property DurationTalk: TimeSpan read Get_DurationTalk;
    property State: ConnectionState read Get_State;
    property ANumber: WideString read Get_ANumber;
    property BNumber: WideString read Get_BNumber;
    property ADisplayText: WideString read Get_ADisplayText;
    property BDisplayText: WideString read Get_BDisplayText;
    property IsRecorded: WordBool read Get_IsRecorded;
    property ID_AsVariant: OleVariant read Get_ID_AsVariant;
  end;

// *********************************************************************//
// DispIntf:  IConnectionDisp
// Flags:     (4416) Dual OleAutomation Dispatchable
// GUID:      {78BA85D8-415B-4431-BF0F-E6377E58AE00}
// *********************************************************************//
  IConnectionDisp = dispinterface
    ['{78BA85D8-415B-4431-BF0F-E6377E58AE00}']
    property ID: {??Int64}OleVariant readonly dispid 10000;
    property TimeStart: TDateTime readonly dispid 10001;
    property DurationTalk: {??TimeSpan}OleVariant readonly dispid 10002;
    property State: ConnectionState readonly dispid 10003;
    property ANumber: WideString readonly dispid 10004;
    property BNumber: WideString readonly dispid 10005;
    property ADisplayText: WideString readonly dispid 10006;
    property BDisplayText: WideString readonly dispid 10007;
    property IsRecorded: WordBool readonly dispid 10008;
    property ID_AsVariant: OleVariant readonly dispid 10014;
    procedure SaveRecordedFileToStream(const stream_: _Stream); dispid 10009;
    procedure SaveRecordedFile(const fileName_: WideString); dispid 10010;
    procedure PlayRecordedFile; dispid 10011;
    procedure StartRecord; dispid 10012;
    procedure StopRecord; dispid 10013;
  end;

implementation

uses ComObj;

end.

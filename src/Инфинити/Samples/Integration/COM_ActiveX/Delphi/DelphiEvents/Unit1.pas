unit Unit1;

interface

uses
  Windows, Messages, SysUtils, Variants, Classes, Graphics, Controls, Forms,
  Dialogs, ExtCtrls, StdCtrls, OleCtnrs, ActiveX, OleCtrls, ComObj, EventsUtils,
  Cx_Client_ThirdPartyIntegration_TLB, Cx_Integration_AgatInfinityConnectorInterfaces_TLB, AxCtrls;

type
  POleVariant = ^OleVariant;

  // 1-ый вариант
  TEventsHandler_UM = class(TEventsHandler)
  protected
    procedure GetEventMethod(DispID: TDispID; var Method: TMethod); override;
  private
    //procedure StateChanged(Sender: TObject; const User: IComUser; oldState: OleVariant; State: OleVariant);
    procedure StateChanged(Sender: TObject; const User: IComUser; oldState: Int64; State: Int64);
  end;

  // 2-ой вариант
  TEventsHandler_UM_2 = class(TEventsHandler)
  protected
    function DoInvoke(DispID: Integer; const IID: TGUID; LocaleID: Integer;
      Flags: Word; var Params; VarResult, ExcepInfo, ArgErr: Pointer): HResult; { stdcall;}override;
  private
    procedure StateChanged(const User: IComUser; oldState: OleVariant; State: OleVariant);
  end;


  // 3-ий вариант - use .tlb

  IUsersManagementEvents = interface(IDispatch)
    ['{A1C9ED36-0DA9-4868-AEC6-6A575A91C741}']
    procedure StateChanged(const User: IComUser; oldState: OleVariant; State: OleVariant); safecall;
  end;

  TEventsHandler_UM_3 = class(TDispatchEventsImpl, IUsersManagementEvents)
  private
    procedure StateChanged(const User: IComUser; oldState: OleVariant; State: OleVariant); safecall;
  end;


  TForm1 = class(TForm)
    Panel1: TPanel;
    Button1: TButton;
    Button2: TButton;
    Button5: TButton;
    Button6: TButton;
    Button3: TButton;
    procedure Button1Click(Sender: TObject);
    procedure Button2Click(Sender: TObject);
    procedure Button5Click(Sender: TObject);
    procedure Button6Click(Sender: TObject);
    procedure Button3Click(Sender: TObject);
  private
    { Private declarations }
    m_Core: IUnknown;
    vCore: OleVariant;
    UsersMngm: OleVariant;
    m_UM_EventsCookie: Longint;
    m_UM_EventsCookie2: Longint;
    m_UM_EventsCookie3: Longint;
    m_UM_EventsHandler: TEventsHandler_UM_3;
  public
    { Public declarations }
    destructor Destroy; override;

  end;

var
  Form1: TForm1;

implementation

{$R *.dfm}


destructor TForm1.Destroy;
begin
   UsersMngm := null;
   vCore := null;
   m_Core := nil;

  inherited;
end;


procedure TForm1.Button1Click(Sender: TObject);
begin
   if ( not Assigned(m_Core) ) then
   begin
      m_Core := CreateComObject ( StringToGUID('{182DE9A3-3FCE-4FA2-8408-72D03EE8F5F7}') );
      vCore := m_Core as IDispatch;
   end;
end;

procedure TForm1.Button2Click(Sender: TObject);
begin
   vCore := null;
   m_Core := nil;
end;

procedure TForm1.Button5Click(Sender: TObject);
var
   bRes: boolean;
   Sink: IDispatch;
begin
   bRes := vCore.Logon;
   if ( bRes ) then
   begin
      // подписка на события IComUsersManagement, подписка на события IComCallManagement делается аналогично


      UsersMngm := vCore.GetUsersManagement;

      if VarType(UsersMngm) = varDispatch then
      begin

         // 1-ый вариант
         Sink := TEventsHandler_UM.Create ( DIID_IUsersManagementEvents ) as IDispatch;
         InterfaceConnect ( UsersMngm ,
                      DIID_IUsersManagementEvents ,
                      Sink ,
                      m_UM_EventsCookie);

         // 2-ой вариант
         Sink := TEventsHandler_UM_2.Create ( DIID_IUsersManagementEvents ) as IDispatch;
         InterfaceConnect ( UsersMngm ,
                      DIID_IUsersManagementEvents ,
                      Sink ,
                      m_UM_EventsCookie2);

         // 3-ий вариант - use .tlb
         if not Assigned(m_UM_EventsHandler) then
         begin
            m_UM_EventsHandler := TEventsHandler_UM_3.Create ( IUsersManagementEvents , LIBID_Cx_Client_ThirdPartyIntegration , Cx_Client_ThirdPartyIntegrationMajorVersion , Cx_Client_ThirdPartyIntegrationMinorVersion );
         end;
         if Assigned(m_UM_EventsHandler) then
         begin
            InterfaceConnect ( UsersMngm ,
                      DIID_IUsersManagementEvents ,
                      m_UM_EventsHandler ,
                      m_UM_EventsCookie3);
         end;
      end;
   end;
end;

procedure TForm1.Button6Click(Sender: TObject);
begin
   try

      if VarType(UsersMngm) = varDispatch then
      begin
         InterfaceDisconnect ( UsersMngm ,
                               DIID_IUsersManagementEvents ,
                               m_UM_EventsCookie);
         InterfaceDisconnect ( UsersMngm ,
                               DIID_IUsersManagementEvents ,
                               m_UM_EventsCookie2);

         if Assigned(m_UM_EventsHandler) then
         begin
            InterfaceConnect ( UsersMngm ,
                      DIID_IUsersManagementEvents ,
                      m_UM_EventsHandler ,
                      m_UM_EventsCookie3);
         end;

      end;

   finally
      UsersMngm := null;
      vCore.Close;
   end;
end;


//procedure TEventsHandler_UM.StateChanged(Sender: TObject; const User: IComUser; oldState: OleVariant; State: OleVariant);
procedure TEventsHandler_UM.StateChanged(Sender: TObject; const User: IComUser; oldState: Int64; State: Int64);
var
   ID: Int64;
   UserDisp: IComUserDisp;
begin
   UserDisp := IComUserDisp(IDispatch(User));  // используем Disp-интерфейс (позднее связывание)
                                               // что бы не зависеть от порядка методов и свойств в случае их изменения в будущем

   ID := UserDisp.ID;

   ShowMessage(Format('StateChanged %d: %d -> %d', [ID, oldState, State]));
end;


procedure TEventsHandler_UM.GetEventMethod(DispID: TDispID; var Method: TMethod);
begin
   Method.Data := Self;

   case DispID of
   1001: Method.Code := @TEventsHandler_UM.StateChanged;
   else
      Method.Code := nil;
   end;
end;


function TEventsHandler_UM_2.DoInvoke(DispID: Integer; const IID: TGUID; LocaleID: Integer;
      Flags: Word; var Params; VarResult, ExcepInfo, ArgErr: Pointer): HResult;
begin
   // POleVariant(@rgvarg^[])^  : преобразование tagVARIANT в OleVariant

   Result := S_OK;
   
   try
      case DispID of
      1001:
         with TDispParams(Params) do
         begin
            if cArgs = 3 then
               StateChanged (
                  IUnknown( POleVariant(@rgvarg^[2])^ ) as IComUser,
                  POleVariant(@rgvarg^[1])^ ,
                  POleVariant(@rgvarg^[0])^ );
         end;
      else
         Result := DISP_E_MEMBERNOTFOUND;
      end;

   except
      Application.HandleException(Self);
   end;
end;

procedure TEventsHandler_UM_2.StateChanged(const User: IComUser; oldState: OleVariant; State: OleVariant);
var
   st1, st2, ID: Int64;
   UserDisp: IComUserDisp;
begin
   UserDisp := IComUserDisp(IDispatch(User));  // используем Disp-интерфейс (позднее связывание)
                                               // что бы не зависеть от порядка методов и свойств в случае их изменения в будущем

   ID := UserDisp.ID;
   st1 := oldState;
   st2 := State;

   ShowMessage(Format('StateChanged (2) %d: %d -> %d', [ID, st1, st2]));
end;


{ TEventsHandler_UM_3 }

procedure TEventsHandler_UM_3.StateChanged(const User: IComUser; oldState,
  State: OleVariant);
var
   st1, st2, ID: Int64;
   UserDisp: IComUserDisp;
begin
   UserDisp := IComUserDisp(IDispatch(User));  // используем Disp-интерфейс (позднее связывание)
                                               // что бы не зависеть от порядка методов и свойств в случае их изменения в будущем

   ID := UserDisp.ID;
   st1 := oldState;
   st2 := State;

   ShowMessage(Format('StateChanged (3) %d: %d -> %d', [ID, st1, st2]));
end;


// Работа с коллекциями
procedure TForm1.Button3Click(Sender: TObject);
var
   CurrentUser: IComUserDisp;
   Extensions: OleVariant;
   vExt: OleVariant;
   I, Cnt: Integer;
   oEnum: IEnumVariant;
   iFetched: LongWord;
   Result: OleVariant;
   DispParamsEmpty: DISPPARAMS;
begin
   if VarType(UsersMngm) <> varDispatch then
   begin
      ShowMessage ( 'VarType(UsersMngm) <> varDispatch' );
      Exit;
   end;

   CurrentUser := IComUserDisp(IDispatch(UsersMngm.CurrentUser));

   if CurrentUser = nil then
   begin
      ShowMessage ( 'CurrentUser = nil' );
      Exit;
   end;

   Extensions := CurrentUser.Extensions;
   if VarType(Extensions) <> varDispatch then
   begin
      ShowMessage ( 'VarType(Extensions) <> varDispatch' );
      Exit;
   end;


   // 1-ый способ

   Cnt := Extensions.Count;
   ShowMessage ( Format ( 'Count: %d' , [Cnt] ) );

   for i := 0 to Cnt - 1 do
   begin
      vExt := Extensions.Item(i);
      ShowMessage ( String(vExt) );
   end;


   // 2-ой способ

   ShowMessage ( 'IEnumVariant' );

   //oEnum := IUnknown(Extensions._NewEnum) as IEnumVariant;

   FillChar (DispParamsEmpty, sizeof (DispParamsEmpty), 0);

   OleCheck( IDispatch(Extensions).Invoke (
        DISPID_NEWENUM, GUID_NULL, LOCALE_SYSTEM_DEFAULT,
        DISPATCH_PROPERTYGET, DispParamsEmpty, @Result, nil, nil) );

   oEnum := IEnumVariant(IUnknown(Result));

   while oEnum.Next(1, vExt, iFetched) = 0 do
   begin
      ShowMessage ( String(vExt) );
   end;


end;

end.


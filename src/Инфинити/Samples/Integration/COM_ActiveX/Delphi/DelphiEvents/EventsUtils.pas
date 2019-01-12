unit EventsUtils;

interface

uses
  Windows, Messages, SysUtils, Variants, Classes, Forms,
  Dialogs, ActiveX, ComObj, Contnrs;

type
  TEventsHandler = class(TInterfacedObject, IDispatch, ISupportErrorInfo, IInterface)
  public
    constructor Create(const DispIntf: TGUID);
  protected
    { IInterface }
    function QueryInterface(const IID: TGUID; out Obj): HResult; stdcall;
    { IDispatch }
    function GetIDsOfNames(const IID: TGUID; Names: Pointer;
      NameCount, LocaleID: Integer; DispIDs: Pointer): HResult; stdcall;
    function GetTypeInfo(Index, LocaleID: Integer; out TypeInfo): HResult; stdcall;
    function GetTypeInfoCount(out Count: Integer): HResult; stdcall;
    function Invoke(DispID: Integer; const IID: TGUID; LocaleID: Integer;
      Flags: Word; var Params; VarResult, ExcepInfo, ArgErr: Pointer): HResult; stdcall;
    { ISupportErrorInfo }
    function InterfaceSupportsErrorInfo(const iid: TIID): HResult; stdcall;
  protected
    function InvokeEvent(DispID: TDispID; var Params: TDispParams): Boolean;

    procedure GetEventMethod(DispID: TDispID; var Method: TMethod); virtual;

    function DoInvoke(DispID: Integer; const IID: TGUID; LocaleID: Integer;
      Flags: Word; var Params; VarResult, ExcepInfo, ArgErr: Pointer): HResult; { stdcall;}virtual;
  private
    m_DispIntf: TGUID;
  public
{$IFDEF MSWINDOWS}
    function SafeCallException(ExceptObject: TObject;
      ExceptAddr: Pointer): HResult; override;
{$ENDIF}
  end;


type
  TIBucketList = class(TBucketList)
  protected
    function BucketFor(AItem: Pointer): Integer; override;
  end;

  TDispatchEventsImpl = class(TInterfacedObject, IDispatch, ISupportErrorInfo)
  public
    constructor Create(const DispIntf: TGUID; const LibGuid: TGUID; wVerMajor, wVerMinor: Word); overload;
    constructor Create(const DispIntf: TGUID; const TypeLib: ITypeLib); overload;
    destructor Destroy; override;
  protected
    { IDispatch }
    function GetIDsOfNames(const IID: TGUID; Names: Pointer;
      NameCount, LocaleID: Integer; DispIDs: Pointer): HResult; stdcall;
    function GetTypeInfo(Index, LocaleID: Integer; out TypeInfo): HResult; stdcall;
    function GetTypeInfoCount(out Count: Integer): HResult; stdcall;
    function Invoke(DispID: Integer; const IID: TGUID; LocaleID: Integer;
      Flags: Word; var Params; VarResult, ExcepInfo, ArgErr: Pointer): HResult; stdcall;
    { ISupportErrorInfo }
    function InterfaceSupportsErrorInfo(const iid: TIID): HResult; stdcall;
  private
    procedure InitTypeInfo;
  private
    //m_StdDisp: IDispatch;
    m_TypeInfo: ITypeInfo;
    m_Impl: Pointer;
    m_FuncsInfo: TIBucketList;
  public
    //property DispIntf: IDispatch read m_StdDisp;
  public
{$IFDEF MSWINDOWS}
    function SafeCallException(ExceptObject: TObject;
      ExceptAddr: Pointer): HResult; override;
{$ENDIF}
  end;

implementation


{ TEventsHandler }

constructor TEventsHandler.Create(const DispIntf: TGUID);
begin
   inherited Create;

   m_DispIntf := DispIntf;
end;

function TEventsHandler.QueryInterface(const IID: TGUID; out Obj): HResult; stdcall;
begin
   if ( IsEqualGUID ( m_DispIntf , IID ) ) then
   begin
      IDispatch(Obj) := Self;
      Result := S_OK;
   end
   else
      Result := inherited QueryInterface(IID, Obj);
end;

function TEventsHandler.GetIDsOfNames(const IID: TGUID; Names: Pointer;
  NameCount, LocaleID: Integer; DispIDs: Pointer): HResult;
begin
   Result := E_NOTIMPL;
end;

function TEventsHandler.GetTypeInfo(Index, LocaleID: Integer;
  out TypeInfo): HResult;
begin
   Result := E_NOTIMPL;
end;

function TEventsHandler.GetTypeInfoCount(out Count: Integer): HResult;
begin
   Count := 0;
   Result := S_OK;
end;

function TEventsHandler.InterfaceSupportsErrorInfo(
  const iid: TIID): HResult;
begin
   Result := S_FALSE;
end;

function TEventsHandler.SafeCallException(ExceptObject: TObject;
  ExceptAddr: Pointer): HResult;
begin
  Result := HandleSafeCallException(ExceptObject, ExceptAddr, GUID_NULL, '', '');
end;

procedure TEventsHandler.GetEventMethod(DispID: TDispID; var Method: TMethod);
begin
   Method.Code := nil;
   Method.Data := nil;
end;



{ from TOleControl }
function TEventsHandler.InvokeEvent(DispID: TDispID; var Params: TDispParams): Boolean;
var
  EventMethod: TMethod;
//  i: Integer;
begin
    Result := False;
    EventMethod.Code := nil;
    EventMethod.Data := nil;

    GetEventMethod(DispID, EventMethod);
    if Integer(EventMethod.Code) < $10000 then Exit;

{
    LShowMessage('Args %d', [Params.cArgs]);
    for i := 0 to Params.cArgs - 1 do
       LShowMessage('Args[%d] vt: %d', [i, Params.rgvarg^[i].vt]);
 }

    try
      asm
                PUSH    EBX
                PUSH    ESI
                MOV     ESI, Params
                MOV     EBX, [ESI].TDispParams.cArgs
                TEST    EBX, EBX
                JZ      @@7
                MOV     ESI, [ESI].TDispParams.rgvarg
                MOV     EAX, EBX
                SHL     EAX, 4                         // count * sizeof(TVarArg)
                XOR     EDX, EDX
                ADD     ESI, EAX                       // EDI = Params.rgvarg^[ArgCount]
        @@1:    SUB     ESI, 16                        // Sizeof(TVarArg)
                MOV     EAX, dword ptr [ESI]
                CMP     AX, varSingle                  // 4 bytes to push
                JA      @@3
                JE      @@5
        @@2:    TEST    DL,DL
                JNE     @@2a
                MOV     ECX, ESI
                INC     DL
                TEST    EAX, varArray
                JNZ     @@6
                MOV     ECX, dword ptr [ESI+8]
                JMP     @@6
        @@2a:   TEST    EAX, varArray
                JZ      @@5
                PUSH    ESI
                JMP     @@6

//LCK --> // Int64 support

//        @@3:    CMP     AX, varDate                    // 8 bytes to push
//                JA      @@2

        @@3:    CMP     AX, varDate                    // 8 bytes to push
                JBE     @@4
                CMP     AX, varInt64
                JB      @@2
                JE      @@4
                CMP     AX, varInt64+1
                JNE     @@2
//LCK <--

        @@4:    PUSH    dword ptr [ESI+12]
        @@5:    PUSH    dword ptr [ESI+8]
        @@6:    DEC     EBX
                JNE     @@1
        @@7:    MOV     EDX, Self
                MOV     EAX, EventMethod.Data
                CALL    EventMethod.Code
                POP     ESI
                POP     EBX
      end;
    except
      Application.HandleException(Self);
    end;
    Result := True;
end;


function TEventsHandler.Invoke(DispID: Integer; const IID: TGUID; LocaleID: Integer;
      Flags: Word; var Params; VarResult, ExcepInfo, ArgErr: Pointer): HResult; stdcall;
begin
   Result := DoInvoke ( DispID, IID, LocaleID, Flags, Params, VarResult, ExcepInfo, ArgErr );
end;

function TEventsHandler.DoInvoke(DispID: Integer; const IID: TGUID; LocaleID: Integer;
      Flags: Word; var Params; VarResult, ExcepInfo, ArgErr: Pointer): HResult;
begin
   if InvokeEvent(DispID, TDispParams(Params)) then
      Result := S_OK
   else
      Result := DISP_E_MEMBERNOTFOUND;
end;



type
   TFuncInfo = record
      oVft: Longint;
      cc: TCallConv;
      vtReturn: TVarType;
      vtArgs: array of TVarType;
   end;
   PFuncInfo = ^TFuncInfo;


{ TDispatchEventsImpl }

(*
constructor TDispatchEventsImpl.Create(const DispIntf: TGUID; const LibGuid: TGUID; wVerMajor, wVerMinor: Word);
var
   //unkStdDisp: IUnknown;
   tlib: ITypeLib;
   //m_TypeInfo: ITypeInfo;
begin
   inherited Create;

   if not Supports ( Self , DispIntf , m_Impl ) then
      raise Exception.Create( 'Interface not implemented on this object: ' + GUIDToString ( DispIntf ) );

   // Этот вариант работает только если в .tlb определён dual-интерфейс для событий, а не только DispInterface.
   // Однако в этом случае Excel не может использовать события
   // Если из .tlb взять DispInterface, то Invoke не работает, т.к. для DispInterface не определено размещение в памяти,
   // т.е. не известно наследуется ли реализация от IUnknown или IDispatch

   OleCheck(LoadRegTypeLib(LibGuid, wVerMajor, wVerMinor, LOCALE_SYSTEM_DEFAULT, tlib));

   // Вместо m_StdDisp используем ITypeLib - так проще, но это ничего не меняет в том, что в итоге этот вариант не работает
{
   OleCheck(tlib.GetTypeInfoOfGuid ( DispIntf , m_TypeInfo ));

   OleCheck(CreateStdDispatch(
      IUnknown(m_Impl), // Controlling unknown.
      m_Impl,           // Instance to dispatch on.
      tinfo,            // Type information describing the instance.
      unkStdDisp));

   m_StdDisp := unkStdDisp as IDispatch;
}
//   ShowMessage( IntToHex ( DWORD(m_StdDisp) , 8 ) );
end;
*)

constructor TDispatchEventsImpl.Create(const DispIntf: TGUID; const LibGuid: TGUID; wVerMajor, wVerMinor: Word);
var
   tlib: ITypeLib;
begin
   inherited Create;

   if not Supports ( Self , DispIntf , m_Impl ) then
      raise Exception.Create( 'Interface not implemented on this object: ' + GUIDToString ( DispIntf ) );

   OleCheck(LoadRegTypeLib(LibGuid, wVerMajor, wVerMinor, LOCALE_SYSTEM_DEFAULT, tlib));

   OleCheck(tlib.GetTypeInfoOfGuid ( DispIntf , m_TypeInfo ));

   InitTypeInfo();
end;

constructor TDispatchEventsImpl.Create(const DispIntf: TGUID; const TypeLib: ITypeLib);
begin
   inherited Create;

   if not Supports ( Self , DispIntf , m_Impl ) then
      raise Exception.Create( 'Interface not implemented on this object: ' + GUIDToString ( DispIntf ) );

   OleCheck(TypeLib.GetTypeInfoOfGuid ( DispIntf , m_TypeInfo ));

   InitTypeInfo();
end;

procedure BucketProc (AInfo, AItem, AData: Pointer; out AContinue: Boolean);
begin
   Dispose ( PFuncInfo(AData) );
end;

destructor TDispatchEventsImpl.Destroy;
begin
   if m_FuncsInfo <> nil then
      m_FuncsInfo.ForEach ( BucketProc );

   FreeAndNil ( m_FuncsInfo );

   inherited;
end;

function GetUserDefinedType(const pTI: ITypeInfo; hrt: HREFTYPE): TVARTYPE;
var
	 spTypeInfo: ITypeInfo;
   pta: PTYPEATTR;
begin
	 Result := VT_USERDEFINED;
	 if FAILED ( pTI.GetRefTypeInfo(hrt, spTypeInfo) ) then
      Exit;

   pta := nil;
   try
      if SUCCEEDED ( spTypeInfo.GetTypeAttr(pta) ) and
         ( pta <> nil ) and ( (pta.typekind = TKIND_ALIAS) or (pta.typekind = TKIND_ENUM) ) then
      begin
         if (pta.tdescAlias.vt = VT_USERDEFINED) then
            Result := GetUserDefinedType(spTypeInfo, pta.tdescAlias.hreftype)
         else
         begin
            case (pta.typekind) of
               TKIND_ENUM:      Result := VT_I4;
               TKIND_INTERFACE: Result := VT_UNKNOWN;
               TKIND_DISPATCH:  Result := VT_DISPATCH;
               else
                  Result := pta.tdescAlias.vt;
            end;
         end;
      end;
   finally
     if ( pta <> nil ) then
        spTypeInfo.ReleaseTypeAttr(pta);
   end;
end;

procedure TDispatchEventsImpl.InitTypeInfo;
var
	 pAttr: PTYPEATTR;
	 pFuncDesc: ActiveX.PFUNCDESC;
   cFuncs, i, j, oVft: Integer;
   pFuncInfo: ^TFuncInfo;
   vtReturn: TVARTYPE;
begin
   OleCheck(m_TypeInfo.GetTypeAttr(pAttr));
   cFuncs := pAttr^.cFuncs;
   m_TypeInfo.ReleaseTypeAttr(pAttr);

   try
      m_FuncsInfo := TIBucketList.Create();

      oVft := Sizeof(Pointer) * ( 3 + 4 );

      for i := 0 to cFuncs-1 do
      begin
         OleCheck(m_TypeInfo.GetFuncDesc(i, pFuncDesc));
         try
            New ( pFuncInfo );
            try
               SetLength ( pFuncInfo.vtArgs , pFuncDesc.cParams );

               for j := 0 to pFuncDesc.cParams-1 do
               begin
                  pFuncInfo.vtArgs[j] := pFuncDesc.lprgelemdescParam[j].tdesc.vt;
                  if (pFuncInfo.vtArgs[j] = VT_PTR) then
                     pFuncInfo.vtArgs[j] := VARTYPE(pFuncDesc.lprgelemdescParam[i].tdesc.ptdesc.vt or VT_BYREF)
                  else if (pFuncInfo.vtArgs[j] = VT_SAFEARRAY) then
                     pFuncInfo.vtArgs[j] := VARTYPE(pFuncDesc.lprgelemdescParam[i].tdesc.ptdesc.vt or VT_ARRAY)
                  else if (pFuncInfo.vtArgs[j] = VT_USERDEFINED) then
                     pFuncInfo.vtArgs[j] := GetUserDefinedType(m_TypeInfo, pFuncDesc.lprgelemdescParam[i].tdesc.hreftype);
               end;

               vtReturn := pFuncDesc.elemdescFunc.tdesc.vt;
               case (vtReturn) of
                  VT_INT: vtReturn := VT_I4;
                  VT_UINT: vtReturn := VT_UI4;
                  VT_VOID: vtReturn := VT_EMPTY; // this is how DispCallFunc() represents void
                  VT_HRESULT: vtReturn := VT_ERROR;
               end;
               pFuncInfo.vtReturn := vtReturn;

               pFuncInfo.cc := pFuncDesc.callconv;

               pFuncInfo.oVft := oVft;
               Inc ( oVft , Sizeof(Pointer) );

               m_FuncsInfo.Add ( Pointer(pFuncDesc.memid) , pFuncInfo );

               pFuncInfo := nil;
            finally
               if ( pFuncInfo <> nil ) then
                  Dispose ( pFuncInfo );
            end;
         finally
            m_TypeInfo.ReleaseFuncDesc(pFuncDesc);
         end;
      end;

   except
      FreeAndNil ( m_FuncsInfo );
      raise;
   end;

end;


{
function TDispatchEventsImpl.QueryInterface(const IID: TGUID; out Obj): HResult; stdcall;
begin
   if ( IsEqualGUID ( m_DispIntf , IID ) ) then
   begin
      IUnknown(Obj) := m_StdDisp;
      Result := S_OK;
   end
   else
      Result := inherited QueryInterface(IID, Obj);
end;
}

function TDispatchEventsImpl.GetIDsOfNames(const IID: TGUID; Names: Pointer;
  NameCount, LocaleID: Integer; DispIDs: Pointer): HResult;
begin
//   Result := m_StdDisp.GetIDsOfNames ( IID , Names , NameCount , LocaleID , DispIDs );
   Result := m_TypeInfo.GetIDsOfNames ( POleStrList(Names) , NameCount , PMemberIDList(DispIDs) );
end;

function TDispatchEventsImpl.GetTypeInfo(Index, LocaleID: Integer;
  out TypeInfo): HResult;
begin
   //Result := m_StdDisp.GetTypeInfo ( Index , LocaleID , TypeInfo );

   if ( Index = 0 ) then
   begin
      ITypeInfo(TypeInfo) := m_TypeInfo;
      Result := S_OK;
   end
   else
      Result := E_INVALIDARG;
end;

function TDispatchEventsImpl.GetTypeInfoCount(out Count: Integer): HResult;
begin
   //Result := m_StdDisp.GetTypeInfoCount ( Count );

   Count := 1;
   Result := S_OK;
end;

function TDispatchEventsImpl.Invoke(DispID: Integer; const IID: TGUID; LocaleID: Integer;
      Flags: Word; var Params; VarResult, ExcepInfo, ArgErr: Pointer): HResult; stdcall;
type
   POleVariant = ^OleVariant;
   PVariantArg = ^TVariantArg;
   PPVariantArg = ^PVariantArg;
var
   pFuncInfo: ^TFuncInfo;
   rgvt: ^TVarType;
   i, nIndex: Integer;
   prgpvararg: PPVariantArg;
   pvararg: array[0..15] of PVariantArg;
   parg: PPVariantArg;
   TempResult: TVariantArg;
   pResult: POleVariant;
begin
{
   //Result := m_StdDisp.Invoke ( DispID, IID, LocaleID, Flags, Params, VarResult, ExcepInfo, ArgErr );
   Result := m_TypeInfo.Invoke ( Self , DispID, Flags, tagDISPPARAMS(Params), VarResult, ExcepInfo, ArgErr );
}

   if ( not m_FuncsInfo.Find ( Pointer(DispID) , Pointer(pFuncInfo) ) ) then
   begin
      Result := DISP_E_MEMBERNOTFOUND;
      Exit;
   end;

//ShowMessage('Invoke');

   pResult := nil;
   prgpvararg := @pvararg;

   try

      with pFuncInfo^ do
      begin
         rgvt := nil;
         if Length(vtArgs) > 0 then
            rgvt := @vtArgs[0];

         if ( tagDISPPARAMS(Params).cArgs > Length(pvararg) ) then
            prgpvararg := AllocMem ( tagDISPPARAMS(Params).cArgs * sizeof(Pointer) );

{
         parg := prgpvararg;
         for i := 0 to tagDISPPARAMS(Params).cArgs - 1 do
         begin
            parg^ := @tagDISPPARAMS(Params).rgvarg[tagDISPPARAMS(Params).cArgs - i - 1];
            Inc ( parg );
         end;
}

         if tagDISPPARAMS(Params).cNamedArgs > 0 then
            ZeroMemory ( prgpvararg , tagDISPPARAMS(Params).cArgs * sizeof(Pointer) );

		     nIndex := 0;

		     while nIndex < tagDISPPARAMS(Params).cNamedArgs do
         begin
            if tagDISPPARAMS(Params).rgdispidNamedArgs[nIndex] >= tagDISPPARAMS(Params).cArgs then
            begin
               Result := E_FAIL;
               Exit;
            end;

            parg := prgpvararg;
            Inc ( parg , tagDISPPARAMS(Params).rgdispidNamedArgs[nIndex] );
            parg^ := @tagDISPPARAMS(Params).rgvarg[nIndex];

            Inc ( nIndex );
         end;

         parg := prgpvararg;
         for i := 0 to tagDISPPARAMS(Params).cArgs - nIndex - 1 do
         begin
            parg^ := @tagDISPPARAMS(Params).rgvarg[tagDISPPARAMS(Params).cArgs - i - 1];
            Inc ( parg );
         end;


         pResult := POleVariant(VarResult);
         if ( pResult = nil ) then
         begin
            pResult := POleVariant(@TempResult);
            TempResult.vt := VT_EMPTY;
         end;

         Result := DispCallFunc ( m_Impl , oVft , cc , vtReturn , tagDISPPARAMS(Params).cArgs , rgvt^ ,
              POleVariant(prgpvararg)^ ,
              pResult^ );

      end;

   finally
      if pResult = @TempResult then
         VarClear ( POleVariant(@TempResult)^ );
      if prgpvararg <> @pvararg then
         FreeMemory ( prgpvararg );
   end;
end;

function TDispatchEventsImpl.InterfaceSupportsErrorInfo(
  const iid: TIID): HResult;
begin
   Result := S_FALSE;
end;

function TDispatchEventsImpl.SafeCallException(ExceptObject: TObject;
  ExceptAddr: Pointer): HResult;
begin
  Result := HandleSafeCallException(ExceptObject, ExceptAddr, GUID_NULL, '', '');
end;


{ TIBucketList }

function TIBucketList.BucketFor(AItem: Pointer): Integer;
begin
   Result := Integer(AItem);
end;

end.

program DelphiEvents;

uses
  Forms,
  Unit1 in 'Unit1.pas' {Form1},
  Cx_Client_ThirdPartyIntegration_TLB in 'Cx_Client_ThirdPartyIntegration_TLB.pas',
  EventsUtils in 'EventsUtils.pas',
  Cx_Integration_AgatInfinityConnectorInterfaces_TLB in '..\1\Cx_Integration_AgatInfinityConnectorInterfaces_TLB.pas';

{$R *.res}

begin
  Application.Initialize;
  Application.CreateForm(TForm1, Form1);
  Application.Run;
end.

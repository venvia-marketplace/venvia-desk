; VenVia Desk - graphical diagnostic preview
[Setup]
AppId=VenVia.Desk.DiagnosticPreview
AppName=VenVia Desk - Anteprima diagnostica
AppVersion=0.4.0-alpha.3
DefaultDirName={localappdata}\VenVia\DeskPreview
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
Uninstallable=no
CreateAppDir=no
DisableWelcomePage=no
SetupLogging=yes
OutputDir=output
OutputBaseFilename=VenVia-Desk-Setup-0.4.0-alpha.3-preview
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
[Languages]
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
[Code]
var
  ReportPage: TOutputMsgMemoWizardPage;
  Scanned: Boolean;
procedure InitializeWizard;
begin
  Scanned := False;
  ReportPage := CreateOutputMsgMemoPage(wpWelcome, 'Diagnostica del computer',
    'VenVia Desk 0.4 - Windows', 'Risultati preliminari (sola lettura):',
    'Premere Avanti per avviare la diagnostica.');
end;
procedure CurPageChanged(CurPageID: Integer);
var
  Version: TWindowsVersion;
  FreeMB, TotalMB: Cardinal;
  Report: String;
begin
  if (CurPageID <> ReportPage.ID) or Scanned then Exit;
  Scanned := True;
  try
    GetWindowsVersionEx(Version);
    Report := 'Windows: ' + IntToStr(Version.Major) + '.' +
      IntToStr(Version.Minor) + ' build ' + IntToStr(Version.Build) + #13#10;
    if IsWin64 then Report := Report + 'Windows a 64 bit' + #13#10
    else Report := Report + 'Windows a 32 bit' + #13#10;
    if GetSpaceOnDisk(ExpandConstant('{sd}\'), True, FreeMB, TotalMB) then
      Report := Report + 'Spazio disponibile sul disco di sistema (MB): ' + IntToStr(FreeMB)
    else Report := Report + 'Spazio disco: non rilevato';
    ReportPage.RichEditViewer.Text := Report + #13#10#13#10 +
      'Anteprima: nessun servizio installato. RAM, porte e servizi ancora da implementare.';
  except
    Log('Diagnostic failure: ' + GetExceptionMessage);
    ReportPage.RichEditViewer.Text := 'Diagnostica non riuscita: ' + GetExceptionMessage;
  end;
end;

using System;
using System.Drawing;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using Microsoft.VisualBasic.Devices;

static class Desk {
 [STAThread] static void Main() {
  Application.EnableVisualStyles();
  Application.SetCompatibleTextRenderingDefault(false);
  Application.Run(new DeskWindow());
 }
}
class DeskWindow : Form {
 readonly TextBox report = new TextBox();
 readonly Label status = new Label();
 readonly string data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"VenVia","Desk");
 public DeskWindow() {
  Text="VenVia Desk 0.4 - Windows"; Width=820; Height=620; MinimumSize=new Size(600,420); StartPosition=FormStartPosition.CenterScreen;
  var top=new FlowLayoutPanel {Dock=DockStyle.Top,Height=64,Padding=new Padding(12)};
  var scan=new Button {Text="Diagnostica",Width=130};
  var server=new Button {Text="Modalita server",Width=150};
  var client=new Button {Text="Modalita client",Width=150};
  top.Controls.AddRange(new Control[]{scan,server,client});
  report.Multiline=true; report.ReadOnly=true; report.ScrollBars=ScrollBars.Vertical; report.Dock=DockStyle.Fill; report.Font=new Font("Consolas",11);
  status.Dock=DockStyle.Bottom; status.Height=48; status.Padding=new Padding(12);
  Controls.Add(report); Controls.Add(status); Controls.Add(top);
  scan.Click+=(s,e)=>Scan(); server.Click+=(s,e)=>Configure("server"); client.Click+=(s,e)=>Configure("client");
  status.Text="Configurazione locale: "+ ReadMode()+" | Nessun servizio gestito in questa versione.";
  Shown+=(s,e)=>Scan();
 }
 string ReadMode(){try {return File.ReadAllText(Path.Combine(data,"mode.txt")).Trim();}catch{return "non impostata";}}
 void Configure(string mode){
  try {
   Directory.CreateDirectory(data); File.WriteAllText(Path.Combine(data,"mode.txt"),mode);
   status.Text="Modalita "+mode+" salvata. Provisioning e connessione non ancora disponibili.";
  } catch(Exception ex){MessageBox.Show(ex.Message,"Configurazione non salvata");}
 }
 void Scan(){
  var b=new System.Text.StringBuilder();
  b.AppendLine("VENVIA DESK - DIAGNOSTICA LOCALE");
  b.AppendLine("Windows: "+Environment.OSVersion.Version);
  b.AppendLine("Sistema a 64 bit: "+Environment.Is64BitOperatingSystem);
  b.AppendLine("Processori logici: "+Environment.ProcessorCount);
  try{b.AppendLine("RAM totale (MB): "+new ComputerInfo().TotalPhysicalMemory/1048576);}catch(Exception ex){b.AppendLine("RAM non rilevata: "+ex.Message);}
  try{var d=new DriveInfo(Path.GetPathRoot(data)); b.AppendLine("Disco disponibile (MB): "+d.AvailableFreeSpace/1048576);}catch(Exception ex){b.AppendLine("Disco non rilevato: "+ex.Message);}
  try {
   var listeners=IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
   foreach(int port in new[]{80,443,3306}){bool used=false; foreach(var endpoint in listeners)if(endpoint.Port==port)used=true; b.AppendLine("TCP "+port+": "+(used?"occupata":"nessun listener rilevato"));}
  }catch(Exception ex){b.AppendLine("Porte non rilevate: "+ex.Message);}
  try{
   foreach(var service in System.ServiceProcess.ServiceController.GetServices()){
    using(service){string n=(service.ServiceName+" "+service.DisplayName).ToLowerInvariant(); if(n.Contains("mysql")||n.Contains("maria")||n.Contains("nginx")||n.Contains("apache")||n.Contains("php")||n.Contains("w3svc")) b.AppendLine("Servizio: "+service.ServiceName+" ("+service.Status+")");}
   }
  }catch(Exception ex){b.AppendLine("Inventario servizi incompleto: "+ex.Message);}
  b.AppendLine("\r\nInventario indicativo: non certifica compatibilita o disponibilita future delle porte.");
  b.AppendLine("PHP, MariaDB, Nginx, DNS, TLS e Core non vengono installati in questa build.");
  report.Text=b.ToString();
 }
}

using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Forms;

static class Program {
    [STAThread]
    static int Main(string[] args) {
        if (args.Length == 1 && args[0] == "--smoke") {
            using var lab = new Lab();
            try { lab.StartAsync(Console.WriteLine).GetAwaiter().GetResult(); return 0; }
            catch (Exception e) { Console.Error.WriteLine(e.Message); return 1; }
        }
        ApplicationConfiguration.Initialize();
        Application.Run(new SetupForm());
        return 0;
    }
}
sealed class SetupForm : Form {
    readonly TextBox log = new() { Multiline=true, ReadOnly=true, ScrollBars=ScrollBars.Vertical, Dock=DockStyle.Fill };
    readonly Button start = new() { Text="Installa e avvia laboratorio", AutoSize=true };
    readonly Button verify = new() { Text="Verifica ambiente", AutoSize=true, Enabled=false };
    readonly Button stop = new() { Text="Arresta laboratorio", AutoSize=true, Enabled=false };
    Lab? lab;
    bool busy;
    public SetupForm() {
        Text="VenVia Server Setup - laboratorio Windows"; Width=850; Height=520;
        var bar=new FlowLayoutPanel { Dock=DockStyle.Top, AutoSize=true };
        bar.Controls.AddRange(new Control[]{start,verify,stop}); Controls.Add(log); Controls.Add(bar);
        Log("PHP + MariaDB + Caddy. Solo loopback, dati di test. Nessun Core operativo.");
        start.Click+=async (_,_)=> { busy=true; start.Enabled=false;
            try { lab=new Lab(); await lab.StartAsync(Log); verify.Enabled=stop.Enabled=true; }
            catch(Exception e) { Log("ERRORE: "+e.Message); lab?.Dispose(); lab=null; start.Enabled=true; }
            finally { busy=false; }
        };
        verify.Click+=async (_,_)=> { if(lab==null)return; busy=true; verify.Enabled=stop.Enabled=false;
            try { await lab.VerifyAsync(); Log("OK: Caddy -> PHP FastCGI -> MariaDB SELECT 1"); }
            catch(Exception e){Log("ERRORE: "+e.Message);}
            finally {busy=false;verify.Enabled=stop.Enabled=true;}
        };
        stop.Click+=(_,_)=> {lab?.Dispose();lab=null;Log("Laboratorio arrestato; cartella conservata.");verify.Enabled=stop.Enabled=false;start.Enabled=true;};
        FormClosing+=(_,e)=> {if(busy){e.Cancel=true;return;}lab?.Dispose();};
    }
    void Log(string s) { if(InvokeRequired){Invoke((Action)(()=>Log(s)));return;} log.AppendText(s+Environment.NewLine); }
}
sealed class Lab : IDisposable {
    readonly List<Process> children=new();
    readonly string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"VenVia","Lab-"+Guid.NewGuid().ToString("N"));
    readonly string token=Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
    readonly string password=Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
    readonly string rootPassword=Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
    const int Web=18080, Php=19000, Db=13306;
    string PathAt(params string[] parts)=>Path.Combine(new[]{root}.Concat(parts).ToArray());
    static void Free(int port){var l=new TcpListener(IPAddress.Loopback,port);try{l.Start();}finally{l.Stop();}}
    static ProcessStartInfo Psi(string exe,IEnumerable<string> args,string cwd) {
        var p=new ProcessStartInfo(exe){WorkingDirectory=cwd,UseShellExecute=false,CreateNoWindow=true};
        foreach(var a in args)p.ArgumentList.Add(a);return p;
    }
    async Task RunAsync(string exe,string[] args,string cwd) {
        using var p=Process.Start(Psi(exe,args,cwd))??throw new Exception("Avvio processo non riuscito");
        using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(3));
        try {await p.WaitForExitAsync(timeout.Token);}catch {try{p.Kill(true);}catch{} throw;}
        if(p.ExitCode!=0)throw new Exception(Path.GetFileName(exe)+" exit "+p.ExitCode);
    }
    void Start(string exe,string[] args,string cwd, bool php=false) {
        var info=Psi(exe,args,cwd);
        if(php){info.Environment["VV_LAB_PASSWORD"]=password; info.Environment["VV_LAB_TOKEN"]=token;}
        children.Add(Process.Start(info)??throw new Exception("Avvio processo non riuscito"));
    }
    void Extract(string filename,string dest,string expected) {
        string source=Path.Combine(AppContext.BaseDirectory,"packages",filename);
        using var f=File.OpenRead(source);
        if(!Convert.ToHexString(SHA256.HashData(f)).Equals(expected,StringComparison.OrdinalIgnoreCase))throw new Exception("Checksum errato: "+filename);
        f.Position=0;
        Directory.CreateDirectory(dest);
        using var z=new ZipArchive(f,ZipArchiveMode.Read);
        long total=0;
        foreach(var e in z.Entries) {
            total=checked(total+e.Length);
            if(total>2L*1024*1024*1024 || ((e.ExternalAttributes>>16)&0xF000)==0xA000)throw new Exception("Archivio non autorizzato");
        }
        z.ExtractToDirectory(dest,false);
    }
    static string Slash(string p)=>p.Replace('\\','/');
    public async Task StartAsync(Action<string> log) {
        if(!Environment.Is64BitOperatingSystem)throw new Exception("Windows x64 richiesto");
        foreach(int p in new[]{Web,Php,Db})Free(p);
        Directory.CreateDirectory(root);log("Cartella: "+root);
        // Manifest is embedded during the CI build, never accepted from the UI.
        using var manifest=JsonDocument.Parse(typeof(Lab).Assembly.GetManifestResourceStream("VenVia.runtime.json")??throw new Exception("Manifest assente"));
        var m=manifest.RootElement;
        await Task.Run(()=>{
            Extract("php.zip",PathAt("php"),m.GetProperty("php").GetString()!);
            Extract("mariadb.zip",PathAt("maria"),m.GetProperty("mariadb").GetString()!);
            Extract("caddy.zip",PathAt("caddy"),m.GetProperty("caddy").GetString()!);
        });
        log("Archivi estratti e checksum verificati.");
        string maria=Directory.GetDirectories(PathAt("maria")).Single();
        string bin=Path.Combine(maria,"bin"), data=PathAt("data"), php=PathAt("php"), web=PathAt("public");
        Directory.CreateDirectory(web);
        File.WriteAllText(PathAt("php","php.ini"),"extension_dir=\""+Slash(PathAt("php","ext"))+"\"\nextension=pdo_mysql\nextension=mbstring\nextension=openssl\nextension=curl\nextension=zip\nextension=fileinfo\ndisplay_errors=Off\nlog_errors=On\ncgi.fix_pathinfo=0\n");
        await RunAsync(PathAt("php","php.exe"),new[]{"-c",PathAt("php","php.ini"),"-r","exit(extension_loaded('pdo_mysql') ? 0 : 1);"},php);
        log("PHP CLI verificato.");
        await RunAsync(Path.Combine(bin,"mariadb-install-db.exe"),new[]{"--datadir="+data,"--password="+rootPassword,"--port="+Db},maria);
        Start(Path.Combine(bin,"mariadbd.exe"),new[]{"--no-defaults","--basedir="+maria,"--datadir="+data,"--bind-address=127.0.0.1","--port="+Db,"--skip-name-resolve","--console"},maria);
        File.WriteAllText(PathAt("public","index.php"),"<?php header('Content-Type: application/json'); try { $p=new PDO('mysql:host=127.0.0.1;port=13306;dbname=venvia_lab','venvia_probe',getenv('VV_LAB_PASSWORD'),[PDO::ATTR_ERRMODE=>PDO::ERRMODE_EXCEPTION]); echo json_encode(['ok'=>(string)$p->query('SELECT 1')->fetchColumn()==='1','token'=>getenv('VV_LAB_TOKEN')]); } catch(Throwable $e){http_response_code(503);echo '{\"ok\":false}';}");
        string opt=PathAt("bootstrap.cnf");
        File.WriteAllText(opt,"[client]\nuser=root\npassword="+rootPassword+"\nhost=127.0.0.1\nport="+Db+"\nprotocol=tcp\n");
        try{
            bool ready=false;
            for(int i=0;i<30;i++){
                try{await RunAsync(Path.Combine(bin,"mariadb.exe"),new[]{"--defaults-extra-file="+opt,"--connect-timeout=2","-e","SELECT 1"},maria);ready=true;break;}catch{await Task.Delay(1000);}
            }
            if(!ready)throw new Exception("MariaDB non risponde");
            await RunAsync(Path.Combine(bin,"mariadb.exe"),new[]{"--defaults-extra-file="+opt,"-e","CREATE DATABASE venvia_lab; CREATE USER 'venvia_probe'@'127.0.0.1' IDENTIFIED BY '"+password+"'; GRANT SELECT ON venvia_lab.* TO 'venvia_probe'@'127.0.0.1';"},maria);
        }finally{File.Delete(opt);}
        log("MariaDB inizializzato e autenticato.");
        Start(PathAt("php","php-cgi.exe"),new[]{"-c",PathAt("php","php.ini"),"-b","127.0.0.1:"+Php},php,true);
        string cfg=PathAt("Caddyfile");
        File.WriteAllText(cfg,"{\n admin off\n auto_https off\n}\nhttp://127.0.0.1:"+Web+" {\n bind 127.0.0.1\n root * \""+Slash(web)+"\"\n php_fastcgi 127.0.0.1:"+Php+"\n}\n");
        await RunAsync(PathAt("caddy","caddy.exe"),new[]{"validate","--config",cfg,"--adapter","caddyfile"},root);
        Start(PathAt("caddy","caddy.exe"),new[]{"run","--config",cfg,"--adapter","caddyfile"},root);
        for(int i=0;i<30;i++){try{await VerifyAsync();log("OK: Caddy -> PHP FastCGI -> MariaDB SELECT 1. http://127.0.0.1:"+Web);return;}catch{await Task.Delay(1000);}}
        throw new Exception("Test integrato non riuscito");
    }
    public async Task VerifyAsync(){
        if(children.Count!=3||children.Any(p=>p.HasExited))throw new Exception("Uno dei processi non è in esecuzione");
        using var http=new HttpClient(new HttpClientHandler{UseProxy=false,AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(3)};
        using var result=JsonDocument.Parse(await http.GetStringAsync("http://127.0.0.1:"+Web+"/"));
        if(!result.RootElement.GetProperty("ok").GetBoolean()||result.RootElement.GetProperty("token").GetString()!=token)throw new Exception("Risposta test non valida");
    }
    public void Dispose(){
        foreach(var p in children.AsEnumerable().Reverse()){try{if(!p.HasExited){p.Kill(true);p.WaitForExit(5000);}}catch{}finally{p.Dispose();}}
        children.Clear();
    }
}

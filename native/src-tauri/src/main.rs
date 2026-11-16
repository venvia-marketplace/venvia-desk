#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]
use serde::{Deserialize, Serialize};
use std::{fs, net::{TcpListener, SocketAddr}, path::PathBuf};
use sysinfo::{System, Disks};
use tauri::Manager;
mod server;

#[derive(Serialize)]
struct Report { os: String, arch: String, cpu: String, logical_cpus: usize, ram_mib: u64, disks: Vec<Disk>, ports: Vec<Port>, processes: Vec<String>, local_ipv4: String, network_inventory: String }
#[derive(Serialize)]
struct Disk { mount: String, free_mib: u64, total_mib: u64 }
#[derive(Serialize)]
struct Port { number: u16, result: String }
#[derive(Serialize, Deserialize, Default)]
struct Settings { mode: String }

fn network_ipv4() -> String {
    use std::net::UdpSocket;
    UdpSocket::bind("0.0.0.0:0").and_then(|s| {
        // UDP connect selects a local route; no payload is sent.
        s.connect("192.0.2.1:9")?;
        s.local_addr()
    }).map(|a| a.ip().to_string()).unwrap_or_else(|_| "Nessuna route IPv4 rilevata".into())
}
#[cfg(target_os = "windows")]
fn network_inventory() -> String {
    use std::os::windows::process::CommandExt;
    let Some(root) = std::env::var_os("SystemRoot") else { return "SystemRoot non disponibile".into(); };
    let cmd = PathBuf::from(root).join("System32").join("ipconfig.exe");
    match std::process::Command::new(cmd).creation_flags(0x08000000).output() {
        Ok(out) if out.status.success() => String::from_utf8_lossy(&out.stdout).into_owned(),
        _ => "Inventario interfacce non disponibile".into(),
    }
}
#[cfg(not(target_os = "windows"))]
fn network_inventory() -> String { "Inventario interfacce non ancora supportato su questa piattaforma".into() }

#[tauri::command]
fn export_report(app: tauri::AppHandle, report: String) -> Result<String, String> {
    use std::io::Write;
    if report.trim().is_empty() || report.len() > 262144 { return Err("Dimensione report non valida".into()); }
    let root = app.path().document_dir().map_err(|e| e.to_string())?;
    let ts = std::time::SystemTime::now().duration_since(std::time::UNIX_EPOCH).map_err(|e| e.to_string())?.as_millis();
    let file = root.join(format!("VenVia-diagnostica-{ts}.txt"));
    let mut out = fs::OpenOptions::new().write(true).create_new(true).open(&file).map_err(|e| e.to_string())?;
    out.write_all(report.as_bytes()).map_err(|e| e.to_string())?;
    out.sync_all().map_err(|e| e.to_string())?;
    Ok(file.display().to_string())
}
fn valid_mode(mode: &str) -> bool { matches!(mode, "server" | "client") }
fn settings_path(app: &tauri::AppHandle) -> Result<PathBuf, String> {
    Ok(app.path().app_config_dir().map_err(|e| e.to_string())?.join("settings.json"))
}
#[tauri::command]
fn load_settings(app: tauri::AppHandle) -> Result<Settings, String> {
    match fs::read(settings_path(&app)?) {
        Ok(bytes) => {
            let s: Settings = serde_json::from_slice(&bytes).map_err(|e| e.to_string())?;
            if !valid_mode(&s.mode) { return Err("Modalità non valida".into()); }
            Ok(s)
        }
        Err(e) if e.kind() == std::io::ErrorKind::NotFound => Ok(Settings::default()),
        Err(e) => Err(e.to_string())
    }
}
#[tauri::command]
fn save_settings(app: tauri::AppHandle, mode: String) -> Result<(), String> {
    if !valid_mode(&mode) { return Err("Modalità non valida".into()); }
    let p = settings_path(&app)?;
    fs::create_dir_all(p.parent().ok_or("Percorso non valido")?).map_err(|e| e.to_string())?;
    fs::write(p, serde_json::to_vec_pretty(&Settings { mode }).map_err(|e| e.to_string())?).map_err(|e| e.to_string())
}
#[tauri::command]
async fn diagnostics() -> Result<Report, String> {
    tauri::async_runtime::spawn_blocking(|| {
        let mut s = System::new_all(); s.refresh_all();
        let disks = Disks::new_with_refreshed_list().list().iter().map(|d| Disk { mount: d.mount_point().display().to_string(), free_mib: d.available_space()/1048576, total_mib: d.total_space()/1048576 }).collect();
        let ports = [80,443,3306].into_iter().map(|number| {
            let addr = SocketAddr::from(([127,0,0,1],number));
            let result = match TcpListener::bind(addr) { Ok(_) => "Bind locale riuscito", Err(_) => "Bind locale non disponibile" }.to_string();
            Port { number, result }
        }).collect();
        let mut processes: Vec<String> = s.processes().values().filter_map(|p| {
            let n = p.name().to_string_lossy().to_string();
            let lower = n.to_lowercase();
            if ["php","nginx","maria","mysql","httpd","apache","w3wp"].iter().any(|x| lower.starts_with(*x)) { Some(n) } else { None }
        }).collect(); processes.sort(); processes.dedup();
        Report { os: System::long_os_version().unwrap_or_else(|| "Non rilevato".into()), arch: std::env::consts::ARCH.into(), cpu: s.cpus().first().map(|c| c.brand().to_string()).unwrap_or_default(), logical_cpus: s.cpus().len(), ram_mib: s.total_memory()/1048576, disks, ports, processes, local_ipv4: network_ipv4(), network_inventory: network_inventory() }
    }).await.map_err(|e| e.to_string())
}
#[tauri::command]
async fn server_plan(app: tauri::AppHandle) -> Result<server::ServerPlan, String> {
    let mode = load_settings(app.clone())?.mode;
    let root = app.path().app_local_data_dir().map_err(|e| e.to_string())?;
    tauri::async_runtime::spawn_blocking(move || server::plan(&root, &mode)).await.map_err(|e| e.to_string())
}
fn main() {
    tauri::Builder::default().invoke_handler(tauri::generate_handler![diagnostics,load_settings,save_settings,server_plan,export_report]).run(tauri::generate_context!()).expect("Avvio VenVia Desk non riuscito");
}
#[cfg(test)]
mod tests { use super::*; #[test] fn modes() { assert!(valid_mode("server")); assert!(valid_mode("client")); assert!(!valid_mode("")); assert!(!valid_mode("../server")); } }

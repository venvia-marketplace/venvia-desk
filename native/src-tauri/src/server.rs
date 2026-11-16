use serde::Serialize;
use std::{net::{TcpListener, SocketAddr}, path::Path};
use sysinfo::{Disks, System};

#[derive(Serialize)]
pub struct ServerPlan {
    pub checks: Vec<Check>,
    pub ready: bool,
    pub installation_enabled: bool,
}
#[derive(Serialize)]
pub struct Check {
    pub name: String,
    pub state: String,
    pub detail: String,
}
fn check(name: &str, state: &str, detail: impl Into<String>) -> Check {
    Check { name: name.into(), state: state.into(), detail: detail.into() }
}
pub fn plan(root: &Path, mode: &str) -> ServerPlan {
    let mut checks = vec![
        check("Modalità", if mode == "server" { "ok" } else { "blocked" }, mode),
        check("Piattaforma", if cfg!(all(target_os = "windows", target_arch = "x86_64")) { "ok" } else { "blocked" }, std::env::consts::OS),
    ];
    let mut sys = System::new();
    sys.refresh_memory();
    let ram = sys.total_memory() / 1048576;
    checks.push(check("RAM", if ram >= 4096 { "ok" } else { "review" }, format!("{ram} MiB; soglia preliminare 4096 MiB, non certificata")));
    let disks = Disks::new_with_refreshed_list();
    let disk = disks.list().iter().filter(|d| root.starts_with(d.mount_point())).max_by_key(|d| d.mount_point().components().count());
    checks.push(match disk {
        Some(d) => check("Spazio dati", if d.available_space() >= 20 * 1024 * 1024 * 1024 { "ok" } else { "review" }, format!("{} MiB disponibili; soglia preliminare 20 GiB", d.available_space()/1048576)),
        None => check("Spazio dati", "review", "Disco non identificato"),
    });
    for port in [80,443,3306,9000] {
        let available = TcpListener::bind(SocketAddr::from(([127,0,0,1], port))).is_ok();
        checks.push(check(&format!("TCP {port}"), if available { "ok" } else { "review" }, "Sonda temporanea loopback; non garantisce disponibilità futura"));
    }
    checks.push(check("Runtime PHP / MariaDB / Nginx", "blocked", "Pacchetti ufficiali versionati e verificati non ancora approvati"));
    checks.push(check("VenVia Core PHP", "blocked", "Archivio sorgente completo e contratto di installazione non ancora disponibili"));
    checks.push(check("DNS e HTTPS", "blocked", "Configurazione dominio e certificati da implementare"));
    checks.push(check("Servizi Windows e ripristino", "blocked", "Provisioning privilegiato non ancora implementato"));
    ServerPlan { ready: checks.iter().all(|c| c.state == "ok"), checks, installation_enabled: false }
}
#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn plan_cannot_authorize_installation() {
        let p = plan(std::env::temp_dir().as_path(), "client");
        assert!(!p.ready);
        assert!(!p.installation_enabled);
        assert_eq!(p.checks[0].state, "blocked");
    }
}

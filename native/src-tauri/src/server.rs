use serde::{Deserialize, Serialize};
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
#[derive(Deserialize)]
struct RuntimeManifest {
    approved: bool,
    packages: Vec<RuntimePackage>,
}
#[derive(Deserialize)]
struct RuntimePackage {
    id: String,
    version: String,
    sha256: Option<String>,
    status: String,
}
fn runtime_checks() -> Vec<Check> {
    let raw = include_str!("../../../runtime/windows-x64.candidates.json");
    let Ok(manifest) = serde_json::from_str::<RuntimeManifest>(raw) else {
        return vec![check("Runtime", "blocked", "Manifest non valido")];
    };
    let mut out = Vec::new();
    for pkg in manifest.packages {
        let hash = pkg.sha256.as_deref().is_some_and(|s| s.len() == 64 && s.bytes().all(|b| b.is_ascii_hexdigit()));
        out.push(check(&format!("Pacchetto {}", pkg.id), "blocked", format!("Versione candidata {}; {}; checksum registrato: {}. Archivio locale e firma non verificati.",pkg.version,pkg.status,hash)));
    }
    out.push(check("Autorizzazione runtime","blocked",if manifest.approved { "Flag presente ma firma e catena di fiducia non implementate" } else { "Manifest candidato non approvato" }));
    out
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
    checks.extend(runtime_checks());
    checks.push(check("VenVia Core PHP", "blocked", "Contratto preliminare disponibile; archivio Core completo non ancora verificato"));
    checks.push(check("DNS e HTTPS", "blocked", "Configurazione dominio e certificati da implementare"));
    checks.push(check("Servizi Windows e ripristino", "blocked", "Provisioning privilegiato non ancora implementato"));
    ServerPlan { ready: checks.iter().all(|c| c.state == "ok"), checks, installation_enabled: false }
}
#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn runtime_candidates_never_authorize_installation() {
        let checks = runtime_checks();
        assert_eq!(checks.len(), 4);
        assert!(checks.iter().all(|c| c.state == "blocked"));
    }
    #[test]
    fn plan_cannot_authorize_installation() {
        let p = plan(std::env::temp_dir().as_path(), "client");
        assert!(!p.ready);
        assert!(!p.installation_enabled);
        assert_eq!(p.checks[0].state, "blocked");
    }
}

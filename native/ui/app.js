"use strict";
const invoke = window.__TAURI__.core.invoke;
const status = document.getElementById("status"), report = document.getElementById("report"), scan = document.getElementById("scan"), save = document.getElementById("save");
async function refresh(){
 scan.disabled=true; report.textContent="Diagnostica in corso...";
 try{
  const r=await invoke("diagnostics");
  report.textContent=[r.os,r.arch,r.cpu,"CPU logiche: "+r.logical_cpus,"RAM MiB: "+r.ram_mib,"IPv4 della route selezionata: "+r.local_ipv4,"Interfacce di rete:\n"+r.network_inventory,...r.disks.map(d=>d.mount+" — liberi "+d.free_mib+" MiB / "+d.total_mib),...r.ports.map(p=>"TCP "+p.number+": "+p.result), "Processi runtime rilevati: "+(r.processes.join(", ")||"nessuno"),"Controlli indicativi: non attestano compatibilità o stato dei servizi."].join("\n");
 }catch(e){report.textContent="Errore diagnostica: "+String(e);}finally{scan.disabled=false;}
}
save.addEventListener("click",async()=>{const m=document.querySelector("input[name=mode]:checked");if(!m){status.textContent="Seleziona una modalità.";return;}save.disabled=true;try{await invoke("save_settings",{mode:m.value});status.textContent="Modalità salvata. Attivazione runtime prevista in una macro successiva.";}catch(e){status.textContent=String(e);}finally{save.disabled=false;}});
scan.addEventListener("click",refresh);
invoke("load_settings").then(s=>{for(const el of document.querySelectorAll("input[name=mode]"))el.checked=el.value===s.mode;}).catch(e=>{status.textContent="Configurazione: "+String(e);});
refresh();
const planButton = document.getElementById("plan"), serverReport = document.getElementById("server-report");
planButton.addEventListener("click", async () => {
 planButton.disabled = true; serverReport.textContent = "Verifica in corso...";
 try {
  const p = await invoke("server_plan");
  serverReport.textContent = p.checks.map(c => "[" + c.state.toUpperCase() + "] " + c.name + ": " + c.detail).join("\n") + "\n\nInstallazione non ancora abilitata.";
 } catch(e) { serverReport.textContent = "Errore: " + String(e); }
 finally { planButton.disabled = false; }
});

const exportButton = document.getElementById("export"), exportStatus = document.getElementById("export-status");
exportButton.addEventListener("click", async () => {
 exportButton.disabled = true;
 try {
  const result = await invoke("export_report", {report: "VenVia Desk — diagnostica locale\n"+new Date().toISOString()+"\n\n"+report.textContent+"\n\n"+serverReport.textContent});
  exportStatus.textContent = "Report salvato: " + result;
 } catch(e) { exportStatus.textContent = "Esportazione non riuscita: "+String(e); }
 finally { exportButton.disabled = false; }
});

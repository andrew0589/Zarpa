"""Genera un vídeo de estudio a partir de un guion JSON.

    python generar_video.py tema01\\1.1_casco.json
    python generar_video.py tema01\\1.1_casco.json --solo-slides   (solo las imágenes, para revisar)

Por cada diapositiva del guion: dibuja la imagen 1920x1080 con Chrome headless,
genera la narración con una voz neural (edge-tts) y monta el clip con ffmpeg.
Al final une los clips y escribe el .mp4 (con subtítulos incrustados como pista)
y el .vtt al lado, en out\\.
La narración se guarda en caché (_build\\<guion>\\audio): solo se regenera la
diapositiva cuyo texto ha cambiado.
"""
import argparse
import asyncio
import hashlib
import html
import json
import subprocess
import sys
from pathlib import Path

import edge_tts

AQUI = Path(__file__).resolve().parent
CHROME = r"C:\Program Files\Google\Chrome\Application\chrome.exe"
VOZ = "es-ES-AlvaroNeural"
VELOCIDAD = "-4%"
ANTES, DESPUES = 0.5, 0.9   # silencio alrededor de la narración de cada diapositiva
FPS = 30

LOGO = ('<svg viewBox="0 0 64 64"><polygon points="32,4 40,30 24,30" fill="#3CC2BC"/>'
        '<polygon points="24,30 40,30 32,40" fill="#FFFFFF"/>'
        '<path d="M8 49 q6 -5.5 12 0 t12 0 t12 0 t12 0" fill="none" stroke="#FFFFFF" stroke-width="4" stroke-linecap="round"/>'
        '<path d="M8 58 q6 -5.5 12 0 t12 0 t12 0 t12 0" fill="none" stroke="#3CC2BC" stroke-width="4" stroke-linecap="round"/></svg>')

CSS = """
*{box-sizing:border-box;margin:0;padding:0}
body{width:1920px;height:1080px;overflow:hidden;font-family:"Plus Jakarta Sans","Segoe UI",sans-serif;background:#F4F7FA;color:#12324A}
.top{height:110px;background:#0B3D5C;color:#fff;display:flex;align-items:center;padding:0 64px;gap:28px}
.top .tema{font-size:26px;font-weight:600;color:#3CC2BC;letter-spacing:2px;text-transform:uppercase}
.top .brand{margin-left:auto;display:flex;align-items:center;gap:14px;font-size:30px;font-weight:800}
.top .brand svg{width:52px;height:52px}
.top .brand span{color:#3CC2BC}
h1{font-size:62px;font-weight:800;color:#0B3D5C;padding:40px 64px 0}
.main{position:absolute;top:245px;left:64px;right:64px;bottom:56px;display:flex;gap:56px;align-items:center}
.main.col{flex-direction:column;gap:36px;align-items:stretch}
.vis{flex:1.25;height:100%;display:flex;align-items:center;justify-content:center;min-width:0}
.main.col .vis{flex:1;height:auto;min-height:0}
.vis img,.vis svg{max-width:100%;max-height:100%;object-fit:contain;border-radius:18px}
.vis img{box-shadow:0 8px 30px rgba(11,61,92,.14);background:#fff}
.txt{flex:1;display:flex;flex-direction:column;gap:22px;min-width:0}
.main.col .txt{flex-direction:row;flex:0 0 auto}
.main.col .card{flex:1}
.main.solo .txt{display:grid;grid-template-columns:1fr 1fr;gap:28px;width:100%}
.main.solo .nota{grid-column:1/-1}
.card{background:#fff;border-radius:18px;padding:24px 30px;border-left:10px solid #3CC2BC;box-shadow:0 4px 18px rgba(11,61,92,.08)}
.card .t{font-size:36px;font-weight:800;color:#0B3D5C;text-transform:uppercase;letter-spacing:.5px}
.card .d{font-size:29px;line-height:1.35;margin-top:8px;color:#24465E}
.card b{color:#0B3D5C}
.card.babor{border-left-color:#D62828}.card.babor .t{color:#B71C1C}
.card.estribor{border-left-color:#2A9D3F}.card.estribor .t{color:#1E7A30}
.nota{background:#FFF6DD;border:3px solid #E0B43E;border-radius:18px;padding:22px 30px;font-size:30px;line-height:1.35;color:#5A4300}
.nota::before{content:"¡Ojo! ";font-weight:800;color:#B07D00}
.recuerda{list-style:none;display:flex;flex-direction:column;gap:20px;width:100%}
.recuerda li{background:#fff;border-radius:16px;padding:24px 30px 24px 90px;font-size:39px;line-height:1.3;position:relative;box-shadow:0 4px 18px rgba(11,61,92,.08)}
.recuerda li::before{content:"✓";position:absolute;left:30px;top:24px;width:38px;height:38px;border-radius:50%;background:#3CC2BC;color:#0B3D5C;font-weight:800;display:flex;align-items:center;justify-content:center;font-size:26px}
.portada{background:#0B3D5C;color:#fff}
.portada .wrap{position:absolute;inset:0;display:flex;flex-direction:column;justify-content:center;padding:0 140px}
.portada .brand{display:flex;align-items:center;gap:18px;font-size:40px;font-weight:800;margin-bottom:70px}
.portada .brand svg{width:76px;height:76px}.portada .brand span{color:#3CC2BC}
.portada .tema{font-size:40px;font-weight:600;color:#3CC2BC;letter-spacing:3px;text-transform:uppercase}
.portada .titulo{font-size:120px;font-weight:800;line-height:1.05;margin-top:20px}
.portada .sub{font-size:42px;color:#C9D8E2;margin-top:30px;font-weight:500}
.portada .olas{position:absolute;left:0;right:0;bottom:70px}
"""

OLAS = ('<svg class="olas" viewBox="0 0 1920 160" preserveAspectRatio="none">'
        '<path d="M0 60 q80 -45 160 0 t160 0 t160 0 t160 0 t160 0 t160 0 t160 0 t160 0 t160 0 t160 0 t160 0 t160 0" fill="none" stroke="#FFFFFF" stroke-opacity=".35" stroke-width="10"/>'
        '<path d="M0 120 q80 -45 160 0 t160 0 t160 0 t160 0 t160 0 t160 0 t160 0 t160 0 t160 0 t160 0 t160 0 t160 0" fill="none" stroke="#3CC2BC" stroke-width="10"/></svg>')


def marcado(s):
    """Texto del guion -> HTML: escapa y convierte **x** en negrita."""
    partes = html.escape(s).split("**")
    return "".join(f"<b>{p}</b>" if i % 2 else p for i, p in enumerate(partes))


def html_slide(guion, s, base):
    if s.get("tipo") == "portada":
        cuerpo = (f'<div class="portada"><div class="wrap"><div class="brand">{LOGO}<div>Navigation<span>ES</span></div></div>'
                  f'<div class="tema">{marcado(s.get("tema", guion["tema"]))}</div>'
                  f'<div class="titulo">{marcado(s["titulo"])}</div>'
                  f'<div class="sub">{marcado(s.get("sub", ""))}</div></div>{OLAS}</div>')
        return f'<!doctype html><html><head><meta charset="utf-8"><style>{CSS}</style></head><body class="portada">{cuerpo}</body></html>'

    vis = ""
    if "img" in s:
        # Primero la carpeta del guion (imágenes propias del vídeo), luego la de las preguntas.
        carpeta = base if (base / s["img"]).exists() or "imagenes" not in guion else Path(guion["imagenes"])
        estilo = f' style="width:{s["ancho"]};height:auto"' if "ancho" in s else ""  # agranda imágenes pequeñas y anchas
        vis = f'<div class="vis"><img src="{(carpeta / s["img"]).resolve().as_uri()}"{estilo}></div>'
    elif "svg" in s:
        vis = f'<div class="vis">{(base / s["svg"]).read_text(encoding="utf-8")}</div>'

    txt = []
    for t in s.get("terminos", []):
        clase = f' {t["clase"]}' if "clase" in t else ""
        txt.append(f'<div class="card{clase}"><div class="t">{marcado(t["t"])}</div><div class="d">{marcado(t["d"])}</div></div>')
    if "nota" in s:
        txt.append(f'<div class="nota">{marcado(s["nota"])}</div>')
    if "puntos" in s:
        txt.append('<ul class="recuerda">' + "".join(f"<li>{marcado(p)}</li>" for p in s["puntos"]) + "</ul>")
    txt = f'<div class="txt">{"".join(txt)}</div>' if txt else ""

    clase = "main"
    if s.get("layout") == "col":
        clase += " col"
    elif not vis:
        clase += " solo" if s.get("terminos") else ""
    return (f'<!doctype html><html><head><meta charset="utf-8"><style>{CSS}</style></head><body>'
            f'<div class="top"><div class="tema">{marcado(guion["tema"])} · {marcado(guion["seccion"])}</div>'
            f'<div class="brand">{LOGO}<div>Navigation<span>ES</span></div></div></div>'
            f'<h1>{marcado(s["titulo"])}</h1><div class="{clase}">{vis}{txt}</div></body></html>')


def randeaza(fisier_html, fisier_png):
    fisier_png.unlink(missing_ok=True)
    subprocess.run([CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars",
                    "--allow-file-access-from-files", "--virtual-time-budget=3000",
                    "--window-size=1920,1080", f"--screenshot={fisier_png}", fisier_html.as_uri()],
                   check=True, capture_output=True)
    if not fisier_png.exists():
        sys.exit(f"Chrome no generó {fisier_png}")


async def narra(texto, mp3, json_frases):
    frases = []
    com = edge_tts.Communicate(texto, VOZ, rate=VELOCIDAD, boundary="SentenceBoundary")
    with open(mp3, "wb") as f:
        async for chunk in com.stream():
            if chunk["type"] == "audio":
                f.write(chunk["data"])
            elif chunk["type"] == "SentenceBoundary":
                frases.append({"ini": chunk["offset"] / 1e7, "dur": chunk["duration"] / 1e7, "txt": chunk["text"]})
    json_frases.write_text(json.dumps(frases, ensure_ascii=False, indent=1), encoding="utf-8")


def durata(fisier):
    r = subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", str(fisier)],
                       check=True, capture_output=True, text=True)
    return float(r.stdout.strip())


def clip(png, mp3, mp4, total):
    filtro = (f"[1:a]adelay={int(ANTES * 1000)}:all=1,apad[a];"
              f"[0:v]format=yuv420p,fade=t=in:st=0:d=0.35,fade=t=out:st={total - 0.35:.3f}:d=0.35[v]")
    subprocess.run(["ffmpeg", "-y", "-v", "error", "-loop", "1", "-framerate", str(FPS), "-i", str(png), "-i", str(mp3),
                    "-filter_complex", filtro, "-map", "[v]", "-map", "[a]", "-t", f"{total:.3f}",
                    "-c:v", "libx264", "-tune", "stillimage", "-preset", "medium", "-crf", "20", "-r", str(FPS),
                    "-c:a", "aac", "-b:a", "160k", "-ar", "48000", "-ac", "2", str(mp4)], check=True)


def trozos(frase, max_chars=84):
    """Parte una frase larga en trozos de subtítulo, repartiendo el tiempo por caracteres."""
    palabras, lineas, act = frase["txt"].split(), [], ""
    for p in palabras:
        if act and len(act) + 1 + len(p) > max_chars:
            lineas.append(act)
            act = p
        else:
            act = f"{act} {p}".strip()
    lineas.append(act)
    total = sum(len(l) for l in lineas)
    t = frase["ini"]
    for l in lineas:
        d = frase["dur"] * len(l) / total
        yield t, t + d, l
        t += d


def ts(seg):
    h, r = divmod(seg, 3600)
    m, s = divmod(r, 60)
    return f"{int(h):02}:{int(m):02}:{s:06.3f}"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("guion")
    ap.add_argument("--solo-slides", action="store_true")
    a = ap.parse_args()

    ruta = Path(a.guion).resolve()
    guion = json.loads(ruta.read_text(encoding="utf-8"))
    nombre = ruta.stem
    build = AQUI / "_build" / nombre
    for d in ("slides", "audio", "clips"):
        (build / d).mkdir(parents=True, exist_ok=True)
    (AQUI / "out").mkdir(exist_ok=True)

    clips, cues, t0 = [], [], 0.0
    for i, s in enumerate(guion["slides"], 1):
        cod = f"{i:02}"
        h = build / "slides" / f"{cod}.html"
        png = build / "slides" / f"{cod}.png"
        h.write_text(html_slide(guion, s, ruta.parent), encoding="utf-8")
        randeaza(h, png)
        print(f"[{cod}] {s.get('titulo', '')[:60]}")
        if a.solo_slides:
            continue

        clave = hashlib.sha1(f"{VOZ}|{VELOCIDAD}|{s['narracion']}".encode()).hexdigest()[:12]
        mp3 = build / "audio" / f"{cod}_{clave}.mp3"
        frases = mp3.with_suffix(".json")
        if not mp3.exists():
            for viejo in (build / "audio").glob(f"{cod}_*"):
                viejo.unlink()
            asyncio.run(narra(s["narracion"], mp3, frases))
        total = ANTES + durata(mp3) + DESPUES
        mp4 = build / "clips" / f"{cod}.mp4"
        clip(png, mp3, mp4, total)
        clips.append(mp4)
        for f in json.loads(frases.read_text(encoding="utf-8")):
            for ini, fin, txt in trozos(f):
                ini = max(t0 + ANTES + ini, cues[-1][1] if cues else 0)
                cues.append((ini, t0 + ANTES + fin, txt))
        t0 += durata(mp4)

    if a.solo_slides:
        print(f"Diapositivas en {build / 'slides'}")
        return

    lista = build / "clips.txt"
    lista.write_text("".join(f"file '{c.as_posix()}'\n" for c in clips), encoding="utf-8")
    vtt = AQUI / "out" / f"{nombre}.vtt"
    vtt.write_text("WEBVTT\n\n" + "".join(f"{ts(i)} --> {ts(f)}\n{t}\n\n" for i, f, t in cues), encoding="utf-8")
    salida = AQUI / "out" / f"{nombre}.mp4"
    subprocess.run(["ffmpeg", "-y", "-v", "error", "-f", "concat", "-safe", "0", "-i", str(lista), "-i", str(vtt),
                    "-map", "0", "-map", "1", "-c", "copy", "-c:s", "mov_text", "-metadata:s:s:0", "language=spa",
                    "-movflags", "+faststart", str(salida)], check=True)
    print(f"\n{salida}  ({durata(salida) / 60:.1f} min)\n{vtt}")


if __name__ == "__main__":
    main()

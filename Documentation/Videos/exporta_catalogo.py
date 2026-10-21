"""Catálogo de vídeos para la pestaña Admin › Vídeos de la web.

Lee los guiones temaNN\\*.json y los vídeos ya generados (out\\, _build\\) y escribe
out\\catalogo.json: una fila por vídeo con tema, código, título, subtítulo, duración,
términos clave y capítulos (inicio de cada diapositiva). Se importa en la web con
"Importar catálogo (JSON)"; la API crea o actualiza por tema + código.

    python exporta_catalogo.py                       # URLs relativas videos/<nombre>.mp4
    python exporta_catalogo.py --base-url https://cdn.ejemplo/videos/
    python exporta_catalogo.py --copia-api           # copia mp4 + vtt a NavigationES.Api\\wwwroot\\videos (pruebas en local)
    python exporta_catalogo.py --publicar            # marca como publicados los que tienen mp4

El primer vídeo de cada tema sale como gratis; en el resto no se envía el campo, así que
la API conserva lo que se haya marcado a mano (y las URLs de foto, que no se tocan).
"""

import argparse
import json
import re
import shutil
import subprocess
from pathlib import Path

AQUI = Path(__file__).resolve().parent
API_VIDEOS = AQUI.parent.parent / "NavigationES.Api" / "wwwroot" / "videos"


def durata(fisier):
    r = subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", str(fisier)],
                       capture_output=True, text=True, check=True)
    return float(r.stdout.strip())


def codigo(nombre):
    # "1.3a_equipo_fondeo" -> "1.3a"
    return nombre.split("_", 1)[0]


def orden(cod):
    # 1.10 después de 1.9; la letra (a/b) desempata.
    m = re.match(r"(\d+)\.(\d+)([a-z]?)", cod)
    return (int(m.group(1)), int(m.group(2)), m.group(3)) if m else (999, 999, cod)


def titulo(guion, cod):
    # "1.3 Equipo de fondeo" -> "Equipo de fondeo"
    sec = guion.get("seccion", "")
    return re.sub(r"^\s*\d+(\.\d+)*[a-z]?\s+", "", sec).strip() or sec


def terminos(guion):
    vistos, salida = set(), []
    for s in guion["slides"]:
        for t in s.get("terminos", []):
            nombre = t.get("t", "").strip()
            # Fuera los "1 · 2 · 3" de los resúmenes: solo términos con letras.
            if re.search(r"[A-Za-zÁÉÍÓÚáéíóúÑñ]", nombre) and nombre.lower() not in vistos:
                vistos.add(nombre.lower())
                salida.append(nombre)
    return salida


def capitulos(guion, nombre):
    clips = AQUI / "_build" / nombre / "clips"
    salida, t = [], 0.0
    for i, s in enumerate(guion["slides"], 1):
        clip = clips / f"{i:02}.mp4"
        if not clip.exists():
            return []
        # La portada inicial se llama "Introducción"; las demás (p. ej. "¡A practicar!") por su título.
        nombre_cap = "Introducción" if i == 1 and s.get("tipo") == "portada" else s.get("titulo", f"Parte {i}")
        salida.append({"title": nombre_cap, "start": int(t)})
        t += durata(clip)
    return salida


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base-url", default="videos/")
    ap.add_argument("--copia-api", action="store_true")
    ap.add_argument("--publicar", action="store_true")
    a = ap.parse_args()
    base = a.base_url if a.base_url.endswith("/") else a.base_url + "/"

    filas = []
    for carpeta in sorted(AQUI.glob("tema[0-9][0-9]")):
        tema = int(carpeta.name[4:])
        guiones = sorted(carpeta.glob("*.json"), key=lambda p: orden(codigo(p.stem)))
        for n, ruta in enumerate(guiones):
            guion = json.loads(ruta.read_text(encoding="utf-8"))
            nombre = ruta.stem
            cod = codigo(nombre)
            mp4 = AQUI / "out" / f"{nombre}.mp4"
            vtt = AQUI / "out" / f"{nombre}.vtt"
            hay_video = mp4.exists()

            portada = next((s for s in guion["slides"] if s.get("tipo") == "portada"), {})
            fila = {
                "topicNumber": tema,
                "code": cod,
                "sortOrder": n,
                "title": titulo(guion, cod),
                "subtitle": portada.get("sub"),
                "videoUrl": base + mp4.name if hay_video else None,
                "subtitlesUrl": base + vtt.name if vtt.exists() else None,
                "durationSeconds": int(round(durata(mp4))) if hay_video else 0,
                "keyTerms": terminos(guion),
                "chapters": capitulos(guion, nombre) if hay_video else [],
                "isFree": True if n == 0 else None,
                "isPublished": True if (a.publicar and hay_video) else None,
            }
            filas.append(fila)
            print(f"tema {tema:2}  {cod:6} {fila['title'][:40]:40} {fila['durationSeconds'] // 60}:{fila['durationSeconds'] % 60:02}"
                  f"  {len(fila['keyTerms'])} términos  {len(fila['chapters'])} capítulos{'' if hay_video else '  (sin mp4)'}")

            if a.copia_api and hay_video:
                API_VIDEOS.mkdir(parents=True, exist_ok=True)
                for f in (mp4, vtt):
                    if f.exists():
                        shutil.copy2(f, API_VIDEOS / f.name)

    salida = AQUI / "out" / "catalogo.json"
    salida.write_text(json.dumps(filas, ensure_ascii=False, indent=1), encoding="utf-8")
    print(f"\n{len(filas)} vídeos -> {salida}")
    if a.copia_api:
        print(f"Copiados a {API_VIDEOS}")


if __name__ == "__main__":
    main()

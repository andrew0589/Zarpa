"""Genera con la API de OpenAI (ChatGPT) las imágenes de las diapositivas que llevan "prompt".

    python generar_imagenes.py tema01\\1.2_estructura.json            (solo las que faltan o cuyo prompt cambió)
    python generar_imagenes.py tema01\\1.2_estructura.json --slide 3  (solo la diapositiva 3, numeradas desde 1)
    python generar_imagenes.py tema01\\1.2_estructura.json --forzar   (regenera aunque no haya cambios)
    python generar_imagenes.py tema01\\1.2_estructura.json --listar   (muestra qué haría, sin llamar a la API)

En el guion, la diapositiva lleva el destino y el prompt:
    "img": "img/quilla_roda_codaste.webp",
    "prompt": "Realistic, warm editorial illustration ...",
    "formato": "horizontal"        (opcional: horizontal | vertical | cuadrado; por defecto horizontal)

Guarda el PNG original en _originales\\<tema>\\img\\ y la copia comprimida (.webp, máx. 1600 px, q88)
en <tema>\\img\\, igual que las imágenes hechas a mano. El hash del prompt queda en
_build\\<guion>\\prompts.json: si el prompt no cambia, la imagen no se vuelve a pedir (ni a pagar).

Configuración en %USERPROFILE%\\.navigationes\\openai.json:
    {"api_key": "sk-...", "model": "gpt-image-1", "quality": "high"}
(o la variable de entorno OPENAI_API_KEY).
"""
import argparse
import base64
import hashlib
import io
import json
import os
import sys
from pathlib import Path

from openai import OpenAI
from PIL import Image

AQUI = Path(__file__).resolve().parent
CONFIG = Path.home() / ".navigationes" / "openai.json"
TAMANOS = {"horizontal": "1536x1024", "vertical": "1024x1536", "cuadrado": "1024x1024"}
LADO_MAX, CALIDAD_WEBP = 1600, 88


def config():
    cfg = json.loads(CONFIG.read_text(encoding="utf-8")) if CONFIG.exists() else {}
    cfg.setdefault("api_key", os.environ.get("OPENAI_API_KEY"))
    cfg.setdefault("model", "gpt-image-1")
    cfg.setdefault("quality", "high")
    if not cfg["api_key"]:
        sys.exit(f"Falta la clave: crea {CONFIG} con {{\"api_key\": \"sk-...\"}} o define OPENAI_API_KEY")
    return cfg


def huella(cfg, s):
    texto = f"{cfg['model']}|{cfg['quality']}|{s.get('formato', 'horizontal')}|{s['prompt']}"
    return hashlib.sha1(texto.encode()).hexdigest()[:12]


def guarda(png_bytes, destino_webp, original_png):
    original_png.parent.mkdir(parents=True, exist_ok=True)
    original_png.write_bytes(png_bytes)
    im = Image.open(io.BytesIO(png_bytes)).convert("RGB")
    im.thumbnail((LADO_MAX, LADO_MAX))
    destino_webp.parent.mkdir(parents=True, exist_ok=True)
    im.save(destino_webp, "WEBP", quality=CALIDAD_WEBP, method=6)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("guion")
    ap.add_argument("--slide", type=int, action="append", help="número de diapositiva (desde 1); se puede repetir")
    ap.add_argument("--forzar", action="store_true")
    ap.add_argument("--listar", action="store_true")
    a = ap.parse_args()

    ruta = Path(a.guion).resolve()
    guion = json.loads(ruta.read_text(encoding="utf-8"))
    tema = ruta.parent.name
    registro_f = AQUI / "_build" / ruta.stem / "prompts.json"
    registro = json.loads(registro_f.read_text(encoding="utf-8")) if registro_f.exists() else {}

    pendientes = []
    for i, s in enumerate(guion["slides"], 1):
        if "prompt" not in s or (a.slide and i not in a.slide):
            continue
        img = s.get("img", "")
        if not img.startswith("img/") or not img.endswith(".webp"):
            sys.exit(f"Diapositiva {i}: con \"prompt\", \"img\" debe ser img/<nombre>.webp (ahora: {img!r})")
        destino = ruta.parent / img
        h = None
        motivo = ("forzada" if a.forzar else "no existe" if not destino.exists()
                  else "prompt nuevo" if registro.get(img) is None else None)
        pendientes.append((i, s, img, destino, motivo))

    if not pendientes:
        print("No hay diapositivas con \"prompt\" en este guion.")
        return

    cfg = None if a.listar else config()
    cliente = None if a.listar else OpenAI(api_key=cfg["api_key"])
    for i, s, img, destino, motivo in pendientes:
        if cfg:
            h = huella(cfg, s)
            if not motivo and registro.get(img) != h:
                motivo = "prompt cambiado"
        titulo = s.get("titulo", "")[:50]
        if not motivo:
            print(f"[{i:02}] {titulo}: sin cambios, se conserva {img}")
            continue
        formato = s.get("formato", "horizontal")
        if a.listar:
            print(f"[{i:02}] {titulo}: se generaría {img} ({formato}, {motivo})")
            continue
        print(f"[{i:02}] {titulo}: generando {img} ({formato}, {motivo})...", flush=True)
        r = cliente.images.generate(model=cfg["model"], prompt=s["prompt"], size=TAMANOS[formato],
                                    quality=cfg["quality"], n=1)
        png = base64.b64decode(r.data[0].b64_json)
        guarda(png, destino, AQUI / "_originales" / tema / img.replace(".webp", ".png"))
        registro[img] = h
        registro_f.parent.mkdir(parents=True, exist_ok=True)
        registro_f.write_text(json.dumps(registro, indent=1), encoding="utf-8")
        print(f"      -> {destino}")


if __name__ == "__main__":
    main()

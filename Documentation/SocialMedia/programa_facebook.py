"""Programa en la página de Facebook las publicaciones de coada.json (Graph API).

Uso:
    python programa_facebook.py verifica            # comprueba el token y la página
    python programa_facebook.py programeaza --max 1 # programa solo la primera (prueba)
    python programa_facebook.py programeaza         # programa todo lo que cabe en la ventana de Facebook
    python programa_facebook.py lista               # publicaciones ya programadas en Facebook

El token NO va en el repositorio: se lee de las variables FB_PAGE_ID / FB_PAGE_TOKEN
o del archivo %USERPROFILE%\\.navigationes\\facebook.json  {"page_id": "...", "token": "..."}.
Las publicaciones programadas pasan de coada.json a publicate.json.
"""
import argparse
import datetime as dt
import json
import mimetypes
import os
import sys
import urllib.error
import urllib.parse
import urllib.request
import uuid
from pathlib import Path

AICI = Path(__file__).resolve().parent
COADA = AICI / "coada.json"
PUBLICATE = AICI / "publicate.json"
SECRET = Path.home() / ".navigationes" / "facebook.json"

# Facebook acepta una hora programada entre 10 minutos y 30 días desde ahora
MIN_INAINTE = dt.timedelta(minutes=15)
MAX_INAINTE = dt.timedelta(days=29)


def credentiale():
    page, token = os.environ.get("FB_PAGE_ID"), os.environ.get("FB_PAGE_TOKEN")
    if not (page and token) and SECRET.exists():
        s = json.loads(SECRET.read_text(encoding="utf-8"))
        page, token = s.get("page_id"), s.get("token")
    if not (page and token):
        sys.exit(f"Falta el token: define FB_PAGE_ID y FB_PAGE_TOKEN o crea {SECRET}")
    return page, token


def cere(url, date=None, tip=None):
    req = urllib.request.Request(url, data=date, headers={"Content-Type": tip} if tip else {})
    try:
        with urllib.request.urlopen(req, timeout=120) as r:
            return json.loads(r.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        corp = e.read().decode("utf-8", "replace")
        try:
            mesaj = json.loads(corp)["error"]["message"]
        except (ValueError, KeyError):
            mesaj = corp
        raise RuntimeError(f"HTTP {e.code}: {mesaj}") from None


def multipart(campuri, fisier):
    granita = uuid.uuid4().hex
    parti = []
    for nume, valoare in campuri.items():
        parti.append(f'--{granita}\r\nContent-Disposition: form-data; name="{nume}"\r\n\r\n{valoare}\r\n'.encode("utf-8"))
    tip = mimetypes.guess_type(fisier.name)[0] or "application/octet-stream"
    parti.append(
        f'--{granita}\r\nContent-Disposition: form-data; name="source"; filename="{fisier.name}"\r\n'
        f"Content-Type: {tip}\r\n\r\n".encode("utf-8") + fisier.read_bytes() + b"\r\n"
    )
    parti.append(f"--{granita}--\r\n".encode("utf-8"))
    return b"".join(parti), f"multipart/form-data; boundary={granita}"


def graph(cfg):
    return f"https://graph.facebook.com/{cfg['graph_version']}"


def verifica(cfg):
    page, token = credentiale()
    r = cere(f"{graph(cfg)}/{page}?" + urllib.parse.urlencode({"fields": "id,name,link", "access_token": token}))
    print(f"OK: {r.get('name')} ({r.get('id')}) {r.get('link', '')}")


def lista(cfg):
    page, token = credentiale()
    r = cere(f"{graph(cfg)}/{page}/scheduled_posts?" + urllib.parse.urlencode(
        {"fields": "id,scheduled_publish_time,message", "limit": 100, "access_token": token}))
    for p in r.get("data", []):
        ora = dt.datetime.fromtimestamp(int(p["scheduled_publish_time"]))
        print(f"{ora:%Y-%m-%d %H:%M}  {(p.get('message') or '').splitlines()[0][:80]}")
    print(f"{len(r.get('data', []))} programadas")


def programeaza(cfg, maxim):
    page, token = credentiale()
    coada = json.loads(COADA.read_text(encoding="utf-8")) if COADA.exists() else []
    publicate = json.loads(PUBLICATE.read_text(encoding="utf-8")) if PUBLICATE.exists() else []
    acum = dt.datetime.now()
    ramase, facute = [], 0

    def salveaza(procesate):
        # lo no procesado sigue tal cual detrás de lo que se ha quedado
        COADA.write_text(json.dumps(ramase + coada[procesate:], ensure_ascii=False, indent=1), encoding="utf-8")
        PUBLICATE.write_text(json.dumps(publicate, ensure_ascii=False, indent=1), encoding="utf-8")

    for x in coada:
        ora = dt.datetime.fromisoformat(x["ora"])
        if (maxim is not None and facute >= maxim) or not (acum + MIN_INAINTE <= ora <= acum + MAX_INAINTE):
            if ora < acum + MIN_INAINTE:
                print(f"  {ora:%Y-%m-%d %H:%M}  pasada, se queda en coada.json (bórrala o vuelve a generar)")
            ramase.append(x)
            continue
        date, tip = multipart({
            "caption": x["descriere"],
            "published": "false",
            "scheduled_publish_time": str(int(ora.timestamp())),
            "access_token": token,
        }, AICI / x["imagine"])
        try:
            r = cere(f"{graph(cfg)}/{page}/photos", date, tip)
        except RuntimeError as e:
            print(f"  {ora:%Y-%m-%d %H:%M}  ERROR {e}")
            ramase.append(x)
            break
        x["fb_id"] = r.get("post_id") or r.get("id")
        x["status"] = "programat"
        publicate.append(x)
        facute += 1
        print(f"  {ora:%Y-%m-%d %H:%M}  programada ({x['fb_id']})")
        # se guarda después de cada una, para no repetir nada si se corta a mitad
        salveaza(len(ramase) + facute)
    salveaza(len(ramase) + facute)
    print(f"{facute} programadas, {len(coada) - facute} siguen en coada.json")


def main():
    p = argparse.ArgumentParser()
    p.add_argument("comanda", choices=["verifica", "programeaza", "lista"])
    p.add_argument("--max", type=int)
    a = p.parse_args()
    cfg = json.loads((AICI / "config.json").read_text(encoding="utf-8"))
    {"verifica": lambda: verifica(cfg), "lista": lambda: lista(cfg),
     "programeaza": lambda: programeaza(cfg, a.max)}[a.comanda]()


if __name__ == "__main__":
    main()

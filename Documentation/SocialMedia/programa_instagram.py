"""Publica en Instagram las publicaciones cuya hora ha llegado.

Instagram no permite programar por API: este script lo lanza el Programador de tareas
de Windows cada 15 minutos y publica lo que toca. Usa las mismas publicaciones que
Facebook (coada.json + publicate.json) y el mismo token de página.

Uso:
    python programa_instagram.py verifica
    python programa_instagram.py publica-scadente   # lo que llama el Programador de tareas
    python programa_instagram.py publica-acum       # publica ya la próxima (prueba)

El estado de Instagram va aparte, en instagram.json, para no tocar los archivos de Facebook.
Registro: instagram.log
"""
import argparse
import contextlib
import datetime as dt
import json
import os
import sys
import time
import urllib.parse
from pathlib import Path

from programa_facebook import AICI, COADA, PUBLICATE, cere, credentiale, graph, multipart

STARE = AICI / "instagram.json"
LOG = AICI / "instagram.log"
BLOCARE = AICI / "instagram.lock"
# si el PC estaba apagado, lo atrasado más de esto se salta en vez de publicarse tarde
INTARZIERE_MAXIMA = dt.timedelta(hours=3)
MAXIM_PE_RULARE = 2


def log(mesaj):
    linie = f"{dt.datetime.now():%Y-%m-%d %H:%M:%S}  {mesaj}"
    print(linie)
    with LOG.open("a", encoding="utf-8") as f:
        f.write(linie + "\n")


@contextlib.contextmanager
def blocare():
    """Una sola ejecución a la vez: dos a la vez publicarían lo mismo dos veces."""
    while True:
        try:
            fd = os.open(BLOCARE, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
            break
        except FileExistsError:
            # un bloqueo de una ejecución que murió a medias no debe parar todo
            if time.time() - BLOCARE.stat().st_mtime > 600:
                BLOCARE.unlink(missing_ok=True)
                continue
            print("Ya hay otra ejecución en marcha; esta se salta")
            sys.exit(0)
    try:
        os.write(fd, str(os.getpid()).encode())
        os.close(fd)
        yield
    finally:
        BLOCARE.unlink(missing_ok=True)


def citeste(fisier):
    return json.loads(fisier.read_text(encoding="utf-8")) if fisier.exists() else []


def postari():
    """Todas las publicaciones conocidas (programadas o en cola), sin repetir, por hora."""
    vazute, toate = set(), []
    for x in citeste(PUBLICATE) + citeste(COADA):
        if x["cheie"] not in vazute:
            vazute.add(x["cheie"])
            toate.append(x)
    return sorted(toate, key=lambda x: x["ora"])


def descriere_instagram(text):
    # en Instagram los enlaces del texto no se pueden pulsar
    linii = []
    for linie in text.splitlines():
        if linie.startswith("📲"):
            linie = "📲 Practica con exámenes reales de tu comunidad: enlace en la bio @navigationes"
        linii.append(linie)
    return "\n".join(linii)


def cont_instagram(cfg, page, token):
    r = cere(f"{graph(cfg)}/{page}?" + urllib.parse.urlencode(
        {"fields": "instagram_business_account{id,username}", "access_token": token}))
    cont = r.get("instagram_business_account")
    if not cont:
        sys.exit("La página no tiene una cuenta de Instagram profesional vinculada")
    return cont


def url_public(cfg, page, token, imagine):
    """Sube la imagen a la página como foto no publicada y devuelve su URL en el CDN de Facebook."""
    date, tip = multipart({"published": "false", "access_token": token}, imagine)
    foto = cere(f"{graph(cfg)}/{page}/photos", date, tip)["id"]
    r = cere(f"{graph(cfg)}/{foto}?" + urllib.parse.urlencode({"fields": "images", "access_token": token}))
    cea_mai_mare = max(r["images"], key=lambda i: i["width"])
    return foto, cea_mai_mare["source"]


def publica(cfg, page, token, ig, x):
    foto, url = url_public(cfg, page, token, AICI / x["imagine"])
    try:
        G = graph(cfg)
        container = cere(f"{G}/{ig}/media", urllib.parse.urlencode({
            "image_url": url, "caption": descriere_instagram(x["descriere"]), "access_token": token,
        }).encode("utf-8"))["id"]
        for _ in range(30):
            stare = cere(f"{G}/{container}?" + urllib.parse.urlencode(
                {"fields": "status_code", "access_token": token})).get("status_code")
            if stare == "FINISHED":
                break
            if stare == "ERROR":
                raise RuntimeError("Instagram no pudo procesar la imagen")
            time.sleep(2)
        media = cere(f"{G}/{ig}/media_publish", urllib.parse.urlencode(
            {"creation_id": container, "access_token": token}).encode("utf-8"))["id"]
        link = cere(f"{G}/{media}?" + urllib.parse.urlencode(
            {"fields": "permalink", "access_token": token})).get("permalink", "")
        return media, link
    finally:
        # la foto auxiliar no debe quedarse en la página
        try:
            cere(f"{graph(cfg)}/{foto}?" + urllib.parse.urlencode({"access_token": token}),
                 urllib.parse.urlencode({"method": "delete"}).encode("utf-8"))
        except RuntimeError as e:
            log(f"aviso: no se pudo borrar la foto auxiliar {foto}: {e}")


def verifica(cfg):
    page, token = credentiale()
    cont = cont_instagram(cfg, page, token)
    stare = json.loads(STARE.read_text(encoding="utf-8")) if STARE.exists() else {}
    print(f"OK: @{cont['username']} ({cont['id']}), {len(stare)} publicaciones registradas")


def ruleaza(cfg, doar_urmatoarea):
    page, token = credentiale()
    stare = json.loads(STARE.read_text(encoding="utf-8")) if STARE.exists() else {}
    acum = dt.datetime.now()
    if doar_urmatoarea:
        viitoare = [x for x in postari() if x["cheie"] not in stare and dt.datetime.fromisoformat(x["ora"]) > acum]
        de_facut = viitoare[:1]
    else:
        de_facut = []
        for x in postari():
            ora = dt.datetime.fromisoformat(x["ora"])
            if x["cheie"] in stare or ora > acum:
                continue
            if acum - ora > INTARZIERE_MAXIMA:
                stare[x["cheie"]] = {"ora": x["ora"], "status": "sarit"}
                log(f"{x['ora']}  saltada (demasiado atrasada)")
                continue
            de_facut.append(x)
        de_facut = de_facut[:MAXIM_PE_RULARE]

    ig = cont_instagram(cfg, page, token)["id"] if de_facut else None
    for x in de_facut:
        try:
            media, link = publica(cfg, page, token, ig, x)
        except RuntimeError as e:
            log(f"{x['ora']}  ERROR {e}")
            break
        stare[x["cheie"]] = {"ora": x["ora"], "status": "publicat", "ig_id": media,
                             "publicat_la": dt.datetime.now().replace(microsecond=0).isoformat()}
        log(f"{x['ora']}  publicada {link}")
        STARE.write_text(json.dumps(stare, ensure_ascii=False, indent=1), encoding="utf-8")
    STARE.write_text(json.dumps(stare, ensure_ascii=False, indent=1), encoding="utf-8")


def main():
    p = argparse.ArgumentParser()
    p.add_argument("comanda", choices=["verifica", "publica-scadente", "publica-acum"])
    a = p.parse_args()
    cfg = json.loads((AICI / "config.json").read_text(encoding="utf-8"))
    try:
        if a.comanda == "verifica":
            verifica(cfg)
        else:
            with blocare():
                ruleaza(cfg, a.comanda == "publica-acum")
    except Exception as e:
        log(f"ERROR {type(e).__name__}: {e}")
        raise


if __name__ == "__main__":
    main()

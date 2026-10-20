"""Genera las publicaciones diarias "Pregunta del día" para Facebook.

Uso:
    python generar_postari.py --zile 7                 # desde mañana, 7 días
    python generar_postari.py --start 2026-10-22 --zile 14

Por cada día y hora de config.json elige una pregunta (rotando PNB/PER/PY/CY),
dibuja la imagen 1080x1350 con Chrome headless y añade la entrada a coada.json.
Las preguntas ya usadas (coada.json + publicate.json) no se repiten.
"""
import argparse
import datetime as dt
import hashlib
import html
import json
import random
import re
import subprocess
import sys
from pathlib import Path

AICI = Path(__file__).resolve().parent
OUT = AICI / "out"
COADA = AICI / "coada.json"
PUBLICATE = AICI / "publicate.json"

TEME = {
    1: "Nomenclatura náutica", 2: "Amarre y fondeo", 3: "Seguridad", 4: "Legislación",
    5: "Balizamiento", 6: "Reglamento (RIPA)", 7: "Maniobra y navegación",
    8: "Emergencias en la mar", 9: "Meteorología", 10: "Teoría de la navegación",
    12: "Seguridad en la mar", 13: "Meteorología", 14: "Teoría de navegación",
    16: "Meteorología", 17: "Inglés", 18: "Teoría de navegación",
}
# grupo de rotación -> (temas, texto de la etiqueta)
GRUPE = {
    "PNB": (range(1, 7), "PNB · PER"),
    "PER": (range(7, 11), "PER"),
    "PY": (range(12, 15), "Patrón de Yate"),
    "CY": (range(16, 19), "Capitán de Yate"),
}
HASHTAGS = {
    "PNB": "#PNB #PER #TituloNautico #Navegacion #Nautica",
    "PER": "#PER #PatronDeEmbarcacionesDeRecreo #TituloNautico #Navegacion",
    "PY": "#PatronDeYate #TituloNautico #Navegacion #Vela",
    "CY": "#CapitanDeYate #TituloNautico #Navegacion #Vela",
}
LITERE = "ABCD"
# preguntas que dependen de una figura, carta o anexo que no sale en la imagen
DEPINDE_DE_FIGURA = re.compile(
    r"figura|imagen|dibujo|croquis|ilustraci|gráfico|grafico|carta n|carta de navegaci|siguiente (marca|señal|baliza)|anexo",
    re.I,
)


def cheie(q):
    return hashlib.sha1((q["text"] + "|" + "|".join(q["answers"])).encode("utf-8")).hexdigest()[:16]


def incarca_intrebari(cfg):
    folder = Path(cfg["folder_teme"])
    toate = []
    for rel in cfg["surse"]:
        for q in json.loads((folder / rel).read_text(encoding="utf-8-sig")):
            if q.get("questionImageUrl") or len(q.get("answers", [])) != 4:
                continue
            if not 1 <= q.get("correctIndex", 0) <= 4 or q.get("topicNumber") not in TEME:
                continue
            if len(q["text"]) > 260 or max(len(a) for a in q["answers"]) > 120:
                continue
            if DEPINDE_DE_FIGURA.search(q["text"]):
                continue
            toate.append(q)
    return toate


def explicatie_scurta(text):
    if not text:
        return ""
    paragraf = text.strip().split("\n\n")[0]
    paragraf = re.sub(r"\*\*|__|`", "", paragraf).strip()
    if len(paragraf) <= 420:
        return paragraf
    taiat = paragraf[:420]
    punct = taiat.rfind(". ")
    return taiat[: punct + 1] if punct > 150 else taiat.rstrip() + "…"


def marimi(q):
    n = len(q["text"])
    lung = max(len(a) for a in q["answers"])
    intrebare = 54 if n < 90 else 48 if n < 150 else 42 if n < 210 else 38
    raspuns = 34 if lung < 50 else 31 if lung < 85 else 28
    return intrebare, raspuns


def html_postare(q, eticheta):
    fi, fr = marimi(q)
    raspunsuri = "".join(
        f'<div class="opt"><span class="lit">{LITERE[i]}</span><span>{html.escape(a)}</span></div>'
        for i, a in enumerate(q["answers"])
    )
    return f"""<!doctype html><html><head><meta charset="utf-8">
<link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@500;600;800&display=block" rel="stylesheet">
<style>
html,body{{margin:0;padding:0}}
body{{width:1080px;height:1350px;overflow:hidden;background:#0B3D5C;font-family:"Plus Jakarta Sans","Segoe UI",sans-serif;color:#fff;position:relative}}
.top{{display:flex;align-items:center;justify-content:space-between;padding:56px 64px 0}}
.brand{{display:flex;align-items:center;gap:16px;font-size:40px;font-weight:800;letter-spacing:-1px}}
.brand svg{{width:58px;height:58px}}
.brand span{{color:#3CC2BC}}
.badge{{background:#3CC2BC;color:#0B3D5C;font-weight:800;font-size:28px;padding:10px 24px;border-radius:999px}}
.main{{position:absolute;left:64px;right:64px;top:170px;bottom:150px;display:flex;flex-direction:column;justify-content:center}}
.tema{{color:#3CC2BC;font-size:28px;font-weight:600;text-transform:uppercase;letter-spacing:2px;margin-bottom:22px}}
.q{{font-size:{fi}px;font-weight:800;line-height:1.22;margin-bottom:44px}}
.opt{{display:flex;align-items:center;gap:24px;background:#14506F;border-radius:22px;padding:22px 28px;margin-bottom:18px;font-size:{fr}px;font-weight:500;line-height:1.25}}
.lit{{flex:0 0 58px;height:58px;border-radius:50%;background:#0B3D5C;border:3px solid #3CC2BC;color:#3CC2BC;display:flex;align-items:center;justify-content:center;font-weight:800;font-size:30px}}
.foot{{position:absolute;left:64px;right:64px;bottom:56px;display:flex;justify-content:space-between;align-items:center;font-size:30px;font-weight:600}}
.foot .cta{{color:#3CC2BC}}
.foot .url{{color:#C9D8E2}}
</style></head><body>
<div class="top">
  <div class="brand"><svg viewBox="0 0 64 64"><polygon points="32,4 40,30 24,30" fill="#3CC2BC"/><polygon points="24,30 40,30 32,40" fill="#FFFFFF"/><path d="M8 49 q6 -5.5 12 0 t12 0 t12 0 t12 0" fill="none" stroke="#FFFFFF" stroke-width="4" stroke-linecap="round"/><path d="M8 58 q6 -5.5 12 0 t12 0 t12 0 t12 0" fill="none" stroke="#3CC2BC" stroke-width="4" stroke-linecap="round"/></svg><div>Navigation<span>ES</span></div></div>
  <div class="badge">{html.escape(eticheta)}</div>
</div>
<div class="main">
  <div class="tema">{html.escape(TEME[q["topicNumber"]])}</div>
  <div class="q">{html.escape(q["text"])}</div>
  {raspunsuri}
</div>
<div class="foot"><span class="cta">¿Cuál es la correcta? 👇</span><span class="url">navigationes.eu</span></div>
</body></html>"""


def descriere(q, grup, eticheta, cfg):
    corect = q["correctIndex"] - 1
    linii = [f"🧭 Pregunta del día · {eticheta} · {TEME[q['topicNumber']]}", "", q["text"], ""]
    linii += [f"{LITERE[i]}) {a}" for i, a in enumerate(q["answers"])]
    linii += ["", "¿Cuál es la correcta? Déjalo en los comentarios 👇", ".", ".", ".",
              f"✅ Respuesta correcta: {LITERE[corect]}) {q['answers'][corect]}"]
    expl = explicatie_scurta(q.get("explanation"))
    if expl:
        linii += ["", expl]
    linii += ["", f"📲 Practica con exámenes reales de tu comunidad: {cfg['site']}?{cfg['utm']}",
              "", HASHTAGS[grup]]
    return "\n".join(linii)


def randeaza(cfg, fisier_html, fisier_png):
    subprocess.run(
        [cfg["chrome"], "--headless=new", "--disable-gpu", "--hide-scrollbars",
         "--virtual-time-budget=4000", "--window-size=1080,1350",
         f"--screenshot={fisier_png}", fisier_html.as_uri()],
        check=True, capture_output=True,
    )
    if not fisier_png.exists():
        raise RuntimeError(f"Chrome no generó {fisier_png}")


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--start", help="primer día AAAA-MM-DD (por defecto: mañana)")
    p.add_argument("--zile", type=int, default=7)
    p.add_argument("--seed", type=int)
    a = p.parse_args()

    cfg = json.loads((AICI / "config.json").read_text(encoding="utf-8"))
    start = dt.date.fromisoformat(a.start) if a.start else dt.date.today() + dt.timedelta(days=1)
    coada = json.loads(COADA.read_text(encoding="utf-8")) if COADA.exists() else []
    publicate = json.loads(PUBLICATE.read_text(encoding="utf-8")) if PUBLICATE.exists() else []
    folosite = {x["cheie"] for x in coada} | {x["cheie"] for x in publicate}
    ocupate = {x["ora"] for x in coada} | {x["ora"] for x in publicate}

    rnd = random.Random(a.seed)
    toate = incarca_intrebari(cfg)
    pe_grup = {g: [q for q in toate if q["topicNumber"] in teme and cheie(q) not in folosite]
               for g, (teme, _) in GRUPE.items()}
    for lista in pe_grup.values():
        rnd.shuffle(lista)
    print("Preguntas disponibles: " + ", ".join(f"{g} {len(l)}" for g, l in pe_grup.items()))

    noi = 0
    for zi in range(a.zile):
        data = start + dt.timedelta(days=zi)
        for k, ora in enumerate(cfg["ore"]):
            moment = dt.datetime.combine(data, dt.time.fromisoformat(ora))
            if moment.isoformat() in ocupate:
                continue
            # en cada día la rotación empieza en un sitio distinto
            rot = cfg["rotatie"]
            grup = rot[(k + zi) % len(rot)]
            if not pe_grup[grup]:
                grup = max(pe_grup, key=lambda g: len(pe_grup[g]))
            q = pe_grup[grup].pop()
            eticheta = GRUPE[grup][1]
            folder = OUT / data.isoformat()
            folder.mkdir(parents=True, exist_ok=True)
            baza = folder / ora.replace(":", "")
            fh, fp = baza.with_suffix(".html"), baza.with_suffix(".png")
            fh.write_text(html_postare(q, eticheta), encoding="utf-8")
            randeaza(cfg, fh, fp)
            coada.append({
                "ora": moment.isoformat(), "grup": grup, "tema": q["topicNumber"],
                "cheie": cheie(q), "imagine": str(fp.relative_to(AICI)),
                "descriere": descriere(q, grup, eticheta, cfg), "status": "nou",
            })
            noi += 1
            print(f"  {moment:%Y-%m-%d %H:%M}  {grup:<3}  {q['text'][:70]}")

    coada.sort(key=lambda x: x["ora"])
    COADA.write_text(json.dumps(coada, ensure_ascii=False, indent=1), encoding="utf-8")
    print(f"{noi} publicaciones nuevas en coada.json")


if __name__ == "__main__":
    sys.exit(main())

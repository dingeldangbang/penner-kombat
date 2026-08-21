# 🚀 Release-Anleitung (GitHub)

So veröffentlichst du das Projekt als **GitHub-Release**.

## Voraussetzungen
- Ein GitHub-Repository (z.B. `penner-kombat`).
- Git lokal installiert, Unity-Projekt im Repo.
- Du hast mindestens einmal im Unity-Editor einen **Kontroll-Build** gefahren
  (damit Assets/Scenes/Preview korrekt sind).

## 1. GitHub-Repo anlegen
1. Auf GitHub: **New repository** → `penner-kombat`, **Public** oder **Private**.
2. NICHT „Initialize with README" (das README liegt schon im Repo).

## 2. Remote setzen & pushen
```bash
cd penner-kombat
git remote add origin https://github.com/<USER>/penner-kombat.git
git branch -M main
git push -u origin main
```

## 3. Tags setzen (semantische Versionierung)
```bash
git tag -a v1.3.0 -m "v1.3.0 — Trophäen, Soundtrack, Fatality-System, Editor-Wizard"
git push origin --tags
```

## 4. GitHub-Release erstellen
1. GitHub → Repo → **Releases → New release**.
2. Tag wählen (z.B. `v1.3.0`), Titel + Beschreibung (siehe `CHANGELOG.md`).
3. **ZIP-Anhang**: `penner-kombat-release.zip` hochladen (Skript unten).
4. Veröffentlichen.

## 4b. Ein-Klick-Push (empfohlen)
Ein fertiges Skript ist im Repo enthalten. Auf deinem Rechner (mit Token):
```bash
cd penner-kombat
GITHUB_TOKEN=ghp_xxxx ./Tools/push.sh
```
Das Skript setzt Remote, benennt den Branch auf `main` um, pusht Code + Tag
`v1.3.0` und entfernt das Token anschließend wieder aus der Remote-Config.


## 5. ZIP erzeugen (ohne .git)
```bash
cd <pfad-mit-penner-kombat>
rm -f penner-kombat-release.zip
zip -rq penner-kombat-release.zip penner-kombat -x "penner-kombat/.git/*"
```

## 6. Danach
- Erste Probleme sammeln: GitHub **Issues**.
- Mitwirkung: **CONTRIBUTING.md** verlinken.
- Eine **Project Roadmap** als Issue-Pinnwand oder `docs/ROADMAP.md` anlegen.

## Kontroll-Build vor dem Release
Im Unity-Editor:
- `Tools → Penner Kombat → Setup-Szene erzeugen`
- **File → Build Settings** → Android/PC, passende Plattform wählen,
  `Arena` als erste Szene, Build starten.
- `TestRunner`-Logs prüfen (✅/❌).

---

Alle dauerhaften Infos: [README.md](../README.md) · [docs/SETUP.md](SETUP.md)

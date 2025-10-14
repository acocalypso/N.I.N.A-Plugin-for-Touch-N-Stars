# TNS MessageBox - Quick Start Guide

## Installation

Die TNS MessageBox ist automatisch Teil des Touch 'N' Stars Plugins.

Nach dem Kompilieren findest du in NINA unter **"Sequencer → Add Item → Touch 'N' Stars"** das neue Item:
- **TNS Message Box**

## Verwendung in der Sequenz

### Schritt 1: Item zur Sequenz hinzufügen

1. Öffne den NINA Sequencer
2. Klicke auf **"Add Item"** (+ Button)
3. Navigiere zu **"Touch 'N' Stars"**
4. Wähle **"TNS Message Box"**

### Schritt 2: MessageBox konfigurieren

Im Item-Editor siehst du drei Bereiche:

#### **Message**
- Gib den Text ein, der in der MessageBox angezeigt werden soll
- Mehrzeilig möglich
- Beispiel: "Bitte Flat-Panel montieren und dann in der Touch-App auf 'Weiter' klicken"

#### **Timeout Settings**
- ☑ **Enable Auto-Close Timeout**: MessageBox schließt sich automatisch nach Zeit
- **Timeout (seconds)**: Zeit in Sekunden (1-3600, Standard: 60)
- ☑ **Continue sequence on timeout**:
  - ✅ = Sequenz läuft nach Timeout weiter
  - ❌ = Sequenz stoppt nach Timeout

#### **Remote Control**
- Info-Box mit API-Endpunkten
- Keine Einstellungen nötig

### Schritt 3: Sequenz starten

Wenn die Sequenz das TNS MessageBox Item erreicht:
1. Eine MessageBox erscheint mit deinem Text
2. In den NINA-Logs erscheint die MessageBox-ID
3. Die MessageBox ist jetzt über die API steuerbar

## API Verwendung - Einfache Beispiele

### 1. Prüfen ob MessageBoxes aktiv sind

```bash
curl http://localhost:5000/messagebox/count
```

**Antwort:**
```json
{
  "Response": {
    "Count": 1,
    "HasActive": true
  }
}
```

### 2. Alle MessageBoxes auflisten

```bash
curl http://localhost:5000/messagebox/list
```

**Antwort:**
```json
{
  "Response": {
    "Count": 1,
    "MessageBoxes": [
      {
        "Id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        "Text": "Bitte Flat-Panel montieren",
        "CreatedAt": "2025-10-14T20:30:00",
        "Age": "00:02:15"
      }
    ]
  }
}
```

### 3. MessageBox schließen und Sequenz fortsetzen

```bash
curl -X POST http://localhost:5000/messagebox/close-all \
  -H "Content-Type: application/json" \
  -d '{"continue": true}'
```

### 4. MessageBox schließen und Sequenz stoppen

```bash
curl -X POST http://localhost:5000/messagebox/close-all \
  -H "Content-Type: application/json" \
  -d '{"continue": false}'
```

## Typische Anwendungsfälle

### Use Case 1: Warte auf manuellen Schritt

**Szenario:** Du willst Flats aufnehmen und musst vorher das Flat-Panel montieren.

**Sequenz:**
1. Normale Target-Aufnahme
2. **TNS MessageBox**: "Flat-Panel montieren"
   - Timeout: 600 Sekunden (10 Min)
   - Continue on timeout: ❌ (Abbruch falls du's vergisst)
3. Flat-Aufnahme

**Ablauf:**
1. Nach der Target-Aufnahme erscheint die MessageBox
2. Du montierst das Panel
3. In der Touch-App klickst du auf "Continue"
4. API-Call: `/messagebox/close-all` mit `continue: true`
5. Flat-Aufnahme startet

### Use Case 2: Remote Check-Point

**Szenario:** Du willst vor einem kritischen Schritt nochmal prüfen, ob alles okay ist.

**Sequenz:**
1. Equipment Connect
2. Slew to Target
3. **TNS MessageBox**: "Prüfe Wetter und Equipment - Fortsetzen?"
   - Timeout: 300 Sekunden
   - Continue on timeout: ✅ (Auto-Continue)
4. Take Exposure

**Ablauf:**
1. MessageBox erscheint nach Slew
2. Du prüfst in der Touch-App Wetter, Guiding, Focus
3. Option A: Alles okay → `/messagebox/close-all` mit `continue: true`
4. Option B: Problem erkannt → `/messagebox/close-all` mit `continue: false`

### Use Case 3: Notifications mit Touch-App

**Szenario:** Du willst benachrichtigt werden, wenn ein bestimmter Schritt erreicht wird.

**Sequenz:**
1. Long-Running Task (z.B. 100x 5min Exposures)
2. **TNS MessageBox**: "Session Mitte erreicht - Check nötig?"
   - Timeout: 120 Sekunden
   - Continue on timeout: ✅ (Auto-Continue falls du schläfst)
3. Restliche Exposures

**Touch-App Integration:**
```javascript
// Polling für neue MessageBoxes
setInterval(async () => {
  const response = await fetch('http://localhost:5000/messagebox/count');
  const data = await response.json();

  if (data.Response.HasActive) {
    // Zeige Notification in Touch-App
    showNotification('NINA wartet auf dich!');

    // Lade MessageBox Details
    const boxes = await fetch('http://localhost:5000/messagebox/list').then(r => r.json());
    displayMessageBoxPanel(boxes.Response.MessageBoxes);
  }
}, 5000); // Alle 5 Sekunden
```

## Touch-App UI Beispiel

### Einfacher Button zum Schließen

```html
<button id="close-all-btn" style="display: none;" onclick="closeAll()">
  Close All MessageBoxes & Continue
</button>

<script>
  async function checkMessageBoxes() {
    const res = await fetch('http://localhost:5000/messagebox/count');
    const data = await res.json();

    const btn = document.getElementById('close-all-btn');
    btn.style.display = data.Response.HasActive ? 'block' : 'none';
  }

  async function closeAll() {
    await fetch('http://localhost:5000/messagebox/close-all', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ continue: true })
    });
    checkMessageBoxes();
  }

  // Check alle 3 Sekunden
  setInterval(checkMessageBoxes, 3000);
</script>
```

### Erweiterte UI mit Details

```html
<div id="msgbox-panel" style="display: none;">
  <h2>Active Message Boxes</h2>
  <div id="msgbox-list"></div>
</div>

<script>
  async function updateMessageBoxPanel() {
    const res = await fetch('http://localhost:5000/messagebox/list');
    const data = await res.json();

    const panel = document.getElementById('msgbox-panel');
    const list = document.getElementById('msgbox-list');

    if (data.Response.Count > 0) {
      panel.style.display = 'block';
      list.innerHTML = data.Response.MessageBoxes.map(box => `
        <div class="msgbox-card">
          <h3>${box.Text}</h3>
          <p>Age: ${box.Age}</p>
          <button onclick="closeBox('${box.Id}', true)">
            Continue ✅
          </button>
          <button onclick="closeBox('${box.Id}', false)">
            Stop Sequence ❌
          </button>
        </div>
      `).join('');
    } else {
      panel.style.display = 'none';
    }
  }

  async function closeBox(id, shouldContinue) {
    await fetch(`http://localhost:5000/messagebox/close/${id}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ continue: shouldContinue })
    });
    updateMessageBoxPanel();
  }

  setInterval(updateMessageBoxPanel, 2000);
</script>
```

## Troubleshooting

### MessageBox erscheint nicht in der Sequenz

**Problem:** Das Item ist nicht verfügbar.

**Lösung:**
1. Stelle sicher, dass das Plugin kompiliert wurde
2. Prüfe ob die DLL in `%LOCALAPPDATA%\NINA\Plugins\3.0.0\Touch 'N' Stars\` liegt
3. Starte NINA neu

### MessageBox kann nicht über API geschlossen werden

**Problem:** API-Call gibt 404 zurück.

**Ursachen:**
1. MessageBox wurde bereits geschlossen (natürlich oder Timeout)
2. Falsche ID verwendet
3. Plugin nicht aktiv

**Lösung:**
```bash
# Prüfe aktive MessageBoxes
curl http://localhost:5000/messagebox/list

# Nutze die aktuelle ID aus der Response
```

### Sequenz stoppt nicht trotz "continue: false"

**Problem:** Sequenz läuft weiter obwohl `continue: false` gesendet wurde.

**Debugging:**
1. Prüfe NINA Logs: `[INFO] MessageBox ... closed via API - Continue: false`
2. Stelle sicher, dass die MessageBox noch aktiv war
3. Prüfe ob Timeout aktiviert war und zuerst feuerte

### Touch-App findet keine MessageBoxes

**Problem:** `/messagebox/count` gibt immer `Count: 0` zurück.

**Checkliste:**
1. Ist die Sequenz gestartet und das TNS MessageBox Item aktiv?
2. Richtige Port-Nummer? Prüfe TNS Plugin-Einstellungen
3. API erreichbar? Test mit `curl http://localhost:5000/version`

## Best Practices

1. **Immer Timeout setzen**: Verhindert, dass die Sequenz ewig hängt
2. **Aussagekräftige Texte**: "Flat-Panel montieren" statt "Warten..."
3. **Polling-Intervall**: 2-5 Sekunden reichen, nicht jede Sekunde abfragen
4. **Error Handling**: 404 ist normal wenn Box schon geschlossen ist
5. **Logging nutzen**: Schaue in die NINA Logs für MessageBox-IDs

## Weiterführende Dokumentation

Siehe [TNSMessageBox_API_Documentation.md](TNSMessageBox_API_Documentation.md) für:
- Vollständige API-Referenz
- Erweiterte Beispiele
- JavaScript Integration Details
- Technische Implementierungsdetails

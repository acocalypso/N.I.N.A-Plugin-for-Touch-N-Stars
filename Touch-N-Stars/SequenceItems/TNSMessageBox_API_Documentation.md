# TNS MessageBox - API Documentation

## Übersicht

Das TNS MessageBox Sequenz-Item ermöglicht es, Message-Boxen in der NINA-Sequenz anzuzeigen, die **remote über die Touch 'N' Stars API geschlossen** werden können.

## Features

- ✅ MessageBox in Sequenz mit benutzerdefiniertem Text
- ✅ Remote-Steuerung über TNS API (Schließen per API-Call)
- ✅ Auto-Timeout mit konfigurierbarer Zeit
- ✅ Wählbar: Sequenz fortsetzen oder stoppen beim Schließen
- ✅ Globales Registry für alle aktiven MessageBoxes
- ✅ Mehrere MessageBoxes gleichzeitig möglich

## Sequenz-Item Einstellungen

### Properties

| Property | Typ | Beschreibung |
|----------|-----|--------------|
| `Text` | string | Der anzuzeigende Nachrichtentext |
| `CloseOnTimeout` | bool | Aktiviert Auto-Close nach Zeit |
| `TimeoutSeconds` | int | Timeout in Sekunden (1-3600) |
| `ContinueOnTimeout` | bool | Bei Timeout: Sequenz fortsetzen (true) oder stoppen (false) |

## API Endpunkte

### 1. Liste aller aktiven MessageBoxes

**GET** `/messagebox/list`

**Response:**
```json
{
  "Success": true,
  "Response": {
    "Count": 2,
    "MessageBoxes": [
      {
        "Id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        "Text": "Warte auf Flat-Panel",
        "CreatedAt": "2025-10-14T20:30:00",
        "Age": "00:05:23"
      }
    ]
  },
  "StatusCode": 200,
  "Type": "MessageBoxList"
}
```

### 2. MessageBox schließen (spezifische ID)

**POST** `/messagebox/close/{id}`

**URL Parameter:**
- `{id}`: GUID der MessageBox (aus `/messagebox/list`)

**Optional Body:**
```json
{
  "continue": true
}
```

**Parameter:**
- `continue` (bool, optional, default: `true`)
  - `true`: Sequenz läuft weiter
  - `false`: Sequenz wird gestoppt

**Response (Success):**
```json
{
  "Success": true,
  "Response": {
    "Message": "MessageBox a1b2c3d4-... closed",
    "ContinueSequence": true
  },
  "StatusCode": 200,
  "Type": "MessageBoxClosed"
}
```

**Response (Not Found):**
```json
{
  "Success": false,
  "Error": "MessageBox with ID ... not found",
  "StatusCode": 404,
  "Type": "NotFound"
}
```

### 3. Alle MessageBoxes schließen

**POST** `/messagebox/close-all`

**Optional Body:**
```json
{
  "continue": false
}
```

**Response:**
```json
{
  "Success": true,
  "Response": {
    "Message": "Closed 3 message box(es)",
    "Count": 3,
    "ContinueSequence": true
  },
  "StatusCode": 200,
  "Type": "MessageBoxesClosed"
}
```

### 4. Info über spezifische MessageBox

**GET** `/messagebox/info/{id}`

**Response:**
```json
{
  "Success": true,
  "Response": {
    "Id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "Text": "Warte auf Flat-Panel",
    "CreatedAt": "2025-10-14T20:30:00",
    "Age": "00:05:23",
    "IsActive": true
  },
  "StatusCode": 200,
  "Type": "MessageBoxInfo"
}
```

### 5. Anzahl aktiver MessageBoxes

**GET** `/messagebox/count`

**Response:**
```json
{
  "Success": true,
  "Response": {
    "Count": 2,
    "HasActive": true
  },
  "StatusCode": 200,
  "Type": "MessageBoxCount"
}
```

## Beispiel-Workflows

### Szenario 1: Warte auf Flat-Panel

**Sequenz:**
1. TNS MessageBox: "Bitte Flat-Panel montieren"
2. User montiert Panel
3. Über Touch-App: Rufe `/messagebox/close-all` mit `"continue": true`
4. Sequenz läuft weiter mit Flat-Aufnahme

### Szenario 2: Remote-Abbruch bei Problem

**Sequenz:**
1. TNS MessageBox: "Prüfe Weather Station"
2. User prüft Wetter, stellt Problem fest
3. Über API: `/messagebox/close-all` mit `"continue": false`
4. Sequenz wird komplett gestoppt

### Szenario 3: Auto-Timeout

**Sequenz-Einstellungen:**
- `CloseOnTimeout`: true
- `TimeoutSeconds`: 300 (5 Minuten)
- `ContinueOnTimeout`: true

**Ablauf:**
1. MessageBox erscheint
2. Nach 5 Minuten: Auto-Close
3. Sequenz läuft automatisch weiter

## Curl Beispiele

### Alle MessageBoxes auflisten
```bash
curl http://localhost:5000/messagebox/list
```

### Spezifische MessageBox schließen (Sequenz fortsetzen)
```bash
curl -X POST http://localhost:5000/messagebox/close/a1b2c3d4-e5f6-7890-abcd-ef1234567890 \
  -H "Content-Type: application/json" \
  -d '{"continue": true}'
```

### Alle MessageBoxes schließen (Sequenz stoppen)
```bash
curl -X POST http://localhost:5000/messagebox/close-all \
  -H "Content-Type: application/json" \
  -d '{"continue": false}'
```

### Anzahl aktiver MessageBoxes abfragen
```bash
curl http://localhost:5000/messagebox/count
```

## JavaScript Beispiel (Touch-App)

```javascript
// Alle MessageBoxes auflisten
async function listMessageBoxes() {
  const response = await fetch('http://localhost:5000/messagebox/list');
  const data = await response.json();
  console.log('Active MessageBoxes:', data.Response.MessageBoxes);
  return data.Response.MessageBoxes;
}

// Spezifische MessageBox schließen
async function closeMessageBox(id, continueSequence = true) {
  const response = await fetch(`http://localhost:5000/messagebox/close/${id}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ continue: continueSequence })
  });
  const data = await response.json();
  console.log('Closed:', data);
  return data.Success;
}

// Alle schließen mit "Sequenz fortsetzen"
async function closeAllAndContinue() {
  const response = await fetch('http://localhost:5000/messagebox/close-all', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ continue: true })
  });
  const data = await response.json();
  console.log(`Closed ${data.Response.Count} boxes`);
}

// Alle schließen mit "Sequenz stoppen"
async function closeAllAndStop() {
  const response = await fetch('http://localhost:5000/messagebox/close-all', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ continue: false })
  });
  const data = await response.json();
  console.log(`Stopped sequence, closed ${data.Response.Count} boxes`);
}

// Polling: Warte auf MessageBox
async function waitForMessageBox() {
  while (true) {
    const response = await fetch('http://localhost:5000/messagebox/count');
    const data = await response.json();

    if (data.Response.HasActive) {
      console.log(`${data.Response.Count} MessageBox(es) aktiv`);
      return true;
    }

    await new Promise(resolve => setTimeout(resolve, 1000)); // 1s warten
  }
}
```

## Integration in Touch-App UI

### Beispiel Button-Design

```html
<div id="messagebox-panel" style="display: none;">
  <h3>Active Message Boxes</h3>
  <div id="messagebox-list"></div>
  <button onclick="closeAllAndContinue()">Close All & Continue</button>
  <button onclick="closeAllAndStop()">Close All & Stop Sequence</button>
</div>

<script>
  // Polling für aktive MessageBoxes
  setInterval(async () => {
    const boxes = await listMessageBoxes();
    const panel = document.getElementById('messagebox-panel');
    const list = document.getElementById('messagebox-list');

    if (boxes.length > 0) {
      panel.style.display = 'block';
      list.innerHTML = boxes.map(box => `
        <div class="msgbox-item">
          <p>${box.Text}</p>
          <small>Age: ${box.Age}</small>
          <button onclick="closeMessageBox('${box.Id}', true)">Continue</button>
          <button onclick="closeMessageBox('${box.Id}', false)">Stop</button>
        </div>
      `).join('');
    } else {
      panel.style.display = 'none';
    }
  }, 2000); // Alle 2 Sekunden prüfen
</script>
```

## Logging

Das TNS MessageBox Item loggt alle wichtigen Events:

```
[INFO] TNS MessageBox displayed with ID: a1b2c3d4-...
[INFO] MessageBox a1b2c3d4-... closed via API - Continue: true
[INFO] TNS MessageBox: User cancelled - Stopping Sequence
[INFO] TNS MessageBox timeout after 60 seconds
[INFO] Closed 3 MessageBoxes via API - Continue: false
```

## Fehlerbehandlung

### MessageBox existiert nicht mehr
Wenn die MessageBox bereits geschlossen wurde, gibt die API einen 404-Fehler zurück:
```json
{
  "Success": false,
  "Error": "MessageBox with ID ... not found",
  "StatusCode": 404,
  "Type": "NotFound"
}
```

### Keine aktiven MessageBoxes
Bei `/messagebox/close-all` wird einfach `Count: 0` zurückgegeben (kein Fehler).

## Best Practices

1. **Polling**: Prüfe alle 1-2 Sekunden auf aktive MessageBoxes
2. **ID Speichern**: Speichere MessageBox IDs für gezielte Steuerung
3. **Timeout nutzen**: Setze immer einen Timeout als Fallback
4. **User Feedback**: Zeige in der Touch-App deutlich an, wenn MessageBoxes aktiv sind
5. **Error Handling**: Behandle 404-Fehler (Box bereits geschlossen) graceful

## Technische Details

### Thread-Safety
Die `MessageBoxRegistry` nutzt `ConcurrentDictionary` für thread-sichere Operationen.

### Lifecycle
- **Register**: Beim `Execute()` Start
- **Unregister**: Beim natürlichen Schließen oder API-Close
- **Cleanup**: Automatisch bei Sequenz-Abbruch

### Cancellation Token
Die MessageBox respektiert den Sequencer `CancellationToken` und registriert auch Timeout-Tokens.

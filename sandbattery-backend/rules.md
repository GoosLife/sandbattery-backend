# Sandbatteri – Systemregler

> **Hovedregel: Sikkerhed > Økonomi. Altid.**

---

## 🔥 Temperaturregler

| Betingelse | Status | Handling |
|---|---|---|
| Sand temp > 70°C | CRITICAL | Varmelegeme SLUKKET. Pumpe TÆNDT. Kan ikke genstartes (auto eller manuelt). |
| Sand temp ≤ 70°C | OK | Normal drift genoptages. |
| Sand temp < 0°C | WARNING | Markeres som outlier. Ingen handling. |
| Sensor læser -127°C | ERROR | Sensor afbrudt. Markeres som fejl. |

---

## 💧 Pumperegler

| Betingelse | Handling |
|---|---|
| Temp > 70°C | Pumpe TVANGSSTARTET, kører til temp ≤ 70°C |
| Pumpe ON + flow = 0 i 30 sek | Sensor markeres WARNING |
| Manuel brugerkommando | Pumpe adlyder (medmindre temperaturreglen tilsidesætter) |
| Minimumstemperatur ikke nået | Pumpe starter IKKE automatisk |

---

## ♨️ Varmelegemeregler

| Betingelse | Handling |
|---|---|
| Temp > 70°C | Varmelegeme BLOKERET (manuel + auto) |
| Pris < brugerdefineret grænse | Varmelegeme auto ON |
| Temp ≥ brugerdefineret maks | Varmelegeme auto OFF |
| Manuel kommando | Varmelegeme adlyder (medmindre blokeret) |

---

## 📡 Sensor / Kommunikationsregler

| Betingelse | Handling |
|---|---|
| Ingen temperaturdata i 30 sek | Sensor markeres OFFLINE |
| Ingen API-succes i 60 sek | Arduino WATCHDOG → system reset, sikker tilstand |
| WiFi mistet | Genforbindelsesforsøg hvert 5. sekund |
| OPTA mister kommunikation | `safeState()` → alle varmeelementer OFF, pumpe OFF, derefter reset |

---

## 🤖 Automatiseringsregler

| Betingelse | Handling |
|---|---|
| Elprishentning | Automatisk én gang i timen |
| Pris < grænse OG temp < brugermaks | Varmelegeme auto ON |
| Temp ≥ brugermaks | Varmelegeme auto OFF |

---

## 📊 Dataregler

| Regel | Værdi |
|---|---|
| Sensor POST-interval | Hvert 10. sekund |
| Heartbeat-interval | Hvert 30. sekund |
| Maks forsinkelse UI-opdatering | 15 sek (95% af tilfældene) |
| Events gemt i DB | Inden for 2 sekunder |

---

## 📋 Sensormålingers gyldighed (DS18B20)

| Måling | Status | Fejlbesked |
|---|---|---|
| -127°C | ERROR / CRITICAL | "Temperaturmåler ikke forbundet" |
| < 0°C | WARNING | "Temperaturen er meget lav – tjek om målingerne passer" |
| > 70°C | CRITICAL | "Maks temperatur overskredet. Sandbatteriet nedkøles." |
| Normal interval | OK | — |

---

## 🔁 Kildeoversigt

| Regel håndteret af | System |
|---|---|
| Temperaturaflæsning & POST | `sandbatteri_r4.ino` (Arduino UNO R4) |
| Relay-styring (varmelegeme + pumpe) | `fuckoff.ino` (Arduino OPTA) |
| Forretningslogik & automatisering | Backend API |
| Visning & manuel styring | Frontend dashboard |

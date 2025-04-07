# Selbstfahrendes-Auto

Dieses Projekt befasst sich mit einem ferngesteuerten Auto, das mithilfe von Ultraschallsensoren, einem Kompass, einem Raspberry Pi und einem neuronalen Netzwerk selbständig fahren soll. Das Ziel ist es, Hindernissen auszuweichen, einem vorgegebenen Ziel zu folgen und die Geschwindigkeit dynamisch anzupassen.

---

## Funktionen und Komponenten

### Ultraschallsensoren
- Scannen die Umgebung in einem 150°-Winkel, ähnlich wie Raycasts in Unity.
- Wenn ein Objekt erkannt wird, zählt es als Hindernis – außer es befindet sich in großer Entfernung.

### Kompass / GPS
- Liefert dem neuronalen Netzwerk Informationen über die Zielposition.

### Raspberry Pi
- Verarbeitet die Sensordaten.
- Steuert das neuronale Netzwerk und kommuniziert mit dem ESC (Electronic Speed Controller).

### Neuronales Netzwerk
- **Input-Layer:** 10 Neuronen (9 für die Ultraschallsensoren, 1 für das Kompassmodul).
- **Hidden-Layer:** 2 Schichten mit jeweils 20 Neuronen.
- **Output-Layer:** 1 Neuron für die Lenkung.
- Zwei Versionen existieren:
  - **Version in Unity:** Zum Trainieren des Netzwerks.
  - **Version für das Auto:** Steuert letztendlich das Fahrzeug.

![Screenshot der Netzwerkarchitektur](/Bilder/Netzwerk.png)

---

## Fehler und Ideen

- **Geschwindigkeitsregelung:** Das neuronale Netzwerk passt die Geschwindigkeit an.
- **Verbindung:** Sicherstellung einer stabilen Verbindung zwischen dem Raspberry Pi und dem ESC.
- **Magnet:** Optimierung der Suche nach dem richtigen Magneten.

---

## Statistiken

- **Lenkung:** Berechnet als Werte zwischen -1 und 1, was einer Lenkung von rund 75° in jede Richtung entspricht.
- **Geschwindigkeit:** Initial auf 5% festgelegt, wird aber zukünftig dynamisch durch das neuronale Netzwerk geregelt.

---

## Aufbau

- **Funkmodul:** Muss ausgebaut werden. Der Raspberry Pi wird direkt mit dem ESC-Controller verbunden, um Motorbefehle zu übermitteln.
- **Ultraschallsensoren:** Werden vorne am Auto angebracht und mit dem Raspberry Pi verbunden.
- **Raspberry Pi:** Wird, falls nötig, zusammen mit einer Powerbank am Wagen befestigt.

![Foto des Autos](/Bilder/Auto.jpg)

---

## Einkaufliste

- **Raspberry Pi 4 (4GB RAM) - 70$**  
  [Zum Angebot](https://www.amazon.de/Raspberry-Pi-4595-Modell-GB/dp/B09TTNF8BT/ref=sr_1_3?__mk_de_DE=%C3%85M%C3%85%C5%BD%C3%95%C3%91&crid=20RZ2OGLKV4JG&dib=eyJ2IjoiMSJ9.7lI8eeW17wtaJWRLUUnFSeZYnZEyX6ROAEKWFeaQ9n9dPGCY11-XEk-CdXNX5lIS0_DkN2zJrK1aOGz5eabvusnmOTDAOv4-f3piFnFwF26ekmPRWHCQoXcotUhX3k9mP-zkbc2D3i6-j1NoblLBCmZS_o8VJYSSzDGdzyhxXvzQiV5DSIVuqWyGaJlLDYiKiGQNX0eA-qpZQF7IWfdiB7G-BvjJCaUAUoz6-tssrSY.j5EROsbGoKeSm_vPQnDVdbm7TVnQTYToFoRyvBxPZGY)

- **PMW-Modul - 7$**  
  [Zum Angebot](https://www.amazon.de/DollaTek-PCA9685-16-Kanal-12-Bit-PWM-Servomotortreiber-I2C-Modulroboter/dp/B0BKZC1XWR/ref=sr_1_3?__mk_de_DE=%C3%85M%C3%85%C5%BD%C3%95%C3%91&crid=1S4W6ICWZ5324&dib=eyJ2IjoiMSJ9.UEsDr0HUBMBS2HJTR02yGB-i7MJkZ3-QjpW-pUxQJDSPt9slgkYRqnIIwMRhuMW-1zV9tq9GBVYnXNKKWH4SiJWyjbtgutZg2R1WBRSqJ30lzHsqnpFIupsnrl672RPT3XfExR5fjQrOInqpe2qSmQM_Q5FEBJpgQ6Dc9C5jOvm1BNU5ghLvACugBfOCgcoXnIfk_7WMLf9eCaKGLyGmp0HUEqJ6N_ua8ZzeRbVDJRSU_gfAWsAojmVntb78dpnLMvnSxvoxJUIi2D85OZbayxGg_9AkfSA7ij2kOTbyPfLdts-90QLRixPl69wLbyrj8cYqCaisSSArF8_y9lHclAWzMMcK_Vtn8z5xxX7AATZ0sxBN_QiwNsUcMc3oj1kBFI6Lo1kF0O9OSt2B0N1FDW8jo6w9SOYKkEwnHxXzqYRJp9ZR9kYw3XjwfYDNJfsP.RrlJFT36xjfFwO7TX8bxrQdL4_0jJO4yJj_UQn_CeLM)

- **Jumper-Kabel - 7$**  
  [Zum Angebot](https://www.amazon.de/Female-Female-Male-Female-Male-Male-Steckbr%C3%BCcken-Drahtbr%C3%BCcken-bunt/dp/B01EV70C78/ref=sr_1_5?__mk_de_DE=%C3%85M%C3%85%C5%BD%C3%95%C3%91&crid=3VXZEL9KXLEGR&dib=eyJ2IjoiMSJ9.QMvsCv-OL1-GLDfzEN_1j6Yugk5MKgqKrVzEUw9u-wdQoeLVA3NSaef8Crii9CSLZo7lHvBL9YoQFreAS6HWhwy02QpDvlKx5siDiHHsR6_Mwh0BXHpX8-EH-hFYU9-moR1JxhlIVKpc-5IkuFWVsjcrsSrTt-C6BWGc8YkTTo2Ry9fuVw12M7DlqzymOymeuuFSAEXmLJsOy8KhC-ktqxSSi0Mi2Jz3-kNTwrR-uQ3NV-yE-GFLCjPxjLChD0d-VW1EZp-L9fbkZ4ZTNbqtG2qsWIWBy8fHwwPNWhlGVEXZxn1C63mu0beC5ZQ3qNe4r9dZkcQCl52tUAcqfnuj77Pwag4FBdOtaKZNxRzy0swGQIMfSjR_XWNKzTQoFFfVAi06Z6E9RMXX5cVHcAel9SaqwYEXz1YSLZTzvdqbacWZBn_ryWXqionTPOJiDtKi)

---

### Weitere Komponenten

- **MicroSD-Karte (64GB) - 9$**  
  [Zum Angebot](https://www.amazon.de/SanDisk-Speicherkarte-Smartphones-Actionkameras-%C3%9Cbertragung/dp/B09X7C7LL1/ref=sr_1_2?__mk_de_DE=%C3%85M%C3%85%C5%BD%C3%95%C3%91&crid=1ZSHGZWU7VHCB&dib=eyJ2IjoiMSJ9.b0CJP71YPIYTiIzJ79WrvtoP6ve1w9XBnzBY5CVPzdXOb_0czlLJad7ESxRUc36kjRyBuadn8WE6yPzP5xOjF6BhyVhf4DeVq8PQoP41UocrCHbTcjOMsETGghfRImGfwZI0s62Mnmv5Y9FV-aYGvCfYn4VeFxdF1IQZrjDICoy2m-5iVc4pIOeJiA6T7x3H0nVwjYXSgYdwWPV0AoiZtRkESpfVPUoDfVNq5qWWPCY.e_bc8fnvfev7zhnouWlgSsd6hLOKP6nbZG7pRm6F8PY)

- **SD-Kartenleser - 6$**  
  [Zum Angebot](https://www.amazon.de/UGREEN-Kartenleser-MacBook-Samsung-Aluminum/dp/B07D1J88CF/ref=sr_1_3?__mk_de_DE=%C3%85M%C3%85%C5%BD%C3%95%C3%91&crid=3BINMGKFADIRS&dib=eyJ2IjoiMSJ9.4W9jlQm7wEGXzTM8lXBobBYBN9TqHK4LglbfSJ-gzIv5VeW1ZIyLAsm4mEjMLsqgfYUwvfFml6pm5lQv3UCdwuJKTfniL5KTVu2CPNUw9X4c7AkYyeRf6KJYkOrJYfVcN9VtxtOU6d3HP0ZF8lp6bJklJk5d_V9mz1snMuYVtYiiCFkmXgLWSq_N4Jkq0BH7-tdgyVipSZIG0coguv4rLMuXTcw8ujlFvW5sFQ49vKY.XhGr7YQNWEF-uVtN5jawd0kTQnban7OfgiDJUkODNJE)

---

- **9x Ultraschallsensoren (HC-SR04) - 2$ (pro 2x)**  
  [Zum Angebot](https://www.amazon.de/HC-SR04-Ultraschall-Sensor-Ultraschallsensor-Arduino-MEGA2560/dp/B07XF4815H/ref=sr_1_1_sspa?__mk_de_DE=%C3%85M%C3%85%C5%BD%C3%95%C3%91&crid=3Q0J9FUAI1W14&dib=eyJ2IjoiMSJ9.-MRldPbjOgxzgGi7I9xXCs3SCvElOmieJ2l0hUO3Sw57CphjM5mNzz7gs0KQxYyzNZUGOC9kiQtgmnCvytIKWTiOIemNlSJXL_uxpKX70KFYOTS_ym_SbV4tvrFrt4s9ru-1hBmQLGrzqHQQS_TFfAskLHWaheylqyOx_FWRczCXwPbWFgQAxqB-Z3y4o885hTS7FZsjVCfVeWFn6O9l3D1ucTDD4HDzR7MXyPor1HqI8pbcQW_FB9No-q63Kvh_ca2UUfk5Ccprrde30HSa19zWzqEerojHeRMAwK2im1k)

- **Kompassmodul - 8$**  
  [Zum Angebot](https://www.amazon.de/DAOKAI-Magnetometer-Dreiachsiges-Kompass-Magnetometer-Sensormodul-Dupont-Kabel/dp/B0CD77DN87/ref=sr_1_2?__mk_de_DE=%C3%85M%C3%85%C5%BD%C3%95%C3%91&crid=2VUYZ32C5JMI9&dib=eyJ2IjoiMSJ9.QlNAQAtHAyE3c-Xv07dOdNjSki1srflCFSMsHCBc2gGsF_z1qToaUu7GSjxH5L9ydxwmLDTvj0lqFpbYpfRwKDr88K6XIXulyyEtPSGwjH0-BuwGLO64TC1-lIrDtkR8S8OIWzprXwM3aZGD6IFu9RtBFiBWdEwJec84-OwuC-IzUJ5ys_qUaAT0tZ6DDBM92OwqLydp2-Ai9uy5UOj05cfuLWhjCZmxsds4MeJPqPG4BXAFGblN1nAoOl63f4iEW7MJ6cRTRHeccZRYSKXlZqnVO7mQbEqvbROIQf5rw08.Kf7_CS7_ByREAAjD6_SYmmYEgvMvoOmLtZa3L78psK8)

- **8-Kanal Pegelwandler - 10$**  
  [Zum Angebot](https://www.amazon.de/Kan%C3%A4le-Wasserwaage-Konverter-bidirektional-Raspberry/dp/B07Y86JBZY/ref=sr_1_2?__mk_de_DE=%C3%85M%C3%85%C5%BD%C3%95%C3%91&crid=1M44ZZFOVWG75&dib=eyJ2IjoiMSJ9.SyGdySgny7iVPbjp29EC-q6CM6C_j8jmlpi4XQxOejZO65cdw_nEA1hOA_zXjYMpywBj51_SERBw8eTA3R3CCgeTDtoO2kJ68AdOLswQQrwjUQQHt5kxeIjzb7ZWt8NhVHgzUY4Z7hnsBubA508C2Of4mJTy9eVjP6MqpGVwfMmEoJUQOxntbYGhcnjEl5zcBCb0sqBa04bWcH90UmllJ9Zp4mVNiP2txdn--axVIZ_amFmRw3RQoSLm7doBmdfdIDd25iDTplHyxkU5wnHfKtOhhS7RNduYLnoVOcZbRFE.PdbFnuvFJiHMj4DkMMLKuotliWGez80S6UHv6FDpXLM)

- **Breadboard - 9$**  
  [Zum Angebot](https://www.amazon.de/VooGenzek-Breadboard-Solderless-Prototype-doppelseitigem/dp/B0BZH86VNJ/ref=sr_1_6?__mk_de_DE=%C3%85M%C3%85%C5%BD%C3%95%C3%91&crid=1CICEAQGUUDCD&dib=eyJ2IjoiMSJ9.FT1wA4dYsvRUDCgnV0egAHoTyBHt6ouA0U1B7M24eioS-d7sEXjkzSWRo1BM6jSomsVsqtxS1HfjvYu68wugdObdy6L4lOBsS8hW7ef7nihxTY_t6TVyHRRSlhLFO_YXPw_Tu7C4WIweP63VJhP4lApnVVQKvQ4JwgKOBxv-LR034pcdlo2LpoHp7DDpA8M9FgL26GdpGR0aQpWUim4fhQ805SFKlFLyLJpUvjAlpNtUghGHEYI_dXwRRwFa0SWxjW29wACRfDKfkWoXt0VbTZBWE8sd8LOJ1iM5ZNf52UM.Yx1Ah1seSrOzl47n_-kIQ5q9ClwVZK_ss7WxMkXwreI)

---

## Versionsstand und Trainingsdatei

- **Version 1.0:**  
  - Konstante Beschleunigung.
  - Das Fahrzeug folgt einem Ziel und weicht Hindernissen aus.
  - Trainingsdatei: [agent.json](/agent.json)

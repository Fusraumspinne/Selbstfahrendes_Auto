import glob
import os
import pandas as pd
import numpy as np
import tensorflow as tf
from sklearn.model_selection import train_test_split

# ======================================
# 1. CSV-Dateien laden
# ======================================

data_folder = "tensorflow"   # Ordner mit deinen CSV-Dateien
csv_files = glob.glob(os.path.join(data_folder, "*.csv"))

if not csv_files:
    raise FileNotFoundError(f"Keine CSV-Dateien in '{data_folder}' gefunden!")

print(f"{len(csv_files)} CSV-Dateien gefunden.")

all_rows = []

for file in csv_files:
    print(f"Lade: {file}")

    # CSV robust einlesen
    df = pd.read_csv(file, header=0, sep=",")

    # --- Sicherstellen, dass jede Datei genau 10 Spalten hat ---
    # 9 Sensorwerte + 1 Aktion
    df = df.iloc[:, :10]  # Mehr Spalten -> abschneiden

    # Falls zu wenige Spalten -> auffüllen
    while df.shape[1] < 10:
        df[f"col_{df.shape[1]}"] = 1.0

    # Sicherstellen, dass keine NaNs drin sind
    df = df.fillna(1.0)

    all_rows.append(df.values)

# Alles zusammenführen
data = np.vstack(all_rows)

print("--------------")
print(f"Gesamte Datenmenge: {data.shape[0]} Zeilen")
print("--------------")

# ======================================
# 2. Features & Labels
# ======================================

X = data[:, :9].astype(np.float32)   # Sensorwerte
y = data[:, 9].astype(np.int32)      # Aktion 0,1,2

print("Beispiel X:", X[0])
print("Beispiel y:", y[0])

# ======================================
# 3. Training / Validation / Test Split
# ======================================

X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.15, random_state=42
)

X_train, X_val, y_train, y_val = train_test_split(
    X_train, y_train, test_size=0.15, random_state=42
)

print(f"Training Samples: {X_train.shape[0]}")
print(f"Validation Samples: {X_val.shape[0]}")
print(f"Test Samples: {X_test.shape[0]}")

# ======================================
# 4. TensorFlow Modell
# ======================================

model = tf.keras.Sequential([
    tf.keras.layers.Input(shape=(9,)),
    tf.keras.layers.Dense(64, activation='relu'),
    tf.keras.layers.Dense(128, activation='relu'),
    tf.keras.layers.Dense(64, activation='relu'),
    tf.keras.layers.Dense(3, activation='softmax')  # Aktionen: 0,1,2
])

model.compile(
    optimizer='adam',
    loss='sparse_categorical_crossentropy',
    metrics=['accuracy']
)

# ======================================
# 5. Training starten
# ======================================

history = model.fit(
    X_train, y_train,
    validation_data=(X_val, y_val),
    batch_size=256,
    epochs=50
)

# ======================================
# 6. Evaluierung
# ======================================

loss, acc = model.evaluate(X_test, y_test)
print(f"\nTest Accuracy: {acc * 100:.2f}%\n")

# ======================================
# 7. Modelle exportieren
# ======================================

# H5 Modell
model.save("avoid_model.h5")
print("✔ H5 Modell gespeichert als avoid_model.h5")

# TFLite Modell
converter = tf.lite.TFLiteConverter.from_keras_model(model)
tflite_model = converter.convert()

with open("avoid_model.tflite", "wb") as f:
    f.write(tflite_model)

print("✔ TFLite Modell gespeichert als avoid_model.tflite")
print("\nFERTIG!")
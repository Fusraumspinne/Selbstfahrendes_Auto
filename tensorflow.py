# Befehle
# cd /home/Desktop/Selbstfahrendes_Auto
# sudo pigpiod
# source venv/bin/activate
# python3 tensorflow/tensorflow.py

import time
import math
import os
import sys
import numpy as np

# sensor / hw libs
try:
    import pigpio
except Exception as e:
    print("Fehler: pigpio fehlt. Installiere mit: sudo apt install pigpio python3-pigpio")
    raise

import smbus2
import RPi.GPIO as GPIO

# TFLite runtime bevorzugen
INTERPRETER = None
try:
    from tflite_runtime.interpreter import Interpreter
    INTERPRETER = "tflite_runtime"
except Exception:
    try:
        # fallback: tensorflow (falls installiert)
        import tensorflow as tf
        Interpreter = tf.lite.Interpreter
        INTERPRETER = "tensorflow"
    except Exception:
        Interpreter = None
        INTERPRETER = None

if Interpreter is None:
    print("Kein TFLite-Interpreter verfügbar. Installiere 'tflite-runtime' oder 'tensorflow'.")
    sys.exit(1)


# -----------------------
# Hardware / Pins
# -----------------------
# QMC5883 (Kompass) address & bus (wie im alten Script)
ADDRESS = 0x0D
bus = smbus2.SMBus(1)

# Ultraschall sensor pins (trig, echo) in deiner Reihenfolge
SENSORS = [
    ("Sensor1",  4, 17),
    ("Sensor2", 18, 27),
    ("Sensor3", 22, 10),
    ("Sensor4",  9, 11),
    ("Sensor5",  5,  6),
    ("Sensor6", 12, 13),
    ("Sensor7", 16, 19),
    ("Sensor8", 20, 21),
    ("Sensor9", 26, 14),
]

# Servo Pin (PWM) - wie vorher
PI_GPIO = 18

# Modellpfade (stelle sicher, dass avoid_model.tflite im gleichen Ordner liegt)
TFLITE_PATH = "avoid_model.tflite"
H5_PATH = "avoid_model.h5"  # fallback, benötigt TF installiert

# Parameter
MAX_DIST_CM = 100.0    # Distanznormierung (wie in Unity/CSV: 0..1 mapped to 0..100cm)
SENSOR_COUNT = 9
SLEEP_BETWEEN_MEASURES = 0.025  # s
LOOP_DELAY = 0.12  # Hauptloop delay (s) -- passt an deine Messfrequenz an
SMOOTHING_ALPHA = 0.4  # 0..1, kleiner = stärkeres Glätten
STEERING_SCALE = 1.0   # ggf. anpassen
SERVO_CENTER_US = 1500
SERVO_RANGE_US = 500   # v=-1 -> 1000us, v=+1 -> 2000us
SAFETY_MIN_DIST = 5.0  # cm, falls closer than this -> reduce speed / act safer

# -----------------------
# Kompass (QMC) helper
# -----------------------
def setup_qmc():
    try:
        bus.write_byte_data(ADDRESS, 0x09, 0b00011101)
        bus.write_byte_data(ADDRESS, 0x0B, 0x01)
    except Exception as e:
        print("Warnung: QMC Init fehlgeschlagen:", e)

def read_raw():
    data = bus.read_i2c_block_data(ADDRESS, 0x00, 6)
    x = data[1] << 8 | data[0]
    y = data[3] << 8 | data[2]
    z = data[5] << 8 | data[4]
    x = x - 65536 if x > 32767 else x
    y = y - 65536 if y > 32767 else y
    z = z - 65536 if z > 32767 else z
    return x, y, z

def get_heading():
    try:
        x, y, z = read_raw()
        heading_rad = math.atan2(y, x)
        heading_deg = math.degrees(heading_rad)
        return ((heading_deg - 90.0) / 180.0)  # wie vorher (normalisiert -1..1 approx)
    except Exception:
        return 0.0

# -----------------------
# Ultraschall helper
# -----------------------
def setup_ultrasound():
    GPIO.setmode(GPIO.BCM)
    for _, trig, echo in SENSORS:
        GPIO.setup(trig, GPIO.OUT)
        GPIO.setup(echo, GPIO.IN)
        GPIO.output(trig, False)
    time.sleep(1.0)

def measure_distance(trig_pin, echo_pin):
    # send pulse
    GPIO.output(trig_pin, True)
    time.sleep(0.00001)
    GPIO.output(trig_pin, False)

    start = time.time()
    timeout = start + 0.02

    while GPIO.input(echo_pin) == 0 and time.time() < timeout:
        start = time.time()
    stop = time.time()
    timeout = stop + 0.02
    while GPIO.input(echo_pin) == 1 and time.time() < timeout:
        stop = time.time()

    elapsed = stop - start
    distance_cm = (elapsed * 34300.0) / 2.0
    # clamp / filter
    if distance_cm <= 0 or distance_cm > MAX_DIST_CM:
        return None
    return distance_cm

def calc_distances():
    dists = []
    for _, trig, echo in SENSORS:
        dist = measure_distance(trig, echo)
        if dist is None:
            dists.append(1.0)   # no obstacle -> 1.0 (wie unity)
        else:
            dists.append(min(dist / MAX_DIST_CM, 1.0))
        time.sleep(SLEEP_BETWEEN_MEASURES)
    return dists

# -----------------------
# Modell loader
# -----------------------
class ModelWrapper:
    def __init__(self, tflite_path=TFLITE_PATH, h5_path=H5_PATH):
        self.interpreter = None
        self.input_details = None
        self.output_details = None
        self.mode = None  # 'tflite' or 'h5_class' or 'h5_reg'
        if os.path.exists(tflite_path):
            try:
                self.interpreter = Interpreter(model_path=tflite_path)
                self.interpreter.allocate_tensors()
                self.input_details = self.interpreter.get_input_details()
                self.output_details = self.interpreter.get_output_details()
                self.mode = 'tflite'
                print("TFLite Model geladen:", tflite_path)
            except Exception as e:
                print("Fehler beim Laden von TFLite:", e)
        elif os.path.exists(h5_path):
            # fallback: load h5 via tensorflow.keras if available
            try:
                import tensorflow as tf
                self.model = tf.keras.models.load_model(h5_path)
                # check output shape
                out_shape = self.model.output_shape
                if out_shape[-1] == 1:
                    self.mode = 'h5_reg'
                else:
                    self.mode = 'h5_class'
                print("Keras H5 Model geladen:", h5_path, "Mode:", self.mode)
            except Exception as e:
                print("Keras H5 Laden fehlgeschlagen:", e)
        else:
            raise FileNotFoundError("Kein Modell gefunden (avoid_model.tflite oder avoid_model.h5)")

    def predict(self, sensors):
        """
        sensors: list/np.array of length 9 (normalized 0..1)
        returns steering float in [-1,1]
        """
        x = np.array([sensors], dtype=np.float32)

        if self.mode == 'tflite':
            self.interpreter.set_tensor(self.input_details[0]['index'], x)
            self.interpreter.invoke()
            out = self.interpreter.get_tensor(self.output_details[0]['index'])
            # out could be shape (1,3) for softmax or (1,1) for regression
            if out.shape[-1] == 1:
                # regression
                steering = float(out[0][0])
                # ensure in [-1,1]
                steering = max(-1.0, min(1.0, steering))
                return steering
            else:
                # classification softmax -> make continuous: p_right - p_left
                probs = out[0]
                # if 3 classes [left, straight, right], build continuous estimate
                left = float(probs[0])
                straight = float(probs[1])
                right = float(probs[2])
                steering = (right - left)  # in [-1,1] approx
                return steering

        elif self.mode in ('h5_reg', 'h5_class'):
            out = self.model.predict(x)
            if out.shape[-1] == 1:
                steering = float(out[0][0])
                steering = max(-1.0, min(1.0, steering))
                return steering
            else:
                probs = out[0]
                left, straight, right = float(probs[0]), float(probs[1]), float(probs[2])
                steering = (right - left)
                return steering
        else:
            raise RuntimeError("Unbekannter Modellmodus")

# -----------------------
# Servo helper
# -----------------------
def steering_to_us(steer):
    # steer expected in [-1, 1]
    steer = max(-1.0, min(1.0, steer))
    return int(SERVO_CENTER_US + steer * SERVO_RANGE_US)

# -----------------------
# main loop
# -----------------------
def main():
    # init hw
    setup_qmc()
    setup_ultrasound()

    pi = pigpio.pi()
    if not pi.connected:
        print("Fehler: pigpio daemon nicht erreichbar. Starte: sudo pigpiod")
        return
    pi.set_mode(PI_GPIO, pigpio.OUTPUT)

    # load model
    try:
        model = ModelWrapper()
    except Exception as e:
        print("Kein Modell: ", e)
        return

    last_servo_us = SERVO_CENTER_US
    smooth_servo = last_servo_us

    try:
        print("Starte Inference Loop. CTRL+C zum beenden.")
        while True:
            heading = get_heading()  # falls du Heading im Input willst (dein dataset hatte heading? If not, ignore)
            sensors = calc_distances()  # returns 9 values normalized 0..1

            # Optional: prepend heading if model expects it; earlier ML model expects 9 inputs (only sensors).
            # Our training used only 9 sensors, so we pass sensors only.
            inputs = sensors

            # predict steering [-1..1]
            steer = model.predict(inputs) * STEERING_SCALE

            # safety: if something super close in front, reduce magnitude (optional)
            front_dist = sensors[4] if len(sensors) > 4 else 1.0
            if front_dist < (SAFETY_MIN_DIST / MAX_DIST_CM):
                steer *= 0.7  # reduce steering aggressiveness if object very close

            # convert to servo us and smooth
            target_us = steering_to_us(steer)
            smooth_servo = int(smooth_servo * (1.0 - SMOOTHING_ALPHA) + target_us * SMOOTHING_ALPHA)

            # write servo
            pi.set_servo_pulsewidth(PI_GPIO, smooth_servo)
            last_servo_us = smooth_servo

            # debug output
            print(f"Sensors: {[round(x,3) for x in sensors]} -> steer={steer:.3f} -> us={smooth_servo}")
            time.sleep(LOOP_DELAY)

    except KeyboardInterrupt:
        print("Beende...")

    finally:
        pi.set_servo_pulsewidth(PI_GPIO, 0)
        pi.stop()
        GPIO.cleanup()

if __name__ == "__main__":
    main()

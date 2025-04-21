import RPi.GPIO as GPIO
import time

sensors = [
    ("Sensor 1",  4, 17),
    ("Sensor 2", 18, 27),
    ("Sensor 3", 22, 10),
    ("Sensor 4",  9, 11),
    ("Sensor 5",  5,  6),
    ("Sensor 6", 12, 13),
    ("Sensor 7", 16, 19),
    ("Sensor 8", 20, 21),
    ("Sensor 9", 26, 14),
]

def setup():
    GPIO.setmode(GPIO.BCM)
    for name, trig, echo in sensors:
        GPIO.setup(trig, GPIO.OUT)
        GPIO.setup(echo, GPIO.IN)
        GPIO.output(trig, False)
    print("Warte 2 Sekunden, bis sich alle Sensoren stabilisieren...")
    time.sleep(2)

def measure_distance(trig_pin, echo_pin):
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
    distance_cm = (elapsed * 34300) / 2
    return distance_cm

def main():
    try:
        setup()
        while True:
            print(f"{'Sensor':<8} | {'Distance [cm]':>12}")
            print("-" * 25)
            for name, trig, echo in sensors:
                dist = measure_distance(trig, echo)
                if dist <= 0 or dist > 400:
                    disp = "out of range"
                else:
                    disp = f"{dist:7.2f}"
                print(f"{name:<8} | {disp:>12}")
            print("\n") 
            time.sleep(1)
    except KeyboardInterrupt:
        print("\nBeenden, GPIO leeren.")
    finally:
        GPIO.cleanup()

if __name__ == "__main__":
    main()

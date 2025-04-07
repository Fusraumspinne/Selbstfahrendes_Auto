import RPi.GPIO as GPIO
import time

TRIG = 23 
ECHO = 24 

def setup():
    GPIO.setmode(GPIO.BCM)
    GPIO.setup(TRIG, GPIO.OUT)
    GPIO.setup(ECHO, GPIO.IN)
    GPIO.output(TRIG, False)
    print("Warte auf Stabilisierung des Sensors...")
    time.sleep(2)

def measure_distance():
    GPIO.output(TRIG, True)
    time.sleep(0.00001)  
    GPIO.output(TRIG, False)
    
    start_time = time.time()
    while GPIO.input(ECHO) == 0:
        start_time = time.time()
    
    stop_time = time.time()
    while GPIO.input(ECHO) == 1:
        stop_time = time.time()
    
    elapsed_time = stop_time - start_time
    
    distance = (elapsed_time * 34300) / 2
    return distance

def main():
    setup()
    try:
        while True:
            dist = measure_distance()
            print("Entfernung: {:.2f} cm".format(dist))
            kiInput = min(dist / 100.0, 1)
            print(f"Ki-Input: {kiInput}")
            time.sleep(1) 
    except KeyboardInterrupt:
        print("\nMessung beendet. Bereinige die GPIO-Pins auf.")
        GPIO.cleanup()

if __name__ == '__main__':
    main()
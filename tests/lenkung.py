import pigpio
import time

PI_GPIO = 18 

pi = pigpio.pi()
pi.set_mode(PI_GPIO, pigpio.OUTPUT)

def set_steering(v):
    us = int(1500 + v * 500) 
    pi.set_servo_pulsewidth(PI_GPIO, us)

try:
    while True:
        for v in [-1.0, 0.0, 1.0]:
            print("Steering:", v)
            set_steering(v)
            time.sleep(1)
finally:
    pi.set_servo_pulsewidth(PI_GPIO, 0)
    pi.stop()
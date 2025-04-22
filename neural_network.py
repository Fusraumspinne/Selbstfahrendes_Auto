import smbus2
import time
import math
import RPi.GPIO as GPIO
import random
import json
import os
import pigpio

ADDRESS = 0x0D  
bus = smbus2.SMBus(1)

def setup_qmc():
    bus.write_byte_data(ADDRESS, 0x09, 0b00011101)
    bus.write_byte_data(ADDRESS, 0x0B, 0x01)

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
    x, y, z = read_raw()
    heading_rad = math.atan2(y, x)
    heading_deg = math.degrees(heading_rad)
    if heading_deg < 0:
        heading_deg += 360
    return heading_deg / 360.0

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

def setup_ultrasound():
    GPIO.setmode(GPIO.BCM)
    for _, trig, echo in sensors:
        GPIO.setup(trig, GPIO.OUT)
        GPIO.setup(echo, GPIO.IN)
        GPIO.output(trig, False)
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

def calc_distance():
    distances = []
    for _, trig, echo in sensors:
        dist = measure_distance(trig, echo)
        if dist <= 0 or dist > 75:
            distances.append(1.0)
        else:
            distances.append(dist / 75.0)
        time.sleep(0.01)
    return distances

class NeuralNetwork:
    def __init__(self, layers):
        self.layers = layers
        self.InitNeurons()
        self.InitWeights()

    def InitNeurons(self):
        self.neurons = [[0.0] * layer for layer in self.layers]

    def InitWeights(self):
        self.weights = []
        for i in range(1, len(self.layers)):
            layer_weights = []
            for _ in range(self.layers[i]):
                neuron_weights = [random.uniform(-1.0, 1.0) for _ in range(self.layers[i - 1])]
                layer_weights.append(neuron_weights)
            self.weights.append(layer_weights)

    def FeedForward(self, inputs):
        self.neurons[0] = inputs[:]
        for i in range(1, len(self.layers)):
            for j in range(self.layers[i]):
                value = sum(self.weights[i - 1][j][k] * self.neurons[i - 1][k] for k in range(self.layers[i - 1]))
                self.neurons[i][j] = math.tanh(value)
        return self.neurons[-1]

    def SetWeights(self, weights):
        self.weights = weights

def LoadAgent():
    file_path = os.path.join(os.getcwd(), "agent.json")
    if not os.path.exists(file_path):
        print("Datei nicht gefunden.")
        return None

    with open(file_path, "r") as file:
        json_data = json.load(file)

    layers = [10, 20, 20, 1]
    net = NeuralNetwork(layers)

    weights = []
    for layer in json_data["layerArrays"]:
        layerWeights = []
        for weightSet in layer["weightsArrays"]:
            layerWeights.append(weightSet["weights"])
        weights.append(layerWeights)

    net.SetWeights(weights)
    print("Netzwerk erfolgreich geladen.")
    return net

PI_GPIO = 18 
pi = pigpio.pi()
pi.set_mode(PI_GPIO, pigpio.OUTPUT)

def set_steering(v):
    v = max(min(v, 0.9), -0.9)
    us = int(1500 + v * 500) 
    pi.set_servo_pulsewidth(PI_GPIO, us)
    print(v)

def main():
    try:
        setup_qmc()
        setup_ultrasound()
        net = LoadAgent()
        if net is None:
            return

        while True:
            heading = get_heading()
            vision = calc_distance()
            inputs = [heading] + vision
            output = net.FeedForward(inputs)[0]
            set_steering(output)
            time.sleep(0.1)

    except KeyboardInterrupt:
        print("Beende...")
    finally:
        pi.set_servo_pulsewidth(PI_GPIO, 0)
        pi.stop()
        GPIO.cleanup()

if __name__ == "__main__":
    main()
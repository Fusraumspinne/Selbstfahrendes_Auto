import smbus2
import time
import math

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
    
    # Vorzeichenkorrektur
    x = x - 65536 if x > 32767 else x
    y = y - 65536 if y > 32767 else y
    z = z - 65536 if z > 32767 else z
    return x, y, z

def calculate_heading(x, y):
    heading_rad = math.atan2(y, x)
    heading_deg = math.degrees(heading_rad)
    if heading_deg < 0:
        heading_deg += 360
    return heading_deg

def main():
    setup_qmc()
    while True:
        x, y, z = read_raw()
        heading = calculate_heading(x, y)
        print(f"Richtung: {heading:.2f}")
        time.sleep(1)

if __name__ == "__main__":
    main()

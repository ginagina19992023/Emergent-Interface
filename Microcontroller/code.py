import board
import digitalio
import analogio
import rotaryio
import time
import usb_hid
from adafruit_hid.keyboard import Keyboard
from adafruit_hid.keycode import Keycode

# --------------------
# Pico W2 Pin Definitions (GPIO numbers)
# --------------------
# Rotary encoder: A and B channels
ENCODER_A_PIN = board.GP2  # Encoder channel A
ENCODER_B_PIN = board.GP3  # Encoder channel B
BIKE_PIN = board.GP4  # Bike revolution sensor (digital, pullup)
MIC_PIN = board.GP26  # Contact mic (analog, ADC0 on Pico W2)

# --------------------
# Timing (matches arduinocontroller.ino: 100Hz update rate)
# --------------------
UPDATE_RATE = 0.010  # 10 ms → ~100 Hz

# --------------------
# Bike sensor (matches .ino: falling edge + debounce)
# --------------------
DEBOUNCE_CYCLES = 10  # 100/UPDATE_RATE equivalent
time_since_last_revolution = 999999
prev_bike_state = True  # pullup → high when no magnet

# --------------------
# Mic hit detection (matches .ino)
# --------------------
MIC_THRESHOLD = 200
HIT_COOLDOWN = 0.100  # 100 ms in seconds
last_hit_time = 0.0

# --------------------
# Encoder: last position for A/D key mapping
# --------------------
last_encoder_position = 0

# --------------------
# Setup
# --------------------
kbd = Keyboard(usb_hid.devices)

# Rotary encoder (quadrature)
encoder = rotaryio.IncrementalEncoder(ENCODER_A_PIN, ENCODER_B_PIN)

# Bike sensor
bike_sensor = digitalio.DigitalInOut(BIKE_PIN)
bike_sensor.direction = digitalio.Direction.INPUT
bike_sensor.pull = digitalio.Pull.UP

# Contact mic (analog)
mic = analogio.AnalogIn(MIC_PIN)

print("Ready. Encoder→A/D, Bike rev→W, Mic hit→SPACE")

while True:
    loop_start = time.monotonic()

    # ---- Rotary encoder → A and D keys ----
    pos = encoder.position
    if pos > last_encoder_position:
        # Turned one way → press D
        kbd.send(Keycode.D)
    elif pos < last_encoder_position:
        # Turned other way → press A
        kbd.send(Keycode.A)
    last_encoder_position = pos

    # ---- Contact mic hit → SPACE ----
    mic_value = mic.value  # 0–65535 on CircuitPython (16-bit)
    # Scale to match Arduino 0–1023 range for same threshold
    mic_scaled = (mic_value * 1023) // 65535
    if mic_scaled > MIC_THRESHOLD:
        now = time.monotonic()
        if (now - last_hit_time) > HIT_COOLDOWN:
            last_hit_time = now
            print("Punch detected", "pressed SPACE")
            kbd.send(Keycode.SPACE)

    # ---- Bike revolution → W ----
    current_bike_state = bike_sensor.value
    # Falling edge: was high, now low (magnet passed)
    if current_bike_state is False and prev_bike_state is True:
        if time_since_last_revolution > DEBOUNCE_CYCLES:
            time_since_last_revolution = 0
            print("Rotation detected", "pressed W")
            kbd.send(Keycode.W)
    time_since_last_revolution += 1
    prev_bike_state = current_bike_state

    # Maintain ~100 Hz update rate
    elapsed = time.monotonic() - loop_start
    sleep_time = UPDATE_RATE - elapsed
    if sleep_time > 0:
        time.sleep(sleep_time)

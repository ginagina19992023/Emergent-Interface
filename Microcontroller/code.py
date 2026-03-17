import board
import digitalio
import time
import usb_hid
from adafruit_hid.keyboard import Keyboard
from adafruit_hid.keycode import Keycode

# Set up the keyboard as a USB HID device
kbd = Keyboard(usb_hid.devices)

# 1. Set up the sensor pin.
# Note: Change 'board.GP2' to match your specific board's pinout
sensor = digitalio.DigitalInOut(board.GP2)  # physical pin 4
sensor.direction = digitalio.Direction.INPUT
sensor.pull = digitalio.Pull.UP

# 2. Variables
debounce_time = 0.200  # 200 milliseconds in seconds
last_revolution_time = -999999  # Ensures the first read triggers immediately
revolution_count = 0

# INPUT_PULLUP means the pin naturally sits at True (High).
# When the magnet passes the sensor, it pulls it to False (Low).
prev_state = True

print("Bike Tracker Ready! Waiting for revolutions to send keystrokes...")

while True:
    current_state = sensor.value

    # Look for a "falling edge" - the moment it goes from True to False
    if current_state is False and prev_state is True:
        current_time = time.monotonic()

        # Check if enough actual time has passed (debounce)
        if (current_time - last_revolution_time) > debounce_time:
            last_revolution_time = current_time
            revolution_count += 1

            # --- HID BUTTON PRESS ---
            # kbd.send() presses and releases the key instantly.
            # Change Keycode.SPACE to Keycode.A, Keycode.UP_ARROW, etc., as needed.
            kbd.send(Keycode.SPACE)

            print("Revolutions:", revolution_count, "| Keystroke Sent!")

    # Update state for the next loop
    prev_state = current_state

    # A tiny delay keeps the loop from maxing out the microcontroller's CPU
    time.sleep(0.001)

import time
import math
from datetime import datetime
import paho.mqtt.client as mqtt

# MQTT setup
broker = "10.10.27.244"
port = 1883
client = mqtt.Client()
client.connect(broker, port, 60)

# Time and frequency setup
sampling_frequency = 250  # Hz
sample_interval = 1.0 / sampling_frequency
total_duration_sec = 10000 * 60  # 10 minutes
total_samples = int(total_duration_sec * sampling_frequency)

# Sawtooth setup
sawtooth_max = 100

start_time = time.perf_counter()

for i in range(total_samples):
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S.%f")[:-3]
    constant = 1

    # Generate signals
    sine_val = round(50 * math.sin(2 * math.pi * i / 60) + 50, 2)
    saw_val = round((i % sawtooth_max), 2)
    dc_val = 42.0

    # Format messages
    sine_msg = f"{timestamp},{constant},{sine_val}"
    saw_msg = f"{timestamp},{constant},{saw_val}"
    dc_msg = f"{timestamp},{constant},{dc_val}"

    # Publish to MQTT
    client.publish("esp32/sensor/data/S1", sine_msg)
    client.publish("esp32/sensor/data/S2", saw_msg)
    client.publish("esp32/sensor/data/S3", dc_msg)

    print("Published:", sine_msg, "|", saw_msg, "|", dc_msg)

    # Calculate next expected time
    next_sample_time = start_time + (i + 1) * sample_interval
    now = time.perf_counter()
    sleep_time = next_sample_time - now

    if sleep_time > 0:
        time.sleep(sleep_time)
    else:
        print(f"⚠️ Warning: Running behind schedule by {-sleep_time*1000:.2f} ms")

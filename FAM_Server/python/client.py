import paho.mqtt.client as mqtt
import csv
import time

# MQTT setup
client = mqtt.Client()
client.connect("localhost")
topic = "sensor/data"

# Temporary for .csv emulation
with open('fast_sine_wave.csv', 'r') as file:
	reader = csv.reader(file)
	rows = list(reader)

sampling_frequency = float(rows[0][0])
data_points = [float(x) for x in rows[2]]
interval = 1 / sampling_frequency

# Publish Data
def publish_data():
	for data_point in data_points:
		message = f"{data_point}"
		client.publish(topic, message)
		print(f"Published: {message}")
		time.sleep(interval)

client.loop_start()
publish_data()
client.loop_stop()

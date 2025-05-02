import os
import paho.mqtt.client as mqtt
import threading
import api_parsers
import csv
import json
import struct
import time
from datetime import datetime, timedelta
from collections import deque
import google_drive_and_networking_functions

csv_file_name = {}
csv_file_handles = {}
current_date = datetime.now().strftime("%Y_%m_%d")

# Initialize CSV file with the current date
def create_new_csv(sensor_name):
    global csv_file_name, current_date, csv_file_handles
    current_date = datetime.now().strftime("%Y_%m_%d")
    file_name = f"{sensor_name}_{current_date}.csv"
    
    file = open(file_name, mode='w', newline='')
    writer = csv.writer(file)
    writer.writerow(["Timestamp", "Voltage_mV"])
    
    csv_file_name[sensor_name] = file_name
    csv_file_handles[sensor_name] = (file, writer)
    print(f"New CSV file created: {file_name}")

def close_all_csvs():
    for sensor_name, (file, _) in csv_file_handles.items():
        file.flush()
        file.close()
        print(f"Closed CSV for {sensor_name}")
        
    csv_file_handles.clear()
    
def write_live_data_to_csv(sensor_name, message):
    if sensor_name in csv_file_handles:
        file, writer = csv_file_handles[sensor_name]
        
        if not file.closed:
            timestamp, channel, voltage = message.split(",", 2)
            writer.writerow([timestamp.strip(), channel.strip(), voltage.strip()])
            file.flush()
        else:
            print(f"Tried to write to closed file: {file}")

# Periodically check for date change
def monitor_date_change():
    global current_date
    while True:
        new_date = datetime.now().strftime("%Y_%m_%d")
        if new_date != current_date:
            close_all_csvs()
            for sensor_name, file_name in csv_file_name.items():
                print(f"Uploading {file_name}")
                google_drive_and_networking_functions.google_drive_upload(file_name)

            create_new_csv("S1") 
            create_new_csv("S2")  
            create_new_csv("S3")  

        time.sleep(60)  # Check every minute

ip_address = google_drive_and_networking_functions.get_ip_address()
create_new_csv("S1")
create_new_csv("S2")
create_new_csv("S3")

date_monitor_thread = threading.Thread(target=monitor_date_change, daemon=True)
date_monitor_thread.start()

print(f"Device IP Address: {ip_address}")

# MQTT setup
broker = "localhost"
current_sensor = "S1"
esp32_sensor_1_topic = "esp32/sensor/data/S1"
esp32_sensor_2_topic = "esp32/sensor/data/S2"
esp32_sensor_3_topic = "esp32/sensor/data/S3"
esp32_settings_topic = "esp32/sensor/settings"
republish_topic = "sensor/data"
command_topic = "desktop/commands"
data_upload_topic = "desktop/data"



MAX_VALUES = 100
TOLERANCE = 0.15

last_voltage_values = {
    "S1": deque(maxlen=MAX_VALUES),
    "S2": deque(maxlen=MAX_VALUES),
    "S3": deque(maxlen=MAX_VALUES)
}

def is_within_tolerance(new_value, last_values, tolerance=TOLERANCE):

    if not last_values:  
        return True
    last_value = last_values[-1] 
    return abs(new_value - last_value) <= abs(last_value * tolerance)
    
def read_settings_function():
    try:
        with open("Settings/settings.json") as file:
            settings = json.load(file)

        sampling_freq = int(settings.get("SamplingFrequency", 0))
        if sampling_freq == 100:
            sampling_freq = 48
        else:
            sampling_freq = 19

        client.publish(esp32_settings_topic, sampling_freq)
        print(f"Payload of {sampling_freq} published to {esp32_settings_topic}")
    except FileNotFoundError:
        print(f"Error: File 'Settings/settings.json' not found.")
    except json.JSONDecodeError:
        print(f"Error: Invalid JSON format")
    except Exception as e:
        print(f"Unexpected error: {e}")

def on_message(client, userdata, msg):
    message = msg.payload.decode('utf-8')
    print(f"Received on {msg.topic}: {message}")
    message_parsed = message.strip().split(',')
    global current_sensor

    if msg.topic == command_topic:
        match message_parsed[0].lower():
            case "live":
                print("Received 'live' command. Outputting today's CSV data.")
                current_sensor = message_parsed[1]

            case "fetch_donki_gst":
                print("Received 'fetch_donki_gst' command. Getting API")
                api_parsers.fetch_and_save_donki_gst_data(message_parsed[1], message_parsed[2])
                publish_csv_to_mqtt("APIs/donki_gst_api.csv", "api/data", 1)
                client.publish("api/data", "lollipop_eof", qos=1)

            case "fetch_temperature_api":
                print("Received 'fetch_temperature_api' command. Getting API")
                api_parsers.fetch_and_save_temperature_api(message_parsed[1], message_parsed[2])
                publish_csv_to_mqtt("APIs/noaa_temp.csv", "api/data", 1)
                client.publish("api/data", "line_eof", qos=1)

            case "fetch_humidity_api":
                print("Received 'fetch_humidity_api' command. Getting API")
                publish_csv_date_range("APIs/SWIRLL", "UAH_Swirll.csv", message_parsed[1], message_parsed[2], "api/data", 2)
                client.publish("api/data", "line_eof", qos=1)

            case "fetch_uah_swirll":
                print("Received 'fetch_uah_swirll' command. Writing to 'api/data'")
                publish_csv_date_range("APIs/SWIRLL", "UAH_Swirll.csv", message_parsed[1], message_parsed[2], "api/data", 1)
                client.publish("api/data", "line_eof", qos=1)

            case "fetch_tree_rhythms":
                print("Received 'fetch_tree_rhythms' command. Writing to 'api/data'")
                publish_csv_to_mqtt(f"APIs/Tree_Rhythms/{message_parsed[1]}_tree_rhythms.csv", "api/data", 1)
                client.publish("api/data", "line_eof", qos=1)

            case "fetch_solar_index":
                print("Received 'fetch_solar_index' command. Writing to 'api/data'")
                publish_csv_date_range("APIs/SWIRLL", "UAH_Swirll.csv", message_parsed[1], message_parsed[2], "api/data", 3)
                client.publish("api/data", "line_eof", qos=1)

            case "fetch_pressure_api":
                print("Received 'fetch_pressure_api' command. Writing to 'api/data'")
                publish_csv_date_range("APIs/SWIRLL", "UAH_Swirll.csv", message_parsed[1], message_parsed[2], "api/data", 1)
                client.publish("api/data", "line_eof", qos=1)


            case "fetch_oura_ring":
                print("Received 'fetch_oura' command. Writing to 'api/data'")
                publish_csv_to_mqtt(f"APIs/oura_ring.csv", "api/data", int(message_parsed[1]))
                client.publish("api/data", "line_eof", qos=1)

            case "fetch_samsung_hr":
                print("Received 'fetch_samsung' command. Writing to 'api/data'")
                publish_csv_to_mqtt(f"APIs/Samsung_hr.csv", "api/data", int(message_parsed[1]))
                client.publish("api/data", "line_eof", qos=1)

            case "settings_upload":
                print("Received 'settings_upload' command. Writing to file.")
                open("Settings/settings.json", "w").close()
                write_mqtt_to_file("Settings/settings.json", "none")
                read_settings_function()

            case "tree_rhythms_upload":
                print("Received 'tree_rhythms_upload' command. Writing to file.")
                write_mqtt_to_file(f"APIs/Tree_Rhythms/{message_parsed[1]}_tree_rhythms.csv", "tree_rhythms")

            case "uah_swirll_upload":
                print("Received 'uah_swirll_upload' command. Writing to file.")
                write_mqtt_to_file(f"APIs/SWIRLL/{message_parsed[1]}_UAH_Swirll.csv", "UAH_SWIRLL")

            case "oura_ring_upload":
                print("Received 'oura_ring_upload' command. Writing to file.")
                write_mqtt_to_file(f"APIs/oura_ring.csv", "oura_ring")

            case "samsung_hr_upload":
                print("Received 'samsung_hr_upload' command. Writing to file.")
                write_mqtt_to_file(f"APIs/Samsung_hr.csv", "samsung_hr")

            case _:
                print(f"Unknown command received: {message}")


    elif msg.topic == esp32_sensor_1_topic:
        voltage_value = float(message_parsed[2])
        timestamp_str = message_parsed[0]
    
        timestamp = datetime.strptime(timestamp_str, "%Y-%m-%d %H:%M:%S.%f")
        if timestamp.year >= 2024:
            if -1.8 <= voltage_value <= 1.8 and not (0.00 == voltage_value):
                last_voltage_values["S1"].append(voltage_value)

                # Publish only if within tolerance
                if is_within_tolerance(voltage_value, last_voltage_values["S1"]):
                    write_live_data_to_csv("S1", message)
                    if current_sensor == "S1":
                        client.publish(republish_topic, message)

    elif msg.topic == esp32_sensor_2_topic:
        voltage_value = float(message_parsed[2])
        timestamp_str = message_parsed[0]
    
        timestamp = datetime.strptime(timestamp_str, "%Y-%m-%d %H:%M:%S.%f")
        if timestamp.year >= 2024:
            if -1.8 <= voltage_value <= 1.8 and not (0.00 == voltage_value):
                last_voltage_values["S2"].append(voltage_value)

                # Publish only if within tolerance
                if is_within_tolerance(voltage_value, last_voltage_values["S2"]):
                    write_live_data_to_csv("S2", message)
                    if current_sensor == "S2":
                        client.publish(republish_topic, message)

    elif msg.topic == esp32_sensor_3_topic:
        voltage_value = float(message_parsed[2])
        timestamp_str = message_parsed[0]

        timestamp = datetime.strptime(timestamp_str, "%Y-%m-%d %H:%M:%S.%f")
        if timestamp.year >= 2024:
            if -1.8 <= voltage_value <= 1.8 and not (0.00 == voltage_value):
                last_voltage_values["S3"].append(voltage_value)

                # Publish only if within tolerance
                if is_within_tolerance(voltage_value, last_voltage_values["S3"]):
                    write_live_data_to_csv("S3", message)
                    if current_sensor == "S3":
                        client.publish(republish_topic, message)


def write_mqtt_to_file(filepath, parserToUse):
    def on_file_message(client, userdata, msg):
        message = msg.payload.decode('utf-8').strip()
        print(f"Message received on {data_upload_topic}: {message}")
        if message == "End of File":
            print("Received 'End of File'. Stopping data collection.")
            client.disconnect()
            return

        with open(filepath, "a") as file:
            if parserToUse == "UAH_SWIRLL":
                parsed = api_parsers.parse_UAH_SWIRLL(message)
                if parsed:
                    file.write(",".join(parsed) + "\n")
                    
            elif parserToUse == "samsung_hr":
                parsed = api_parsers.parse_Samsung_hr(message)
                if parsed:
                    file.write(parsed + "\n")
                    
            else:
                file.write(message + "\n")

    command_client = mqtt.Client()
    command_client.on_message = on_file_message
    command_client.connect(broker)
    command_client.subscribe(data_upload_topic)

    print(f"Listening to '{data_upload_topic}' and writing to {filepath}. Waiting for 'eof' to stop.")
    command_client.loop_forever()

def publish_csv_date_range(folder_name, filename, start_date, end_date, mqtt_topic, column_number):
    try:
        start = datetime.strptime(start_date, "%Y_%m_%d")
        end = datetime.strptime(end_date, "%Y_%m_%d")

        while start <= end:
            date_str = f"{start.year}_{start.month:02d}_{start.day:02d}"
            full_filename = f"{folder_name}/{date_str}_{filename}"
            
            if os.path.exists(full_filename):
                print(f"File '{full_filename}' exists.")
                publish_csv_to_mqtt(full_filename, mqtt_topic, column_number)
            else:
                print(f"File '{full_filename}' does not exist.")
                
            start += timedelta(days=1)
            
    except Exception as e:
        print(f"Error in publish_csv_date_range: {e}")

def publish_csv_to_mqtt(file_name, mqtt_topic, column_number):
    try:
        batch_size = 100
        batch = []
        with open(file_name, mode='r') as file:
            reader = csv.reader(file)
            next(reader)  # Skip the header
            for row in reader:
                if len(row) > column_number:
                    message = f"{row[0]}, {row[column_number]}"
                    batch.append(message)
                    if len(batch) >= batch_size:
                        for m in batch:
                            client.publish(mqtt_topic, m, qos=1)
                            print(m)
                            time.sleep(0.001)
                        
                        batch = []
                else:
                    print(f"Skipping row {row} due to insufficient columns.")
            if batch:
                for m in batch:
                    client.publish(mqtt_topic, m, qos=1)
    except FileNotFoundError:
        print(f"File {file_name} not found. Cannot publish data.")
    except Exception as e:
        print(f"Error publishing data to MQTT topic: {e}")

client = mqtt.Client()
client.on_message = on_message
client.connect(broker)
client.subscribe(esp32_sensor_1_topic)
client.subscribe(esp32_sensor_2_topic)
client.subscribe(esp32_sensor_3_topic)
client.subscribe(command_topic)

print("MQTT Client started. Listening for messages...")
client.loop_forever()

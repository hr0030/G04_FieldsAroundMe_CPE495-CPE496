soimport os
import paho.mqtt.client as mqtt
import threading
import api_parsers
import csv
import json
import struct
import time
from datetime import datetime, timedelta
import google_drive_and_networking_functions

csv_file_name = {}
# Initialize CSV file with the current date
def create_new_csv(sensor_name):
    global csv_file_names
    current_date = datetime.now().strftime("%Y_%m_%d")
    file_name = f"{sensor_name}_{current_date}.csv"
    
    with open(file_name, mode='w', newline='') as file:
        writer = csv.writer(file)
        writer.writerow(["Timestamp", "Voltage_mV"])

    csv_file_names[sensor_name] = file_name
    print(f"New CSV file created: {file_name}")


    # Periodically check for date change
def monitor_date_change():
    global current_date
    while True:
        new_date = datetime.now().strftime("%Y_%m_%d")
        if new_date != current_date:
            
            for sensor_name, file_name in csv_file_names.items():
                print(f"Uploading {file_name}")
                google_drive_and_networking_functions.google_drive_upload(file_name)


            create_new_csv("S1") 
            create_new_csv("S2")  
            create_new_csv("S3")  

        time.sleep(60)  # Check every minute


# Print Local IP
ip_address = google_drive_and_networking_functions.get_ip_address()
create_new_csv()
date_monitor_thread = threading.Thread(target=monitor_date_change, daemon=True)
date_monitor_thread.start()

print(f"Device IP Address: {ip_address}")

# MQTT setup
broker = "localhost"
current_sensor = "S1"
esp32_sensor_1_topic = "esp32/sensor/data/S1"  # Topic the ESP32 publishes to
esp32_sensor_2_topic = "esp32/sensor/data/S2"  # Topic the ESP32 publishes to
esp32_sensor_3_topic = "esp32/sensor/data/S3"  # Topic the ESP32 publishes to
esp32_settings_topic = "esp32/sensor/settings" # Topic settings are published to
republish_topic = "sensor/data"  # Topic to republish to
command_topic = "desktop/commands"  # Topic for receiving commands from the Fam_app
data_upload_topic = "desktop/data"


def read_settings_function():
    try:
        with open("Settings/settings.json") as file:
            settings = json.load(file)

        sampling_freq = int(settings.get("SamplingFrequency", 0))
        if sampling_freq == 100:
            sampling_freq = 48
        else:
            sampling_freq = 19

        # Convert to uint16 format (2 bytes)
        # payload = struct.pack(">H", sampling_freq)  # Big-endian uint16
        client.publish(esp32_settings_topic, sampling_freq)
        print(f"Payload of {sampling_freq} published to {esp32_settings_topic}")
    except FileNotFoundError:
        print(f"Error: File 'Settings/settings.json' not found.")
    except json.JSONDecodeError:
        print(f"Error: Invalid JSON format")
    except Exception as e:
        print(f"Unexpected error: {e}")

def on_message(client, userdata, msg):
    message = msg.payload.decode()
    # print(f"Received on {msg.topic}: {message}")
    message_parsed = message.strip().split(',')

    # Check what command
    if msg.topic == command_topic:
        match message_parsed[0].lower():

            case "live":
                print("Received 'live' command. Outputting today's CSV data.")
                current_sensor = message_parsed[1]
                # publish_csv_to_mqtt(csv_file_name, "sensor/data", 1)

            case "fetch_donki_gst":
                print("Received 'fetch_donki_gst' command. Getting API")
                api_parsers.fetch_and_save_donki_gst_data(message_parsed[1], message_parsed[2])
                publish_csv_to_mqtt("APIs/donki_gst_api.csv", "api/data", 1)

            case "fetch_temperature_api":
                print("Received 'fetch_temperature_api' command. Getting API")
                api_parsers.fetch_and_save_temperature_api(message_parsed[1], message_parsed[2])
                publish_csv_to_mqtt("APIs/noaa_temp.csv", "api/data", 1)

            case "fetch_humidity_api":
                print("Received 'fetch_humidity_api' command. Getting API")
                publish_csv_date_range("APIs/SWIRLL", "UAH_Swirll.csv", message_parsed[1], message_parsed[2], "api/data", 2)

            case "fetch_uah_swirll":
                print("Received 'fetch_uah_swirll' command. Writing to 'api/data'")
                publish_csv_date_range("APIs/SWIRLL", "UAH_Swirll.csv", message_parsed[1], message_parsed[2], "api/data", 1)

            case "fetch_tree_rhythms":
                print("Received 'fetch_tree_rhythms' command. Writing to 'api/data'")
                publish_csv_to_mqtt(f"APIs/Tree_Rhythms/{message_parsed[1]}_tree_rhythms.csv", "api/data", 1)

            case "fetch_solar_index":
                print("Received 'fetch_solar_index' command. Writing to 'api/data'")
                publish_csv_date_range("APIs/SWIRLL", "UAH_Swirll.csv", message_parsed[1], message_parsed[2], "api/data", 3)

            case "fetch_pressure_api":
                print("Received 'fetch_pressure_api' command. Writing to 'api/data'")
                publish_csv_date_range("APIs/SWIRLL", "UAH_Swirll.csv", message_parsed[1], message_parsed[2], "api/data", 1)

            case "fetch_sunrise_time":
                print("Received 'fetch_sunrise_time' command. Writing to 'api/data'")
                api_parsers.fetch_and_save_sunrise_api()

            case "fetch_sunset_time":
                print("Received 'fetch_sunset_time' command. Writing to 'api/data'")
                api_parsers.fetch_and_save_sunset_api()

            case "fetch_oura":
                print("Received 'fetch_oura' command. Writing to 'api/data'")
                publish_csv_to_mqtt(f"APIs/oura_ring.csv", "api/data", message_parse[1])
            

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
                write_mqtt_to_file(f"APIs/Oura_Ring.csv", "oura_ring")

            case _:
                print(f"Unknown command received: {message}")

    elif msg.topic == esp32_sensor_1_topic:
        
        # Write Data to CSV
        with open(csv_file_name["S1"], mode='a', newline='') as file:
            writer = csv.writer(file)
            writer.writerow(message)
        if current_sensor == "S1"
            # Republish the received data
            client.publish(republish_topic, message)

    elif msg.topic == esp32_sensor_2_topic:
        
        # Write Data to CSV
        with open(csv_file_name["S2"], mode='a', newline='') as file:
            writer = csv.writer(file)
            writer.writerow(message)
        if current_sensor == "S2"
            # Republish the received data
            client.publish(republish_topic, message)

    elif msg.topic == esp32_sensor_3_topic:
        
        # Write Data to CSV
        with open(csv_file_name["S3"], mode='a', newline='') as file:
            writer = csv.writer(file)
            writer.writerow(message)
        if current_sensor == "S3"
            # Republish the received data
            client.publish(republish_topic, message)


# Function to write messages from "desktop/data" to a file until "eof" is received
def write_mqtt_to_file(filepath, parserToUse):
    def on_message(client, userdata, msg):
        message = msg.payload.decode().strip()
        print(f"Message recieved on {data_upload_topic}: {message}")
        if message == "End of File":
            print("Received 'End of File'. Stopping data collection.")
            client.disconnect()
            return

        with open(filepath, "a") as file:
            if parserToUse == "UAH_SWIRLL":
                parsed = api_parsers.parse_UAH_SWIRLL(message)
                if parsed:
                    file.write(",".join(parsed) + "\n")
            else:
                file.write(message + "\n")

    command_client = mqtt.Client()
    command_client.on_message = on_message
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


# Function to publish CSV file content to MQTT
def publish_csv_to_mqtt(file_name, mqtt_topic, column_number):
    try:
        with open(file_name, mode='r') as file:
            reader = csv.reader(file)
            next(reader)  # Skip the header row
            for row in reader:
                if len(row) > column_number:
                    message = f"{row[0]}, {row[column_number]}"
                    client.publish(mqtt_topic, message)
                    # print(f"Published to {mqtt_topic}: {message}")
                else:
                    print(f"Skipping row {row} due to insufficient columns.")
    except FileNotFoundError:
        print(f"File {file_name} not found. Cannot publish data.")
    except Exception as e:
        print(f"Error publishing data to MQTT topic: {e}")

# MQTT Client Setup
client = mqtt.Client()
client.on_message = on_message
client.connect(broker)
client.subscribe(esp32_sensor_1_topic)
client.subscribe(esp32_sensor_2_topic)
client.subscribe(esp32_sensor_3_topic)
client.subscribe(command_topic)

print("MQTT Client started. Listening for messages...")
client.loop_forever()

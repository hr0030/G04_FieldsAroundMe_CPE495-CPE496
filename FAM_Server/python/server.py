import os
from googleapiclient.discovery import build
from googleapiclient.http import MediaFileUpload
from google.oauth2 import service_account
import paho.mqtt.client as mqtt
import time
import socket
import csv
import requests
from datetime import datetime
import api_parsers
import mqtt_csv_read_and_write_functions

def get_ip_address():
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.connect(("192.168.1.1", 80))
        ip_address = s.getsockname()[0]
        s.close()
        return ip_address
    except Exception as e:
        return f"Unable to get IP address: {e}"

# Print Local IP
ip_address = get_ip_address()
print(f"Device IP Address: {ip_address}")

# MQTT setup
broker = "localhost"
esp32_topic = "esp32/sensor/data"  # Topic the ESP32 publishes to
republish_topic = "sensor/data"  # Topic to republish to
command_topic = "desktop/commands"  # Topic for receiving commands from the Fam_app

# Path to your service account JSON file
SERVICE_ACCOUNT_FILE = 'Settings/fieldsaroundme-server-key.json'

# Google Drive API scopes
SCOPES = ['https://www.googleapis.com/auth/drive.file']

def google_drive_upload(file_path, folder_id=None):
    try:
        # Authenticate using service account
        creds = service_account.Credentials.from_service_account_file(
            SERVICE_ACCOUNT_FILE, scopes=SCOPES)

        # Build the Google Drive API service
        service = build('drive', 'v3', credentials=creds)

        # File metadata
        file_metadata = {
            'name': file_path.split('/')[-1],  # Extract file name from path
            'mimeType': 'text/csv'
        }

        if folder_id:
            file_metadata['parents'] = [folder_id]

        # Upload file
        media = MediaFileUpload(file_path, mimetype='text/csv')
        file = service.files().create(
            body=file_metadata,
            media_body=media,
            fields='id'
        ).execute()

        print(f"File uploaded successfully. File ID: {file['id']}")

        # Set permissions to make the file public
        permissions = {
            'type': 'anyone',
            'role': 'reader',  # 'writer' if you want write access
        }

        # Create permission
        service.permissions().create(
            fileId=file['id'],
            body=permissions,
            fields='id'
        ).execute()

        print("File permissions updated.")
        return file['id']

    except Exception as e:
        print(f"An error occurred: {e}")
        return None
        
# Initialize CSV file with the current date
def create_new_csv():
    global current_date, csv_file_name
    current_date = datetime.now().strftime("%Y_%m_%d")
    csv_file_name = f"{current_date}.csv"
    with open(csv_file_name, mode='w', newline='') as file:
        writer = csv.writer(file)
        writer.writerow(["Timestamp", "Voltage_mV"])
    print(f"New CSV file created: {csv_file_name}")
    # google_drive_upload(csv_file_name)  # Upload the old file before switching

create_new_csv()

# Periodically check for date change
def monitor_date_change():
    global current_date
    while True:
        new_date = datetime.now().strftime("%Y_%m_%d")
        if new_date != current_date:
            print(f"Date changed from {current_date} to {new_date}. Uploading {csv_file_name} to Google Drive.")

            google_drive_upload(csv_file_name)  # Upload the old file before switching

            create_new_csv()  # Create a new file for the new day

        time.sleep(60)  # Check every minute

# Run the date monitoring function in a separate thread
import threading

date_monitor_thread = threading.Thread(target=monitor_date_change, daemon=True)
date_monitor_thread.start()

# Callback function to handle received messages
def on_message(client, userdata, msg):
    message = msg.payload.decode()
    print(f"Received on {msg.topic}: {message}")
    message_parsed = message.strip().split(',')

    # Check what command
    if msg.topic == command_topic:
        match message_parsed[0].lower():

            case "live":
                print("Received 'live' command. Outputting today's CSV data.")
                publish_csv_to_mqtt(csv_file_name, "sensor/data",1)

            case "fetch_donki_gst":
                print("Received 'fetch_donki_gst' command. Getting API")
                fetch_and_save_donki_gst_data(message_parsed[1], message_parsed[2])

            case "fetch_temperature_api":
                print("Received 'fetch_temperature_api' command. Getting API")
                fetch_and_save_temperature_api(message_parsed[1], message_parsed[2])

            case "fetch_humidity_api":
                print("Received 'fetch_humidity_api' command. Getting API")
                publish_csv_date_range("APIs", "UAH_Swirll.csv", message_parsed[1], message_parsed[2], "api/data", 2)

            case "fetch_uah_swirll":
                print("Received 'fetch_uah_swirll' command. Writing to 'api/data'")
                publish_csv_date_range("APIs", "UAH_Swirll.csv", message_parsed[1], message_parsed[2], "api/data", 1)

            case "fetch_tree_rhythms":
                print("Received 'fetch_tree_rhythms' command. Writing to 'api/data'")
                publish_csv_to_mqtt("APIs/{message_parsed[1]}_tree_rhythms.csv", "api/data", 1)

            case "fetch_solar_index":
                print("Received 'fetch_solar_index' command. Writing to 'api/data'")
                publish_csv_date_range("APIs", "UAH_Swirll.csv", message_parsed[1], message_parsed[2], "api/data", 3)

            case "fetch_pressure_api":
                print("Received 'fetch_pressure_api' command. Writing to 'api/data'")
                publish_csv_date_range("APIs", "UAH_Swirll.csv", message_parsed[1], message_parsed[2], "api/data", 1)

            case "fetch_sunrise_time":
                print("Received 'fetch_sunrise_time' command. Writing to 'api/data'")
                fetch_and_save_sunrise_api()

            case "fetch_sunset_time":
                print("Received 'fetch_sunset_time' command. Writing to 'api/data'")
                fetch_and_save_sunset_api()

            case "settings_upload":
                print("Received 'settings_upload' command. Writing to file.")
                write_mqtt_to_file("Settings/settings.json", "none")

            case "tree_rhythms_upload":
                print("Received 'tree_rhythms_upload' command. Writing to file.")
                write_mqtt_to_file("APIs/tree_rhythms.csv", "tree_rhythms")

            case "uah_swirll_upload":
                print("Received 'uah_swirll_upload' command. Writing to file.")
                write_mqtt_to_file("APIs/{message_parsed[1]_UAH_Swirll.csv", "UAH_SWIRLL")

            case _:
                print(f"Unknown command received: {message}")

    elif msg.topic == esp32_topic:
        # Write Data to CSV
        timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        formatted_message = f"{timestamp},{message}"
        
        with open(csv_file_name, mode='a', newline='') as file:
            writer = csv.writer(file)
            writer.writerow([timestamp, message])
            print(f"Written to {csv_file_name}: {timestamp}, {message}")

        # Republish the received data
        client.publish(republish_topic, formatted_message)
        print(f"Republished to {republish_topic}: {formatted_message}")



# MQTT Client Setup
client = mqtt.Client()
client.on_message = on_message
client.connect(broker)
client.subscribe(esp32_topic)
client.subscribe(command_topic)

print("MQTT Client started. Listening for messages...")
client.loop_forever()
